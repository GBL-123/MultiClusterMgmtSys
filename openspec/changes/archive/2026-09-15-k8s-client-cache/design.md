# k8s-client-cache Design

## Context

现状:每个 K8s 调用点都是 `KubernetesClientConfig.Build(entity)` → `clientFactory(config)` → `using var client`(约 50 处,分布在 ConfigMapService / SvcService / WorkloadService / NamespaceService / ClusterNodeService / ClusterService.ProbeAsync)。每次调用都是全新 `HttpClient` 连接池 + 一次完整 TLS 握手;`repo.GetByIdAsync` 拿到的实体在方法内已知。既有约束:

- 工厂 `Func<KubernetesClientConfiguration, IKubernetes>` 注册在 `Program.cs`,是全部测试 mock 的注入点(`K8sMocks.Factory`);
- 统一 10s 超时由 `KubernetesClientConfig.Build` 施加,契约 `kubernetes-call-timeout` 有测试钉死;
- Service 全部 scoped,缓存要跨用户会话共享必须 singleton。

## Goals / Non-Goals

**Goals:**

- 同集群、凭据未变的调用复用客户端与连接池,消除每次调用的 TLS 握手;
- 失效不需要任何编辑路径配合(观察式失效);
- 现有测试基建(工厂注入、mock、超时契约测试)零改动通过。

**Non-Goals:**

- 不做凭据加密落盘(独立 change);
- 不做 `GetByIdAsync` 凭据投影裁剪(独立小刀);
- 不改变任何 K8s 服务的输入/输出契约与异常翻译语义;
- 不引入 IHttpClientFactory 式的 handler 池改造。

## Decisions

### D1: singleton `IClusterClientCache`,实体传入、同步签名

`IKubernetes GetOrCreate(ClusterInfo cluster)`。

- **为什么同步**:Service 调它之前必然已持有实体(判 NotFound、审计文案都要用),缓存内部是纯内存操作,无 I/O,async 无意义。
- **为什么传实体不传 Id**:收 Id 要么 Service 查两次库,要么把"按 Id 取实体"的职责搅进缓存。实体传入 = 零额外 DB 命中。
- **替代方案**:DI 动态按集群注册客户端(无法干净实现)、按指纹为键去重共享(省内存但驱逐所有权复杂化)——均否决。

### D2: 凭据指纹自愈失效(不依赖显式通知)

每次 `GetOrCreate` 对实体五个凭据输入 `ConnectionType / KubeConfig / ApiServer / Token / SkipTlsVerify`(即 `Build` 的全部输入)求哈希指纹,与缓存条目比对:相同复用,不同则重建并替换条目。

- **为什么压倒显式失效**:`Invalidate(id)` 钩子把"谁改了凭据"的知识分布到所有编辑路径——今天只有一处编辑入口,但任何未来入口(批量导入、种子、其他服务)漏挂即泄漏旧凭据客户端。指纹失效是被观察的,不依赖通知;哈希几 KB kubeconfig 文本是微秒级,对面省的是 100~300ms 握手。
- **已知瑕疵(接受)**:KubeConfig 模式下 `ProbeAsync` 会用探测结果回填 `ApiServer`(null → host),指纹含 ApiServer 导致每个集群首次探测成功后多重建一次客户端。为省一次握手为指纹做模式分支不值得——接受这次多余重建。若日后实测有影响,再收窄 KubeConfig 模式指纹为 `Hash(KubeConfig)`。
- **哈希拼接防注入**:各字段分别哈希后组合(或长度前缀拼接),避免 kubeconfig 文本含分隔符导致的碰撞。

### D3: `IMemoryCache` 承载,滑动空闲过期 + 驱逐时 dispose

条目 `(fingerprint, client)`,滑动过期 10 分钟(默认值,可再调),驱逐回调释放客户端。容量上限设远超实际规模的值(如 100),压力驱逐在现实集群量级下永不触发。

- **为什么驱逐释放而非永不 dispose**:永不 dispose 更简单且彻底无竞态,但死集群客户端会占 socket 到进程结束;驱逐释放更卫生。竞态分析见 R1——窗口实际为 0,且有既有降级链路兜底,倾向卫生。
- **驱逐语义**:滑动过期要求 10 分钟内无任何 `GetOrCreate` 触碰;在途请求意味着条目刚被触碰,而单次调用上限 10s,窗口数学上存在、实际为 0。

### D4: 工厂保留为唯一 miss 路径

缓存未命中/指纹变化时仍走 `Build` + `Func<...>` 工厂。

- **动机**:① 超时契约测试(捕获 config 断言 10s)原样通过;② 全部测试的 mock 注入点不变;③ `KubernetesClientConfig` 与工厂注册零改动。
- **对现有测试的影响面**:`K8sMocks.Factory(_ => mock.Object)` 本来就对每次调用返回同一 mock 实例,缓存把"每次新建"变成"每次同实例"后行为等价;唯一理论风险是某测试依赖"中途换 mock 实例"的巧合——仓库模式是一测试一 mock,风险极低,以全量回归硬验证。

### D5: 探测路径同样走缓存

`ProbeAsync` 的两个调用(版本 + 节点)与其他调用一样取缓存客户端。

- **"每轮新鲜客户端更保险"不成立**:指纹失效已覆盖配置变化;客户端的传输层复用不会隐藏集群健康状态(探测结果由 API 响应决定,与客户端新旧无关)。
- 收益:同步每轮省 N 次客户端创建 + N 次握手;探测并发 4 打的是 4 个不同集群条目,无共享争用。

## Risks / Trade-offs

- **[R1] 驱逐与在途请求竞态** → 窗口实际为 0(见 D3);即使命中,`ObjectDisposedException` 走既有 `K8sExceptionMapper` → `ExceptionPresenter` 降级,无新故障类别(spec 已将其契约化为降级场景)。
- **[R2] 凭据在内存中驻留变长** → 缓存的客户端内部持有凭据至过期。进程内存本就在调用期间持有凭据,差异是驻留时长;与凭据加密(Non-Goal)同属安全面,由后续 change 收口。
- **[R3] 测试中 mock 实例语义变化** → 见 D4 影响面分析;实现后跑全量 600 测试为硬验证。
- **[R4] `Kubernetes` 客户端线程安全** → 无状态包装 `HttpClient`,官方推荐即"每集群一个复用";spec 的并发共享场景以测试固化。

## Migration Plan

1. 新增 `Services/ClusterClientCache.cs` + 接口,`Program.cs` 注册 singleton;
2. 6 个 K8s 服务逐个机械替换获取行(`using var client = clientFactory(config)` → `var client = clientCache.GetOrCreate(entity)`),每替换一个跑相关服务测试;
3. 新增缓存单测(命中/指纹重建/驱逐释放/探测复用/并发共享);
4. 全量 `dotnet build` + `dotnet test` 回归。

回滚:纯代码改动,无 schema/数据卷入,revert 即可。

## Open Questions

- 空闲过期时长取 10 还是 15 分钟——可安全后定,默认 10 分钟;不影响 spec 与任务拆分。

## Context

现状:单项目 slnx(应用 + docker-compose),无测试项目;`AGENTS.md` 明言没有测试命令。服务层可测性良好(主构造函数注入、SQLite 内存库可直连、`KubernetesException` 可构造 V1Status),唯一死角是 K8s 直连(`new Kubernetes(config)`)。近期真实 bug 类型:组件对 MudBlazor API 的误用(DateRangePicker 绑定)、异常/查询契约无回归保护。用户决策:xUnit、服务边界测试、bUnit 只测"自己的接线"、K8s 工厂一并改造。

## Goals / Non-Goals

**Goals:**
- 建立可跑的测试项目(`dotnet test`,0 失败)
- 覆盖异常映射/查询契约/校验规则的回归防线
- K8s 工厂注入使服务端到端可测
- bUnit 覆盖"组件↔MudBlazor 接线契约"(不含 MudBlazor 内部)

**Non-Goals:**
- 不测 MudBlazor 本身(不断言 `.mud-*` 内部 DOM/CSS/弹层行为)
- 不做 bUnit 全页面渲染冒烟(只测高价值接线契约)
- 不测登录/登出 HttpContext 流、审计写失败静默路径、CSS/滚动条类 UI 问题
- 不引入覆盖率工具/CI 门槛(可后续变更)

## Decisions

**D1. 测试项目布局(镜像主项目 + TestInfrastructure)**
```
MultiClusterMgmtSys.Tests/
├── Common/Exceptions/K8sExceptionMapperTests.cs
├── Services/  (AccountService/ClusterService/ClusterNodeService/AuditService/GroupService/ConfigMapService/ExceptionPresenter 各一 Tests)
├── Components/ (bUnit:Clusters 门控+FilterBar、Profile 空态、Configmaps 徽章)
├── TestInfrastructure/
│   ├── SqliteDbFactory.cs     ← 每测新建 :memory: 库(独立连接保持打开)
│   ├── TestData.cs            ← 集群/分组/审计造数
│   ├── TestHarness.razor      ← MudBlazor providers + auth(Admin/Member)
│   └── AuthTestHelper.cs
```
镜像目录只为可发现性;测试类型(仓库/SQLite/bUnit)由文件名与 TestInfrastructure 表达,不建 Unit/Integration 扁平分类。

**D2. 后端测试口径:服务为边界,仓库穿服务测**
- 直接调 `Service` 公开方法,内部经真实 SQLite 内存库执行(与生产同 provider,不用 EF InMemory)
- 断言类型:`NotFoundException`/`ValidationException` 等业务异常(含中文 UserMessage)、`PagedResult` 结果、审计查询条数/顺序
- 查询契约(GroupId 0→NULL、版本 OnlyNull、日期区间)经 `ClusterService.GetPagedAsync` 端到端断言
- 数据库每测新建(EnsureCreated),避免状态污染;必要时 TestData 播种

**D3. K8s 客户端工厂(行为零变化)**
- 3 个服务构造加 `Func<KubernetesClientConfiguration, IKubernetes> clientFactory`,`new Kubernetes(config)` → `clientFactory(BuildConfig(entity))`
- Program.cs:`AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(cfg => new Kubernetes(cfg))`
- 测试:Moq `IKubernetes`(只 setup 用到的 CoreV1 成员),`ThrowsAsync(new KubernetesException(new V1Status { Code = 404, ... }))` 断言服务抛 `NotFoundException`
- 备选:完整 `IKubernetesClientFactory` 接口 —— 过度设计,Func 已够

**D4. bUnit 口径:测试"你的组件如何使用 MudBlazor"**
- 通过 `FindComponent<T>()` 取 MudBlazor 组件**实例**,`InvokeAsync` 触发其公开事件/参数,断言**自己组件**的状态/回调/渲染分支
- 断言对象仅限:自己传的参数、自己绑的事件、自己的条件渲染、自己的 CSS 类(如 `.status-badge`/`.empty-state`/`.link-primary`)
- 禁:断言 `.mud-*` 内部类、模拟 MudBlazor 内部交互流程
- 渲染树需包 TestHarness(providers + auth),组件级 API 稳定,不随 MudBlazor DOM 变化碎

**D5. MudBlazor 分析器告警转错误**
- 主项目 csproj 配置 `WarningsAsErrors` 含 MudBlazor 分析器告警(`MUD0002` 等)——`@bind-Value` 用在无 `Value` 参数的组件上会在构建期失败,而非运行时静默失效
- 现有存量 MUD0002 告警(如 `Title` 属性误用)先清零再开闸:将 `Title=` 改为 `aria-label`/合法属性或加 `NoWarn`(执行期逐条处理)

**D6. 验证与文档**
- 命令:`dotnet test MultiClusterMgmtSys.slnx`(或 `--project MultiClusterMgmtSys.Tests`)
- AGENTS.md:Commands 增 `dotnet test`;新增 Testing 约定(服务边界、bUnit 口径、TestInfrastructure 用法)

## Risks / Trade-offs

- [SQLite 内存库与 Identity 种子交互(AccountService 测试需建用户/角色)] → TestData 提供 `SeedUserAsync`(真实 UserManager 建库),覆盖"新旧密码相同"与"成功改密"路径
- [Moq IKubernetes 大接口 setup 繁琐] → 只 setup 被测方法;成员未被调用时 Moq 返回默认值,断言只关心抛出的业务异常
- [存量 MUD0002 告警清零工作量] → 当前构建已列出全部告警点(约 10 处,多为 `Title` 属性),逐条替换为 `aria-label`/`Tooltip`,量小
- [bUnit 测试慢/脆] → 只保留 4 个高价值契约测试,不做全页面冒烟;失败时优先看自己组件逻辑而非 MudBlazor

## Migration Plan

- 纯新增 + 工厂注入(行为不变);`dotnet build` 0 错误 + `dotnet test` 全绿为完成标准
- 回滚 = git revert;测试项目不影响生产构建

## Open Questions

- 无阻塞项。执行期细化:存量 MUD0002 逐条处理方式(改属性 vs NoWarn)、bUnit 契约测试数量上限(4-5 个)。
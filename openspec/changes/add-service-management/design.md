# add-service-management — 设计

## Context

系统已有两类 K8s 资源管理功能可作模板:ConfigMaps(模板 A:列表/详情 YAML/创建/删除,CoreV1 单资源)与 Workloads(模板 B:多 Kind + Scale/Restart 专属操作)。K8s Service 是 CoreV1 资源,骨架落在模板 A,但展示面更富(类型/端口/Endpoints)且有自己的不可变性规则。多集群场景下集群版本不受控(EndpointSlice v1.21 GA,旧 Endpoints v1.33 弃用),读取路径必须兼容老集群。

命名约束:`Service` 与项目 `Services/` 目录(C# 服务)及 K8s 模型 `V1Service` 高频撞名,全链路采用 K8s 圈通用缩写 `Svc` 作为唯一可 grep token。

## Goals / Non-Goals

**Goals:**

- 服务列表(端口一等公民 + 对外入口列)、详情(YAML + 端口表 + Endpoints)、创建/编辑/删除(Admin)。
- 编辑对不可变字段安全:冲突在打到 API 之前以中文业务异常呈现。
- Endpoints 读取兼容新旧集群,统一数据形态。
- 与现有功能同构:异常翻译链、审计、服务契约(Requests 入 / ViewModels 出)、Swiss Industrial Print 视觉。

**Non-Goals:**

- 不做 Ingress(网络分组仅为预留位)。
- 不做跨集群服务对照/聚合视图、端口占用全局搜索。
- 不迁移或修改既有 `ConfigMapService.BuildConfig` 为共享基类(保持各服务独立,与现状一致)。

## Decisions

### D1:命名 — 全链路 `Svc` 前缀

`SvcService`(`Services/SvcService.cs`)、`SvcQueryRequest`/`SvcKeyRequest`/`SvcCreateRequest`/`SvcUpdateRequest`、`SvcListViewModel`/`SvcDetailViewModel`/`SvcPortViewModel`/`SvcEndpointViewModel`、`SvcMappingExtensions`、页面 `Components/Services/`(Razor 命名空间随物理路径 `MultiClusterMgmtSys.Components.Services`)、路由 `/services`、`AuditCategory.Service = 7`。

理由:`Service` 一词在本库有三种身份(C# 服务层、K8s 对象、路由),`grep Service` 不可用;`Svc` 唯一且符合 `kubectl get svc` 的圈子习惯。备选 `ServiceService`(机械一致但难读)、`ServiceManagementService`(冗长)已否决。

### D2:YAML 编辑 — 读回 existing + 不可变字段守卫 + 整对象 Replace

```
用户 YAML → Deserialize<V1Service>
  → 读回 existing(取 clusterIPs/ipFamilies/resourceVersion/uid/status)
  → 守卫:用户字段与现有不同 → ValidationException(中文,先于 API 调用)
          用户字段为空 → 以 existing 补齐;相同 → 放行
  → 补 resourceVersion/uid、保留 status → ReplaceNamespacedServiceAsync
```

理由:ConfigMap 的"白名单合并"在 Service 上可变面过大(selector/ports/type/sessionAffinity/trafficPolicy…),逐字段白名单会静默吞掉用户想改的字段——比报错更糟。整对象覆盖让可变字段天然全量生效;显式守卫把唯一真正不可变的 `clusterIP`/`clusterIPs`/`ipFamilies` 冲突提前到 UI 层给出中文提示。残余风险(type 变更的约束、ipFamilyPolicy 边缘规则)由 `K8sExceptionMapper` 409→Conflict 兜底。备选"纯 Replace 让 API 报 422"(错误信息生硬)与"白名单合并"(静默失真)已否决。

创建路径无守卫(clusterIP 填了即指定、不填自动分配),仅校验 `metadata.namespace` 必填,与 ConfigMap 一致。

### D3:Endpoints 读取 — EndpointSlice 优先,404 降级旧 Endpoints

```
DiscoveryV1.ListNamespacedEndpointSlice(label: kubernetes.io/service-name=<svc>)
  ├─ 200 → 展平全部 slice → 统一 VM
  └─ 404(discovery API 组不存在 = v1.21 前集群)
        → CoreV1.ListNamespacedEndpoints(svc) → 统一 VM
```

理由:EndpointSlice 是 GA 方向(旧 API v1.33 起弃用),但多集群管老集群是真实场景;404 恰好命中现有 `K8sExceptionMapper` 的 404→NotFound 映射,catch 中可精确区分"老集群"与"真故障"(仅 404 降级,其余照常 Translate 抛出)。两类来源折叠为同一 `SvcEndpointViewModel`(地址:端口 + Ready 徽章),UI 不感知来源。新集群上旧 Endpoints 对象仍由 API server 维护,降级路径恒可用。k8s client 19 两个 API 均现成(`client.DiscoveryV1` / `client.CoreV1`),无新依赖。

### D4:列表端口列 — 每端口一行,kubectl 语法 + targetPort

- NodePort/LoadBalancer:`{port}:{nodePort}/{protocol} → {targetPort}`;其余:`{port}/{protocol} → {targetPort}`;命名 targetPort 显示端口名。
- `V1ServicePort.TargetPort` 为 `IntstrIntOrString`:`SvcPortViewModel.TargetPort` 收成 `string?`,映射扩展按 `HasString`/`HasInt32` 分支(覆盖 `8080` 与 `http` 两种形态)。
- 超过 3 个端口截断为前 3 行 + `+N`(详情页端口表全量)。
- Headless(`clusterIP: None`)显示 mono `None`;ExternalName 端口格显示 `[ 暂无 ]`。
- 「对外入口」列仅取自 Service 对象自身(LB ingress 地址 / `*:{nodePort}` / externalName DNS / `—`),零额外 K8s 调用。

理由:复用 kubectl `PORT(S)` 视觉语法零学习成本;对外暴露与端口映射是两个问题(安全审计 vs 流量拓扑),分列呈现。

### D5:结构 — 复制模板 A(ConfigMaps)骨架

`Components/Services/Pages`(Services.razor、ServiceDetail.razor、EditServiceYaml.razor)+ `Shared`(列表表格、过滤栏、YAML 查看/编辑卡、创建对话框、详情工具栏、端口卡、Endpoints 卡)。导航为「网络管理」`MudNavGroup`(图标 `Icons.Material.Filled.Lan`),子项图标避开已占用的 `Dns`(建议 `SettingsEthernet`),展开状态照抄 `workloadsExpanded` 模式。`Program.cs` 注册 `SvcService`(scoped)。

### D6:审计 — 扩展现有枚举与调用点

`AuditCategory` 追加 `Service = 7`,`AuditAction` 复用现有 创建/修改/删除;三个写操作成功后 `auditService.LogAsync(AuditCategory.Service, …)`,目标格式 `服务: {ns}/{name} @ 集群 {clusterName}`(与 ConfigMap 一致)。

## Risks / Trade-offs

- [K8s 不可变性规则演进(ipFamilyPolicy 等边缘字段)] → 守卫仅锁定三类核心不可变字段,其余交由 API 校验,409/422 经异常翻译呈现中文 Conflict 错误。
- [老集群 EndpointSlice 404 之外的可能失败形态] → 仅捕获 404 降级;其余异常走 `Translate` 正常报错,避免吞掉真实故障。
- [列表列数多(8 列)导致横向拥挤] → 端口列截断 +3 行、DataLabel 响应式折叠;若仍拥挤,创建时间列为第一让位项(详情页有)。
- [多端口大服务(如 >10 端口)列表行高不齐] → 3 行截断兜底;详情页全量展示。
- [替换时 resourceVersion 过期(并发修改)] → API 409 经 `K8sExceptionMapper` → `ConflictException`,用户刷新后重试。

## Migration Plan

纯增量功能,无 schema 变更、无数据迁移。部署 = 正常发布;回滚 = 回退镜像。`AuditCategory` 新枚举值向后兼容(既有行不受影响)。

## Open Questions

- 列表筛选栏是否加"端口搜索"(文本匹配 port/nodePort)?成本低、排障价值真实,默认**做**,实施时若发现过滤链复杂可降级为后续 change。
- 创建时间列显示完整时间(与 ConfigMap 一致)还是 Age?默认与 ConfigMap 一致。

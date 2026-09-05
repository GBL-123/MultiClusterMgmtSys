# design — workload-management

## Context

系统已有三类 K8s 资源管理(集群、节点、配置),其中 ConfigMap 模块是最接近的样板:命名空间级对象、列表/详情/YAML 视图编辑/新建/删除的完整闭环,以及 `WorkloadService` 将要复用的全部基础设施(`BuildConfig`、`K8sExceptionMapper.Translate`、`AuditService.LogAsync`、`ClusterSelectionState`、`ExceptionPresenter`)。

工作负载与 ConfigMap 的本质差异:**它是四型的"家族"**——apps/v1 下的 Deployment / StatefulSet / DaemonSet / ReplicaSet 共享同构的 CRUD 心智,但 API 方法、状态字段、可执行操作逐型不同。设计的核心就是把这个"分叉"压到映射层/枚举层,让页面层保持共享。

约束:遵循仓库既有约定(服务层禁 MudBlazor 类型、输入收拢 `Requests/`、输出 `ViewModels/`、`@inject` 顶部注入、Swiss Industrial Print 设计系统、中文文案)。

## Goals / Non-Goals

**Goals:**

- apps/v1 四型工作负载的完整管理闭环:列表、详情、YAML 新建/编辑、删除、扩缩容、滚动重启
- 滚动指示(就绪/滚动中/异常)提供运维反馈闭环
- 分叉收敛:一个统一列表行模型 + 逐型映射;共享组件 ×4 页面复用

**Non-Goals:**

- 回滚(基于历史的复杂逻辑)
- 镜像 tag 编辑 UI(YAML 编辑已覆盖)
- Pod 下钻、Job/CronJob(batch/v1)、暂停/恢复 rollout、Server-side Apply
- 集群级资源(未纳入 apps/v1 之外的范围)
- 详情页 tab 化布局:实现过 MudTabs 与自绘 tab 两版,均回退——留作后续独立 change(需先解决 MudBlazor 9.9 display:contents 面板与层叠陷阱,或沿用自绘方案并重新过设计评审)

## Decisions

### D1. 统一行模型,分叉压在映射层

一个 `WorkloadListViewModel`(Name、Namespace、Kind、就绪 n/m、滚动状态、CreatedAt)吃下四型;`WorkloadMappingExtensions` 为每种 K8s 类型提供 `ToWorkloadListViewModel()`,**就绪/滚动算法逐型实现,页面层零分叉**。一张 `WorkloadListTable`、一个 `WorkloadListFilterBar` 服务全部列表页。

- 理由:四型的列表心智完全同构(名称/命名空间/就绪度/年龄);差异只在"把哪些 status 字段算成就绪"。
- 备选:每型独立 ViewModel+Table——×4 重复代码,否。

### D2. 页面组织:每型独立页面,共享组件做薄

4 个薄列表页(`Deployments.razor` 等,~100 行接线)+ 4 个薄详情页,全部组合共享组件。路由沿用 ConfigMaps 惯例:列表 `/workloads/deployments` 与 `/workloads/deployments/{ClusterId:int}`,详情 `/workloads/deployments/{ClusterId:int}/{Namespace}/{Name}`。Drawer 用 `MudNavGroup`(Prefix match `/workloads`)下拉展开 4 项。

- 理由:用户明确要求导航下拉展开独立管理页;每型独立页面让路由语义清晰,且运行时无需 kind 分发;薄页面 + 共享组件兼顾两种诉求。
- 备选:单页 + kind 路由参数——导航形态不符;每型完整独立代码——重复严重,否。

### D3. WorkloadService:显式 per-kind 方法,不搞泛型反射

`Services/WorkloadService.cs` 约 20 个方法:`ListDeploymentsAsync` / `GetDeploymentAsync` / `CreateDeploymentFromYamlAsync` / `UpdateDeploymentFromYamlAsync` / `DeleteDeploymentAsync` / `ScaleDeploymentAsync` / `RestartDeploymentAsync`,StatefulSet/DaemonSet/ReplicaSet 同构命名。KubernetesClient 的 `V1Deployment`/`V1StatefulSet`/`V1DaemonSet`/`V1ReplicaSet` 无共享接口,泛型化只能靠反射/字符串体操,违背仓库"显式可读"风格。

- DaemonSet 不提供 Scale 方法,ReplicaSet 不提供 Restart 方法——**用方法缺席表达能力矩阵**,而非运行时抛错。

### D4. 扩缩容走 scale 子资源,重启走注解 patch

- **Scale**:`ReadNamespacedXxxScaleAsync` → 修改 `V1Scale.Spec.Replicas` → `ReplaceNamespacedXxxScaleAsync`。不读整对象改 replicas 再 replace——那会和 controller 的 status 写入打架(409)。适用:Deployment / StatefulSet / ReplicaSet。
- **Restart**:对 `spec.template.metadata.annotations` 打 `kubectl.kubernetes.io/restartedAt=<RFC3339>` 的 StrategicMergePatch(`PatchNamespacedXxxAsync` + `V1Patch`),与 kubectl rollout restart 同一约定。适用:Deployment / StatefulSet / DaemonSet。

### D5. YAML 编辑冲突策略:方案 A(读最新,只覆盖 spec)

保存时序:`GET 最新对象` → `KubernetesYaml.Deserialize` 用户 YAML → **只把 `spec` 覆盖到最新对象上**(metadata 一律以服务器最新值为准)→ `ReplaceNamespacedXxxAsync`(携带最新 resourceVersion)。冲突窗口缩至毫秒级;万一仍 409,由 `K8sExceptionMapper` 翻译为 ConflictException,UI 提示"集群状态已变化,请重试"。

- 用户 YAML 只取 `spec`:status 是 controller 领地,metadata(labels/annotations)由服务器权威,永不覆盖。
- 备选:Server-side Apply(KubernetesClient 19 content-type 支持别扭,需 spike,否);裸 replace(冲突/覆盖 controller,否)。
- 新建 YAML 与 ConfigMap 同构:反序列化 → 校验 `metadata.namespace` 必填 → `CreateNamespacedXxxAsync`。

### D6. 操作列职责分配

```
列表页头: [刷新] [新建(Admin)]           ← 新建 = YAML 模板对话框(CreateWorkloadDialog,逐型模板)
列表行:  name(.link-primary → 详情) · 就绪 n/m · 滚动状态 · 创建时间
         操作: [扩缩容] [重启] [⋯ → 删除]   ← 按能力矩阵条件渲染
详情页:  toolbar [刷新] [编辑 YAML] [扩缩容] [重启] [删除]
        + 状态卡 · YAML 视图卡(纵向堆叠,ConfigMapDetail 同款布局)
```

高频运维动作(扩缩/重启)住列表行,低频/上下文动作(编辑 YAML)与破坏性动作(删除)住详情页;`⋯` 菜单收纳删除以备顺手。重启需轻确认(ConfirmDialog:"将触发滚动,Pod 会逐步替换"),删除需强确认(文案明确级联:"将删除其管理的副本与 Pod")。扩缩容弹出 `WorkloadScaleDialog`(当前值预填)。

### D7. 滚动指示:三态判定,逐型取数

| 状态 | 语义 | 判定原则 |
|---|---|---|
| 就绪 | 稳定 | ready == desired 且 updated == desired |
| 滚动中 | 变更进行中 | updated < desired,或 generation > observedGeneration |
| 未就绪 | 卡住/不足 | 其余(ready < desired 且非滚动中) |

逐型字段映射:Deployment(readyReplicas/updatedReplicas/replicas + generation/observedGeneration)、StatefulSet(readyReplicas/updatedReplicas + currentRevision≠updateRevision 视为滚动中)、DaemonSet(numberReady/updatedNumberScheduled/desiredNumberScheduled)、ReplicaSet(readyReplicas/replicas + generation 差)。列表以 `status-badge` + `.font-mono` "n/m" 呈现。

### D8. 审计与权限

`AuditCategory.Workload = 6`;`AuditAction` 增加 `Scale = 8`、`Restart = 9`(创建/修改/删除复用现有值)。审计描述:"部署: {ns}/{name} @ 集群 {name}"、"扩缩容 Deployment {ns}/{name} → {n} @ 集群 {name}"、"重启 StatefulSet {ns}/{name} @ 集群 {name}"。全部写操作以 `<AuthorizeView Roles="Admin">` 门控,查看角色无关——与 ConfigMaps 一致。

### D9. 测试策略

- 服务层:真实 SQLite 内存库 + `TestServices.ThrowingFactory()` 验证 K8s 异常翻译链(`KubernetesException(V1Status{Code=404})` → NotFoundException 等);YAML 解析失败 → ValidationException("YAML 格式错误:…")
- 状态算法:构造假 `V1Deployment` 等对象,断言三态与 n/m 计算
- bUnit:`WorkloadListTable` 接线契约(组件实例 + 事件参数 + 自有 CSS 类);能力矩阵条件渲染(DaemonSet 行无扩缩按钮、ReplicaSet 行无重启按钮)

## Risks / Trade-offs

- [GET→PUT 毫秒级冲突窗口仍存在] → 409 经 `K8sExceptionMapper` 翻译,UI 明确提示重试;可接受
- [重启注解是客户端约定而非原生 API] → 与 kubectl rollout restart 使用同一注解,生态兼容
- [大集群全 namespace 列表可能慢] → 与 ConfigMaps 同策略:命名空间过滤 + 名称搜索;保持一致,不引入缓存
- [删除工作负载级联删 Pod,破坏面大] → 强确认文案明确级联影响;审计留痕
- [MudBlazor 组件 API 误用(MUD0002 直接编译失败)] → 遵循 `_Imports`/MudBlazor 9.9 约定;`MudDateRangePicker` 类陷阱(无 `@bind-Value` 的组件)在本模块无场景
- [AuditLog 枚举扩展与历史数据] → int 存储,新增枚举值不影响旧记录

## Migration Plan

纯增量变更,无 DB schema 变更、无迁移。部署 = 新版本上线;回滚 = 还原代码版本,无数据清理需求。

## Open Questions

无——探索阶段已收敛全部关键决策。

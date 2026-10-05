# Tasks

## 1. 契约类型与服务骨架

- [x] 1.1 `Application/Requests/ComparePairQueryRequest` + `CompareCloneRequest`(中文 XML 注释与 `<param>`)
- [x] 1.2 `Application/ViewModels/ComparePairViewModel`(含 SourceExists/TargetExists/SourceYaml/TargetYaml/HasDifference/DifferenceCount)与 `YamlDiffRowViewModel`(Left/Right 文本 + 行类)或等价行分类 record
- [x] 1.3 `ClusterCompareService` 骨架(族 switch + 两簇并行读取 + 任一侧缺失标注;`AppServiceCollectionExtensions` 注册)— 阅读既有 `WorkloadService` 四类 YAML 读取与 `ConfigMapService` 创建方法签名后按实测方式接入

## 2. 服务层单测(先写,确认口径)

- [x] 2.1 对照读取:同名两侧都存在 → 双 YAML + 差异计数;目标缺失 → TargetExists=false;源缺失 → 中文错误(按族既有「不存在」异常)
- [x] 2.2 同集群选择 → ValidationException
- [x] 2.3 剥离单测:含 uid/resourceVersion/creationTimestamp/status/managedFields/mcms 标签注释的 YAML 剥管后不含以上任一;数据段(如 ConfigMap data)原样保留
- [x] 2.4 克隆:走既有创建服务 mock 断言(参数含剥管文本、目标集群);冲突(K8s 429/409 → ConflictException 中文)与 Member kube- 命名空间拒绝透传既有异常
- [x] 2.5 权限口径:Member 对照读取可见(既有列表样式),无 CanOperate 时的克隆入口隐藏由 ViewModel/服务层字段承载并断言

## 3. 对照页与 diff 渲染

- [x] 3.1 `Web/Components/Compare/Pages/Compare.razor`(路由 /compare,页面根 `flex-auto`,双簇选择+族+ns 选择+资源查询,`MudSelect` 与既有双语约定)
- [x] 3.2 `Components/Compare/YamlDiff.cs`(纯函数 LCS 行 diff,零依赖,单元测试直接对接)
- [x] 3.3 `Shared/CompareDiffView.razor`(双栏对照 + `.is-changed/.is-added/.is-removed` 高亮 + 「仅看差异」开关;`app.css` 新 15.x 节)
- [x] 3.4 Drawer 新增「跨集群对照」NavLink(Helm 之后),`MudNavGroup` 之外的独立项

## 4. 克隆交互

- [x] 4.1 `Shared/CloneConfirmDialog.razor`(剥管 YAML 预览 + 目标集群名 + 确认/取消;复用 `ConfirmDialog` 风格而非 MudForm 提交流)
- [x] 4.2 `Compare.razor` 接线:差异非零或目标缺失时出现克隆入口;执行中禁用;完成/失败后刷新对照与提示(等待 `IDialogReference` 结果,对照既有模式)
- [x] 4.3 异常映射核对:读取/克隆失败经 `ExceptionPresenter`(业务异常显示中文 UserMessage,非业务异常通用提示)

## 5. bUnit 测试

- [x] 5.1 `ComparePageTests`(渲染选择器、同集群选择提示、目标缺失标注、差异高亮类存在性、「仅看差异」切换)
- [x] 5.2 `CloneConfirmDialogTests`(预览文本无 mcms 标签、确认触发服务调用、取消不调用)
- [x] 5.3 服务栈注册 : `BunitServiceExtensions` 新增 `AddCompareStack`(含 WorkloadService/ConfigMapService/CompareService + 客户端缓存 mock)

## 6. 收尾

- [x] 6.1 `openspec validate "add-cross-cluster-compare" --strict` 通过
- [x] 6.2 `dotnet build` 0 错误;`dotnet test` 全绿(基线 1141 + Change A 新增 + 本 change 新增);`./coverage.ps1` ≥ 75%

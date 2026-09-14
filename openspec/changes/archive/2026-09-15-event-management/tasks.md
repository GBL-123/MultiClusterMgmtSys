## 1. 服务与映射层

- [x] 1.1 新建 `Requests/EventQueryRequest.cs`(`record EventQueryRequest(int ClusterId)`)与 `ViewModels/EventListViewModel.cs`(类型/原因/消息/关联对象/次数/时间/来源字段及派生属性),`dotnet build MultiClusterMgmtSys.slnx` 0 错误
- [x] 1.2 在 `ViewModels/Mappings/K8sDisplayText.cs` 增加事件类型双语映射(`Normal` → 正常、`Warning` → 警告,未登记回退原文);在 `ViewModels/Mappings/EventMappingExtensions.cs` 实现 `Corev1Event -> EventListViewModel`,包含四级时间回退(`LastTimestamp` → `Series.LastObservedTime` → `EventTime` → 元数据创建时间)与 kind→路由静态白名单(Node/Namespace/ConfigMap/Service/四类工作负载);补映射单测(LastTimestamp 为空、时间全空、count=1、未知类型回退)并运行通过
- [x] 1.3 新建 `Services/EventService.cs`:`ListEventsAsync(EventQueryRequest)` 单次 `CoreV1.ListEventForAllNamespacesAsync` + `GetNamespacesAsync(int)`;经 `KubernetesClientConfig.Build`(统一 10s 超时)与 `K8sExceptionMapper.Translate`,集群不存在抛 `NotFoundException`,不注入审计;`Program.cs` 注册 scoped;`dotnet build` 0 错误
- [x] 1.4 `TestInfrastructure/K8sMocks` 增补事件列举 setup(底层 `ListEventForAllNamespacesWithHttpMessagesAsync`);新建 `MultiClusterMgmtSys.Tests/Services/EventServiceTests.cs`(全命名空间映射、时间回退、集群不存在、403 翻译、命名空间排序),`dotnet test` 相关用例全绿

## 2. 前端过滤

- [x] 2.1 新建 `Models/EventListFilter.cs`:`Apply(namespace/type/keyword)` 纯前端过滤,关键词包含匹配关联对象名、reason 与 message(忽略大小写),类型比对保持 K8s 原始值;补单测覆盖筛选组合与空条件,`dotnet test` 通过

## 3. 页面与组件

- [x] 3.1 `wwwroot/css/app.css` 增加 `.status-badge.warning`(琥珀 `#FBF3DB/#956400`)与 `.status-badge.normal`(中性 `#EDEAE3/#57534E`)变体,保持淡彩底 + 深字公式与 3px 圆角;`dotnet build` 0 错误且既有徽章页面渲染无回归(bUnit 全绿)
- [x] 3.2 新建 `Components/Events/Shared/EventListTable.razor`:类型徽章(`StatusBadge` 中文主行 + 等宽英文次行)、原因、关联对象(白名单可点 `.link-primary`,其余纯文本)、消息单行截断不撑行高、次数 `×N`、最近发生相对时间 + 绝对时间 tooltip、可排序列标签、分页 `共 {all_items} 条`、`[ 暂无事件 ]` 空态与 `// 正在加载...` 加载态
- [x] 3.3 新建 `Components/Events/Shared/EventListFilterBar.razor`:命名空间/类型/关键词即时筛选(无「查询」按钮)、重置、刷新按钮与「数据截至」时间戳展示
- [x] 3.4 新建 `Components/Events/Shared/EventDetailDialog.razor`:等宽完整消息、来源组件(`Source.Component`)、`InvolvedObject.FieldPath`、首见/末见绝对时间、次数;关闭后不回退筛选/排序/分页状态
- [x] 3.5 新建 `Components/Events/Pages/Events.razor`(路由 `/events` 与 `/events/{ClusterId:int}`,`[Authorize]`):集群侧栏、路由参数写入 `ClusterSelectionState`、未选集群提示、集群未找到/离线/加载分支、刷新重取与「数据截至」更新;页面无写操作入口;经 4.2 的 bUnit 用例验证

## 4. 接线与页面测试

- [x] 4.1 `Components/Layout/Drawer.razor` 在「网络管理」之后加「事件管理」导航(无 Admin 门控);`TestInfrastructure/BunitServiceExtensions.cs` 增补事件页服务栈;`dotnet build` 0 错误
- [x] 4.2 新建 `MultiClusterMgmtSys.Tests/Components/Pages/EventsPageTests.cs`(bUnit):无集群提示、离线空态、表格渲染、筛选交互、详情对话框打开/关闭;异步断言经 `WaitForState`/`InvokeAsync` 调度,`dotnet test` 通过

## 5. 验证

- [x] 5.1 `dotnet build MultiClusterMgmtSys.slnx` 0 错误 + `dotnet test MultiClusterMgmtSys.Tests` 全绿(基线 600 + 新增用例)
- [x] 5.2 真机 spike:在 K8s 1.25+ 集群确认 `LastTimestamp` / `Series.LastObservedTime` / `EventTime` 的实际填充,验证回退链取到正确最近发生时间;行为与预期不符时更新映射与单测;无可用集群时记录「未验证」并保留回退链(**已验证:3 个 K8s v1.35.4 集群,`LastTimestamp`/`FirstTimestamp`/`CreationTimestamp` 有值、`EventTime`/`Series` 为空,回退链经 `LastTimestamp` 正确解析;回退分支与单测保留**) 
- [ ] 5.3 手动冒烟:`dotnet run --project MultiClusterMgmtSys`,分别对在线与离线集群验收事件列表、即时筛选、排序、详情对话框、关联对象跳转与刷新时间戳

## 6. 对象类型分类条(级别更名)

- [x] 6.1 `Models/EventListFilter.Apply` 增加 `appliedKind` 参数(精确匹配 `InvolvedKind`),既有调用点同步;新增分类选项模型(`Kind` + `Count` 小 record,落 `Models/`);补筛选单测(kind 精确匹配、与 namespace/级别/关键词组合、空 kind 不进分类),`dotnet test` 通过
- [x] 6.2 `EventListFilterBar` 增加第二行「对象类型」分类条:`MudChipSet<string>` SingleSelection,「全部」用 `""` 哨兵,chip 文案 `Pod 51`;新增参数 `KindOptions`/`SelectedKind`/`SelectedKindChanged`;「类型」筛选与文案更名「级别」(下拉:全部级别/正常/警告)
- [x] 6.3 `EventListTable` 列头「类型」更名「级别」;页面 `Events.razor` 增加 `selectedKind` 状态、分类选项计算(基于其余筛选后集合、不含对象类型自身,计数降序同数按名升序、仅 count>0)与重置联动;build 0 错误
- [x] 6.4 `EventsPageTests` 补用例:点击分类 chip 即筛、计数随其余筛选变化且选中后不塌缩、重置回到「全部」、空 kind 事件仅在「全部」出现;全量 build + `dotnet test` 全绿
- [ ] 6.5 手动冒烟覆盖分类条(与 5.3 一并验收):分类点击、计数联动、与级别/命名空间/关键词叠加

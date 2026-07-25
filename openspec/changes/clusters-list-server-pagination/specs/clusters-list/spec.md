## ADDED Requirements

### Requirement: 集群列表服务端分页查询

系统 SHALL 提供集群列表的服务端分页查询能力，把 GroupId / Name 模糊 / Status / Version / 创建时间区间筛选，以及排序与 Skip/Take 分页全部下推到 EF Core / SQL 层执行，而非在前端内存 LINQ 过滤。查询 MUST 使用 `AsNoTracking`。

#### Scenario: 仅分页无筛选

- **WHEN** 调用 `ClusterService.GetPagedAsync` 传入空筛选条件、`Page=1`、`PageSize=20`
- **THEN** 系统返回 `PagedResult<ClusterViewModel>`，其 `Items` 为按 `CreatedAt` 倒序的前 20 条 ViewModel，`Total` 为集群全量计数

#### Scenario: 按名称模糊筛选

- **WHEN** 调用方设置 `ClusterQuery.Name = "prod"`
- **THEN** 系统 MUST 仅返回 `Name` 包含 "prod"（大小写不敏感）的集群；SQL 翻译 MUST 等价于 `WHERE Name LIKE '%prod%'`，不得在内存过滤

#### Scenario: 按分组筛选

- **WHEN** 调用方设置 `ClusterQuery.GroupId = 5`
- **THEN** 系统 MUST 仅返回 `GroupId == 5` 的集群，未分组集群（`GroupId = null`）不被包含

#### Scenario: 按状态筛选

- **WHEN** 调用方设置 `ClusterQuery.Status = ClusterStatus.Online`
- **THEN** 系统 MUST 仅返回 `Status == Online` 的集群

#### Scenario: 按版本精确筛选

- **WHEN** 调用方设置 `ClusterQuery.Version = "v1.28.0"`
- **THEN** 系统 MUST 仅返回 `Version == "v1.28.0"` 的集群
- **WHEN** 调用方设置 `ClusterQuery.Version = "__NULL__"` sentinel
- **THEN** 系统 MUST 仅返回 `Version` 为 null 或空字符串的集群
- **WHEN** 调用方设置 `ClusterQuery.Version = "__ALL__"` 或 null
- **THEN** 系统 MUST 不对 `Version` 附加任何过滤条件

#### Scenario: 按创建时间区间筛选

- **WHEN** 调用方同时设置 `DateStart` 与 `DateEnd`
- **THEN** 系统 MUST 返回 `CreatedAt >= DateStart` 且 `CreatedAt < DateEnd.AddDays(1)` 的集群（结束日含当天全日）

#### Scenario: 按列排序

- **WHEN** 调用方设置 `SortBy = Name`、`SortDescending = false`
- **THEN** 系统 MUST 以 `Name` 升序返回当前页结果
- **WHEN** 调用方未指定 SortBy 或传入未知值
- **THEN** 系统 MUST 默认以 `CreatedAt` 倒序返回，并在主排序键相同时以 `Id` 倒序作为二级稳定键

#### Scenario: 分页越界返回空页

- **WHEN** 调用方设置 `Page` 超过总页数
- **THEN** 系统 MUST 返回 `Items` 为空列表，`Total` 仍为满足筛选条件的全集计数

### Requirement: 集群分页查询参数与结果 DTO

系统 SHALL 引入 `ClusterQuery`（POCO 查询参数类）与 `PagedResult<T>`（泛型分页结果类）作为查询契约。`ClusterQuery` MUST 至少包含字段：`Name`、`GroupId`、`Status`、`Version`、`DateStart`、`DateEnd`、`Page`、`PageSize`、`SortBy`（枚举 `ClusterSortField`，值为 `Name/Status/Version/NodeCount/CreatedAt`）、`SortDescending`。`PagedResult<T>` MUST 至少包含字段：`Items : List<T>`、`Total : int`。这些类型 MUST 仅在本 change 内自洽，不预先抽共享命名空间或基类。

#### Scenario: 查询参数默认值

- **WHEN** 使用无参构造 `new ClusterQuery()`
- **THEN** `Page` MUST 默认为 1，`PageSize` MUST 默认为 20，`SortBy` MUST 默认为 `CreatedAt`，`SortDescending` MUST 默认为 true，其余筛选字段 MUST 默认为 null（表示不过滤）

#### Scenario: PagedResult 容纳当前页与总数

- **WHEN** 某查询命中 137 条、当前 `Page=3 Page=20`
- **THEN** 返回的 `PagedResult<ClusterViewModel>` 的 `Items.Count` MUST 为 20，`Total` MUST 为 137

### Requirement: 集群仓库分页查询方法

`ClusterRepository` SHALL 新增 `GetPagedAsync(ClusterQuery query)` 方法，返回 `Task<(List<ClusterInfo> Items, int Total)>`，所有筛选 / 排序 / `Skip` / `Take` MUST 在 `IQueryable<ClusterInfo>` 装配完成后再 `ToListAsync` 与 `CountAsync`。该方法 MUST 使用 `AsNoTracking`，且 MUST NOT 把排序 `SortBy` 字段名识别下放到 SQL 字符串拼接，而 MUST 通过 switch 翻译到强类型 `OrderBy` lambda。`GetAllAsync` MUST 保留不变以服务现有依赖者。

#### Scenario: 仓库不分页时仍可调用

- **WHEN** 调用现存的 `ClusterRepository.GetAllAsync()`
- **THEN** 系统必须返回全部集群（含 `Group` 导航属性），行为与现状一致

#### Scenario: 仓库分页查询不计 tracking

- **WHEN** 调用 `GetPagedAsync`
- **THEN** EF Core ChangeTracker MUST 不持有返回实体的跟踪

### Requirement: 集群服务分页查询投影

`ClusterService` SHALL 新增 `GetPagedAsync(ClusterQuery query) → Task<PagedResult<ClusterViewModel>>`，把 `ClusterRepository.GetPagedAsync` 返回的实体投影为 ViewModel，`Total` 透传。Service MUST 负责把 MudBlazor `TableState.SortLabel`（字符串 SortKey）映射到 `ClusterQuery.SortBy` 枚举——这是排序字符串协议翻译的唯一归属点，Repo MUST NOT 认识 ViewModel 字符串名。`GetClustersAsync` MUST 保留不变。

#### Scenario: Service 投影 PagedResult

- **WHEN** Repo 返回 `(Items=[3 实体], Total=137)`
- **THEN** `ClusterService.GetPagedAsync` MUST 返回 `PagedResult<ClusterViewModel>`，`Items` 含 3 个 ViewModel、`Total` 为 137

#### Scenario: Service 翻译未知排序列兜底

- **WHEN** 上层调用传入 `SortBy` 中的未知枚举值或 MudTable 传入未知 SortLabel 字符串
- **THEN** Service MUST 兜底使用 `CreatedAt` 倒序，MUST NOT 抛异常

### Requirement: 集群列表页 ServerData 数据流

`Components/Pages/Clusters/Clusters.razor` SHALL 把 `MudTable` 切换为 `ServerData` 异步回调模式，`RowsPerPage` 默认 20，并配 `<PagerContent><MudTablePager /></PagerContent>`。父页面 MUST 在 `@code` 持有唯一一份 `ClusterQuery` 实例作为筛选真相源，翻页与排序状态由 MudTable 内部持有，MUST NOT 在父页面冗余保存 `Page`、`PageSize`、`SortBy`、`SortDescending` 字段。父页面 MUST 持有 `TotalCount` 字段，由 `OnLoadPaged` 回调在每次返回后从 `PagedResult.Total` 同步，用于显示"共 N 个集群"文案。

#### Scenario: 首次进入页面

- **WHEN** 用户导航到 `/clusters`
- **THEN** 父页面 MUST 先 ping 出 `groups` 与 `availableVersions` 作工具栏下拉数据源，随后 MudTable 自动触发首次 `ServerData` 回调，按默认排序与默认每页 20 等参数加载第一页

#### Scenario: 翻页触发后端拉取

- **WHEN** 用户点击 `MudTablePager` 切换到第 3 页
- **THEN** 父页面 MUST 不清空 filter 字段，MudTable 自动触发回调，系统 MUST 在 SQL 层用对应 `Skip`/`Take` 返回第 3 页数据；MUST NOT 在前端切片全量

#### Scenario: 排序触发后端拉取

- **WHEN** 用户点击"名称"列标题按名升序
- **THEN** MudTable 自动触发回调，系统 MUST 在 SQL 层执行 `OrderBy(Name)`，并回到第 1 页

### Requirement: 集群列表筛选触发后端重拉

当任一筛选字段（Name / GroupId / Status / Version / DateStart / DateEnd）发生改变后，系统 MUST 通过 `ClusterTable.ReloadAsync()` 触发 MudTable 重新拉取并回到第 1 页，MUST NOT 在前端对当前页结果再做内存过滤。集合中所有筛选字段变更与"重置"按钮 MUST 走同一 reload 路径，不得遗漏任一。

#### Scenario: 重置筛选

- **WHEN** 用户点击"重置"按钮
- **THEN** 系统 MUST 把 `ClusterQuery` 所有筛选字段清空、保持排序与分页参数默认，并触发 `ClusterTable.ReloadAsync()` 回到第 1 页

#### Scenario: 改分组下拉

- **WHEN** 用户在分组下拉中选择 `GroupId = 3`
- **THEN** 该改动 MUST 同步到 `ClusterQuery.GroupId`，并触发 Reload 回到第 1 页

### Requirement: 集群列表页组件拆分

`Clusters.razor` SHALL 拆分为三个 Razor 组件：页面壳 `Clusters.razor`、工具栏 `ClusterToolbar.razor`、表格 `ClusterTable.razor`，三文件 MUST 同位于 `Components/Pages/Clusters/` 目录。状态字段（ClusterQuery / groups / availableVersions / loading / processing / TotalCount）MUST 集中在 `Clusters.razor` 的 `@code`，子组件 MUST NOT 各自复制 filter 字段。ClusterToolbar MUST 通过 `[Parameter] ClusterQuery Filters` 共享引用并 invoke `[EventCallback] OnFilterChanged`；ClusterTable MUST 暴露 `public Task ReloadAsync()` 转发 `MudTable.ReloadServerData()`，父页面经此触发 reload。状态 Chip 与操作按钮组 MUST 内联于 `ClusterTable` 的 RowTemplate，MUST NOT 抽出更细的独立组件。

#### Scenario: 工具栏 filter 写回父页面

- **WHEN** 用户在 `ClusterToolbar` 内输入搜索关键字
- **THEN** `ClusterToolbar` 直接写回 `Filters.Name` 字段（引用共享），并 invoke `OnFilterChanged`；父页面 handler MUST 仅触发 `ClusterTable.ReloadAsync()`，MUST NOT 自己同步 filter 字段

#### Scenario: 表格组件封装 MudTable 引用

- **WHEN** `Clusters.razor` 调用 `tableComponent.ReloadAsync()`
- **THEN** `ClusterTable` 内部 MUST 转发到 `MudTable.ReloadServerData()`；`MudTable` 实例引用 MUST NOT 暴露到 `Clusters.razor`

#### Scenario: 不再下沉更细组件

- **WHEN** 实现者审查 `ClusterTable.razor`
- **THEN** 文件内 MUST 同时包含 HeaderContent、RowTemplate、状态 Chip 渲染、操作按钮组、`<MudTablePager />` 等；MUST NOT 进一步拆出 `ClusterStatusChip.razor` 或 `ClusterRowActions.razor` 等更细组件

### Requirement: 版本下拉候选由后端 distinct 提供

`Clusters.razor` 的版本下拉候选 MUST 由 `ClusterService.GetAvailableVersionsAsync()` 提供，该 Service 方法 MUST 转发 `ClusterRepository.GetDistinctVersionsAsync()`（`db.Clusters.Select(c => c.Version).Distinct()`），MUST NOT 从任何全量 ViewModel 列表内存派生。下拉的固定项目（"全部"、"_未知_" sentinel）MUST 在组件 markup 端拼接，MUST NOT 让 Service 返回 sentinel 字符串。该 ping MUST 不分页，候选集应是全部非空 distinct 版本。

#### Scenario: 版本下拉数据源

- **WHEN** `Clusters.razor` 初始化
- **THEN** `ClusterService.GetAvailableVersionsAsync()` 返回非空 distinct 版本字符串列表，组件 markup 在列表前后拼接 "__ALL__"（全部）与 "__NULL__"（未知）两个 sentinel 项

### Requirement: 删除前端全量过滤逻辑

下沉完成后，`Clusters.razor` MUST 删除 `filteredClusters` getter 与基于 `clusters` 字段派生的 `availableVersions` 属性。`clusters : List<ClusterViewModel>` 全量字段 MUST 删除。前端 MUST NOT 保留任何对集群全量列表的引用；列表数据 MUST 仅由 `ClusterTable` 内的 `MudTable ServerData` 持有。

#### Scenario: 无前端全量字段

- **WHEN** 检查 `Clusters.razor` 源码
- **THEN** 不应存在 `private List<ClusterViewModel> clusters = []` 字段、不应存在 `filteredClusters` 属性、不应存在 `availableVersions` 派生属性；只应存在 `query : ClusterQuery` 与 `TotalCount : int` 等本地状态字段

### Requirement: 其它对话框组件不变

`AddClusterDialog.razor` / `EditClusterDialog.razor` / `CreateGroupDialog.razor` / `ManageGroupsDialog.razor` MUST 保持完全不变。新增集群 / 编辑集群 / 创建分组 / 分组管理对话框成功关闭后，`Clusters.razor` 触发 `ClusterTable.ReloadAsync()` 重拉当前页即可，MUST NOT 重新全量 ping。

#### Scenario: 添加集群成功后仅重拉表格

- **WHEN** 用户在 `AddClusterDialog` 成功添加集群并关闭对话框
- **THEN** 父页面 MUST 调用 `tableComponent.ReloadAsync()`，MUST NOT 调用任何全量 `GetClustersAsync`
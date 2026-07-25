## 1. 后端 DTO 与枚举

- [x] 1.1 新增 `MultiClusterMgmtSys/ViewModels/ClusterQuery.cs`：POCO 查询参数类，字段含 `Name?`、`GroupId?`、`Status?`、`Version?`、`DateStart?`、`DateEnd?`、`Page=1`、`PageSize=20`、`SortBy=ClusterSortField.CreatedAt`、`SortDescending=true`
- [x] 1.2 新增 `ClusterSortField` 枚举（值 `Name/Status/Version/NodeCount/CreatedAt`）——放在 `MultiClusterMgmtSys/Constants/` 下与 `ClusterStatus`/`ConnectionType` 共处
- [x] 1.3 新增泛型 `MultiClusterMgmtSys/ViewModels/PagedResult.cs`：`Items : List<T>` + `Total : int` 双字段，无参/全参构造

## 2. 仓储层分页查询

- [x] 2.1 在 `Daos/ClusterRepository.cs` 新增 `GetPagedAsync(ClusterQuery q) → Task<(List<ClusterInfo> Items, int Total)>`：`IQueryable<ClusterInfo>` 上装配 `Where`（GroupId / Name 模糊 / Status / Version 含 `__ALL__`/`__NULL__` sentinel 处理 / DateStart / DateEnd），`Include(Group)`，`AsNoTracking`
- [x] 2.2 在 `GetPagedAsync` 内用 switch 把 `q.SortBy` 枚举翻译到强类型 `OrderBy`/`ThenByDescending(Id)` 兜底稳定排序；`SortDescending=true` 走 `OrderByDescending`，`false` 走 `OrderBy`
- [x] 2.3 在 `GetPagedAsync` 内先 `CountAsync` 取 `Total`，再 `Skip((Page-1)*PageSize).Take(PageSize).ToListAsync()` 取当前页
- [x] 2.4 新增 `GetDistinctVersionsAsync() → Task<List<string>>`：`db.Clusters.Select(c => c.Version).Where(v => v != null).Distinct().OrderBy(v => v).ToListAsync()`，仅返回非空 distinct 字符串
- [x] 2.5 保留 `GetAllAsync` 完全不动，确认 `ClusterDetail.razor` 下拉等依赖者仍能编译

## 3. 服务层投影与排序映射

- [x] 3.1 在 `Services/ClusterService.cs` 新增 `GetPagedAsync(ClusterQuery q) → Task<PagedResult<ClusterViewModel>>`：调用 `repo.GetPagedAsync`，把 `(Items, Total)` 投影为 `PagedResult<ClusterViewModel>`（用 `ToViewModel()` 扩展）
- [x] 3.2 新增 `Service` 重载 `GetPagedAsync(TableState state, ClusterQuery baseQuery)`：把 `state.SortLabel`（字符串）映射到 `ClusterQuery.SortBy` 枚举（switch `Name/Status/Version/NodeCount/CreatedAt`，default → `CreatedAt`），`state.SortDirection` 映射到 `SortDescending`，`state.Page` + 1 / `state.PageSize` 同步到 query 后调单一参数重载。MudTable SortLabel 字符串与 SortBy 表达式的对应需用日志/断言确认
- [x] 3.3 新增 `GetAvailableVersionsAsync() → Task<List<string>>`，直接转发 `repo.GetDistinctVersionsAsync`
- [x] 3.4 保留 `GetClustersAsync` 完全不动

## 4. 工具栏组件

- [x] 4.1 新建 `Components/Pages/Clusters/ClusterToolbar.razor`：`[Parameter] ClusterQuery Filters`、`[Parameter] IReadOnlyList<ClusterGroupViewModel> Groups`、`[Parameter] IReadOnlyList<string> AvailableVersions`、`[EventCallback] OnFilterChanged`
- [x] 4.2 内部 6 个筛选 markup（Name 文本 / GroupId Select / Status Select / Version Select 带 `__ALL__`/`__NULL__` sentinel + AvailableVersions / DateStart / DateEnd），全部 `@bind-Value="Filters.Xxx"` 直接写回引用字段；DateEnd 改后须 invoke `OnFilterChanged`
- [x] 4.3 每个筛选字段 `ValueChanged` 后 invoke `OnFilterChanged`，让父页面仅触发 reload
- [x] 4.4 "重置"按钮清空 `Filters` 各筛选字段（保留 `SortBy`/`SortDescending`/`Page`/`PageSize` 默认），invoke `OnFilterChanged`

## 5. 表格组件

- [x] 5.1 新建 `Components/Pages/Clusters/ClusterTable.razor`：`[Parameter] ClusterQuery Filters`、`[Parameter] Func<TableState, Task<TableData<ClusterViewModel>>> OnLoadPaged`、`[EventCallback] Func<int,Task> OnRefresh/OnEdit/OnDelete/OnNavigate`、`[Parameter] bool Processing`
- [x] 5.2 内部 `<MudTable T="ClusterViewModel" ServerData="@LoadInner" RowsPerPage="20">`，`<PagerContent><MudTablePager /></PagerContent>`
- [x] 5.3 `LoadInner(TableState s)` 转发到 `OnLoadPaged(s)`——`OnLoadPaged` 由父页面实现并回传 `TableData<ClusterViewModel>`（`Items` 当前页 + `TotalItems` 用于 Pager）
- [x] 5.4 `HeaderContent` 含 5 个 `MudTableSortLabel`（Name/Status/Version/NodeCount/CreatedAt）+ 3 个固定列（分组/API Server/操作）；`RowTemplate` 内联状态 Chip、操作按钮组（Refresh/Edit/Delete + AuthorizeView Roles="Admin"）
- [x] 5.5 暴露 `public Task ReloadAsync() => table.ReloadServerData()`，`@ref MudTable<ClusterViewModel> table` 不外露

## 6. 页面壳重写

- [x] 6.1 重写 `Components/Pages/Clusters/Clusters.razor`：`@page "/clusters"`、`@inject ClusterService/GroupService/DialogService/Snackbar/IdentityRedirectManager`、`@attribute [Authorize]`
- [x] 6.2 `@code` 状态字段精简为：`groups : List<ClusterGroupViewModel>`、`availableVersions : List<string>`、`query : ClusterQuery = new()`、`TotalCount : int`、`loading/processing : bool`、`tableComponent : ClusterTable? @ref`
- [x] 6.3 `OnInitializedAsync` 先 `GroupService.GetGroupsAsync()` 与 `ClusterService.GetAvailableVersionsAsync()` 并行 ping 作工具栏下拉数据源；MudTable ServerData 首拉由组件生命周期自动触发
- [x] 6.4 `LoadPaged(TableState s)`：调 `ClusterService.GetPagedAsync(s, query)` 返回 `PagedResult`，`TotalCount` 同步 `result.Total`，返回 `new TableData<ClusterViewModel> { Items = result.Items, TotalItems = result.Total }`
- [x] 6.5 `RefreshFromFilter()` handler 仅调 `tableComponent.ReloadAsync()`，不做 filter 字段同步（已由工具栏写回引用）
- [x] 6.6 把 4 个对话框方法 `OpenAddClusterDialog/OpenCreateGroupDialog/OpenEditClusterDialog/OpenManageGroupsDialog` 迁过来；对话框成功关闭后调 `tableComponent.ReloadAsync()` 替代旧 `LoadAsync` 全量重拉；`OpenManageGroupsDialog` 成功后额外 ping `groups` 与检查 `filterGroupId` 是否仍存在
- [x] 6.7 `DeleteCluster` / `RefreshCluster` / `NavigateToDetail` 迁过来，成功后调 `tableComponent.ReloadAsync()`；"共 N 个集群" 改用 `TotalCount`
- [x] 6.8 删除 `clusters : List<ClusterViewModel>` 全量字段、`filteredClusters` getter、`availableVersions` 派生属性、`LoadAsync` 中 `GetClustersAsync` 调用
- [x] 6.9 顶部按钮区（分组管理/新建分组/添加集群）保留，`AuthorizeView Roles="Admin"` 嵌套不变

## 7. 验证

- [x] 7.1 运行 `dotnet build MultiClusterMgmtSys.slnx`，修复编译错误
- [ ] 7.2 `dotnet run --project MultiClusterMgmtSys/MultiClusterMgmtSys.csproj`，登录后访问 `/clusters` 验证首屏拉取、翻页、6 个筛选、5 列排序、"重置"按钮、4 个对话框正常
- [ ] 7.3 验证 MudTable SortLabel 字符串与 Service switch 对应正确（改动某一列排序后看 SQL/实际顺序）
- [ ] 7.4 验证"共 N 个集群"文案随翻页/筛选保持正确总数
- [ ] 7.5 删除 DB 文件 `MultiClusterMgmtSys/MultiClusterMgmtSys.db` 重建后用 `admin/Changeme_123` 登录复测
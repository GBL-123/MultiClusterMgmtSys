## Context

集群列表 `Components/Pages/Clusters/Clusters.razor` 单文件 412 行：markup 三段（计数+按钮 / 6 项筛选工具栏 / 四态表格）+ `@code` 190 行含 9 个状态字段、`filteredClusters` getter（每次渲染跑全量 LINQ）、`availableVersions` 派生属性、4 个 `OpenXxxDialog` 三段式、刷新/删除/导航回调、静态 `GetStatusColor`。

数据流当前全前端：

```
ClusterRepository.GetAllAsync()  →  List<ClusterInfo>  (无 Where/OrderBy/Skip/Take, tracking)
   ↓
ClusterService.GetClustersAsync()  →  List<ClusterViewModel>  (Select 投影)
   ↓
Clusters.razor.clusters  →  filteredClusters getter (内存 LINQ Where * 6)
   ↓
MudTable Items=filteredClusters  (无 Pager, 全量渲染)
```

后端 `ClusterRepository` 仅 `GetAll/GetById/Add/Update/Delete`，无查询DSL。全项目（Accounts/ConfigMaps/Nodes 列表页）皆同构，均无分页先例。`openspec/specs/` 当前为空。

约束：
- Blazor Server 单渲染模式（`AddInteractiveServerComponents`），`@code` 字段在用户电路存活期间常驻，天然是状态容器。
- MudBlazor 9.x 的 `MudTable` 已支持 `ServerData` 回调与 `TableData<T>` 返回类型。
- EF Core + SQLite，`EnsureCreated` 建表无迁移。`ClusterInfo` 实体不变。
- 中文用户文案、英文代码标识符。

## Goals / Non-Goals

**Goals:**
- 把集群列表的筛选、排序、分页全部下推到 EF / SQL（`AsNoTracking`，单次查询出当前页 + 一次 `Count`），支撑 1000 量级不卡。
- `Clusters.razor` 拆分为三个文件：页面壳 / 工具栏 / 表格，单文件行数显著下降，wire 清晰可读。
- 引入 `ClusterQuery` / `PagedResult<T>` 两个泛型/POCO 类型承载查询参数与结果，本 change 内自洽。
- 保持用户可见行为基本一致（同样的 6 项筛选 + 排序 + 操作按钮），新增真正的翻页 Pager。

**Non-Goals:**
- 不为 Accounts / ConfigMaps / Nodes 同步下沉——本 change 内立 DTO，但等被复用一次后再决定是否抽共享命名空间或基类。
- 不引入 `ClusterListState` 这类页面状态聚合 class——Blazor Server `@code` 字段已是合适容器。
- 不抽比 `ClusterTable` 更细的组件（状态 Chip / 操作按钮组保持内联于 RowTemplate）。
- 不改 `AddClusterDialog` / `EditClusterDialog` / `CreateGroupDialog` / `ManageGroupsDialog` 任何一个字。
- 不改 `GetAllAsync` / `GetClustersAsync`（详情页下拉与现有依赖者仍用）。
- 不动数据库 schema、不引入新 NuGet 包。
- 不实现 `NodeCount` 这类派生字段的排序（`NodeCount` 是 K8s 探活后的缓存值，可下推 SQL，但"按节点数排序"非本 change 目标——见 Open Questions）。

## Decisions

### 决策 1：MudTable 走 `ServerData` 模式，非 `Items` 模式

**选择**：`<MudTable T="ClusterViewModel" ServerData="LoadPagedData" RowsPerPage="20">`，配 `<PagerContent><MudTablePager /></PagerContent>`。

**理由**：
- `Items` 模式下 MudTable 是纯前端切片；无法与"Server-side 真分页"共存（MudBlazor 无"Items 但 TotalCount 外部给"的折中态）。
- `ServerData` 回调签名 `Task<TableData<T>>(TableState state)`，`state.Page / PageSize / SortLabel / SortDirection` 由 MudTable 内部持有并随翻页/点列头触发；父页面不再保存 Page/PageRow/Sort 字段，状态字段从 9 个降到 6 个（只剩 filter 字段）。
- `TableData<T>` 同时携带 `Items` 与 `TotalItems`，正好喂给 `MudTablePager` 显示共 N 条与页码。

**备选**：保留 `Items` 模式 + 自 Pager + 自存 Page 字段。否决——状态字段更多、排序下推 SQL 时仍需把 `state.Sort` 传后端，重复造 MudTable 已有的轮子。

### 决策 2：filter 改变后由父页面调 `tableRef.ReloadServerData()`

`MudTable ServerData` 只在内部触发（翻页/排序）时回调，不感知外部 filter 变化。因此：

```
ClusterToolbar.razor
   └ [Parameter] ClusterQuery Filters  (字段级双向 @bind)
   └ [EventCallback] OnFilterChanged
        ▼
Clusters.razor (页面)
   ├ 1. 工具栏内部 @bind 已同步 filter 字段到父页 ClusterQuery
   │    (因为 ClusterQuery [Parameter] 是引用, 字段写回父即可)
   └ 2. OnFilterChanged → tableRef.ReloadServerData()  (回第 1 页 + 触发 ServerData)
        ▼
ClusterTable.razor
   └ tableRef 在此处声明 (@ref), ReloadServerData 父页面通过转发调用
```

`tableRef` 放 `ClusterTable` 内部 `@ref`，父页面通过 `ClusterTable` 的 `[Parameter]` 关联，由 `ClusterTable` 暴露 `public Task ReloadAsync()` 转发 `MudTable.ReloadServerData()`。这样 `Clusters.razor` 不直接持有 MudTable 引用，封装在 `ClusterTable`。

**备选**：`tableRef` 放父页面，由父直接调 MudTable API。否决——破坏 `ClusterTable` 封装，让父页面认识 MudTable 内部细节。

### 决策 3：排序字符串→表达式映射放在 Service，不放在 Repo

`MudTableSortLabel SortBy="x => x.Name"` 在 MudTable 内部把 SortKey 转成 SortLabel 字符串 "Name"。Service 把字符串映射到 IQueryable 的 OrderBy：

```csharp
// ClusterService.GetPagedAsync 内部
query = state.SortLabel switch {
    "Name"      => state.SortDirection == SortDirection.Ascending ? query.OrderBy(c => c.Name) : query.OrderByDescending(c => c.Name),
    "Status"    => state.SortDirection == SortDirection.Ascending ? query.OrderBy(c => c.Status) : query.OrderByDescending(c => c.Status),
    "Version"   => state.SortDirection == SortDirection.Ascending ? query.OrderBy(c => c.Version) : query.OrderByDescending(c => c.Version),
    "NodeCount" => state.SortDirection == SortDirection.Ascending ? query.OrderBy(c => c.NodeCount) : query.OrderByDescending(c => c.NodeCount),
    "CreatedAt" => state.SortDirection == SortDirection.Ascending ? query.OrderBy(c => c.CreatedAt) : query.OrderByDescending(c => c.CreatedAt),
    _           => query.OrderByDescending(c => c.CreatedAt)  // 默认倒序最新
};
```

**理由**：
- Repo 不应认识 ViewModel 字段名（"Name" 是 ClusterViewModel.Name，但 `OrderBy(x => x.Name)` 在 `IQueryable<ClusterInfo>` 上）；Service 是协议翻译点。
- 若放 Repo，Repo 需要接受字符串并 switch，违反"Repo 只认实体"的边界。
- Service 拿到 `TableState` 后，但要传给 Repo 的是已展开的 `ClusterQuery`（含 `SortBy` 枚举或字符串字段名 + 方向）——见决策 4。

### 决策 4：`ClusterQuery` 携带排序信息，排序在 Service 映射、但 Repo 仍接受 query-derived 已展开参数

为了让 Repo 保持纯 EF 装配器（不要 switch），Service 把 `TableState.SortLabel/SortDirection` 翻译成 `ClusterQuery.SortBy`（枚举 `ClusterSortField`，值 `Name/Status/Version/NodeCount/CreatedAt`）+ `SortDescending: bool`，再连同其它 filter 字段一并交给 Repo。Repo 的 `GetPagedAsync` 用 `ClusterQuery.SortBy` 枚举再次 switch 到 `OrderBy` —— 这次 switch 在 Repo 是实体字段，纯实体感知。

```
ClusterQuery (POCO)
{
    string? Name;
    int? GroupId;
    ClusterStatus? Status;
    string? Version;     // "__NULL__" 表"版本为空", "__ALL__" 或 null 表不过滤, 其它字符串精确匹配
    DateTime? DateStart;
    DateTime? DateEnd;
    int Page;             // 1-based
    int PageSize;         // 默认 20
    ClusterSortField SortBy;  // 枚举
    bool SortDescending;
}

PagedResult<T>
{
    List<T> Items;
    int Total;
}
```

`ClusterSortField` 枚举放在 `ViewModels/` 或 `Constants/` 下（视实现方便，本 change 不强行规定，倾向 `Constants/`，与既有 `ClusterStatus`/`ConnectionType` 共处）。

**为何要双重 switch**：Service 翻译字符串→枚举（"ViewModel 世界" 的 SortLabel 字符串 → 实体感知的枚举），Repo 翻译枚举→`OrderBy` lambda（实体字段感知）。两边的 switch 都是受限集合且小（5 个排序列），重复成本低；好处是 Repo 仍是"不认识 ViewModel 字符串"的纯实体仓储。

### 决策 5：版本下拉候选 distinct 仍由后端单独 ping

`availableVersions` 派生当前从 `clusters` 全量 LINQ 算，下沉后 `clusters` 不再是全量列表。版本下拉候选改由后端单独 ping：

- `ClusterRepository.GetDistinctVersionsAsync() → List<string?>`：`db.Clusters.Select(c => c.Version).Distinct().ToListAsync()`，排除 null（或包含 null——见 Open Questions），轻量 SQL，单列 distinct。
- `ClusterService.GetAvailableVersionsAsync()` 转发，或在 `LoadAsync` 时与首次分页查询并行 ping。
- 这个 ping 不分页——下拉候选就应是全集合的可能值，数量级是几十。

`OnInitializedAsync` 顺序：先 `GetGroupsAsync`（已有）+ `GetAvailableVersionsAsync`（新）作工具栏数据源，再触发 MudTable ServerData（自动随组件初始化触发首次加载）。

### 决策 6：组件接口与状态所有权

```
┌────────────────────────────────────────────────────────────────┐
│ Clusters.razor  (页面)                                          │
│ @page "/clusters"                                              │
│ @inject ClusterService / GroupService / DialogService / ...    │
│                                                                │
│ state(在 @code):                                               │
│   groups : List<ClusterGroupViewModel>                         │
│   availableVersions : List<string?>                           │
│   query  : ClusterQuery   ←  唯一 filter 真相源                │
│   loading / processing                                         │
│                                                                │
│ <ClusterToolbar Filters="@query"                              │
│     Groups="@groups"                                          │
│     AvailableVersions="@availableVersions"                    │
│     OnFilterChanged="RefreshFromFilter" />                    │
│                                                                │
│ <ClusterTable Filters="@query"                                │
│     OnLoadPaged="LoadPagedData"     ← ServerData 回调实现     │
│     OnRefresh="RefreshCluster"                                │
│     OnEdit="OpenEditClusterDialog"                            │
│     OnDelete="DeleteCluster"                                  │
│     OnNavigate="NavigateToDetail"                             │
│     Processing="@processing"                                   │
│     @ref="tableComponent" />                                  │
│                                                                │
│ RefreshFromFilter() → tableComponent.ReloadAsync()            │
│ LoadPagedData(TableState s) → ClusterService.GetPagedAsync()  │
│         + 把 state.SortLabel/Dir 翻译到 query.Sort*           │
└────────────────────────────────────────────────────────────────┘

┌────────────────────────────┐   ┌─────────────────────────────────┐
│ ClusterToolbar.razor       │   │ ClusterTable.razor             │
│ [Parameter] ClusterQuery F │   │ [Parameter] ClusterQuery F     │
│ [Parameter] groups         │   │ [Parameter] OnLoadPaged (Func) │
│ [Parameter] availableV     │   │ [Parameter] OnRefresh/Edit/    │
│ [EventCallback] OnFilter   │   │   Delete/Navigate              │
│                            │   │ [Parameter] processing         │
│ 内部 @bind-Value 直接     │   │                                 │
│ 写回 F.Name/GroupId/...   │   │ @ref MudTable<ClusterViewModel>│
│ (引用共享, 父即看到)       │   │ ServerData="@LoadInner"        │
│ 改后 invoke OnFilter       │   │ LoadInner(s) → OnLoadPaged(s)  │
│                            │   │ public ReloadAsync() =>        │
│ SE-100 重置按钮清空所有    │   │   table.ReloadServerData()      │
│ filter 字段, 再 invoke     │   └─────────────────────────────────┘
│ OnFilter                   │
└────────────────────────────┘
```

`ClusterToolbar` 的 `Filters` 是 reference-shared `[Parameter]`，工具栏内部 `@bind-Value` 直接改其字段（ClusterQuery 是 class，引用传递）。这违反 Blazor 官方"不要直接改 [Parameter]"的指引，但在"工具栏 ↔ 页面"这种紧耦合场景是常见实操。备选是把每个字段都加 `EventCallback`——6 个回调会让 markup 冗长、且同步体验差。**取舍**：接受 reference-shared 写回，但每次写完都 invoke `OnFilterChanged`，父页面在 handler 里只做"触发 ReloadServerData"，不再做字段同步——职责清晰。

### 决策 7：删除 `filteredClusters` getter 与 `availableVersions` 派生，`ResetFilters` 改为重置 query 字段 + 触发 Reload

下沉后：
- `filteredClusters` getter 删——列表数据完全由 `MudTable ServerData` 持有（其实在 `ClusterTable` 内部 `MudTable`）。
- `availableVersions` 派生 → 改由后端 ping 出，存在 `@code` 字段。
- `ResetFilters` → 重置 `query` 各字段（Name/GroupId/Status/Version/DateStart/DateEnd）+ `tableComponent.ReloadAsync()`。
- "共 N 个集群" 计数语义变化：原为 `filteredClusters.Count`，下沉后由 `PagedResult.Total` 提供——`ClusterTable` 把 `Total` 暴露给父页面或 `OnLoadPaged` 回调里把 Total 写回 `@code.TotalCount` 字段。

## Risks / Trade-offs

- **[Risk] MudTable `ServerData` filter 改变不自动 reload** → 由 `ClusterTable.ReloadAsync()` 在 `OnFilterChanged` 显式触发；并在 `Reset` 按钮与任意 filter 字段 `ValueChanged` 都走同一路径，不能漏。
- **[Risk] 排序 `MudTableSortLabel SortBy="x => x.Name"` 的 SortLabel 实际字符串是不可见的 MudTable 内部约定** → 实现时需通过日志/断言确认每个 SortLabel 字符串；若 MudBlazor 升级，约定可能变；Service 的 switch `default` 分支兜底为 `OrderByDescending(CreatedAt)` 避免崩。
- **[Risk] `NodeCount` 排序虽可下推但语义模糊**：它是探活缓存值，离线集群为 0，按节点数排序会把所有离线集群排到一起——可下推但 UX 可能误导。**约定本 change 启用但不在 Service 兜底拼接，让 MudTable 排序直接展示该行为**；若后续判定误导，再禁用该 SortLabel。
- **[Risk] reference-shared `[Parameter]` 改字段** → 仅在"`ClusterToolbar`/`Clusters` 是同生命周期亲子且 ClusterQuery class 由父持有"成立时安全；任何把 `ClusterToolbar` 复用到其它页面的需求须新建该页自己的 ClusterQuery 实例，不要共享。
- **[Risk] `GetPagedAsync` 的 `Order by` 必须有稳定排序键，否则分页不稳定** → `default` 分支 `OrderByDescending(CreatedAt)` 兜底；若 SortBy 字段为 `Version` 且多行版本相同、`CreatedAt` 也可能相同，理论上仍可能不稳——加二级 `ThenByDescending(Id)` 兜底。
- **[Trade-off] 双重 switch（Service 字符串→枚举 / Repo 枚举→lambda）** → 5 字段排序时重复成本低，但加新可排序字段需改两处。
- **[Trade-off] 版本下拉仍要单独 ping distinct 版本** → 与下沉逻辑割裂，但 SQL distinct 单列几十行极轻，可接受；不在分页查询里聚合是因为聚合会受当前 filter 影响，下拉应是全集候选。

## Migration Plan

无外部数据迁移；纯代码重构。落地顺序（详见 tasks.md）：

1. 后端基线：先加 `ClusterQuery`/`PagedResult<T>`/`ClusterSortField` 类型 → Repo `GetPagedAsync` + `GetDistinctVersionsAsync` → Service `GetPagedAsync` + `GetAvailableVersionsAsync`。
2. 前端：新建 `ClusterToolbar.razor` 与 `ClusterTable.razor` → 改 `Clusters.razor` 切到 `ServerData` 模式。
3. 删除 `filteredClusters` getter、`availableVersions` 派生、4 个 `OpenXxxDialog` 三段式中重复的 `LoadAsync` 逻辑改为只在"对话成功后 Reload 表格"而非"全量重新 ping"。

**回滚策略**：git 单 commit 切换即可；无 schema、无配置、无外部 API 变更。

**Build 验证**：`dotnet build MultiClusterMgmtSys.slnx` 必通过；无 lint/test 框架配置。

## Open Questions

1. **版本下拉候选是否包含 `null`（"未知"分支）？** 现有代码用 `"__NULL__"` sentinel 区分"显示未知"分支。下沉建议：`GetDistinctVersionsAsync` 只返非空 string，下拉固定项"未知"保留 sentinel `__ALL__/__NULL__` 维持现有交互——本 change 保留 sentinel 机制，不改 UX。
2. **默认每页 20 行是否合适？** 选 20 是 MudBlazor 常见默认；若用户期望更密可改 50。本 change 定 20，不阻塞。
3. **是否在 `Clusters.razor` 顶部显示 `TotalCount`？** 现版本有 "共 N 个集群" 文案，下沉后 `Clusters.razor` 持 `TotalCount` 字段，由 `OnLoadPaged` 回调同步 `query.Total` 更新；文案保留。
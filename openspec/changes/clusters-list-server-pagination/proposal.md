## Why

集群列表页 `Clusters.razor` 当前 412 行单一文件，把页面壳、工具栏、表格、所有事件处理与状态字段全压在一处，难以阅读与演进。更关键的是筛选/排序当前为前端内存 LINQ（`filteredClusters` getter 每次渲染重算全量），后端 `ClusterRepository.GetAllAsync()` 与 `ClusterService.GetClustersAsync()` 一次性拉全部集群到内存——集群量级预期达 1000，这套假分页会随数据增长卡顿，并使"分页查询"成为名实不符的承诺。当前为下推后端真分页、并把长页拆分成可维护组件的合适时机。

## What Changes

- **后端查询下沉**：`ClusterRepository` 新增 `GetPagedAsync(ClusterQuery)`，把 GroupId / Name 模糊 / Status / Version / 创建时间区间筛选，以及 `OrderBy` + `Skip/Take` 分页全部下推到 EF / SQL，使用 `AsNoTracking`。`GetAllAsync()` 保留（详情页下拉选集群等仍依赖）。
- **服务层投影**：`ClusterService` 新增 `GetPagedAsync(ClusterQuery) → PagedResult<ClusterViewModel>`，把实体投影到 ViewModel；排序的字符串→表达式映射放在 Service（Repo 只认实体字段）。
- **新增查询/分页 DTO**：`ViewModels/ClusterQuery.cs`（POCO，承载筛选+分页+排序参数）、`ViewModels/PagedResult.cs`（泛型 `Items + Total` 双字段容箱）。本 change 内立但不立"全站规范先例"，等 Accounts/ConfigMaps/Nodes 列表下沉时由复用验证再决定是否抽共享命名空间与基类。
- **Blazor 列表页拆分为三组件**：`Clusters.razor`（页面壳，`@page "/clusters"`、注入、filter state、`ServerData` 回调与对话/操作分发），`ClusterToolbar.razor`（6 个筛选字段 + 重置，`[Parameter]` + `OnFilterChanged`），`ClusterTable.razor`（`MudTable` ServerData 模式 + `<PagerContent>`、HeaderContent、RowTemplate，含状态 Chip 与操作按钮，不进一步抽细）。
- **数据模式切换**：`MudTable` 由 `Items=plainList` 切到 `ServerData` 异步回调函数，`RowsPerPage` 默认 20。翻页/排序状态住 MudTable 内部，仅 filter 状态住父页面 `@code`。filter 改变后由父页面调 `tableRef.ReloadServerData()` 触发重新拉取并回到第一页。
- **`filteredClusters` getter 与 `availableVersions` 派生属性删除**：筛选全在 SQL；版本下拉列表改由后端在分页查询的同包内或独立 ping 返回的 distinct 列表提供（仅下拉候选，非分页结果）。

## Capabilities

### New Capabilities

- `clusters-list`: 集群列表页的服务端分页查询能力与 Blazor 组件拆分形态。覆盖 Repo/Service 的分页过滤排序下推与 `ClusterQuery`/`PagedResult<T>` DTO 引入，列表页拆为 `Clusters` / `ClusterToolbar` / `ClusterTable` 三组件并以 MudTable `ServerData` 模式串联数据流。

### Modified Capabilities

（无——本仓库 `openspec/specs/` 当前为空，这是首个 OpenSpec change。）

## Impact

- **代码**：
  - 新增 `MultiClusterMgmtSys/ViewModels/ClusterQuery.cs`、`MultiClusterMgmtSys/ViewModels/PagedResult.cs`
  - 修改 `MultiClusterMgmtSys/Daos/ClusterRepository.cs`（新增 `GetPagedAsync`，`GetAllAsync` 保留）
  - 修改 `MultiClusterMgmtSys/Services/ClusterService.cs`（新增 `GetPagedAsync`，含排序字符串→表达式映射；`GetClustersAsync` 保留）
  - 重写 `MultiClusterMgmtSys/Components/Pages/Clusters/Clusters.razor`
  - 新增 `MultiClusterMgmtSys/Components/Pages/Clusters/ClusterToolbar.razor`
  - 新增 `MultiClusterMgmtSys/Components/Pages/Clusters/ClusterTable.razor`
  - `AddClusterDialog` / `EditClusterDialog` / `CreateGroupDialog` / `ManageGroupsDialog` 完全不动（这些是对话框，与列表分页无耦合）
- **API / 协议**：无外部 API 改动；新增的为应用内 Service 方法。
- **依赖**：无需新增 NuGet 包；MudBlazor 9 的 `MudTable` `ServerData` 与 `TableData<T>` 已在现有版本支持。
- **数据库**：无 schema 改动；`EnsureCreated` 建表时机不变。
- **行为兼容性**：列表筛选/排序/分页对用户可见行为基本一致，区别是从内存过滤变成 SQL 过滤、补齐了真正的分页器（旧版本根本没有 Pager）。
- **未来影响**：`ClusterQuery`/`PagedResult<T>` 在本 change 内自洽；若被后续 Accounts/ConfigMaps/Nodes 列表下沉复用，可考虑抽共享命名空间或基类——本 change 不预做。
## 1. 数据模型与枚举

- [ ] 1.1 新建 `Models/ClusterIpCategory.cs` 枚举：`MgmtVip`（管理节点 VIP）、`BizVip`（业务节点 VIP）、`CommVip`（集群通信 VIP）、`MgmtPublicIp`（管理看板公网 IP）、`BizPublicIp`（业务配置公网 IP）
- [ ] 1.2 新建 `Models/NodeIpCategory.cs` 枚举：`K8sIp`（k8s IP）、`BizIp`（业务 IP）、`CommIp`（集群通信 IP）
- [ ] 1.3 新建 `Models/ClusterIp.cs` 实体：`Id`、`ClusterId`、`Category`（ClusterIpCategory）、`Ip`（string, Required）、`Remark`（string?）、`Cluster` 导航属性
- [ ] 1.4 新建 `Models/ClusterDomain.cs` 实体：`Id`、`ClusterId`、`Domain`（string, Required）、`Remark`（string?）、`Cluster` 导航属性
- [ ] 1.5 新建 `Models/ClusterNodeIp.cs` 实体：`Id`、`ClusterId`、`NodeName`（string, Required）、`Category`（NodeIpCategory）、`Ip`（string, Required）、`Remark`（string?）、`Cluster` 导航属性
- [ ] 1.6 在 `Models/ClusterInfo.cs` 新增 `Remark`（string?）标量字段与 `List<ClusterIp> ClusterIps`、`List<ClusterDomain> ClusterDomains` 导航属性

## 2. DbContext 配置

- [ ] 2.1 在 `Daos/AppDbContext.cs` 新增 `DbSet<ClusterIp> ClusterIps`、`DbSet<ClusterDomain> ClusterDomains` 与 `DbSet<ClusterNodeIp> ClusterNodeIps`
- [ ] 2.2 在 `OnModelCreating` 配置 `ClusterIp`：FK 到 `ClusterInfo` 级联删除、`Category` 枚举 `HasConversion<string>()`、`Ip` Required、`(ClusterId, Category, Ip)` 索引
- [ ] 2.3 在 `OnModelCreating` 配置 `ClusterDomain`：FK 到 `ClusterInfo` 级联删除、`Domain` Required、`(ClusterId, Domain)` 唯一索引
- [ ] 2.4 在 `OnModelCreating` 配置 `ClusterNodeIp`：FK 到 `ClusterInfo` 级联删除、`Category` 枚举 `HasConversion<string>()`、`NodeName`/`Ip` Required、`(ClusterId, NodeName, Category)` 唯一索引

## 3. Repository 层

- [ ] 3.1 新建 `Daos/ClusterIpRepository.cs`：`GetByClusterAsync(clusterId)` 返回 `List<ClusterIp>`、`GetDomainsByClusterAsync(clusterId)` 返回 `List<ClusterDomain>`、`GetByClusterAndNodeAsync(clusterId, nodeName)` 返回 `List<ClusterNodeIp>`、`GetAllNodeIpsByClusterAsync(clusterId)` 返回该集群全部 `ClusterNodeIp`、`ReplaceClusterIpsAsync(clusterId, List<ClusterIp>)` 整体替换集群 IP、`ReplaceClusterDomainsAsync(clusterId, List<ClusterDomain>)` 整体替换集群域名、`ReplaceNodeIpsAsync(clusterId, nodeName, List<ClusterNodeIp>)` 整体替换节点 IP
- [ ] 3.2 在 `ClusterRepository.GetByIdAsync` 中 Include `ClusterIps` 与 `ClusterDomains`（确保编辑/详情加载集合）

## 4. ViewModel 与 Mapping

- [ ] 4.1 新建 `ViewModels/ClusterIpViewModel.cs`：`Id`、`Category`（ClusterIpCategory）、`CategoryText`（只读展示用）、`Ip`、`Remark`
- [ ] 4.2 新建 `ViewModels/ClusterDomainViewModel.cs`：`Id`、`Domain`、`Remark`
- [ ] 4.3 新建 `ViewModels/ClusterNodeIpViewModel.cs`：`Id`、`NodeName`、`Category`（NodeIpCategory）、`CategoryText`、`Ip`、`Remark`
- [ ] 4.4 在 `ClusterCreateViewModel` 新增 `Remark`（string?）与 `List<ClusterIpViewModel> Ips`、`List<ClusterDomainViewModel> Domains`
- [ ] 4.5 在 `ClusterUpdateViewModel` 新增 `Remark` 与 `List<ClusterIpViewModel> Ips`、`List<ClusterDomainViewModel> Domains`
- [ ] 4.6 在 `ClusterEditViewModel` 新增 `Remark` 与 `List<ClusterIpViewModel> Ips`、`List<ClusterDomainViewModel> Domains`
- [ ] 4.7 在 `ClusterDetailViewModel` 新增 `Remark` 与 `List<ClusterIpViewModel> Ips`、`List<ClusterDomainViewModel> Domains`
- [ ] 4.8 在 `ClusterViewModel` 新增 `MgmtPublicIp`（string?，列表展示用，由 ClusterIps 中 MgmtPublicIp 第一条映射）
- [ ] 4.9 在 `ClusterNodeDetailViewModel` 新增 `List<ClusterNodeIpViewModel> ManualIps`
- [ ] 4.10 在 `ClusterNodeViewModel` 新增 `List<NodeIpCategory> IpCategories` 与 `Dictionary<NodeIpCategory, string> ManualIps`（列表合并展示用）
- [ ] 4.11 在 `Mappings/ClusterMappingExtensions.cs`：`ToViewModel` 映射 `MgmtPublicIp`；`ToDetailViewModel` 映射 `Remark`/`Ips`/`Domains`；`ToEditViewModel` 映射 `Remark`/`Ips`/`Domains`
- [ ] 4.12 新建 `ViewModels/Mappings/ClusterIpMappingExtensions.cs`：`ClusterIp` ↔ `ClusterIpViewModel`、`ClusterDomain` ↔ `ClusterDomainViewModel`、`ClusterNodeIp` ↔ `ClusterNodeIpViewModel` 双向映射（含 `CategoryText` 本地化）

## 5. Service 层

- [ ] 5.1 在 `ClusterService.AddClusterAsync` 构造 `ClusterInfo` 时透传 `Remark`，保存集群后调用 `ClusterIpRepository.ReplaceClusterIpsAsync` 持久化 `vm.Ips` 与 `ReplaceClusterDomainsAsync` 持久化 `vm.Domains`
- [ ] 5.2 在 `ClusterService.UpdateClusterAsync` 赋值 `entity.Remark`（不纳入 `configChanged`），保存后调用 `ReplaceClusterIpsAsync` 整体替换集群 IP 与 `ReplaceClusterDomainsAsync` 整体替换集群域名
- [ ] 5.3 在 `ClusterService.GetClusterDetailAsync` 确保加载 `ClusterIps` 与 `ClusterDomains` 并映射到 `ClusterDetailViewModel.Ips`/`Domains`
- [ ] 5.4 新建 `Services/ClusterNodeIpService.cs`（Scoped，注入 `ClusterIpRepository` + `ILogger`）：`GetNodeIpsAsync(clusterId, nodeName)` 返回 `List<ClusterNodeIpViewModel>`、`GetAllNodeIpsByClusterAsync(clusterId)` 返回 `List<ClusterNodeIpViewModel>`、`SaveNodeIpsAsync(clusterId, nodeName, List<ClusterNodeIpViewModel>)` 整体替换（内部 try/catch + 日志）
- [ ] 5.5 在 `Program.cs` 注册 `ClusterIpRepository`（Scoped）与 `ClusterNodeIpService`（Scoped）

## 6. 添加集群对话框

- [ ] 6.1 在 `Pages/Clusters/AddClusterDialog.razor` 连接配置区块之后新增「集群 IP（可选）」分区（`MudDivider` + `MudText Typo.h6` 标题 + 「添加 IP」按钮）
- [ ] 6.2 在「集群 IP」分区实现动态增删行：每行 `MudSelect`（绑定 ClusterIpCategory 枚举）+ `MudTextField` IP + `MudTextField` 备注 + `MudIconButton` 删除
- [ ] 6.3 在「集群 IP」分区之后新增「访问域名（可选）」分区（「添加域名」按钮 + 动态增删行：`MudTextField` 域名 + `MudTextField` 备注 + 删除按钮）
- [ ] 6.4 在「访问域名」分区之后新增「备注」（Lines=3）`MudTextField`
- [ ] 6.5 在 `@code` 块新增 `List<ClusterIpRow> ipRows`（含 Category/Ip/Remark）、`List<ClusterDomainRow> domainRows`（含 Domain/Remark）、`Remark` 私有字段
- [ ] 6.6 在 `Submit` 构造 `ClusterCreateViewModel` 时填入 `Remark`（空转 null）、`Ips`（过滤空 IP 行后映射）、`Domains`（过滤空域名行后映射）

## 7. 编辑集群对话框

- [ ] 7.1 在 `Pages/Clusters/EditClusterDialog.razor` 新增「集群 IP」分区、「访问域名」分区与「备注」字段，结构同添加对话框
- [ ] 7.2 在 `@code` 块新增 `ipRows`、`domainRows`、`Remark` 字段
- [ ] 7.3 在 `OnInitializedAsync` 从 `edit`（`ClusterEditViewModel`）回显 `Remark`、`Ips`（转为 ipRows）与 `Domains`（转为 domainRows）
- [ ] 7.4 在 `Submit` 构造 `ClusterUpdateViewModel` 时填入 `Remark`、`Ips`（过滤空 IP 行）、`Domains`（过滤空域名行）

## 8. 集群列表与详情页

- [ ] 8.1 在 `Pages/Clusters/Clusters.razor` 的 `MudTable` 表头新增「公网 IP」列（位于 API Server 列之后），`RowTemplate` 新增 `MudTd` 展示 `context.MgmtPublicIp ?? "—"`
- [ ] 8.2 在 `Pages/Clusters/ClusterDetail.razor` 基本信息卡片 `MudGrid` 新增「备注」`MudItem`（未录入显示「—」）
- [ ] 8.3 在 `ClusterDetail.razor` 基本信息卡片与连接信息卡片之间新增「集群 IP」`MudCard`：按 `ClusterIpCategory` 分组展示，每组类别名 + IP（含备注），无记录显示「暂无集群 IP」
- [ ] 8.4 在「集群 IP」卡片之后新增「访问域名」`MudCard`：列表展示全部域名（含备注），无记录显示「暂无访问域名」

## 9. 节点 IP 编辑对话框

- [ ] 9.1 新建 `Pages/Nodes/NodeIpEditDialog.razor`：接收 `ClusterId` + `NodeName` 参数，动态增删 IP 行（`MudSelect` NodeIpCategory + `MudTextField` IP + `MudTextField` 备注 + 删除按钮）
- [ ] 9.2 在 `@code` 块 `OnInitializedAsync` 调 `ClusterNodeIpService.GetNodeIpsAsync` 加载现有 IP 回显
- [ ] 9.3 在 `Submit` 调 `ClusterNodeIpService.SaveNodeIpsAsync` 整体替换，成功后 `Dialog.Close(DialogResult.Ok)`
- [ ] 9.4 在 `_Imports.razor` 确认 `@using MultiClusterMgmtSys.Components.Pages.Nodes` 已存在（NodeIpEditDialog 可被 `DialogService.ShowAsync` 解析）

## 10. 节点详情页

- [ ] 10.1 在 `Pages/Nodes/NodeDetail.razor` 「地址」卡片之后新增「手工 IP」`MudCard`：按 `NodeIpCategory` 分组展示（k8s IP/业务 IP/集群通信 IP），每组类别名 + IP + 备注，无记录显示「暂无手工 IP」
- [ ] 10.2 在「手工 IP」卡片头部 `CardHeaderActions` 添加 `AuthorizeView Roles="Admin"` 包裹的「编辑 IP」按钮，点击打开 `NodeIpEditDialog`，关闭后刷新 `LoadAsync`
- [ ] 10.3 在 `@code` 块 `LoadAsync` 中调 `ClusterNodeIpService.GetNodeIpsAsync(ClusterId, NodeName)` 填充 `node.ManualIps`（集群离线时仍加载手工 IP，不依赖 k8s 可达性）

## 11. 节点列表页

- [ ] 11.1 在 `Pages/Nodes/Nodes.razor` `MudTable` 表头将「内网 IP」改为「节点 IP」
- [ ] 11.2 在 `LoadNodesAsync` 中调 `ClusterNodeIpService.GetAllNodeIpsByClusterAsync(ClusterId)` 一次性加载该集群全部手工 IP，在内存按 `NodeName` 分组后合并到 `nodes` 列表（避免 N+1）
- [ ] 11.3 在 `RowTemplate` 「节点 IP」列展示：有手工 IP 时按类别拼接 IP 值，无手工 IP 时回退到 k8s `InternalIP`

## 12. 数据库重建与验证

- [ ] 12.1 删除 `MultiClusterMgmtSys/clusters.db`、`clusters.db-shm`、`clusters.db-wal`（若存在）
- [ ] 12.2 运行 `dotnet build MultiClusterMgmtSys/MultiClusterMgmtSys.csproj` 确认编译通过
- [ ] 12.3 运行应用，添加集群时录入多条集群 IP + 多条访问域名 + 备注，确认列表「公网 IP」列、详情页「集群 IP」卡片按类别分组展示、「访问域名」卡片列表展示正确
- [ ] 12.4 编辑集群增删改 IP 行与域名行，确认保存后详情页更新正确且不触发探测
- [ ] 12.5 进入某节点详情页，Admin 录入多类手工 IP，确认「手工 IP」卡片展示正确且 Guest 不可见编辑按钮
- [ ] 12.6 确认节点列表「节点 IP」列合并展示手工 IP 与 k8s InternalIP，手工 IP 优先
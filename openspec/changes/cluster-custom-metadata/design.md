## Context

当前 `ClusterInfo` 实体仅承载 k8s 探测自动回填字段与连接配置，无结构化 IP 容器。多集群纳管场景下，集群与节点的 IP 语义复杂：

- 集群级 IP 有 5 种固定类别：管理节点 VIP、业务节点 VIP、集群通信 VIP、管理看板公网 IP、业务配置公网 IP。
- 集群访问域名可能有 1 个或多个，无固定类别区分（自由录入）。
- 节点级 IP 有 3 种固定类别：k8s IP、业务 IP、集群通信 IP；每节点网卡数不固定。
- 节点列表/详情当前由 k8s API 实时拉取（`ClusterNodeService`），手工 IP 需持久化并与 k8s 地址合并展示。

分层架构：Razor → Service（ViewModel）→ Repository（实体）→ DbContext。`EnsureCreated()` 建库，模型变更需删 `clusters.db*` 重建。节点管理页当前为只读，本变更引入 Admin 写操作。

## Goals / Non-Goals

**Goals:**
- 以固定枚举分类的子表持久化集群级 IP（`ClusterIp`）与节点级 IP（`ClusterNodeIp`）。
- 集群级：添加/编辑集群时可动态增删多条 IP（类别 + IP + 备注），详情页按类别分组展示。
- 集群访问域名可有多条，以 `ClusterDomain` 子表持久化（无类别，自由录入），添加/编辑集群时动态增删，详情页列表展示。
- 节点级：Admin 可在节点详情页录入/编辑某节点的多类 IP，与 k8s 自动获取地址合并展示。
- 集群备注作为 `ClusterInfo` 标量字段保留（单值通用注释）。
- 节点 IP 纯持久化 CRUD，不调 k8s，不影响连通性探测。

**Non-Goals:**
- 不做 IP 格式强校验（仅长度限制），保持录入灵活。
- 不让手工 IP 参与连接探测（`BuildConfig` 仍用 `ApiServer`/`KubeConfig`/`Token`）。
- 不自动从 k8s 同步节点 IP 到手工表——k8s 地址是实时拉取的只读数据，手工表是运维补充的持久数据，两者分离。
- 不做历史数据迁移脚本（开发阶段删库重建）。
- 不做 IP 类别的用户自定义（固定枚举）。

## Decisions

### 决策 1：集群级 IP 用 `ClusterIp` 子表 + `ClusterIpCategory` 固定枚举

**选择**：新建 `ClusterIp` 实体（`Id`/`ClusterId`/`Category`/`Ip`/`Remark`），`Category` 为 `ClusterIpCategory` 枚举（MgmtVip/BizVip/CommVip/MgmtPublicIp/BizPublicIp）。`ClusterInfo` 新增 `List<ClusterIp> ClusterIps` 导航属性，FK 级联删除。

**理由**：一个集群每类 IP 可有一条或多条（如多个业务节点 VIP），子表天然支持。固定枚举保证语义一致，便于按类别筛选与分组展示。EF Core 按约定映射枚举为 string（`HasConversion<string>()` 显式声明更安全）。

**备选**：固定列挂在 `ClusterInfo`（每类一列）——被否决，同类别多条 IP 无法表达，且新增类别需改 schema。

### 决策 2：集群访问域名用 `ClusterDomain` 子表，无类别枚举

**选择**：新建 `ClusterDomain` 实体（`Id`/`ClusterId`/`Domain`（string, Required）/`Remark`（string?）），`ClusterInfo` 新增 `List<ClusterDomain> ClusterDomains` 导航属性，FK 级联删除。无类别枚举——域名自由录入，仅 Domain + 备注。

**理由**：用户明确访问域名可有 1 个或多个，但未区分域名类型（不像 IP 有管理/业务等语义分类），故无需类别枚举。子表支持多条，自由录入保持灵活。`(ClusterId, Domain)` 加唯一索引防止重复录入。

**备选**：域名也加类别枚举（如管理域名/业务域名）——被否决，用户未表达该需求，过度设计。

### 决策 3：节点级 IP 用 `ClusterNodeIp` 子表 + `NodeIpCategory` 固定枚举，按 NodeName 关联

**选择**：新建 `ClusterNodeIp` 实体（`Id`/`ClusterId`/`NodeName`/`Category`/`Ip`/`Remark`），`Category` 为 `NodeIpCategory` 枚举（K8sIp/BizIp/CommIp）。不建独立 `ClusterNode` 实体（节点生命周期由 k8s 管理），以 `(ClusterId, NodeName)` 作为业务键关联。

**理由**：节点由 k8s 动态管理（增删），建独立 `ClusterNode` 表需同步生命周期，成本高且易脏。以 `NodeName` 软关联——节点被 k8s 删除后手工 IP 记录保留（孤儿数据），下次该名称节点重新加入时自动复用，运维语义合理。`(ClusterId, NodeName, Category)` 加唯一索引防止重复录入。

**备选**：建 `ClusterNode` 实体表同步 k8s 节点——被否决，同步逻辑复杂且与 k8s 真源冲突。

### 决策 4：节点 IP 服务独立为 `ClusterNodeIpService`，不混入 `ClusterNodeService`

**选择**：新建 `ClusterNodeIpService`（Scoped，注入 `ClusterIpRepository` 或 `AppDbContext`），提供 `GetNodeIpsAsync(clusterId, nodeName)`、`SaveNodeIpsAsync(clusterId, nodeName, List<ClusterNodeIpViewModel>)` 等方法。`ClusterNodeService` 保持只读 k8s 查询职责不变。

**理由**：`ClusterNodeService` 当前无 ILogger、不做 try/catch、纯 k8s 查询；混入持久化 CRUD 会模糊职责。分离后节点 IP 服务可独立 try/catch + 日志，符合 AGENTS.md 的服务错误处理模式约定。

**备选**：扩展 `ClusterNodeService`——被否决，职责混杂。

### 决策 5：集群 IP 与集群域名在添加/编辑对话框内嵌编辑，节点 IP 用独立编辑对话框

**选择**：
- 集群 IP：在 `AddClusterDialog`/`EditClusterDialog` 内嵌「集群 IP」分区，动态增删行（`MudSelect` 类别 + `MudTextField` IP + `MudTextField` 备注 + 删除按钮），随集群保存一并提交。
- 集群域名：在同一对话框内嵌「访问域名」分区，动态增删行（`MudTextField` 域名 + `MudTextField` 备注 + 删除按钮），随集群保存一并提交。
- 节点 IP：在 `NodeDetail.razor` 新增「手工 IP」卡片，Admin 点击「编辑 IP」打开 `NodeIpEditDialog.razor`（colocate `Pages/Nodes/`），对话框内动态增删行，保存后刷新卡片。

**理由**：集群 IP 与集群强绑定，随集群表单一起保存事务一致；节点 IP 在节点详情页编辑，避免节点列表页过载，且节点 IP 与 k8s 实时数据解耦，独立对话框更清晰。

### 决策 6：节点列表 IP 列合并展示，手工 IP 优先

**选择**：`Nodes.razor` 节点列表「内网 IP」列改为「节点 IP」列，展示逻辑：若该节点有手工录入 IP，按类别拼接展示（如 `k8s:10.0.0.1 业务:10.1.0.1`）；无手工 IP 时回退到 k8s `InternalIP`。

**理由**：手工 IP 是运维补充的语义化地址，价值高于 k8s 原始 InternalIP；但避免列表列过宽，仅展示 IP 值（类别在详情页展开）。加载节点列表时批量查询集群所有节点 IP 一次性合并，避免 N+1 查询。

### 决策 7：集群列表展示代表性公网 IP

**选择**：`Clusters.razor` 表格新增「公网 IP」列，展示该集群 `ClusterIp` 中 `Category == MgmtPublicIp` 的第一条 IP；无则显示「—」。

**理由**：管理看板公网 IP 是运维定位集群最常用的入口，单列展示价值最高；其余类别 IP 在详情页查看，避免列表列爆炸。

### 决策 8：枚举以 string 存储便于可读与未来扩展

**选择**：`ClusterIpCategory`/`NodeIpCategory` 枚举在 EF Core 配置中 `HasConversion<string>()`，列类型 `TEXT`。

**理由**：string 存储的枚举值在 SQLite 中可读，便于排障；未来若需新增类别，枚举追加成员即可，旧数据不受影响（string 列不校验枚举范围）。

## Frontend UI Changes

本节按页面汇总前端界面改动，明确每个页面的布局结构、新增/修改组件与交互行为。

### 1. `Pages/Clusters/AddClusterDialog.razor`（添加集群对话框）

现有结构：连接方式 Toggle → 名称 → 分组 → API Server → KubeConfig/Token 区块。

新增分区（位于连接配置区块之后，按顺序）：

1. **「集群 IP（可选）」分区**
   - `MudDivider` + `MudText Typo.h6` 标题「集群 IP（可选）」
   - 「添加 IP」`MudButton Variant=Text` 按钮，点击向 `ipRows` 列表追加一行
   - 动态行列表：每行用 `MudGrid` 横向排列
     - `MudSelect`（绑定 `ClusterIpCategory` 枚举，Label「类别」，Dense，约 160px）
     - `MudTextField`（Label「IP 地址」，Dense，flex:1）
     - `MudTextField`（Label「备注」，Dense，约 200px）
     - `MudIconButton`（Icon `DeleteOutline`，Color Error，Small，点击移除该行）
   - 空列表时显示 `MudText` 提示「点击「添加 IP」录入集群 IP 信息」

2. **「访问域名（可选）」分区**
   - `MudDivider` + `MudText Typo.h6` 标题「访问域名（可选）」
   - 「添加域名」`MudButton Variant=Text` 按钮，点击向 `domainRows` 追加一行
   - 动态行列表：每行
     - `MudTextField`（Label「域名」，Dense，flex:1）
     - `MudTextField`（Label「备注」，Dense，约 200px）
     - `MudIconButton`（Icon `DeleteOutline`，点击移除）
   - 空列表时显示提示文本

3. **「备注」字段**
   - `MudTextField`（Label「集群备注」，Lines=3，Variant=Outlined，Dense，全宽）

交互：所有新增字段非必填，不阻断 `Submit` 现有校验逻辑。提交时过滤空 IP 行（IP 为空的行丢弃）与空域名行后映射到 ViewModel。

### 2. `Pages/Clusters/EditClusterDialog.razor`（编辑集群对话框）

结构与添加对话框一致，新增相同的三个分区。差异：
- `OnInitializedAsync` 加载 `ClusterEditViewModel` 后，将 `edit.Ips` 转为 `ipRows`、`edit.Domains` 转为 `domainRows`、`edit.Remark` 回填备注字段。
- `Submit` 构造 `ClusterUpdateViewModel` 时填入 `Remark`/`Ips`/`Domains`（过滤空行）。

### 3. `Pages/Clusters/Clusters.razor`（集群列表页）

`MudTable` 表头与行模板修改：
- 在「API Server」列之后、「创建时间」列之前新增「公网 IP」`MudTh`
- `RowTemplate` 新增对应 `MudTd`，展示 `context.MgmtPublicIp ?? "—"`，`Title` 属性悬停显示完整 IP

其余工具栏、筛选、操作列不变。

### 4. `Pages/Clusters/ClusterDetail.razor`（集群详情页）

在现有「基本信息」卡片与「连接信息」卡片之间插入两张新卡片：

1. **「集群 IP」`MudCard`**
   - `MudCardHeader`：标题「集群 IP」
   - `MudCardContent`：若 `cluster.Ips` 非空，按 `ClusterIpCategory` 分组，每组用 `MudText Typo=subtitle2` 显示类别名（如「管理节点 VIP」），下方列出该组全部 IP + 备注（`MudText` 每条一行，格式 `IP  · 备注`，无备注则仅 IP）；`cluster.Ips` 为空时显示 `MudText` 「暂无集群 IP」
   - 分组之间用 `MudDivider` 分隔

2. **「访问域名」`MudCard`**
   - `MudCardHeader`：标题「访问域名」
   - `MudCardContent`：若 `cluster.Domains` 非空，列表展示每条域名 + 备注（`MudText` 每条一行，格式 `域名  · 备注`）；为空时显示「暂无访问域名」

「基本信息」卡片 `MudGrid` 新增「备注」`MudItem`（xs=12 sm=6 md=4），展示 `cluster.Remark ?? "—"`。

### 5. `Pages/Nodes/NodeIpEditDialog.razor`（新建，节点 IP 编辑对话框）

colocate 于 `Pages/Nodes/` 子目录（遵循 AGENTS.md 目录约定）。

- `MudDialog` 外壳，`DialogContent` 内：
  - `MudText Typo=h6` 标题「编辑节点 IP：{NodeName}」
  - 「添加 IP」`MudButton`，点击向 `ipRows` 追加一行
  - 动态行列表：每行
    - `MudSelect`（绑定 `NodeIpCategory` 枚举：K8sIp/BizIp/CommIp，Label「类别」，Dense）
    - `MudTextField`（Label「IP 地址」，Dense，flex:1）
    - `MudTextField`（Label「备注」，Dense，约 200px）
    - `MudIconButton`（Icon `DeleteOutline`，点击移除）
- `DialogActions`：取消 + 保存按钮（保存时调 `ClusterNodeIpService.SaveNodeIpsAsync`，成功后 `Dialog.Close(DialogResult.Ok)`）
- `@code` 块：`[Parameter] int ClusterId`、`[Parameter] string NodeName`、`[Inject] ClusterNodeIpService`、`[Inject] ISnackbar`、`OnInitializedAsync` 加载现有 IP 回显

### 6. `Pages/Nodes/NodeDetail.razor`（节点详情页）

在现有「地址」卡片之后插入新卡片：

- **「手工 IP」`MudCard`**
  - `MudCardHeader`：标题「手工 IP」
  - `CardHeaderActions`：`AuthorizeView Roles=Admin` 包裹「编辑 IP」`MudButton Variant=Text Color=Primary Size=Small`，点击 `DialogService.ShowAsync<NodeIpEditDialog>` 传 `ClusterId`+`NodeName`，关闭后调 `LoadAsync` 刷新
  - `MudCardContent`：若 `node.ManualIps` 非空，按 `NodeIpCategory` 分组展示（k8s IP/业务 IP/集群通信 IP），每组类别名 + IP + 备注；为空显示「暂无手工 IP」
- `@code` 块 `LoadAsync` 中追加调 `ClusterNodeIpService.GetNodeIpsAsync(ClusterId, NodeName)` 填充 `node.ManualIps`（集群离线时仍加载，不依赖 k8s 可达性）

### 7. `Pages/Nodes/Nodes.razor`（节点列表页）

`MudTable` 修改：
- 表头「内网 IP」`MudTh` 改为「节点 IP」
- `RowTemplate` 对应 `MudTd`：有手工 IP 时按类别拼接 IP 值（如 `10.0.0.1 · 业务:10.1.0.1`），无手工 IP 时回退到 k8s `InternalIP`，全空显示「—」
- `LoadNodesAsync` 追加调 `ClusterNodeIpService.GetAllNodeIpsByClusterAsync(ClusterId)` 一次性加载该集群全部手工 IP，内存按 `NodeName` 分组后合并到 `nodes` 列表（避免 N+1）

### 8. `_Imports.razor`

确认 `@using MultiClusterMgmtSys.Components.Pages.Nodes` 已存在（`NodeIpEditDialog` 可被 `DialogService.ShowAsync` 解析）。若不存在则新增。

### 交互与权限约定

- 所有写操作（添加/编辑集群 IP 与域名、编辑节点 IP）仅 Admin 可见，用 `AuthorizeView Roles="Admin"` 包裹。
- Guest 用户可查看集群 IP/域名/备注与节点手工 IP（只读），但不可见编辑入口。
- 动态增删行采用前端 `List<Row>` 状态管理，提交时统一映射，不做逐行实时持久化。
- 所有新增分区字段非必填，不阻断现有集群添加/编辑流程的校验逻辑。

## Risks / Trade-offs

- **[风险] 节点删除后孤儿 IP 记录** → 以 `NodeName` 软关联，节点重新加入时自动复用；不做级联清理（运维可能希望保留历史）。可在详情页提示「该节点已不在 k8s 中」但不自动删 IP。
- **[风险] `(ClusterId, NodeName, Category)` 重复录入** → 加唯一索引约束，保存时 upsert（按业务键匹配，存在则更新，不存在则插入）。
- **[风险] 节点列表 N+1 查询** → 加载节点列表时一次性查询该集群全部 `ClusterNodeIp`，在内存按 `NodeName` 分组后合并，单次查询。
- **[权衡] 列表只展示 MgmtPublicIp 一列** → 其余 IP 需进详情页，牺牲一览性换取列表紧凑度。
- **[风险] 删库重建丢失数据** → 仓库当前有意采用的方式（AGENTS.md 已记录），开发环境可接受。
- **[风险] 节点管理页引入写操作改变只读语义** → 写操作仅限 Admin（`AuthorizeView Roles="Admin"`），Guest 保持只读，最小化对现有只读约束的影响。
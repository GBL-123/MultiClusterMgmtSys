## MODIFIED Requirements

### Requirement: 集群列表展示

系统 SHALL 在 `/clusters` 路由下以 `MudTable` 表格展示全部已纳管集群，列含名称、状态、版本、节点数、分组、API Server、公网 IP、创建时间、操作。名称列可点击进入详情页，可排序列用 `MudTableSortLabel`。「公网 IP」列展示该集群 `ClusterIp` 中 `Category == MgmtPublicIp` 的第一条 IP，无则显示「—」。

#### Scenario: 加载集群列表

- **WHEN** 用户进入 `/clusters`
- **THEN** `Clusters.razor` 调 `ClusterService.GetClustersAsync()` 加载全部集群（含 `ClusterIps` 集合），渲染 `MudTable` 表格

#### Scenario: 表头排序

- **WHEN** 用户点击可排序列的表头（名称/状态/版本/节点数/创建时间）
- **THEN** `MudTableSortLabel` 切换升降序，表格行按该列排序

#### Scenario: 名称点击进入详情

- **WHEN** 用户点击表格中某集群的名称
- **THEN** 跳转至 `/clusters/{Id}` 详情页

#### Scenario: 公网 IP 列展示

- **WHEN** 集群已录入 `Category == MgmtPublicIp` 的 `ClusterIp`
- **THEN** 表格「公网 IP」列显示该类别第一条 IP；未录入时显示「—」

### Requirement: 添加集群

系统 SHALL 允许 Admin 用户通过对话框添加集群，支持 KubeConfig 与 Token 两种连接方式，提交后立即探测连通性。对话框 SHALL 提供「集群 IP（可选）」分区，支持动态增删多条 IP（类别下拉 + IP 输入 + 备注），类别为固定枚举（管理节点 VIP/业务节点 VIP/集群通信 VIP/管理看板公网 IP/业务配置公网 IP）。对话框 SHALL 提供「访问域名（可选）」分区，支持动态增删多条访问域名（域名输入 + 备注，无类别）。对话框 SHALL 提供集群备注可选标量字段。

#### Scenario: KubeConfig 方式添加

- **WHEN** Admin 用户选择 KubeConfig 方式，填写名称、分组（可选）、粘贴 kubeconfig 文本或上传文件，提交
- **THEN** 系统建集群记录（状态 Unknown），调 `BuildConfig` + `ProbeAsync` 探测，成功则状态 Online 并回填 Version/NodeCount/ApiServer/LastCheckedAt，失败则状态 Offline（不阻断添加）

#### Scenario: Token 方式添加

- **WHEN** Admin 用户选择 Token 方式，填写名称、API Server、Bearer Token、SkipTlsVerify，提交
- **THEN** 系统建集群记录并探测，逻辑同 KubeConfig 方式

#### Scenario: 文件上传

- **WHEN** 用户选择上传文件方式提供 kubeconfig
- **THEN** `InputFile` 接受 `.yaml/.yml/.config`，限制 256KB，读取为文本填入 `KubeConfig` 字段

#### Scenario: 非 Admin 不可见添加按钮

- **WHEN** 非 Admin 用户查看集群列表
- **THEN** "添加集群"按钮不渲染（`AuthorizeView Roles="Admin"` 包裹）

#### Scenario: 录入集群 IP

- **WHEN** Admin 用户在添加集群对话框的「集群 IP」分区点击「添加 IP」，选择类别、填写 IP（与备注），可添加多条
- **THEN** 提交时这些 `ClusterIp` 记录随集群一并持久化（FK 关联）；所有 IP 条目非必填，不填不阻断提交

#### Scenario: 录入访问域名

- **WHEN** Admin 用户在添加集群对话框的「访问域名」分区点击「添加域名」，填写域名（与备注），可添加多条
- **THEN** 提交时这些 `ClusterDomain` 记录随集群一并持久化（FK 关联）；所有域名条目非必填，不填不阻断提交

#### Scenario: 录入备注

- **WHEN** Admin 用户填写集群备注字段
- **THEN** 提交时 `Remark` 随集群记录持久化；留空以 null 存储

#### Scenario: 补充信息全部留空

- **WHEN** Admin 用户未填写任何集群 IP、访问域名、备注直接提交
- **THEN** 集群正常创建，无 `ClusterIp`/`ClusterDomain` 记录，`Remark` 以 null 持久化，不报错

#### Scenario: 删除 IP 行

- **WHEN** Admin 用户在「集群 IP」分区点击某行的删除按钮
- **THEN** 该行从表单移除，提交时不持久化该条 IP

#### Scenario: 删除域名行

- **WHEN** Admin 用户在「访问域名」分区点击某行的删除按钮
- **THEN** 该行从表单移除，提交时不持久化该条域名

### Requirement: 编辑集群

系统 SHALL 允许 Admin 用户编辑集群信息（名称、分组、连接配置、集群 IP、访问域名、备注），连接配置变更时保存后重新探测。集群 IP 与访问域名编辑为整体替换（提交时按表单当前行 upsert，删除表单中已移除的行）。

#### Scenario: 编辑集群

- **WHEN** Admin 用户点击操作列"编辑"按钮，`EditClusterDialog` 打开并预填当前值（含集群 IP 行、访问域名行、备注）
- **THEN** 用户可修改名称、分组、切换连接方式、更新连接配置、增删改集群 IP 行、增删改访问域名行、修改备注，保存后若连接配置变更则重新探测

#### Scenario: 仅改名不触发探测

- **WHEN** 用户仅修改名称或分组，未改连接配置
- **THEN** 保存后不触发 `ProbeAsync`，直接更新记录

#### Scenario: 仅修改集群 IP 或域名或备注不触发探测

- **WHEN** 用户仅修改集群 IP 行、访问域名行、备注，未改连接配置
- **THEN** 保存后不触发 `ProbeAsync`，直接更新记录与 IP/域名子表

#### Scenario: 集群 IP 整体替换

- **WHEN** 用户在编辑对话框增删改集群 IP 行后保存
- **THEN** 系统以表单提交的 IP 列表为准，对现有 `ClusterIp` 记录做 upsert + 删除（表单中不存在的旧记录被删除），保证与表单一致

#### Scenario: 访问域名整体替换

- **WHEN** 用户在编辑对话框增删改访问域名行后保存
- **THEN** 系统以表单提交的域名列表为准，对现有 `ClusterDomain` 记录做 upsert + 删除（表单中不存在的旧记录被删除），保证与表单一致

### Requirement: 集群详情页

系统 SHALL 在 `/clusters/{Id:int}` 路由下提供集群详情页，展示基本信息（含备注）、集群 IP 卡片、访问域名卡片、连接信息、节点列表与操作区。

#### Scenario: 查看详情

- **WHEN** 用户从列表点击集群名称进入详情页
- **THEN** 加载 `GetClusterDetailAsync(id)`，展示基本信息卡片（名称/状态/版本/节点数/分组/API Server/备注/创建时间/最后检测时间）+ 集群 IP 卡片 + 访问域名卡片 + 连接信息卡片 + 节点列表卡片 + 操作区

#### Scenario: 集群 IP 卡片按类别分组展示

- **WHEN** 集群已录入 `ClusterIp` 记录
- **THEN** 详情页「集群 IP」卡片按 `ClusterIpCategory` 分组展示，每组显示类别名 + IP 列表（含备注）；无任何 IP 记录时卡片显示「暂无集群 IP」

#### Scenario: 访问域名卡片展示

- **WHEN** 集群已录入 `ClusterDomain` 记录
- **THEN** 详情页「访问域名」卡片以列表展示全部域名（含备注）；无任何域名记录时卡片显示「暂无访问域名」

#### Scenario: 备注未录入降级

- **WHEN** 集群未录入 `Remark`
- **THEN** 基本信息卡片备注项显示「—」

#### Scenario: 节点列表实时拉取

- **WHEN** 集群 `IsReachable == true`
- **THEN** 详情页调 `GetClusterNodesAsync` 实时拉取节点列表，表格展示名称/状态/角色/Kubelet版本/OS/内网IP

#### Scenario: 离线集群节点列表降级

- **WHEN** 集群 `IsReachable == false`
- **THEN** 节点列表区显示"集群不可达，无法获取节点列表"，不发起 k8s 请求

#### Scenario: 显示连接密文

- **WHEN** Admin 用户点击"显示密文"按钮
- **THEN** 调 `GetClusterForEditAsync(id)` 加载含密文的 `ClusterEditViewModel`，以密码态 `MudTextField` 展示，可切换明文/密文
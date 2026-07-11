## 1. ViewModel 层

- [ ] 1.1 创建 `ViewModels/NetworkPolicyListViewModel.cs`：Name, Namespace, PolicyTypes, IngressRuleCount, EgressRuleCount, CreatedAt
- [ ] 1.2 创建 `ViewModels/NetworkPolicyPortViewModel.cs`：Protocol, Port（k8s `V1NetworkPolicyPort` 映射）
- [ ] 1.3 创建 `ViewModels/NetworkPolicyPeerViewModel.cs`：NamespaceSelector, PodSelector（简化为 key-value 字典），IpBlock（CIDR + Except 列表）
- [ ] 1.4 创建 `ViewModels/NetworkPolicyRuleViewModel.cs`：Ports（`List<NetworkPolicyPortViewModel>`），Peers（`List<NetworkPolicyPeerViewModel>`）
- [ ] 1.5 创建 `ViewModels/NetworkPolicyDetailViewModel.cs`：Name, Namespace, Uid, PodSelector, PolicyTypes, IngressRules, EgressRules, Yaml, CreatedAt
- [ ] 1.6 创建 `ViewModels/NetworkPolicyCreateViewModel.cs`：ClusterId, Name, Namespace, PodSelector（`Dictionary<string,string>`）, PolicyTypes（`List<string>`）, IngressRules, EgressRules
- [ ] 1.7 创建 `ViewModels/NetworkPolicyUpdateViewModel.cs`：与 Create 相同结构，额外含 Name（只读）用于定位资源
- [ ] 1.8 创建 `ViewModels/Mappings/NetworkPolicyMappingExtensions.cs`：`V1NetworkPolicy` → ListVM / DetailVM 的扩展方法

## 2. Service 层

- [ ] 2.1 创建 `Services/NetworkPolicyService.cs`，primary constructor 注入 `ClusterRepository`，复制 `BuildConfig` 私有方法
- [ ] 2.2 实现 `ListNetworkPoliciesAsync(int clusterId, string? ns)`：调用 `client.NetworkingV1.ListNamespacedNetworkPolicyAsync` 或 `ListNetworkPolicyForAllNamespacesAsync`，映射为 `List<NetworkPolicyListViewModel>`
- [ ] 2.3 实现 `GetNetworkPolicyAsync(int clusterId, string name, string ns)`：读取单个 NP 并映射为 `NetworkPolicyDetailViewModel`，含 `KubernetesYaml.Serialize` 生成 YAML
- [ ] 2.4 实现 `CreateNetworkPolicyAsync(int clusterId, NetworkPolicyCreateViewModel vm)`：构造 `V1NetworkPolicy` 对象，调用 `CreateNamespacedNetworkPolicyAsync`
- [ ] 2.5 实现 `UpdateNetworkPolicyAsync(int clusterId, NetworkPolicyUpdateViewModel vm)`：read-then-replace 模式，`ReadNamespacedNetworkPolicyAsync` → 修改指定字段 → `ReplaceNamespacedNetworkPolicyAsync`
- [ ] 2.6 实现 `DeleteNetworkPolicyAsync(int clusterId, string name, string ns)`：调用 `DeleteNamespacedNetworkPolicyAsync`
- [ ] 2.7 实现 `UpdateNetworkPolicyFromYamlAsync(int clusterId, string name, string ns, string yaml)`：`KubernetesYaml.Deserialize<V1NetworkPolicy>` 后调用 `ReplaceNamespacedNetworkPolicyAsync`

## 3. 列表页

- [ ] 3.1 创建 `Components/Pages/NetworkPolicies/NetworkPolicies.razor`：双栏布局（`/networkpolicies` + `{ClusterId:int}`），左侧集群树 + 右侧 `MudTable` 列表
- [ ] 3.2 实现集群选择、加载、可达性检查逻辑（遵循 Nodes/ConfigMaps 模式）
- [ ] 3.3 实现搜索过滤（名称）、表格列（Name 可点击跳详情、Namespace、PolicyTypes、Ingress/Egress 规则数、CreatedAt、操作列）
- [ ] 3.4 操作列：Admin 角色显示删除按钮（`DialogService.ShowMessageBoxAsync` 确认 → service.Delete → Snackbar → 刷新）

## 4. 详情页

- [ ] 4.1 创建 `Components/Pages/NetworkPolicies/NetworkPolicyDetail.razor`（`/networkpolicies/{ClusterId:int}/{Namespace}/{Name}`）
- [ ] 4.2 展示 PodSelector（matchLabels 表格）、PolicyTypes、Ingress/Egress 规则树
- [ ] 4.3 展示 YAML 原始内容（`MudText` 或 `MudPaper` + `<pre>` 标签）
- [ ] 4.4 操作按钮：Admin 角色显示"编辑"（跳转编辑页）、"YAML 编辑"（跳转 YAML 页）；所有角色显示"返回列表"

## 5. 创建对话框

- [ ] 5.1 创建 `Components/Pages/NetworkPolicies/CreateNetworkPolicyDialog.razor`（`MudDialog`，colocated）
- [ ] 5.2 表单字段：Name、Namespace（`MudSelect` 动态加载命名空间列表）、PodSelector（`MudTable` 增删 key-value 行）、PolicyTypes（`MudSelect MultiSelection`）
- [ ] 5.3 Ingress/Egress 规则输入：可增删的规则卡片，每张卡片含端口列表 + 对等体列表
- [ ] 5.4 提交逻辑：校验必填字段 → 构建 `NetworkPolicyCreateViewModel` → 调用 service.Create → 成功 Snackbar + `Dialog.Close(Ok)` / 失败 Snackbar

## 6. YAML 编辑页

- [ ] 6.1 创建 `Components/Pages/NetworkPolicies/EditNetworkPolicyYaml.razor`（`/networkpolicies/{ClusterId:int}/{Namespace}/{Name}/yaml`）
- [ ] 6.2 `AuthorizeView Roles="Admin"` 包裹，加载当前 NP YAML 内容，`MudTextField` 多行编辑
- [ ] 6.3 保存按钮调用 `UpdateNetworkPolicyFromYamlAsync`，成功跳转详情页 / 失败 Snackbar

## 7. 导航与注册

- [ ] 7.1 `Program.cs`：注册 `builder.Services.AddScoped<NetworkPolicyService>()`
- [ ] 7.2 `Components/_Imports.razor`：添加 `@using MultiClusterMgmtSys.Components.Pages.NetworkPolicies`
- [ ] 7.3 `Components/Layout/Drawer.razor`：添加 `<MudNavLink Href="/networkpolicies" Icon="@Icons.Material.Filled.Security" Match="NavLinkMatch.Prefix">网络策略</MudNavLink>`

## 8. 验证

- [ ] 8.1 `dotnet build MultiClusterMgmtSys/MultiClusterMgmtSys.csproj` 通过
- [ ] 8.2 运行应用，验证列表页加载、集群选择、NetworkPolicy 表格渲染
- [ ] 8.3 验证详情页展示完整规则树和 YAML 内容
- [ ] 8.4 验证创建/删除流程（在有 NetworkPolicy 的集群上测试）
- [ ] 8.5 验证 Guest 角色只读、Admin 角色全功能

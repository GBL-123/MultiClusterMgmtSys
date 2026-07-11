## ADDED Requirements

### Requirement: ConfigMap 列表查看

系统 SHALL 在 `/configmaps` 与 `/configmaps/{ClusterId:int}` 路由下提供 ConfigMap 列表页面，展示所选集群中（可按命名空间过滤）的全部 ConfigMap。列表 SHALL 包含名称、命名空间、Data 键数量、键名预览、创建时间、操作列。页面 SHALL 采用双栏布局：左侧集群选择树 + 右侧内容区，与 `Nodes.razor` 一致。列表中名称列 SHALL 可点击跳转至独立详情页（`/configmaps/{ClusterId}/{Namespace}/{Name}`），操作列 SHALL 提供"编辑"（跳转结构化编辑页）、"编辑YAML"（跳转 YAML 编辑页）、"删除"（确认后删除）三个按钮，均仅 Admin 可见，不使用对话框。

#### Scenario: 通过侧边栏进入未选集群

- **WHEN** 用户从侧边栏「配置管理」进入 `/configmaps`（无 ClusterId）
- **THEN** 左侧显示集群选择树，右侧显示"请从左侧选择一个集群"空状态提示

#### Scenario: 选择集群后加载列表

- **WHEN** 用户在左侧集群选择树中点击一个 Online 集群
- **THEN** URL 跳转至 `/configmaps/{ClusterId}`，右侧先调 `ClusterService.GetClusterDetailAsync` 确认可达，再调 `ConfigMapService.ListConfigMapsAsync` 加载 ConfigMap 列表并渲染表格

#### Scenario: 按命名空间过滤

- **WHEN** 用户在命名空间下拉中切换至某个具体命名空间
- **THEN** 列表立即重新拉取该命名空间下的 ConfigMap（通过 `ValueChanged` 回调即时触发，无需手动点击刷新按钮）；选择"全部命名空间"时拉取所有命名空间的 ConfigMap

#### Scenario: 按名称搜索

- **WHEN** 用户在名称搜索框中输入关键词
- **THEN** 已加载列表在前端实时过滤，显示名称包含关键词的 ConfigMap（不发起新请求）

#### Scenario: 手动刷新

- **WHEN** 用户点击刷新按钮
- **THEN** 以当前选中的集群和命名空间重新拉取列表

#### Scenario: 点击名称跳转至详情页

- **WHEN** 用户点击列表中某 ConfigMap 的名称
- **THEN** 浏览器导航至 `/configmaps/{ClusterId}/{Namespace}/{Name}` 独立详情页

#### Scenario: 跳转至结构化编辑页

- **WHEN** Admin 用户点击列表中某 ConfigMap 的"编辑"按钮
- **THEN** 浏览器导航至 `/configmaps/{ClusterId}/{Namespace}/{Name}/edit` 独立编辑页

#### Scenario: 跳转至 YAML 编辑页

- **WHEN** Admin 用户点击列表中某 ConfigMap 的"编辑YAML"按钮
- **THEN** 浏览器导航至 `/configmaps/{ClusterId}/{Namespace}/{Name}/yaml` 独立 YAML 编辑页

#### Scenario: 操作列按钮权限

- **WHEN** 非 Admin 登录用户查看 ConfigMap 列表
- **THEN** 操作列的"编辑"、"编辑YAML"、"删除"按钮均不可见（`AuthorizeView Roles="Admin"` 包裹），仅可点击名称查看详情

### Requirement: ConfigMap 详情查看

系统 SHALL 在 `/configmaps/{ClusterId:int}/{Namespace}/{Name}` 路由下提供 ConfigMap 只读详情页，展示该 ConfigMap 的元信息与 Data 键值对（左侧页签布局）。详情页 SHALL 不使用对话框，SHALL 不展示 YAML 定义。

#### Scenario: 查看详情

- **WHEN** 用户从列表页点击 ConfigMap 名称导航至详情页
- **THEN** 页面调 `ConfigMapService.GetConfigMapAsync` 加载 ConfigMap，展示名称、命名空间、创建时间、UID 元信息卡片，以及 Data 键值对区域：使用 `MudTabs Position="Position.Left"` 左侧垂直页签布局，每个 Data 键作为一个 `MudTabPanel` 页签（页签文本为键名），点击页签切换显示该键对应的值（只读多行文本，等宽字体）

#### Scenario: 无 Data 键

- **WHEN** ConfigMap 的 Data 为空
- **THEN** Data 页签区域显示"暂无 Data"提示

#### Scenario: 详情加载失败

- **WHEN** ConfigMap 在打开详情页时已被删除（k8s 返回 404）
- **THEN** 页面显示"ConfigMap 不存在或已被删除"提示与返回列表按钮

#### Scenario: 返回列表

- **WHEN** 用户点击详情页的"返回"按钮
- **THEN** 浏览器导航回 `/configmaps/{ClusterId}` 列表页

### Requirement: ConfigMap 新建

系统 SHALL 允许 `Admin` 角色用户在所选集群的指定命名空间下创建新 ConfigMap，通过对话框填写名称、命名空间、Data 键值对。

#### Scenario: Admin 新建 ConfigMap

- **WHEN** Admin 用户点击"新建 ConfigMap"按钮，填写名称、选择命名空间、编辑 Data 键值对后提交
- **THEN** 系统调 `ConfigMapService.CreateConfigMapAsync` 在目标集群创建 ConfigMap，成功后 `Snackbar` 提示"创建成功"，关闭对话框，父页面刷新列表

#### Scenario: 非 Admin 用户不可见新建按钮

- **WHEN** 非 Admin 登录用户查看 ConfigMap 列表
- **THEN** "新建 ConfigMap"按钮不可见（`AuthorizeView Roles="Admin"` 包裹）

#### Scenario: 名称校验失败

- **WHEN** 用户填写的名称不符合 k8s 命名规则（非小写字母/数字/`-`组成，或开头结尾非字母数字，或超过 253 字符）
- **THEN** 表单校验不通过，不提交，字段下方显示错误提示

#### Scenario: 键重复校验

- **WHEN** 用户填写的 Data 键值对中存在重复键名
- **THEN** 表单校验不通过，不提交

#### Scenario: 创建时名称冲突

- **WHEN** 提交创建后 k8s 返回 409（同名 ConfigMap 已存在）
- **THEN** `Snackbar` 提示"同名 ConfigMap 已存在"，对话框保持打开

### Requirement: ConfigMap 修改

系统 SHALL 允许 `Admin` 角色用户在 `/configmaps/{ClusterId:int}/{Namespace}/{Name}/edit` 路由下编辑已存在 ConfigMap 的 Data 键值对（增/改/删键），通过独立编辑页提交后全量替换集群上的 `data` 字段。编辑页 SHALL 不使用对话框，SHALL 不展示 YAML 定义，Data 键值对 SHALL 使用左侧垂直页签布局。

#### Scenario: Admin 修改 ConfigMap

- **WHEN** Admin 用户从列表页导航至编辑页，页面加载当前 Data 作为初始值，用户编辑后点击"保存"
- **THEN** 系统先 `Read` 取回原 `V1ConfigMap`（保留 `Uid`/`ResourceVersion`），替换 `Data` 后调 `ReplaceNamespacedConfigMapAsync`，成功后 `Snackbar` 提示"修改成功"，导航回列表页

#### Scenario: 页签式编辑 Data

- **WHEN** 编辑页展示 Data 键值对
- **THEN** 使用 `MudTabs Position="Position.Left"` 左侧垂直页签布局，每个 Data 键作为一个 `MudTabPanel` 页签（页签文本为键名），点击页签切换显示该键对应的可编辑多行文本（等宽字体）；页签可关闭（`Closeable`）用于删除键；页签栏提供"添加键"按钮用于新增键

#### Scenario: 添加新键

- **WHEN** 用户点击"添加键"按钮并输入新键名
- **THEN** 系统创建新的 `MudTabPanel` 页签并自动切换至该页签，用户可编辑新键的值

#### Scenario: 修改时资源版本冲突

- **WHEN** 提交修改后 k8s 返回 409（资源已被他人修改）
- **THEN** `Snackbar` 提示"资源已被他人修改，请刷新后重试"，编辑页保持打开

#### Scenario: 修改时 ConfigMap 已被删除

- **WHEN** 编辑页 `OnInitializedAsync` 调 `GetConfigMapAsync` 时 ConfigMap 已不存在
- **THEN** 页面显示"ConfigMap 不存在或已被删除"提示与返回列表按钮

#### Scenario: 键重复校验

- **WHEN** 用户添加新键时输入已存在的键名
- **THEN** 系统提示"键名已存在"，不创建重复页签

#### Scenario: 名称和命名空间只读

- **WHEN** 编辑页展示
- **THEN** 名称和命名空间字段为只读（`MudTextField ReadOnly`），仅 Data 键值对可编辑

#### Scenario: 返回列表

- **WHEN** 用户点击编辑页的"返回"按钮（未保存）
- **THEN** 浏览器导航回 `/configmaps/{ClusterId}` 列表页

### Requirement: ConfigMap YAML 编辑

系统 SHALL 允许 `Admin` 角色用户在 `/configmaps/{ClusterId:int}/{Namespace}/{Name}/yaml` 路由下通过编辑 YAML 定义来修改 ConfigMap。YAML 编辑页 SHALL 不使用对话框。

#### Scenario: Admin 编辑 YAML

- **WHEN** Admin 用户从列表页点击"编辑YAML"按钮导航至 YAML 编辑页
- **THEN** 页面调 `ConfigMapService.GetConfigMapAsync` 加载 ConfigMap，展示元信息卡片（名称、命名空间只读）与 YAML 编辑区（等宽字体多行文本框，内容来自 `ConfigMapDetailViewModel.Yaml`）

#### Scenario: 保存 YAML

- **WHEN** 用户编辑 YAML 后点击"保存"
- **THEN** 系统将 YAML 反序列化为 `V1ConfigMap`（`KubernetesYaml.Deserialize<V1ConfigMap>`），调 `ReplaceNamespacedConfigMapAsync` 提交，成功后 `Snackbar` 提示"修改成功"，导航回列表页

#### Scenario: YAML 格式错误

- **WHEN** 用户提交的 YAML 格式不合法（反序列化失败）
- **THEN** `Snackbar` 提示"YAML 格式错误: {ex.Message}"，编辑页保持打开

#### Scenario: YAML 编辑时资源版本冲突

- **WHEN** 提交 YAML 编辑后 k8s 返回 409（资源已被他人修改）
- **THEN** `Snackbar` 提示"资源已被他人修改，请刷新后重试"，编辑页保持打开

#### Scenario: YAML 编辑时 ConfigMap 已被删除

- **WHEN** YAML 编辑页 `OnInitializedAsync` 调 `GetConfigMapAsync` 时 ConfigMap 已不存在
- **THEN** 页面显示"ConfigMap 不存在或已被删除"提示与返回列表按钮

#### Scenario: 返回列表

- **WHEN** 用户点击 YAML 编辑页的"返回"按钮
- **THEN** 浏览器导航回 `/configmaps/{ClusterId}` 列表页

### Requirement: ConfigMap 删除

系统 SHALL 允许 `Admin` 角色用户从列表页删除指定 ConfigMap，删除前 SHALL 弹出确认对话框。

#### Scenario: Admin 删除 ConfigMap

- **WHEN** Admin 用户点击列表行的"删除"按钮
- **THEN** 系统弹出确认对话框（`DialogService.ShowMessageBoxAsync`），显示"确认删除 ConfigMap「{name}」？此操作不可撤销。"

#### Scenario: 确认删除

- **WHEN** 用户在确认对话框中点击"删除"
- **THEN** 系统调 `ConfigMapService.DeleteConfigMapAsync(clusterId, name, ns)`，成功后 `Snackbar` 提示"删除成功"，列表刷新

#### Scenario: 取消删除

- **WHEN** 用户在确认对话框中点击"取消"
- **THEN** 对话框关闭，不执行删除

#### Scenario: 删除时 ConfigMap 已不存在

- **WHEN** 提交删除后 k8s 返回 404（ConfigMap 已被他人删除）
- **THEN** `Snackbar` 提示"ConfigMap 不存在或已被删除"，列表刷新

### Requirement: 命名空间列表获取

系统 SHALL 在 ConfigMap 页面加载选定集群后，提供该集群的可用命名空间列表供过滤和新建对话框使用。命名空间列表来自集群实时查询（`ListNamespaceAsync`）。

#### Scenario: 获取命名空间列表成功

- **WHEN** 页面加载选定集群后调用 `GetNamespacesAsync(clusterId)`
- **THEN** 返回该集群的命名空间名称列表（`List<string>`），填充命名空间下拉

#### Scenario: 获取命名空间列表失败

- **WHEN** `ListNamespaceAsync` 抛异常（权限不足或网络问题）
- **THEN** 页面 try/catch 捕获，`Snackbar` 提示错误，命名空间下拉回退为手动输入框（新建对话框中）

### Requirement: 离线集群降级

系统 SHALL 在选定集群不可达时，不崩溃、不发起 k8s 调用，以明确状态提示替代列表内容。

#### Scenario: 选定离线集群

- **WHEN** 用户选择一个 `Status == Offline` 的集群（`ClusterService.GetClusterDetailAsync` 返回 `IsReachable == false`）
- **THEN** 右侧内容区显示"集群不可达，无法获取 ConfigMap"提示，不调 `ConfigMapService`，新建/编辑/删除按钮禁用

#### Scenario: 集群在线但 k8s 调用失败

- **WHEN** 集群 `IsReachable == true` 但 `ListConfigMapsAsync` 抛异常（网络抖动/认证过期）
- **THEN** 页面 try/catch 捕获，`Snackbar` 提示"加载 ConfigMap 列表失败: {ex.Message}"，表格区显示空

### Requirement: 导航入口

系统 SHALL 在侧边栏导航菜单中提供「配置管理」入口，指向 `/configmaps` 路由。

#### Scenario: 侧边栏导航

- **WHEN** 用户查看侧边栏 `Drawer.razor` 的 `MudNavMenu`
- **THEN** 在「节点管理」之后显示「配置管理」`MudNavLink`，图标为 `Settings`，`Match="NavLinkMatch.Prefix"` 使 `/configmaps` 和 `/configmaps/{id}` 均高亮该项

### Requirement: 页面访问控制

系统 SHALL 要求用户登录后才能访问 ConfigMap 管理页面（列表页、详情页），且编辑页、YAML 编辑页仅 `Admin` 角色可访问。

#### Scenario: 未登录访问

- **WHEN** 未登录用户访问 `/configmaps`、`/configmaps/{ClusterId}` 或 `/configmaps/{ClusterId}/{Namespace}/{Name}`
- **THEN** 重定向至登录页（`@attribute [Authorize]` + 全局认证配置）

#### Scenario: 非 Admin 用户访问编辑页

- **WHEN** 非 Admin 登录用户访问 `/configmaps/{ClusterId}/{Namespace}/{Name}/edit` 或 `/configmaps/{ClusterId}/{Namespace}/{Name}/yaml`
- **THEN** 重定向至 `/access-denied` 页面（`@attribute [Authorize(Roles="Admin")]`）

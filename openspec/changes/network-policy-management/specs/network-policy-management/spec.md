## ADDED Requirements

### Requirement: NetworkPolicy 列表查看

系统 SHALL 提供 `/networkpolicies` 页面，以双栏布局展示多集群 NetworkPolicy 列表。左侧为集群树选择器（按分组折叠），右侧为选中集群的 NetworkPolicy 表格，包含名称、命名空间、策略类型（Ingress/Egress）、入站规则数、出站规则数、创建时间等列。

#### Scenario: 选择集群查看 NetworkPolicy 列表

- **WHEN** 用户在左侧集群树点击一个可达集群
- **THEN** 右侧表格展示该集群所有命名空间的 NetworkPolicy 列表，每行含名称、命名空间、策略类型、入站/出站规则数量、创建时间

#### Scenario: 未选择集群

- **WHEN** 用户访问 `/networkpolicies` 但尚未选择集群
- **THEN** 右侧显示空状态提示"请从左侧选择一个集群"

#### Scenario: 集群不可达

- **WHEN** 用户选择一个不可达（Offline）的集群
- **THEN** 右侧显示"集群不可达"提示，不尝试加载 NetworkPolicy 列表

#### Scenario: 搜索过滤

- **WHEN** 用户在搜索框输入名称关键词
- **THEN** 表格仅显示名称匹配的 NetworkPolicy（前端过滤，大小写不敏感）

### Requirement: NetworkPolicy 详情查看

系统 SHALL 提供独立详情页 `/networkpolicies/{ClusterId:int}/{Namespace}/{Name}`，完整展示单个 NetworkPolicy 的 Pod 选择器、策略类型和所有 Ingress/Egress 规则。

#### Scenario: 查看 NetworkPolicy 详情

- **WHEN** 用户从列表页点击 NetworkPolicy 名称
- **THEN** 跳转到详情页，展示 Pod 选择器（matchLabels）、策略类型、完整 Ingress 规则列表（含端口、对等体选择器、IP 块）和 Egress 规则列表

#### Scenario: 详情页展示 YAML 原始内容

- **WHEN** 用户在详情页查看 NetworkPolicy
- **THEN** 页面底部或独立区域展示序列化为 YAML 格式的完整 NetworkPolicy 内容

#### Scenario: 详情页资源不存在

- **WHEN** 用户访问不存在的 NetworkPolicy（如已被删除）
- **THEN** 页面显示"未找到此 NetworkPolicy"提示并提供返回列表的按钮

### Requirement: NetworkPolicy 创建

系统 SHALL 提供创建 NetworkPolicy 的对话框，允许 Admin 角色用户输入名称、命名空间、Pod 选择器、策略类型和入站/出站规则。

#### Scenario: 成功创建 NetworkPolicy

- **WHEN** Admin 用户在列表页点击"新建"按钮，填写名称、命名空间、Pod 选择器（key-value）、选择策略类型（Ingress 和/或 Egress）、添加至少一条规则（含端口和/或对等体），提交表单
- **THEN** 系统调用 k8s API 创建 NetworkPolicy，成功后显示成功提示并刷新列表

#### Scenario: 创建失败

- **WHEN** 创建请求被 k8s API Server 拒绝（如名称冲突或规则格式错误）
- **THEN** 显示包含 k8s 错误信息的 Snackbar 提示，对话框保持打开以便用户修改

#### Scenario: Guest 不可见创建按钮

- **WHEN** Guest 用户查看 NetworkPolicy 列表
- **THEN** "新建"按钮不渲染（`AuthorizeView Roles="Admin"`）

### Requirement: NetworkPolicy 更新

系统 SHALL 提供两种更新方式：表单编辑（从详情页进入）和 YAML 直接编辑（独立页面 `/networkpolicies/{ClusterId:int}/{Namespace}/{Name}/yaml`）。

#### Scenario: 通过表单更新 NetworkPolicy

- **WHEN** Admin 用户在详情页点击"编辑"进入编辑页，修改 Pod 选择器或规则后保存
- **THEN** 系统调用 k8s API 替换 NetworkPolicy（read-then-replace），成功后显示成功提示并返回详情页

#### Scenario: 通过 YAML 编辑更新 NetworkPolicy

- **WHEN** Admin 用户在 YAML 编辑页修改 YAML 内容后点击"保存"
- **THEN** 系统反序列化 YAML 为 k8s 对象并调用 `ReplaceNamespacedNetworkPolicy`，成功后显示成功提示并返回详情页

#### Scenario: Guest 不可见编辑按钮

- **WHEN** Guest 用户查看 NetworkPolicy 详情
- **THEN** "编辑"和"YAML 编辑"按钮不渲染

### Requirement: NetworkPolicy 删除

系统 SHALL 允许 Admin 角色用户删除 NetworkPolicy，删除前需确认。

#### Scenario: 成功删除 NetworkPolicy

- **WHEN** Admin 用户在列表页点击删除按钮，确认对话框中选择"是"
- **THEN** 系统调用 k8s API 删除 NetworkPolicy，成功后显示成功提示并刷新列表

#### Scenario: 取消删除

- **WHEN** Admin 用户在确认对话框中点击"取消"
- **THEN** 不执行删除操作，对话框关闭

#### Scenario: Guest 不可见删除按钮

- **WHEN** Guest 用户查看 NetworkPolicy 列表
- **THEN** 删除按钮不渲染

### Requirement: 导航入口

系统 SHALL 在左侧抽屉导航菜单中提供"网络策略"入口。

#### Scenario: 导航到 NetworkPolicy 列表

- **WHEN** 已登录用户点击左侧导航"网络策略"（图标 `GppMaybe` 或 `Security`）
- **THEN** 跳转到 `/networkpolicies` 页面，且当前导航项高亮

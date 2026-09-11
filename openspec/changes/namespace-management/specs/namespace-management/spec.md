# namespace-management delta

## Purpose

为多集群管理系统提供 Kubernetes Namespace 的统一管理能力:按集群查看命名空间清单与状态、查看标签/注解与 YAML、以 YAML 创建命名空间,并带系统命名空间硬保护的删除。命名空间是集群级资源,本能力不改变各页面既有的命名空间下拉过滤行为。

## ADDED Requirements

### Requirement: 命名空间管理导航入口
系统 SHALL 在侧边导航(Drawer)新增顶层「命名空间管理」入口(路由 `/namespaces`),位于「节点管理」之后;入口 SHALL 通过 `/namespaces` 前缀匹配保持激活态,样式遵循 Swiss Industrial Print 导航契约(墨块反白激活态)。列表页 SHALL 同时支持不带集群与带集群参数两种路由(`/namespaces` 与 `/namespaces/{ClusterId:int}`),沿用集群选择状态(`ClusterSelectionState`)在无参路由下恢复上次选择的集群。

#### Scenario: 导航入口展示与激活
- **WHEN** 任意登录用户打开侧边导航
- **THEN** 可以看到顶层「命名空间管理」入口;进入 `/namespaces` 系页面时该入口呈墨块激活态

#### Scenario: 会话内集群记忆
- **WHEN** 用户从 `/namespaces` 直接访问(不带集群参数)
- **THEN** 页面恢复会话中上次选择的集群并加载其命名空间列表

### Requirement: 命名空间列表页
系统 SHALL 提供命名空间列表页,页面结构沿用既有集群级列表骨架:集群选择侧栏、集群状态徽章、刷新按钮、名称搜索与列表表格。表格列 SHALL 为:名称(`.link-primary` 链接,进入详情)、状态、标签数(等宽字体)、创建时间、操作;操作列含「详情」(所有登录用户)与「删除」(仅 Admin,受保护命名空间禁用)。名称搜索 SHALL 在点击「查询」时对已加载列表做客户端过滤,「重置」清空搜索并重新加载。刷新按钮 SHALL 重新加载集群上下文与命名空间列表,且不对其做角色门控。未选择集群时 SHALL 显示「请从左侧选择一个集群」空态引导;集群不可达时 SHALL 显示不可达提示、不提供列表数据并禁用「新建命名空间」入口。列表加载态与空态 SHALL 遵循 `ui-theme` 的空态/加载态契约。

#### Scenario: 列表加载与名称过滤
- **WHEN** 用户选择集群,在名称搜索框输入 `kube` 并点击查询
- **THEN** 表格只显示名称包含 `kube` 的命名空间,行内展示状态、标签数与创建时间

#### Scenario: 集群不可达
- **WHEN** 所选集群状态为不可达
- **THEN** 页面显示「集群不可达」提示,不提供列表数据与「新建命名空间」入口

#### Scenario: 名称链接进入详情
- **WHEN** 用户点击列表行中的名称链接
- **THEN** 页面导航到该命名空间的详情页

#### Scenario: 未选择集群
- **WHEN** 用户打开 `/namespaces` 且会话中没有已选择的集群
- **THEN** 页面显示「请从左侧选择一个集群」空态引导

### Requirement: 命名空间状态展示
命名空间状态 SHALL 以 `ui-theme` 的淡彩状态徽章展示:阶段为 `Active` 的命名空间显示「在线」样式,阶段为 `Terminating` 的命名空间显示「未知」样式,其余未识别阶段显示「未知」样式。

#### Scenario: Active 命名空间
- **WHEN** 列表中某命名空间的 `.status.phase` 为 `Active`
- **THEN** 该行状态列显示在线徽章

#### Scenario: Terminating 命名空间
- **WHEN** 列表中某命名空间的 `.status.phase` 为 `Terminating`
- **THEN** 该行状态列显示未知徽章,且该行仍可查看详情

### Requirement: 命名空间详情页
系统 SHALL 提供命名空间详情页(路由 `/namespaces/{ClusterId:int}/{Name}`),对所有登录用户可见。页面 SHALL 由工具栏与 `MudTabs` 组成:工具栏含「返回列表」、命名空间名称、状态徽章与「刷新」按钮;tab 顺序 SHALL 为 YAML → 标签与注解。YAML tab SHALL 展示只读 YAML 视图(复用 `yaml-textarea` 只读卡);标签与注解 tab SHALL 以带计数的只读键值表分别展示标签与注解,为空时显示空态占位。tab 选择 SHALL 为页面局部状态,不写入路由或持久化存储。命名空间不存在(K8s 404)时,页面 SHALL 显示「不存在或已被删除」空态并提供返回列表入口;K8s 读取失败时 SHALL 呈现失败态,不向用户弹错。

#### Scenario: 详情页加载
- **WHEN** 用户从列表进入某命名空间详情页
- **THEN** 页面显示工具栏、YAML 视图与标签/注解键值表

#### Scenario: 对象已被删除
- **WHEN** 详情页请求的命名空间在集群中不存在
- **THEN** 页面显示不存在提示与「返回列表」入口

#### Scenario: 标签与注解为空
- **WHEN** 命名空间没有任何标签或注解
- **THEN** 对应键值表显示空态占位

### Requirement: 命名空间 YAML 新建
系统 SHALL 允许 Admin 从列表页通过「新建命名空间」按钮打开 YAML 对话框;对话框 SHALL 在打开时预置 `wwwroot/templates/namespace/default.yaml` 模板,模板缺失或读取失败时 SHALL 回退最小骨架并记录警告日志,不阻塞对话框打开。提交时系统 SHALL 反序列化为 `V1Namespace` 并校验 `metadata.name` 非空;解析失败或缺少名称 SHALL 抛出中文校验异常且不调用 K8s API。创建成功 SHALL 提示成功、关闭对话框、刷新列表并写入审计;K8s 返回 409 冲突时 SHALL 提示「同名命名空间已存在」并保持对话框打开。

#### Scenario: 从模板新建命名空间
- **WHEN** Admin 打开新建对话框,填写 `metadata.name: dev` 后提交
- **THEN** 系统在集群中创建该命名空间,提示成功并刷新列表,写入命名空间创建审计

#### Scenario: YAML 缺少名称
- **WHEN** Admin 提交的 YAML 未指定 `metadata.name`
- **THEN** 系统提示中文校验错误,不调用 K8s API

#### Scenario: 同名命名空间已存在
- **WHEN** Admin 提交的命名空间名称在集群中已存在(K8s 返回 409)
- **THEN** 对话框提示「同名命名空间已存在」并保持打开

#### Scenario: 模板文件缺失仍可打开对话框
- **WHEN** `wwwroot/templates/namespace/default.yaml` 不存在且 Admin 打开新建对话框
- **THEN** YAML 编辑框显示附缺失提示的最小骨架,服务日志记录警告,不阻塞对话框打开

### Requirement: 命名空间删除与系统命名空间保护
系统 SHALL 允许 Admin 从列表页删除命名空间,删除前 SHALL 弹出确认对话框,文案 SHALL 说明该操作将删除命名空间及其中的所有资源。服务端 SHALL 硬保护 `default` 及以 `kube-` 开头的命名空间:对这些名称的删除请求 SHALL 直接抛出中文 `ValidationException`,不调用 K8s API;列表页 SHALL 同步禁用这些行的删除入口。删除成功 SHALL 提示成功、刷新列表并写入审计(类别「命名空间」、操作「删除」,目标描述含名称与集群名)。Member 角色不可见删除与新建入口。

#### Scenario: 确认后删除
- **WHEN** Admin 确认删除某普通命名空间
- **THEN** 系统调用 K8s 删除该命名空间,提示成功,刷新列表,写入删除审计

#### Scenario: 受保护命名空间被服务端拒绝
- **WHEN** 通过任意入口请求删除 `kube-system`
- **THEN** 服务层抛出中文校验异常,不调用 K8s API

#### Scenario: 受保护命名空间删除入口禁用
- **WHEN** Admin 浏览包含 `default`、`kube-system` 的列表
- **THEN** 这些行的删除入口为禁用状态

#### Scenario: Member 无写入口
- **WHEN** Member 用户浏览命名空间列表与详情页
- **THEN** 页面不渲染新建与删除入口

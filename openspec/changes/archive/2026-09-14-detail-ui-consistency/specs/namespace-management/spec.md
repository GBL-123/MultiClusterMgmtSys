## MODIFIED Requirements

### Requirement: 命名空间详情页
系统 SHALL 提供命名空间详情页(路由 `/namespaces/{ClusterId:int}/{Name}`),对所有登录用户可见。页面 SHALL 由工具栏与 `MudTabs` 组成:工具栏含「返回列表」、命名空间名称、状态徽章与「刷新」按钮;tab 顺序 SHALL 为 YAML → 标签与注解。YAML tab SHALL 展示只读 YAML 视图(复用 `yaml-textarea` 只读卡);标签与注解 tab SHALL 以只读键值表分别展示标签与注解,为空时显示空态占位。tab 选择 SHALL 为页面局部状态,不写入路由或持久化存储。命名空间不存在(K8s 404)时,页面 SHALL 显示「不存在或已被删除」空态并提供返回列表入口;K8s 读取失败时 SHALL 呈现失败态,不向用户弹错。

#### Scenario: 详情页加载
- **WHEN** 用户从列表进入某命名空间详情页
- **THEN** 页面显示工具栏、YAML 视图与标签/注解键值表

#### Scenario: 对象已被删除
- **WHEN** 详情页请求的命名空间在集群中不存在
- **THEN** 页面显示不存在提示与「返回列表」入口

#### Scenario: 标签与注解为空
- **WHEN** 命名空间没有任何标签或注解
- **THEN** 对应键值表显示空态占位

#### Scenario: 标签与注解卡标题无计数
- **WHEN** 标签与注解 tab 渲染标签卡与注解卡
- **THEN** 卡片标题仅显示「标签」「注解」文字,不显示条目数量

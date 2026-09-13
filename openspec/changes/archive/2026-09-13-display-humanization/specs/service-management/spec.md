## MODIFIED Requirements

### Requirement: 服务列表页
系统 SHALL 提供服务列表页,页面结构沿用 ConfigMap 列表骨架:集群选择侧栏、集群状态徽章、刷新按钮、命名空间下拉过滤(选项来自集群命名空间列表)、类型下拉过滤(ClusterIP/NodePort/LoadBalancer/ExternalName,选项标签遵循 `display-conventions` 以中文为主、原值对照)、名称搜索、端口搜索、统一列表表格。列表行 SHALL 展示:名称(`.link-primary` 链接,进入详情)、命名空间、类型、端口列(见端口展示要求)、对外入口列、ClusterIP、创建时间、操作列(详情、编辑 YAML、删除,删除与编辑仅 Admin 可见);类型列 SHALL 以中文主行 + 英文原值次行展示(`ClusterIP` → 集群内 IP、`NodePort` → 节点端口、`LoadBalancer` → 负载均衡、`ExternalName` → 外部名称)。端口搜索 SHALL 同时匹配服务端口、NodePort 与容器端口(targetPort)。未选择集群时 SHALL 显示空态引导;集群不可达时 SHALL 显示不可达提示并禁用写操作入口;空态文案为「[ 暂无服务 ]」。

#### Scenario: 列表加载与过滤
- **WHEN** 用户选择集群并按命名空间、类型过滤,输入名称或端口搜索
- **THEN** 表格只显示匹配条件的服务,行内展示类型、端口与对外入口

#### Scenario: 端口搜索命中容器端口
- **WHEN** 用户在端口搜索框输入某服务的容器端口数值(如 apiserver 服务的 6443)
- **THEN** 列表仍能筛出该服务

#### Scenario: 类型筛选
- **WHEN** 用户在类型下拉选择 NodePort
- **THEN** 列表只显示 NodePort 型服务
- **AND** 该选项显示为「节点端口 (NodePort)」,筛选值仍为原始 `NodePort`

#### Scenario: 类型列双语展示
- **WHEN** 列表行渲染类型为 `ClusterIP` 的服务
- **THEN** 类型单元格主行显示「集群内 IP」,次行显示等宽字体的 `ClusterIP`

#### Scenario: 集群不可达
- **WHEN** 所选集群状态为不可达
- **THEN** 页面显示"集群不可达"提示,不提供列表数据与写操作入口

#### Scenario: 名称链接进入详情
- **WHEN** 用户点击列表行中的名称链接
- **THEN** 页面导航到该服务的详情页

### Requirement: 服务详情页
系统 SHALL 提供服务详情页(路由 `/services/{ClusterId:int}/{Namespace}/{Name}`),对所有登录用户可见,展示 YAML 视图(复用 `yaml-textarea` 只读卡)、端口表(端口/targetPort/协议/nodePort 全量列)与后端 Endpoints 列表(地址:端口 + 状态徽章,复用 `.status-badge` 设计语言)。后端列表的 tab 标签 SHALL 为「后端端点 (Endpoints)」,就绪状态 SHALL 以中文主行 + 英文原值次行展示(就绪 / `Ready`、未就绪 / `NotReady`);页面标题 SHALL 为「服务详情」。资源不存在时 SHALL 显示"不存在或已被删除"空态并可返回列表。K8s 读取失败时相应区块 SHALL 呈现失败态,不向用户弹错。

#### Scenario: 详情页加载
- **WHEN** 用户从列表进入服务详情页
- **THEN** 页面显示该服务的 YAML 视图、端口表与「后端端点 (Endpoints)」列表

#### Scenario: 端点状态双语展示
- **WHEN** Endpoints 列表渲染一个 `Ready` 的后端
- **THEN** 该行状态徽章主行显示「就绪」,次行显示等宽字体的 `Ready`

#### Scenario: 对象已被删除
- **WHEN** 详情页请求的服务在集群中不存在
- **THEN** 页面显示不存在提示与"返回列表"入口

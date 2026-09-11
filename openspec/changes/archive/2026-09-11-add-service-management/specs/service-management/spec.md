# service-management delta

## ADDED Requirements

### Requirement: 服务管理导航入口
系统 SHALL 在侧边导航(Drawer)新增「网络管理」导航组(`MudNavGroup`),内含「服务管理」子入口(路由 `/services`),位于「工作负载」组之后;导航组 SHALL 通过 `/services` 前缀匹配保持展开,样式遵循 Swiss Industrial Print 导航契约(墨块反白激活态)。列表页 SHALL 同时支持不带集群与带集群参数两种路由(`/services` 与 `/services/{ClusterId:int}`),沿用集群选择状态(`ClusterSelectionState`)在无参路由下恢复上次选择。

#### Scenario: 导航组展示与激活
- **WHEN** 任意登录用户打开侧边导航
- **THEN** 可以看到「网络管理」导航组,展开后含「服务管理」入口;进入 `/services` 系页面时该入口呈墨块激活态

#### Scenario: 会话内集群记忆
- **WHEN** 用户从 `/services` 直接访问(不带集群参数)
- **THEN** 页面恢复会话中上次选择的集群并加载其服务列表

### Requirement: 服务列表页
系统 SHALL 提供服务列表页,页面结构沿用 ConfigMap 列表骨架:集群选择侧栏、集群状态徽章、刷新按钮、命名空间下拉过滤(选项来自集群命名空间列表)、类型下拉过滤(ClusterIP/NodePort/LoadBalancer/ExternalName)、名称搜索、端口搜索、统一列表表格。列表行 SHALL 展示:名称(`.link-primary` 链接,进入详情)、命名空间、类型(ClusterIP/NodePort/LoadBalancer/ExternalName,等宽字体)、端口列(见端口展示要求)、对外入口列、ClusterIP、创建时间、操作列(详情、编辑 YAML、删除,删除与编辑仅 Admin 可见)。端口搜索 SHALL 同时匹配服务端口、NodePort 与容器端口(targetPort)。未选择集群时 SHALL 显示空态引导;集群不可达时 SHALL 显示不可达提示并禁用写操作入口。

#### Scenario: 列表加载与过滤
- **WHEN** 用户选择集群并按命名空间、类型过滤,输入名称或端口搜索
- **THEN** 表格只显示匹配条件的服务,行内展示类型、端口与对外入口

#### Scenario: 端口搜索命中容器端口
- **WHEN** 用户在端口搜索框输入某服务的容器端口数值(如 apiserver 服务的 6443)
- **THEN** 列表仍能筛出该服务

#### Scenario: 类型筛选
- **WHEN** 用户在类型下拉选择 NodePort
- **THEN** 列表只显示 NodePort 型服务

#### Scenario: 集群不可达
- **WHEN** 所选集群状态为不可达
- **THEN** 页面显示"集群不可达"提示,不提供列表数据与写操作入口

#### Scenario: 名称链接进入详情
- **WHEN** 用户点击列表行中的名称链接
- **THEN** 页面导航到该服务的详情页

### Requirement: 端口展示格式
列表端口列 SHALL 每端口独占一行、等宽字体展示,遵循 kubectl 视觉语法并补充 targetPort:NodePort/LoadBalancer 型显示 `{端口}:{nodePort}/{协议} → {targetPort}`,其余类型显示 `{端口}/{协议} → {targetPort}`;targetPort 为命名端口时显示端口名。端口数超过 3 个时 SHALL 截断为前 3 行加"+N"提示(详情页提供全量端口表)。

#### Scenario: NodePort 服务端口行
- **WHEN** NodePort 型服务声明端口 80(nodePort 30080,协议 TCP,targetPort 8080)
- **THEN** 端口列该行显示 `80:30080/TCP → 8080`

#### Scenario: 命名 targetPort
- **WHEN** 服务的 targetPort 为命名端口 "http"
- **THEN** 端口列该行以 `→ http` 结尾

#### Scenario: 多端口截断
- **WHEN** 服务声明 5 个端口
- **THEN** 端口列显示前 3 行并附"+2"提示

### Requirement: 对外入口列
列表 SHALL 提供「对外入口」列,汇总服务的集群外可达信息:LoadBalancer 型显示 `status.loadBalancer.ingress` 外部地址(未分配时显示"待分配"),NodePort 型逐端口显示 `*:{nodePort}`,ExternalName 型显示其外部 DNS 名称,ClusterIP 型显示"—"。该列数据 SHALL 仅取自 Service 对象自身,不产生额外 K8s 调用。

#### Scenario: LoadBalancer 已分配外部 IP
- **WHEN** LoadBalancer 型服务已获得外部 IP 203.0.113.7
- **THEN** 对外入口列显示 `203.0.113.7`

#### Scenario: ClusterIP 无对外入口
- **WHEN** ClusterIP 型服务无任何对外暴露
- **THEN** 对外入口列显示"—"

### Requirement: 特殊服务形态展示
系统 SHALL 显式区分两类特殊服务形态:Headless 服务(`clusterIP: None`)在 ClusterIP 列显示等宽字体 `None`;ExternalName 服务在端口列显示 `[ 暂无 ]` 空态样式(复用空态设计语言),对外入口列显示其 DNS 名称。

#### Scenario: Headless 服务
- **WHEN** 列表中某服务 `spec.clusterIP` 为 `None`
- **THEN** 该行 ClusterIP 列显示 `None`,其余列正常展示

#### Scenario: ExternalName 服务
- **WHEN** 列表中某服务类型为 ExternalName,外部名称为 `ext.db.io`
- **THEN** 该行端口列显示 `[ 暂无 ]`,对外入口列显示 `ext.db.io`

### Requirement: 服务详情页
系统 SHALL 提供服务详情页(路由 `/services/{ClusterId:int}/{Namespace}/{Name}`),对所有登录用户可见,展示 YAML 视图(复用 `yaml-textarea` 只读卡)、端口表(端口/targetPort/协议/nodePort 全量列)与后端 Endpoints 列表(地址:端口 + Ready 状态徽章,复用 `.status-badge` 设计语言)。资源不存在时 SHALL 显示"不存在或已被删除"空态并可返回列表。K8s 读取失败时相应区块 SHALL 呈现失败态,不向用户弹错。

#### Scenario: 详情页加载
- **WHEN** 用户从列表进入服务详情页
- **THEN** 页面显示该服务的 YAML 视图、端口表与 Endpoints 列表

#### Scenario: 对象已被删除
- **WHEN** 详情页请求的服务在集群中不存在
- **THEN** 页面显示不存在提示与"返回列表"入口

### Requirement: Endpoints 读取与降级
系统 SHALL 优先通过 EndpointSlice API(discovery.k8s.io/v1,按 `kubernetes.io/service-name` 标签过滤)读取服务后端并展平全部 slice;当集群不支持该 API(请求返回 404)时 SHALL 降级为旧 Endpoints API(CoreV1)读取,其余异常照常走 K8s 异常翻译链路。两类来源 SHALL 统一映射为相同的数据形态(地址:端口、Ready/NotReady 状态)。

#### Scenario: 新集群走 EndpointSlice
- **WHEN** 集群支持 discovery.k8s.io/v1 且服务有 2 个就绪后端
- **THEN** 详情页 Endpoints 列表显示 2 条记录,状态徽章为就绪

#### Scenario: 老集群降级
- **WHEN** 集群不支持 EndpointSlice API(请求返回 404)
- **THEN** 系统改用旧 Endpoints API 读取,详情页仍正常显示后端列表

### Requirement: 服务 YAML 新建
系统 SHALL 允许 Admin 从列表页通过「新建」按钮打开 YAML 编辑对话框;对话框 SHALL 提供 Service 类型选择(ClusterIP/NodePort/LoadBalancer/ExternalName),并按所选类型预置对应 YAML 模板(ExternalName 模板无 selector 与端口,含 externalName;NodePort/LoadBalancer 模板的 nodePort 行以注释说明可省略并由集群自动分配)。创建模板 SHALL 外置为配置文件(`wwwroot/templates/{资源}/{类型}.yaml`),不写死在组件代码中;模板文件缺失或读取失败时 SHALL 回退最小骨架(附缺失提示注释)并记录警告日志,不阻塞对话框打开。提交时系统 SHALL 反序列化用户 YAML 并校验 `metadata.namespace` 必填后创建。YAML 解析失败 SHALL 抛出中文校验异常(不直出原始异常);创建成功 SHALL 写入审计(类别"服务"、操作"创建")并刷新列表。

#### Scenario: 按类型选择模板
- **WHEN** Admin 在创建对话框中选择 LoadBalancer 类型
- **THEN** YAML 编辑框内容切换为 `type: LoadBalancer` 的模板

#### Scenario: 从模板新建服务
- **WHEN** Admin 点击「新建」并提交合法 YAML(含 `metadata.namespace`)
- **THEN** 系统在对应命名空间创建 Service,提示成功并刷新列表,写入审计记录

#### Scenario: YAML 缺少命名空间
- **WHEN** Admin 提交的 YAML 未指定 `metadata.namespace`
- **THEN** 系统提示中文校验错误,不调用 K8s API

#### Scenario: 模板文件缺失仍可打开对话框
- **WHEN** `wwwroot/templates/service/clusterip.yaml` 不存在且 Admin 打开创建对话框
- **THEN** YAML 编辑框显示附缺失提示的最小骨架,服务日志记录警告,不抛出用户可见错误

### Requirement: 服务 YAML 编辑与不可变字段守卫
系统 SHALL 允许 Admin 对既有服务进行 YAML 编辑:系统读取现有对象后,若用户 YAML 中 `spec.clusterIP`/`spec.clusterIPs`/`spec.ipFamilies` 与现有值不同,SHALL 在调用 K8s API 之前抛出中文校验异常(提示该字段不可变,如需更换请删除后重建);用户 YAML 省略这些字段时 SHALL 以现有值补齐。提交 SHALL 携带现有 `resourceVersion`/`uid` 并保留服务状态(status)后整体替换。编辑成功 SHALL 写入审计(类别"服务"、操作"修改")并刷新详情。

#### Scenario: 修改可变字段成功
- **WHEN** Admin 将服务的 selector 与端口修改后提交(不可变字段未变)
- **THEN** 系统替换成功,新 selector 与端口生效,写入审计记录

#### Scenario: 修改 clusterIP 被拒
- **WHEN** Admin 将 YAML 中 clusterIP 改为另一 IP 后提交
- **THEN** 系统提示"clusterIP 为不可变字段,如需更换请删除后重建"类中文错误,不调用 K8s API

#### Scenario: 省略不可变字段
- **WHEN** Admin 提交的 YAML 未包含 `spec.clusterIP`
- **THEN** 系统以现有 clusterIP 补齐并替换成功

### Requirement: 服务删除
系统 SHALL 允许 Admin 从列表或详情页删除服务,删除前 SHALL 弹出确认对话框(含命名空间与名称);删除成功 SHALL 写入审计(类别"服务"、操作"删除",目标含命名空间、名称与集群名)并刷新列表。Member 角色不可见删除入口。

#### Scenario: 确认后删除
- **WHEN** Admin 确认删除 `default/nginx-svc`
- **THEN** 系统调用 K8s 删除该服务,提示成功,刷新列表,写入审计记录

#### Scenario: Member 无删除入口
- **WHEN** Member 用户浏览服务列表与详情页
- **THEN** 页面不渲染删除、新建、编辑入口

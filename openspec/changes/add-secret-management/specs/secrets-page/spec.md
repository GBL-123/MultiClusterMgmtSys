# Spec Delta

## Purpose

为多集群管理系统提供 Secret(敏感配置)管理:在 ClusterMap 现有归属与页面模式下新增密钥的列表/详情/YAML 创建/编辑/删除,并以「值掩码 + 查看明文揭示 + 揭示审计」与「编辑占位符机制」处理 Secret 特有的敏感性,避免明文泄露与既有值被误清空。

## ADDED Requirements

### Requirement: Secret 列表页与集群选择侧栏

系统 SHALL 提供密钥管理列表页(路由 `/secrets`、`/secrets/{ClusterId:int}`),布局与配置管理页一致:左侧持久的集群选择侧栏(240px `MudPaper`,「集群选择」标题 + `MudNavMenu` 按 `GroupName` 分组排列、「未分组」最后,组头可折叠,集群行带状态圆点与活动高亮,无搜索框),右侧为内容面板;点击集群行 `NavigateTo("/secrets/{ClusterId}")`。侧栏集群列表 SHALL 在页面挂载时经 `ClusterService.GetPagedAsync` 一次载入(页大小 1000,按名称排序),分组在侧栏组件内部完成。

URL 不带 `ClusterId` 时:若 `ClusterSelectionState.SelectedClusterId` 为 null,右侧 SHALL 渲染垂直居中的「请从左侧选择一个集群」空态;若记忆了集群 Id,页面 SHALL 就地加载该集群的 Secret 列表(URL 保持 `/secrets` 不跳转,侧栏高亮恢复的集群行)。页面在 `ClusterId` 有效且集群加载成功时 SHALL 调 `ClusterSelectionState.Set`。

#### Scenario: 无选择时空态

- **WHEN** 登录用户打开 `/secrets` 且 `ClusterSelectionState.SelectedClusterId` 为 null
- **THEN** 右侧渲染「请从左侧选择一个集群」空态,左侧侧栏显示完整分组集群列表

#### Scenario: 电路内记忆集群就地恢复

- **WHEN** 用户先前在 `/secrets/3`(或集群详情页)选中集群 3,再打开 `/secrets`
- **THEN** URL 保持 `/secrets`,右侧就地渲染集群 3 的 Secret 列表,侧栏高亮集群 3

#### Scenario: 侧栏切换集群

- **WHEN** 用户在侧栏点击集群 5
- **THEN** 系统导航到 `/secrets/5` 并加载集群 5 的 Secret 列表

### Requirement: Secret 列表页表格与筛选

选中集群时,系统 SHALL 在右侧渲染列表壳(单个 `MudPaper` 内标题行 + 筛选栏,下方 `MudTable`):标题行含「密钥管理」h5、所选集群名称与状态的 `MudChip`、刷新按钮、新建 Secret 按钮;MudTable 与集群表格视觉一致(Hover/FixedHeader/可排序表头/「正在加载...」/「暂无 Secret 或没有符合筛选条件的 Secret」空态)。

表格列 SHALL 为:名称(点击进详情)、命名空间、类型(等宽原文展示,如 Opaque)、键数(data/stringData/binaryData 键数之和)、创建时间(`yyyy-MM-dd HH:mm`)、操作(详情/编辑/删除图标按钮)。筛选栏 SHALL 提供命名空间下拉 + 名称搜索 + 查询/重置按钮,命名空间下拉数据经 `SecretService.GetNamespacesAsync`。

新建 Secret 按钮 SHALL 对所有登录用户渲染(集群不可达时禁用;Member 创建进保护命名空间由服务端黑名单拒绝);集群不可达时页面 SHALL 渲染「集群不可达,无法获取 Secret」空态卡且不渲染表格;集群不存在时 SHALL 渲染「未找到该集群」卡与「返回集群列表」按钮。

#### Scenario: 可达集群列表渲染

- **WHEN** 用户进入 `/secrets/3` 且集群可达
- **THEN** 渲染标题行(集群 chip + 新建按钮)+ 筛选栏 + Secret 表格,各列为名称/命名空间/类型/键数/创建时间/操作
- **AND** 名称/命名空间/键数/创建时间表头可客户端排序

#### Scenario: 不可达集群

- **WHEN** 所选集群 `IsReachable` 为 false
- **THEN** 新建按钮渲染但禁用,页面显示集群不可达空态卡,无表格

#### Scenario: Member 可见新建入口

- **WHEN** Member 打开可达集群的 Secret 列表页
- **THEN** 新建 Secret 按钮可见且可打开创建对话框

### Requirement: Secret 详情页与内容受限态

系统 SHALL 提供详情页(路由 `/secrets/{ClusterId:int}/{Namespace}/{Name}`),以双 tab 展示:键值 tab(逐 key 的值行)与 YAML tab(原始 YAML 文本,值保持 base64 原文)。详情页 SHALL 在服务端投影 `CanOperate`(复用 k8s-resource-ownership 判定,Admin 短路为 true):

- `CanOperate` 为 true 的用户 SHALL 看到完整内容(键值 + YAML);
- `CanOperate` 为 false 的用户 SHALL 看到受限态:不渲染任何值内容与 YAML 文本,显示「无权查看该 Secret 的内容」提示,页面其余结构(名称/命名空间/类型/创建时间等元数据)照常;
- 非归属用户访问他人 Secret 的 YAML 编辑页与揭示入口 SHALL 同样不可用。

#### Scenario: 归属者查看详情

- **WHEN** Secret 的归属者(或 Admin)打开详情页
- **THEN** 键值 tab 与 YAML tab 均渲染完整内容

#### Scenario: 非归属者查看详情受限

- **WHEN** Member 打开他人(或无归属)Secret 的详情页
- **THEN** 页面显示「无权查看该 Secret 的内容」受限态,不渲染任何值与 YAML 文本

#### Scenario: YAML 原文展示

- **WHEN** 归属者切换到详情页 YAML tab
- **THEN** 显示该 Secret 的原始 YAML,data 值保持 base64 原文

### Requirement: 值掩码与查看明文揭示

键值 tab 中每个 key 的值 SHALL 默认掩码显示(复用凭据揭示的 credential-redacted 模式),SHALL NOT 直接显示解码后的明文。对 `data` 与 `stringData` 键,`CanOperate` 为 true 的用户 SHALL 可点击该行「查看明文」按钮解码(base64 → UTF-8 文本)并以 credential-viewer 模式显示明文,同时提供「隐藏」按钮恢复掩码;`binaryData` 键 SHALL 只显示字节数与 base64 原文,不提供明文揭示。

每次揭示明文成功 SHALL 写一条审计记录(类别「密钥」、动作「查看明文」,目标描述含集群名、命名空间、Secret 名称与 key 名)。非 `CanOperate` 用户 SHALL NOT 渲染任何揭示按钮。YAML tab 的原文展示 SHALL NOT 写审计。

#### Scenario: 默认掩码

- **WHEN** 归属者打开自己 Secret 的详情页键值 tab
- **THEN** 每个 key 的值以掩码形式显示,不出现明文

#### Scenario: 揭示明文并审计

- **WHEN** 归属者点击 key "password" 的「查看明文」
- **THEN** 该行解码显示明文并可「隐藏」恢复掩码
- **AND** 审计日志新增一条类别「密钥」、动作「查看明文」、目标含 `default/app-secret/password` 的记录

#### Scenario: 非归属者无揭示入口

- **WHEN** Member 打开受限态详情页
- **THEN** 页面无任何「查看明文」按钮

#### Scenario: binaryData 不揭示

- **WHEN** 归属者查看含 `binaryData` 键的 Secret
- **THEN** 该键显示字节数与 base64 原文,无明文揭示按钮

### Requirement: YAML 创建 Secret

系统 SHALL 提供 YAML 创建对话框(标题行「新建 Secret」打开),初始 YAML 经 `IYamlTemplateService` 读取模板 `secret/default`(缺失时回退内置最小骨架),本地 YAML 校验失败提示「YAML 格式错误:...」且对话框保持打开。服务端 SHALL 反序列化 `V1Secret` 并校验 `metadata.namespace`(缺失抛中文校验异常),执行创建目标命名空间黑名单(Admin 不受限)与归属盖章(无条件覆盖用户 YAML 的归属元数据),经 CoreV1 创建;成功后关闭对话框、刷新列表并写审计(类别「密钥」、动作「创建」,目标含命名空间、名称与集群名)。K8s 失败经既有异常翻译为中文业务异常。

#### Scenario: 创建成功

- **WHEN** 用户在集群 "prod-k8s" 经对话框提交合法 YAML(命名空间 default)创建 Secret "app-secret"
- **THEN** Secret 创建成功、列表刷新,审计记录类别「密钥」、动作「创建」、目标含 `default/app-secret` 与集群名

#### Scenario: 缺少命名空间

- **WHEN** 用户提交的 YAML 未指定 metadata.namespace
- **THEN** 服务端抛中文校验异常,对话框保持打开

#### Scenario: Member 创建进保护命名空间被拒

- **WHEN** Member 提交目标命名空间为 `kube-system` 的 Secret YAML
- **THEN** 服务端抛中文校验异常且不调用 K8s 创建 API

### Requirement: YAML 编辑与占位符机制

系统 SHALL 为既有 Secret 提供全高 YAML 编辑页(路由 `/secrets/{ClusterId:int}/{Namespace}/{Name}/yaml`,页面级 `[Authorize]`,实际门在服务层归属判定)。加载回显时,系统 SHALL 把 `data`、`stringData`、`binaryData` 中每个 key 的值替换为 `<REDACTED:KEYNAME>` 占位符(KEYNAME 为该 key 名),SHALL NOT 把解码明文或 base64 原文回显进编辑框。

提交时系统 SHALL 逐 key 处理:值仍为 `<REDACTED:KEYNAME>` 占位符的 key SHALL 保留服务器现值;值与占位符不同的 key SHALL 以用户提交的新值覆盖(含将 key 删除或新增 key)。用户 SHALL NOT 经提交修改对象元数据(name/namespace 与归属元数据以服务器对象为准)。修改成功后写审计(类别「密钥」、动作「修改」)。K8s 冲突等失败经既有异常翻译提示。

#### Scenario: 回显被掩码

- **WHEN** 归属者打开 Secret(含 data key "password")的编辑页
- **THEN** 编辑框中该 key 的值为 `<REDACTED:password>`,不显示原值

#### Scenario: 占位符保留服务器现值

- **WHEN** 归属者提交的 YAML 中 "password" 值仍为 `<REDACTED:password>` 而 "token" 改为新值
- **THEN** 服务器对象 "password" 保持原值、"token" 更新为新值

#### Scenario: 占位符 key 删除生效

- **WHEN** 归属者提交的 YAML 中删除了原有 key "old"
- **THEN** 服务器对象的 "old" 键被移除

#### Scenario: 非归属者编辑被拒

- **WHEN** Member 对他人 Secret 提交 YAML 编辑
- **THEN** 服务端抛 `PermissionException`(中文提示),不调用 K8s 修改 API

### Requirement: Secret 删除

系统 SHALL 提供 Secret 删除入口(列表行/详情页,确认对话框「确认删除 Secret「ns/name」?此操作不可撤销。」),删除 SHALL 经服务端归属判定(Admin 穿透/归属者/无归属 fail-closed 抛 `PermissionException`),成功后刷新列表并写审计(类别「密钥」、动作「删除」,目标含命名空间、名称与集群名);判定失败 SHALL NOT 写审计并记录警告日志。

#### Scenario: 归属者删除成功

- **WHEN** 归属者确认删除自己的 Secret "default/app-secret"
- **THEN** Secret 删除成功、列表刷新,审计记录类别「密钥」、动作「删除」

#### Scenario: 非归属者删除被拒

- **WHEN** Member 确认删除他人 Secret
- **THEN** 服务端抛 `PermissionException` 中文提示,审计日志无本次记录

### Requirement: Secret 归属判定与 CanOperate 投影

Secret 列表与详情 SHALL 由服务端把「当前用户是否可操作该对象」计算进 ViewModel(`CanOperate`,Admin 短路为 true),UI SHALL 只按 `CanOperate` 渲染编辑/删除/揭示入口;Helm 托管的 Secret(携带 `meta.helm.sh/release-*` 注解)SHALL 按既有 Helm 互通判定归属。列表 SHALL NOT 新增创建者列(追溯走审计日志)。

#### Scenario: Member 列表行操作

- **WHEN** Member 打开 Secret 列表
- **THEN** 自己创建的行渲染编辑/删除入口,其他行不渲染

#### Scenario: 列表不新增创建者列

- **WHEN** 用户查看 Secret 列表
- **THEN** 列表不含创建者/所有者列

### Requirement: Drawer 密钥管理入口

侧边导航 SHALL 包含「密钥管理」入口(路由 `/secrets`),位置在「配置管理」之后、「工作负载管理」分组之前,对所有登录用户可见。

#### Scenario: 导航入口可见

- **WHEN** 任意登录用户打开侧边导航
- **THEN** 可以看到「密钥管理」入口并可进入 `/secrets`

# Spec Delta

## MODIFIED Requirements

### Requirement: 工作负载 YAML 新建
系统 SHALL 允许所有登录用户从列表页通过「新建」按钮打开 YAML 编辑对话框,按类型预填 YAML 模板;提交时系统 SHALL 反序列化用户 YAML 并校验 `metadata.namespace` 必填后创建,创建前 SHALL 按 `k8s-resource-ownership` 契约执行归属盖章(无条件覆盖用户 YAML 中的归属元数据)与目标命名空间黑名单校验(Member 禁止创建进 `kube-` 前缀命名空间,`default` 允许)。YAML 解析失败 SHALL 抛出中文校验异常(不直出原始异常);创建成功 SHALL 写入审计(类别"工作负载"、操作"创建")并刷新列表。

#### Scenario: 从模板新建 Deployment
- **WHEN** 登录用户在部署管理页点击「新建」并提交合法 YAML(含 `metadata.namespace`)
- **THEN** 系统在对应命名空间创建 Deployment 并盖章归属,提示成功并刷新列表,写入审计记录

#### Scenario: YAML 缺少命名空间
- **WHEN** 用户提交的 YAML 未包含 `metadata.namespace`
- **THEN** 系统提示中文校验错误,不发起创建请求

#### Scenario: YAML 格式错误
- **WHEN** 用户提交的 YAML 无法反序列化为对应类型
- **THEN** 系统提示"YAML 格式错误:…"且不发起创建请求

#### Scenario: Member 创建进保护命名空间被拒
- **WHEN** Member 提交 YAML、目标命名空间为 `kube-system`
- **THEN** 系统提示中文校验错误,不调用 K8s 创建 API

### Requirement: 工作负载 YAML 编辑
系统 SHALL 允许 Admin 或资源归属者(按 `k8s-resource-ownership` 契约判定,归属者含 Helm 互通情形)在详情页进入 YAML 编辑并保存。保存 SHALL 采用乐观并发安全策略:先读取集群中该对象的最新状态,仅将用户 YAML 中的 `spec` 覆盖到最新对象上(元数据与状态以服务器为准),再以最新 resourceVersion 执行替换——归属元数据保持服务器值,不随编辑改写。保存触发 409 冲突时 SHALL 提示"集群状态已变化,请重试";YAML 解析失败 SHALL 提示"YAML 格式错误:…"。保存成功 SHALL 写入审计(操作"修改")并返回视图态。

#### Scenario: 常规保存
- **WHEN** 归属者编辑自己创建的 Deployment 的 `spec` 并保存
- **THEN** 系统读取最新对象、覆盖 spec 并替换成功,对象的 status 不被触碰,归属元数据不变

#### Scenario: 保存时发生冲突
- **WHEN** 替换请求返回 409 冲突
- **THEN** 系统提示"集群状态已变化,请重试",不覆盖控制器写入的内容

#### Scenario: 非归属者编辑被拒
- **WHEN** Member 通过任意入口尝试编辑他人(或无归属)的工作负载
- **THEN** 服务端抛中文权限异常,不调用 K8s API

### Requirement: 工作负载扩缩容
系统 SHALL 允许 Admin 或资源归属者对 Deployment、StatefulSet、ReplicaSet 执行扩缩容:从列表行或详情页打开扩缩容对话框(当前副本数预填),提交后通过 scale 子资源更新 `spec.replicas`,不读取/替换整个对象;提交前 SHALL 按 `k8s-resource-ownership` 契约完成归属判定。DaemonSet SHALL 不提供扩缩容入口。扩缩容成功 SHALL 写入审计(类别"工作负载"、操作"扩缩容",目标含 `ns/name → n`)并刷新就绪度展示。

#### Scenario: 扩容 Deployment
- **WHEN** 归属者将 Deployment 副本数从 2 调整为 4 并提交
- **THEN** 系统通过 scale 子资源更新副本数,提示成功并刷新列表,审计记录目标包含"→ 4"

#### Scenario: DaemonSet 无扩缩容
- **WHEN** 用户查看守护进程列表或详情
- **THEN** 页面不渲染任何扩缩容入口

#### Scenario: 非归属者扩缩容被拒
- **WHEN** Member 通过任意入口尝试扩缩容他人的 Deployment
- **THEN** 服务端抛中文权限异常,不调用 K8s API

### Requirement: 工作负载滚动重启
系统 SHALL 允许 Admin 或资源归属者对 Deployment、StatefulSet、DaemonSet 执行滚动重启:经轻量确认后,向 `spec.template.metadata.annotations` 打补丁写入 `kubectl.kubernetes.io/restartedAt`(RFC3339 当前时间),触发新滚动;提交前 SHALL 按 `k8s-resource-ownership` 契约完成归属判定。ReplicaSet SHALL 不提供重启入口。重启成功 SHALL 写入审计(类别"工作负载"、操作"重启")并刷新列表,重启后滚动状态 SHALL 呈现"滚动中"直至完成。

#### Scenario: 重启 Deployment
- **WHEN** 归属者确认重启自己创建的 Deployment
- **THEN** 系统写入 restartedAt 注解触发滚动,提示成功,列表行随后显示"滚动中"

#### Scenario: ReplicaSet 无重启
- **WHEN** 用户查看副本集列表或详情
- **THEN** 页面不渲染任何重启入口

### Requirement: 工作负载删除
系统 SHALL 允许 Admin 或资源归属者删除工作负载,删除前 SHALL 经强确认,确认文案 SHALL 明示级联影响(删除工作负载将连带删除其管理的副本与 Pod);删除前 SHALL 按 `k8s-resource-ownership` 契约完成归属判定。删除成功 SHALL 写入审计(操作"删除",目标含 `ns/name`)并刷新列表。

#### Scenario: 删除前确认
- **WHEN** 归属者或 Admin 在列表 ⋯菜单或详情页点击删除
- **THEN** 系统弹出确认对话框,文案包含级联影响说明

#### Scenario: 删除成功
- **WHEN** 归属者或 Admin 确认删除
- **THEN** 系统调用删除 API,提示成功并刷新列表,写入审计记录

#### Scenario: 非归属者删除被拒
- **WHEN** Member 通过任意入口尝试删除他人(或无归属)的工作负载
- **THEN** 服务端抛中文权限异常,不调用 K8s API

### Requirement: 工作负载权限控制
系统 SHALL 对所有登录用户开放工作负载查看(列表、详情、过滤)。写操作按 `k8s-resource-ownership` 契约执行:新建对所有登录用户开放(受命名空间黑名单约束);编辑 YAML、扩缩容、重启、删除仅 Admin 或资源归属者(含 Helm 互通判定)可操作。页面与组件 SHALL 仅按服务端计算的 `CanOperate` 投影渲染写操作入口,SHALL NOT 以固定角色门控替代归属判定。

#### Scenario: Member 查看列表
- **WHEN** Member 用户打开工作负载列表
- **THEN** 页面正常展示列表与过滤,自己创建的资源行有寫入口,其余行仅有查看入口

#### Scenario: Member 尝试写操作
- **WHEN** Member 用户访问无归属资源的写路径
- **THEN** 页面无对应入口;服务端仍强制归属判定并拒绝

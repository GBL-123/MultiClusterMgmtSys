# Spec Delta

## MODIFIED Requirements

### Requirement: 服务 YAML 新建
系统 SHALL 允许所有登录用户从列表页通过「新建」按钮打开 YAML 编辑对话框;对话框 SHALL 提供 Service 类型选择(ClusterIP/NodePort/LoadBalancer/ExternalName),并按所选类型预置对应 YAML 模板(ExternalName 模板无 selector 与端口,含 externalName;NodePort/LoadBalancer 模板的 nodePort 行以注释说明可省略并由集群自动分配)。创建模板 SHALL 外置为配置文件(`wwwroot/templates/{资源}/{类型}.yaml`),不写死在组件代码中;模板文件缺失或读取失败时 SHALL 回退最小骨架(附缺失提示注释)并记录警告日志,不阻塞对话框打开。提交时系统 SHALL 反序列化用户 YAML 并校验 `metadata.namespace` 必填后创建,创建前 SHALL 按 `k8s-resource-ownership` 契约执行归属盖章(无条件覆盖用户 YAML 中的归属元数据)与目标命名空间黑名单校验(Member 禁止创建进 `kube-` 前缀命名空间,`default` 允许)。YAML 解析失败 SHALL 抛出中文校验异常(不直出原始异常);创建成功 SHALL 写入审计(类别"服务"、操作"创建")并刷新列表。

#### Scenario: 按类型选择模板
- **WHEN** 登录用户在创建对话框中选择 LoadBalancer 类型
- **THEN** YAML 编辑框内容切换为 `type: LoadBalancer` 的模板

#### Scenario: 从模板新建服务
- **WHEN** 登录用户点击「新建」并提交合法 YAML(含 `metadata.namespace`)
- **THEN** 系统在对应命名空间创建 Service 并盖章归属,提示成功并刷新列表,写入审计记录

#### Scenario: YAML 缺少命名空间
- **WHEN** 用户提交的 YAML 未指定 `metadata.namespace`
- **THEN** 系统提示中文校验错误,不调用 K8s API

#### Scenario: 模板文件缺失仍可打开对话框
- **WHEN** `wwwroot/templates/service/clusterip.yaml` 不存在且用户打开创建对话框
- **THEN** YAML 编辑框显示附缺失提示的最小骨架,服务日志记录警告,不抛出用户可见错误

#### Scenario: Member 创建进保护命名空间被拒
- **WHEN** Member 提交 YAML、目标命名空间为 `kube-system`
- **THEN** 系统提示中文校验错误,不调用 K8s 创建 API

### Requirement: 服务 YAML 编辑与不可变字段守卫
系统 SHALL 允许 Admin 或资源归属者(按 `k8s-resource-ownership` 契约判定,含 Helm 互通情形)对既有服务进行 YAML 编辑:系统读取现有对象后,若用户 YAML 中 `spec.clusterIP`/`spec.clusterIPs`/`spec.ipFamilies` 与现有值不同,SHALL 在调用 K8s API 之前抛出中文校验异常(提示该字段不可变,如需更换请删除后重建);用户 YAML 省略这些字段时 SHALL 以现有值补齐。提交 SHALL 携带现有 `resourceVersion`/`uid` 并保留服务状态(status)与归属元数据(服务器值)后整体替换。编辑成功 SHALL 写入审计(类别"服务"、操作"修改")并刷新详情。

#### Scenario: 修改可变字段成功
- **WHEN** 归属者将服务的 selector 与端口修改后提交(不可变字段未变)
- **THEN** 系统替换成功,新 selector 与端口生效,写入审计记录,归属元数据不变

#### Scenario: 修改 clusterIP 被拒
- **WHEN** 用户将 YAML 中 clusterIP 改为另一 IP 后提交
- **THEN** 系统提示"clusterIP 为不可变字段,如需更换请删除后重建"类中文错误,不调用 K8s API

#### Scenario: 省略不可变字段
- **WHEN** 用户提交的 YAML 未包含 `spec.clusterIP`
- **THEN** 系统以现有 clusterIP 补齐并替换成功

#### Scenario: 非归属者编辑被拒
- **WHEN** Member 通过任意入口尝试编辑他人(或无归属)的服务
- **THEN** 服务端抛中文权限异常,不调用 K8s API

### Requirement: 服务删除
系统 SHALL 允许 Admin 或资源归属者(按 `k8s-resource-ownership` 契约判定,含 Helm 互通情形)从列表或详情页删除服务,删除前 SHALL 弹出确认对话框(含命名空间与名称)并完成归属判定;删除成功 SHALL 写入审计(类别"服务"、操作"删除",目标含命名空间、名称与集群名)并刷新列表。非 Admin 的非归属资源不渲染删除入口,服务端亦强制拒绝。

#### Scenario: 确认后删除
- **WHEN** 归属者或 Admin 确认删除 `default/nginx-svc`
- **THEN** 系统调用 K8s 删除该服务,提示成功,刷新列表,写入审计记录

#### Scenario: Member 无删除入口
- **WHEN** Member 用户浏览服务列表与详情页
- **THEN** 自己创建的服务渲染删除/新建/编辑入口,其余资源不渲染

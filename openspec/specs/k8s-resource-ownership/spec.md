# k8s-resource-ownership

## Purpose

原生 Kubernetes 资源(工作负载/ConfigMap/Service)的对象级归属契约:以 K8s label 记录创建者身份,创建后归创建者全生命周期管理,Admin 穿透操作一切,无归属资源 fail-closed 仅 Admin 可操作。跨切面规则由本契约统一持有,各资源页面的 spec 引用本契约的判定语义。

## Requirements

### Requirement: 归属载体与创建盖章

系统 SHALL 以 label `mcms.ms/owner-uid`(值为创建者账号 Id)与注解 `mcms.ms/owner-name`(值为创建者用户名快照)记录资源的创建者。受管资源 SHALL 覆盖工作负载(Deployment/StatefulSet/DaemonSet/ReplicaSet)、ConfigMap、Service、Secret 与 PersistentVolumeClaim。系统内创建成功时 SHALL 无条件写入这两项归属元数据(覆盖用户 YAML 中自带的同名/同键归属元数据,防止伪造);Admin 经系统创建的资源 SHALL 同样盖章。归属元数据随对象自身存续:对象被删除则归属消失,系统 SHALL NOT 维护任何独立的归属数据库结构。

#### Scenario: Member 创建即盖章

- **WHEN** Member 用户经系统在非保护命名空间创建任一受管资源成功
- **THEN** 该对象携带 `mcms.ms/owner-uid: <当前账号 Id>` 与 `mcms.ms/owner-name: <当前账号名>`

#### Scenario: 用户 YAML 伪造归属被覆盖

- **WHEN** 用户提交的 YAML 中已写有 `mcms.ms/owner-uid: 8`(冒充他人)
- **THEN** 实际创建的对象携带服务端写入的 `mcms.ms/owner-uid: <当前账号 Id>`,用户提供的归属值不生效

#### Scenario: Admin 创建同样盖章

- **WHEN** Admin 用户经系统创建资源成功
- **THEN** 该对象同样携带归属元数据(指向 Admin 自身)

#### Scenario: 归属没有数据库结构

- **WHEN** 检查本 change 的 schema 影响
- **THEN** 数据库无新增表、无字段变更,不需要删库重建

#### Scenario: Secret 创建同样盖章

- **WHEN** 用户经系统创建 Secret 成功
- **THEN** 该 Secret 对象同样携带归属 label 与注解,归属判定与编辑占位符揭示等页面行为的准入均以此为准

#### Scenario: PersistentVolumeClaim 创建同样盖章

- **WHEN** 用户经系统创建 PVC 成功
- **THEN** 该 PVC 对象同样携带归属 label 与注解,删除断言(仅归属者或 Admin)以此判定

### Requirement: 变更前归属判定

系统 SHALL 在服务层对受管资源的全部写操作(新建/编辑 YAML/扩缩容/重启/删除;新建另有黑名单规则)强制归属判定:Admin SHALL 放行一切;非 Admin 用户 SHALL 仅可操作 `mcms.ms/owner-uid` 与当前账号 Id 相等的对象;其余(无归属、归属他人、无身份)SHALL 抛 `PermissionException` 并携带中文用户消息,不调用 K8s 变更 API。判定必须服务端执行,SHALL NOT 仅依赖界面隐藏。既有的 spec 覆盖/不可变字段守卫/resources 语义检查不变。

#### Scenario: Member 编辑自己的资源

- **WHEN** Member 提交自己创建的 Deployment 的 YAML 编辑
- **THEN** 系统执行 spec 覆盖逻辑,编辑成功并写审计

#### Scenario: Member 操作他人资源被拒

- **WHEN** Member 通过任意入口尝试修改、扩缩容、重启或删除他人创建(label 指向别人)的资源
- **THEN** 服务端抛 `PermissionException`,提示中文权限消息,不调用 K8s 变更 API

#### Scenario: Member 操作无归属资源被拒

- **WHEN** Member 尝试操作没有归属 label 的资源(存量资源或系统外创建)
- **THEN** 服务端抛 `PermissionException`,仅 Admin 可操作

#### Scenario: Admin 穿透一切

- **WHEN** Admin 对任意资源(有归属、无归属)执行任意写操作
- **THEN** 操作通过校验并成功执行

#### Scenario: 无登录身份被拒

- **WHEN** 服务在无法从当前上下文获取登录账号时收到写操作请求
- **THEN** 抛 `PermissionException`(与无法判定归属一致,fail-closed)

#### Scenario: 归属在读路径不被破坏

- **WHEN** Admin 对 Member 创建的资源执行 YAML 编辑(本仓既有编辑策略仅覆盖 spec/data、保留 metadata)
- **THEN** 对象的归属 label 与注解保持不变,该 Member 仍可继续操作自己的资源

### Requirement: 创建目标命名空间黑名单

Member 经系统创建受管资源时,目标命名空间 SHALL 允许 `default`,SHALL 拒绝 `kube-` 前缀的受保护命名空间(不调用 K8s API,抛中文校验异常);其余命名空间 SHALL 允许。Admin 创建不受黑名单限制。

#### Scenario: Member 创建进 kube-system 被拒

- **WHEN** Member 提交 YAML 目标命名空间为 `kube-system`
- **THEN** 系统抛中文校验异常且不调用 K8s API

#### Scenario: Member 创建进 default 放行

- **WHEN** Member 提交 YAML 目标命名空间为 `default`
- **THEN** 系统正常执行创建与盖章

### Requirement: Helm 托管对象的互通判定

对象携带 Helm 托管标识(标准 `meta.helm.sh/release-name` 与 `meta.helm.sh/release-namespace` 注解,`app.kubernetes.io/managed-by: Helm` label)时,资源页的对象级归属判定 SHALL 改查既有 `HelmReleaseOwnership` 表:release 归属记录的安装者账号 Id 与当前用户一致当前用户 SHALL 可操作该对象,否则按无归属规则处理。资源页互通判定 SHALL 不做 release revision 比对(降级启发式仍是 Helm 应用的职责),且判定结果 SHALL NOT 回写 Helm 模块的状态。

#### Scenario: Member 操作自己安装的 release 的 Deployment

- **WHEN** Member 在 `/helm` 安装 nginx release 并到部署管理页查看其下 Deployment
- **THEN** 该 Deployment 的操作按钮对当前 Member 可用,与在 `/helm` 页面的权限一致

#### Scenario: Member 操作他人 release 的 Deployment 被拒

- **WHEN** Member 查看他人(或无主 release)的 Helm 托管 Deployment
- **THEN** 资源页按无归属规则拒绝该 Member 的写操作入口与服务端调用

### Requirement: 操作权限投影

列表与详情的读路径 SHALL 把「当前用户是否可操作该对象」计算进 ViewModel(`CanOperate`,Admin 短路为 true),服务端计算,UI 只按 `CanOperate` 渲染写操作入口。资源列表 SHALL NOT 新增创建者列(追溯走审计日志,与 Helm 归属的展示口径一致)。新建入口 SHALL 对所有登录用户渲染(集群不可达时仍禁用)。

#### Scenario: Member 列表行操作按钮

- **WHEN** Member 打开部署列表
- **THEN** 自己创建的资源行渲染编辑/扩缩容/重启/删除入口,其他资源行不渲染

#### Scenario: 列表不新增创建者列

- **WHEN** 用户查看任一受管资源列表
- **THEN** 列表不含创建者/所有者列

#### Scenario: 新建入口对所有登录用户可见

- **WHEN** Member 打开可达集群的工作负载列表页
- **THEN** 「新建」按钮可见且可打开创建对话框(Member 创建进保护命名空间由服务端黑名单拒绝)

### Requirement: 违规与审计口径

归属校验通过的写操作成功后按既有口径写审计(操作者取登录上下文);归属判定失败的请求 SHALL NOT 写审计,SHALL 记录警告日志(含集群、命名空间、名称与当前用户,不含凭据),与 Helm 归属的失败姿态一致。

#### Scenario: 拒绝不写审计

- **WHEN** Member 尝试操作他人资源被服务端拒绝
- **THEN** 审计日志无本次记录,应用日志出现一条警告

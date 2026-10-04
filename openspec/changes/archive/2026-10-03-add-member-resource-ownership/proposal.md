# Member 自建资源归属管理

## Why

系统当前对原生 K8s 资源的权限模型是「Admin 全部写操作、Member 纯只读」:工作负载/ConfigMap/Service 的新建、编辑、扩缩容、重启、删除全部以 Admin 角色门控,Member 只能看不能做。这条规则把 Member 完全排除在部署工作之外——而 `add-helm-management` 刚刚验证了一种中间态:member 可以创建自己的资源、只操作自己的资源(归属记账 + 服务端强制 + fail-closed)。该模式已经过一轮实现与评审,把同样的语义推广到原生资源,不需要再发明模型,只需要把归属载体从数据库表换成 K8s 原生 label。

## What Changes

- **对象级归属模型**:`mcms.ms/owner-uid` label(值为创建者账号 Id,恒为合法 label 值)+ `mcms.ms/owner-name` 注解(用户名快照,仅展示)。创建成功即盖章;Admin 创建的同样盖章(统一规则,为日后转让留跳板)。
- **Member 自助写生命周期**:Member 可在有建路径的资源上从 YAML 新建;对自己创建的 resource 拥有完整生命周期写权限(编辑 YAML/扩缩容/重启/删除);对无归属资源(存量、CI 系统外创建、Helm 无主 release)不可操作并抛 `PermissionException`(fail-closed);Admin 穿透一切。
- **归属载体跟随对象**:不建表、不删库重建;归属随对象创建/删除自然生灭,外部删除即归属消失,外部重建无 label 即无主——无需 Helm 采用的 revision 启发式补丁。
- **权限强制点迁移**:K8s 写操作的服务层方法今天不做任何角色/归属检查(仅在 UI 层遮挡)。本 change 把强制挪进服务层——Mutation 前先读对象校验归属,UI 改为 `CanOperate` 投影(沿用 Helm 的 D8 模式:服务端是唯一可信来源)。
- **防伪造**:创建时归属 label/注解由服务端无条件覆盖,用户 YAML 自带的 `mcms.ms/*` 归属元数据不生效。
- **命名空间创建目标黑名单**:Member 创建资源的目标命名空间禁止 `kube-*`(复用 `IsProtected` 语义),`default` 允许。
- **Helm 互通**:资源页读到带 Helm 托管标识(`meta.helm.sh/release-*` 注解)的对象时,归属改查 `HelmReleaseOwnership` 表按 owner uid 判定——member 在 `/helm` 安装的 release,其下资源在部署管理等资源页同样可操作。资源页互通只按 uid 判,revision 降级启发式仍是 `/helm` 页的职责。
- **存量资源不迁移**:没有归属 label 的既有资源维持仅 Admin 可操作,不提供认领/转让入口(需要时后续 change 加)。

## Capabilities

### New Capabilities

- `k8s-resource-ownership`: 原生 K8s 资源的对象级归属契约——归属载体(label/注解键名与取值)、创建盖章与防伪造规则、Mutation 前置归属判定(Admin 穿透/owner 匹配/无主 fail-closed)、创建目标命名空间黑名单、Helm 托管对象的互通判定、CanOperate 服务端投影、存量资源与审计口径。

### Modified Capabilities

- `workload-management`: 权限控制要求从「全部写操作以 Admin 角色门控」改为归属模型;列表/详情的写操作入口由 `CanOperate` 驱动;YAML 新建/编辑、扩缩容、重启、删除要求中的 Admin 限定改为「Admin 或有归属者」;新增 Member 创建场景与 CMS 归属盖章要求。
- `configmaps-page`: 新建按钮/行操作/编辑 YAML 入口的 Admin 角色门控改为 `CanOperate` 投影;新建对话框与保存路径授权改为归属校验;编辑页 `[Authorize(Roles="Admin")]` 页面属性放宽为登录用户 + 服务端归属校验。
- `service-management`: 服务 YAML 新建/编辑/删除要求中的 Admin 限定改为归属模型,删除"x 仅 Admin"的遮挡表述。

## Impact

- **代码**:
  - 新增:`Application/Common/Ownership/`(归属常量、创建盖章纯函数、归属判定纯函数);`Web/Components/Common` 或共享组件里供各页复用的 `CanOperate` 投影辅助。
  - 修改:`WorkloadService`(Create/Update/Scale/Restart/Delete 系列加身份校验 + 归属检查,读映射加 CanOperate)、`ConfigMapService`、`SvcService`(同款)、四个列表 VM 与详情 VM(+`CanOperate`)、`WorkloadListView/WorkloadListTable/WorkloadDetailToolbar`、`ConfigMaps` 页面 + `ConfigMapListTable/ConfigMapDetailToolbar` + `EditConfigMapYaml` 页面属性、`Svcs` 页面 + `SvcListTable/SvcDetailToolbar` + `EditSvcYaml` 页面属性;`CreateXxxDialog` 系列的提交路径不改签名。
  - 不动:命名空间管理(仍 Admin-only)、Pod 只读能力、Secret(未纳入管理)、`/helm` 模块自身语义、审计枚举(现有类别/操作够用)、DbContext(无 schema 变更)。
- **测试**:服务层五类身份矩阵(Admin/本人/他人/无主/无身份)×(create/update/scale/restart/delete)×(三资源族)、伪造 label 覆盖、kube-* 黑名单、Helm 互通分支;bUnit 的 `CanOperate` 按钮分支与 Member 可见的新建入口。
- **文档/部署**:无 schema 变更、无删库重建;AGENTS.md 补记归属模型与五个受影响 spec 的口径。

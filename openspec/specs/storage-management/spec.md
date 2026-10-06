# storage-management

## Purpose

提供集群持久化存储的可见性与最小管控:PersistentVolumeClaim 支持命名空间级列表、YAML 创建、详情(含挂载 Pod)与删除,归属规则与权限照工作负载/ConfigMap/Secret 同款;PersistentVolume 与 StorageClass 提供只读浏览与详情,不发写请求、不产生审计。
## Requirements

### Requirement: 存储页导航与路由

系统 SHALL 在主导航 Drawer 提供「存储管理」导航组(置于「密钥管理」之后),含三个入口:持久卷声明(/storage/claims)、持久卷(/storage/volumes)、存储类(/storage/classes)。每个入口 SHALL 支持集群路由:/storage/claims/{clusterId}、/storage/volumes/{clusterId}、/storage/classes/{clusterId};持久卷声明 SHALL 额外提供详情路由 /storage/claims/{clusterId}/{namespace}/{name};持久卷 SHALL 提供只读详情路由 /storage/volumes/{clusterId}/{volumeName}。列表页 SHALL 复用共享集群选择侧栏,侧栏行为与集群页一致。

#### Scenario: 三入口可导航

- **WHEN** 用户点击侧栏「存储管理」组内的任一入口
- **THEN** 页面路由到对应 /storage 子页,集群上下文与共享侧栏选中状态保持

#### Scenario: 未选集群提示选择

- **WHEN** 用户直接进入 /storage 子页且未选集群
- **THEN** 页面显示空态「请从左侧选择一个集群」;集群不存在时显示「未找到该集群」与返回入口

### Requirement: PersistentVolumeClaim 列表

页面 SHALL 对已选集群列出 PersistentVolumeClaim(支持全命名空间或指定命名空间读取):列包含名称(可进详情)、命名空间、状态(Bound/Pending/Lost,双语)、容量、存储类、创建时间与操作;名称 SHALL 支持客户端过滤并可重置。列表 SHALL NOT 展示创建者列。操作列的写操作(删除)SHALL 按 CanOperate 投影条件渲染,查看入口对全员可见。

#### Scenario: 列表渲染

- **WHEN** Admin 选择可达集群打开持久卷声明列表
- **THEN** 表格渲染名称/命名空间/状态/容量/存储类/创建时间,行操作含详情与删除

#### Scenario: 集群不可达时禁用新建

- **WHEN** 集群不可达
- **THEN** 页面显示「集群不可达，无法获取持久卷声明」提示,新建按钮仍渲染但禁用,表格无数据行

### Requirement: PersistentVolumeClaim 创建

系统 SHALL 通过对话框以 YAML 创建 PersistentVolumeClaim:入口为模板(wwwroot/templates/pvc/default.yaml,缺失时回退内置最小骨架),提交前校验 YAML;kind 非 PersistentVolumeClaim SHALL 拒绝。命名空间黑名单与归属盖章同受管资源规则(Member 禁止在 kube- 前缀命名空间创建,default 允许),成功后写入审计。校验或服务失败 SHALL 判定原因显示且对话框保持打开。

#### Scenario: 模板创建成功

- **WHEN** 用户以默认模板填名称/命名空间提交（合法 PVC）
- **THEN** 系统创建成功、刷新列表、提示成功、对话框关闭并写入审计(类别"存储"、操作"创建")

#### Scenario: 伪造或非法提交被拒

- **WHEN** 用户提交 kind 不为 PersistentVolumeClaim 的 YAML,或提交目标为 kube-system（非 Admin）
- **THEN** 系统显示对应中文错误且对话框保持打开,不创建资源

### Requirement: PersistentVolumeClaim 详情与挂载 Pod

详情页 SHALL 展示名称/命名空间/UID/容量/访问模式/存储类/状态/创建时间、只读 YAML 视图与**挂载此 PVC 的 Pod 列表**(在本命名空间 Pod 卷中引用该 ClaimName; 列 Pod 名称/状态/开始时间,空态提示「无 Pod 挂载此卷」)。挂载信息对全员可见;详情页 SHALL NOT 提供 YAML 编辑入口(spec 几乎不可变)。

#### Scenario: 挂载 Pod 展示

- **WHEN** 用户打开被某 Pod 引用的 PVC 详情
- **THEN** 挂载 Pod 列表显示该 Pod 的名称/状态/开始时间

#### Scenario: 无挂载

- **WHEN** 该 PVC 未被 Pod 引用
- **THEN** 挂载 Pod 区显示空态

### Requirement: PersistentVolumeClaim 删除

删除 SHALL 位于详情页与列表行,经确认对话框二次确认,仅对 CanOperate 用户启用;删除成功写审计(类别"存储"、操作"删除"),并将对象从列表移除。

#### Scenario: 归属者删除

- **WHEN** 归属者点击删除并确认
- **THEN** 系统删除成功、提示成功、刷新列表且写入审计

#### Scenario: 非归属者不可见按钮

- **WHEN** Member 打开非本人 PVC 的列表/详情
- **THEN** 删除按钮不渲染;直接调用服务被 PermissionException 拒绝且不写审计

### Requirement: PersistentVolume 只读浏览与详情

页面 SHALL 对已选集群列出 PersistentVolume:列含名称(进详情)、状态(Phase 双语)、容量、回收策略、绑定 claim(claimRef 命名空间/名称)、存储类;名称点击进入**只读详情页**(镜像节点详情布局):字段行(Phase/容量/回收策略/绑定 claim/存储类/来源 source 展开)、只读 YAML 视图。持久卷 SHALL NOT 提供任何写操作入口,SHALL NOT 写入审计。

#### Scenario: 只读详情字段

- **WHEN** 用户点击某 PV 名称进入详情
- **THEN** 展示 Phase/容量/回收策略/绑定 claim/存储类与只读 YAML 视图,无删除或编辑按钮

#### Scenario: 只读不写审计

- **WHEN** 用户浏览 PV 列表与详情
- **THEN** 审计日志无新增记录

### Requirement: StorageClass 只读列表

页面 SHALL 对已选集群列出 StorageClass:列含名称、Provisioner、回收策略(Reclaim Policy)、绑定模式、允许卷扩展;这是纯浏览视图,SHALL NOT 提供详情页/写操作/审计。

#### Scenario: SC 列表渲染

- **WHEN** 用户选择可达集群打开存储类列表
- **THEN** 表格渲染名称/Provisioner/回收策略/绑定模式/允许卷扩展

### Requirement: 审计与权限边界

存储域内仅 PersistentVolumeClaim 的创建、删除 SHALL 写审计(经 audit-log 契约的存储类目);PersistentVolume/StorageClass 只读浏览、PVC 列表与详情读取 SHALL NOT 写审计。PVC 全部写路径(创建/删除)SHALL 经服务层归属判定(RequireCreator/RequireOperate 语义同 Secret),拒绝时记 LogWarning 并无审计写入。

#### Scenario: 只写 PVC 产生审计

- **WHEN** 用户依次进行:浏览 PV 列表、查看 PVC 详情、创建 PVC、删除 PVC
- **THEN** 仅创建与删除各产生一条审计记录

# Spec Delta

## MODIFIED Requirements

### Requirement: 归属载体与创建盖章

系统 SHALL 以 label `mcms.ms/owner-uid`(值为创建者账号 Id)与注解 `mcms.ms/owner-name`(值为创建者用户名快照)记录资源的创建者。受管资源 SHALL 覆盖工作负载(Deployment/StatefulSet/DaemonSet/ReplicaSet)、ConfigMap、Service 与 Secret。系统内创建成功时 SHALL 无条件写入这两项归属元数据(覆盖用户 YAML 中自带的同名/同键归属元数据,防止伪造);Admin 经系统创建的资源 SHALL 同样盖章。归属元数据随对象自身存续:对象被删除则归属消失,系统 SHALL NOT 维护任何独立的归属数据库结构。

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

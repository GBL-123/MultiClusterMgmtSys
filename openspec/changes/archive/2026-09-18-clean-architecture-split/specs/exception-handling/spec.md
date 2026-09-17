# exception-handling Delta

## MODIFIED Requirements

### Requirement: 业务异常层次
系统 SHALL 提供 `MultiClusterMgmtSys.Domain.Exceptions` 下的业务异常层次:`BusinessException`(抽象基类,携带中文 `UserMessage` 属性)及子类 `NotFoundException` / `ConflictException` / `ValidationException` / `PermissionException`。业务异常的 `UserMessage` SHALL 是可直接展示给用户的中文文案。

#### Scenario: 抛业务异常
- **WHEN** 服务层发现资源不存在并抛出 `NotFoundException`
- **THEN** 该异常携带中文用户文案(如「集群 5 不存在」),且继承 `BusinessException`

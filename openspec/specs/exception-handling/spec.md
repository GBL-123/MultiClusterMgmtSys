# exception-handling

## Purpose

Define the contract for business exception handling across service and UI layers. Services translate Kubernetes client errors and throw a typed business exception hierarchy under `MultiClusterMgmtSys.Common.Exceptions` carrying Chinese user-facing messages; the UI presents exceptions uniformly through an injected `ExceptionPresenter` without leaking underlying `ex.Message` text. Logging records warnings for translated business exceptions and errors for unexpected ones, while audit-log write failures stay silent.

## Requirements

### Requirement: 业务异常层次
系统 SHALL 提供 `MultiClusterMgmtSys.Common.Exceptions` 下的业务异常层次:`BusinessException`(抽象基类,携带中文 `UserMessage` 属性)及子类 `NotFoundException` / `ConflictException` / `ValidationException` / `PermissionException`。业务异常的 `UserMessage` SHALL 是可直接展示给用户的中文文案。

#### Scenario: 抛业务异常
- **WHEN** 服务层发现资源不存在并抛出 `NotFoundException`
- **THEN** 该异常携带中文用户文案(如「集群 5 不存在」),且继承 `BusinessException`

### Requirement: K8s 异常翻译
系统 SHALL 将 Kubernetes 客户端异常(`k8s.KubernetesException` 携带 `Status.Code`、`k8s.Autorest.HttpOperationException` 携带 `Response.StatusCode`)翻译为业务异常后再向 UI 层抛出:404→`NotFoundException`、409→`ConflictException`、403→`PermissionException`、400→`ValidationException`(优先取 API 返回消息)、超时/连接失败→`ClusterUnreachableException`(或等效的集群不可达业务异常)。非上述状态(5xx、未知)SHALL NOT 冒充业务异常。

#### Scenario: 409 冲突翻译
- **WHEN** K8s API 返回 409 Conflict
- **THEN** 服务层抛出 `ConflictException` 并携带「资源已被他人修改,请刷新后重试」

#### Scenario: 集群不可达
- **WHEN** 调用 K8s API 发生超时或连接失败
- **THEN** 服务层抛出集群不可达业务异常,携带「集群连接失败或超时」类中文文案

#### Scenario: 未映射状态码
- **WHEN** K8s API 返回 5xx 或未知状态码
- **THEN** 不翻译为业务异常,交由上层按系统异常处理

### Requirement: 服务层日志
服务层 SHALL 在抛出或翻译业务异常时记录日志用于定位:`logger.LogWarning`(含操作名与上下文标识如 clusterId/name);未预期异常 SHALL 以 `logger.LogError` 记录。既有"优雅降级"分支(如集群详情页节点加载失败置 `IsReachable=false`)SHALL 保持静默降级,不向用户弹提示。

#### Scenario: 业务异常被记录
- **WHEN** 服务层抛出一个已翻译的业务异常
- **THEN** 日志中记录 Warning 级别条目,含操作与受影响资源标识

#### Scenario: 降级分支不打扰用户
- **WHEN** 集群在线但节点列表加载失败
- **THEN** 详情页显示集群不可达状态,不弹出错误提示

### Requirement: UI 统一用户提示
UI 层 SHALL 通过注入的 `ExceptionPresenter` 统一呈现异常:`HandleAsync(ex, fallbackMessage)` 对 `BusinessException` 显示其 `UserMessage`,`ConflictException` 用 Warning 级别、其余业务异常用 Error 级别;非业务异常 SHALL 显示通用文案「{fallbackMessage}失败,请稍后重试」且不向用户暴露 `ex.Message`。UI 组件 SHALL NOT 再直接拼接 `ex.Message` 到提示。

#### Scenario: 业务异常提示
- **WHEN** 操作抛出 `NotFoundException`
- **THEN** Snackbar 显示该异常的 `UserMessage`

#### Scenario: 冲突提示级别
- **WHEN** 操作抛出 `ConflictException`
- **THEN** Snackbar 以 Warning 级别显示冲突文案

#### Scenario: 系统异常不泄漏细节
- **WHEN** 操作抛出未预期系统异常
- **THEN** Snackbar 显示「{操作}失败,请稍后重试」,不包含异常消息或堆栈

### Requirement: 存量 throw 迁移
系统 SHALL 移除向 UI 暴露英文/底层消息的存量异常抛出:原有 `InvalidOperationException("Cluster {id} not found")` 等 SHALL 迁移为对应业务异常并使用中文文案。迁移后 SHALL NOT 存在向用户展示 `ex.Message` 的 catch。

#### Scenario: 迁移后无英文直出
- **WHEN** 资源不存在触发旧式 throw 路径
- **THEN** 用户看到中文业务文案而非英文「not found」消息

### Requirement: 审计日志静默失败
`AuditService.LogAsync` 的写失败 SHALL 保持静默(catch 后 `LogWarning`),不向用户抛异常或弹提示。

#### Scenario: 审计写失败
- **WHEN** 审计日志写入数据库失败
- **THEN** 操作流程不中断,仅记录 Warning 日志
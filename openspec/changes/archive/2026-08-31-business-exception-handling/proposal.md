## Why

当前异常是"裸奔"的:服务层混用 `InvalidOperationException`(英文消息,如 "Cluster not found")和原始 `KubernetesClientException`(含 URL/堆栈/英文状态),UI 层 36 处 `catch (Exception ex)` 直接把 `ex.Message` 弹给用户。结果是英文/底层错误泄漏、业务冲突(409)靠 `Contains("409")` 字符串匹配判断、业务失败与系统失败无法区分、每处 catch 各写各的提示。

## What Changes

- 新增业务异常层次 `Common/Exceptions/`:`BusinessException`(基类,携带中文 `UserMessage`)及 `NotFoundException` / `ConflictException` / `ValidationException` / `PermissionException`(如需要再加 `ClusterUnreachableException`)。
- 三个调 K8s 的服务(`ClusterService` / `ClusterNodeService` / `ConfigMapService`,约 8 个方法)每个调用点包 try/catch:**`KubernetesClientException` 按状态码翻译为业务异常**(404→NotFound、409→Conflict、403→Permission、400→Validation、超时/连接失败→ClusterUnreachable),并在服务层记录 `LogWarning`/`LogError` 用于定位。
- **BREAKING**: 存量 19 处 `throw`(InvalidOperationException/ArgumentException)全量迁移为对应业务异常,消息改中文(如「集群 5 不存在」)。UI 层不再直接显示 `ex.Message`。
- 新增 `ExceptionPresenter`(Scoped 注入):UI 36 处 catch 收敛为一行 `HandleAsync(ex, "操作前缀")`——业务异常显示其 `UserMessage`(Conflict 等用 `Severity.Warning`,其余 `Error`);非业务异常显示通用文案「操作失败,请稍后重试」且不泄漏技术细节。
- `AuditService.LogAsync` 保持现有静默 catch + LogWarning(审计写失败不打扰用户)。

## Capabilities

### New Capabilities

- `exception-handling`: 业务异常类型、K8s 异常→业务异常翻译契约、服务层日志约定、UI 层 `ExceptionPresenter` 的用户提示规则(按异常类型的 Severity/消息映射)。

### Modified Capabilities

<!-- 现有 spec 均属功能行为层;本变更不改既有功能规格,只统一错误呈现。 -->

## Impact

- 新增 `Common/Exceptions/`(异常类)与 `Services/ExceptionPresenter.cs`(或等效位置)
- `Services/ClusterService.cs`、`Services/ClusterNodeService.cs`、`Services/ConfigMapService.cs`:K8s 调用包装 + throw 迁移
- 其他服务(`AccountService`、`GroupService` 等)的存量 throw 迁移为业务异常
- UI 组件 36 处 catch 改为调用 `ExceptionPresenter`(集群/节点/ConfigMap/账号/审计/Profile/对话框等)
- `Program.cs`:`ExceptionPresenter` 注册为 scoped
- 不涉及数据库、K8s 数据结构变更;无新外部依赖
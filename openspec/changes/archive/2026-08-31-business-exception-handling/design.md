## Context

当前 `Services/` 抛 `InvalidOperationException`(英文/混合消息)与原始 `KubernetesClientException`;UI 36 处 `catch (Exception ex) { Snackbar.Add($"失败: {ex.Message}", Error) }`。K8s 调用集中在 `ClusterService` / `ClusterNodeService` / `ConfigMapService`(约 8 个方法,`new Kubernetes(config)` + `client.CoreV1.*`),异常来自 `KubernetesClientException`(携带 `HttpStatusCode`/`Status`/`Reason`/`Message`)。`EditConfigMapYaml.razor:125` 已出现 `Contains("409")` 字符串猜语义的脆弱写法。UI 全中文;无测试项目;`dotnet build` 通过。

## Goals / Non-Goals

**Goals:**
- 建立业务异常层次,携带可直接展示的中文 `UserMessage`
- 服务层把 K8s 异常翻译为业务异常,并记录日志(定位用)
- UI 层统一经 `ExceptionPresenter` 提示用户,业务失败与系统失败可区分
- 存量 throw 全量迁移,UI 不再直出 `ex.Message`

**Non-Goals:**
- 不引入 Result/Union 返回模式(改动面过大,与现有风格不符)
- 不新增全局 ErrorBoundary 兜底(操作失败走 Snackbar;整页崩溃兜底属另一变更)
- 不改 `AuditService.LogAsync` 的静默失败语义
- 不做字段级校验框架引入(现有 DataAnnotations 校验保留)

## Decisions

**D1. 异常层次(Common/Exceptions/)**
```
BusinessException (abstract, UserMessage 中文)
├── NotFoundException
├── ConflictException
├── ValidationException
└── PermissionException
(如需要) ClusterUnreachableException : BusinessException
```
每个异常构造接受中文 `UserMessage`。命名空间 `MultiClusterMgmtSys.Common.Exceptions`(符合 AGENTS.md 的 Common/** → 物理路径规则)。
- 备选:放 `Services/` —— 否,异常是跨层契约,Common 更合适。

**D2. K8s 异常翻译:静态 helper + 每方法包装**
提供 `Common/Exceptions/K8sExceptionMapper.Translate(Exception ex, string operation)`:
```
k8s.KubernetesException(Status.Code, KubernetesClient 19 携带状态码的主要异常类型)
  404 → NotFoundException(「{operation}:资源不存在或已被删除」)
  409 → ConflictException(「{operation}:资源已被他人修改,请刷新后重试」)
  403 → PermissionException(「没有权限执行该操作」)
  400 → ValidationException(优先取 Status.Message,回退通用文案)
k8s.Autorest.HttpOperationException(Response.StatusCode,旧式 Autorest 包装)
  同上状态码映射
TaskCanceledException/OperationCanceledException/HttpRequestException
  → ClusterUnreachableException(「集群连接失败或超时,请稍后重试」)
其他(含无状态码的 KubernetesClientException、5xx、未知状态)→ 原样返回,当系统异常
```
注:KubernetesClient 19 的 `KubernetesClientException` 不再暴露状态码属性(仅消息构造),故翻译基于 `KubernetesException.Status` 与 `HttpOperationException.Response`。
3 个服务中每个实际调 K8s 的方法包 try/catch:catch → `logger.LogWarning(ex, ...)` 记录操作与上下文 → `throw K8sExceptionMapper.Translate(ex, "删除配置");`。
- 备选:集中 `KubernetesClient` 工厂统一拦截 —— 重构面大(要改所有调用方、client 生命周期),用户已选"每个服务都包"。

**D3. 服务层日志约定**
- 业务异常(翻译后/主动抛):`logger.LogWarning`(带 operation、clusterId、name 等上下文)
- 未预期系统异常:保持 `logger.LogError`(现有 catch 中的记录方式)
- 既有"优雅降级"catch 保留:`ClusterService.GetClusterDetailAsync` 节点加载失败 → `IsReachable=false` + LogWarning,**不** 转成用户提示(数据缺失非操作失败)。

**D4. ExceptionPresenter(Scoped)**
```csharp
public class ExceptionPresenter(ISnackbar snackbar, ILogger<ExceptionPresenter> logger)
{
    Task HandleAsync(Exception ex, string fallbackMessage) // fallbackMessage=操作名,如「保存」
}
```
- `BusinessException` → `Snackbar.Add(UserMessage, Severity)`:Conflict→`Warning`,其余→`Error`
- 非业务异常 → `logger.LogError(ex, ...)` + `Snackbar.Add($"{fallbackMessage}失败,请稍后重试", Error)`(不泄漏技术细节)
- Severity 映射集中在 presenter(单一入口),异常类不感知 UI 类型
- 注册:`builder.Services.AddScoped<ExceptionPresenter>()`

**D5. 存量 throw 迁移(19 处)**
| 现状 | 迁移为 |
|---|---|
| `InvalidOperationException("Cluster {id} not found")` | `NotFoundException($"集群 {id} 不存在")` |
| `InvalidOperationException("Group {id} not found")` | `NotFoundException($"分组 {id} 不存在")` |
| `InvalidOperationException("角色 {roleName} 不存在")` | `NotFoundException($"角色 {roleName} 不存在")` |
| `InvalidOperationException("备注长度不能超过 64 个字符")` | `ValidationException(...)` |
| `InvalidOperationException("YAML metadata.namespace 未指定")` | `ValidationException("YAML 未指定 metadata.namespace")` |
| `ArgumentException("target group id ...")` | `ValidationException(...)` |
- 规则:资源不存在→NotFound;冲突/并发→Conflict;输入/规则校验→Validation;无权限→Permission;其余业务失败→按语义归入最近类型或 BusinessException 匿名子类。

**D6. UI catch 收敛(36 处)**
每处 `catch (Exception ex)` 改为:
```csharp
catch (Exception ex) { await exHandler.HandleAsync(ex, "删除"); }
```
保留:YAML 本地格式校验(Deserialize)失败 → 这是纯本地校验,直接 `ValidationException` 语义 → 用 `HandleAsync` 传入或保持原 Snackbar(执行期按「消息是否适合用户」定)。既有"部分成功"流程(批量、刷新全部)只把失败分支接到 presenter,成功/计数逻辑不动。

## Risks / Trade-offs

- [36 处 catch 迁移遗漏] → tasks 按文件枚举 + 任务末尾 `grep "catch (Exception ex)"` 复查无 `ex.Message` 残留
- [400 错误详情提取不可靠(KubernetesClientException.Message 可能为空)] → 取 `ex.Status?.Details?.Message`,空则回退通用文案
- [翻译层把"系统级"K8s 异常误当业务异常] → 仅按明确状态码(400/403/404/409)映射;5xx/网络/未知状态一律走通用「请稍后重试」,不冒充业务语义
- [中文消息与现有英文日志混用] → 异常 UserMessage 中文;日志上下文键值用英文标识(id、operation),维持可检索

## Migration Plan

- 纯代码变更,无数据/API 迁移;部署照常 `docker compose -f docker-compose.prod.yml up -d --build`
- 回滚 = git revert;异常类型是新增,不破坏旧存储

## Open Questions

- 无阻塞项。执行期细化:`400` 详情取到什么粒度、`ClusterUnreachableException` 是否独立成类或并入 BusinessException —— 均不改变 spec 契约。
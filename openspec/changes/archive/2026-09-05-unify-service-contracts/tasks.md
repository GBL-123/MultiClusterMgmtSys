## 1. 新建 Request 类

- [x] 1.1 新增 `Requests/AccountCreateRequest`(UserName、Password、RoleName)
- [x] 1.2 新增 `Requests/AccountUpdateRequest`(Id、RoleName)
- [x] 1.3 新增 `Requests/ChangePasswordRequest`(CurrentPassword、NewPassword)
- [x] 1.4 新增 `Requests/ResetPasswordRequest`(Id、NewPassword)
- [x] 1.5 新增 `Requests/BatchRoleUpdateRequest`(Ids、RoleName)
- [x] 1.6 新增 `Requests/ClusterCreateRequest`(字段=现 ClusterCreateViewModel)与 `Requests/ClusterUpdateRequest`(多 Id)
- [x] 1.7 新增 `Requests/ClusterEndpointsUpdateRequest`(ClusterId、Items)
- [x] 1.8 新增 `Requests/NodeDetailQueryRequest`(ClusterId、NodeName)
- [x] 1.9 新增 `Requests/NodeIpNotesUpdateRequest`(ClusterId、NodeName、Items)
- [x] 1.10 新增 `Requests/GroupRenameRequest`(Id、NewName)与 `Requests/MoveClustersRequest`(ClusterIds、TargetGroupId)
- [x] 1.11 新增 `Requests/ConfigMapQueryRequest`(ClusterId、Namespace 可选)、`Requests/ConfigMapKeyRequest`(ClusterId、Name、Namespace)、`Requests/ConfigMapCreateRequest`(ClusterId、Yaml)、`Requests/ConfigMapUpdateRequest`(ClusterId、Name、Namespace、Yaml)

## 2. 目录搬迁与 Request 调整

- [x] 2.1 `AccountBatchResult` 从 `Requests/` 移至 `ViewModels/`(namespace 同步)
- [x] 2.2 `NodeListFilter` 从 `Requests/` 移至 `Models/`(namespace 同步)
- [x] 2.3 `ClusterEndpointEditItem` 从 `ViewModels/` 移至 `Requests/`(namespace 同步,更新类注释)
- [x] 2.4 删除 `ClusterCreateViewModel`;删除 `ClusterUpdateViewModel`(回显由 ClusterEditViewModel 承接)
- [x] 2.5 `AccountQueryRequest` 补 `SortBy`(string,取值 UserName/LastLoginAt/CreatedAt)
- [x] 2.6 `AuditLogQueryRequest` 补排序字段(对齐 Audits 页现排序标签)
- [x] 2.7 `ClusterQueryRequest.DateRange` 拆为 `DateTime? CreatedFrom`/`DateTime? CreatedTo`,去除 MudBlazor using

## 3. AccountService 改造

- [x] 3.1 注入 `IHttpContextAccessor`;`GetPagedAccountsAsync` 移除 `TableState`,分页排序只读 Request(删双通道回退逻辑)
- [x] 3.2 `CreateAccountAsync`/`UpdateAccountAsync`/`ResetPasswordAsync`/`ChangePasswordAsync`/`BatchUpdateRoleAsync` 改吃对应 Request(ChangePassword 用户名走 HttpContext)
- [x] 3.3 `DeleteAccountAsync`/`BatchDeleteAsync` 移除 currentUserId,防自删校验改从 HttpContext 取;取不到抛 `PermissionException`
- [x] 3.4 `GetUserByNameAsync` 返回 `AccountViewModel?`(不再返回 ApplicationUser)

## 4. AuditService 与 Repository 改造

- [x] 4.1 `GetRecentAsync` 移除 userName 参数(走 HttpContext)
- [x] 4.2 `GetPagedAsync` 移除 `TableState`、`currentUserName`、`isAdmin` 参数(身份走 HttpContext)
- [x] 4.3 `AuditLogRepository.GetPagedAsync` 移除 `TableState` 依赖,排序分页从 Request 读

## 5. 其余服务改造

- [x] 5.1 ClusterService:`AddClusterAsync`/`UpdateClusterAsync` 改吃 Request;`UpdateClusterEndpointsAsync` 改吃 `ClusterEndpointsUpdateRequest`
- [x] 5.2 ClusterNodeService:`GetNodeDetailAsync`/`UpdateNodeIpNotesAsync` 改吃对应 Request
- [x] 5.3 GroupService:`RenameGroupAsync`/`MoveClustersToGroupAsync` 改吃对应 Request
- [x] 5.4 ConfigMapService:`ListConfigMapsAsync`/`GetConfigMapAsync`/`DeleteConfigMapAsync`/`CreateConfigMapFromYamlAsync`/`UpdateConfigMapFromYamlAsync` 改吃对应 Request(Get/Delete 共用 `ConfigMapKeyRequest`)
- [x] 5.5 AuthService:`LogoutAsync` 移除 userName 参数(审计用户名走 HttpContext)

## 6. 调用点与映射扩展同步

- [x] 6.1 Account 页/对话框(Accounts.razor、AccountEditDialog、ResetPasswordDialog)与 Profile 页(ChangePasswordDialog、Profile.razor)按新签名调用
- [x] 6.2 EditClusterDialog 改绑/组装 Request;Clusters.razor 的 MoveClusters 调用同步
- [x] 6.3 节点/配置页调用点(NodeDetail、ConfigMaps)同步;ClusterFilterBar 日期区间映射新字段
- [x] 6.4 检查 `ViewModels/Mappings/` 与 `_Imports.razor` 的 namespace 引用,清理失效 using

## 7. 测试同步

- [x] 7.1 更新 `MultiClusterMgmtSys.Tests` 中引用变更签名的服务测试(组装 Request 入参)
- [x] 7.2 为"操作者上下文走 HttpContext"新增/调整测试(可构造身份的 Mock HttpContext;取不到身份抛 PermissionException)

## 8. 验证与文档

- [x] 8.1 `dotnet build MultiClusterMgmtSys.slnx` 0 错误
- [x] 8.2 `dotnet test MultiClusterMgmtSys.Tests` 全绿
- [x] 8.3 AGENTS.md 服务契约节更新:Request 入参/ViewModel 返回约定、豁免清单、目录归属规则
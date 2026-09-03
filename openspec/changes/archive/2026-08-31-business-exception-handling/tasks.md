## 1. 异常体系基础

- [x] 1.1 新建 `Common/Exceptions/BusinessException.cs`:抽象基类,含中文 `UserMessage` 属性,命名空间 `MultiClusterMgmtSys.Common.Exceptions`
- [x] 1.2 新建子类 `NotFoundException` / `ConflictException` / `ValidationException` / `PermissionException` / `ClusterUnreachableException`(均继承 BusinessException)
- [x] 1.3 新建 `Common/Exceptions/K8sExceptionMapper.cs`:`Translate(Exception ex, string operation)` 按 design D2 映射 KubernetesClientException(404/409/403/400/超时),非 KubernetesClientException 原样返回

## 2. ExceptionPresenter

- [x] 2.1 新建 `Components/Common/ExceptionPresenter.cs`:`HandleAsync(Exception ex, string fallbackMessage)`——业务异常按 UserMessage + Severity(Conflict→Warning,其余→Error)弹 Snackbar;非业务异常 `LogError` + 通用「{fallbackMessage}失败,请稍后重试」
- [x] 2.2 `Program.cs` 注册 `builder.Services.AddScoped<ExceptionPresenter>()`

## 3. 服务层改造(包装 + throw 迁移)

- [x] 3.1 `ClusterService.cs`:刷新状态方法的 K8s 调用包 try/catch → `K8sExceptionMapper` + LogWarning;存量 throw 迁移(Cluster not found→`NotFoundException` 中文文案)
- [x] 3.2 `ClusterNodeService.cs`:ListNodes/ReadNode 两处 K8s 调用包装;throw 迁移(Cluster not found、备注长度校验→ValidationException)
- [x] 3.3 `ConfigMapService.cs`:ListNamespaces/ListConfigMaps/GetConfigMap/Delete/Update/Create 各方法 K8s 调用包装;throw 迁移(YAML metadata.namespace 校验→ValidationException)
- [x] 3.4 `GroupService.cs` + `AccountService.cs`:存量 throw 迁移(Group not found→NotFoundException、角色不存在→NotFoundException、ArgumentException→ValidationException)

## 4. UI catch 收敛(注入 ExceptionPresenter)

- [x] 4.1 集群:Clusters.razor、ClusterDetail.razor、ClusterOverviewCard.razor、ClusterEndpointsDialog.razor、EditClusterDialog.razor、EditGroupDialog.razor(共 ~16 处 catch)
- [x] 4.2 节点:Nodes.razor、NodeDetail.razor、NodeIpNotesDialog.razor(共 ~5 处 catch)
- [x] 4.3 ConfigMap:ConfigMaps.razor、ConfigMapDetail.razor、EditConfigMapYaml.razor、CreateConfigMapDialog.razor(共 ~10 处 catch;删除 `Contains("409")` 字符串判断,409 改由翻译层处理;YAML 本地 Deserialize 校验失败用 ValidationException 语义)
- [x] 4.4 账号与个人:Accounts.razor、Profile.razor(共 ~3 处 catch)
- [x] 4.5 确认 `AuditService.LogAsync` 静默失败逻辑保持不变

## 5. 验证

- [x] 5.1 全局 grep 复查:无 catch 中 `ex.Message` 直出、无 `Contains("409")` 残留、无旧英文 throw(`Cluster .* not found` 等)
- [x] 5.2 `dotnet build MultiClusterMgmtSys.slnx` 通过,0 错误
- [x] 5.3 冒烟:`dotnet run` 启动,抽查集群/ConfigMap 页面操作失败时的提示文案(中文、无技术细节)
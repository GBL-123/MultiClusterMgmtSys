# namespace-management 任务清单

## 1. 输入输出契约与枚举

- [ ] 1.1 新增 `Requests/NamespaceKeyRequest.cs`(ClusterId, Name)与 `Requests/NamespaceCreateRequest.cs`(ClusterId, Yaml)record,附中文 XML 注释;验证 `dotnet build MultiClusterMgmtSys.slnx` 0 错误、CS1591 零命中。
- [ ] 1.2 新增 `ViewModels/NamespaceListViewModel.cs`(Name、StatusText、StatusCssClass、LabelCount、CreatedAt)与 `ViewModels/NamespaceDetailViewModel.cs`(Name、StatusText、StatusCssClass、CreatedAt、Labels、Annotations、Yaml),附中文 XML 注释;验证构建通过。
- [ ] 1.3 新增 `ViewModels/Mappings/NamespaceMappingExtensions.cs`:`V1Namespace` → 列表/详情 VM,YAML 用 `KubernetesYaml.Serialize`,状态归一 `Active`→在线(`online`)、其余(含 `Terminating`)→未知(`unknown`);验证:映射断言在 3.2 服务测试中通过。
- [ ] 1.4 `Common/Enums/AuditCategory.cs` 追加 `Namespace = 8`(含中文 XML 注释);验证构建通过且既有枚举数值不变。

## 2. NamespaceService 与注册

- [ ] 2.1 新增 `Services/NamespaceService.cs`:注入 `ClusterRepository`、`AuditService`、`ILogger<NamespaceService>`、`Func<KubernetesClientConfiguration, IKubernetes>` 工厂,复制 `BuildConfig` 私有方法;实现 `ListNamespacesAsync(int clusterId)`(集群不存在抛 `NotFoundException`,K8s 失败 `K8sExceptionMapper.Translate(ex, "加载命名空间列表")`);验证构建通过。
- [ ] 2.2 实现 `GetNamespaceAsync(NamespaceKeyRequest)`(集群不存在返回 `null`;K8s 失败 Translate「加载命名空间详情」)与 `CreateNamespaceFromYamlAsync(NamespaceCreateRequest)`(`KubernetesYaml.Deserialize<V1Namespace>`,`metadata.name` 空白抛中文 `ValidationException`,成功后 `auditService.LogAsync(AuditCategory.Namespace, AuditAction.Create, ...)`);验证:3.2/3.3 测试通过。
- [ ] 2.3 实现 `DeleteNamespaceAsync(NamespaceKeyRequest)`:受保护名(`default` 或以 `kube-` 开头)在调用任何 K8s API 之前抛中文 `ValidationException`;普通删除成功后写删除审计(目标含名称与集群名);验证:3.3 保护测试通过。
- [ ] 2.4 `Program.cs` 注册 `builder.Services.AddScoped<NamespaceService>()`;验证构建通过且 `NamespaceService` 可被页面注入。

## 3. 测试基建与服务层测试

- [ ] 3.1 扩展 `TestInfrastructure/K8sMocks.cs`:列表 setup(支持带阶段/标签的 `V1Namespace`)、`SetupReadNamespace`(+Throws)、`SetupCreateNamespace`(捕获提交对象)、`SetupDeleteNamespace`(+Throws);`*WithHttpMessagesAsync` 签名用 sigtool 反查后落定;验证 `dotnet build MultiClusterMgmtSys.slnx` 通过。
- [ ] 3.2 新增 `Tests/Services/NamespaceServiceTests.cs`:列表与详情映射(名称/状态归一/标签/注解/YAML)、集群不存在抛 `NotFoundException`、K8s 403/404 翻译;验证 `dotnet test MultiClusterMgmtSys.Tests` 相关测试通过。
- [ ] 3.3 在 `NamespaceServiceTests` 补:创建缺失 `metadata.name` 与非法 YAML 抛 `ValidationException` 且 K8s 零调用、同名 409 翻译为 `ConflictException`、删除保护(`default`/`kube-system`/`kube-custom`)抛 `ValidationException` 且 K8s 零调用、普通删除成功且审计落库;验证测试通过。

## 4. 页面与共享组件

- [ ] 4.1 新增 `Components/Namespaces/Shared/NamespaceListFilterBar.razor`(名称 `MudTextField` + 查询/重置按钮,仅更新页面状态,过滤为客户端行为);验证:页面测试的渲染与过滤断言通过。
- [ ] 4.2 新增 `Components/Namespaces/Shared/NamespaceListTable.razor`:名称(`.link-primary`)、状态徽章、标签数(等宽)、创建时间、操作列(详情 = 链接;删除 = `<AuthorizeView Roles="Admin">` 且受保护名禁用),空态用 `.empty-state`、加载态 `// 正在加载...`;验证:页面测试断言。
- [ ] 4.3 新增 `Components/Namespaces/Shared/CreateNamespaceDialog.razor`:打开时 `YamlTemplateService.GetTemplateAsync("namespace", "default")` 预置模板;提交前 `KubernetesYaml.Deserialize<V1Namespace>` 预解析,缺失 `metadata.name` 显示中文校验错误且不提交;成功关闭并刷新,409 显示「同名命名空间已存在」并保持打开;验证:对话框测试通过。
- [ ] 4.4 新增 `Components/Namespaces/Shared/NamespaceDetailToolbar.razor`(返回列表 + 名称 + 状态徽章 + 刷新)与 `NamespaceLabelsCard.razor` / `NamespaceAnnotationsCard.razor`(带计数只读键值表、空态占位)、`NamespaceYamlViewCard.razor`(只读 `yaml-textarea` 卡);验证:详情页测试渲染断言通过。
- [ ] 4.5 新增 `Components/Namespaces/Pages/Namespaces.razor`(`/namespaces`、`/namespaces/{ClusterId:int}`):`ClusterSelectSidebar` + 集群徽章 + 刷新 + Admin 新建按钮 + 过滤条 + 表格,支持 `ClusterSelectionState` 会话内恢复、集群不可达提示与禁写、未选集群空态;验证:`dotnet build` + 页面测试通过。
- [ ] 4.6 新增 `Components/Namespaces/Pages/NamespaceDetail.razor`(`/namespaces/{ClusterId:int}/{Name}`):工具栏 + `MudTabs`(YAML | 标签与注解),404/集群不存在显示「不存在或已被删除」空态与返回入口、读取失败走 `ExceptionPresenter`;验证:页面测试通过。
- [ ] 4.7 `Components/Layout/Drawer.razor` 在「节点管理」之后新增顶层「命名空间管理」入口(`Match="NavLinkMatch.Prefix"`);验证:导航测试断言入口存在且路由激活态正确。

## 5. YAML 模板

- [ ] 5.1 新增 `wwwroot/templates/namespace/default.yaml`(`kind: Namespace`、`apiVersion: v1`、`metadata.name` 示例,不含 `namespace` 字段);验证:创建对话框预置内容的测试断言通过。

## 6. 页面测试与整体验证

- [ ] 6.1 扩展 `TestInfrastructure/BunitServiceExtensions.cs` 注册 `NamespaceService` 与命名空间页面依赖;新增页面测试覆盖:侧栏集群选择/会话恢复、名称过滤、Admin 新建按钮与 Member 门控、受保护行删除禁用、详情 tab 切换、不可达空态;验证:`dotnet test MultiClusterMgmtSys.Tests` 全绿。
- [ ] 6.2 全量验证:`dotnet build MultiClusterMgmtSys.slnx` 0 错误;`dotnet test MultiClusterMgmtSys.Tests` 全绿且测试数不低于 485;按 AGENTS 口径用最新单份 cobertura 确认行覆盖率不低于 75%。
- [ ] 6.3 运行应用手工冒烟(`dotnet run --project MultiClusterMgmtSys`):`/namespaces` 选集群 → 列表加载 → YAML 创建 → 详情两个 tab → 删除普通命名空间 → 确认 `default`/`kube-system` 删除按钮禁用且服务端拒绝;验证观察结果与 spec 场景一致。

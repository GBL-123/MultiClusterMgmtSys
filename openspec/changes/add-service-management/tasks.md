## 1. 基础设施与契约层

- [x] 1.1 `Common/Enums/AuditCategory.cs` 追加 `Service = 7`
- [x] 1.2 创建 `Requests/`:SvcQueryRequest、SvcKeyRequest、SvcCreateRequest、SvcUpdateRequest(对齐 ConfigMap*Request 形状)
- [x] 1.3 创建 `ViewModels/`:SvcListViewModel(含 List<SvcPortViewModel>、对外入口、ClusterIP、Headless 标记)、SvcPortViewModel(TargetPort 为 string?)、SvcEndpointViewModel(地址:端口 + Ready)、SvcDetailViewModel
- [x] 1.4 创建 `ViewModels/Mappings/SvcMappingExtensions.cs`:V1Service → 列表/详情 VM(IntOrString 分支、nodePort、LB ingress、ExternalName)、V1EndpointSlice/V1Endpoints → SvcEndpointViewModel 统一映射
- [x] 1.5 `Program.cs` 注册 SvcService(scoped)

## 2. SvcService 服务层

- [x] 2.1 复制 ConfigMapService 的 BuildConfig 模式创建 `Services/SvcService.cs`(clientFactory 注入)
- [x] 2.2 GetNamespacesAsync / ListServicesAsync(SvcQueryRequest,命名空间过滤)
- [x] 2.3 GetServiceAsync(SvcKeyRequest)→ SvcDetailViewModel
- [x] 2.4 GetServiceEndpointsAsync:EndpointSlice 优先(label `kubernetes.io/service-name`),仅 404 降级 CoreV1 Endpoints,统一折叠为 SvcEndpointViewModel
- [x] 2.5 CreateServiceFromYamlAsync:反序列化(失败抛中文 ValidationException)+ metadata.namespace 必填 + 审计(创建)
- [x] 2.6 UpdateServiceFromYamlAsync:读回 existing → clusterIP/clusterIPs/ipFamilies 守卫(不同抛中文校验异常,省略补齐)→ 补 resourceVersion/uid、保留 status → Replace + 审计(修改)
- [x] 2.7 DeleteServiceAsync:删除 + 审计(删除,目标含 ns/name/集群名)
- [x] 2.8 全部 K8s 调用点 try/catch → LogWarning → K8sExceptionMapper.Translate(ex, 中文操作名)

## 3. UI — 页面与共享组件

- [x] 3.1 `Components/Layout/Drawer.razor` 新增「网络管理」MudNavGroup(Lan 图标)+「服务管理」子项(SettingsEthernet 图标),workloadsExpanded 同款展开逻辑
- [x] 3.2 `Components/Services/Pages/Services.razor`(路由 /services、/services/{ClusterId:int}):集群侧栏 + 状态徽章 + 命名空间过滤 + 名称搜索 + 列表表格 + 新建对话框入口(Admin)
- [x] 3.3 `Components/Services/Shared/SvcListTable.razor`:名称链接/命名空间 chip/类型(mono)/端口列(每端口一行,kubectl 语法 + targetPort,>3 截断 +N)/对外入口列/ClusterIP(None 形态)/创建时间/操作列;ExternalName 端口格 `[ 暂无 ]`
- [x] 3.4 `Components/Services/Shared/SvcListFilterBar.razor`(命名空间下拉 + 名称搜索,含端口搜索文本框,若实现)
- [x] 3.5 `Components/Services/Shared/`:SvcYamlViewCard / SvcYamlEditCard(yaml-textarea 模式)/ CreateServiceDialog(YAML 模板)
- [x] 3.6 `Components/Services/Pages/ServiceDetail.razor`(路由 /services/{ClusterId:int}/{Namespace}/{Name}):YAML 视图 + 端口表 + Endpoints 卡(.status-badge Ready 徽章)+ 详情工具栏(刷新/编辑/删除,Admin 门控);不存在空态、不可达降级
- [x] 3.7 `Components/Services/Pages/EditServiceYaml.razor`:编辑页复用 SvcYamlEditCard,异常经 ExHandler.HandleAsync 呈现

## 4. 测试(MultiClusterMgmtSys.Tests)

- [x] 4.1 SvcMappingExtensions 测试:IntOrString(数字/命名)、nodePort、Headless None、ExternalName、LB ingress 待分配、多端口
- [x] 4.2 SvcService 测试(K8sMocks `*WithHttpMessagesAsync`):列表/详情/创建/删除的成败与异常翻译
- [x] 4.3 UpdateServiceFromYamlAsync 守卫测试:修改 clusterIP 抛中文 ValidationException 且不触 K8s、省略补齐成功、可变字段替换成功
- [x] 4.4 Endpoints 降级测试:EndpointSlice 200 展平;404 → 旧 Endpoints;其余异常 Translate
- [x] 4.5 审计断言:创建/修改/删除成功后 AuditCategory.Service 落库,目标含 ns/name/集群名
- [x] 4.6 bUnit 接线契约:SvcListTable 行内端口行渲染与 +N 截断、Headless/ExternalName 分支、操作按钮 Admin 门控(不测 .mud-* 内部 DOM)

## 5. 验证与收尾

- [x] 5.1 `dotnet build MultiClusterMgmtSys.slnx` 0 错误
- [x] 5.2 `dotnet test MultiClusterMgmtSys.Tests` 全绿(MTP)
- [ ] 5.3 手工核对:对真实集群走通 列表→详情→新建→编辑→删除,老/新集群 Endpoints 均正常显示

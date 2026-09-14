# pod-management Tasks

## 1. 服务层与数据类型

- [x] 1.1 新增 `Requests/PodQueryRequest.cs`(ClusterId + 可空 Namespace)、`Requests/PodKeyRequest.cs`、`ViewModels/PodListViewModel.cs`、`ViewModels/PodDetailViewModel.cs`(含容器状态集合);`dotnet build` 0 错误
- [x] 1.2 新增 `Services/PodService.cs`(只读、无审计、经 `IClusterClientCache`:`GetNamespacesAsync`、`ListPodsAsync`(null ns = 全部命名空间,轻投影)、`GetPodAsync`),集群不存在抛 `NotFoundException`,K8s 失败经 `K8sExceptionMapper` 翻译;`Program.cs` 注册;验证:`dotnet build` 0 错误
- [x] 1.3 `K8sMocks` 补 `SetupListPods`/`SetupListNamespacedPods`/`SetupReadPod` mock 扩展(含容器 waiting/terminated 状态构造辅助);新增 `PodServiceTests`(列表加载/NotFound/集群不可达翻译/详情读取/404→未找到);验证:测试全绿

## 2. 状态语义与展示映射

- [x] 2.1 `ViewModels/Mappings/K8sDisplayText.cs` 增补 Pod phase 与容器 waiting/terminated 原因的中文映射(未登记回退原文);投影层实现"容器信号优先于 phase"的徽章判定(主文案/英文次行/CssClass online|offline|unknown),验证:映射单元测试覆盖「Running + CrashLoopBackOff → 异常徽章」「正常 Running → online」「Succeeded → 中性」「未登记原因回退原文」
- [x] 2.2 新增 `Models/PodListFilter.cs`(命名空间精确/状态分类/关键词[名称·节点·IP] 内存过滤,含重置语义);验证:纯函数单元测试全绿

## 3. Pod 列表页

- [x] 3.1 新增 `Components/Pods/Pages/Pods.razor`(`/pods`、`/pods/{ClusterId:int}`,`[Authorize]`,集群选择状态回退与离线降级卡)+ `Components/Pods/Shared/` 的 FilterBar 与 `PodsListTable`(命名空间下拉、状态分类、关键词、刷新与数据截至时间、行点击进详情);`app.css` 登记 `.pods-table` flex-fill 三处选择器;导航注册 Pod 入口;验证:`dotnet build` 0 错误
- [x] 3.2 列表页 bUnit 接线测试(新增 `AddPodStack`:数据加载后渲染行、过滤不触发新 K8s 调用、CrashLoopBackOff 行徽章为异常文案、离线集群降级卡、重置恢复全量);验证:测试全绿

## 4. Pod 详情页与工作负载直达

- [x] 4.1 新增 `Components/Pods/Pages/PodDetail.razor`(`/pods/{ClusterId:int}/{Namespace}/{Name}`:基本信息卡 + 容器状态卡[就绪/状态/重启/镜像/最近终止原因与时间];不存在→「未找到」空状态;不可达→降级);验证:`dotnet build` 0 错误
- [x] 4.2 工作负载详情(`WorkloadDetailView`)新增「查看 Pod」入口:按该 workload 的 label selector 经 `ListNamespacedPodAsync` 取其 Pod 并跳转列表;验证:服务级测试(selector 过滤命中)+ `dotnet test` 全绿

## 5. 全量回归

- [x] 5.1 `dotnet build MultiClusterMgmtSys.slnx` 0 错误
- [x] 5.2 `dotnet test MultiClusterMgmtSys.Tests` 全绿(基线 667 + 新增)

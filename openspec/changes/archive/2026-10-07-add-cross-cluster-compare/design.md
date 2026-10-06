# Design

## 决策记录

- **D1 全部语义写进新契约 `cross-cluster-compare`,不修改既有 capability**:克隆的归属盖章/黑名单/审计/冲突语义由既有创建服务天然承接(它们已在 `workload-management`/`configmaps-page`/`k8s-resource-ownership` 契约内),对照页不改变任何既有页面的 requirement 正文,只加 Drawer 入口(入口语义写进新契约的页面 requirement,与 `cluster-dashboard` 处理登录落地页的先例一致,但避免在 ui-theme/列表契约上做 MODIFIED 拷贝)。
- **D2 对照 =「同命名空间 + 同名对象」一一对照**,不做全命名空间扫描 diff(行数不可控、价值密度低):源侧选清单(复用既有列表服务分页/排序产物),对照侧按 ns+name 对齐读取;差异对齐从「资源清单对照」维持最小切片。
- **D3 diff 在 Web 层纯函数实现(`Components/Compare/YamlDiff.cs`,LCS 行算法,零依赖)**:服务层返回两侧 `string[]`(YAML 原文行数组)、`SourceExists/TargetExists`;渲染组件把 diff 行映射 CSS 类。不使用第三方 diff 库——仓库无该依赖,引入需单独评审。
- **D4 元数据剥离在服务层而非 UI 层**:`ClusterCompareService.StripServerMetadata`(解析为一棵 `KubernetesYaml` 对象图后删字段,再序列化回 YAML),保证进入克隆预览与最终提交的是同一份剥管文本;`managedFields`/归属标签/归属注解/status/uid/resourceVersion/creationTimestamp 全清空。既有创建服务的 `PrepareForCreation` 只负责黑名单与盖章,不做剥离——两者的分工是「去身份」(对照服务)对「定归属」(创建服务)。
- **D5 克隆不允许二段式冲突**:目标已有同名对象直接中文冲突提示,不提供「更新对齐」选项(v1);这样克隆条目恒为幂等失败,无需回滚/补偿。
- **D6 族调度靠服务层 switch,不引入抽象家族接口**:`ClusterCompareService` 按 kind 分派到 `WorkloadService`(四类)或 `ConfigMapService` 的既有读/建方法;两个服务的公开方法签名不动。

## 架构落位

```
Domain:                          无新实体
Application/ViewModels:          ComparePairViewModel { Kind, Namespace, Name, SourceCluster*,
                                   SourceExists, SourceYaml, TargetExists, TargetYaml, HasDifference,
                                   DifferenceCount }
Application/Requests:            ComparePairQueryRequest { SourceClusterId, TargetClusterId,
                                   Kind, Namespace, Name }
                                 CompareCloneRequest { SourceClusterId, TargetClusterId, Kind,
                                   Namespace, Name }   (身份信息取 IHttpContextAccessor,同既有口径)
Application/Services:            ClusterCompareService(IClusterClientCache, WorkloadService,
                                   ConfigMapService, AuditService?, …)
                                   —— GET 走两簇客户端并行(Task.WhenAll);IServices 已要求 CanOperate
                                      的写路径仅克隆用到;审计由被复用的创建服务写(参照既有口径)
Contrast/YAML:                   读取沿用既有「YAML 视图」的序列化口径(KubernetesYaml.SerializeObject);
                                 剥离也在服务层做,不统一 Telos。
Web/Components/Compare/:         Pages/Compare.razor([Authorize],路由 /compare;顶部双簇选择+族+ns)、
                                 Shared/CompareDiffView.razor(双栏 + 差异类名 + 仅看差异开关)、
                                 Shared/CloneConfirmDialog.razor(剥管 YAML 预览 + 起始按钮)
YamlDiff.cs:                     Lcs(string[] left, string[] right) → 行分类 (Same / LeftOnly /
                                 RightOnly / Changed 行对) —— LeftOnly=源有对照无,RightOnly=对照有源无
Drawer:                          MudNavGroup 外的独立 NavLink「跨集群对照」,Href="/compare",位于
                                 「Helm 应用管理」之后
app.css:                         新第 16 节 .compare-group:`.compare-columns`(双栏 minmax(320px,1fr))、
                                 `.compare-diff-row.is-changed/.is-added/.is-removed`(发丝线行 +
                                 暖底色高亮,仅 changed/added/removed 着色)、`.compare-toggle`(开关行);
                                 页面根 MudStack 带 `flex-auto`
```

## 边界与契约一致性核对

- `k8s-resource-ownership`:克隆后的归属 = 既有创建服务对**当前操作者**的盖章;对照读取对无 `CanOperate` 者不展示克隆入口(与「Member 建目标 Namespace 禁 `kube-`」同一条承路,不为对照页开例外)。
- `kubernetes-call-timeout` / `k8s-client-cache`:对照两簇读取与克隆写入全部经 `IClusterClientCache`,10s 超时适用;两侧并行非并写,不存在组合超时难题。
- `exception-handling`:`K8sExceptionMapper.Translate(ex, "读取对照对象"/"克隆创建")`;409 经映射自然变 `ConflictException`(中文),与克隆「同族已存在」的业务冲突统一回走 UI `ExHandler.HandleAsync`。
- `display-conventions`:族选择器双语(中文主行 + 英文次行,提交值用 kind 原文);「不存在」侧用 `.empty-state`;按钮统一 `TooltipIconButton` 或常规按钮,无原生 title。
- `service-contracts`:入参一律 Request 对象;输出一律 ViewModel;无 MudBlazor 类型泄漏。
- 分层:diff 在 Web、剥离在 Application;Infrastructure 不出现新类型。

## 风险与开放问题

- 大型 ConfigMap(数十 KB 文本)的双栏渲染成本可接受(静态 HTML 行),未做虚拟滚动;
  如实测卡顿,再在 `CompareDiffView` 加渲染上限注记,不阻塞本 change。
- Workload 五类读 YAML 的既有方法签名以变更实施时对照实测为准(不猜签名,实施前先读);
  服务层新方法若与现有 YAML 读取口径差异明显,以复用序列化 cfg 为准。

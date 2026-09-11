# namespace-management 设计

## Context

Kubernetes Namespace 是集群级资源。当前系统只在 ConfigMap/工作负载/服务三个页面把它当作筛选下拉(三个服务各自持有一份 `GetNamespacesAsync`),没有独立页面或服务。相关页面骨架与契约已经成熟并可直接复用:

- `ClusterSelectSidebar` + `ClusterSelectionState`(集群级列表页的标准布局与会话内集群记忆);
- `detail-page-tabs` / `ui-theme` 契约(工具栏 + `MudTabs`、状态徽章、空态/加载态);
- `YamlTemplateService`(创建模板外置 + 缺失回退)、`Func<KubernetesClientConfiguration, IKubernetes>` 工厂注入、`K8sExceptionMapper` + `ExceptionPresenter` 异常链路、`AuditService`。

动机与范围见 `proposal.md`;行为契约见 `specs/namespace-management/spec.md` 与 `specs/audit-log/spec.md`。

## Goals / Non-Goals

**Goals:**

- 以与既有资源页同构的最小新增面,提供命名空间列表、详情、YAML 创建与删除。
- 把删除保护做成服务层不可绕过的安全边界,而不是仅 UI 禁用。

**Non-Goals:**

- 不收敛 ConfigMapService/WorkloadService/SvcService 中重复的 `GetNamespacesAsync` 与 `BuildConfig`(留作后续独立重构)。
- 不做 ResourceQuota/LimitRange、准入资源计数、标签/注解编辑、finalizer 处理。
- 不新增数据库 schema、NuGet 依赖或新设计 token。

## Decisions

### 1. 新增独立 `NamespaceService`,沿用复制式客户端构建

复制既有服务的 `BuildConfig(ClusterInfo)` 私有方法与 `Func<KubernetesClientConfiguration, IKubernetes>` 工厂注入模式。

- 备选:把 `BuildConfig`/命名空间读取抽成共享 helper 并改造三个既有服务。否决:会牵动 3 个服务、8 个页面测试与 `BunitServiceExtensions`,在覆盖率基线尚未收尾时引入无用户可见收益的风险;本次刻意不扩面。
- 备选:并入 `ClusterService`(集群实体服务)。否决:数据库实体与实时 K8s 资源边界不应混合,且与 `SvcService`/`ConfigMapService`/`WorkloadService` 的既有分层不一致。

### 2. 创建走 YAML 对话框,不新增表单

对话框预置 `wwwroot/templates/namespace/default.yaml`,提交时 `KubernetesYaml.Deserialize<V1Namespace>` 并校验 `metadata.name` 非空。

- 备选:表单(名称 + 标签)。否决:破坏四类资源统一的 YAML 创建路径,还要自行实现 K8s DNS-1123 名称校验。
- 备选:表单 + 高级 YAML 双模式。否决:UI 与测试面翻倍,收益不足以支撑。
- 命名空间是集群级资源,YAML 校验的是 `metadata.name`(而非其他资源的 `metadata.namespace`),服务层在调用 K8s 前抛中文 `ValidationException`。

### 3. 删除保护在服务层硬编码

规则:`name == "default" || name.StartsWith("kube-")` 时,在发起任何 K8s 调用之前抛出中文 `ValidationException`;列表页对同样规则的行禁用删除按钮。

- 备选:仅 UI 禁用。否决:任何服务调用入口(测试、未来 API)都能绕过 UI,安全边界必须在服务层。
- 备选:保护名单做成配置。否决:这是 K8s 本身的系统命名空间约定,当前没有真实的可配置需求(YAGNI);未来确有例外场景再升级为配置。
- 备选:删除要求输入命名空间名二次确认。否决:交互过重;服务端保护 + 确认对话框文案提示"将删除其中所有资源"已覆盖主要风险。

### 4. 服务输入输出契约

- `ListNamespacesAsync(int clusterId)` 返回 `List<NamespaceListViewModel>`;集群不存在抛 `NotFoundException`(与其他两个下拉方法的"集群不存在"语义一致)。单原语参数符合 `service-contracts` 的豁免。
- `GetNamespaceAsync(NamespaceKeyRequest)` 返回 `NamespaceDetailViewModel?`;集群不存在返回 `null`(与 `ConfigMapService.GetConfigMapAsync`/`SvcService.GetSvcAsync` 先例一致),K8s 404 经翻译抛 `NotFoundException`。
- `CreateNamespaceFromYamlAsync(NamespaceCreateRequest)`、`DeleteNamespaceAsync(NamespaceKeyRequest)`:集群不存在抛 `NotFoundException`,K8s 失败经 `K8sExceptionMapper.Translate` 翻译。
- 请求对象:`Requests/NamespaceKeyRequest.cs`(ClusterId, Name)、`Requests/NamespaceCreateRequest.cs`(ClusterId, Yaml)。展示模型:`ViewModels/NamespaceListViewModel.cs`、`ViewModels/NamespaceDetailViewModel.cs` + `ViewModels/Mappings/NamespaceMappingExtensions.cs`(YAML 用 `KubernetesYaml.Serialize`)。

### 5. 状态归一在映射层完成

`Active` 归一为在线(`online`),其余(含 `Terminating`、空阶段)归一为未知(`unknown`);列表与详情工具栏共用。不新增 CSS/设计 token,沿用 `ui-theme` 的 `.status-badge` 词汇(在线=绿、未知=琥珀)。

### 6. 页面与组件拆分

- `Components/Namespaces/Pages/Namespaces.razor`(`/namespaces`、`/namespaces/{ClusterId:int}`):复用 `ClusterSelectSidebar`,骨架对齐 ConfigMap/Svc 列表页(集群徽章 + 刷新 + Admin 新建按钮 + 名称搜索 + 表格)。
- `Components/Namespaces/Pages/NamespaceDetail.razor`(`/namespaces/{ClusterId:int}/{Name}`):工具栏(返回列表 + 名称 + 状态徽章 + 刷新)+ `MudTabs`(YAML | 标签与注解),tab 为页面局部状态。
- `Components/Namespaces/Shared/`:列表表格、搜索过滤条、创建对话框、详情工具栏、标签卡与注解卡(命名空间专属,不泛化 Nodes 卡片以避免触碰节点页测试)。
- 详情页数据在进入页面时一次性拉取,与 `detail-page-tabs` "不引入新 ViewModel 字段支撑 tab 化"的要求一致。

### 7. 审计

新增 `AuditCategory.Namespace = 8`(追加在 `Service = 7` 之后,数值稳定);创建/删除成功后 `auditService.LogAsync(AuditCategory.Namespace, AuditAction.Create|Delete, $"命名空间: {name} @ 集群 {clusterName}")`;写入失败保持静默(既有 `AuditService` 行为)。`AuditAction` 无需新增值。

### 8. 测试策略

- 服务层:真实 SQLite(`SqliteDbFactory`)+ Moq `IKubernetes`(`*WithHttpMessagesAsync` 签名用 sigtool 反查);必测:列表映射与排序无关性、详情映射(含 YAML/标签/注解)、集群不存在、404 翻译、创建缺失 `metadata.name`、409 冲突翻译、删除保护(受保护名 K8s 调用零次)。
- 页面层:bUnit 只测接线契约(侧栏选择与 `ClusterSelectionState`、tab 渲染、Admin 门控、受保护行删除禁用、不可达态);遵循 `BunitContext` + `LTS` 既有约定。
- 基建:`K8sMocks` 增加 Read/Create/Delete Namespace 的 setup 与 throws;`BunitServiceExtensions` 增加命名空间页面测试的服务注册。

## Risks / Trade-offs

- 删除是异步的(命名空间进入 `Terminating` 才真正消失)→ 列表将其显示为未知徽章,不做轮询;用户可手动刷新观察。
- `kube-` 前缀保护可能挡住极少数用户自建的同前缀命名空间(如 `kube-custom`)→ 校验消息说明"系统命名空间受保护";如出现真实需求,再升级为可配置名单。
- `BuildConfig` 与命名空间读取出现第 4 份重复 → 认知成本上升;已在 proposal 记为非目标,建议后续以单独 change 收敛。
- YAML-only 创建对不熟悉 K8s 语法的用户门槛偏高 → 与四类资源保持一致的既有取舍;模板文件可降低门槛。

## Migration Plan

- 无数据库迁移:`AuditCategory` 仅追加编译期枚举值,`EnsureCreated` 创建的 schema 不受影响;已有 DB 无需删除或重建。
- 部署即常规发布;回滚为还原代码,不涉及数据回填。

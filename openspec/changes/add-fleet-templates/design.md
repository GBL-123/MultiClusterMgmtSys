# Design

## Context

五族的创建 / 编辑 / 单对象读路径已存在:`WorkloadService` 的 `CreateDeploymentFromYamlAsync` 等四族创建方法与 `UpdateDeploymentFromYamlAsync` 等四族更新方法(入参 `WorkloadCreateRequest(clusterId, yaml)` / `WorkloadUpdateRequest(clusterId, name, ns, yaml)`),`ConfigMapService` 的 `CreateConfigMapFromYamlAsync` / `UpdateConfigMapFromYamlAsync`;创建路径内部完成归属盖章(`PrepareForCreation`:kube- 前缀黑名单 + `ResourceOwnershipStamp` 无条件盖章)、更新路径内部完成 `RequireOperateAsync`(Admin 短路放行,更新不重盖归属)与审计,全部经 `IClusterClientCache`。`/compare` 已提供双栏 diff 全链:`CompareDiffView.razor`(三分支呈现 + 仅看差异切换,内嵌克隆按钮 / 提示 / 对话框)、`YamlDiff` 纯静态行级 diff(Web 层,`Compute(leftYaml, rightYaml)` CRLF 归一)、`ClusterCompareService` 私有 `StripServerMetadata`(剥 uid/resourceVersion/creationTimestamp/status 段/managedFields/归属 label 与注解,复用克隆剥管口径)。`helm-fleet-deploy` 提供下发先例:`HelmService.DeployToFleetAsync`(`MaxFleetConcurrency = 4`、`Parallel.ForEachAsync`、逐集群失败隔离、`Interlocked` 进度「已完成 / 总数」、逐成功审计;其 `_dbWriteGate` 只围自家 DB 写,helm CLI 调用在闸外)。动机与范围见 proposal.md。

## Goals / Non-Goals

**Goals:**

- 趟通「渲染 → diff → upsert」链:`{{var}}` 纯文本渲染、逐集群双栏预览(新建 / 更新 / 一致 / 获取失败)、既有路径下发
- 双栏 diff 视图提取为共享组件,Compare 页面行为与既有断言不变
- 并发 4 下,既有路径(方法内部混用 DbContext 与 K8s 调用)可安全并行
- 审计双层:对象级(随既有路径自动)+ 舰队级(逐成功集群,描述含「舰队下发」与目标集群)

**Non-Goals:**

- 模板库与任何持久化(v2 北极星,见 proposal 演进口)
- 五族之外(Secret / PVC / Service / Ingress / CronJob 等)
- 模板引擎语义(条件 / 循环 / 表达式)与多文档 YAML(`---`)
- 下发后回读验证、逐变量粒度 diff、非 Admin 可见性、下发重试编排

## Decisions

### F1 入口与页面形态:独立页 + Drawer Admin 段

路由 `/fleet-templates`,页面 `Web/Components/FleetTemplates/Pages/FleetTemplates.razor`(命名空间 `MultiClusterMgmtSys.Web.Components.FleetTemplates`),页面级 `@attribute [Authorize(Roles = "Admin")]`。四态流程(粘贴 → 矩阵 → 预览 → 执行 / 汇总)装不进对话框,且与 `/compare` 同为工作台页形态,不接 `ClusterSelectSidebar`(目标集群由页面内多选控件表达)。Drawer 入口在 AuthorizeView Admin 段内 NavLink「舰队模板」(图标 `Icons.Material.Filled.Difference`),顺位:跨集群对照 → **舰队模板** → 告警中心 → 账号管理(add-alert-center 同段相邻,两者落位互不冲突)。备选否决:挂在 /helm 或 /compare 下的对话框(四态 + 逐集群双栏 diff 展示面过大)。

### F2 diff 复用:YamlDiff 直接引用 + CompareDiffView 提取薄壳化

`YamlDiff` / `CompareDiffRow` / `CompareDiffRowKind` 为纯静态 Web 层类型,舰队模板页直接引用,不复制。`CompareDiffView` 的 diff 呈现提取为共享组件 `Web/Components/Common/YamlDiffView.razor`(与 StatusBadge 等共享组件同目录),参数:`LeftTitle` / `RightTitle`(string)、`LeftYaml` / `RightYaml`(string?,null 或空 = 该侧不存在,与 `YamlDiff.Compute` 约定一致)、`Rows`(由调用方 `YamlDiff.Compute` 算好传入,共享组件无逻辑)、可选 `HeaderActions` RenderFragment(置于标题行右侧)。承接三分支:双侧存在 → 双栏网格 + 仅看差异切换 + 图例 +「两侧内容一致」提示;单侧存在 → 纯文本 pane + `[ (不存在) ]`;`.compare-diff-*` 等 CSS 类为 app.css 全局类,不迁移不改名。`CompareDiffView` 改为薄壳:保留 `Pair` / `Rows` / `OnCloned` 对外契约与克隆按钮 / 提示 / 对话框,呈现体委托 `YamlDiffView`——`Compare.razor` 与 `CompareDiffViewTests` 渲染断言不变。备选否决:舰队模板页自绘 diff(两套 diff 呈现规则必然漂移)。

### F3 upsert 下游路径:新建走创建、更新走编辑,归属语义全随既有路径

判定为「新建」→ 调该族既有创建方法(入参 `ConfigMapCreateRequest(clusterId, yaml)` / `WorkloadCreateRequest(clusterId, yaml)`);判定为「更新」→ 调既有编辑方法(入参 `ConfigMapUpdateRequest` / `WorkloadUpdateRequest` 的 `(clusterId, name, ns, yaml)`,name / namespace 取自该集群渲染产物解析后的 metadata,namespace 缺省按既有路径口径处理)。Admin 更新 Member 归属对象由 `RequireOperateAsync` 的 Admin 短路放行(F3 场景);创建时的归属盖章取当前操作者身份(Admin),更新不重盖(保留原 owner label / 注解);本变更不另立归属规则、不降权、不绕过。对象级审计(该族创建 / 更新审计)随路径自动产生。

### F4 服务形态与并发模型:每集群独立 IServiceScope,既有路径在私有 scope 内执行

`FleetTemplateService`(Application/Services,scoped,`AddApplicationServices` 注册;主构造 `IClusterRepository`、`IHttpContextAccessor`、`IServiceScopeFactory`、`ILogger`)。Preview 与 Deploy 统一结构:入口 Admin 检查与参数校验(串行)→ `GetAllForDashboardAsync` 解析目标集群(串行,构造名称映射)→ `Parallel.ForEachAsync`(`MaxDegreeOfParallelism = 4`,镜像 helm fleet)逐集群单元,**单元内 `CreateScope()`**:scope 中经 `IServiceScopeFactory` 解析 `IClusterRepository` / `WorkloadService` / `ConfigMapService` / `AuditService`,预取现值、判定、下发、写审计全部封闭在该集群私有 DbContext 内。

理由:既有创建 / 编辑 / 读路径在方法内部混用 DbContext(集群读、归属 / 审计写)与 K8s 调用,外层信号量无法只围 DB 段(helm fleet 的 `_dbWriteGate` 能真并行是因为 DB 写全在 HelmService 自家、CLI 调用在闸外;本变更的等价物是 scope 隔离)。备选否决:①下发全串行(违背 spec 有界并发 4 硬需求);②绕过既有路径自行拼 K8s 调用(违背「走既有创建 / 编辑路径」需求,且盖章 / 黑名单 / 审计 / 归属判定全部漂移)。

### F5 渲染与校验管线:纯函数助手 + 七段校验序

纯静态助手 `FleetTemplateVariables`(与 `FleetTemplateService` 同文件,镜像 AlertRuleKind/AlertRuleText 同文件先例):`Extract`(正则提取 `{{标识符}}` 变量名,有序去重;含空格等非标识符结构如 `{{if ...}}` 不提取)、`Render`(逐值原样替换,空串替换为空串)、残留扫描(渲染后仍存 `{{...}}` 即未填或非变量结构)。服务暴露单原语方法 `ExtractVariables(string templateYaml)`(契约单原语豁免)供页面生成矩阵列;模板重解析时按变量名保留已填值(UI 细节实现时定)。

校验序(PreviewAsync 与 DeployAsync 入口同序,全部先于任何 K8s 调用):①Admin 检查(`IsAdmin`,非 Admin `PermissionException`,先于一切);②目标集群 ≥1 且全部存在(空 → `ValidationException`「请选择至少一个目标集群」;不存在 → `NotFoundException`,镜像 helm fleet);③矩阵完备性(`Extract` 的变量集 vs 矩阵 keys,缺者列出「集群 X 缺变量 y」;**UI 留空的格子提交空串 = 已填合法空值**,未填 = 矩阵缺 key——满足 spec 空串替换与未填拒绝两场景);④逐集群渲染;⑤残留 `{{...}}` 扫描(含 `{{if ...}}` 类结构 → `ValidationException` 列出残留占位符);⑥逐集群渲染产物 `KubernetesYaml` 解析(失败 → `ValidationException($"YAML 格式错误:{ex.Message}")`,仓库唯一例外口径)+ 单文档校验(多文档 / 空文档拒绝)+ kind 五族校验(解析产物类型为 `V1Deployment` / `V1StatefulSet` / `V1DaemonSet` / `V1ReplicaSet` / `V1ConfigMap` 之一,否则中文提示仅支持五族);⑦逐集群预取现值(失败隔离:该集群标注获取失败,不中断其余)。

### F6 判定口径:剥管提取共享 + 双侧规范化比较

`ClusterCompareService.StripServerMetadata` 提取为 `Application/Common/Yaml/ServerYamlSanitizer`(纯静态,`Sanitize(yaml, kind)`),调用方保留自己的 try/catch 与日志上下文;`ClusterCompareService` 两处调用点改调共享助手(行为不变,`ClusterCompareServiceTests` 回归护)。舰队模板判定:现值侧经 Sanitizer 剥管,渲染值侧同样过一遍(消除用户模板手写 creationTimestamp 等噪音,对称归一),判定「一致」= 两侧剥管后序列化文本的行序列相等;「新建」= 现值不存在;「更新」= 存在且行序列不等。页面行级 rows 由页面 `YamlDiff.Compute(剥管后现值, 剥管后渲染值)` 计算(与 /compare 同口径)——服务端只判四态(新建 / 更新 / 一致 / 获取失败),不做行级计算。预览返回的 CurrentYaml / RenderedYaml 均为剥管后文本,diff 视图两侧即判定依据,所见即所判。

### F7 下发执行:不信页面判定,逐集群重判 + 舰队审计双层

DeployAsync 入口重新执行 F5 全校验与逐集群预取重判(不信任页面传入的判定;预览与确认之间发生漂移时按当下事实执行:他人刚建同名对象 → 创建路径 Conflict 异常 → 该集群失败隔离)。逐集群执行:重判「新建 / 更新」→ 调 F3 既有路径 → 成功后在**同一集群 scope 内**写舰队审计:`AuditCategory` 随族(Configmap / Workload),`AuditAction.Create` / `Update`,描述含「舰队下发」+ 目标集群名 + 族与对象名(如「舰队下发:在集群 X 创建 Deployment Y」);「一致」跳过零写、「获取失败」跳过零写、失败不写(镜像 helm fleet 逐成功口径与 spec「一致集群跳过不写审计」)。进度 `IProgress<(int Current, int Total)>`(契约豁免)+ `Interlocked`;`CancellationToken` 透传 `ParallelOptions`,停机取消时已完成集群的结果与审计保持一致。结果 `FleetTemplateDeployResultViewModel`(逐集群行 + `SuccessCount` / `FailureCount` 计算属性,镜像 `HelmFleetDeployResultViewModel`)。

### F8 页面四态机与零持久化 UI 口径

页面私有状态机四态:①粘贴(裸 `textarea class="yaml-textarea"`,仓库 YAML 输入口径)+「解析变量」按钮;②矩阵(目标集群多选 + 变量格 `MudTextField`,空格 = 空串值;变量区 caption 注明「留空视为空串值」)+「生成预览」;③预览:逐集群卡片 = 判定徽标(`StatusBadge`:新建 / 更新 → `unknown`(琥珀),一致 → `online`,获取失败 → `offline`)+ `YamlDiffView`(左 = 剥管后现值、右 = 剥管后渲染值)+ 页面级「确认下发」;④执行态(进度行「已完成 / 总数」+ 按钮禁用,镜像「刷新全部」反馈模式)→ 汇总态(逐集群结果行:判定 + 成功 / 失败 + 中文消息)+「再下发一次」重置回粘贴态——**完成即重置,不残留可再次提交的模板与矩阵状态**(满足 spec 即传即用场景);离开页面状态自然丢弃。页面根 `Class="flex-auto"`(非表格页,不登记 `{feature}-table` flex-fill 规则组,对齐 /compare、/profile 形态)。异常统一 `ExceptionPresenter` 兜底;权限双保险:页面 attribute + Drawer Admin 段仅 Admin 渲染,强制在服务端入口(F5 ①)。

请求与视图模型:`FleetTemplatePreviewRequest` / `FleetTemplateDeployRequest`(同形:`TemplateYaml` / `ClusterIds` / 矩阵 `IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>>`,clusterId → 变量 → 值);`FleetTemplateAction` 枚举(Create / Update / Identical / FetchFailed,Application/Enums,镜像 `CompareKind` 附 `ToDisplayText` 中文扩展:新建 / 更新 / 一致 / 获取失败);`FleetTemplatePreviewItemViewModel`(ClusterId / ClusterName / Action / CurrentYaml / RenderedYaml / Message)+ `FleetTemplatePreviewResultViewModel`;`FleetTemplateDeployItemViewModel`(ClusterId / ClusterName / Action / Succeeded / Message)+ `FleetTemplateDeployResultViewModel`。

## Risks / Trade-offs

- [scope-per-cluster 并发正确性:scope 未释放或依赖未在容器注册 → 运行时解析失败] → 每单元 `using var scope`;服务测试以真实 ServiceProvider(ServiceCollection + 共享连接 SQLite + mock k8s 工厂 + `TestHttpContext` 的 `IHttpContextAccessor`)构造,scopes 间同库,断言审计落库;bUnit 栈 `AddFleetTemplatesStack` 封装同一注册面。
- [矩阵「留空 = 空串值」超出用户预期(以为会拒)] → 矩阵区 caption 明示;未填防线 = 矩阵缺 key 校验 + 渲染后残留 `{{...}}` 扫描双保险。
- [剥管遗漏某系统字段 → 误判「更新」] → 下发走更新路径幂等无害;剥管与克隆同源(同一助手)杜绝两套口径;`ClusterCompareServiceTests` 回归护。
- [预览与确认之间漂移] → DeployAsync 逐集群重判(不信页面);同名被抢建 → 创建路径 Conflict → 该集群失败隔离,不整批失败。
- [CompareDiffView 提取回归] → 渲染输出逐字节等价薄壳化,`CompareDiffViewTests` 断言不动;CSS 类名不迁移。
- [单文档限制过紧?] → 既有创建路径即单对象口径,多文档在路径内同样失败;前置校验给出明确中文提示,行为一致体验更好。
- [变量正则过宽 / 过窄(如 `{{a.b}}` 点号)] → 标识符集 `[A-Za-z0-9_.-]+` 覆盖常规变量名;非匹配结构一律走残留扫描拒绝,无静默吞掉路径。

## Migration Plan

零 schema、零配置、零模板文件:无新表(appsettings 不动,无 `EnsureCreated` 影响,**无需删库重建**,区别于 add-alert-center),无新 K8s 调用形态。部署即生效;回滚 = 回退构建,不留任何状态(即传即用)。升级路径:v2 模板库在链前加 CRUD,本变更一行不浪费。

# Tasks

> 说明:本文件在实施过程中经历过一次编码往返,个别行尾字符已按当下事实重述;任务语义与勾选状态以本文件为准。

## 1. Application 渲染核心与契约类型

- [x] 1.1 在 `Application/Services/FleetTemplateService.cs` 起骨架(主构造 `IClusterRepository` / `IHttpContextAccessor` / `IServiceScopeFactory` / `ILogger`)并同文件写纯静态助手 `FleetTemplateVariables`(Extract 有序去重提取 / Render 原样替换含空串 / 渲染后残留 `{{...}}` 扫描),以 `FleetTemplateServiceTests` 中的变量直测与 `ExtractVariables` 单原语方法覆盖(提取去重有序、空串替换不留裸占位符、`{{if ...}}` 类结构判残留)——`dotnet test --filter-class FleetTemplateServiceTests` 全绿
- [x] 1.2 建 `Application/Enums/FleetTemplateAction.cs`(Create/Update/Identical/FetchFailed + `ToDisplayText` 中文扩展,镜像 CompareKind)与 `FleetTemplatePreviewRequest` / `FleetTemplateDeployRequest` / 两对 Item/Result ViewModel(计数属性镜像 `HelmFleetDeployResultViewModel`;矩阵 `IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>>`)——`dotnet build` 0 错误、XML 中文注释契约齐、单原语方法豁免注释

## 2. 既有件提取:剥管助手与共享 diff 组件(行为不变)

- [x] 2.1 将 `ClusterCompareService.StripServerMetadata` 提取为 `Application/Common/Yaml/ServerYamlSanitizer` 纯静态 `Sanitize(yaml, kind)`(补写 apiVersion/kind 头保证两侧序列化形态一致),两处调用点改调、日志与异常上下文留在调用方——`ClusterCompareServiceTests` 与全量 `dotnet test` 回归全绿(剥管口径不变,仅头行由省略变固定)
- [x] 2.2 新建 `Web/Components/Common/YamlDiffView.razor`(参数 LeftTitle/RightTitle/LeftYaml/RightYaml(string?,null=不存在)/Rows/可选 HeaderActions/HintRow;三分支呈现 + 仅看差异切换 + 图例 +「两侧内容一致」提示,CSS 沿用全局 `.compare-diff-*` 类名),`CompareDiffView` 改薄壳(保留 Pair/Rows/OnCloned 契约与克隆按钮 / 提示 / 对话框,呈现体委托)——`CompareDiffViewTests` 全绿回归(五条既有断言零改动)

## 3. FleetTemplateService 预览管线

- [x] 3.1 实现入口校验序(Admin 检查 → 目标集群非空且存在 → 矩阵完备性列「集群 X 缺变量 y」→ 逐集群渲染 → 残留 `{{...}}` 扫描 → 逐集群解析 + 单文档 + 五族 kind 样验,「YAML 格式错误:…」口径)与单原语方法 `ExtractVariables(string)`,配 `FleetTemplateServiceTests` 校验矩阵(非 Admin 拒、集群空 / 不存在、缺变量、残留占位符含 `{{if ...}}`、多文档、kind=Ingress 拒、语法错误中文提示;断言零 K8s 调用)——`dotnet test` 新测试全绿
- [x] 3.2 实现 `PreviewAsync`:预循环串行 `GetAllForDashboardAsync` 解析集群 → `Parallel.ForEachAsync`(并发 4)逐集群 `CreateScope` 内经 `WorkloadService`/`ConfigMapService` 既有读方法预取现值 → `ServerYamlSanitizer` 双侧剥管 → 行序列相等判定四态 → 获取失败隔离(单集群异常标注 FetchFailed,不中断其余)——`FleetTemplateServiceTests`(真实 ServiceProvider:文件级 SQLite + 每作用域上下文 + mock k8s 路由工厂 + `TestHttpContext`;mock 读侧口径照抄 `ClusterCompareServiceTests`)覆盖新建 / 更新 / 一致 / 获取失败四态与失败隔离,全绿

## 4. DeployAsync 下发编排

- [x] 4.1 实现逐集群重校验 + upsert 分支:重走校验与预取重测(不信任页面判定)→「新建」调既有创建方法(`ConfigMapCreateRequest`/`WorkloadCreateRequest(clusterId, yaml)`)、「更新」调既有编辑方法(name/ns 取自渲染产物 metadata)、「一致 / 获取失败」跳过零写——测试断言:新建走创建路径(对象盖章 + 对象级审计 + k8s 调用形态)、更新走编辑路径、一致跳过零写零审计,全绿
- [x] 4.2 实现并发与结果编排:每集群 scope 内执行既有路径 + 成功后同 scope 写舰队审计(`AuditCategory.Configmap`/`Workload` + `AuditAction.Create`/`Update`,描述含「舰队下发」与集群名)、失败中文消息记入结果行不中断、`Interlocked` 进度回调「已完成/总数」、`CancellationToken` 透传(已完成集群结果与审计保持一致)、返回 `FleetTemplateDeployResultViewModel`——测试覆盖:一集群失败其余成功、舰队审计描述断言、进度回调总数推进、重复下发第二次全「一致」幂等、非 Admin 抛权限异常零调用,全绿
- [x] 4.3 `AddApplicationServices()` 注册 `FleetTemplateService`(scoped)——`dotnet build` 0 错误、全量 `dotnet test` 全绿

## 5. Web 页面与导航

- [x] 5.1 写 `Web/Components/FleetTemplates/Pages/FleetTemplates.razor`(`@page /fleet-templates` + `@attribute [Authorize(Roles = "Admin")]`,四态流:粘贴 `yaml-textarea` + 解析变量 → 集群多选 + 变量矩阵 MudTextField(留空 = 空串值口径)→ 逐集群卡片判定徽标 + `YamlDiffView`(左现值右渲染值)→ 确认对话框 + 执行进度行「已完成 / 总数」+ 禁用 → 汇总行成功/失败计数 +「再下发一次」重置回粘贴态;页面根 `flex-auto`,异常经 `ExceptionPresenter`)——新增 `FleetTemplatesPageTests` 覆盖表单骨架 / 按钮禁用态 / 矩阵呈现 / 预览行渲染 / 校验错误不出行,全绿(矩阵驱动经 reflection + 手动 Render,MudSelect 菜单在 bUnit 下不可交互的既知口径)
- [x] 5.2 `BunitServiceExtensions` 加 `AddFleetStack`(mock k8s 工厂 + `AddClientCache` + `FleetTemplateService`/`WorkloadService`/`ConfigMapService` + 集群栈 Guard/Audit/TestHttpContext/共享 SQLite 注册体内 scope 内解析可用)——栈内首个页面测试通过、既有 bUnit 栈测试回归全绿
- [x] 5.3 Drawer AuthorizeView Admin 段加 NavLink「舰队模板」(`/fleet-templates`,Difference 图标,顺位:跨集群对照 → 舰队模板 → 告警中心 → 账号管理)——bUnit 断言 Admin 可见 / Member 不可见(Drawer 相关测试回归全绿)

## 6. 集成与验证

- [x] 6.1 更新 `AGENTS.md`:能力覆盖地图 + Architecture notes 加 fleet-templates 条目(路由 / 四态 / scope 并发模型 / 审计双层 / 零库落注释)、Commands 段测试基线数刷新——条目与 spec.md 语义一致
- [x] 6.2 `dotnet build MultiClusterMgmtSys.slnx` 0 错误 + `dotnet test MultiClusterMgmtSys.Tests` 全绿(基线 1207 + 新增)+ `./coverage.ps1` 四程序集合并行覆盖率 ≥ 75%
- [ ] 6.3 端到端手工走查(dev-run):粘贴带变量模板 → 矩阵留空 / 填值两分支 → 预览四态标注与双栏 diff → 确认下发进度与汇总 → 审计含「舰队下发」→「再下发一次」重置无残留;Member 直达 `/fleet-templates` 被服务端拒绝;卸载页重入无模板残留

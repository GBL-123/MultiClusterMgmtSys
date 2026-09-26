# Design

## Context

动机与范围见 `proposal.md`;需求契约见 `specs/`。以下是塑造本方案的既有事实:

- **仓库内零 Helm 痕迹**:唯一命中是 `.dockerignore` 的 `**/charts`。现有 K8s 访问全部经 `IClusterClientCache` + `KubernetesClient 19.0.2`,与 Helm 无任何交集。
- **用户环境约束(已确认)**:helm 命令目前只在集群节点/跳板机上可用,应用主机没有;用户确认**可以随本系统分发 helm 二进制**,因此执行引擎 = 官方 helm CLI 子进程。
- **Helm 版本时间点(2026-09)**:Helm 4 为当前主线(2025-11 发布),Helm 3 最后一个功能版 3.22.0 于 2026-09-09,安全补丁至 2027-02;Helm 4 可直接读取/升级/回滚 Helm 3 创建的 release。**Helm 4 有两个行为差异**:flag 改名(`--atomic`→`--rollback-on-failure`)与新建 release 默认 server-side apply(升级/回滚沿用 release 既有 apply 方式)。
- **无 EF migrations**:`db.Database.EnsureCreated()`,`EnsureCreated` 不补建表 → 本次新增归属表意味着**升级需删库重建**,与本仓 `add-cluster-dashboard` 同款影响。
- **容器运行约束**:镜像以非 root(`$APP_UID`)运行,`/app` 不可写,仅 `db/`、`logs/` 挂载可写;helm 默认向 `$HOME/.cache/helm` 等写缓存/config/data → **必须钉 `HELM_*` 环境变量到可写临时目录**,否则首次执行即失败。
- **Blazor Server 交互模型**:`InputFile` 是 `ui-theme` 已文档化的例外;其 `OpenReadStream` 默认单文件上限 500KB,chart 包可达数 MB。
- **Helm 不记录安装者**:release Secret 无用户身份,归属必须由系统记账;`helm list -o json` 输出不含 release labels,无法仅靠标签做全员可见的归属判定。
- **测试与分层契约**:服务以端口隔离外部依赖(见 `architecture-layering`);测试以 mock/假实现为边界(见 `unit-testing`);服务入参收拢为 Request、输出为 ViewModel(见 `service-contracts`);操作者身份经 `IHttpContextAccessor` 获取。

## Goals / Non-Goals

**Goals:**

- 用官方 helm 引擎提供 release 完整生命周期(安装/升级/回滚/卸载)与只读视图,兼容性零风险。
- 归属权限在服务端强制、失败关闭(无主 = 仅 Admin),UI 只是第二道门。
- CLI 运行时安全可测:参数表执行、无 shell、有界超时、取消杀进程树、凭据不落日志、临时文件必清理。
- schema 变更只加一张表;chart 包不落库、不加后台队列,把改动面压到最小。

**Non-Goals:**

- 不做 chart 仓库管理、repo 目录、OCI 拉取;来源只有上传 `.tgz`。
- 不做 chart 包持久化库(即传即用),不做「同一包批量部署多集群」。
- 不做后台作业队列与进度持久化;操作同步等待,断线结果不补投。
- 不做 Helm 输出的流式实时进度;不做 release 资源对象级下钻(那是各资源页职责)。
- 不做进程内按 release 串行锁;不做归属对账器(外部操作漂移只做启发式降级)。
- 不做 helm 命令的「dry-run 预览」界面(v1 用 `--wait`/失败回显替代;留后续)。

## Decisions

### D1 引擎 = 官方 helm CLI 子进程,随系统镜像分发

**选择**:应用以子进程方式执行钉定版本的 helm 二进制;二进制随本系统 Docker 镜像分发,开发机经 PATH 或 `Helm:CliPath`。

**备选**:(a) HelmSharp 等 .NET 托管 SDK(进程内重实现);(b) 在目标集群里以 Job 方式跑 helm 镜像。

**理由**:用户环境已确认二进制可随系统分发,这条路用的是**官方引擎**,不存在模板/Sprig/生命周期语义的兼容性长尾;而 (a) 是年轻重实现(单维护者、下载量千级), (b) 要自建 Job 生命周期、镜像分发、RBAC 与日志回收,工程量与运维成本都显著更高。代价(镜像多一个二进制、开发机前置)由 D13 的钉版与文档化承接。

### D2 版本钉定 Helm 4.x

**选择**:镜像内钉定一个 Helm 4.x 具体版本;命令构建按 Helm 4 的 flag 名。

**备选**:钉 Helm 3(团队现有流水线锁定 3 时)。

**理由**:Helm 3 已进入 EOL 倒计时(2027-02 停更);Helm 4 能直接管理 Helm 3 创建的 release(升级/回滚沿用既有 apply 方式),迁移成本几乎为零。**互操作注意**:Helm 4 新建 release 默认 server-side apply,若团队其他流水线仍以 Helm 3 手工操作同一 release,存在 apply 方式交叉的风险(见 Risks)。

### D3 端口分层:CLI 运行时与纯逻辑分离

**选择**:

- `Application/Abstractions/IHelmCliRunner.cs`(端口):接收「helm 参数表 + 待物化文件(chart 包/values/kubeconfig)+ 超时」,返回退出码与 stdout/stderr。
- `Infrastructure/Helm/`:实现 runner(经薄封装 `IProcessExecutor` 调用 `Process.Start`)与临时文件物化、环境变量注入。
- `Application/Services/HelmCommandBuilder.cs`(纯):把 Request 映射为参数表。
- `Application/Services/ChartPackageReader.cs`(纯):解 tgz 取 `Chart.yaml`/`values.yaml`。
- `Application/Services/HelmErrorTranslator.cs`(纯):退出码 + stderr → 业务异常。
- `Application/Services/HelmService.cs`:编排(取集群、物化、执行、解析、审计、归属)。

**备选**:在服务里直接 `Process.Start`;引入第三方进程执行库。

**理由**:进程不可 mock,直接写进服务会让整个服务层失去可测性;把「参数怎么拼、输出怎么解、错误怎么翻」都留在 Application 的纯函数里,单测覆盖这些即可;Infrastructure 的 runner 只做机械动作,由假 executor 覆盖。这正是本仓 `IYamlValidator`/`K8sExceptionMapper` 的既有模式。

### D4 凭据物化:统一走临时 kubeconfig

**选择**:每次操作生成临时 kubeconfig 并经 `--kubeconfig` 传入。KubeConfig 型连接直写既有文本;Token 型连接**合成**最小 kubeconfig(server、token、`insecure-skip-tls-verify` 取自 `SkipTlsVerify`)。

**备选**:helm 的 `--kube-apiserver/--kube-token/--kube-insecure-skip-tls-verify` 全局 flags。

**理由**:两种连接方式走**同一条代码路径**,合成逻辑是纯文本生成、完全可单测;全局 flags 在不同 Helm 版本间存在面差异,且把凭据摊在命令行参数上(进程列表可见),不如文件可控。文件权限与清理见 D5。

### D5 临时文件与环境隔离:每操作独立目录,finally 清理

**选择**:每次操作创建 `{temp}/mcm-helm/{guid}/`,写入 kubeconfig/values/chart 包;子进程环境变量钉 `HELM_CACHE_HOME`、`HELM_CONFIG_HOME`、`HELM_DATA_HOME`、`HELM_REPOSITORY_CACHE`、`HELM_REPOSITORY_CONFIG`、`HELM_REGISTRY_CONFIG`、`KUBECONFIG` 指向该目录;操作结束(成功/失败/取消)在 `finally` 删除目录;应用启动时清扫 `{temp}/mcm-helm/` 下超过 24 小时的遗留目录(best-effort,仅日志)。

**理由**:容器内非 root 且 `$HOME` 不可写,helm 默认目录会直接失败;每操作独立目录同时解决了并发操作互不污染与「失败也清理」的问题。启动清扫兜底进程崩溃/掉电留下的残留。

### D6 命令与解析:优先 `-o json`,fixture 驱动解析

**选择**:

| 操作 | 命令(要点) | 解析 |
|---|---|---|
| 列表 | `helm list -A -o json` | JSON |
| 详情 | `helm status <name> -n <ns> -o json` | JSON |
| 历史 | `helm history <name> -n <ns> -o json` | JSON |
| 用户 values | `helm get values <name> -n <ns> -o yaml` | YAML 文本直读(编辑器预填与展示用) |
| manifest | `helm get manifest <name> -n <ns>` | YAML 文本 |
| 安装 | `helm install <name> <tgz> -n <ns> [-f <values>] [--create-namespace] [--wait --timeout <t>]` | 退出码 + 事后重查 |
| 升级 | `helm upgrade <name> <tgz> -n <ns> [--reuse-values \| -f <values>] [--wait --timeout <t>]` | 同上 |
| 回滚 | `helm rollback <name> <rev> -n <ns>` | 同上 |
| 卸载 | `helm uninstall <name> -n <ns> [--keep-history]` | 同上 |

install/upgrade/rollback/uninstall 的 stdout 是给人看的文本,不做结构化解析:成功即按需重查列表/详情,失败交给 `HelmErrorTranslator`。JSON DTO 用 `System.Text.Json` 手写最小模型;**解析测试以真实 Helm 4 输出裁剪出的 fixture 驱动**,实现首步先抓 fixture。

**理由**:`-o json` 是 helm 对不同命令支持不一但覆盖面最大、最稳的结构化出口;文本输出跨版本措辞会变,不值得依赖。

### D7 归属记账:新增 `HelmReleaseOwnership` 表

**选择**:实体字段 `Id`、`ClusterId`(随 `ClusterInfo` 级联删除)、`Namespace`、`ReleaseName`、`OwnerUserId`(Identity int 主键,不建导航外键)、`OwnerUserName`(快照)、`InstalledAt`、`InstalledRevision`;唯一索引 `(ClusterId, Namespace, ReleaseName)`。安装成功后写入;系统内卸载成功后删除;列表查询时与归属表按三键左连接。

**备选**:(a) 用 Helm release labels 记 owner;(b) 从审计记录派生归属。

**理由**:(a) 被 `helm list -o json` 不含 labels 卡死——要做全员可见的归属判定就得逐 release 解析 Secret,等于引入我们刻意避免的第二条读路径;且外部安装的 release 根本没有标签,标签值还受合法性约束。(b) 目标文本是给人看的中文描述,解析脆弱且查询昂贵。独立表还能承载 `InstalledRevision` 用于外部重装降级:当前 revision 小于记录值即视为已被系统外重装,降级为无主。

**写入顺序与失败姿态**:先执行 helm,成功后写归属;归属写库失败仅记警告并放行(该 release 自然落入「无主 = 仅 Admin」,失败关闭,不会把别人的 release 误判成自己的)。

### D8 权限强制:服务端判定 + ViewModel 携带 `CanOperate`

**选择**:`HelmService` 经 `IHttpContextAccessor` 取当前用户(取不到身份且操作需要归属判定时抛 `PermissionException`);对升级/回滚/卸载执行 `isAdmin || ownership.OwnerUserId == currentUserId`;不满足抛 `PermissionException`(中文)。读取列表/详情时同一规则计算 `CanOperate` 放入 ViewModel,UI **只按 `CanOperate` 渲染动作**,不做任何角色/归属判断。列表不返回也不展示安装者列。

**备选**:UI 自行组合 `AuthorizeView` + 归属查询。

**理由**:归属不是角色,`AuthorizeView` 表达不了;服务端判定是唯一可信来源,UI 只是投影。`CanOperate` 进 ViewModel 也让 bUnit 测试可以直接断言按钮分支。

### D9 上传通道:`InputFile` + 立即落盘 + 50MB 上限

**选择**:对话框用 `InputFile` 选包;`OpenReadStream(maxAllowedSize)` 以 50MB(常量,可配置)为上限,选中即**立即**拷贝到本操作的临时目录;元数据解析与后续操作都基于该临时文件;对话框放弃或操作结束清理。

**备选**:专用 HTTP 上传端点(multipart)。

**理由**:`InputFile` 已是 `ui-theme` 文档化的例外,不引入新面;立即落盘规避「文件引用在后续事件循环中失效」并让重解析/重试都便宜。若实测中 Blazor Server 电路对多 MB 文件不可靠,后备方案是加一个最小上传端点——这是**实现期验证项**,不影响契约与任务拆分。

**包解析安全**:`ChartPackageReader` 用 `GZipStream` + `TarReader` **在内存中**读条目,不落盘解压,天然没有路径穿越面;只取 `<root>/Chart.yaml` 与 `<root>/values.yaml`;`Chart.yaml` 缺 `name`/`version` 或不可解析即拒绝。`Chart.yaml` 声明了 dependencies 而包内无 `charts/` 时,对话框给出明确警告(不拦截,交给 helm 报错)。

### D10 长操作:同步等待,默认不 `--wait`

**选择**:安装/升级默认不带 `--wait`(资源创建即返回);高级选项可勾选等待就绪,默认 `--timeout 5m`;回滚/卸载不带 wait。进程级超时 = helm 超时 + 60 秒余量(非 wait 操作默认 120 秒)。操作期间按钮进入忙碌态禁用;断线后服务端任务继续完成,用户刷新可见结果,不补投提示。

**备选**:后台作业队列 + 状态持久化 + 进度展示。

**理由**:v1 用同步等待换取整套作业基础设施的零成本;`--wait` 本身可选项,大多数安装不阻塞。后台队列与「同一 release 并发操作」的强串行化都列为后续,不进本次范围。

### D11 错误翻译:模式映射 + 新异常 `HelmOperationException`

**选择**:`HelmErrorTranslator` 按 stderr 模式映射:`release ... already exists`/`cannot re-use` → `ConflictException`;`not found`/`no releases found` → `NotFoundException`;`forbidden`/`unauthorized` → `PermissionException`;`unreachable`/`connection refused`/TLS 错误 → `ClusterUnreachableException`;超时(进程级) → `ClusterUnreachableException`;无法归类 → 新增 `Domain/Exceptions/HelmOperationException`(UserMessage 通用中文)。原始 stderr 只进 `LogWarning`,不进 UserMessage。

**备选**:兜底用 `ValidationException`;或把 stderr 原样抛给用户。

**理由**:helm 的失败类别与既有异常体系一一对得上(冲突/未找到/权限/不可达),对得上就复用;对不上的归入专用异常而不是硬塞语义不符的 `ValidationException`。stderr 可能包含文件路径、集群地址甚至凭据片段,不能直出;**但日志必须留全**,否则线上排障无据。

### D12 页面、路由与组件落位

**选择**:

- 页面:`Web/Components/Helm/Pages/Helm.razor`(路由 `/helm`,集群上下文,复用 `ClusterSelectSidebar`);`HelmReleaseDetail.razor`(路由 `/helm/releases/{clusterId:int}/{namespace}/{name}`);共享组件在 `Web/Components/Helm/Shared/`(列表表、Values/Manifest 卡、历史表、四个对话框)。
- 列表列:名称、命名空间、Chart、版本(chart 版本 + appVersion)、Revision、状态、更新时间、操作;无安装者列。状态用既有 `StatusBadge`;操作按钮只在 `CanOperate` 时渲染。
- 详情:Tabs(概览/Values/Manifest/历史),沿用既有详情页 tab 样式契约;Manifest 复用 YAML 查看卡片;Values 编辑复用 `yaml-textarea`。
- 安装对话框:上传包 → 元数据展示 → release 名(默认 chart 名)→ 命名空间下拉(复用既有 `GetNamespacesAsync` 模式)→ values(textarea,初值取包内 `values.yaml`)→ 高级(create-namespace / wait / timeout)。
- 升级对话框:上传新包(展示旧 chart 版本 vs 新版本)→ values 模式(默认沿用现存用户 values 经 `--reuse-values`;切换「重新编辑」时预填 `helm get values` 结果并以 `-f` 提交)。
- 回滚对话框:历史列表选 revision + 确认;卸载对话框:确认 + 可勾选保留历史。
- 导航:Drawer 顶层新增「应用管理」,置于「事件管理」之后、账号管理之前,`Match=NavLinkMatch.Prefix`。

**理由**:完全套用既有资源页骨架(集群选择 + 表格 + 详情 Tabs + 对话框),把新范式面压到零;「无安装者列」由需求方明确。

### D13 二进制分发与配置

**选择**:Dockerfile 在 build 阶段按 `TARGETARCH` 从 `get.helm.sh` 下载钉定版本 tarball、校验 `.sha256sum`、把 `helm` 拷入 runtime 镜像 `/usr/local/bin/helm`;`appsettings.json` 增加 `Helm:CliPath`(默认 `helm`,PATH 查找);开发机前置(安装 Helm 4)与升级指引写入 AGENTS.md。

**备选**:`COPY --from=alpine/helm:<tag>`(依赖镜像 tag 存在且与基础镜像 libc 相容);把二进制提交进仓库(体积与许可管理都不合适)。

**理由**:下载 + 校验把版本与哈希都显式钉在 Dockerfile 里,升级路径清晰、可审计;`alpine/helm` 是否同步发布 4.x tag 不确定,不作为唯一来源。开发态 PATH 查找 + `Helm:CliPath` 覆盖两者兼顾。

### D14 审计口径

**选择**:新增 `AuditCategory.Helm` 与 `AuditAction.Install/Upgrade/Rollback/Uninstall`;目标格式 `Helm: <动作中文> <release>(集群 <集群名> / 命名空间 <ns>)`;审计记录实际操作者(Admin 卸载他人 release 时记 Admin)。

**理由**:与既有审计契约的事件/类别/动作口径一致;归属表只管权限,追溯仍看审计(因此列表不展示安装者也不损失追溯能力)。

### D15 测试策略

**选择**:

- `HelmCommandBuilder`:纯参数表断言(含 `-o json`、`--kubeconfig`、`--reuse-values`/`-f`、`--create-namespace`、`--wait --timeout`)。
- `ChartPackageReader`:内存 `TarWriter`+`GZipStream` 造包,覆盖合法/缺 Chart.yaml/坏归档/依赖警告/values 预填。
- `HelmService`:假 runner 喂 JSON fixture,断言解析、ViewModel、`CanOperate`、异常翻译、审计与归属写入/删除、无主与外部重装降级。
- 权限矩阵:Admin/成员本人/成员他人/无主/无身份五种输入。
- `HelmCliRunner`:假 `IProcessExecutor`,断言环境变量、`ArgumentList`、取消时杀进程树、临时目录成功/失败/取消均清理。
- kubeconfig 合成:KubeConfig 透传、Token 合成、SkipTlsVerify 两态。
- 归属仓库:`SqliteDbFactory` 覆盖唯一索引、级联删除、三键查询。
- bUnit:页面壳(未选集群/离线/空态)、`CanOperate` 按钮分支、对话框渲染、导航项。
- 不启动真实 helm;`dotnet test` 全绿、覆盖率门禁 75% 不降、架构测试通过。

## Risks / Trade-offs

- **[Helm 4 与团队 Helm 3 流水线交叉操作同一 release]** → Helm 4 升级/回滚沿用 release 既有 apply 方式,新建 release 才用 SSA;在 AGENTS.md 记录互操作说明;若确认需要与旧流水线完全一致,可在命令构建处统一加 `--server-side=false`(一处开关,不涉契约)。
- **[新增归属表需删库重建,丢失已登记集群与凭据]** → 既定流程,AGENTS.md 补记;影响仅开发态与自建部署;回滚同样删库重建。
- **[归属记录与集群真实状态漂移(系统外操作)]** → `InstalledRevision` 启发式降级为无主(失败关闭);系统内操作始终同步归属;对账器列为后续。
- **[helm 二进制带来镜像更新责任与攻击面]** → 版本与 sha256 钉在 Dockerfile;升级指引入 AGENTS.md;镜像是唯一分发路径,不依赖宿主机。
- **[Blazor 断线后长操作结果不展示]** → 操作本身在服务端完成;列表/详情可刷新查看;忙碌态防重复提交;后台作业列为后续。
- **[上传大包受电路/内存限制]** → 50MB 上限 + 立即落盘 + 超限中文报错;实测不可靠时切 HTTP 端点(实现期验证项)。
- **[无法归类的 helm 失败信息对用户过糙]** → `HelmOperationException` 给通用中文,stderr 全量进日志;宁可少说不可泄露。
- **[同 release 并发操作]** → helm 自身对 release 存储做版本校验,冲突以 `ConflictException` 呈现;v1 不加进程内锁,列后续。
- **[`--wait` 超时默认值不适合所有 chart]** → 高级选项可调;超时翻译为不可达语义并提示。

## Migration Plan

1. 停止应用。
2. 删除 `MultiClusterMgmtSys.Web/db/` 下的库文件(SQLite 主文件与 `-shm`/`-wal` 一并删除)。
3. 部署新版本镜像(内置钉版 helm)并启动:`EnsureCreated` 重建含 `HelmReleaseOwnership` 的完整 schema,并按既有逻辑播种管理员。
4. 重新录入集群与凭据(或从备份旧库比对重建)。
5. 开发机安装 Helm 4 并确保在 PATH(或配置 `Helm:CliPath`)。
6. 验证:上传一个 chart 包安装到某集群 → 列表出现 → 用成员账号确认无法操作他人 release。

**回滚**:还原代码 → 删除库文件 → 启动重建。新增表在回滚后的 schema 中不存在,无数据转换;损失的只是归属记录(可在升级前备份旧库留档)。

## Open Questions

- 上传通道是否最终需要 HTTP 端点(`InputFile` 实测后定);纯实现方式,不影响规格、方案与任务拆分。
- 列表是否提供「显示全部状态(含 superseded/uninstalled)」开关;默认隐藏已满足「已安装」语义,纯 UX 增量。
- 页面与导航的中文命名(「应用管理」/「Helm 应用」);纯文案,实现时定。

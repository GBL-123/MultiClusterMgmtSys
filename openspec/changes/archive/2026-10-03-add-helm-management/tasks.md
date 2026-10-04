# Tasks

## 1. Helm 二进制分发与运行配置

- [x] 1.1 在 `MultiClusterMgmtSys.Web/Dockerfile` 的 build 阶段按 `TARGETARCH` 下载钉定版本 Helm 4.x tarball 并校验 `.sha256sum`,将 `helm` 拷入 runtime 镜像 `/usr/local/bin/helm`;验证:`docker build` 成功且 `docker run --rm <image> helm version` 输出钉定版本(本地无 Docker 时记录构建日志与镜像内文件清单)
- [x] 1.2 `appsettings.json` 增加 `Helm:CliPath`(默认 `helm`)与 `Helm:MaxPackageBytes`(默认 52428800),经选项类绑定并在 DI 注册;验证:构建通过 + 选项默认值/覆盖值单测
- [x] 1.3 AGENTS.md 补记:镜像内 Helm 分发方式、版本升级指引(改 Dockerfile 版本与哈希)、开发机需安装 Helm 4 或配置 `Helm:CliPath`;验证:按文档在新机器走查前置步骤可复现

## 2. CLI 运行时(进程执行、隔离、凭据)

- [x] 2.1 定义 `IHelmCliRunner` 端口与 `HelmRunRequest`/`HelmRunResult`(参数表、待物化文件、超时、退出码与 stdout/stderr),含中文 XML 注释;验证:`dotnet build` 0 错误且 CS1591 零命中
- [x] 2.2 实现 `HelmCliRunner`(Infrastructure):每操作独立临时目录、写入 chart 包/values/kubeconfig、钉 `HELM_CACHE_HOME`/`HELM_CONFIG_HOME`/`HELM_DATA_HOME`/`HELM_REPOSITORY_CACHE`/`HELM_REPOSITORY_CONFIG`/`HELM_REGISTRY_CONFIG`/`KUBECONFIG`、经 `Helm:CliPath` 与 `ArgumentList` 启动、进程级超时与取消时终止进程树、`finally` 清理目录;验证:以假 `IProcessExecutor` 单测覆盖环境变量、参数表、取消终止、成功/失败/取消均清理
- [x] 2.3 实现真实进程执行薄封装(`Process.Start` + 异步读 stdout/stderr + 进程树终止),与假实现分离;验证:构建通过 + 开发机冒烟执行 `helm version` 成功
- [x] 2.4 凭据物化:KubeConfig 型直写文本、Token 型合成最小 kubeconfig(server/token/`insecure-skip-tls-verify` 取自 `SkipTlsVerify`);验证:纯单测覆盖两种连接方式与 SkipTlsVerify 两态,断言合成内容
- [x] 2.5 应用启动清扫受管临时根目录下超 24 小时的遗留目录(best-effort、仅记日志);验证:单测造旧/新目录断言只清理旧的
- [x] 2.6 二进制缺失/不可执行时抛出带中文消息的业务异常且不崩溃;验证:单测假执行器返回启动失败断言异常类型与消息

## 3. 命令构建、输出解析与错误翻译

- [x] 3.1 抓取或按 Helm 4 输出结构构造 fixture(`list`/`status`/`history`/`get values`/`get manifest` 与典型失败输出),放入测试项目;验证:fixture 被解析单测引用且 `dotnet test` 可读
- [x] 3.2 最小 JSON DTO 与解析器(列表/状态/历史;values 按 `-o yaml` 文本直读、不做 JSON 解析);验证:单测以 fixture 断言解析结果字段
- [x] 3.3 `HelmCommandBuilder`(纯):九类命令的参数表生成,含 `-o json`、`--kubeconfig`、`-n`、`--create-namespace`、`--wait`/`--timeout`、`--reuse-values`/`-f`、`--keep-history` 与 Helm 4 flag 名;验证:纯单测逐命令断言参数表
- [x] 3.4 新增 `Domain/Exceptions/HelmOperationException.cs` 并在 `HelmErrorTranslator` 实现 stderr 模式映射(冲突/未找到/权限/不可达/超时/兜底),原始 stderr 只进日志;验证:单测覆盖各模式与兜底,断言 UserMessage 不含 stderr 原文

## 4. Chart 包解析与上传校验

- [x] 4.1 `ChartPackageReader`(纯):以 `GZipStream`+`TarReader` 内存读取 `Chart.yaml`/`values.yaml`,返回名称、版本、appVersion、描述、依赖声明、values 文本与警告;验证:单测(内存造包)覆盖合法包、缺 `Chart.yaml`、坏归档、声明依赖但缺 `charts/` 的警告
- [x] 4.2 上传上限校验(超 `Helm:MaxPackageBytes` 拒绝并中文提示);验证:单测超限拒绝、边界值通过

## 5. 归属记账(数据模型与仓库)

- [x] 5.1 新增 `Domain/Entities/HelmReleaseOwnership.cs`(`ClusterId`/`Namespace`/`ReleaseName`/`OwnerUserId`/`OwnerUserName`/`InstalledAt`/`InstalledRevision`,中文 XML 注释),在 `ApplicationDbContext` 注册 DbSet、唯一索引 `(ClusterId, Namespace, ReleaseName)` 与随 `ClusterInfo` 级联删除;验证:`dotnet build` 0 错误 + 仓库测试断言唯一索引与级联
- [x] 5.2 新增 `IHelmReleaseOwnershipRepository` 端口与 `Infrastructure/Persistence` 实现(按三键取单条、按集群批量取、新增、删除);验证:经 `SqliteDbFactory` 的仓库测试覆盖四类操作
- [x] 5.3 在 `ServiceHarness` 等测试脚手架接入归属仓库;验证:构建通过且既有测试全绿
- [x] 5.4 AGENTS.md 补记 `HelmReleaseOwnership` 表与「升级需删库重建」要求;验证:文档与 schema 一致

## 6. HelmService:读路径、展示映射与权限投影

- [x] 6.1 新增 `Application/ViewModels/Mappings/HelmDisplayText.cs`(状态中文/英文/CSS 映射,未登记回退原文);验证:单测覆盖全部已知状态与未登记回退
- [x] 6.2 `HelmService` 骨架 + `ListReleasesAsync`/`GetReleaseDetailAsync`/`GetReleaseHistoryAsync`/`GetReleaseValuesAsync`/`GetReleaseManifestAsync`(经 `IClusterRepository` 取集群),ViewModel 含 `CanOperate`;验证:假 runner 单测断言解析、映射与 `CanOperate`
- [x] 6.3 读取路径异常翻译接入(`NotFoundException`/`ClusterUnreachableException` 等);验证:单测各失败 fixture 断言异常类型与中文消息
- [x] 6.4 `HelmService` 注册进 `AddApplicationServices()`,`HelmCliRunner` 注册进 `AddInfrastructure()`;验证:应用启动无 DI 解析错误

## 7. HelmService:写路径(安装/升级/回滚/卸载)

- [x] 7.1 扩展 `AuditCategory` 增加 Helm,`AuditAction` 增加安装/升级/回滚/卸载;验证:构建 + `AuditService` 相关测试全绿
- [x] 7.2 实现安装:包暂存、命令构建、执行、成功后写归属与审计,失败不写归属;验证:单测覆盖成功(归属 + 审计目标格式)、名称冲突、渲染失败
- [x] 7.3 实现升级:values 沿用(`--reuse-values`)与重新编辑(`-f`)两模式,归属不变;验证:单测断言两种参数表、归属不新增不删除、审计写入
- [x] 7.4 实现回滚(选 revision)与卸载(可选保留历史):卸载成功删除归属;验证:单测断言参数表、归属删除、审计写入
- [x] 7.5 写操作统一权限校验:Admin 放行;成员仅本人;无主仅 Admin(含外部重装 revision 降级判定),违规抛 `PermissionException`;验证:五类身份矩阵单测(Admin/本人/他人/无主/无身份)

## 8. Web 页面、对话框与导航

- [x] 8.1 `/helm` 列表页(集群选择栏、表格列、状态徽章、空态、离线提示、操作按钮按 `CanOperate`);验证:bUnit 断言未选集群/离线/空态/有数据与按钮分支
- [x] 8.2 release 详情页(路由、Tabs 概览/Values/Manifest/历史、Manifest 复用 YAML 查看卡);验证:bUnit 断言各 Tab 分支与不存在态
- [x] 8.3 安装对话框(上传 → 元数据 → release 名/命名空间/values 预填 → 高级选项);验证:bUnit 断言渲染与字段联动 + 用真实多 MB `.tgz` 在 `dotnet run` 下实测上传,超限或电路不稳则按 design D9 切换 HTTP 端点并记录结论
- [x] 8.4 升级/回滚/卸载对话框(新包与旧版本对照、values 模式切换、revision 选择、保留历史勾选、确认);验证:bUnit 断言各分支
- [x] 8.5 Drawer 新增「应用管理」入口(「事件管理」之后)与 `app.css` Helm 样式(`.helm-table` 登记进 flex-fill 规则组、页面根节点 `flex-auto`);验证:bUnit 断言导航 + `dotnet run` 走查表格拉伸与视觉
- [x] 8.6 AGENTS.md 补记 Helm 页面/路由/操作与权限口径;验证:文档与实现一致

## 9. 集成验收

- [x] 9.1 `dotnet build` 0 错误、`dotnet test MultiClusterMgmtSys.Tests` 全绿(测试数量不低于基线)、`ArchitectureTests` 通过
- [x] 9.2 `./coverage.ps1` 四程序集合并行覆盖率 ≥75% 门禁不降
- [ ] 9.3 端到端手工验收:删库重建 → 启动 → Admin 上传 `.tgz` 安装到测试集群 → 列表/详情/历史正确 → 成员账号可见但无法操作他人 release → Admin 升级/回滚/卸载 → 审计日志出现 Helm 记录;验证:按步骤走查通过

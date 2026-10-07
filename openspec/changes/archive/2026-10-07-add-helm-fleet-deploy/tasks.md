# Tasks

## 1. 请求与视图模型契约

- [x] 1.1 `Application/Requests/` 新增 `HelmFleetDeployRequest`(Namespace/ReleaseName/ChartPackage/ValuesYaml/CreateNamespace/Wait/ClusterIds),补中文 XML 注释,验证 CS1591 零命中 + `dotnet build` 通过
- [x] 1.2 `Application/ViewModels/` 新增 `HelmFleetDeployResultViewModel`(Items + 成功/失败计数)与逐集群结果项 `HelmFleetDeployItemViewModel`(ClusterId/ClusterName/Action/Succeeded/Message),`Application/Enums/` 新增 `HelmFleetDeployAction`(Install/Upgrade),验证 `dotnet build` 通过

## 2. 服务层:批量下发

- [x] 2.1 `HelmService.DeployToFleetAsync`:Admin 强制(非 Admin LogWarning + 中文 `PermissionException` 且不调 Helm)→ 前置校验(集群多选非空、集群存在经 `GetAllForDashboardAsync` 过滤、包大小上限、release 名与命名空间 DNS-1123)→ 返回 `HelmFleetDeployResultViewModel`,验证新单测:权限拒绝零 Helm 调用、各校验失败抛中文异常
- [x] 2.2 执行语义:常量 4 有界 `Parallel.ForEachAsync` → 逐集群 `helm status` 预检(NotFound → `BuildInstall` 统一 values/create-ns/wait;存在 → `BuildUpgrade` 统一 values 不带 reuse)→ 失败隔离(异常捕获转失败项,LogWarning 含集群与 release 上下文)→ 进度 `IProgress<(int, int)>` 逐集群回报,验证单测:install/upgrade 分支参数表断言(FakeHelmCliRunner.Handler 按调用序编排)、混合成败整批不中断、进度序列
- [x] 2.3 归属与审计:安装路径成功 `TryUpsertOwnershipAsync`(Owner=当前 Admin、InstalledRevision=1)+ 审计(类别 Helm、操作 Install、描述含「批量下发」与集群名/命名空间/release 名);升级路径归属不动 + 审计(操作 Upgrade);失败集群零归属零审计,验证单测覆盖四类结果
- [x] 2.4 预检异常分类测试:status 调用抛 `NotFoundException`(release 缺失)判定为「不存在」走安装;抛不可达/其它业务异常记失败项不再下发,验证单测

## 3. Web:批量下发对话框与入口

- [x] 3.1 `Helm.razor` 工具栏新增「批量下发」按钮:`AuthorizeView Roles="Admin"` 条件渲染,不随选中集群 `IsReachable` 禁用,点击打开 `FleetDeployHelmDialog`,完成(Ok)后刷新列表,验证编译 + 既有 bUnit 页面测试全绿
- [x] 3.2 `Components/Helm/Shared/FleetDeployHelmDialog.razor`:上传 chart(InputFile + 解析预填,复用单集群口径)→ release 名称/命名空间文本框(DNS-1123 前端校验)/自动创建命名空间与 --wait 复选框/统一 values textarea → 目标集群复选列表(状态徽章,≥1 校验)→ 提交调 `DeployToFleetAsync`,验证 `dotnet build` 通过
- [x] 3.3 对话框执行态与汇总态:进度(「已完成 x / 总数」+ 进度条,提交按钮禁用)→ 完成后逐集群结果行(集群/动作/结果/中文消息)与「成功 n / 失败 m」,错误统一走 `ExHandler`,验证 `dotnet build` 通过

## 4. 对话框与页面测试

- [x] 4.1 bUnit `FleetDeployHelmDialog` 测试:`AddHelmStack` 栈 + Admin 身份;断言上传解析预填、未选集群提交被阻、集群复选与状态徽章渲染、执行后结果行与汇总文案接线(DOM 交互 + provider 冲刷,不 await 长任务链),验证测试全绿
- [x] 4.2 bUnit `Helm.razor` Admin 门控断言:Admin 渲染批量下发入口、Member 不渲染,验证测试全绿

## 5. 收尾

- [x] 5.1 审计与归属口径核对:与单集群安装/升级描述差异仅在「批量下发」前缀;失败/权限拒绝/校验失败三类路径零写入,验证服务层断言已覆盖
- [x] 5.2 `openspec validate --strict` 校验 change 零错误
- [x] 5.3 全量回归:`dotnet build` 0 错误 + `dotnet test` 全绿(MTP,基线 1187+)+ `./coverage.ps1` ≥75% 门禁通过

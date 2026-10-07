# Proposal

## Why

Helm 应用管理目前只能逐集群安装:同一个 chart 包要发布到 N 个集群时,管理员需要重复上传 N 次、在 N 个集群页面之间来回切换。多集群管理系统的核心价值正是舰队级操作,补一个「批量下发」能力:一次上传、选择目标集群、并发下发、逐集群汇报结果。

## What Changes

- `/helm` 页面工具栏新增 Admin-only「批量下发」按钮,打开 `FleetDeployHelmDialog`(大宽度对话框)
- 表单能力:
  - 上传 `.tgz` chart 包(解析后预填 release 名称与 values 初值,与单集群安装对话框一致)
  - release 名称、命名空间(文本输入,前端校验 DNS-1123;可选「命名空间不存在时自动创建」)
  - **统一 values**(YAML 文本,应用到所有目标集群)与「等待资源就绪(--wait)」选项
  - 目标集群多选(复选框列表,展示集群状态徽章,至少选择 1 个)
- 执行语义(服务端):
  - 逐集群先经 `helm status` 预检:release 不存在执行 **install**(写入归属),已存在执行 **upgrade**(沿用统一 values,归属不变)
  - 有界并发(与集群探测一致的常量 4);逐集群失败隔离,单个失败不中断整批
  - 进度回报「已完成 / 总数」,复用「刷新全部」反馈模式
- 完成汇总:逐集群结果行(集群 / 动作(安装或升级) / 结果 / 中文消息)+ 汇总「成功 n / 失败 m」
- 审计:成功集群各写一条审计(类别 Helm、操作安装/升级沿用既有枚举、描述含「批量下发」与集群名),无新增枚举值
- 权限:仅 Admin(服务层强制,非 Admin 抛 `PermissionException`;UI 按角色条件渲染)
- 不进本期:按集群 values 覆盖、批量回滚/卸载、用户侧取消按钮

## Capabilities

### New Capabilities

- `helm-fleet-deploy`:Helm 批量多集群下发的服务契约与对话框行为:入口与 Admin 门控、表单与目标选择口径、逐集群预检与 install/upgrade 分支语义、有界并发与失败隔离、结果汇总、归属记账与审计

### Modified Capabilities

- `helm-release-management`:`/helm` 页面工具栏 requirement 增补 Admin「批量下发」入口(既有单集群安装/升级/回滚/卸载行为不变)

## Impact

- **Application**:`HelmService` 新增 `DeployToFleetAsync`;`Requests/` 新增 `HelmFleetDeployRequest`;`ViewModels/` 新增 `HelmFleetDeployResultViewModel`(含逐集群结果项);`Application/Enums/` 新增下发动作枚举
- **Web**:`Components/Helm/Shared/FleetDeployHelmDialog.razor`(上传、表单、集群多选、进度与汇总);`Helm.razor` 工具栏入口
- **Infrastructure**:无改动(`HelmCliRunner` 每次调用独立临时目录,天然满足并发隔离契约)
- **测试**:`HelmService` 批量下发服务层测试(FakeHelmCliRunner 已支持脚本化)+ bUnit 对话框接线与 Admin 门控断言
- 不涉及数据库 schema、无新表、无新配置项;审计沿用既有 `AuditCategory.Helm` 与 `AuditAction.Install/Upgrade`

## MODIFIED Requirements

### Requirement: 测试项目可运行
系统 SHALL 提供测试项目 `MultiClusterMgmtSys.Tests/`(net10.0,xUnit.v3),挂载于 `MultiClusterMgmtSys.slnx`,可通过 `dotnet test` 运行且全部通过。测试项目 SHALL 以 Microsoft.Testing.Platform 运行(`UseMicrosoftTestingPlatformRunner` + `xunit.v3.mtp-v2`,`OutputType Exe`),SHALL NOT 引入 VSTest bridge 包(`Microsoft.NET.Test.Sdk`/`xunit.runner.visualstudio`/coverlet)。csproj SHALL 含 `FrameworkReference Microsoft.AspNetCore.App` 与主项目 `ProjectReference`,并配置 Moq、bUnit、EF Core SQLite、`Microsoft.Testing.Extensions.CodeCoverage` 与 `ReportGenerator`。测试项目 SHALL 使用 SQLite 内存数据库与生产同 provider。

#### Scenario: 运行测试
- **WHEN** 执行 `dotnet test MultiClusterMgmtSys.Tests`
- **THEN** 测试项目编译并全部通过(MTP 正常退出)

#### Scenario: 测试数据库隔离
- **WHEN** 任一测试需要数据库
- **THEN** 该测试使用独立新建的 SQLite 内存库,测试间互不影响

#### Scenario: VSTest 时代参数拒绝
- **WHEN** 向 MTP 测试进程传入 VSTest 风格参数(如 `--nologo`)
- **THEN** 测试进程按 MTP 约定拒绝(exit 5),不误报为 Zero tests ran

### Requirement: K8s 客户端工厂可注入
`ConfigMapService` / `ClusterNodeService` / `ClusterService` / `WorkloadService` SHALL 通过构造函数注入 `Func<KubernetesClientConfiguration, IKubernetes>` 创建 K8s 客户端(替代直连 `new Kubernetes(config)`),Program.cs 注册默认工厂。生产行为 SHALL 不变。

#### Scenario: 生产注册
- **WHEN** 应用启动
- **THEN** 默认工厂创建真实 Kubernetes 客户端,服务行为与改造前一致

#### Scenario: 测试注入
- **WHEN** 测试注入 mock 工厂
- **THEN** 服务使用 mock 客户端,可模拟 K8s 异常验证翻译链路

### Requirement: 服务层异常契约测试
服务测试 SHALL 以服务公开方法为边界,经真实 SQLite 内存库覆盖业务异常契约:资源不存在→`NotFoundException`(中文 `UserMessage`)、输入非法(备注超长/新旧密码相同/分组移动 sentinel 0/scale 负数等)→`ValidationException`、名称冲突→`ConflictException`;SHALL 经 mock K8s 工厂模拟 404→`NotFoundException`、409→`ConflictException`、403/401→`PermissionException`、超时→`ClusterUnreachableException`,验证 `K8sExceptionMapper` 翻译链路。变更类服务方法 SHALL 断言审计写入副作用。

#### Scenario: 不存在资源
- **WHEN** 服务操作不存在的集群/分组/工作负载
- **THEN** 抛出对应业务异常且 UserMessage 为中文

#### Scenario: K8s 状态码翻译
- **WHEN** mock 客户端返回 404/409/403 或抛超时异常
- **THEN** 服务抛出对应翻译后的业务异常

#### Scenario: 审计副作用
- **WHEN** 任一变更类服务方法(创建/更新/删除/重命名/移动/批量)成功返回
- **THEN** `AuditService.LogAsync` 被调用且分类与动作为中文描述

### Requirement: bUnit 接线契约测试
组件测试 SHALL 通过 bUnit 断言"组件如何使用 MudBlazor"(接线契约),SHALL NOT 断言 `.mud-*` 内部 DOM 或 MudBlazor 渲染结果(判定标准:替换 MudBlazor 组件库后测试应照常通过);驱动方式 SHALL 通过 `FindComponent<T>()` 取 MudBlazor 组件实例并以公开参数/事件触发,断言对象为自有状态/ViewModel/自有 CSS 类。创建过 MudBlazor 组件的 `BunitContext` SHALL 以 `await using` 释放。覆盖面 SHALL 扩至全部 feature(Clusters/Nodes/Workloads/Configmaps/Account/AuditLogs/Profile),至少含:`ClusterFilterBar` 的 `DateRangePicker` 显式 `DateRangeChanged` 回写、各列表页 Admin/Member 门控渲染两态、状态徽章三态 CSS 类、空态/加载态标记、详情页 tab 面板分支、YAML view/edit 卡片分支。

#### Scenario: DateRangePicker 回写
- **WHEN** 触发 FilterBar 内 DateRangePicker 的 `DateRangeChanged`
- **THEN** FilterBar 的查询对象 `DateRange` 被回写

#### Scenario: 权限门控
- **WHEN** 以 Member 身份渲染集群页
- **THEN** 不渲染「刷新所有集群」「添加集群」等 Admin 按钮;以 Admin 身份渲染时可见

#### Scenario: 徽章与空态
- **WHEN** 渲染含在线/离线/未知状态的数据行及空数据列表
- **THEN** 出现对应 `.status-badge online|offline|unknown` 类与 `.empty-state` 标记

#### Scenario: MudBlazor 内部禁断言
- **WHEN** 测试试图断言 `.mud-*` 内部 DOM 或 MudBlazor 渲染细节
- **THEN** 该测试不符合本契约,评审拒绝(自有组件的 markup 断言不受限)

### Requirement: AGENTS.md 测试约定
AGENTS.md SHALL 记录:`dotnet test MultiClusterMgmtSys.Tests` 命令(MTP)、覆盖率收集与 HTML 报告生成命令、75% 覆盖率口径与排除清单、测试目录镜像结构、服务边界测试口径、bUnit 只测接线契约的口径、TestInfrastructure 用法。

#### Scenario: 命令与口径文档化
- **WHEN** 查看 AGENTS.md
- **THEN** Commands 含 MTP 测试命令与覆盖率命令,Testing 节描述上述约定且项目名与现状一致

## ADDED Requirements

### Requirement: 75% 行覆盖率目标与排除口径
测试套件 SHALL 使主项目 `MultiClusterMgmtSys` 的行覆盖率 ≥75%,口径为排除以下 4 个文件后统计:`Program.cs`、`Services/Identity/IdentityComponentsEndpointRouteBuilderExtensions.cs`、`App.razor`、`Routes.razor`。排除 SHALL 以生产代码内 `[ExcludeFromCodeCoverage]` attribute 显式声明(仅 attribute,零行为变更)。`ChineseIdentityErrorDescriber` SHALL NOT 排除(纯函数,纳入测试)。

#### Scenario: 覆盖率达标
- **WHEN** 按 D2 工具链生成 cobertura 报告并按排除口径汇总
- **THEN** 主项目行覆盖率 ≥75%

#### Scenario: 排除清单生效
- **WHEN** 查看 cobertura 报告
- **THEN** 被排除的 4 个文件不计入分母,且这些文件带有 `[ExcludeFromCodeCoverage]` 标记

### Requirement: 覆盖率工具链
系统 SHALL 提供:① `dotnet test MultiClusterMgmtSys.Tests --coverage --coverage-output-format cobertura` 在解决方案根 `TestResults/` 生成 cobertura 报告;② 经测试项目 PackageReference 形态的 ReportGenerator 将 cobertura 转换为 `coveragereport/index.html`(行级下钻明细)。`TestResults/` 与 `coveragereport/` SHALL 加入 `.gitignore` 不入库。

#### Scenario: 生成 cobertura
- **WHEN** 执行 `dotnet test MultiClusterMgmtSys.Tests --coverage --coverage-output-format cobertura`
- **THEN** `TestResults/` 下生成 cobertura XML 文件且包含主程序集模块(存在真实测试时)

#### Scenario: 生成 HTML 报告
- **WHEN** 以 ReportGenerator 处理最新 cobertura 文件
- **THEN** `coveragereport/index.html` 生成,可下钻查看未覆盖行

#### Scenario: 覆盖率产物不入库
- **WHEN** 执行 `git status`
- **THEN** `TestResults/` 与 `coveragereport/` 不出现在未跟踪列表

### Requirement: Workload 与同步服务测试
服务测试 SHALL 覆盖 `WorkloadService`(`WorkloadKind` 四类工作负载的列表分页/过滤、详情、创建、scale、YAML 读写、rollout 状态计算)、`ClusterSyncSettingService`(设置读取与更新 + 审计)、`ClusterSyncBackgroundService.RunOnceAsync`(经 scope 调用 `ClusterService.RefreshAllClustersStatusAsync` 一次)。

#### Scenario: 工作负载列表
- **WHEN** mock K8s 返回 Deployment/StatefulSet/DaemonSet/ReplicaSet 数据并按 `WorkloadQueryRequest` 查询
- **THEN** 返回对应 `WorkloadListViewModel` 分页结果且字段映射正确

#### Scenario: scale 校验
- **WHEN** 调用 scale 并验证 ReplaceScale 收到新副本数
- **THEN** K8s 收到更新后的副本数并写 Scale 审计(副本数合法性由前端约束)

#### Scenario: 定时同步一轮
- **WHEN** 调用 `RunOnceAsync`
- **THEN** 同步执行一轮集群状态刷新并返回成功数

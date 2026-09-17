# unit-testing Delta

## MODIFIED Requirements

### Requirement: 测试项目可运行
系统 SHALL 提供测试项目 `MultiClusterMgmtSys.Tests/`(net10.0,xUnit.v3),挂载于 `MultiClusterMgmtSys.slnx`,可通过 `dotnet test` 运行且全部通过。测试项目 SHALL 以 Microsoft.Testing.Platform 运行(`UseMicrosoftTestingPlatformRunner` + `xunit.v3.mtp-v2`,`OutputType Exe`),SHALL NOT 引入 VSTest bridge 包(`Microsoft.NET.Test.Sdk`/`xunit.runner.visualstudio`/coverlet)。csproj SHALL 含 `FrameworkReference Microsoft.AspNetCore.App` 与四个生产项目(`MultiClusterMgmtSys.Domain`/`MultiClusterMgmtSys.Application`/`MultiClusterMgmtSys.Infrastructure`/`MultiClusterMgmtSys.Web`)的 `ProjectReference`,并配置 Moq、bUnit、EF Core SQLite、`Microsoft.Testing.Extensions.CodeCoverage` 与 `ReportGenerator`。测试项目 SHALL 使用 SQLite 内存数据库与生产同 provider。测试目录 SHALL 镜像生产项目结构(`Domain`/`Application`/`Infrastructure`/`Components`)组织,已有测试仅随移动更新命名空间与 using,断言不变。

#### Scenario: 运行测试
- **WHEN** 执行 `dotnet test MultiClusterMgmtSys.Tests`
- **THEN** 测试项目编译并全部通过(MTP 正常退出)

#### Scenario: 测试数据库隔离
- **WHEN** 任一测试需要数据库
- **THEN** 该测试使用独立新建的 SQLite 内存库,测试间互不影响

#### Scenario: VSTest 时代参数拒绝
- **WHEN** 向 MTP 测试进程传入 VSTest 风格参数(如 `--nologo`)
- **THEN** 测试进程按 MTP 约定拒绝(exit 5),不误报为 Zero tests ran

### Requirement: AGENTS.md 测试约定
AGENTS.md SHALL 记录:`dotnet test MultiClusterMgmtSys.Tests` 命令(MTP)、覆盖率收集与 HTML 报告生成命令、四程序集合并覆盖率口径与排除清单、四层测试目录镜像结构、四项目分层结构与组件命名空间架构测试规则、服务边界测试口径、bUnit 只测接线契约的口径、TestInfrastructure 用法。

#### Scenario: 命令与口径文档化
- **WHEN** 查看 AGENTS.md
- **THEN** Commands 含 MTP 测试命令与覆盖率命令,Testing 节描述上述约定且项目名与现状一致

### Requirement: 75% 行覆盖率目标与排除口径
测试套件 SHALL 使四个生产程序集(`MultiClusterMgmtSys.Domain`、`MultiClusterMgmtSys.Application`、`MultiClusterMgmtSys.Infrastructure`、`MultiClusterMgmtSys.Web`)的合并行覆盖率 ≥75%(covered/total 行数跨程序集求和),口径为排除以下 4 个文件后统计:`MultiClusterMgmtSys.Web/Program.cs`、`MultiClusterMgmtSys.Web/Endpoints/IdentityComponentsEndpointRouteBuilderExtensions.cs`、`MultiClusterMgmtSys.Web/Components/App.razor`、`MultiClusterMgmtSys.Web/Components/Routes.razor`。排除 SHALL 以生产代码内 `[ExcludeFromCodeCoverage]` attribute 显式声明(仅 attribute,零行为变更)。`ChineseIdentityErrorDescriber` SHALL NOT 排除(纯函数,纳入测试)。测试程序集 SHALL NOT 计入覆盖率分母。

#### Scenario: 覆盖率达标
- **WHEN** 按 D2 工具链生成 cobertura 报告并按排除口径汇总四个程序集
- **THEN** 合并行覆盖率 ≥75%

#### Scenario: 排除清单生效
- **WHEN** 查看 cobertura 报告
- **THEN** 被排除的 4 个文件不计入分母,且这些文件带有 `[ExcludeFromCodeCoverage]` 标记

### Requirement: 覆盖率工具链
系统 SHALL 提供:① 仓库根一键脚本 `coverage.ps1`:构建 → 带覆盖跑测试(cobertura 固定输出至 `coverage/coverage.cobertura.xml`)→ ReportGenerator 生成 `coverage/report/index.html`(行级下钻明细)→ 四程序集合并 75% 行覆盖率门禁(低于阈值非零退出,详见 capability `coverage-report-script`);② 手动调试路径:`dotnet test MultiClusterMgmtSys.Tests --coverage --coverage-output-format cobertura` 在解决方案根 `TestResults/` 生成 cobertura 报告,经测试项目 PackageReference 形态的 ReportGenerator 将其转换为 `coveragereport/index.html`(手动路径须只喂最新一份 cobertura,避免多份报告重复计覆盖)。`TestResults/`、`coveragereport/` 与 `coverage/` SHALL 加入 `.gitignore` 不入库。

#### Scenario: 一键脚本出报告
- **WHEN** 在仓库根执行 `./coverage.ps1`
- **THEN** `coverage/coverage.cobertura.xml` 与 `coverage/report/index.html` 生成,且门禁按四程序集合并行覆盖率 ≥75% 判定退出码

#### Scenario: 生成 cobertura
- **WHEN** 执行 `dotnet test MultiClusterMgmtSys.Tests --coverage --coverage-output-format cobertura`
- **THEN** `TestResults/` 下生成 cobertura XML 文件且包含四个生产程序集模块(存在真实测试时)

#### Scenario: 生成 HTML 报告
- **WHEN** 以 ReportGenerator 处理最新 cobertura 文件
- **THEN** `coveragereport/index.html` 生成,可下钻查看未覆盖行

#### Scenario: 覆盖率产物不入库
- **WHEN** 执行 `git status`
- **THEN** `TestResults/`、`coveragereport/` 与 `coverage/` 均不出现在未跟踪列表

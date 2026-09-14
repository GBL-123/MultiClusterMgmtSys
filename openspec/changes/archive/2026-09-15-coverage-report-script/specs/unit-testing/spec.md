# unit-testing Delta

## MODIFIED Requirements

### Requirement: 覆盖率工具链
系统 SHALL 提供:① 仓库根一键脚本 `coverage.ps1`:构建 → 带覆盖跑测试(cobertura 固定输出至 `coverage/coverage.cobertura.xml`)→ ReportGenerator 生成 `coverage/report/index.html`(行级下钻明细)→ 主程序集 75% 行覆盖率门禁(低于阈值非零退出,详见 capability `coverage-report-script`);② 手动调试路径:`dotnet test MultiClusterMgmtSys.Tests --coverage --coverage-output-format cobertura` 在解决方案根 `TestResults/` 生成 cobertura 报告,经测试项目 PackageReference 形态的 ReportGenerator 将其转换为 `coveragereport/index.html`(手动路径须只喂最新一份 cobertura,避免多份报告重复计覆盖)。`TestResults/`、`coveragereport/` 与 `coverage/` SHALL 加入 `.gitignore` 不入库。

#### Scenario: 一键脚本出报告
- **WHEN** 在仓库根执行 `./coverage.ps1`
- **THEN** `coverage/coverage.cobertura.xml` 与 `coverage/report/index.html` 生成,且门禁按主程序集行覆盖率 ≥75% 判定退出码

#### Scenario: 生成 cobertura
- **WHEN** 执行 `dotnet test MultiClusterMgmtSys.Tests --coverage --coverage-output-format cobertura`
- **THEN** `TestResults/` 下生成 cobertura XML 文件且包含主程序集模块(存在真实测试时)

#### Scenario: 生成 HTML 报告
- **WHEN** 以 ReportGenerator 处理最新 cobertura 文件
- **THEN** `coveragereport/index.html` 生成,可下钻查看未覆盖行

#### Scenario: 覆盖率产物不入库
- **WHEN** 执行 `git status`
- **THEN** `TestResults/`、`coveragereport/` 与 `coverage/` 均不出现在未跟踪列表

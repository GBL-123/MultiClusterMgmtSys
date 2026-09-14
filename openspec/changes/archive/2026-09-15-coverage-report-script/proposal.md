# Proposal: coverage-report-script

## Why

覆盖率收集与 HTML 报告生成目前是三步手动流程(`dotnet test` → 从 `TestResults/` 的随机 GUID 文件里挑最新一份 cobertura → 手动调 ReportGenerator),每次都要回查 AGENTS.md 抄命令;且"多份 cobertura 混在 TestResults/ 会重复计覆盖"是已知坑。需要一个一键脚本把流程固化,消除人工挑选与口径漂移。

## What Changes

- 新增仓库根 `coverage.ps1`(适配自 MacroTool 同名脚本):一键执行 build → 带覆盖跑测试(cobertura 固定输出 `coverage/coverage.cobertura.xml`)→ ReportGenerator 生成 `coverage/report/` HTML 报告 → 75% 行覆盖率门禁。
- 门禁经 TextSummary 报告解析主程序集(`MultiClusterMgmtSys`)行覆盖率,低于阈值 exit 1;`-Threshold` 参数可覆盖默认 75(不卡分支覆盖)。
- `coverage/` 目录每轮整删重建(幂等);build / 测试 / 报告生成任一失败即中止,不生成报告,以非零码退出。
- `.gitignore` 补 `coverage/`(现有 `coverage*.xml` 模式只盖住 XML,`report/` 的 HTML 无忽略规则)。
- AGENTS.md Commands 段更新为一键脚本入口;原手动两步命令保留为调试用说明。

## Capabilities

### New Capabilities

- `coverage-report-script`: 一键 UT + 覆盖率报告脚本的行为契约(输出布局、75% 门禁语义、失败即中止语义、幂等清理、产物不入库)。

### Modified Capabilities

- `unit-testing`: 「覆盖率工具链」需求改为以 `coverage.ps1` 为一键入口(cobertura 固定输出至 `coverage/coverage.cobertura.xml`、HTML 报告至 `coverage/report/`),原手动两步命令降级为调试路径保留;`.gitignore` 增补 `coverage/`。

## Impact

- 新增 `coverage.ps1`(仓库根);修改 `.gitignore`、`AGENTS.md`。
- 不触碰产品代码与测试代码;复用测试 csproj 已钉住的 ReportGenerator 5.5.11(NuGet 缓存路径)与 MTP 覆盖扩展,无新依赖。
- 门禁阈值 75 与既有「75% 行覆盖率目标与排除口径」需求一致,不引入新口径。
- 已知残留风险沿用现状:有残留 `dotnet run` 占用主项目 exe 时 build 失败(MSB3021/3026/3027),脚本按失败即中止处理,不自动清理进程。

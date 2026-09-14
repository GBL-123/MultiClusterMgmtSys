# Tasks: coverage-report-script

## 1. 脚本落地

- [x] 1.1 在仓库根创建 `coverage.ps1`:按 design.md 决策适配参考脚本——build `MultiClusterMgmtSys.slnx`(失败 throw)→ 整删重建 `coverage/` → 直接执行 `MultiClusterMgmtSys.Tests\bin\Debug\net10.0\MultiClusterMgmtSys.Tests.exe --coverage --coverage-output coverage\coverage.cobertura.xml`(测试失败 throw,保留 fallback:测试 bin 下 `TestResults/` 搜最新 `coverage.cobertura.xml` 拷到固定路径)→ ReportGenerator 5.5.11(硬编码 NuGet 缓存路径)生成 `coverage/report/`,报告类型 `Html;TextSummary`。仅用 PowerShell 5.1 兼容语法;验证:`./coverage.ps1` 全绿跑通,`coverage/coverage.cobertura.xml` 与 `coverage/report/index.html` 均生成且 exit 0

- [x] 1.2 实现 75% 门禁:解析 `coverage/report/Summary.txt` 主程序集(`MultiClusterMgmtSys`)行覆盖率,低于 `-Threshold`(默认 75)输出实际值与阈值并 exit 1,达标输出报告路径 exit 0;程序集行找不到或百分比解析失败必须 throw。验证:① 默认阈值下现状(77.8%)PASS;② `./coverage.ps1 -Threshold 99` 输出 FAIL 且 exit 1

- [x] 1.3 验证失败即中止语义与幂等:构造一个失败测试(或临时 `-Threshold` 之外的手段验证测试失败分支)确认 throw 后 `coverage/` 无报告产物;连续执行两次脚本确认 `coverage/` 仅含本轮产物、`git status` 不出现 `coverage/`。验证完毕恢复代码原状

## 2. 仓库配套

- [x] 2.1 `.gitignore` 补 `coverage/` 一行(与既有 `TestResults/`、`coveragereport/` 注释分组一致)。验证:`git status` 中 `coverage/` 不出现

- [x] 2.2 更新 AGENTS.md:Commands 段加 `./coverage.ps1` 一键命令(注释含"667 tests + 门禁 75%"语义);"Coverage 工具链"节改为以脚本为首选入口、原手动两步命令降级为调试路径并保留"口径以最新单份 cobertura 为准"警告。验证:通读该节与脚本实际行为一致,无遗留"手动三步"主入口描述

- [x] 2.3 OpenSpec 收尾:更新本 change 的 tasks 勾选状态;确认 `dotnet build MultiClusterMgmtSys.slnx` 0 错误、`dotnet test MultiClusterMgmtSys.Tests` 全绿(脚本引入未影响基线)。验证:两条命令输出符合基线

# Design: coverage-report-script

## Context

现状是三步手动流程(见 proposal Why):`dotnet test --coverage --coverage-output-format cobertura` 产出 `TestResults/<guid>.cobertura.xml`(随机 GUID 名),再手动调 NuGet 缓存里的 ReportGenerator 5.5.11 生成 HTML。多份历史 cobertura 混在 `TestResults/` 会因重复计覆盖而虚高,这是 AGENTS.md 已记录的坑(口径必须"只喂最新一份")。测试项目为 MTP(xunit.v3 + `OutputType Exe`),测试 exe 支持直接执行并接受 MTP 覆盖参数,这使固定输出路径成为可能(MacroTool 的 coverage.ps1 已验证该路径)。参考脚本见探索记录:MacroTool 仓库根 `coverage.ps1`。

## Goals / Non-Goals

**Goals:**

- 单命令完成 build → 带覆盖测试 → HTML 报告,cobertura 固定落 `coverage/coverage.cobertura.xml`(从根上绕开"挑最新 GUID"问题)
- 75% 行覆盖率门禁(与 unit-testing spec 既有口径一致),fail loud
- 幂等:每轮整删重建 `coverage/`,永不混入历史轮次产物

**Non-Goals:**

- 不改产品/测试代码、不改覆盖率排除口径、不引入新依赖
- 不做 CI 流水线适配(本地 Windows 开发场景)
- 不清理历史遗留的 `TestResults/`、`coveragereport/`(gitignored,手动路径仍在用,不自动动它们)
- 不自动处理 exe 被占用类构建失败(沿用 AGENTS.md 手册:按 PID Stop-Process)

## Decisions

1. **直接执行测试 exe,而非 `dotnet test`**。`& MultiClusterMgmt.Tests.exe --coverage --coverage-output <绝对路径>`。理由:`--coverage-output` 直接指定固定文件名,消灭 GUID 挑选逻辑;少一层 `dotnet test` 进程包装,输出更干净。备选:`dotnet test --results-directory <独立目录>` 再改名——仍依赖时间戳挑文件,且多一次改名步骤,否。
   前提:csproj `OutputType Exe`(xunit.v3 已满足);MTP 覆盖扩展可能把输出 normalize 到测试 bin 下 `TestResults/`,因此保留 fallback 搜索(取最新 `coverage.cobertura.xml`)。
2. **门禁经 TextSummary 解析**。ReportGenerator 加 `-reporttypes:Html;TextSummary`,对 `coverage/report/Summary.txt` 按行正则匹配主程序集行(如 `^\s*MultiClusterMgmt\s+[\d.,]+%`),提取百分比与阈值比较。备选:解析 cobertura XML 按 `line-rate` 累加——更重且要自己处理聚合口径,否。解析失败(找不到程序集行)必须 throw,不允许静默放行。
3. **ReportGenerator 路径硬编码** `%USERPROFILE%\.nuget\packages\reportgenerator\5.5.11\tools\net10.0\ReportGenerator.dll`,与测试 csproj PackageReference 钉住的版本一致。备选:经 `dotnet nuget locals global-packages --list` 动态解析——多几行,且版本仍需 glob 猜测,先硬编码,升级 ReportGenerator 时同步改脚本(编译期 fail loud:文件不存在即 throw)。
4. **失败语义:逐步检查 `$LASTEXITCODE`,任一步失败 throw 中止,不出报告**。`$ErrorActionPreference = "Stop"`。与探索期确认一致(CI 友好、避免"红测试配绿报告"误导)。
5. **不加 `-SkipBuild` 开关**(探索期已定):增量 build 只需几秒,开关省时有限、多一条分支路径。
6. **`.gitignore` 增补 `coverage/`**:现有 `coverage*.xml` 模式只命中 XML 文件名,`coverage/report/` 下 HTML 无规则覆盖。
7. **AGENTS.md 同步**:Commands 段加 `./coverage.ps1` 一键入口;"Coverage 工具链"节改为以脚本为首选、手动两步命令降级为调试路径,并保留"口径以最新单份 cobertura 为准"的警告(手动路径仍适用)。

## Risks / Trade-offs

- [MTP 覆盖扩展 normalize 输出路径,固定路径落空] → 保留 fallback:在测试 bin 下 `TestResults/` 递归搜最新 `coverage.cobertura.xml`,拷到固定位置;找不到才报错
- [ReportGenerator 升级后硬编码路径失效] → 文件不存在即 throw(fail loud);升级 csproj 时同步改脚本一行
- [TextSummary 输出格式变化导致门禁解析失败] → 解析失败 throw(不静默);格式属 ReportGenerator 5.5.11 钉死版本,变化风险极低
- [脚本在 PowerShell 5.1 下运行] → 只用 5.1 兼容语法(参考脚本已是),避免三元运算符等 7+ 特性
- [build 遇 MSB3021/3026/3027(exe 被占用)] → 不自动杀进程,throw 透出构建错误(错误信息已含占用 PID),人工按 AGENTS.md 处理

## Migration Plan

无部署面:脚本、gitignore、AGENTS.md 三处落地即可用。历史 `coveragereport/` 目录可手动删除;gitignore 保留其规则(手动调试路径仍会产生)。回滚 = 删脚本,恢复 AGENTS.md 段落。

## Open Questions

无。

# coverage-tooling

## Why

项目当前没有任何覆盖率工具链：测试工程未挂采集器，覆盖率无法产出，"新增代码必须有测试"只停留在口头约定，没有任何机制能阻止存量覆盖回退。实测基线（dotnet-coverage，2026-09-06）：主项目可执行行 5933，已覆盖 1714，行覆盖率 28.9%。目标定为全项目行覆盖 75%（razor 组件计入分母），需要一个可机械执行的棘轮门禁与书面规范，作为后续两个补测 change（后端补测、UI 补测）的强制地基。

## What Changes

- 测试工程 `MultiClusterMgmtSys.Tests.csproj` 新增 `coverlet.msbuild` PackageReference（采集 + 门禁一体，单测试工程无需 collector）。
- 定义覆盖率验证命令：`dotnet test MultiClusterMgmtSys.Tests /p:CollectCoverage=true /p:Threshold=<棘轮值> /p:ThresholdType=line /p:ThresholdStatus=includeAll /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./coverage/`；低于阈值即 `dotnet test` 失败。
- 排除启动引导代码 `Program.cs` 与 `App.razor`（约 127 可执行行，无业务逻辑，业界惯例不计入）。
- 棘轮起点设为 29（当前实测 28.9% 向上取整）：存量覆盖只进不退；此后每完成一个补测 change，把 Threshold 调到新的实测值，长期爬升至 75 并锁定。
- 门禁指标为行覆盖；分支覆盖仅在 cobertura 报告中呈现、跟踪不设卡。
- 建立覆盖率规范并写入 `unit-testing` spec 与 AGENTS.md：
  - 所有新增/修改代码行覆盖率 SHALL ≥75%（随附测试实现，开发者以 cobertura 按文件自查；无 CI 现状下全局棘轮兜住稀释，精确按 diff 检查待 CI 落地后升级）。
  - 全局行覆盖率 SHALL ≥ 棘轮阈值，只升不降。
  - 补测 change 收尾任务 SHALL 包含"把 Threshold 更新为新实测值"。
- 日常快速验证 `dotnet test`（不带 `/p:CollectCoverage`）行为不变，门禁仅在正式验证命令中生效。

## Capabilities

### New Capabilities

- `coverage-gate`: 覆盖率工具链与棘轮门禁契约——采集方式、验证命令、排除范围、棘轮机制（只升不降）、门禁指标口径（行覆盖）、分支覆盖跟踪口径。

### Modified Capabilities

- `unit-testing`: 新增"覆盖率规范"需求——新增/修改代码 ≥75% 行覆盖、棘轮阈值维护责任、验证命令纳入 AGENTS.md 命令节；与既有测试约定（服务边界、bUnit 接线契约）衔接。

## Impact

- **构建/测试**：`MultiClusterMgmtSys.Tests.csproj` 加一个 PackageReference（coverlet.msbuild，MSBuild 集成，无运行时依赖引入主项目）；`dotnet test` 裸跑行为不变，正式验证命令在覆盖率低于棘轮阈值时失败。
- **排除对象**：`Program.cs`、`App.razor`（通过 `[ExcludeFromCodeCoverage]` 特性或 coverlet 排除模式，方式在 design 定夺）。
- **文档/规范**：`openspec/specs/unit-testing/spec.md`、AGENTS.md（Commands 节验证命令 + Testing 节覆盖率规范）。
- **依赖本 change 的后续工作**：后端补测 change（Services 1046 缺口，WorkloadService 323 / AccountService 298 / AuthService 与 ClusterSyncSource 零测试）与 UI 补测 change（razor 2300 缺口）；二者每次收尾抬升棘轮。
- **无 CI 依赖**：本 change 不假设 CI 存在；门禁由本地验证命令承担。

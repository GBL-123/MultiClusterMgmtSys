# coverage-gate

## Purpose

定义单元测试覆盖率的采集与门禁契约：测试工程产出 cobertura 覆盖率数据，全项目行覆盖率由"棘轮阈值"强制约束（只升不降），启动引导代码排除在统计外，分支覆盖仅跟踪不设卡。该契约是补测工作的强制地基，目标为全项目行覆盖 75%。

## ADDED Requirements

### Requirement: 覆盖率采集可用

测试工程 SHALL 集成 coverlet.msbuild，通过覆盖率验证命令产出 cobertura 格式覆盖率数据（含逐文件行/分支命中统计）。日常快速命令 `dotnet test`（不带采集参数）SHALL 行为不变、不产出覆盖率文件。

#### Scenario: 运行验证命令产出报告

- **WHEN** 执行覆盖率验证命令（`dotnet test` 携带 CollectCoverage 等参数）
- **THEN** 生成 cobertura 格式 XML 报告，可解析出全项目及逐文件行覆盖率

#### Scenario: 日常命令不受影响

- **WHEN** 开发者日常执行 `dotnet test MultiClusterMgmtSys.Tests`
- **THEN** 行为与改造前一致，不触发门禁，不产出覆盖率文件

### Requirement: 棘轮门禁

全项目行覆盖率 SHALL ≥ 当前棘轮阈值，低于阈值时验证命令 SHALL 失败（非零退出）。棘轮阈值 SHALL 只升不降：每次调整只允许更新为新的实测值。棘轮起点 SHALL 为按 coverlet.msbuild 首次实测的全项目行覆盖率向下取整（预期约 28，保证门禁在补测落地前即为绿态；不同采集器插桩计数略有差异，起点值以所选采集器实测为准）。

#### Scenario: 低于阈值失败

- **WHEN** 全项目行覆盖率低于棘轮阈值
- **THEN** 覆盖率验证命令失败并报告当前值与阈值

#### Scenario: 达标通过

- **WHEN** 全项目行覆盖率 ≥ 棘轮阈值
- **THEN** 覆盖率验证命令通过

#### Scenario: 阈值只升不降

- **WHEN** 更新棘轮阈值
- **THEN** 新阈值 SHALL ≥ 旧阈值；不允许为迁就未覆盖代码而调低

#### Scenario: 棘轮起点即绿

- **WHEN** change 落地后首次执行覆盖率验证命令（尚未新增任何补测）
- **THEN** 验证命令通过（起点阈值不超过实测基线）

### Requirement: 启动引导代码排除

`Program.cs` 与 `App.razor` SHALL 排除在覆盖率统计之外（启动引导/装配代码，无业务分支）；除此之外的全项目代码（含 razor 组件、ViewModels、Requests、Models、Data、Common、其余 Services）SHALL 全部计入分母。

#### Scenario: 排除生效

- **WHEN** 解析覆盖率报告
- **THEN** 报告中不含 Program.cs 与 App.razor 的统计条目

#### Scenario: UI 计入分母

- **WHEN** 解析覆盖率报告
- **THEN** razor 组件（Components/**）的执行行计入全项目分母，不设 UI 豁免

### Requirement: 门禁指标口径

门禁 SHALL 仅以行覆盖率为准。分支覆盖率 SHALL 在报告中呈现并跟踪，但 SHALL NOT 参与门禁判定。

#### Scenario: 分支不设卡

- **WHEN** 行覆盖率达标而分支覆盖率偏低
- **THEN** 覆盖率验证命令通过；分支数据仅用于报告查看

### Requirement: 阈值维护

任何提升覆盖率的补测 change，其收尾任务 SHALL 包含"将棘轮阈值更新为该 change 完成后的实测值"，并同步 AGENTS.md 中记录的当前阈值与最新实测基线。

#### Scenario: 补测收尾抬升棘轮

- **WHEN** 一个补测 change 完成且实测行覆盖率上升
- **THEN** 该 change 将棘轮阈值更新为新实测值（≥旧值），AGENTS.md 基线记录同步更新

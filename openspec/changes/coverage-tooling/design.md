# coverage-tooling — design

## Context

实测基线（dotnet-coverage，2026-09-06）：主项目可执行行 5933，覆盖 1714，行覆盖 28.9%；缺口分布见 proposal。仓库无 CI，门禁只能挂在本地验证命令上。测试工程 `MultiClusterMgmtSys.Tests`（net10.0，xUnit + bUnit + Moq + SQLite 内存库）单工程、引用单一主项目，无其他测试工程。

## Goals / Non-Goals

**Goals:**

- 一条可机械失败的覆盖率验证命令（棘轮门禁），起点即绿。
- 采集器与门禁一体，不引入脚本；日常 `dotnet test` 行为不变。
- 覆盖率规范（新增代码 ≥75%、棘轮只升不降）写入 unit-testing spec 与 AGENTS.md。

**Non-Goals:**

- 不在本 change 内补任何业务测试（属后续两个补测 change）。
- 不建 CI、不做按 diff 的"新增代码"精确检查（CI 落地后升级）。
- 不把分支覆盖纳入门禁。

## Decisions

### D1：采集器选 coverlet.msbuild（三选一）

| 候选 | 出数字 | 门禁 | 结论 |
|---|---|---|---|
| coverlet.msbuild | ✓ | ✓ `/p:Threshold` 原生 | **选定** |
| coverlet.collector | ✓ | ✗ 需自写阈值脚本 | 无门禁，还得养脚本 |
| dotnet-coverage（全局工具） | ✓ | ✗ | 只用于本次摸底，不进工具链 |

VS 自带覆盖率仅 Enterprise 版可用，不作为依赖。coverlet.msbuild 是"单测试工程 + 零脚本 + 原生门禁"的唯一组合。

### D2：棘轮起点 = coverlet 首测值向下取整（预期 28），而非 29

- 依据：基线 28.9% 来自 dotnet-coverage 的插桩，coverlet 的可执行行计数与之不完全可比，起点必须以**所选采集器**的实测为准。
- coverlet.msbuild 默认 `ThresholdStat=minimum`：对被插桩的每个程序集（主项目 + 测试工程）逐一比对阈值。测试工程自身 ~99% 恒高于阈值，实际生效约束 = 主项目行覆盖——与"全项目行覆盖"口径一致。起点若取 29（> 28.9），门禁在补测落地前即红，违背"棘轮不回退、不影响现有节奏"的初衷；向下取整保证 change 1 收尾即绿。
- 抬升规则：每个补测 change 收尾，把 Threshold 改为该 change 完成后的实测值向下取整（只升不降）。

### D3：排除方式 = 测试工程 csproj 的 coverlet Exclude 过滤器（不动主项目代码）

- 用 `/p:Exclude`（等价的 csproj 属性）排除 `Program.cs` 与 `App.razor`，过滤器形如 `[*]*Program.cs` / `[*]*App.razor`（确切过滤串在实现时验证，见 tasks）。
- 备选 `[ExcludeFromCodeCoverage]` 特性：Program.cs 需改主项目源码、App.razor 需在 razor 内加 `@attribute`，排除配置散落两处且属主项目改动。集中放在测试工程（覆盖率配置的属主）更干净，主项目零接触。
- razor 组件计数说明：bUnit 用真实渲染器执行组件，`@code` 块与渲染行均会计入——无需特殊处理，"UI 计入分母"自然成立。

### D4：验证命令固化在 AGENTS.md（无 CI 的强制点）

```
dotnet test MultiClusterMgmtSys.Tests ^
  /p:CollectCoverage=true ^
  /p:CoverletOutputFormat=cobertura ^
  /p:CoverletOutput=./coverage/ ^
  /p:Threshold=28 ^
  /p:ThresholdType=line
```

- 门禁仅在带 `/p:CollectCoverage=true` 的命令中生效，日常快速测试零影响。
- 无 CI 时这是唯一机械化执行点：规范要求"正式验证必须跑覆盖率验证命令"，由 AGENTS.md 与 unit-testing spec 约束；被绕过的风险接受为现状已知局限（见 R2）。

## Risks / Trade-offs

- **[R1] coverlet 与 dotnet-coverage 计数口径不同** → 基线以 coverlet 首测为准重测，棘轮起点按首测值设定；实现任务含"首测并记录基线"。
- **[R2] 无 CI，门禁靠约定执行**（不跑验证命令就不会红）→ AGENTS.md 将验证命令列为正式验证的一部分；CI 落地后升级为按 diff 门禁，spec 已预留该路径。
- **[R3] 全局阈值是稀释型指标**：新增零测试代码会拉低全局值直到撞棘轮，不能精确定位"哪次提交稀释了" → 接受；规范要求开发者以报告逐文件自查新增文件 ≥75%，棘轮兜住累积效应。
- **[R4] 插桩拖慢测试** → 只在验证命令启用采集，日常命令无开销。
- **[R5] razor 生成代码行计数波动**（Blazor 编译器版本升级可能改变可执行行分布）→ 棘轮抬升只看实测趋势，波动由重测吸收；不因此调低阈值。

## Migration Plan

1. 测试 csproj 加 coverlet.msbuild 引用 + 排除过滤器 + Threshold 属性（初值待首测后回填）。
2. coverlet 首测 → 记录基线 → 回填棘轮起点值 → 验证命令绿。
3. 更新 AGENTS.md（Commands + Testing）与 unit-testing spec 归档同步。
4. 回滚：还原 csproj 两处编辑即可，主项目无改动，无数据/运行时影响。

## Open Questions

无阻塞项。按 diff 精确门禁的 CI 方案（R2 升级路径）留待 CI 存在后另立 change。

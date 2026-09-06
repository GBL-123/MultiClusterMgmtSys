# coverage-tooling — tasks

## 1. 工具链接入

- [ ] 1.1 `MultiClusterMgmtSys.Tests.csproj` 新增 `coverlet.msbuild` PackageReference（最新稳定版）
- [ ] 1.2 csproj 属性组新增排除过滤器（`Program.cs`、`App.razor`，过滤串形如 `[*]*Program.cs`、`[*]*App.razor`，确切写法以 coverlet 文档验证）
- [ ] 1.3 日常命令回归：`dotnet test MultiClusterMgmtSys.Tests` 裸跑行为不变、不产出覆盖率文件

## 2. 基线与棘轮起点

- [ ] 2.1 用 coverlet.msbuild 首测全项目行覆盖率（`/p:CollectCoverage=true /p:CoverletOutputFormat=cobertura`），解析逐文件报告并与 dotnet-coverage 基线（28.9% / 1714/5933）比对，记录口径差异
- [ ] 2.2 棘轮起点值 = 首测值向下取整，回填 csproj `/p:Threshold` 与验证命令
- [ ] 2.3 验证覆盖率验证命令为绿（未新增任何补测，起点即通过——spec"棘轮起点即绿"场景）

## 3. 门禁行为验证

- [ ] 3.1 临时把 Threshold 调到实测值之上，确认验证命令失败并报告当前值与阈值（"低于阈值失败"场景），随后还原
- [ ] 3.2 确认覆盖率报告中不含 `Program.cs` / `App.razor` 条目，且 Components/** 计入分母（"排除生效"“UI 计入分母"场景）

## 4. 规范落地

- [ ] 4.1 AGENTS.md Commands 节加入覆盖率验证命令（含当前棘轮阈值与实测基线注释）
- [ ] 4.2 AGENTS.md Testing 节加入覆盖率规范：新增/修改代码 ≥75%、逐文件自查方法、棘轮只升不降、补测 change 收尾抬升阈值
- [ ] 4.3 全量回归：`dotnet build MultiClusterMgmtSys.slnx` 0 错误 + `dotnet test` 129 测试全绿 + 覆盖率验证命令通过

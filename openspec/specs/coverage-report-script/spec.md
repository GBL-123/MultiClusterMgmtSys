# coverage-report-script

## Purpose

提供仓库根一键脚本 `coverage.ps1`,把「build → 带覆盖跑测试 → 生成 HTML 覆盖率报告」固化为单命令,消除手动三步流程与"多份 cobertura 重复计覆盖"的坑,并以 75% 行覆盖率门禁防止覆盖率滑坡。

## Requirements

### Requirement: 一键执行入口
仓库根 SHALL 提供 `coverage.ps1`,单命令完成构建、带覆盖率的测试执行与 HTML 报告生成,无需手动挑选中间产物或回查文档抄命令。

#### Scenario: 一键成功
- **WHEN** 在仓库根执行 `./coverage.ps1`
- **THEN** 生成 `coverage/coverage.cobertura.xml` 与 `coverage/report/index.html`,脚本以 0 退出

### Requirement: 固定输出布局
覆盖率产物 SHALL 集中在仓库根 `coverage/`:cobertura XML 固定命名 `coverage.cobertura.xml`(不落随机 GUID 文件名),HTML 报告位于 `coverage/report/`。每轮执行 SHALL 先整删重建 `coverage/`(幂等,不残留历史轮次产物)。

#### Scenario: 幂等重建
- **WHEN** 连续执行两次脚本
- **THEN** 第二次执行后 `coverage/` 仅含本轮产物,无历史轮次残留

#### Scenario: 产物不入库
- **WHEN** 脚本执行完成后执行 `git status`
- **THEN** `coverage/` 不出现在未跟踪列表

### Requirement: 75% 行覆盖率门禁
脚本 SHALL 解析覆盖率报告中的主程序集(`MultiClusterMgmtSys`)行覆盖率,低于阈值时以非零码退出并提示报告路径;阈值默认 75,SHALL 支持经 `-Threshold` 参数覆盖。分支覆盖率 SHALL NOT 纳入门禁。

#### Scenario: 达标通过
- **WHEN** 主程序集行覆盖率 ≥ 阈值
- **THEN** 脚本输出实际覆盖率并以 0 退出,同时提示报告路径

#### Scenario: 低于阈值拦截
- **WHEN** 主程序集行覆盖率 < 阈值(或执行时显式传入高于现状的 `-Threshold`)
- **THEN** 脚本输出实际覆盖率与阈值并以非零码退出

### Requirement: 失败即中止
构建、测试执行、报告生成任一步失败时,脚本 SHALL 以非零码退出且 SHALL NOT 生成本轮覆盖率报告。脚本 SHALL NOT 自动清理外部残留进程(如占用 exe 导致构建失败的 `dotnet run`),按失败即中止透出错误。

#### Scenario: 测试失败中止
- **WHEN** 存在失败的测试用例
- **THEN** 脚本以非零码退出,`coverage/` 下无本轮报告

#### Scenario: 构建失败中止
- **WHEN** 构建失败(如 exe 被残留 `dotnet run` 占用)
- **THEN** 脚本以非零码中止并透出构建错误信息,不继续执行测试

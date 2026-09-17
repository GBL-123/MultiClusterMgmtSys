# coverage-report-script Delta

## MODIFIED Requirements

### Requirement: 75% 行覆盖率门禁
脚本 SHALL 解析覆盖率报告中四个生产程序集(`MultiClusterMgmtSys.Domain`、`MultiClusterMgmtSys.Application`、`MultiClusterMgmtSys.Infrastructure`、`MultiClusterMgmtSys.Web`)的合并行覆盖率(covered/total 行数跨程序集求和),低于阈值时以非零码退出并提示报告路径;阈值默认 75,SHALL 支持经 `-Threshold` 参数覆盖。分支覆盖率 SHALL NOT 纳入门禁。测试程序集 SHALL NOT 计入合并分母。

#### Scenario: 达标通过
- **WHEN** 四程序集合并行覆盖率 ≥ 阈值
- **THEN** 脚本输出实际覆盖率并以 0 退出,同时提示报告路径

#### Scenario: 低于阈值拦截
- **WHEN** 四程序集合并行覆盖率 < 阈值(或执行时显式传入高于现状的 `-Threshold`)
- **THEN** 脚本输出实际覆盖率与阈值并以非零码退出

# Spec Delta

## ADDED Requirements

### Requirement: Helm release 状态的中英双语展示

系统 SHALL 对 Helm release 状态以「中文主行 + 英文原值次行」展示,映射至少覆盖:`deployed` → 已部署、`failed` → 已失败、`pending-install` → 安装中、`pending-upgrade` → 升级中、`pending-rollback` → 回滚中、`superseded` → 已取代、`uninstalling` → 卸载中、`uninstalled` → 已卸载;未识别状态 SHALL 回退原始文本单行展示。

#### Scenario: deployed 双语展示

- **WHEN** release 状态为 `deployed`
- **THEN** 主行显示「已部署」,次行以等宽字体显示 `deployed`

#### Scenario: 未识别状态回退

- **WHEN** release 状态为未登记的值
- **THEN** 以原始文本单行展示,不显示空白或臆造中文

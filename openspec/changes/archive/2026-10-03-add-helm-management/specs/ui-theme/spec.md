# Spec Delta

## ADDED Requirements

### Requirement: Helm release 状态徽章的语义归属

Helm release 状态徽章 SHALL 沿用既有在线/离线/未知三套淡彩底深字配色,SHALL NOT 新增颜色:`deployed` SHALL 使用在线色系;`failed` SHALL 使用离线色系;`pending-install`、`pending-upgrade`、`pending-rollback`、`superseded`、`uninstalling`、`uninstalled` SHALL 使用未知色系。该样式 SHALL 统一应用于 Helm release 列表与详情。

#### Scenario: deployed 徽章

- **WHEN** release 状态为 `deployed`
- **THEN** 徽章为在线色系(淡绿底深绿字)

#### Scenario: failed 徽章

- **WHEN** release 状态为 `failed`
- **THEN** 徽章为离线色系(淡红底深红字)

#### Scenario: pending 徽章

- **WHEN** release 状态为 `pending-upgrade`
- **THEN** 徽章为未知色系(淡琥珀底深琥珀字)

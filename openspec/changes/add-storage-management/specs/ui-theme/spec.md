# Spec Delta

## ADDED Requirements

### Requirement: PV 与 PVC phase 徽章的语义归属

持久卷与持久卷声明的 phase SHALL 经既有 `.status-badge` 徽章体系展示,凭 CssClass 归入预定义色系,SHALL NOT 新建独立徽章组件:`Bound`(绑定)与 `Available`(可用)为在线色系(资源处于可正常使用状态);`Pending`(等待中)为未知色系(琥珀,进行中待定);`Released`(已释放)为未知色系(数据保留但已解绑);`Lost` 与 `Failed` 为离线色系(异常状态)。色系沿用状态徽章既有实现,SHALL NOT 因存储域新增独立样式体系。

#### Scenario: 正常态在线色系

- **WHEN** PV Phase 为 Available 或 Bound
- **THEN** 状态徽章呈现在线色系圆点

#### Scenario: 待定与已释放未知色系

- **WHEN** PVC Phase 为 Pending 或 PV Phase 为 Released
- **THEN** 状态徽章呈现未知色系圆点

#### Scenario: 异常态离线色系

- **WHEN** PVC Phase 为 Lost 或 PV Phase 为 Failed
- **THEN** 状态徽章呈现离线色系圆点

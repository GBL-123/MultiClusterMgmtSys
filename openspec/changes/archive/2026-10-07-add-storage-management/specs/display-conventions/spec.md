# Spec Delta

## ADDED Requirements

### Requirement: PV 与 PVC phase 的中英双语展示

持久卷(PV)与持久卷声明(PVC)的状态字段 SHALL 双语展示:中文主行 + 等宽英文次行(row 用 `Components/Common/StackedText`)。展示统一走 Application 展示映射纯静态方法(未登记值回退原文、单行展示);raw 字段保留供排序/过滤/提交使用。映射:PV `Available→可用、Bound→已绑定、Released→已释放、Failed→失败`;PVC `Bound→已绑定、Pending→等待中、Lost→丢失`。

#### Scenario: 已登记映射双语展示

- **WHEN** 列表渲染 Phase 为 Bound 的 PV 与 Pending 的 PVC
- **THEN** 分别显示主行「已绑定」与「等待中」,次行等宽显示英文原值

#### Scenario: 未登记值回退

- **WHEN** API 返回未登记的 phase 值(如未来新增的 "Maintenance")
- **THEN** 单行展示原文,不拆双语行

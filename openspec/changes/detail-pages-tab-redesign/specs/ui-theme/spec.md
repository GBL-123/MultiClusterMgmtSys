# ui-theme delta

## ADDED Requirements

### Requirement: 详情页 Tab 样式

详情页使用的 `MudTabs` SHALL 遵循 Swiss Industrial Print:tab 栏底色为卡面 `#FCFBF7`、下沿为发丝线 `#E2DED5`;激活 tab 的指示条 SHALL 为琥珀色 `#B45309` 细线;激活 tab 文字为墨色 `#111111`,未激活为次文字 `#6E675C`;SHALL 去除默认投影与面板圆角,tab 高度保持密集。该样式 SHALL 通过共享 CSS 类统一应用于集群、节点、工作负载与 ConfigMap 详情页,SHALL NOT 出现各页各异的 tab 观感。自定义 tab 样式属于设计词汇,不违反组件优先策略(仍使用 `MudTabs` 组件本身)。

#### Scenario: 指示条与发丝线

- **WHEN** 任一详情页渲染 tab 栏
- **THEN** tab 栏下沿为发丝线,激活 tab 的指示条为琥珀色细线,无投影与圆角

#### Scenario: 未激活态

- **WHEN** 用户查看未选中的 tab 标签
- **THEN** 其文字为次文字色,悬停时出现暖灰色背景反馈

#### Scenario: 全站一致

- **WHEN** 对比集群详情页与 ConfigMap 详情页的 tab 栏
- **THEN** 两者使用相同的指示条颜色、分隔线与文字配色

#### Scenario: 面板内容贴靠

- **WHEN** tab 面板渲染卡片内容
- **THEN** 面板无 MudBlazor 默认的额外内边距圆角框,卡片边距与既有详情页卡片间距一致

## MODIFIED Requirements

### Requirement: 设计 token(调色板/圆角/阴影)
系统 SHALL 使用 Swiss Industrial Print 全亮色设计 token:MudTheme 与 app.css 中 SHALL 使用暖纸背景 `#F4F4F0`、卡面 `#FCFBF7`、发丝线 `#E2DED5`、墨色主文字/主色 `#111111`、次文字 `#6E675C`、单一琥珀强调色 `#D97706`。默认圆角 SHALL 为 3px。UI 层级 SHALL 通过发丝线与 inset 高光表达,不使用投影阴影。

#### Scenario: 主题加载
- **WHEN** 应用启动并渲染任意页面
- **THEN** 页面背景为暖纸色,卡片为卡面色,分隔线为发丝线色,主按钮为墨底白字

#### Scenario: 强调色稀缺性
- **WHEN** 检查强调色出现位置
- **THEN** 琥珀色仅出现在品牌方牌、焦点环、刷新进度条与空态框中,不用于常规按钮与文字

### Requirement: 详情页 Tab 样式
详情页使用的 `MudTabs` SHALL 遵循 Swiss Industrial Print:tab 栏底色为卡面 `#FCFBF7`、下沿为发丝线 `#E2DED5`;激活 tab 的指示条 SHALL 为琥珀色 `#D97706` 细线;激活 tab 文字为墨色 `#111111`,未激活为次文字 `#6E675C`;SHALL 去除默认投影与面板圆角,tab 高度保持密集。该样式 SHALL 通过共享 CSS 类统一应用于集群、节点、工作负载与 ConfigMap 详情页,SHALL NOT 出现各页各异的 tab 观感。自定义 tab 样式属于设计词汇,不违反组件优先策略(仍使用 `MudTabs` 组件本身)。

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

## ADDED Requirements

### Requirement: Tooltip 视觉与行为
系统 SHALL 以统一的 Swiss Industrial Print 样式渲染所有 tooltip:墨色 `#111111` 底、纸色 `#F4F4F0` 字、3px 圆角、无投影、无半透明;箭头与气泡同色;技术/原始值 SHALL 使用等宽字体,中文说明 SHALL 使用正文栈;内容宽度受限并允许换行。tooltip 的显示延迟、位置与焦点行为 SHALL 全站一致,SHALL NOT 各调用点自行设定互不相同的风格。tooltip 内 SHALL NOT 使用琥珀强调色或填充色块。

#### Scenario: 墨底纸字
- **WHEN** 用户悬停任意 tooltip 触发器
- **THEN** 气泡为墨色底、纸色字、3px 圆角且无投影,箭头与气泡同色

#### Scenario: 原始值等宽
- **WHEN** tooltip 内容为 Kubernetes 原始 quantity(`16297496Ki`)或枚举原值(`Ready`)
- **THEN** 内容以等宽字体渲染

#### Scenario: 长文本换行
- **WHEN** tooltip 内容为较长的条件消息或标签值
- **THEN** 气泡宽度受限并换行展示完整内容,不裁切

### Requirement: Tooltip 触发统一与原生 title 禁用
面向用户的悬停提示 SHALL 一律经统一 tooltip 组件(基于 MudBlazor tooltip 体系)呈现;SHALL NOT 使用原生 `title` 属性承载用户可见提示。行内图标操作 SHALL 使用 `TooltipIconButton`;被截断的文本(标签/注解值、条件消息、审计目标、端口说明)SHALL 通过统一 tooltip 暴露完整内容。tooltip 触发 SHALL 同时支持鼠标悬停与键盘聚焦。

#### Scenario: 原生 title 清零
- **WHEN** 检查任一页面组件的渲染标记
- **THEN** 不存在用于用户可见提示的原生 `title` 属性,等价提示均由统一 tooltip 组件承载

#### Scenario: 截断文本完整可读
- **WHEN** 标签值、注解值、条件消息或审计目标因列宽被省略号截断
- **THEN** 悬停该单元格显示完整内容的统一 tooltip

#### Scenario: 图标操作提示
- **WHEN** 表格行内渲染刷新/编辑/删除等图标按钮
- **THEN** 提示经 `TooltipIconButton` 呈现,`Text` 同时作为 tooltip 与 `aria-label`

### Requirement: 双语展示视觉词汇
中英双语展示 SHALL 使用统一视觉词汇:中文主行使用正文样式;英文次行使用 caption 级等宽字体与次文字色;状态徽章保持淡彩底 + 深字,徽章文本为中文,英文原值作为徽章相邻的次行展示。SHALL NOT 为英文次行引入新的强调色、填充色或投影。

#### Scenario: 主次层次清晰
- **WHEN** 任一值以双语展示
- **THEN** 中文主行在视觉层级上高于英文次行,次行为等宽字体且使用 `#6E675C` 次文字色

#### Scenario: 徽章不新增颜色
- **WHEN** 状态徽章附带英文次行
- **THEN** 徽章仍使用既有在线/离线/未知的淡彩底与深字配色,英文次行不改变徽章底色

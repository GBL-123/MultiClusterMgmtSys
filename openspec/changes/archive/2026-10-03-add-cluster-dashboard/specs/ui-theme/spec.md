# Spec Delta

## MODIFIED Requirements

### Requirement: 设计 token(调色板/圆角/阴影)
系统 SHALL 使用 Swiss Industrial Print 全亮色设计 token:MudTheme 与 app.css 中 SHALL 使用暖纸背景 `#F4F4F0`、卡面 `#FCFBF7`、发丝线 `#E2DED5`、墨色主文字/主色 `#111111`、次文字 `#6E675C`、单一琥珀强调色 `#D97706`。默认圆角 SHALL 为 3px。UI 层级 SHALL 通过 MudBlazor 默认 elevation 阴影与发丝线共同表达:内容卡片/面板 SHALL 保持默认 `Elevation`(`MudPaper`/`MudCard` 默认 1,列表页空态卡 2,卡片内表格 0),SHALL NOT 将其压平为 `Elevation="0"`;登录/注册面板、兜底页、AppBar/Drawer、tooltip 与断线重连弹窗 SHALL 保持无阴影、仅以发丝线表达层级。

#### Scenario: 主题加载
- **WHEN** 应用启动并渲染任意页面
- **THEN** 页面背景为暖纸色,卡片为卡面色,分隔线为发丝线色,主按钮为墨底白字

#### Scenario: 强调色稀缺性
- **WHEN** 检查强调色出现位置
- **THEN** 琥珀色仅出现在品牌方牌、焦点环、刷新与重连进度条、空态框中,不用于常规按钮与文字

#### Scenario: 内容卡片保持默认阴影
- **WHEN** 渲染任一内容页(列表页、详情页或看板)的卡片/面板
- **THEN** 卡片呈现 MudBlazor 默认 elevation 阴影,与其余页面层级观感一致,不存在被压平的内容卡片

#### Scenario: 无阴影例外
- **WHEN** 检查登录/注册面板、兜底页、AppBar/Drawer、tooltip 与断线重连弹窗
- **THEN** 这些界面仅以发丝线表达层级,不带投影阴影

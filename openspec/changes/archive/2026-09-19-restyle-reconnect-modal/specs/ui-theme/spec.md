# ui-theme Delta

## MODIFIED Requirements

### Requirement: 设计 token(调色板/圆角/阴影)
系统 SHALL 使用 Swiss Industrial Print 全亮色设计 token:MudTheme 与 app.css 中 SHALL 使用暖纸背景 `#F4F4F0`、卡面 `#FCFBF7`、发丝线 `#E2DED5`、墨色主文字/主色 `#111111`、次文字 `#6E675C`、单一琥珀强调色 `#D97706`。默认圆角 SHALL 为 3px。UI 层级 SHALL 通过发丝线与 inset 高光表达,不使用投影阴影。

#### Scenario: 主题加载
- **WHEN** 应用启动并渲染任意页面
- **THEN** 页面背景为暖纸色,卡片为卡面色,分隔线为发丝线色,主按钮为墨底白字

#### Scenario: 强调色稀缺性
- **WHEN** 检查强调色出现位置
- **THEN** 琥珀色仅出现在品牌方牌、焦点环、刷新与重连进度条、空态框中,不用于常规按钮与文字

### Requirement: 组件优先策略
页面实现 SHALL 优先使用 MudBlazor 组件(按钮、输入框、下拉、开关、表格、对话框、卡片、进度条等),SHALL NOT 用原生 HTML 元素加自定义 CSS 重新实现 MudBlazor 已提供的交互组件。以下例外 SHALL 被允许并视为契约的一部分:(1) 全高 YAML 查看/编辑用原生 `<textarea class="yaml-textarea">`(MudTextField multiline 存在 CSS 特异性与降级问题);(2) `ReconnectModal.razor` 中 Blazor Server 框架依赖的原生对话框与按钮(`components-reconnect-button`/`components-resume-button` 为框架 JS 契约 id;新增 `components-reload-button` 由同组件模块脚本绑定);(3) Blazor 内置 `InputFile` 组件(核心 MudBlazor 无对应文件上传组件)。设计系统自有的展示性 span(状态徽章、品牌方牌、角色徽章、空态框、mono 提示行)属于设计词汇,不算违规。

#### Scenario: 新增交互元素选型
- **WHEN** 开发者为页面新增按钮、输入框、下拉或开关
- **THEN** 使用 MudButton、MudTextField、MudSelect、MudSwitch 等 MudBlazor 组件,而非原生 `<button>`/`<input>`/`<select>`

#### Scenario: YAML 编辑例外
- **WHEN** 实现 ConfigMap 或 Workload 的 YAML 查看/编辑卡片
- **THEN** 使用原生 `<textarea class="yaml-textarea">`,不使用 MudTextField multiline

#### Scenario: 重连弹窗例外
- **WHEN** 检查 `ReconnectModal.razor` 的对话框与按钮
- **THEN** 保留框架依赖的原生对话框、按钮及其 id(以及同组件模块脚本绑定的刷新按钮),不替换为 MudDialog/MudButton

#### Scenario: 文件上传例外
- **WHEN** 页面需要上传 kubeconfig 等本地文件
- **THEN** 使用 Blazor 内置 `InputFile` 组件

## ADDED Requirements

### Requirement: 重连弹窗视觉与文案
断线重连弹窗 SHALL 遵循 Swiss Industrial Print:遮罩为墨色淡化(无渐变),卡片为 `#FCFBF7` 底 + 1px `#E2DED5` 发丝线 + 3px 圆角 + 无阴影;头部 SHALL 为琥珀方牌 + 中文标题;正文 SHALL 为中文说明与等宽 mono 技术状态行(随连接状态切换);连接进度 SHALL 以 2px 琥珀不确定进度线表达;按钮 SHALL 为墨底主按钮(`[重试]`/`[恢复会话]`)与发丝线描边次按钮(`[刷新页面]`),焦点环 `#D97706`、按压 `scale(0.98)`。文案 SHALL 为中文,SHALL NOT 保留英文模板文案。JS 契约(`dialog#components-reconnect-modal`、`#components-reconnect-button`、`#components-resume-button`、`#components-seconds-to-next-attempt` 与全部状态类名)SHALL 逐字保留;模块脚本 SHALL 位于 `wwwroot/js/reconnect.js` 并经 `@Assets["js/reconnect.js"]` 引用(规避 dev-run 静态资产 500 怪癖),重连/暂停/恢复状态机与自动重试行为 SHALL NOT 改变。重连样式 SHALL 定义在全局 `app.css`(不依赖组件 scoped CSS)。

#### Scenario: 卡片语言
- **WHEN** 断线重连弹窗显示
- **THEN** 卡片为纸卡底、发丝线描边、3px 圆角、无投影阴影,遮罩无渐变

#### Scenario: 中文状态文案
- **WHEN** 弹窗处于重连中、重试倒计时、失败或暂停状态
- **THEN** 各状态文案为中文(失败态含墨红 `#9F2F2D` 提示),倒计时仍写入 `#components-seconds-to-next-attempt`

#### Scenario: 按钮语言与可见性
- **WHEN** 状态为 failed 或 resume-failed
- **THEN** `[重试]`(墨底)与 `[刷新页面]`(发丝线描边)同时可见;暂停态显示 `[恢复会话]`

#### Scenario: 刷新页面动作
- **WHEN** 点击 `[刷新页面]`
- **THEN** 页面整页刷新,效果与浏览器刷新一致

#### Scenario: JS 契约保留
- **WHEN** 检查重连弹窗标记与脚本
- **THEN** 框架依赖的对话框 id、按钮 id、倒计时 span 与状态类名未被改动,模块脚本位于 `wwwroot/js/reconnect.js`,自动重试与恢复失败自动刷新行为不变

#### Scenario: 不依赖 scoped CSS
- **WHEN** 删除 `ReconnectModal.razor.css` 后在 `dotnet run` 开发态打开应用
- **THEN** 重连弹窗样式完整生效(规则来自 `app.css`),不出现样式包 500

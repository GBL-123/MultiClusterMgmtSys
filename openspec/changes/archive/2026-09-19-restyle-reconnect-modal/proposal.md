# 重连弹窗风格统一

## Why

断线重连弹窗(`ReconnectModal.razor`)是 Blazor 模板自带的"半主题化"样式:紫色主色回退 `#594AE2`、双径向渐变遮罩、12px 圆角 + elevation-24 阴影、999px 胶囊按钮、全英文文案。它是网络抖动/服务重启时用户看到的第一界面,观感与 Swiss Industrial Print(暖纸底、墨色、发丝线、3px 圆角、无阴影、中文界面)完全脱节,像是"外来组件"。

## What Changes

- **卡片语言替换**:遮罩改为墨色淡化(无渐变);卡片为 `#FCFBF7` + 1px `#E2DED5` 发丝线 + 3px 圆角 + 无阴影;头部琥珀方牌 + 中文标题;正文中文说明 + 等宽 mono 技术状态行(随状态切换);2px 琥珀不确定进度线;焦点环 `#D97706`、按压 `scale(0.98)`。
- **按钮语言替换**:`[重试]` 为墨底纸字主按钮(保留 `#components-reconnect-button`),`[恢复会话]` 为发丝线描边次按钮(保留 `#components-resume-button`),新增 `[刷新页面]` 次按钮(仅失败态随两按钮同现,模块 JS 内绑定 `location.reload()`,不引入内联脚本)。
- **JS 契约保留(位置迁移)**:`dialog#components-reconnect-modal`、两个既有按钮 id、`span#components-seconds-to-next-attempt`、全部状态类(`components-reconnect-*` / `components-pause*` / `components-resume-failed`)与重连/暂停/恢复状态机逐字保留;模块脚本由 collocated `Components/Layout/ReconnectModal.razor.js` 移至 `wwwroot/js/reconnect.js`(引用 `@Assets["js/reconnect.js"]`),规避 dev-run 静态资产 500 框架怪癖,使开发态弹窗可用可验收;id/类名与行为契约零变化。
- **样式落点迁移**:删除 `ReconnectModal.razor.css`,重连规则移入全局 `app.css` 设计语言区;删除后 scoped 包与清单引用均消失,移除 `App.razor` 的 `MultiClusterMgmtSys.Web.styles.css` 链接(已实测确认)。
- **兜底页面统一**:`Pages/NotFound.razor`(路由 `/not-found`)与 `Pages/Error.razor`(路由 `/Error`)从 Blazor 模板英文文案改为中文 + 工业词汇(`.fallback-page` 发丝线卡片、mono 代码行、墨底按钮「返回集群列表」);Error 页保留请求 ID 展示、移除开发环境说明段落。
- **契约同步**:`ui-theme` 强调色枚举由"刷新进度条"扩为"刷新与重连进度条";新增"重连弹窗视觉与文案"需求;组件例外清单补充 `[刷新页面]` 按钮 id;新增 `fallback-pages` 契约(404/错误页路由、中文文案、请求 ID 与返回入口、无模板英文残留)。
- **无行为变更**:不改变重连显示/隐藏、倒计时、自动重试、恢复失败自动刷新,以及 404/错误页的中间件管线与状态码语义;纯表现层替换。

## Capabilities

### New Capabilities
- `fallback-pages`: 404 与错误兜底页契约——路由(`/not-found`、`/Error`)、中文文案、请求 ID 展示、返回入口与无模板英文残留。

### Modified Capabilities
- `ui-theme`: 强调色出现位置枚举扩项(重连进度);组件优先策略例外清单补充重连刷新按钮 id;新增重连弹窗的卡片语言、中文状态文案、按钮语言与 JS 契约保留要求。

## Impact

- **代码**:`Web/Components/Layout/ReconnectModal.razor`(文案/结构)、模块脚本移至 `Web/wwwroot/js/reconnect.js` 并新增刷新按钮监听、`wwwroot/css/app.css`(新增重连样式段与兜底页样式段)、删除 `ReconnectModal.razor.css`、`Components/App.razor`(移除 styles 包链接)、`Components/Pages/NotFound.razor` 与 `Components/Pages/Error.razor`(中文 + 工业词汇重写,路由与布局保留)。
- **测试**:不新增(均为静态标记/CSS 与既有页面逻辑,无新行为);`dotnet build`、733 测试、`./coverage.ps1` 门禁不受影响。
- **文档**:AGENTS.md UI/CSS 节补记(重连样式与脚本位置、兜底页面约定);`ui-theme` 与 `fallback-pages` delta 随归档合并进主 spec。
- **范围外**:不引入 MudBlazor 对话框(JS 契约要求原生 `<dialog>`);不修复 dev-run 静态资产 500 本身(通过样式/脚本落点迁移规避);不改重连状态机与 404/错误页的中间件管线。

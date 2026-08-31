## Why

当前 UI 是一套默认 MudBlazor 模板长相(Tailwind slate 蓝 + 系统字体 + 6px 圆角),与任何脚手架项目无法区分;暗色模式与亮色模式共用同一套组件却无独立的视觉语言。作为长期盯屏的运维工具,需要一套有辨识度、耐看、数据密度高的视觉系统。

## What Changes

- 引入 "Swiss Industrial Print" 工业印刷设计系统:暖纸色基底、墨色文字、单一琥珀强调色、发丝线分格、小圆角、无阴影。
- 全站数据类内容(版本号、API Server、节点名、计数、时间戳)改用等宽字体 IBM Plex Mono;标题与数字使用 Space Grotesk;正文保留系统中文栈。
- 状态展示从实心色块 Chip 改为淡彩底 + 深字徽章(在线/离线/未知)。
- AppBar 增加品牌方牌与 mono 副标;导航激活态改为墨块反白。
- 表格密集化:2px 墨线表头、等宽数字、无竖分隔线、行 hover 暖灰。
- 新增设计 token 层(app.css):发丝线、inset 高光、状态徽章、空态等宽框、焦点环。
- **BREAKING**: 删除暗色模式——移除 `PaletteDark`、AppBar 主题切换按钮、`ThemeManager` 的 localStorage 持久化与系统偏好跟随逻辑(`mcm-theme-dark-mode`)。

## Capabilities

### New Capabilities

- `ui-theme`: 工业印刷视觉系统的设计 token(调色板、字体、圆角、阴影、状态徽章、表格/导航/AppBar 组件样式),以及暗色模式的移除契约。

### Modified Capabilities

<!-- 现有 spec 均属功能行为层,无主题相关需求;本变更不改变既有功能规格。 -->

## Impact

- `Components/Common/ThemeManager.cs` — 精简为纯主题配置,删除暗色逻辑
- `Components/Layout/MainLayout.razor` — 移除暗色绑定,仅保留 MudThemeProvider
- `Components/Layout/AppBar.razor` — 删除主题切换按钮,新增品牌方牌
- `Components/Layout/Drawer.razor` — 墨块激活态
- `wwwroot/css/app.css` — 设计 token 层
- 5 个列表页表格 + 过滤栏 + 对话框(集群/节点/ConfigMap/账号/审计):状态徽章、等宽数据列、密集行
- 字体资源:新增 `wwwroot/fonts/`(Space Grotesk + IBM Plex Mono woff2,OFL 许可证,提交进仓库)
- 不涉及服务层/数据库/API 变更;`ThemeManager` 依赖注入注册保持不变(或随精简调整)
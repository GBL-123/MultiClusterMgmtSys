## 1. 字体资源

- [x] 1.1 下载 Space Grotesk(可变/Regular+Medium+SemiBold+ Bold 子集)与 IBM Plex Mono(Regular+Medium+Semibold)的 woff2,放入 `MultiClusterMgmtSys/wwwroot/fonts/`,附 OFL 许可证文本
- [x] 1.2 在 `wwwroot/css/app.css` 添加 `@font-face`(Space Grotesk + IBM Plex Mono,`font-display: swap`),并建立 `.font-grotesk` / `.font-mono` 工具类

## 2. 主题与暗色模式删除

- [x] 2.1 重写 `Components/Common/ThemeManager.cs`:按 design.md D2 冻结值构建 `PaletteLight`(删除 PaletteDark),Typography 首字体位改 Space Grotesk,DefaultBorderRadius 3px;删除 `IsDarkMode`/`ObserveSystemDarkModeChange`/`ToggleDarkModeAsync`/`InitializeAsync`/`StorageKey` 与全部 localStorage 逻辑
- [x] 2.2 `Components/Layout/MainLayout.razor`:移除 `IsDarkMode` 绑定、`IsDarkModeChanged`、`ToggleTheme` 与 `ThemeManager` 注入,`MudThemeProvider` 仅挂静态主题
- [x] 2.3 `Components/Layout/AppBar.razor`:删除暗色切换按钮及其 `ThemeManager` 注入,改为纸色 + 底部发丝线
- [x] 2.4 全局搜索确认无 `PaletteDark`、`IsDarkMode`、`mcm-theme-dark-mode`、`ToggleDarkModeAsync` 残留引用

## 3. app.css 设计 token 层

- [x] 3.1 实现状态徽章 `.status-badge`(淡彩底 + 深字,在线/离线/未知三态)与 `.status-dot`(如需)
- [x] 3.2 实现表头 2px 墨线(作用于 `.mud-table-head`)、行 hover 暖灰、`tabular-nums` 全局数据数字
- [x] 3.3 实现导航激活墨块(`.mud-nav-link.active` 反白)与抽屉发丝线
- [x] 3.4 实现琥珀焦点环、按钮按压 `scale(0.98)` 与悬停背景变化
- [x] 3.5 实现空态虚线框 `.empty-state`(等宽字)与细石墨滚动条
- [x] 3.6 实现品牌方牌 `.appbar-brand`(琥珀方牌)与 AppBar 底部发丝线

## 4. 布局组件

- [x] 4.1 `AppBar.razor` 加入品牌方牌 + 标题 + 等宽副标 `MCM // CONTROL`(Caption 级,宽字距)
- [x] 4.2 `Drawer.razor` 应用墨块激活态与纸色背景(验证 `DrawerVariant.Mini` 下激活样式)

## 5. 表格与状态徽章逐页改造

- [x] 5.1 集群表 `ClusterTable.razor` + 详情页 `ClusterOverviewCard`/`ClusterNodesCard`:状态徽章改 `.status-badge`,版本/API Server/节点名/计数/时间戳列加 `.font-mono`
- [x] 5.2 节点表 `NodeListTable.razor` + 节点详情卡片:状态与数据列等宽化,IP/节点名/计数
- [x] 5.3 ConfigMap 表 `ConfigMapListTable.razor`:名称/命名空间/更新时间等宽化
- [x] 5.4 账号表 `AccountTable.razor` + 审计日志表:创建时间/登录时间等宽化
- [x] 5.5 统一 5 个表格的 `LoadingContent` 与 `NoRecordsContent`(空态虚线框样式)
- [x] 5.6 逐页验证 MudBlazor CSS 覆盖生效(集群/节点/ConfigMap/账号/审计/详情页)

## 6. 构建与验收

- [x] 6.1 `dotnet build MultiClusterMgmtSys.slnx` 通过,0 错误
- [x] 6.2 `dotnet run --project MultiClusterMgmtSys` 视觉验收:主题、字体、状态徽章、空态、焦点环、按压反馈、无暗色入口
- [x] 6.3 提交(短中文提交信息),字体文件与设计变更一并入库
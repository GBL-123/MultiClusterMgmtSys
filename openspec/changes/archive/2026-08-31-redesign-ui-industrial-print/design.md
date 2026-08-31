## Context

当前 UI 由 `Components/Common/ThemeManager.cs` 定义的一套 MudBlazor 主题驱动:Tailwind slate 调色板(`#2563EB` Primary)、系统字体栈、6px 全局圆角,并带有完整的明暗双模式(本地存储 `mcm-theme-dark-mode` + 系统偏好跟随)。自定义 CSS(`wwwroot/css/app.css`)仅 38 行布局 workaround,无设计 token。全部 5 个列表页(集群/节点/ConfigMap/账号/审计)共用「工具栏纸片 + MudTable」骨架,状态用实心 `MudChip Variant.Filled`。

约束:Blazor Server + MudBlazor 9.9,单一 csproj,无前端构建链(无 npm/webpack),UI 字符串全中文,`BlazorDisableThrowNavigationException` 开启,`min-width: 1280px` 桌面专用布局保留。

## Goals / Non-Goals

**Goals:**
- 落地 "Swiss Industrial Print" 全亮色设计系统:暖纸基底 + 墨色 + 单一琥珀强调色
- 数据类内容等宽化(版本/API/节点名/计数/时间戳),标题数字用 Space Grotesk
- 状态展示改淡彩底 + 深字徽章
- 建立 app.css 设计 token 层(发丝线、inset 高光、状态徽章、空态框、焦点环)
- **删除暗色模式**(PaletteDark、切换按钮、localStorage/系统跟随逻辑)

**Non-Goals:**
- 不重构导航结构(保留 Mini 抽屉)、不引入新功能(全局健康指示 `在线 n/m` 属后续变更)
- 不更换图标库(保留 `Icons.Material.Filled`,统一描边风格)
- 不改移动端适配(桌面工具,`min-width: 1280px` 保留)
- 不引入前端构建链;全部样式经 MudBlazor 主题 + app.css 完成

## Decisions

**D1. 视觉基底:V2 纯印刷(全亮),删除暗色模式**
用户明确选择严格 V2:仅亮色基底,移除 `PaletteDark`、AppBar 主题切换按钮、`ThemeManager` 中 `IsDarkMode`/`ObserveSystemDarkModeChange`/`ToggleDarkModeAsync`/localStorage 逻辑。`MudThemeProvider` 仍需保留(它是应用 MudTheme 的机制),但去掉 `IsDarkMode` 双向绑定。`ThemeManager` 保留 DI 注册,精简为只暴露构建好的 `MudTheme`(实现上可退化为静态工厂)。
- 备选:保留暗色作为 CRT 变体(用户已否决,理由:两套视觉语言维护成本 + 产品决策要"一个样")。

**D2. 调色板(冻结值)**
```
纸 Background #F4F4F0 │ 卡面 Surface #FCFBF7 │ 发丝线 #E2DED5
墨 Primary/TextPrimary #111111 │ PrimaryContrastText #FFFFFF │ 次文字 #6E675C
强调色(琥珀) #B45309 —— 仅用于品牌方牌/focus 环/刷新进度条/空态框
状态:在线 #EDF3EC 底/#346538 字;离线 #FDEBEC 底/#9F2F2D 字;未知 #FBF3DB 底/#956400 字
语义色:Success #346538 / Warning #956400 / Error #9F2F2D(深字用)
Divider #E2DED5 │ DrawerBackground/AppbarBackground = 纸色(纯印刷,无深色机壳)
圆角 DefaultBorderRadius 3px │ 阴影:LayoutProperties 不设重阴影,层级靠发丝线 + inset 高光
```

**D3. 字体:自托管 woff2,OFL 许可证**
- `Space Grotesk`(latin + 数字,标题/大数字,`font-variant-numeric: tabular-nums`)
- `IBM Plex Mono`(数据列:版本号、API Server、节点名、计数、时间戳)
- 中文正文保留系统栈(PingFang SC / Microsoft YaHei / Noto Sans SC)——自托管 CJK 字体数 MB 级,不引入
- 字体文件提交进仓库(`wwwroot/fonts/`),`@font-face` + `font-display: swap` 写进 app.css;ThemeManager Typography 的 `FontFamily` 数组首位置放新字体,中文回退由系统栈兜底
- 备选:Google Fonts CDN —— 否决,内网/生产 nginx 部署下外链不可控

**D4. 样式分层:MudTheme(生成式)+ app.css(覆盖式 token)**
- MudTheme 负责 MudBlazor 组件能消费的 palette/typography/radius
- app.css 负责 MudBlazor 覆盖不到或需微调的:`.status-badge`(淡彩底)、`.mono`(等宽数据)、`.appbar-brand`、导航激活墨块(`.mud-nav-link.active`)、表头 2px 墨线、空态虚线框、琥珀焦点环、按钮按压 `scale(0.98)`、行 hover 暖灰、滚动条(细石墨)
- 选择器需针对 MudBlazor 9.x 的 DOM 结构验证,优先用 MudBlazor 已有类(`mud-table-head`, `mud-nav-link` 等)减少侵入

**D5. 状态徽章:淡彩底 + 深字**
保留 `MudChip Variant.Text` 组件(改动最小),用 CSS 覆盖成"淡彩底 + 深字 + 3px 圆角";颜色从 ThemeManager 语义色映射(Success/Warning/Error),离线=Error、在线=Success、未知=Default。全站 5 个表格 + 节点/集群详情卡片统一。

**D6. 品牌方牌**
AppBar 左侧:`MudIconButton`(菜单)后加一个 28×28 琥珀底方牌(内嵌白色粗体字形/图标,如 `▮` 或 `Icons.Material.Filled.TravelExplore` 反白)+ 标题「多集群管理系统」+ mono 副标 `MCM // CONTROL`(Caption,次文字色,宽字距)。

**D7. 暗色模式删除清单(必须全删,不留死代码)**
- `ThemeManager.cs`:PaletteDark、IsDarkMode、ObserveSystemDarkModeChange、ToggleDarkModeAsync、StorageKey、InitializeAsync
- `MainLayout.razor`:`IsDarkMode` 绑定、`ToggleTheme`、`ThemeManager` 注入(若退化静态)
- `AppBar.razor`:主题切换按钮及其 `ThemeManager` 注入
- app.css:无暗色相关遗留(当前没有)

**D8. 空态与加载态**
- 空态:`MudTable` 的 `NoRecordsContent` 改为等宽字 + 虚线框(`[ 暂无集群 ]` 风格)
- 加载态:保留 `LoadingContent` 但换文案与样式(骨架 shimmer 若实现成本高可退化为等宽 `正在加载...`,列为任务中的可选增强)

## Risks / Trade-offs

- [中文字形与拉丁字体混排时基线/字距不齐] → 标题/数字只用 Space Grotesk,中文标题维持系统栈;视觉验收时检查混合字符串(如「集群 v1.30.3」)
- [删除暗色模式损失偏好深色用户] → 产品决策(用户已确认);localStorage 键彻底移除,不残留
- [MudBlazor CSS 特异性冲突,覆盖失效] → 每个覆盖类在目标页实机验证;优先用 MudBlazor 官方类名挂载
- [woff2 需联网下载一次] → 实施时下载并提交仓库,附带 OFL 许可说明;字体不可用时有系统栈兜底,不阻塞渲染
- [MudChip Variant.Text 覆盖成淡彩底可能被 MudBlazor 升级破坏] → 覆盖集中在 `.status-badge` 单一入口,升级后只需修一处

## Migration Plan

- 纯前端变更,无数据库/API 迁移。部署照常 `docker compose -f docker-compose.prod.yml up -d --build`
- 回滚 = git revert;暗色删除无数据残留,localStorage 键自然失效

## Open Questions

- 无阻塞性问题。骨架 shimmer 是否实现(见 D8)留待任务执行时按成本决定,不影响 spec 契约。
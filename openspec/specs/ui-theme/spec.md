# ui-theme

## Purpose

为系统提供 "Swiss Industrial Print" 工业印刷视觉系统契约：暖纸色基底 + 墨色文字 + 单一琥珀强调色、发丝线分格、小圆角、无投影阴影；自托管拉丁字体（Space Grotesk / IBM Plex Mono）+ 系统中文栈；淡彩底状态徽章；AppBar 品牌区与墨块导航激活态；密集表格、等宽数据列；空态虚线框与加载反馈；琥珀焦点环与按压缩放反馈；并约定移除暗色模式（无 `PaletteDark`、无主题切换入口、无本地偏好残留）。

## Requirements

### Requirement: 设计 token(调色板/圆角/阴影)
系统 SHALL 使用 Swiss Industrial Print 全亮色设计 token:MudTheme 与 app.css 中 SHALL 使用暖纸背景 `#F4F4F0`、卡面 `#FCFBF7`、发丝线 `#E2DED5`、墨色主文字/主色 `#111111`、次文字 `#6E675C`、单一琥珀强调色 `#B45309`。默认圆角 SHALL 为 3px。UI 层级 SHALL 通过发丝线与 inset 高光表达,不使用投影阴影。

#### Scenario: 主题加载
- **WHEN** 应用启动并渲染任意页面
- **THEN** 页面背景为暖纸色,卡片为卡面色,分隔线为发丝线色,主按钮为墨底白字

#### Scenario: 强调色稀缺性
- **WHEN** 检查强调色出现位置
- **THEN** 琥珀色仅出现在品牌方牌、焦点环、刷新进度条与空态框中,不用于常规按钮与文字

### Requirement: 字体体系
系统 SHALL 自托管两种拉丁字体:Space Grotesk(标题与数字)与 IBM Plex Mono(数据类内容);中文正文 SHALL 使用系统中文栈(PingFang SC / Microsoft YaHei / Noto Sans SC)。字体文件 SHALL 存放于 `wwwroot/fonts/` 并提交进仓库,通过 `@font-face` 加载并设置 `font-display: swap`。数字 SHALL 启用 `font-variant-numeric: tabular-nums`。

#### Scenario: 数据列等宽渲染
- **WHEN** 表格展示版本号、API Server、节点名、计数或时间戳列
- **THEN** 这些单元格使用 IBM Plex Mono 渲染,数字对齐一致

#### Scenario: 中文字体回退
- **WHEN** 页面渲染中文文本
- **THEN** 中文由系统中文栈渲染,拉丁字体文件缺失时不阻塞中文显示

### Requirement: 状态徽章
系统 SHALL 用「淡彩底 + 深字」徽章表达集群/节点状态,替代实心色块:在线 = `#EDF3EC` 底 + `#346538` 字;离线 = `#FDEBEC` 底 + `#9F2F2D` 字;未知 = `#FBF3DB` 底 + `#956400` 字。徽章圆角 SHALL 为 3px。该样式 SHALL 统一应用于集群表、节点表、ConfigMap 表与详情页状态展示。

#### Scenario: 在线集群展示
- **WHEN** 集群状态为在线
- **THEN** 其状态徽章显示为淡绿底深绿字

#### Scenario: 离线集群展示
- **WHEN** 集群状态为离线
- **THEN** 其状态徽章显示为淡红底深红字

### Requirement: AppBar 品牌与操作
AppBar SHALL 为纸色背景 + 底部发丝线,左侧依次为菜单按钮、琥珀色品牌方牌、标题「多集群管理系统」与等宽副标 `MCM // CONTROL`。AppBar SHALL NOT 包含暗色模式切换按钮。

#### Scenario: 品牌区渲染
- **WHEN** 用户登录后看到 AppBar
- **THEN** 品牌方牌为琥珀底、标题可见、副标以等宽字体渲染

#### Scenario: 无主题切换入口
- **WHEN** 用户查看 AppBar 操作区
- **THEN** 不存在深色/浅色切换按钮

### Requirement: 导航激活态
抽屉导航 SHALL 以墨块反白表达当前页面:激活项为墨色底 + 纸色文字,非激活项为次文字色。抽屉 SHALL 为纸色背景 + 右侧发丝线。

#### Scenario: 激活导航高亮
- **WHEN** 用户位于集群管理页
- **THEN** 抽屉中「集群管理」项显示为墨块反白,其余项为次文字色

### Requirement: 表格数据呈现
列表表格 SHALL:表头底部使用 2px 墨色分隔线;数据列中的版本/API Server/节点名/计数/时间戳使用等宽字体与 tabular 数字;行 hover 为暖灰色;不显示竖分隔线;行高保持密集。

#### Scenario: 表头分隔线
- **WHEN** 渲染集群/节点/ConfigMap/账号/审计任一表格
- **THEN** 表头与数据区之间显示 2px 墨色线

#### Scenario: 行悬停
- **WHEN** 鼠标悬停于表格行
- **THEN** 该行背景变为暖灰色

### Requirement: 空态与加载态
表格空态 SHALL 使用等宽字体 + 虚线框呈现(如 `[ 暂无集群 ]` 风格)。加载态 SHALL 提供明确反馈文案或骨架占位。

#### Scenario: 空表展示
- **WHEN** 集群表没有任何记录且无筛选条件
- **THEN** 空态区域显示等宽字体的虚线框提示

#### Scenario: 加载反馈
- **WHEN** 表格数据加载中
- **THEN** 用户可见加载中提示而非空白区域

### Requirement: 焦点与按压反馈
可交互元素 SHALL 有琥珀色可见焦点环;按钮按压 SHALL 有轻微缩放反馈(`scale(0.98)`),悬停 SHALL 有背景变化。

#### Scenario: 键盘焦点
- **WHEN** 用户用 Tab 键聚焦按钮或输入框
- **THEN** 该元素显示琥珀色焦点环

#### Scenario: 按钮按压
- **WHEN** 用户按下主按钮
- **THEN** 按钮轻微缩小并在松开后恢复

### Requirement: 暗色模式移除
系统 SHALL NOT 提供暗色模式:SHALL 移除 `PaletteDark` 定义、AppBar 主题切换按钮、`ThemeManager` 的 `IsDarkMode`/`ObserveSystemDarkModeChange`/`ToggleDarkModeAsync`/`InitializeAsync` 与 `mcm-theme-dark-mode` localStorage 读写逻辑。移除后 SHALL 无相关死代码残留。

#### Scenario: 无暗色调色板
- **WHEN** 检查主题配置
- **THEN** 不存在 `PaletteDark` 或等价暗色定义

#### Scenario: 无本地偏好残留
- **WHEN** 用户刷新页面或更换浏览器
- **THEN** 界面始终为亮色印刷基底,不受任何已存储偏好影响
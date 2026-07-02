# 集群管理 UI 主题设计文档

> 状态：设计阶段（待评审） · 适用技术：.NET 10 Blazor Server + MudBlazor 9.6
> 本文档只描述主题与视觉规范，不给出具体实现代码。

---

## 1. 主题系统架构

### 1.1 核心接线方式

使用 MudBlazor 9.6 的 `MudThemeProvider` 参数绑定（API 已对照源码核实）：

| 参数 | 类型 | 默认 | 作用 |
|------|------|------|------|
| `Theme` | `MudTheme?` | 内置默认主题 | 包含 `PaletteLight`、`PaletteDark` 两套调色板及 Typography、LayoutProperties、Shadows、ZIndex 等配置 |
| `IsDarkMode` | `bool` | `false` | 控制当前渲染哪一套调色板；`true` 启用暗色模式 |
| `IsDarkModeChanged` | `EventCallback<bool>` | — | `IsDarkMode` 变化时触发；用于系统偏好自动跟随场景同步应用状态 |
| `ObserveSystemDarkModeChange` | `bool` | `true` | 内置监听 OS `prefers-color-scheme` 变化（见 §1.4），**默认即开启** |
| `DefaultScrollbar` | `bool` | `false` | 是否用浏览器原生滚动条 |

> 注：`Direction`（RTL/LTR）不是 `MudThemeProvider` 的参数，由独立的 `MudRTLProvider` 负责，本项目不涉及。

在 `MainLayout.razor` 中替换为：

```text
<MudThemeProvider Theme="@_theme"
                 IsDarkMode="@_isDarkMode"
                 IsDarkModeChanged="@(v => _isDarkMode = v)"
                 ObserveSystemDarkModeChange="true" />
```

> 注：`MudTheme` 持有 `PaletteLight`（`PaletteLight` 子类）与 `PaletteDark`（`PaletteDark` 子类）两个属性；`Palette` 为二者共同的抽象基类。两套色板分别在对应属性上配置。

### 1.2 状态管理位置

推荐新增一个 scoped 服务 `Services/ThemeService.cs`：

| 职责 | 说明 |
|------|------|
| 持有 `IsDarkMode` 状态 | 作为单一事实源，供 `MainLayout` 绑定到 `MudThemeProvider` |
| 封装 JS interop | 读取/写入 `localStorage`，读取 OS `prefers-color-scheme` |
| 提供 `ToggleDarkMode()` 方法 | 切换状态并持久化 |
| 提供初始化方法 | 在应用启动时按「已保存偏好 > 系统偏好 > 默认 false」的顺序决定初始模式 |

替代方案：仅把状态放在 `MainLayout.razor` 的 `@code` 中。不推荐，因为后续其他组件（如详情页、Dialog）可能需要感知主题或读取偏好；集中管理更利于维护。

### 1.3 持久化

- **存储键名**：`mcm-theme-dark-mode`
- **存储值**：布尔字符串 `"true"` / `"false"`
- **读写方式**：通过 `IJSRuntime` 调用 `localStorage.setItem` / `getItem`
- **时机**：用户点击切换按钮时立即写入；应用初始化时读取
- **注意**：Blazor Server 在 `OnInitializedAsync` 中调用 JS interop 是安全的；若选择在 `App.razor` 预读，需确认其生命周期与 JS 可用性

### 1.4 系统偏好与首次访问

首次访问（`localStorage` 中无保存值）时，默认跟随操作系统。**MudBlazor 9.6 内置 `prefers-color-scheme` 跟踪，无需手动 JS interop**：`MudThemeProvider.ObserveSystemDarkModeChange` 默认 `true`，其内置 JS 模块（`mudThemeProvider.js`）会监听 `window.matchMedia('(prefers-color-scheme: dark)')`，OS 主题变化时自动调用 `[JSInvokable] SystemDarkModeChangedAsync` 并触发 `IsDarkModeChanged` 回调。

决策逻辑（保存值 > 系统偏好 > 默认 false）：

| 步 | 行为 |
|----|------|
| 1 | 检查 `localStorage`：若存在 `mcm-theme-dark-mode`，直接采用，**并临时关闭系统自动跟随**（用户已显式选择，不应被 OS 变更覆盖） |
| 2 | 若无保存值：依赖内置 `ObserveSystemDarkModeChange="true"` 自动跟随 OS；初始值可由 `MudThemeProvider` 的 C# 方法 `GetSystemDarkModeAsync()` 一次性读取 |
| 3 | 回退：两者都不可用时默认 `false`（浅色模式） |

> 实现提示：若用户已保存显式偏好，可在 `ThemeService` 中将 `ObserveSystemDarkModeChange` 设为 `false` 避免被 OS 变更意外覆盖；未保存偏好时保持 `true` 以获得自动跟随体验。`GetSystemDarkModeAsync()` 与 `WatchSystemDarkModeAsync()` 是 provider 暴露的 C# 公共方法，可直接调用而无需手写 JS。

### 1.5 切换按钮 UX

将 `MainLayout.razor` 中当前无功能的太阳图标改为可用主题切换按钮：

- **位置**：保留在 `AppBar` 右侧、`MudSpacer` 之后，作为全局控件
- **图标**：
  - 浅色模式时显示 `Icons.Material.Filled.DarkMode`（提示可切换为暗色）
  - 暗色模式时显示 `Icons.Material.Filled.LightMode`（提示可切换为浅色）
- **颜色**：`Color="Color.Inherit"`，使图标随 AppBar 文本色自动适配
- **ARIA/提示**：建议加 `Title` 或 `AriaLabel` 属性，值为「切换深色模式」
- **交互**：点击调用 `ThemeService.ToggleDarkMode()`，更新 `IsDarkMode` 并写回 `localStorage`

---

## 2. 调色板设计

主色选择 **Indigo-Blue（靛蓝）** `#2563EB`：
- 在基础设施 / DevOps / 云原生管理工具中广泛出现（Kubernetes Dashboard、Lens、Azure Portal、AWS Console 均偏蓝），用户潜意识能识别为「运维控制台」。
- 冷暖适中，在线/成功绿、离线/错误红与其并置时对比清晰，状态可读性强。

### 2.1 浅色模式调色板

| 属性 | 色值 | 用途说明 |
|------|------|----------|
| `Primary` | `#2563EB` | 主按钮、激活导航、状态 chip（在线）、链接、进度条 |
| `PrimaryContrastText` | `#FFFFFF` | 主色背景上的文字/图标 |
| `Secondary` | `#64748B` | 次要按钮、辅助图标、占位文字 |
| `SecondaryContrastText` | `#FFFFFF` | 次要色背景上的文字 |
| `Background` | `#F8FAFC` | 页面底层背景，比纯白更柔和，减少刺眼感 |
| `Surface` | `#FFFFFF` | 卡片、Drawer、Dialog、表单容器表面 |
| `AppbarBackground` | `#FFFFFF` | 顶部导航栏背景，营造干净控制面板感 |
| `AppbarText` | `#0F172A` | AppBar 标题与图标颜色 |
| `DrawerBackground` | `#FFFFFF` | Mini Drawer 背景 |
| `DrawerText` | `#0F172A` | Drawer 文字/图标颜色 |
| `TextPrimary` | `#0F172A` | 主标题、正文 |
| `TextSecondary` | `#475569` | 副标题、描述、数量统计 |
| `Divider` | `#E2E8F0` | 列表分隔线、边框、表头下划线 |
| `Success` | `#16A34A` | 成功状态、在线 chip（备用）、健康指标 |
| `Warning` | `#D97706` | 警告、需要关注的状态 |
| `Error` | `#DC2626` | 错误、离线 chip、删除操作 |
| `Info` | `#0891B2` | 提示信息、信息 chip、连接方式标识 |
| `Hover`（默认悬停） | 使用 MudBlazor 默认生成的透明黑遮罩 | 表格行、列表项悬停 |

### 2.2 暗色模式调色板

暗色模式不使用纯黑，而是采用 **Slate（石板灰蓝）** 系背景，降低对比眩光，更符合长时间盯屏的运维场景。

| 属性 | 色值 | 用途说明 |
|------|------|----------|
| `Primary` | `#60A5FA` | 暗色下的主色，比浅色主色更亮以保证对比度 |
| `PrimaryContrastText` | `#0F172A` | 亮主色上的文字 |
| `Secondary` | `#94A3B8` | 次要操作、辅助文字 |
| `SecondaryContrastText` | `#0F172A` | 次要色背景上的文字 |
| `Background` | `#0F172A` | 页面底层背景（深蓝灰） |
| `Surface` | `#1E293B` | 卡片、Dialog、表单容器 |
| `AppbarBackground` | `#1E293B` | 顶部导航栏与 Surface 同层或略深 |
| `AppbarText` | `#F1F5F9` | AppBar 标题与图标颜色 |
| `DrawerBackground` | `#0F172A` | Drawer 比 Surface 略深，形成层级 |
| `DrawerText` | `#F1F5F9` | Drawer 文字/图标颜色 |
| `TextPrimary` | `#F1F5F9` | 主标题、正文 |
| `TextSecondary` | `#94A3B8` | 副标题、描述 |
| `Divider` | `#334155` | 分隔线，比背景明显但不过亮 |
| `Success` | `#4ADE80` | 暗色下的成功绿 |
| `Warning` | `#FBBF24` | 暗色下的警告黄 |
| `Error` | `#F87171` | 暗色下的错误红 |
| `Info` | `#22D3EE` | 暗色下的信息青 |

### 2.3 语义色使用原则

| 场景 | 浅色模式 | 暗色模式 |
|------|----------|----------|
| 在线状态 chip | `Success` 填充 | `Success` 填充 |
| 离线状态 chip | `Error` 填充 | `Error` 填充 |
| 未知状态 chip | `Default` / `Secondary` 填充 | `Default` / `Secondary` 填充 |
| 主要操作按钮 | `Primary` Filled | `Primary` Filled |
| 次要/重置按钮 | `Secondary` Text | `Secondary` Text |
| 删除按钮 | `Error` Text/Outlined | `Error` Text/Outlined |

---

## 3. 排版规范

### 3.1 字体家族

采用系统字体栈，保证中/英文混合渲染的清晰度与加载速度，符合管理工具对性能与可读性的要求：

```text
-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, "Noto Sans", "PingFang SC", "Microsoft YaHei", sans-serif
```

### 3.2 设计原则

- **可读性优先**：不使用装饰性字体，行高适中，避免长文本拥挤。
- **密度适中**：正文大小控制在 13–14px，标题与正文拉开层级。
- **字重克制**：标题用 600，按钮/标签用 500，正文用 400，避免过细字重在暗色下发虚。

### 3.3 Typography 覆盖建议

| 样式 | 字号 | 字重 | 行高 | 字间距 | 用途 |
|------|------|------|------|--------|------|
| `H4` | 1.5rem (24px) | 600 | 1.3 | -0.01em | 页面大标题（如「集群管理」） |
| `H5` | 1.25rem (20px) | 600 | 1.35 | -0.005em | 内容区标题（当前分组名） |
| `H6` | 1.125rem (18px) | 600 | 1.4 | 0 | 卡片标题、Dialog 标题 |
| `Subtitle1` | 1rem (16px) | 500 | 1.5 | 0 | 侧边栏分组标题、列表主名称 |
| `Subtitle2` | 0.875rem (14px) | 500 | 1.5 | 0 | 小标题、表单分组标签 |
| `Body1` | 0.875rem (14px) | 400 | 1.6 | 0 | 主要正文、表格单元格 |
| `Body2` | 0.8125rem (13px) | 400 | 1.5 | 0 | 辅助说明、元数据 |
| `Button` | 0.8125rem (13px) | 500 | 1.5 | 0.02em | 按钮文字 |
| `Caption` | 0.75rem (12px) | 400 | 1.4 | 0 | 时间戳、提示、徽标计数 |
| `Overline` | 0.6875rem (11px) | 500 | 1.4 | 0.08em | 标签大写（少用） |

---

## 4. 形状、阴影与密度

### 4.1 圆角

管理工具应呈现「利落、工具感」而非「圆润、消费感」：

| 元素 | 圆角建议 |
|------|----------|
| `LayoutProperties.DefaultBorderRadius` | `6px` |
| 按钮（Button） | `6px` |
| 输入框（TextField/Select） | `6px` |
| 卡片（Card） | `6px` |
| Dialog | `8px` |
| Chip | `4px`（小号）或继承默认 |
| Table 单元格/表头 | 不单独设置圆角 |

### 4.2 阴影策略

| 模式/元素 | 阴影 | 说明 |
|-----------|------|------|
| 浅色卡片 | `Elevation="1"` | 轻微浮起，边界清晰 |
| 浅色卡片悬停 | `Elevation="2"` | 提示可交互 |
| 暗色卡片 | `Elevation="1"` 或 `2` | 暗色下阴影不易察觉，可辅以 1px divider 边框 |
| 表格 | 无阴影 | 用 divider 和 hover 背景区分行 |
| Dialog | `Elevation="8"` | 模态需要明确层级 |
| Drawer（Mini） | `Elevation="1"` | 默认即可 |
| AppBar | `Elevation="1"` | 与内容区形成分隔 |

### 4.3 密度

数据密集型管理界面建议使用 **Dense 模式**：

- `MudTable`：`Dense="true"`、`Hover="true"`
- `MudTextField`、`MudSelect`、`MudAutocomplete`：默认或 `Dense="true"`
- `MudButton`：操作列使用 `Size="Size.Small"`
- `MudChip`：状态 chip 使用 `Size="Size.Small"`
- `MudNavLink`（Mini Drawer）：保持默认高度即可，避免过挤

---

## 5. 应用外壳视觉

### 5.1 AppBar

| 属性 | 浅色模式 | 暗色模式 |
|------|----------|----------|
| 高度 | 56px（默认） | 56px |
| 背景色 | `#FFFFFF` | `#1E293B` |
| 文字/图标色 | `#0F172A` | `#F1F5F9` |
| 阴影 | `Elevation="1"` | `Elevation="1"` |
| 左侧 | 汉堡菜单 + 应用图标 + 标题 | 同左 |
| 右侧 | 主题切换按钮（位于 `MudSpacer` 后） | 同左 |
| 标题 | `MudText Typo="Typo.h6"`，字重 600 | 同左 |
| 应用图标 | `TravelExplore`，颜色 Inherit | 同左 |

### 5.2 Mini Drawer

| 属性 | 浅色模式 | 暗色模式 |
|------|----------|----------|
| 宽度 | 56px（Mini 默认） | 56px |
| 背景 | `#FFFFFF` | `#0F172A` |
| 文字/图标 | `#0F172A` | `#F1F5F9` |
| 激活项背景 | `Primary` 10% 透明或 `rgba(37, 99, 235, 0.1)` | `rgba(96, 165, 250, 0.15)` |
| 激活项文字/图标 | `Primary` | `Primary` |
| 悬停背景 | `rgba(15, 23, 42, 0.04)` | `rgba(241, 245, 249, 0.06)` |
| 项间距 | 默认 | 默认 |

### 5.3 整体氛围

- **控制面板感**：浅色模式下白色 AppBar + 浅灰背景 + 卡片/表格内容区，类似现代云控制台；暗色模式下深蓝灰背景 + 略亮表面，适合夜间运维。
- **一致性**：所有表面、文字、分割线的明暗关系在两套模式下保持一致（背景最暗/最浅，Surface 高一阶，AppBar/Drawer 与 Surface 同阶或相邻）。
- **可聚焦**：主色仅用于真正需要强调的元素（主按钮、激活状态、链接），避免到处高饱和。

---

## 6. 组件风格指南

### 6.1 MudCard（集群卡片）

- `Elevation="1"`，圆角 6px，背景 `Surface`
- 标题 `Typo="Typo.h6"`，单行截断
- 状态 chip 置于标题右侧，小号填充
- 信息行 label 使用 `TextSecondary`，value 使用 `TextPrimary`
- 操作区按钮使用 `Size="Size.Small"`，主/次/错误色区分

### 6.2 MudTable（表格视图）

- `Dense="true"`、`Hover="true"`、`Striped="false"`
- 表头文字使用 `TextSecondary`，字号 `Subtitle2`，字重 500
- 表头底部分隔线颜色 `Divider`
- 可排序列使用 `MudTableSortLabel`，激活排序时文字变 `Primary`
- 操作列按钮仅图标，紧凑排列

### 6.3 MudChip（状态）

| 状态 | 变体 | 颜色 |
|------|------|------|
| 在线 | `Variant="Variant.Filled"` | `Success` |
| 离线 | `Variant="Variant.Filled"` | `Error` |
| 未知 | `Variant="Variant.Filled"` | `Default` 或 `Secondary` |
| 连接方式 KubeConfig | `Variant="Variant.Outlined"` | `Info` |
| 连接方式 Token | `Variant="Variant.Outlined"` | `Info` |

所有状态 chip 使用 `Size="Size.Small"`。

### 6.4 MudButton

| 用途 | 变体 | 颜色 | 尺寸 |
|------|------|------|------|
| 添加集群 | `Filled` | `Primary` | 默认 |
| 刷新/编辑 | `Text` | `Default` | `Small` |
| 删除 | `Text` | `Error` | `Small` |
| 重置筛选 | `Text` | `Secondary` | 默认 |
| 保存/提交 | `Filled` | `Primary` | 默认 |
| 取消 | `Text` | `Secondary` | 默认 |

### 6.5 MudDialog（添加/编辑集群）

- `MaxWidth="MaxWidth.Medium"`，`FullWidth="true"`
- 圆角 8px，阴影 `Elevation="8"`
- 标题 `Typo="Typo.h6"`，底部 1px divider
- 内容区padding 默认，表单控件纵向间距 16px
- 操作区按钮右对齐：取消（Text Secondary）+ 保存/提交（Filled Primary）

### 6.6 MudTextField / MudSelect / MudAutocomplete

- 推荐使用 `Variant="Variant.Outlined"`（工具感更强，边界清晰）
- `Dense="true"` 用于筛选栏、表格上方工具栏
- 默认密度用于 Dialog 表单
- Label 使用 `TextSecondary`，聚焦时 Label 与边框变 `Primary`

### 6.7 MudProgressLinear

- `Color="Color.Primary"`
- 不确定模式用于页面级加载
- 确定模式可用于需要进度的场景（如未来批量操作）
- 高度使用默认或 2px，避免过度抢眼

---

## 7. 实现要点

### 7.1 涉及文件

| 文件 | 改动内容 |
|------|----------|
| `MultiClusterMgmtSys/Components/Layout/MainLayout.razor` | 1) `MudThemeProvider` 增加 `Theme` 与 `IsDarkMode` 绑定；2) 主题切换按钮改为可用；3) 注入 `ThemeService` |
| `MultiClusterMgmtSys/Services/ThemeService.cs`（新增） | 定义 `MudTheme` 实例、持有 `IsDarkMode`、封装 `localStorage` 读写与系统偏好读取 |
| `MultiClusterMgmtSys/Program.cs` | 注册 `ThemeService` 为 Scoped 服务 |
| `MultiClusterMgmtSys/Components/App.razor`（可选） | 若需在渲染前读取系统偏好，可在此初始化；否则由 `MainLayout` 在 `OnAfterRenderAsync` 中读取 |

### 7.2 实现步骤清单

1. 新建 `Services/ThemeService.cs`，定义完整 `MudTheme`（含 `PaletteLight`、`PaletteDark`、`Typography`、`LayoutProperties`）。
2. 在 `ThemeService` 中实现：
   - `bool IsDarkMode { get; set; }`
   - `Task InitializeAsync()`：读 `localStorage`，无值则读 `prefers-color-scheme`
   - `Task ToggleDarkModeAsync()`：切换并持久化
   - `MudTheme Theme { get; }`
3. `Program.cs` 中 `builder.Services.AddScoped<ThemeService>()`。
4. `MainLayout.razor` 中注入 `ThemeService`，替换 `<MudThemeProvider />` 为带绑定版本。
5. 将 AppBar 右侧太阳图标改为条件渲染的月亮/太阳切换按钮，点击调用 `ThemeService.ToggleDarkModeAsync()`。
6. 在 `MainLayout.razor` 的 `OnAfterRenderAsync` 中调用 `ThemeService.InitializeAsync()`，完成后 `StateHasChanged()`。
7. 运行验证：切换主题后 AppBar、Drawer、卡片、表格、Dialog 均应正确响应深浅色；刷新页面后应记住上次选择。

### 7.3 MudBlazor 9.6 API 注意事项（已对照源码核实）

- `MudTheme` 通过 `PaletteLight` / `PaletteDark` 两个属性分别配置两套色板（`Palette` 为二者抽象基类，不直接配置）。
- `MudThemeProvider` 绑定参数为 `Theme`、`IsDarkMode`、`IsDarkModeChanged`、`ObserveSystemDarkModeChange`（系统偏好自动跟随，默认 `true`）、`DefaultScrollbar`。
- 系统 `prefers-color-scheme` 跟踪为内置能力（`mudThemeProvider.js`），无需手写 JS interop；provider 暴露 C# 公共方法 `GetSystemDarkModeAsync()`（一次性读取）与 `WatchSystemDarkModeAsync()`（订阅变更）。
- **无内置 `ThemeService`/`localStorage` 持久化**——需由本项目新增 `ThemeService` 自行管理状态与 `localStorage` 读写。
- Palette 颜色属性名为 camelCase：`AppbarBackground`、`AppbarText`、`DrawerBackground`、`DrawerText`、`DrawerIcon`（注意是 `Appbar*` 不是 `AppBar*`，`DrawerText` 不是 `DrawerTextColor`）。
- 自定义 Typography 通过 `MudTheme.Typography` 对象覆盖，例如 `theme.Typography.H4.FontSize = "1.5rem"`。
- 自定义圆角通过 `MudTheme.LayoutProperties.DefaultBorderRadius = "6px"`（默认 `"4px"`）。
- Typography 可覆盖项：`Default`、`H1`–`H6`、`Subtitle1`、`Subtitle2`、`Body1`、`Body2`、`Button`、`Caption`、`Overline`。

### 7.4 验证要点

- 浅色/暗色切换无闪烁。
- `localStorage` 键 `mcm-theme-dark-mode` 正确读写。
- 首次访问未保存偏好时，跟随 OS `prefers-color-scheme`。
- 卡片、表格、按钮、输入框、Dialog 在两套模式下均保持足够对比度。

---

## 8. 设计决策摘要

1. **主色**：靛蓝 `#2563EB`（浅色）/ `#60A5FA`（暗色），符合云原生运维工具认知，状态色对比清晰。
2. **暗色默认行为**：首次无保存偏好时读取 OS `prefers-color-scheme`；否则使用保存值。
3. **持久化**：`localStorage` 键 `mcm-theme-dark-mode`，由 `ThemeService` 集中管理读写。
4. **待编辑文件**：`MainLayout.razor`（Provider 绑定 + 切换按钮）、新增 `Services/ThemeService.cs`、`Program.cs`（注册服务）。

## Context

See `proposal.md` — Why. Design-relevant current state:

- 展示格式化散落三层:服务层只在节点资源容量做了换算(`ClusterNodeService.MapResources` → `NodeResourceViewModel.Label/Text/Raw`),映射层只把部分枚举转中文(`NamespaceMappingExtensions` 状态归一、`AuditLogMappingExtensions` 类别/动作),组件层各自持有 `GetNodeStatusClass`(3 处)、`GetRolloutText/Class`(2 处) 并大量直出 raw 字符串。
- Tooltip 现状:共享 `TooltipIconButton`(行内图标) + `AccountTable` 直接使用 `MudTooltip` + 8 处原生 `title`(资源原始值、条件消息、标签/注解长值、审计目标、端口说明);`app.css` 无任何 tooltip 覆盖,MudBlazor 9.9 默认样式是 `--mud-palette-gray-darker` 灰底 + `dark-text`,与设计系统不符。
- MudBlazor 9.9 tooltip DOM:`.mud-tooltip-root`(含 `.mud-tooltip-inline` 内联模式)→ `.mud-tooltip.mud-tooltip-default`(padding 4/8、12px、3px 圆角)→ `.mud-tooltip-arrow::after`(6px 三角,颜色取自 `--mud-palette-gray-darker`);`MudTooltip` 支持 `Text`/`TooltipContent`/`Arrow`/`Delay`/`Duration`/`Placement`/`Inline`/`ShowOnFocus`/`Visible`。
- 测试基线 526 绿,多条断言直接校验英文原文(如 `NodeListTableTests` 断言 `Ready`/`control-plane`,`AccountTableTests` 断言 `Member`),改造必须同步更新。

## Goals / Non-Goals

**Goals:**

- 一个展示映射入口负责 K8s 枚举 → 中文/CSS 类的转换,全站复用且可单测。
- 值展示统一「中文主行 + 英文原值次行」;字段标签统一「中文 (English)」;计量统一人类可读单位;原始值经统一 tooltip 查看。
- Tooltip 视觉与行为全站一致,消灭原生 `title`;保留 raw 数据供排序/过滤/类名/原值提示。
- 改动全部落在展示层,服务输入输出契约、数据库 schema、路由与交互流程不变。

**Non-Goals:**

- 不引入 .NET 本地化框架(resx/IStringLocalizer):本系统为中文优先的静态展示,不做运行时语言切换。
- 不翻译自由文本(条件 Reason/Message、镜像/内核/运行时值)、标识符(YAML/UID/API Server/ClusterIP/PodCIDR)、协议缩写与资源名称。
- 不清理 `nodes-page` 中与本变更无关的历史遗留要求(如旧的卡片组合条目);仅修正本变更触及的展示条款。
- 不改 dark mode、不动设计 token 体系(仅把 spec 的琥珀值与既有代码对齐)。

## Decisions

### D1 — 新增展示映射层 `ViewModels/Mappings/K8sDisplayText.cs`

纯静态函数类,提供 `NodeStatusText/NodeStatusCssClass/RoleText/NodePhaseText/ConditionTypeText/ConditionStatusText/ConditionStatusCssClass/AddressTypeText/TaintEffectText/WorkloadConditionTypeText/SvcTypeText/SvcEndpointStatusText/SvcEndpointCssClass/AccountRoleText/ConnectionTypeText/NamespaceStatusText` 等;未登记值一律回退原文。

- ViewModel 以**计算属性**暴露展示值(如 `StatusText => K8sDisplayText.NodeStatusText(Status)`、`StatusCssClass => ...`),而不是映射时写死字段——先例是 `WorkloadListViewModel.ReadyText`;这样 service 直接构造的 VM(`ClusterNodeService.MapNode`)与映射扩展都能受益,raw 字段保持不变。
- 备选:放在服务层(否——纯枚举映射不需要服务参与,会无谓扩大服务面);放在组件 helper(否——违反分层,无法集中单测,重复 helper 正是现状病因);做成枚举扩展方法放 `Common/Enums`(否——枚举应保持无展示语义,与 `AuditLogMappingExtensions` 先例一致)。

### D2 — 两个共享展示组件

- `Components/Common/StackedText.razor`:`Primary`(中文/主行)+ `Secondary`(英文/次行,可选,等宽 caption 次要色)。
- `Components/Common/StatusBadge.razor`:`Text`(中文)、`CssClass`(online/offline/unknown)、`Raw`(次行 + tooltip);渲染既有 `.status-badge` span + `.status-dot`,契约不破。
- 备选:只定文档约定不加组件(否——现状已有三套重复 class helper,约定挡不住漂移);合并为单组件(否——徽章的圆点/淡彩语义与普通双行文本不同)。

### D3 — Tooltip:全局 CSS 覆盖 + 触发统一

- `app.css` 覆盖 `.mud-tooltip.mud-tooltip-default`(墨底 `#111111`、纸字 `#F4F4F0`、3px、无投影)与 `.mud-tooltip-arrow::after`(同色),通过 `.mud-tooltip-mono`/内容类支持等宽原始值;不改 ThemeManager 调色板(避免 gray 变量影响其他组件)。
- 新增 `Components/Common/TextTooltip.razor`:参数 `Text`/`Mono`/`Inline`/子内容触发器,内部用 MudTooltip,统一 `Delay`/`Placement`/焦点行为;替换全部 8 处原生 `title`。
- `TooltipIconButton` 保留,补统一延迟/箭头/位置,继续同时提供 tooltip 与 `aria-label`。
- 备选:改 `PaletteLight.GrayDarker`/`DarkText` 让 MudBlazor 自动生效(否——语义污染调色板,且文字/字体/内边距仍要 CSS);自研 tooltip 组件(否——违反「交互组件优先 MudBlazor」契约)。

### D4 — 单位换算的边界

- 复杂换算继续留在服务层:`ClusterNodeService.MapResources` 是范式,原始 quantity 已在 `NodeResourceViewModel.CapacityRaw/AllocatableRaw`,只需把 UI 的 `title` 换成 tooltip。
- 简单计数/后缀放在展示层/VM:`NodeCountText`(`3 台`)、`ReadyText`(`2/3 个`);节点数在 `ClusterViewModel`/`ClusterDetailViewModel` 加 `NodeCountText`。
- 备选:把所有单位逻辑塞进服务(否——计数后缀是展示职责,服务入参/输出契约保持纯净)。

### D5 — 逐 feature 落点

| capability | 主要改动 |
|---|---|
| `display-conventions`(新) | 契约本体:双语规则、例外清单、单位与原始值 tooltip |
| `ui-theme` | tooltip 视觉/行为/触发统一;双语展示词汇;琥珀 token 对齐 `#D97706` |
| `node-detail-layout` | `ClusterNodeDetailViewModel`/`NodeAddressViewModel`/`NodeTaintViewModel`/`NodeConditionViewModel` 加 `*Text`;`NodeOverviewCard`/`NodeConditionsCard`/`NodeSystemInfoCard`/`NodeResourcesCard` 改用组件与 tooltip |
| `nodes-page` | `ClusterNodeViewModel` 加状态/角色双语;`NodeListTable`/`NodeListFilterBar` |
| `service-management` | `SvcListViewModel`/`SvcDetailViewModel`/`SvcPortViewModel`/`SvcEndpointViewModel`;`SvcListTable`/`SvcListFilterBar`/`SvcPortTable`/`SvcEndpointTable`/`SvcDetail` |
| `workload-management` | `WorkloadListViewModel`(`ReadyText` 加「个」)、`WorkloadConditionViewModel` 加 `*Text`;`WorkloadStatusCard`/`WorkloadListTable` |
| `accounts-page` / `profile-page` | `AccountViewModel.RoleText`;`AccountTable`/`AccountEditDialog`/`Profile` |
| `cluster-detail` | `ConnectionTypeText`/`NodeCountText`;`ClusterOverviewCard` |
| `namespace-management` | `NamespaceListViewModel`/`NamespaceDetailViewModel` 暴露原始 phase 供次行;`NamespaceListTable`/`NamespaceDetailToolbar` |

### D6 — 顺手修复与收编

- `AuditLogMappingExtensions.ToDisplayName` 补 `Service`/`Namespace`(现有 spec 已要求中文类别,属实现缺陷,不加 delta)。
- 删除组件内重复 helper,统一下沉 D1;`TooltipIconButton` 的内部 `MudTooltip` 成为图标操作的唯一入口。

## Risks / Trade-offs

- [双行值抬高表格行高] → 次行使用 caption 级等宽小字号、单行省略;仅在值类单元格使用,表头与操作列不变。
- [CSS 覆盖依赖 MudBlazor 内部类名] → 锁定 `.mud-tooltip`/`.mud-tooltip-arrow` 两类并在升级 MudBlazor 时回归;tooltip 契约测试断言渲染类名而非像素。
- [既有测试断言英文原文] → 本 change 内同步更新(已识别:`NodeListTableTests`、`AccountTableTests`、`SvcMappingTests`、`WorkloadMappingTests`、`ClusterNodeServiceTests` 中相关断言),并新增映射层单测与 tooltip 契约测试。
- [`nodes-page` 存在与 `node-detail-layout` 冲突的历史要求] → 本变更只修订其展示条款;其余遗留漂移记录为后续独立清理,避免把无关重写塞进本次。
- [`TooltipContent` 与 `Text` 在 9.9 的具体渲染差异] → 实现时以 `TooltipIconButton` 现有 `Text` 路径为主,需要富内容/等宽时用子内容;若 `TooltipContent` 行为不符,回退为 CSS 类方案,不影响规格。

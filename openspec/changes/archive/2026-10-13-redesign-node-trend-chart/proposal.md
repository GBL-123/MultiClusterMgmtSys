# Proposal

## Why

看板「节点就绪趋势」卡当前的视觉编码在最常见的平稳工况下几乎不传递信息,且存在多处渲染缺陷:

- **末端标签碰撞**:就绪序列的 endLabel(如 `3`)与 TOTAL 参考线的末端标签(`TOTAL 3`)在右缘同一 y 位置重叠,渲染成一团无法辨认的文字;未就绪序列取 0 时其 endLabel 与 X 轴角碰撞。
- **幽灵虚线**:TOTAL 琥珀虚线与就绪绿线数值相同时被绿线整段遮盖,只在窗口起点到首个快照点之间单独露出,形成图例中不存在的"第三条线",无法解释。
- **零值歧义**:未就绪=0 的平线与 X 轴完全重合,看起来像坐标轴而非数据;"窗口起点前无快照"(无数据)与"未就绪为 0"(有数据且健康)在视觉上无法区分。
- **信息密度低**:就绪/未就绪双线在总数恒定时互为镜像,是冗余编码;Y 轴仅 0/2/4 三档,数据悬空于网格线之间,底部整带留白;24 小时全平稳时两条平线占满整卡,异常(未就绪出现、节点数变化)反而不跳脱。

需要对这张图做一次纯呈现层的重新设计,让"一眼健康"与"一眼异常"都成立,同时保持既有数据口径零改动。

## What Changes

- 趋势图由**双阶梯折线 + TOTAL 参考线**改为**堆叠阶梯面积图**:就绪(绿)在下、未就绪(红)在上,堆叠顶边即舰队节点总数——总数升降、未就绪侵蚀、全健康平带三种形态各自一眼可辨。
- **移除** TOTAL markLine 与全部末端标签(endLabel),右缘留白收回;当前值继续由卡头徽章表达并补充"共 N 台"。
- 窗口起点到首个快照点之间的**无数据区**用斜纹(decal)markArea 显式表达「无快照数据」,与"未就绪=0"严格区分。
- Y 轴改为整数刻度(`minInterval: 1`)并收紧上界,消除数据悬空与底部空带。
- Tooltip 增强为 时刻 + 就绪/未就绪/合计 + 就绪率。
- 红色面积透明度高于绿色,保证大舰队下小比例未就绪仍可辨认。
- **数据口径零改动**:C# 侧 `DashboardService.GetNodeReadinessTrendAsync`、`DashboardTrendPointViewModel`、组件 payload 形状全部不变;24 小时窗口、阶梯推进语义、空态行为不变。改动集中在 `dashboard-trend.js` 渲染、卡头标记与 CSS。

## Capabilities

### New Capabilities

(无)

### Modified Capabilities

- `cluster-dashboard`:修改「节点就绪趋势」Requirement 的呈现条款——明确堆叠阶梯面积编码、无数据区表达、禁用末端标签与 TOTAL 参考线、整数 Y 轴;数据口径、零 K8s 调用、空态、窗口与序列标注等既有条款保持不变。

## Impact

- **代码**:`MultiClusterMgmtSys.Web/Assets/Scripts/dashboard-trend.js`(渲染重写,payload 契约不变)、`MultiClusterMgmtSys.Web/Components/Dashboard/Shared/DashboardNodeTrendCard.razor`(卡头徽章文案)、`MultiClusterMgmtSys.Web/wwwroot/css/app.css`(第 15 节趋势卡样式微调)。
- **契约**:`openspec/specs/cluster-dashboard/spec.md` 的「节点就绪趋势」Requirement(归档时同步)。
- **测试**:`DashboardTrendCardTests`(挂载断言措辞与系列形状断言随面积化调整;payload 字段断言不变)、`DashboardPageTests`(基本不动)。不触及 Application/Domain/Infrastructure。
- **依赖**:ECharts 按需导入需增加面积所需组件(仍在既有 `echarts/core` 单文件自托管口径内,不新增依赖)。
- **无破坏性变更**:数据口径、窗口、payload 形状、空态行为全部向后兼容。

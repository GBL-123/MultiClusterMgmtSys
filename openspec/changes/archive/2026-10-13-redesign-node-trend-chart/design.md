# Design

## Context

现状(动机见 proposal.md - Why;行为契约见 specs/cluster-dashboard 本 change 的 delta):

- 数据链路:`DashboardService.GetNodeReadinessTrendAsync()` 读本地 `ClusterHealthSnapshot` 窗口(24h)→ `BuildTrendPoints` 逐快照事件推进舰队合计(同秒合并),产出 `List<DashboardTrendPointViewModel>(CapturedAtUtc, ReadyNodes, NotReadyNodes)`。**该链路本设计零改动。**
- 组件:`Web/Components/Dashboard/Shared/DashboardNodeTrendCard.razor` 把序列拆成两条 series(就绪 `#346538`、未就绪 `#9F2F2D`)连同 `windowStart/windowEnd/totalNodes` 经 JS interop 传给 `/js/dashboard-trend.js` 的 `mount/update`。**payload 形状本设计零改动。**
- 渲染:`Web/Assets/Scripts/dashboard-trend.js`(vite 打包为单文件自托管 ESM,`echarts/core` 按需引入 LineChart/Grid/Tooltip/MarkLine + CanvasRenderer),当前为双阶梯折线 + 末端标签 + TOTAL 琥珀 markLine。
- 样式:`app.css` 第 15 节(`.dashboard-trends`/`.dashboard-trend-meta`/`.dashboard-trend-legend`/`.dashboard-trend-swatch`/`.dashboard-trend-now`/`.dashboard-trend-note`/`.dashboard-trend-chart`)。
- 测试:`DashboardTrendCardTests`(bUnit,`ctx.JSInterop.SetupModule(ModulePath)` 断言 mount/update/dispose 与 payload 字段)、`DashboardPageTests`(卡片落位)。
- 约束:设计系统 Swiss Industrial Print(ink `#111111`、hairline `#E2DED5`、muted `#6E675C`、amber `#D97706` 仅 brand/focus/progress;数据等宽 IBM Plex Mono);契约要求保留 24h 窗口标注、就绪/未就绪图例、阶梯口径、空态、零 K8s 调用。

## Goals / Non-Goals

**Goals:**

- 三种典型形态一眼可辨:全健康(整带绿色平带)、有未就绪(红色侵蚀顶边)、节点数变化(顶边整体升降)。
- 消除现有渲染缺陷:末端标签碰撞、幽灵 TOTAL 虚线、零值线与 X 轴不分、无数据区与零值不分。
- 「无快照数据」区间显式表达,且与「未就绪=0」严格可区分。
- 纯呈现层改动:不动 C#、不动 payload、不动数据口径,测试面最小化。

**Non-Goals:**

- 不改 24 小时窗口与阶梯推进语义,不加窗口切换(6h/7d)交互。
- 不加按集群钻取、不加事件标记(markPoint)、不做就绪率归一化视图。
- 不改卡片在看板中的位置、外框与高度,不改空态/失败态行为。
- 不引入新的 npm 依赖;不新增第二张图(如 per-cluster 小 multiples)。

## Decisions

### D1:堆叠阶梯面积图替代双折线

就绪面积在下(绿)、未就绪面积堆叠其上(红),`stack` 同一组、`step: "end"` 保持阶梯口径。顶边 = 就绪+未就绪 = 舰队节点总数。

- 为什么堆叠而非修碰撞后的双线:双线在总数恒定时互为镜像,是冗余编码;未就绪=0 的线必然与 X 轴重合,歧义无法根除。堆叠后"未就绪=0"= 没有红色,语义即图形;总数变化从"两条线间距变化"升级为"顶边升降",可读性更高。
- 为什么就绪在下:绿色平带贴底读作"容量基座",红色在顶边读作"侵蚀",符合直觉且红色永远贴着最显眼的顶边。
- 替代方案否弃:①100% 归一化堆叠——丢失绝对规模,3 台掉 1 台与 300 台掉 100 台无法区分;②事件流/状态块重设计——偏离契约的阶梯序列口径,交互复杂度高;③纯修碰撞的最小修补——信息密度低的问题仍在。

### D2:移除 TOTAL markLine 与全部 endLabel

堆叠顶边本身就是总数,TOTAL 参考线冗余;当前值由卡头徽章表达,末端标签冗余且是碰撞源。移除后 `grid.right` 由 56 收回至 ~12,绘图区变宽。移除琥珀元素后趋势卡不再使用 amber,更贴合 token「amber 仅 brand/focus/progress」的语义。

### D3:卡头徽章补充总数

`.dashboard-trend-now` 文案由 `3 就绪 / 0 未就绪` 改为 `就绪 3 · 未就绪 0 · 共 3`(等宽,分隔符 `·`)。图例与「近 24 小时」标注保留不变。卡头布局不变。

### D4:无数据区 = 斜纹 markArea,且仅出现在首点之前

契约的阶梯语义决定:窗口内某集群快照断流时沿用其最近值(不是无数据),因此真正的"无数据"只有**窗口起点 → 首个快照点**这一段。该区间渲染 `markArea`,样式为墨色 6% 斜纹 decal + 居中 muted 等宽小字「无快照数据」;区间内不渲染任何面积。(落地注:markArea 是标记组件、不经过序列 decal 管线,`itemStyle.decal` 走查实测不渲染;实现改用 canvas 图案平铺的 45° 斜纹、canvas 不可用时降级 ink 4% 纯色。)

- 宽度小于阈值(如 < 2 分钟)时不渲染,避免首点贴边时闪出一截细条。
- 工业印刷语境下斜纹是"留白/不适用"的标准语言,与实心面积形成材质对比,不与任何序列色混淆。
- 替代方案否弃:①把首点值向前延伸到窗口起点——伪造数据,违背"未参与曲线"口径;②浅灰纯色块——与空态卡片底色过近,且不像"缺失"而像"背景"。

### D5:右缘延伸到"现在"

`step: "end"` 下序列视觉止于最后一个数据点,带子在右缘前悬空。渲染层在 `windowEnd` 追加一个与末点同值的**纯渲染点**,使面积带延伸到"现在"。这只是阶梯语义的可视化补全(值保持到下一事件),不改动 payload 与数据口径。

### D6:Y 轴整数化并收紧上界

`minInterval: 1` 保证节点数只出现整数刻度;`max` 取 `Math.max(4, peak + 1)` 量级(nice ceil),取代现行 `peak * 1.25` 公式(3 节点时旧公式给出上界 4、刻度仅 0/2/4,数据悬空)。`splitNumber` ≤ 4,小数值时刻度自然稀疏。

### D7:色彩与透明度

- 就绪:线 `#346538` 1.5px,面积同色 alpha ≈ 0.32——健康带必须一眼读绿(0.14 实测过淡,读作灰底),绿线同时标出带的内部顶界。
- 未就绪:**纯填充不描线**,面积 `#9F2F2D` alpha ≈ 0.45——红色权重显著高于绿色,保证大舰队下小比例未就绪仍可辨认。堆叠上层系列若画线,其线位恒等于堆叠顶边,未就绪=0 时会整段把绿带顶边描红(走查实测),故红线禁用;顶边由独立渲染的「合计」墨色描线系列(y=就绪+未就绪)表达。
- 无数据区:decal 斜纹 ink alpha ≈ 0.12,文字 muted 11px mono。
- 网格/轴/刻度样式全部沿用现状(ink 轴线、hairline 虚线 splitLine、mono 11px muted 标签、4h 主刻度)。


### D8:Tooltip 增强

axis trigger 保留,formatter 输出五行等宽内容:`时刻(MM-dd HH:mm:ss)`、`就绪 N`、`未就绪 N`、`合计 N`、`就绪率 NN.N%`(总数为 0 时显示 `—`)。无数据区悬停显示「无快照数据」。卡片头徽章不变,不承担 tooltip 职责。

### D9:ECharts 依赖面

`areaStyle`/`stack`/`decal` 均为 LineChart 与内置渲染能力;markArea 需将 `MarkLineComponent` 替换/增补为 `MarkAreaComponent`(同属 `echarts/components`,单文件自托管口径不变,bundle 体积微增)。 vite 构建链路与 `SkipVite` 口径不变。

### D10:测试口径

- payload 不变 → `DashboardTrendCardTests` 的 windowStart/windowEnd/totalNodes/series 字段断言全部保留;仅"step 折线"措辞类断言随面积化更新,徽章断言补"共 N"。
- JS 内部渲染无单测基建,面积/斜纹/无数据区由 bUnit payload 断言 + 端到端手工走查覆盖(与其他前端图表一致)。
- `DashboardPageTests` 的落位断言(`.dashboard-metrics + .dashboard-trends + .dashboard-lists`)不动。

## Risks / Trade-offs

- [大舰队下未就绪窄带仍可能只有 1~2px 高] → 红色 alpha 高于绿色,窄带贴着墨色顶边可辨;tooltip 提供精确数值兜底;达标判据是"能看出顶边有红色侵蚀"而非读出精确台数。
- [decal 斜纹在 CanvasRenderer 下的表现] → decal 是 ECharts 5.3+ 内置能力,ECharts 6 Canvas 支持;走查时验证,若效果不佳降级为 ink 4% 纯色 + 左侧虚线边界(仍满足"与零值可区分"的契约)。
- [markArea 与面积系列的 z 序] → markArea 默认在系列之下,斜纹不会遮盖面积;首点之前的区间本无面积,无冲突。
- [右缘渲染延点被误读为新数据] → 延点不进入 tooltip 数据(用独立系列或 tooltip formatter 按时间戳过滤);视觉上与阶梯语义一致(值保持),可接受。
- [用户习惯了 TOTAL 参考线] → 卡头徽章的"共 N"承担同一信息,且无时不在;权衡后接受。

## Migration Plan

纯前端呈现改动,无 schema/数据/配置迁移:替换 `dashboard-trend.js` 渲染实现 → `npm run build` 重建 bundle(宿主 `dotnet build` 自动触发)→ 卡头徽章文案与 CSS 微调 → bUnit 断言更新。回滚 = revert 三处文件并重建 bundle。Docker 镜像构建走既有 `charts` 阶段,无需调整。

## Open Questions

- 窗口切换(24h / 7d)是否值得做:依赖快照保留策略(默认 90 天,可行),但会引入窗口参数贯穿服务与 payload,留待后续独立 change 评估。
- 未就绪时段是否叠加事件标记(起止刻度):当前红色面积 + tooltip 已够用,观察实际使用后再定。

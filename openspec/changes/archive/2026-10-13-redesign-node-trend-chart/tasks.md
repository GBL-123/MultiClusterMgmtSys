# Tasks

## 1. 渲染层重写(dashboard-trend.js)

- [x] 1.1 将两条 step 折线改为同组 `stack` 的阶梯面积系列(就绪 `#346538` 在下 alpha≈0.32、未就绪 `#9F2F2D` 在上 alpha≈0.45,线宽 1.5px、无 symbol、`step: "end"`),移除 `MarkLineComponent` 引入 `MarkAreaComponent`,并验证 `npm run build` 重建 `wwwroot/js/dashboard-trend.js` 成功
- [x] 1.2 移除全部 endLabel 与 TOTAL markLine,`grid.right` 由 56 收回至 ~12,并验证走查页面右缘不再出现重叠文字、绘图区变宽
- [x] 1.3 追加 `windowEnd` 纯渲染延点(与末点同值、tooltip 按时间戳过滤不计入),使面积带延伸到右缘"现在",并验证走查页面带子贯通到右缘
- [x] 1.4 实现无数据区 markArea:窗口起点早于首个快照点且区间宽 ≥ 2 分钟时,渲染墨色斜纹底(canvas 图案平铺;markArea 是标记组件、不走 decal 管线,`itemStyle.decal` 实测不渲染)+ 居中 muted 11px mono「无快照数据」,区间内无任何面积;canvas 不可用时降级 ink 4% 纯色,并验证首点贴边(< 2 分钟)时不渲染
- [x] 1.5 Y 轴改 `minInterval: 1` 且上界取贴近峰值的 nice 整数(取代 `peak * 1.25` 公式),`splitNumber` ≤ 4,并验证 3 节点走查页面刻度只出现整数、数据带不悬空
- [x] 1.6 Tooltip formatter 输出 `时刻(MM-dd HH:mm:ss)`、`就绪 N`、`未就绪 N`、`合计 N`、`就绪率 NN.N%`(总数 0 显示 `—`),无数据区悬停显示「无快照数据」,并验证走查悬停五行内容正确

## 2. 卡头与样式

- [x] 2.1 `DashboardNodeTrendCard.razor` 徽章文案改为 `就绪 3 · 未就绪 0 · 共 3`(保留 `.dashboard-trend-now` 类与等宽字体),并验证 `DashboardTrendCardTests` 徽章断言更新后通过
- [x] 2.2 `app.css` 第 15 节趋势卡样式按新呈现微调(如图例 swatch 与面积观感对齐),并验证页面无样式回归

## 3. 测试与契约

- [x] 3.1 更新 `DashboardTrendCardTests`:系列形状/措辞断言随面积化调整,徽章断言补「共 N」,payload 字段断言(windowStart/windowEnd/totalNodes/series)保持不变,并验证 `dotnet test MultiClusterMgmtSys.Tests --filter-class "*DashboardTrendCardTests*"` 全绿
- [x] 3.2 确认 `DashboardPageTests` 卡片落位断言无需变更,并验证 `dotnet test MultiClusterMgmtSys.Tests --filter-class "*DashboardPageTests*"` 全绿
- [x] 3.3 按本 change delta 同步 `openspec/specs/cluster-dashboard/spec.md` 的「节点就绪趋势」Requirement(归档时执行),并验证 `openspec validate redesign-node-trend-chart --strict` 通过

## 4. 集成验证

- [x] 4.1 `dotnet build MultiClusterMgmtSys.slnx` 0 错误 + `dotnet test MultiClusterMgmtSys.Tests` 全绿 + `./coverage.ps1` 门禁通过,并验证三项命令依次成功
- [x] 4.2 端到端手工走查:平稳全绿(整带平带)、制造未就绪(红色侵蚀顶边)、首点前无数据区(斜纹标识)、悬停 tooltip、缩放窗口,并验证五种形态与 design.md 视觉规格一致(2026-10-13 修复走查:A 斜纹已渲染、B 延点 tooltip 折算回最近快照+无高亮圈、C tooltip 五行)

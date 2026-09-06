# design — 详情页 Tab 化重构

## Context

四个详情页模板(ClusterDetail、NodeDetail、WorkloadDetailView、ConfigMapDetail)均为「`*DetailToolbar` + 纵向 `MudStack` 卡片堆」,全仓库未用过 `MudTabs`。数据层无变化:所有页面数据在 `OnInitializedAsync` 一次性拉取,`ConfigMapDetailViewModel.Data`(`Dictionary<string,string>`)已存在。设计系统为 Swiss Industrial Print(发丝线 + 琥珀强调 + 无阴影,见 `openspec/specs/ui-theme/spec.md`),MudBlazor 9.9,`MUD0002` 分析器把组件 API 误用当编译错误。动机与范围见 proposal.md。

## Goals / Non-Goals

**Goals:**

- 四个详情页模板改为统一的「工具栏 + `MudTabs`」骨架,各页 tab 组成按 specs delta 落实。
- `MudTabs` 视觉贴合设计系统,一份 CSS 全站复用。
- ConfigMap 新增键值只读视图:长 value 截断 + 点击查看对话框。
- 保持既有卡片组件与其内容契约不动(只是挂载位置移入 tab 面板)。

**Non-Goals:**

- URL 化 tab 路由、query-string 持久化、tab 懒加载数据、记住上次 tab。
- 键值 tab 的编辑能力(编辑仍走 YAML 编辑器路由)。
- `binaryData` 在键值 tab 的展示(`Data` 字典不含;YAML tab 可见,够用)。
- 任何服务层 / ViewModel / 仓储改动。

## Decisions

1. **Tab 状态用页面局部 `ActivePanelIndex` 绑定(方案 A;MudBlazor 9.9 的参数名,v8 时代的 `ActivePanelIndex` 已更名)**,不进 URL。
   备选:routable tab(`/nodes/{id}/{tab}`)或 `?tab=` 查询参数。放弃理由:当前无「把某个 tab 页分享给别人」的真实场景;方案 A 改动面小、五页(四模板)改法统一,refresh 回第一个 tab 可接受。若日后出现深链需求,再立 change 引入路由化,届时仅需给每页加一段路由参数映射,现有 tab 结构不受影响。

2. **不建通用 `DetailTabs` 包装组件,直接在四个页面原地重组标记。**
   备选:抽 `<DetailTabs>` 组件统一包 `MudTabs` + 样式类。放弃理由:四处用法高度相似但面板内容各异,抽象只省一处 CSS 类名字符串;横切约定(tab 内外归属、计数、样式类)已由 `detail-page-tabs` spec 与共享 CSS 类约束,抽象层反而让 bUnit 接线测试多一层穿透。四个页面各自持有 `<MudTabs Class="detail-tabs">`。

3. **共享样式类 `.detail-tabs` 放 app.css,按 ui-theme token 取值。**
   要点:tab 栏背景卡面色、下沿 1px `#E2DED5` 发丝线;激活指示条为琥珀细线(替换 MudBlazor 默认下划线颜色);激活文字墨色、未激活次文字色;去掉面板圆角与投影;tab 高度密集。备选:MudTheme 全局覆盖 `MudTabs` —— 放弃理由:项目里 `MudTabs` 仅详情页使用,类级作用域足够,且避免主题对象膨胀;若将来更多页面用 tab,再考虑提升到主题层。
   补充(实现时验证):app.css 先于 MudBlazor.min.css 加载,选择器按「`.detail-tabs` + 内部类」加权到高于 MudBlazor 对应规则的特异性;MudBlazor 9.9 激活 tab 类为 `.mud-tab.mud-tab-active`(非 `.active`),激活面板包装层为 `display:contents`(内容直接成为 panels 容器的 flex 项),因此 flex 链由 `TabPanelsClass` 的 flex 容器 + 卡片自身 flex 属性完成,无需对 `PanelClass` 包装层加规则。

4. **tab 标签计数优先用 `MudTabPanel` 的 header 自定义渲染;若 MudBlazor 9.9 该 API 不合用,回退为纯文本标题(如「集群端点 3」的 `Text` 拼接)。**
   计数数据全部来自已加载的 ViewModel(`Endpoints.Count`、`NodeCount`/`Nodes.Count`、`Data.Count`),无需新查询。具体 API 以 9.9 实测为准,`MUD0002` 会在编译期拦截无效参数。

5. **`KeepTabsAlive` 维持 MudBlazor 默认(不保活,切换重渲染)。**
   数据一次拉全,面板重渲染是纯标记重建,开销可忽略;不保活还能避免各卡片内部临时状态(如密文展开态)在切走后残留的隐蔽问题。

6. **ConfigMap 键值视图做成 `Configmaps/Shared/ConfigMapDataViewCard.razor`,与 `ConfigMapYamlViewCard` 对称。**
   表格用 `MudTable`(`Elevation="0" Dense`,键列 `.font-mono`);value 截断用 CSS 单行省略(`white-space:nowrap; overflow:hidden; text-overflow:ellipsis`)而非 MudBlazor 行截断,保证不撑高行;查看对话框用 `MudDialog` + 等宽 `<pre>` 风格容器 + 复制按钮(复用端点表已有的剪贴板 + Snackbar 模式)。备选:内联在页面标记里 —— 放弃理由:与既有 Shared 卡片组织方式不一致,且点击展开状态需要一个有状态的家。

7. **实施顺序:CSS 先行,四页逐个落地,每页 build + test 后再下一页。**
   先在 ClusterDetail(三 tab,含计数)上把 `.detail-tabs` 样式打磨成型,再铺 Node(五 tab)、Workload(双 tab,全高 yaml 卡片风险点)、ConfigMap(双 tab + 新键值卡片)。

## Risks / Trade-offs

- **[yaml 全高布局在 tab 内失效]** 全高 `yaml-textarea` 依赖从页面 `MudStack` 到卡片的 `flex: 1 1 auto; min-height: 0` 链,`MudTabs` 面板包装层会打断该链 → 在 `.detail-tabs` 的面板容器上补 flex 传递规则;若 MudBlazor 内部包装层无法穿透,接受面板内 YAML 卡片按内容自适应高度(视觉降级,不违约——spec 未约定 textarea 必须满高)。
- **[MudBlazor 9.9 tab API 与预期不符(计数模板、KeepTabsAlive 语义)]** → 实现前先在 ClusterDetail 上小步验证;MUD0002 兜底拦截;计数模板不可用时有纯文本回退(见决策 4)。
- **[刷新回第一个 tab]** 方案 A 的已知取舍,spec 已明确此行为;不视为缺陷。
- **[键值 tab 只显示 `Data` 不含 `binaryData`]** 与既有 ViewModel 契约一致,binaryData 键在 YAML tab 可见;如需覆盖属后续 change。
- **[tab 化可能破坏既有测试]** 详情页主体无既有 bUnit 测试,工具栏测试不受影响;新增测试遵循 unit-testing spec 的接线契约风格(断言 `MudTabs` 默认选中与面板数,不碰 `.mud-*` 内部 DOM)。

## Migration Plan

纯前端展示层重构,无数据/接口变更:单次部署即生效,回滚 = revert 对应提交。实施顺序按决策 7;每页完成后 `dotnet build` 0 错误 + `dotnet test` 全绿再继续。

## Open Questions

无——两个遗留小口子(MudTabs 样式、长值交互)已在探索阶段与用户敲定。

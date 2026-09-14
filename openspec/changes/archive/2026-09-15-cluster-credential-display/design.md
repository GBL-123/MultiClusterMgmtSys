## Context

现状见 `proposal.md - Why`。约束与既有资产(实现前需遵守):

- 现有实现 `Components/Clusters/Shared/ClusterOverviewCard.razor` 用两个布尔(`showSecret` / `showSecretContent`)+ `MudTextField ReadOnly` 承载密文,header 按钮控制第一次展开,眼睛装饰控制第二层遮蔽;数据经 `ClusterService.GetClusterForEditAsync` 懒加载一次并缓存在 `editForSecret`。
- 设计词汇已存在,无需发明:虚线等宽空态框(`.empty-state`)、等宽只读滚动块(`.cm-value-viewer`)、复制模式(`ClusterEndpointsCard.CopyToClipboard`:`navigator.clipboard.writeText` + 「已复制到剪贴板」snackbar)、类型双语标签(`display-conventions`)、`MudButton Variant="Text" Size="Small"` 卡片操作按钮范式。
- `ui-theme` 约束:发丝线分层、无投影、琥珀色仅限品牌/焦点/进度/空态;组件优先,展示性 span/框属设计词汇例外。
- `unit-testing` 约束:bUnit 只断言自有 CSS 类与接线契约,禁止断言 `.mud-*` 内部 DOM;服务层已由 `ClusterServiceTests` 覆盖。
- 服务层/数据层完全不动。

## Goals / Non-Goals

**Goals:**

- 密文区从「表单控件 + 双层遮蔽」收敛为单一开关的分类条:隐藏态可识别(类型 + 占位)、展开态可读(等宽、可滚动)、可复制。
- 显隐状态机无不可达状态;加载中/失败有明确反馈且不泄露内容。
- 保持 Admin-only 门控与「首次展开懒加载一次并缓存」契约不变。
- bUnit 契约测试与新交互对齐。

**Non-Goals:**

- 不改 `GetClusterForEditAsync` 契约或新增服务方法;不做下载 `.kube/config`、语法高亮、自动隐藏计时器。
- 不提供「隐藏态直接复制」(复制仅在展开态,保持单一心智模型)。
- 不抽共享组件(当前仅一处消费;未来第二处出现再提取)。

## Decisions

### D1:内嵌分类条,而非对话框 / 最小改动

概览卡网格下方追加全宽「连接凭据」区块,与元数据同屏。备选:对话框查看器(信息与上下文割裂、读 kubeconfig 多一层模态)、仅替换控件的最小改动(双层开关与悬挂式布局仍在)。见 `proposal.md` 方案 A。

### D2:状态机替代双布尔

组件内用私有枚举 `CredentialState { Redacted, Loading, Revealed }` + 现有 `editForSecret` 缓存,替代 `showSecret` / `showSecretContent` 与「眼睛」装饰。理由:双布尔存在「已展开但内容遮蔽」「未展开但内容已遮蔽」等无意义组合;新模型把「展开即内容完整可见」作为唯一含义。加载失败回到 `Redacted` 并经 `ExceptionPresenter` 提示(保持现有行为)。

### D3:展开态不做密码遮蔽

展开是管理员的显式动作,再次遮蔽只会复现圆点墙并需要第二个开关。肩窥风险由「默认隐藏 + 隐藏按钮 + Admin-only」承担。备选:展开后二次遮蔽(否决,双开关换汤不换药)、限时自动隐藏(否决,过度设计且需要额外计时状态)。

### D4:类型标签先行,无类型不渲染分类条

条头类型用 `Cluster.ConnectionTypeText` / `ConnectionTypeRawText`(「配置文件 (Kubeconfig)」/「访问令牌 (Token)」),不依赖接口返回;`Cluster.ConnectionType` 为 null(无凭据记录)时整条不渲染。

### D5:样式新增两个自有类,复用既有 token

`app.css` 追加:

- `.credential-annex`:分类条容器,顶部发丝线(`#E2DED5`)+ 上间距,与网格区隔但同属卡片。
- `.credential-redacted`:隐藏态占位,虚线框(`#C9C2B5`)、等宽、次文字色、左对齐(不套用 `.empty-state` 的居中 flex,避免误用空态语义)。
- `.credential-viewer`:展开态只读块,等宽 12.5px、`overflow: auto`、`white-space: pre-wrap` + `word-break: break-all`(kubeconfig 的 base64 长行与 Token 单行都不撑破卡片)。
- `.credential-fill`:展开态概览卡接入全高 flex 链(与 `.yaml-card` 同思路:卡片/`mud-card-content`/分类条逐级 `flex: 1 1 auto; min-height: 0`),`.credential-viewer` 以 `flex: 1 1 auto; min-height: 320px` 撑满剩余页面高度;收起时移除该类,卡片恢复内容高度。`ClusterDetail.razor` 根 `MudStack` 改为 YAML 详情页同款 `flex-auto d-flex` + `align-self: stretch; min-height: 0`(去掉 `pb-4`,底部间距与 YAML tab 一致,只保留主容器 `pa-4`)。

不引入新颜色;琥珀色不出现。

### D6:操作按钮与反馈

条头右侧 `MudButton Variant="Text" Size="Small"`:隐藏态「查看凭据」;`Loading` 禁用并显示 `// 正在加载...`;展开态「复制」「隐藏」。复制失败 `JSException` → `ExHandler.HandleAsync(ex, "复制")`(与端点卡一致)。

### D7:测试策略

`ClusterCardsTests` 改造:`Admin_can_reveal_secret_via_service` 按「查看凭据」驱动并断言 `.credential-viewer` 出现且含 token 原文;`Member_has_no_secret_button` 断言无「查看凭据」且无 `.credential-annex`;新增隐藏态不渲染原始值(断言 markup 不含 token 原文)。加载失败/复制 JS 调用的覆盖视需要补,不引入 MudBlazor 内部 DOM 断言。

### D8:Blazor Server 安全边界

隐藏态 DOM 无原始值;展开后值经 SignalR 下发并在组件内存缓存(与现状一致,Admin-only、不落盘)。「隐藏即清缓存」被否决——会破坏 `cluster-detail` 的懒加载一次契约,且不改变服务器内存中已存在过该值的事实。

## Risks / Trade-offs

- [隐藏后缓存仍在组件内存] → 与现状相同且 Admin-only;若后续要求更强隔离,再评估「隐藏时清缓存 + 每次重取」。
- [切 tab 后展开态可能保留] → MudTabs 不保证销毁非激活面板;接受,不做自动收起(可后续加)。
- [超长 kubeconfig 撑高卡片] → `max-height` + 滚动 + `break-all` 兜底。
- [复制在 HTTP(非 HTTPS)环境可能被浏览器拒绝] → 既有端点卡同样行为,走统一异常提示,不单独处理。

## Migration Plan

纯前端展示变更,无数据库/schema 迁移,无接口兼容问题。部署即替换应用;回滚按提交回退即可。规格同步在归档时经 delta 合入 `openspec/specs/cluster-detail/spec.md`。

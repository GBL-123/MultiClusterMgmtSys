# pod-logs Design

## Context

`pod-management` 已落地归档:Pod 详情页已是 `PodDetailToolbar + MudTabs`(detail-tabs)结构,含「概览」「容器状态」两个 tab;`PodDetailViewModel.Containers` 已提供容器名(常规容器 + init 容器,来自容器状态)。K8s 日志 API 签名已探针确认:`ReadNamespacedPodLogWithHttpMessagesAsync(name, namespace, container, follow, insecureSkipTLSVerifyBackend, limitBytes, pretty, previous, sinceSeconds, stream, tailLines, timestamps, headers, ct)`。基线 716 测试全绿。

## Goals / Non-Goals

**Goals:**

- 在 Pod 详情页按容器查看日志:容器选择、行数上限(200/500/1000/5000,默认 500)、重启前(previous)开关;
- 日志按需加载(首次切到日志 tab 才拉取),手动刷新 + 数据截至;
- 等宽滚动查看器与空态,错误按既有链路提示且不破坏其它详情内容。

**Non-Goals:**

- 流式/自动跟随(follow)、日志下载、时间戳开关、多容器合并输出、日志搜索高亮;
- 修改 detail-page-tabs 契约(它按页命名,PodDetail 是新增页)。

## Decisions

### D1: 日志懒加载(首次 tab 激活)

进入详情页只拉 Pod 详情;`ActivePanelIndex` 首次变到日志 tab 且尚未加载时拉取。理由:日志是长文本载荷(数千行),随页进入即拉会让"只看概览"的用户白付一次请求;detail-page-tabs 契约"进入页面一次性拉取"的适用范围是既有四页的正文数据,不约束新增页的按需日志。tab 选择仍为纯局部状态,不写路由——与本契约一致。

- **替代方案**:随详情一起拉(简单但浪费)、路由驱动 tab(违背 detail-page-tabs 的局部状态要求)——否决。

### D2: 参数形态与默认容器

`PodLogRequest(ClusterId, Namespace, Name, Container, TailLines, Previous)`;服务透传 `container/tailLines/previous`,其余参数留 K8s 默认。UI 容器下拉默认取详情容器列表第一项——多容器 Pod 不带 container 会 400,显式默认规避;极端情形(容器列表为空)传空走 K8s 默认语义。

### D3: previous 与错误呈现

「上次运行(重启前)」开关仅透传参数;容器从未重启时 K8s 返回 400,按既有翻译(携 API 消息)呈现。日志区域错误**显示在查看器区域内**(错误文案 + 重试),不用全局 snackbar,不清空页级其它内容——满足"重试不破坏其它详情内容"。

### D4: 展示语言

`.log-viewer` = `pre` + mono + 固定最大高度滚动 + hairline/paper(参照 `.event-message-viewer` 与 yaml 卡系);空内容显示 `.empty-state「[ 暂无日志 ]」`;工具条 = 容器下拉 + 行数下拉 + previous 开关 + 「数据截至 HH:mm:ss」+ 刷新按钮(事件页口径)。所有控件走 MudBlazor,不引入原生 select/checkbox。

### D5: 输出契约

`PodLogViewModel { Content, LineCount }`(行数取非空行,供「共 N 行」标注);服务只读、无审计。裸 string 不直出,维持"展示数据一律 ViewModel"契约。

## Risks / Trade-offs

- **[R1] 超大日志** → 服务端 `tailLines` 档位上限 5000 截断 + 统一 10s 超时,页面不回传超量内容。
- **[R2] 容器列表来源是状态而非 spec** → 无状态的未启动容器本就无日志可看;多容器运行/崩溃场景(status 必有)覆盖主路径,记录为已知边界。
- **[R3] 懒加载状态机** → `loaded` 标志 + 参数变更重拉的最小实现,bunit 固化"进入详情不发日志请求"与"改参数重拉"两个场景。
- **[R4] K8s 400 的英文消息直出** → 属既有翻译语义(K8s 原始消息,携带明确原因如容器未启动);不另造中文映射,避免臆造。

## Migration Plan

纯新增能力:服务 → mock → 详情页 tab → 样式 → 测试;每步独立可验证;回滚即 revert。

## Open Questions

- 无。

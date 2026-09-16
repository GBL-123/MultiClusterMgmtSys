# pod-logs Tasks

## 1. 服务与数据契约

- [x] 1.1 新增 `Requests/PodLogRequest.cs`(ClusterId/Namespace/Name/Container?/TailLines/Previous)与 `ViewModels/PodLogViewModel.cs`(Content/LineCount);`dotnet build` 0 错误
- [x] 1.2 `PodService` 增 `GetPodLogAsync`:透传 container/tailLines/previous,集群不存在抛 `NotFoundException`,K8s 失败经 `K8sExceptionMapper` 翻译(含容器未启动 400),不写审计;验证:`dotnet build` 0 错误
- [x] 1.3 `K8sMocks` 补 `SetupReadPodLog`/`SetupReadPodLogThrows`(照探针签名,含 previous/tailLines/container 匹配);`PodServiceTests` 覆盖默认读取/重启前/行数透传/空日志行数/404 翻译/400 容器未启动翻译;验证:测试全绿

## 2. 详情页日志 tab

- [x] 2.1 `PodDetail.razor` 增「日志」tab 与工具条(容器下拉[默认第一项]/行数下拉[200/500/1000/5000,默认 500]/「上次运行(重启前)」开关/数据截至/刷新),懒加载:首次激活才拉取,参数变更重拉,失败在查看器区域显示错误 + 重试;验证:`dotnet build` 0 错误
- [x] 2.2 `app.css` 增 `.log-viewer`(pre + mono + 最大高度滚动 + hairline/paper,对齐 `.event-message-viewer` 语言);空日志显示 `[ 暂无日志 ]` 空态;验证:构建通过 + 手工核对样式类与既有语言一致
- [x] 2.3 bunit 测试:进入详情不发日志请求、切到日志 tab 拉取并展示内容、切换参数重拉、空日志空态、加载失败可重试;验证:测试全绿
- [x] 2.4 日志查看器纵向撑满浏览器高度(日志卡走 `.log-card` 全高配方:card/content/stack/viewer 逐层 flex + min-height:0,与 `.yaml-card` 同模式;去除固定 max-height),验证:构建通过 + 日志 tab 相关测试全绿

## 3. 全量回归

- [x] 3.1 `dotnet build MultiClusterMgmtSys.slnx` 0 错误
- [x] 3.2 `dotnet test MultiClusterMgmtSys.Tests` 全绿(基线 716 + 新增)

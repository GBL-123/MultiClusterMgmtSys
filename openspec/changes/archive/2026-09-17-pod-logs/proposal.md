# Pod 日志查看

## Why

`pod-management` 已提供 Pod 列表与详情(含容器状态与崩溃原因),但排障闭环缺最后一半:定位到异常容器(CrashLoopBackOff/OOMKilled)后无法查看其输出与退出前的日志。日志是 Pod 视角的核心使用场景,补齐后与事件页、状态徽章形成完整排障路径。

## What Changes

- 新增只读服务能力:按集群/命名空间/名称读取单个容器日志,支持**容器选择**、**行数上限**(200/500/1000/5000,默认 500)与**上次运行日志(重启前,previous)**;容器未启动等 K8s 错误经既有异常翻译链路提示;无审计。
- Pod 详情页新增「日志」tab(沿用既有 `*DetailToolbar` + `MudTabs` 结构):**首次切到该 tab 才加载**(不随详情页进入一次性拉取),容器/行数/重启前变更触发重新加载;手动刷新 + 「数据截至」时间戳,不做自动跟随。
- 新增等宽滚动日志查看器展示约定(`.log-viewer`:hairline + paper,空日志显示空态)。
- `K8sMocks` 增补日志读取 mock 与测试。

## Capabilities

### New Capabilities
- `pod-logs`: Pod 日志读取契约——参数(容器/行数/重启前)、按需加载与重拉语义、展示与空态、只读边界与错误降级。

### Modified Capabilities

## Impact

- **代码**:`Services/PodService.cs` 增 `GetPodLogAsync` 及 `Requests/PodLogRequest.cs`、`ViewModels/PodLogViewModel.cs`(Content/LineCount);`Components/Pods/Pages/PodDetail.razor` 增「日志」tab 与共享控件;`wwwroot/css/app.css` 增 `.log-viewer`。
- **测试**:服务级(参数透传/错误翻译/空日志)+ bunit(懒加载只拉一次、切换参数重拉、空态、错误可重试),基线 716 全绿为回归底线。
- **范围外**:流式/自动跟随(follow)、日志下载、时间戳开关、多容器合并输出。

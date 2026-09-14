# Pod 管理(列表 + 详情)

## Why

全系统当前没有任何 Pod 视角(代码中无 Pod API 调用):工作负载页面停在 Deployment/StatefulSet/DaemonSet/ReplicaSet 层级,而日常运维约八成的操作与排障发生在 Pod 层(看状态、看重启、定位异常容器)。事件管理页刚上线,缺少 Pod 维度与之联动;补齐 Pod 视角同时能放大约 80% 的既有页面价值。

## What Changes

- 新增只读 `PodService`(无审计、经集群客户端缓存):按集群列举 Pod(全量拉取后轻投影成瘦展示模型)、读取单个 Pod 详情(基本信息 + 容器状态)、拉取命名空间下拉。
- 新增 Pods 列表页(`/pods`、`/pods/{ClusterId:int}`):命名空间/状态分类/关键词(名称、节点、IP)内存过滤,沿用全站 FilterBar/Table 三件套与展示约定。
- 新增 Pod 详情页(`/pods/{ClusterId:int}/{Namespace}/{Name}`):基本信息与容器状态卡,**暂无日志 tab**(日志由后续 change 提供)。
- 新增 Pod 状态徽章语义:**容器级信号优先于 phase**(waiting/terminated reason 有值时以 reason 为主文案并按异常处理,如 CrashLoopBackOff 在 phase=Running 时必须暴露),未登记值回退原文;中英双语按 display-conventions。
- 工作负载详情页新增「查看 Pod」直达:按该工作负载的 label selector 列出其 Pod。
- 集群不可达/加载失败按既有优雅降级与异常翻译链路处理;列表无写操作、不写审计。

## Capabilities

### New Capabilities
- `pod-management`: Pod 只读视角契约——列表加载与内存过滤、状态徽章语义(容器信号优先)、详情展示、工作负载直达、只读边界与规模语义。

### Modified Capabilities

## Impact

- **代码**:新增 `Services/PodService.cs` 与 `Requests/`、`ViewModels/`、`Models/` 各若干瘦类型;新增 `Components/Pods/` 列表与详情页面;`ViewModels/Mappings/K8sDisplayText.cs` 增补 Pod phase 与容器状态映射;工作负载详情视图加一个直达链接;导航注册。
- **测试**:服务级测试(K8sMocks 需补 ListPods/ReadPod mock 扩展)+ bUnit 接线测试(经 `IClusterClientCache`),新增测试沿用既有基线(667 全绿为回归底线)。
- **规模语义**:全量拉取模式适用于数千 pod 量级;万级 pod 集群的后续路径(K8s 原生分页 + field selector 兜底)记录于 design,不在本期。
- **范围外**:Pod 写操作(删除/重建)、exec 终端、日志(后续 `pod-logs` change)、自动跟随轮询。

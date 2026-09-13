# kubernetes-call-timeout

## Purpose

为系统所有对集群的 Kubernetes API 调用提供统一、可预期的时间上限,使不可达集群以固定的短超时降级,而不是用客户端库默认的长超时拖垮状态刷新与页面数据加载。

## Requirements

### Requirement: Kubernetes API 调用统一超时
系统 SHALL 对每次 Kubernetes REST 调用施加不超过 10 秒的请求超时,覆盖 kubeconfig 与 Token 两种连接方式登记的集群,以及集群探测、节点/工作负载/ConfigMap/Service/命名空间的读取与写入。超时 SHALL 按集群不可达语义降级,任何调用 SHALL NOT 无限期等待。

#### Scenario: 探测超时快速降级
- **WHEN** 某集群 API 端点接受连接但不响应(连接黑洞),整轮刷新探测到该集群
- **THEN** 该次探测在约 10 秒内结束,集群被置为 `Offline`、版本清空、节点数置 0、`LastCheckedAt` 更新,并记录告警日志

#### Scenario: 页面数据加载超时
- **WHEN** 用户打开不可达集群的节点、ConfigMap、工作负载、Service 或命名空间数据
- **THEN** 加载在约 10 秒内失败,并按既有异常体系提示集群不可达或连接失败,而非长时间挂起

#### Scenario: 两种连接方式行为一致
- **WHEN** 两个集群分别以 kubeconfig 与 Token 方式登记,且 API 端点均为连接黑洞
- **THEN** 两者的调用具有相同的超时上限与降级行为

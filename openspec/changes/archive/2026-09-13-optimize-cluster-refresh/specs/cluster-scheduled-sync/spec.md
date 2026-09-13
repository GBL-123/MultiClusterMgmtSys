# cluster-scheduled-sync delta (optimize-cluster-refresh)

## MODIFIED Requirements

### Requirement: 后台定时刷新集群状态
系统 SHALL 提供一个后台服务(`BackgroundService`),按当前生效间隔对所有集群执行与手动「刷新所有集群」一致的探测(探测 `Status`/`Version`/`NodeCount`/`LastCheckedAt`),无需用户干预。每轮开始时 SHALL 读取最新生效设置;启用时执行探测,停用时跳过本轮。整轮探测 SHALL 以有界并发执行(同时最多 4 个),任何单个集群的探测(无论成功、失败或挂起)SHALL NOT 阻塞其它集群的探测完成与落库。单集群探测失败 SHALL 静默降级(置 `Offline`、清空版本与节点数快照、记录日志),不得中断整轮同步,不得向用户弹错。

#### Scenario: 按间隔自动刷新
- **WHEN** 定时同步处于启用状态且到达当前生效间隔
- **THEN** 系统对所有集群并发执行探测并持久化最新状态快照

#### Scenario: 单集群探测失败不中断
- **WHEN** 某集群探测时 K8s API 不可达
- **THEN** 该集群状态被置为 `Offline`、`Version` 清空、`NodeCount` 置 0、`LastCheckedAt` 更新,日志记录告警
- **AND** 其余集群继续探测,整轮同步正常完成

#### Scenario: 一个挂起集群不拖慢其余集群
- **WHEN** 某集群的 API 调用挂起直到超时,而其余集群可正常探测
- **THEN** 其余集群的探测结果不等待挂起集群即可完成并持久化

#### Scenario: 停用后跳过本轮
- **WHEN** 生效设置中定时同步为停用状态
- **THEN** 后台循环跳过探测,等待下一个间隔后重新读取设置

## ADDED Requirements

### Requirement: 整轮同步支持停机取消
系统 SHALL 将宿主停机信号透传到整轮刷新:取消后 SHALL NOT 再发起新的集群探测,并尽快结束当前轮次。因取消而中止的探测 SHALL NOT 被当作探测失败处理——不得把集群状态改写为 `Offline`、不得改写版本/节点数/`LastCheckedAt` 快照、不得写状态翻转审计;取消前已完成的集群结果保留。

#### Scenario: 停机停止后续探测
- **WHEN** 宿主在整轮刷新期间停机(或调用方传入已取消的令牌)
- **THEN** 系统不再发起新的集群探测,整轮调用尽快返回

#### Scenario: 取消不写失败状态
- **WHEN** 某集群的探测因停机取消而中止
- **THEN** 该集群的 `Status`/`Version`/`NodeCount`/`LastCheckedAt` 保持不变,且不产生状态翻转审计

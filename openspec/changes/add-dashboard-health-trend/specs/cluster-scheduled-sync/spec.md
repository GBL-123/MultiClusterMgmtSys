# Spec Delta

## ADDED Requirements

### Requirement: 快照保留清理

系统 SHALL 提供节点健康快照的保留策略:过期记录(采集时间早于保留窗口起点)SHALL 被自动删除。保留窗口 SHALL 以天数计,代码默认 90 天;应用配置(appsettings 配置节 `ClusterSync:SnapshotRetentionDays`)SHALL 可覆写该值,配置缺失或非法(非正整数)SHALL 回退代码默认,不得导致宿主启动失败或崩溃。

清理 SHALL 在每轮全量刷新完成后执行——手动「刷新全部」与定时同步轮次共用该收尾时机——并 SHALL 在进程内限频:实际执行每小时至多一次,限频窗口内的完成轮次 SHALL 跳过清理。清理 SHALL 仅针对本地节点健康快照表删除过期记录,SHALL NOT 发起任何 Kubernetes API 调用。

清理失败 SHALL 只记录日志告警,SHALL NOT 影响当轮刷新的既有结果,SHALL NOT 向用户弹错;清理属于自动维护操作,SHALL NOT 写入审计记录。

#### Scenario: 默认保留窗口生效

- **WHEN** 未配置 `ClusterSync:SnapshotRetentionDays` 且一条快照采集于 91 天前
- **THEN** 清理执行后该快照被删除;90 天内的快照保留

#### Scenario: 配置覆写生效

- **WHEN** 配置 `ClusterSync:SnapshotRetentionDays` 为 30 且一条快照采集于 45 天前
- **THEN** 清理执行后该快照被删除

#### Scenario: 非法配置回退默认

- **WHEN** 配置 `ClusterSync:SnapshotRetentionDays` 为 `0`、负数或其他非正整数
- **THEN** 系统回退默认 90 天并按其清理,后台与宿主不受影响

#### Scenario: 清理失败不破坏刷新

- **WHEN** 清理执行时数据库写入发生异常,而本轮全量刷新已成功
- **THEN** 系统只记录告警日志,本轮刷新结果与用户界面不受影响

#### Scenario: 自动清理不写审计

- **WHEN** 一轮清理成功删除了过期快照
- **THEN** 不产生任何审计记录

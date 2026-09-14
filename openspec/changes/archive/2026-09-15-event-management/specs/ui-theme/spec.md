## MODIFIED Requirements

### Requirement: 状态徽章
系统 SHALL 用「淡彩底 + 深字」徽章表达集群/节点状态与事件类型,替代实心色块:在线 = `#EDF3EC` 底 + `#346538` 字;离线 = `#FDEBEC` 底 + `#9F2F2D` 字;未知 = `#FBF3DB` 底 + `#956400` 字;事件类型警告(`Warning`)= `#FBF3DB` 底 + `#956400` 字;事件类型正常(`Normal`)= `#EDEAE3` 底 + `#57534E` 字。徽章圆角 SHALL 为 3px。该样式 SHALL 统一应用于集群表、节点表、ConfigMap 表、事件表与详情页状态展示。事件类型正常变体 SHALL 为中性语义,SHALL NOT 复用绿/红/琥珀语义。

#### Scenario: 在线集群展示
- **WHEN** 集群状态为在线
- **THEN** 其状态徽章显示为淡绿底深绿字

#### Scenario: 离线集群展示
- **WHEN** 集群状态为离线
- **THEN** 其状态徽章显示为淡红底深红字

#### Scenario: 警告事件展示
- **WHEN** 事件类型为 `Warning`
- **THEN** 其类型徽章显示为淡琥珀底深琥珀字

#### Scenario: 正常事件展示
- **WHEN** 事件类型为 `Normal`
- **THEN** 其类型徽章显示为中性淡底深灰字,不呈现绿/红/琥珀语义

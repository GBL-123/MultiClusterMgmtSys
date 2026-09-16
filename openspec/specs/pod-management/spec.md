# pod-management

## Purpose

为系统补齐 Pod 只读视角:按集群列举与检视 Pod(状态徽章语义以容器级信号优先,暴露 CrashLoopBackOff 等藏在运行中 Pod 里的异常),提供从列表到详情与事件联动的排障路径,并从工作负载详情直达其 Pod。所有能力只读,复用既有集群客户端缓存、统一超时与异常翻译契约。

## Requirements

### Requirement: Pod 列表按集群加载

系统 SHALL 支持按集群一次性加载该集群全部 Pod 并投影为瘦展示模型(名称、命名空间、所在节点、状态信号、重启次数、IP、启动时间);集群不存在 SHALL 抛业务"未找到"异常,K8s 调用失败 SHALL 经既有异常翻译为业务异常。命名空间过滤 SHALL 在服务入参中支持(null = 全部命名空间)。该全量加载模式 SHALL 适用于数千 pod 量级。

#### Scenario: 列出全部命名空间的 Pod

- **WHEN** 用户打开某在线集群的 Pod 列表且未选择命名空间
- **THEN** 系统一次性加载该集群全部命名空间的 Pod 并展示瘦模型列表

#### Scenario: 集群不存在或不可达时降级

- **WHEN** 用户请求加载一个不存在的集群,或该集群当前不可达
- **THEN** 前者抛业务"未找到"异常,后者经既有异常翻译呈现集群不可达提示,页面不崩溃、不长时间挂起

### Requirement: 列表内存过滤

Pod 列表 SHALL 支持纯前端内存过滤:命名空间精确匹配、状态分类、关键词(按 Pod 名称、所在节点与 Pod IP 包含匹配,忽略大小写);过滤 SHALL NOT 触发额外的 K8s 调用。

#### Scenario: 组合过滤

- **WHEN** 用户已选择某命名空间并输入关键词
- **THEN** 列表仅显示该命名空间下名称/节点/IP 命中关键词的 Pod,期间不发起新的 K8s 请求

#### Scenario: 重置过滤

- **WHEN** 用户点击重置
- **THEN** 过滤条件清空并恢复显示全量数据,不重新加载

### Requirement: Pod 状态徽章语义

Pod 行的状态徽章 SHALL 以容器级信号优先于 Pod phase:存在 waiting 或 terminated 原因时,徽章主文案 SHALL 为该原因(中文主行 + 英文次行)并按异常样式呈现;否则按 phase 呈现(运行中 online、失败 offline、其余 unknown)。未登记的原因值 SHALL 回退显示原文。重启次数 SHALL 独立展示,不计入徽章语义。

#### Scenario: 运行中 Pod 处于崩溃循环

- **WHEN** 某 Pod 的 phase 为 Running 但其容器处于 CrashLoopBackOff
- **THEN** 该行徽章显示崩溃循环原因(CrashLoopBackOff)并按异常样式呈现,而非"运行中"

#### Scenario: 正常运行与已完成

- **WHEN** 某 Pod 无容器异常信号且 phase 为 Running;另一 Pod phase 为 Succeeded(如 Job 产物)
- **THEN** 前者显示"运行中 (Running)"在线样式,后者显示"已完成 (Succeeded)"中性样式

#### Scenario: 未登记的容器原因回退原文

- **WHEN** 某容器 waiting 原因为系统未登记的新值
- **THEN** 徽章主文案回退显示该原文,不丢失信息

### Requirement: Pod 详情展示

系统 SHALL 提供单个 Pod 详情视图:基本信息(命名空间、所在节点、IP、phase、QoS、启动时间、条件)与容器状态卡(每容器的就绪/状态/重启次数/镜像/最近终止原因);详情 SHALL 由列表行点击进入,Pod 不存在或已删除时呈现"未找到"提示。

#### Scenario: 从列表进入详情

- **WHEN** 用户点击列表中某 Pod 行
- **THEN** 进入该 Pod 详情页,展示基本信息与各容器状态(含最近一次异常终止原因与时间)

#### Scenario: 详情目标已不存在

- **WHEN** 用户打开的 Pod 已被删除或不存在
- **THEN** 详情页呈现"未找到"空状态,不抛出系统错误

### Requirement: 工作负载直达 Pod 列表

工作负载详情页 SHALL 提供「查看 Pod」入口,按该工作负载的 label selector 列出其 Pod;列表的加载与状态语义 SHALL 与 Pod 列表页一致。

#### Scenario: 从 Deployment 详情直达其 Pod

- **WHEN** 用户在某 Deployment 详情页点击「查看 Pod」
- **THEN** 按该 Deployment 的 selector 列出其 Pod,过滤与状态语义与 Pod 列表页一致

### Requirement: 只读边界

Pod 视角 SHALL 只提供查看能力:不提供 Pod 删除/重建等写操作,不写审计;仅当用户已认证才可访问。集群不可达时详情与列表 SHALL 按既有降级语义呈现,不向用户弹系统错误。

#### Scenario: 无写操作入口

- **WHEN** 用户(含管理员)浏览 Pod 列表与详情
- **THEN** 界面不出现删除/重建等变更操作入口,审计日志不因此产生记录

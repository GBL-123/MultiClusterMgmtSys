## MODIFIED Requirements

### Requirement: K8s 枚举值的中英双语展示
系统 SHALL 对已知的 Kubernetes 枚举值以「中文主行 + 英文原值次行」展示:主行为中文(正文样式),次行为英文原值(等宽字体、次要色)。替换 K8s 英文原值的展示 MUST 在同一展示单元保留英文原值(徽章场景为徽章相邻次行;详情字段场景为值下方次行),SHALL NOT 丢弃原文。应用自有状态(集群在线/离线、工作负载滚动三态)与契约已定义中文口径的展示不强制附带英文。未识别的值 SHALL 回退为原始文本单行展示,不得显示空白或臆造中文。

映射 SHALL 至少覆盖:

- 节点状态:`Ready` → 就绪、`NotReady` → 未就绪、`Unknown` → 未知
- 节点角色:`control-plane` → 控制平面、`worker` → 工作节点、`master` → 主节点;多角色以顿号连接,未知角色保留原值
- 节点阶段:`Running` → 运行中、`Pending` → 等待中、`Terminated` → 已终止
- 节点地址类型:`InternalIP` → 内网 IP、`ExternalIP` → 外网 IP、`Hostname` → 主机名
- 污点效果:`NoSchedule` → 禁止调度、`PreferNoSchedule` → 尽量不调度、`NoExecute` → 驱逐
- 节点条件类型:`Ready` → 就绪、`MemoryPressure` → 内存压力、`DiskPressure` → 磁盘压力、`PIDPressure` → PID 压力、`NetworkUnavailable` → 网络不可用
- 条件状态:`True` → 成立、`False` → 不成立、`Unknown` → 未知
- 工作负载条件类型:`Available` → 可用、`Progressing` → 进行中、`ReplicaFailure` → 副本失败
- Service 类型:`ClusterIP` → 集群内 IP、`NodePort` → 节点端口、`LoadBalancer` → 负载均衡、`ExternalName` → 外部名称
- Endpoints 就绪状态:`Ready` → 就绪、`NotReady` → 未就绪
- 账号角色:`Admin` → 管理员、`Member` → 成员
- 命名空间阶段:`Active` → 在线、`Terminating` → 未知
- 集群连接方式:`Kubeconfig` → 配置文件、`Token` → 访问令牌
- 事件类型:`Normal` → 正常、`Warning` → 警告

#### Scenario: 节点状态双语展示
- **WHEN** 节点状态为 `Ready`
- **THEN** 展示单元主行显示「就绪」,次行显示等宽字体的 `Ready`
- **AND** 状态徽章的淡彩底/深字语义与 `ui-theme` 状态徽章契约一致

#### Scenario: Service 类型双语展示
- **WHEN** Service 类型为 `NodePort`
- **THEN** 类型单元格主行显示「节点端口」,次行显示等宽字体的 `NodePort`

#### Scenario: 事件类型双语展示
- **WHEN** 事件类型为 `Warning`
- **THEN** 徽章主行显示「警告」,次行显示等宽字体的 `Warning`

#### Scenario: 未知枚举回退
- **WHEN** 节点角色为未登记的 `custom-role`
- **THEN** 该值以原始文本展示,不显示空白或臆造中文

#### Scenario: 应用自有状态不强制英文
- **WHEN** 集群状态展示为「在线」或「离线」
- **THEN** 不要求附带英文次行

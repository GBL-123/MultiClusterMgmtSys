# display-conventions

## Purpose

定义全站统一的展示规范:将 Kubernetes 英文字段与枚举默认以中文呈现并配英文对照,将数值换算为人类可读单位,并约定原始值通过统一样式的 tooltip 保留,使所有页面的展示口径一致、可直接看懂。

## Requirements

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

#### Scenario: 节点状态双语展示
- **WHEN** 节点状态为 `Ready`
- **THEN** 展示单元主行显示「就绪」,次行显示等宽字体的 `Ready`
- **AND** 状态徽章的淡彩底/深字语义与 `ui-theme` 状态徽章契约一致

#### Scenario: Service 类型双语展示
- **WHEN** Service 类型为 `NodePort`
- **THEN** 类型单元格主行显示「节点端口」,次行显示等宽字体的 `NodePort`

#### Scenario: 未知枚举回退
- **WHEN** 节点角色为未登记的 `custom-role`
- **THEN** 该值以原始文本展示,不显示空白或臆造中文

#### Scenario: 应用自有状态不强制英文
- **WHEN** 集群状态展示为「在线」或「离线」
- **THEN** 不要求附带英文次行

### Requirement: 字段标签与例外清单
系统 SHALL 将页面中直接使用 Kubernetes 英文字段名的字段标签改为「中文 (English)」形式,中文为主、英文括注。以下内容 SHALL 保持原文,不做翻译:指标类标识符(YAML、UID、API Server、ClusterIP、PodCIDR、TCP/UDP/SCTP 协议)、自由文本(条件 Reason/Message、镜像/内核/容器运行时版本值)、K8s 标签与注解键、YAML 内容与资源名称。

#### Scenario: 系统信息字段标签
- **WHEN** 节点详情的系统信息卡渲染 `Architecture` 字段
- **THEN** 标签显示为「架构 (Architecture)」

#### Scenario: 标识符与自由文本不翻译
- **WHEN** 页面渲染 YAML tab、UID、API Server、ClusterIP、协议 `TCP`,或条件的 Reason/Message 文本
- **THEN** 这些内容保持原文,不添加中文翻译

### Requirement: 数值人类可读化与原始值 tooltip
数值类展示 SHALL 换算为人类可读单位并保留原始值:CPU 以「核」展示(最多三位小数)、字节量以 IEC 二进制单位展示(最多一位小数)、Pod/副本以「个」、节点数以「台」。复杂换算(如 Kubernetes quantity)的原始字符串 SHALL 通过统一样式的 tooltip 查看,SHALL NOT 依赖原生 `title` 属性。无法解析或换算失败时 SHALL 回退展示原始文本。

#### Scenario: 资源容量原始值可查
- **WHEN** 节点资源容量显示 `15.5 GiB`
- **THEN** 悬停该值显示统一样式的 tooltip,内容为原始 quantity `16297496Ki`

#### Scenario: 计数带单位
- **WHEN** 集群有 3 个节点、某工作负载 2/3 副本就绪
- **THEN** 节点数显示 `3 台`,就绪度显示 `2/3 个`

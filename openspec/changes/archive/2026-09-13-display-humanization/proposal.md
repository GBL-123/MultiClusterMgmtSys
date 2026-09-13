## Why

展示层大量直出 Kubernetes 英文枚举与原始数值(节点状态 `Ready`/`NotReady`、角色 `control-plane`、阶段 `Running`、条件 `True`/`False`/`MemoryPressure`、Service 类型 `ClusterIP`、协议 `TCP`、账号角色 `Admin`/`Member`、系统信息字段标签 `Architecture` 等),审计类别还漏翻了 `Service`/`Namespace`;tooltip 形态混杂(MudBlazor 默认灰气泡、原生 `title`、无视觉规范),不符合 Swiss Industrial Print 设计系统。用户需要在所有页面"中文默认 + 英文对照",数值换算为人类可读单位,原始值可在 tooltip 查看——节点资源容量 tab 已经验证了这套范式,本次把它推广到全站。

## What Changes

- **新增展示映射层**:把 K8s 枚举 → 中文/CSS 类的转换集中到一个纯函数映射类(节点状态/角色/阶段/地址类型/污点效果/条件类型与 True-False-Unknown/工作负载条件/Service 类型/账号角色/命名空间阶段);ViewModel 增加 `*Text`(必要时 `*CssClass`)字段,raw 字段保留(排序、过滤、tooltip 原值仍然依赖它)。
- **全站中英双语展示(BREAKING:若干可见文案变化)**:值类展示采用"中文主行 + 英文次行"(与资源容量 tab 的 资源/原始 key 一致),字段标签采用"中文 (English)";自由文本(条件 Reason/Message、镜像名、标签注解 key、YAML)保持原样;`TCP`/`YAML`/`ClusterIP`/`API Server` 等标识符与缩写不翻译。
- **单位人类可读化**:节点资源容量沿用现有换算(契约在 `node-detail-layout`);集群节点数带"台"、工作负载副本数带"个";复杂换算的原始值通过统一 tooltip 查看(替代原生 `title`)。
- **Tooltip 全站统一**:墨底纸字、3px 圆角、无投影、小三角;原始值用等宽字体,长文本可换行;统一延迟/位置/焦点行为;消灭所有原生 `title=(8 处)`,图标操作统一走 `TooltipIconButton`,新增文本 tooltip 共享组件。
- **收编重复 helper**:`GetNodeStatusClass`(3 处)、`GetRolloutText/Class`(2 处)、`GetConditionClass`。
- **修复既有缺陷**:`AuditLogMappingExtensions` 补 `Service`/`Namespace` 类别中文(现为英文,违反 `audit-log` 既有契约);`ui-theme` 强调色 token 与代码对齐。
- 不新增 NuGet 依赖、不改数据库 schema、不改路由与交互流程。

## Capabilities

### New Capabilities

- `display-conventions`: 全站展示规范——K8s 字段/枚举的中英双语展示、语言例外清单、数值人类可读单位与原始值 tooltip 的内容约定。

### Modified Capabilities

- `ui-theme`: 新增 tooltip 视觉与行为契约(墨底纸字/3px/无投影/等宽原值/统一延迟与焦点),原生 `title` 禁用与触发统一策略;新增双语展示词汇(`StatusBadge`/`StackedText`);强调色 token 与代码实际值对齐。
- `node-detail-layout`: 节点概览、条件表与系统信息卡的字段标签/枚举值双语展示;资源容量原始值由 `title` 属性改为统一 tooltip。
- `nodes-page`: 节点列表的状态/角色双语展示;筛选下拉选项(角色/状态)双语标签(提交值保持 raw)。
- `service-management`: Service 类型、Endpoints 状态徽章与标签页名称双语展示;类型筛选选项双语标签。
- `workload-management`: 工作负载条件表的类型/状态双语与「UID」字段标签中文化。
- `accounts-page`: 账号列表角色列双语展示。
- `profile-page`: 个人资料角色徽章双语展示。
- `cluster-detail`: 连接方式中文化 + 英文对照;节点数带单位。
- `namespace-management`: 命名空间状态徽章补充英文次要行(`Active`/`Terminating`)。

## Impact

- **新增**:`ViewModels/Mappings/K8sDisplayText.cs`(纯函数映射)、`Components/Common/` 下双语展示与 tooltip 共享组件。
- **修改**:`ViewModels/`(ClusterNodeViewModel、ClusterNodeDetailViewModel、NodeAddressViewModel、NodeTaintViewModel、NodeConditionViewModel、WorkloadListViewModel、WorkloadConditionViewModel、SvcListViewModel、SvcPortViewModel、SvcEndpointViewModel、AccountViewModel、ClusterDetailViewModel、Namespace* 等)及既有映射扩展;`Components/`(节点、工作负载、服务、账号、审计、集群、命名空间、个人资料约 20 个组件);`wwwroot/css/app.css`(tooltip 全局样式);`Program.cs` 无改动。
- **测试**:更新对英文原文的既有断言(`NodeListTableTests`、`AccountTableTests`、Svc/Workload 映射测试等);新增映射层单测、双语渲染接线测试与 tooltip 契约测试。
- **规格**:`openspec/specs/display-conventions/`(新)、`ui-theme`、`node-detail-layout`、`nodes-page`、`service-management`、`workload-management`、`accounts-page`、`profile-page`、`cluster-detail`、`namespace-management`。

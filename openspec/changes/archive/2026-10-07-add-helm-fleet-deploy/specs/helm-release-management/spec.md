# Spec Delta

## MODIFIED Requirements

### Requirement: 页面、导航与展示口径

系统 SHALL 提供 `/helm` 页面(集群上下文,复用集群选择栏)并在侧边导航提供入口;release 状态 SHALL 按 `display-conventions`(中文主行 + 英文次行)与 `ui-theme`(在线/离线/未知徽章语义)的口径展示;页面 SHALL 使用中文文案与既有设计词汇(空态、等宽数据列、YAML 查看卡、统一 tooltip),SHALL NOT 使用原生 `title` 属性承载提示。页面工具栏 SHALL 按 Admin 角色条件渲染「批量下发」入口(批量下发行为见 `helm-fleet-deploy` 契约),Member 视图 SHALL NOT 出现该入口。

#### Scenario: 导航入口

- **WHEN** 任意登录用户打开侧边导航
- **THEN** 可见「应用管理」入口并可进入 `/helm`

#### Scenario: 空态

- **WHEN** 选中集群没有安装任何 release
- **THEN** 列表显示既有空态样式的中文提示

#### Scenario: 状态徽章

- **WHEN** release 状态为 `deployed`
- **THEN** 徽章以中文「已部署」与在线色系展示,并附等宽英文次行

#### Scenario: 批量下发入口按角色渲染

- **WHEN** Admin 与 Member 分别打开 `/helm` 页面
- **THEN** Admin 工具栏可见「批量下发」入口,Member 工具栏不出现该入口

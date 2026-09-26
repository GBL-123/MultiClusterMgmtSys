# Spec Delta

## Purpose

定义 Helm release 的读取与生命周期契约:已安装 Chart 包的可见范围、安装/升级/回滚/卸载行为、上传包校验与临时文件语义、归属记账与操作权限强制,以及页面路由与展示口径。

## ADDED Requirements

### Requirement: Release 列表读取

系统 SHALL 在「应用管理」页(路由 `/helm`)以当前选中集群为上下文,经 Helm CLI 列出该集群全部命名空间的 release,每项至少包含:名称、命名空间、chart 名称与版本、appVersion、revision、状态、更新时间。集群不存在时 SHALL 抛 `NotFoundException`;集群不可达或调用失败时 SHALL 经既有异常体系翻译为中文业务异常提示,SHALL NOT 直出原始错误输出。

#### Scenario: 列出全部命名空间的 release

- **WHEN** 用户在集群选择栏选中集群并通过 `/helm` 打开应用管理页
- **THEN** 列表展示该集群全部命名空间下已安装的 release,含名称、命名空间、chart 版本、revision 与状态

#### Scenario: 未选择集群

- **WHEN** 用户打开 `/helm` 且未选择任何集群
- **THEN** 页面显示与其它资源页一致的「请选择集群」提示,不发起 Helm 调用

#### Scenario: 集群不可达

- **WHEN** 选中集群不可达,用户打开 `/helm`
- **THEN** 页面按既有集群不可达语义提示中文错误,不长时间挂起

### Requirement: Release 详情与历史

系统 SHALL 提供 release 详情(状态与 NOTES、用户提供的 values、渲染后的 manifest)与 revision 历史列表(序号、状态、时间、描述),全部经 Helm CLI 读取;release 不存在时 SHALL 提示未找到;读取失败时 SHALL 经既有异常体系提示。

#### Scenario: 详情展示

- **WHEN** 用户打开某 release 详情
- **THEN** 页面展示状态、NOTES、用户 values 与 manifest,manifest 使用既有 YAML 查看卡片样式

#### Scenario: 历史列表

- **WHEN** 用户查看某 release 的历史
- **THEN** 展示按 revision 倒序的历史项,含状态、时间与描述

#### Scenario: release 不存在

- **WHEN** release 已被系统外删除,用户访问其详情
- **THEN** 页面提示「release 不存在」类中文错误

### Requirement: Chart 包上传与校验

安装与升级对话框 SHALL 接受 `.tgz` chart 包;系统 SHALL 在上传时解析包内 `Chart.yaml`,提取名称与版本用于展示、默认 release 名与升级前版本对照;包不合法(非法归档、缺失或无法解析 `Chart.yaml`)时 SHALL 以中文校验错误拒绝,SHALL NOT 调用 Helm 执行。包内容 SHALL 仅服务于当次操作,以临时文件承载,操作结束(无论成败)或对话框放弃后 SHALL 清理。

#### Scenario: 合法包解析

- **WHEN** 用户上传 chart 包 `nginx-1.2.3.tgz`
- **THEN** 系统展示 chart 名称 `nginx` 与版本 `1.2.3`,并将 release 名称预填为 `nginx`

#### Scenario: 非法包拒绝

- **WHEN** 用户上传的文件不是合法 chart 包或缺少 `Chart.yaml`
- **THEN** 系统以中文校验错误提示并保持对话框可继续操作,不调用 Helm

#### Scenario: 临时文件清理

- **WHEN** 安装操作成功、失败或被取消
- **THEN** 该次上传的 chart 包与 values 临时文件被删除

### Requirement: 安装 release

系统 SHALL 支持以「上传的 chart 包 + release 名称 + 命名空间 + 用户 values」安装 release,并可选是否创建命名空间、是否等待就绪与等待超时;安装成功 SHALL 记录归属并写入审计;安装失败 SHALL 依据 Helm 输出翻译为中文业务异常提示,SHALL NOT 记录归属。

#### Scenario: 安装成功

- **WHEN** 成员在目标集群的目标命名空间安装上传的 chart 包,release 名与包名一致且成功
- **THEN** release 出现在列表中,归属记录为当前成员,审计写入一条类别为 Helm、操作为安装的记录

#### Scenario: release 名称冲突

- **WHEN** 目标命名空间已存在同名 release 且未选择升级
- **THEN** 系统提示中文冲突错误

#### Scenario: 安装失败不留归属

- **WHEN** 安装因包渲染或资源应用失败
- **THEN** 系统提示中文错误,且不产生归属记录

### Requirement: 升级 release

系统 SHALL 支持对 release 上传新版本 chart 包执行升级;values 默认沿用现存用户提供的 values,SHALL 支持切换为重新编辑;升级成功 SHALL 写入审计且归属不变;升级失败 SHALL 经既有异常体系提示。

#### Scenario: 升级保留归属

- **WHEN** 安装者对自己的 release 上传新版本包并升级成功
- **THEN** 该 release 归属保持为原安装者,审计写入一条类别为 Helm、操作为升级的记录

#### Scenario: 沿用现存 values

- **WHEN** 用户未修改 values 直接升级
- **THEN** 升级沿用现存用户提供的 values

### Requirement: 回滚 release

系统 SHALL 支持从历史列表选择 revision 回滚;回滚成功 SHALL 写入审计,归属不变;回滚失败 SHALL 经既有异常体系提示。

#### Scenario: 回滚成功

- **WHEN** 安装者选择历史中的某个 revision 回滚
- **THEN** release 回到该 revision 的内容,审计写入一条类别为 Helm、操作为回滚的记录

### Requirement: 卸载 release

系统 SHALL 支持卸载 release,并可选是否保留历史;卸载成功 SHALL 删除归属记录并写入审计;卸载失败 SHALL 经既有异常体系提示。

#### Scenario: 卸载清理归属

- **WHEN** 安装者卸载自己的 release 成功
- **THEN** 该 release 从列表消失,归属记录被删除,审计写入一条类别为 Helm、操作为卸载的记录

#### Scenario: Admin 卸载他人 release

- **WHEN** Admin 卸载他人安装的 release 成功
- **THEN** 卸载成功且归属记录被删除

### Requirement: 归属记账与无主判定

系统 SHALL 在安装成功时记录 release 归属(集群、命名空间、release 名称、操作者、安装时间、安装时 revision);同一集群同一命名空间同一 release 名称 SHALL 至多一条归属记录;集群删除时归属记录 SHALL 级联删除。无归属记录、或记录的安装时 revision 高于当前 revision(表明 release 已被系统外重装)的 release SHALL 视为无主。

#### Scenario: 外部重装降级为无主

- **WHEN** 某 release 的归属记录对应 revision 3,而当前列表显示该 release 为 revision 1(已被系统外卸载后重装)
- **THEN** 该 release 视为无主,仅 Admin 可操作

#### Scenario: 集群删除级联

- **WHEN** 某集群被删除
- **THEN** 该集群的全部归属记录随之删除

### Requirement: 操作权限强制

系统 SHALL 在服务层强制操作权限:Admin 可操作任意 release;非 Admin 仅可操作自己安装的 release;无主 release 仅 Admin 可操作。违规调用 SHALL 抛 `PermissionException` 并带中文用户消息,SHALL NOT 仅依赖界面隐藏。release 列表对所有登录用户可见,SHALL NOT 展示安装者列。界面 SHALL 仅对可操作者渲染安装/升级/回滚/卸载动作。

#### Scenario: 成员操作他人 release 被拒

- **WHEN** 成员 A 通过任意入口尝试升级或卸载成员 B 安装的 release
- **THEN** 服务端抛 `PermissionException`,提示仅可操作自己安装的 Chart 包

#### Scenario: Admin 操作任意 release

- **WHEN** Admin 对任意 release 执行升级、回滚或卸载
- **THEN** 操作通过权限校验并成功执行

#### Scenario: 无主 release 仅 Admin 可操作

- **WHEN** 成员尝试操作系统外安装(无归属)的 release
- **THEN** 服务端拒绝并提示中文权限错误

#### Scenario: 列表不展示安装者

- **WHEN** 用户查看 release 列表
- **THEN** 列表不含安装者列,且非自己安装的 release 不渲染操作按钮

### Requirement: 页面、导航与展示口径

系统 SHALL 提供 `/helm` 页面(集群上下文,复用集群选择栏)并在侧边导航提供入口;release 状态 SHALL 按 `display-conventions`(中文主行 + 英文次行)与 `ui-theme`(在线/离线/未知徽章语义)的口径展示;页面 SHALL 使用中文文案与既有设计词汇(空态、等宽数据列、YAML 查看卡、统一 tooltip),SHALL NOT 使用原生 `title` 属性承载提示。

#### Scenario: 导航入口

- **WHEN** 任意登录用户打开侧边导航
- **THEN** 可见「应用管理」入口并可进入 `/helm`

#### Scenario: 空态

- **WHEN** 选中集群没有安装任何 release
- **THEN** 列表显示既有空态样式的中文提示

#### Scenario: 状态徽章

- **WHEN** release 状态为 `deployed`
- **THEN** 徽章以中文「已部署」与在线色系展示,并附等宽英文次行

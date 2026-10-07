# Spec Delta

## Purpose

提供 Helm chart 的批量多集群下发:管理员一次上传 chart 包并配置统一 release 参数,系统并发下发到选定的多个集群,对每个集群按「不存在即安装、已存在即升级」分支执行,逐集群隔离失败并汇总结果。

## ADDED Requirements

### Requirement: 批量下发入口与权限

系统 SHALL 在 `/helm` 页面工具栏提供「批量下发」入口,该入口 SHALL 仅对 Admin 角色渲染,Member SHALL NOT 看到入口。服务层 SHALL 独立强制 Admin 角色:非 Admin 调用 SHALL 抛中文 `PermissionException` 且 SHALL NOT 执行任何 Helm 调用或写归属/审计。批量下发入口 SHALL NOT 因当前选中集群不可达而被禁用(目标集群由表单独立选择)。

#### Scenario: Admin 可见入口

- **WHEN** Admin 打开 `/helm` 页面
- **THEN** 工具栏渲染「批量下发」按钮,点击打开批量下发对话框

#### Scenario: Member 不可见且服务端拒绝

- **WHEN** Member 打开 `/helm` 页面,或经任意入口直接调用批量下发服务方法
- **THEN** 页面不渲染「批量下发」入口;服务端抛中文权限异常,且无 Helm 调用、无归属与审计写入

#### Scenario: 入口不随选中集群禁用

- **WHEN** Admin 当前选中的集群不可达
- **THEN** 「批量下发」入口仍可点击,目标集群以表单中勾选的集群为准

### Requirement: 批量下发表单与目标选择

批量下发对话框 SHALL 接受 `.tgz` chart 包上传,复用单集群安装的上传校验口径:超过大小上限或包不合法时以中文校验错误拒绝且不发起 Helm 调用;解析成功 SHALL 预填 release 名称与 values 初值。表单 SHALL 包含:release 名称、命名空间(文本输入,前端与服务端均校验 DNS-1123 合法性)、「命名空间不存在时自动创建」选项、「等待资源就绪(--wait)」选项与统一 values(YAML 文本,应用到全部目标集群)。目标集群 SHALL 以复选框多选方式选择,每项展示集群状态徽章,未选择任何集群时 SHALL 以中文提示阻止提交。全部前置校验通过之前 SHALL NOT 发起任何集群的 Helm 调用。

#### Scenario: 上传解析与预填

- **WHEN** 用户上传合法 chart 包 `nginx-1.2.3.tgz`
- **THEN** 对话框展示 chart 名称与版本,release 名称预填为 `nginx`,values 初值为包内 values

#### Scenario: 非法包拒绝

- **WHEN** 用户上传的文件不是合法 chart 包或超过大小上限
- **THEN** 对话框以中文校验错误提示,可继续选择其它文件,不发起 Helm 调用

#### Scenario: 未选集群阻止提交

- **WHEN** 用户未勾选任何目标集群即提交
- **THEN** 对话框提示「请选择至少一个目标集群」类中文文案,不发起任何调用

#### Scenario: 前置校验失败不发起调用

- **WHEN** release 名称或命名空间不合法、包超限、或所选集群中含不存在的集群 Id
- **THEN** 服务端抛中文校验/未找到异常,整批终止,无任何集群被下发

### Requirement: 批量下发执行语义

系统 SHALL 对每个目标集群独立执行:先经 `helm status` 预检该 release 是否存在——不存在时执行安装(携带统一 values、可选创建命名空间、可选 `--wait --timeout`),已存在时执行升级(以统一 values 重新提交,不沿用 `--reuse-values`)。多集群下发 SHALL 有界并发执行(与集群探测同级的并发上限常量),SHALL 逐集群隔离失败:任一集群失败(含不可达、Helm 报错、预检失败)SHALL 记为该集群的失败结果并继续其余集群,SHALL NOT 中断整批。执行过程 SHALL 经进度回调回报「已完成 / 总数」,复用「刷新全部」的进度反馈模式;本期 SHALL NOT 提供用户侧取消入口。

#### Scenario: 新集群安装

- **WHEN** 目标集群 A 中不存在该 release,批量下发执行
- **THEN** 集群 A 走安装路径,release 出现在该集群的应用列表

#### Scenario: 已有集群升级

- **WHEN** 目标集群 B 中已存在同名同名空间的 release
- **THEN** 集群 B 走升级路径,以统一 values 重新提交,release 内容被更新

#### Scenario: 失败隔离

- **WHEN** 目标 5 个集群中 2 个不可达、3 个安装成功
- **THEN** 3 个成功集群照常完成安装与审计,2 个不可达集群记为失败结果并附中文原因,整批流程不中断

#### Scenario: 进度回报

- **WHEN** 批量下发对 7 个集群执行中
- **THEN** 对话框展示「已完成 x / 总数」形态的进度,全部完成后进入结果汇总

### Requirement: 结果汇总、归属与审计

批量下发完成 SHALL 展示逐集群结果行(集群名称、动作(安装或升级)、结果(成功或失败)、失败时的中文原因)与「成功 n / 失败 m」汇总。归属记账:走安装路径成功的集群 SHALL 写入归属记录(操作者为发起下发的 Admin);走升级路径成功的集群 SHALL 保持归属不变(与单集群升级口径一致);失败集群 SHALL NOT 写归属。审计:每个成功集群 SHALL 写一条审计(类别 Helm、操作分别为安装/升级、描述含「批量下发」与集群名、命名空间、release 名称);失败集群 SHALL NOT 写审计。

#### Scenario: 结果汇总展示

- **WHEN** 批量下发完成(3 成功 2 失败)
- **THEN** 对话框列出 5 行结果(集群/动作/结果/消息)并显示「成功 3 / 失败 2」

#### Scenario: 安装写归属

- **WHEN** Admin 对无该 release 的集群批量下发成功(安装路径)
- **THEN** 该集群生成归属记录,归属为当前 Admin

#### Scenario: 升级保持归属

- **WHEN** Admin 对已有成员安装的 release 的集群批量下发成功(升级路径)
- **THEN** 该 release 归属保持为原安装者

#### Scenario: 失败不留归属不写审计

- **WHEN** 某集群下发失败
- **THEN** 该集群不产生归属记录、不写审计,失败原因以中文展示在结果行

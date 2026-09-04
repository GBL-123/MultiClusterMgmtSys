# unit-testing

## Purpose

Define the contract for unit testing in this repository: a runnable test project mirroring the main project structure, an injectable Kubernetes client factory for mocking K8s calls, service-layer tests asserting the business-exception and query contracts, bUnit tests asserting only wiring contracts (never MudBlazor internals), MudBlazor analyzer warnings promoted to build errors, and test conventions documented in AGENTS.md.

## Requirements

### Requirement: 测试项目可运行
系统 SHALL 提供测试项目 `MultiClusterMgmtSys.Tests/`(net10.0,xUnit),挂载于 `MultiClusterMgmtSys.slnx`,可通过 `dotnet test` 运行且全部通过。测试项目 SHALL 使用 SQLite 内存数据库与生产同 provider,并配置 Moq 与 bUnit。

#### Scenario: 运行测试
- **WHEN** 执行 `dotnet test MultiClusterMgmtSys.slnx`
- **THEN** 测试项目编译并全部通过

#### Scenario: 测试数据库隔离
- **WHEN** 任一测试需要数据库
- **THEN** 该测试使用独立新建的 SQLite 内存库,测试间互不影响

### Requirement: K8s 客户端工厂可注入
`ConfigMapService` / `ClusterNodeService` / `ClusterService` SHALL 通过构造函数注入 `Func<KubernetesClientConfiguration, IKubernetes>` 创建 K8s 客户端(替代直连 `new Kubernetes(config)`),Program.cs 注册默认工厂。生产行为 SHALL 不变。

#### Scenario: 生产注册
- **WHEN** 应用启动
- **THEN** 默认工厂创建真实 Kubernetes 客户端,服务行为与改造前一致

#### Scenario: 测试注入
- **WHEN** 测试注入 mock 工厂
- **THEN** 服务使用 mock 客户端,可模拟 K8s 异常验证翻译链路

### Requirement: 服务层异常契约测试
服务测试 SHALL 覆盖业务异常契约:集群/分组不存在→`NotFoundException`(中文消息)、备注超长→`ValidationException`、新旧密码相同→`ValidationException`、分组移动 sentinel 0→`ValidationException`;经 K8s 工厂模拟 404→`NotFoundException`、409→`ConflictException`、超时→`ClusterUnreachableException`。

#### Scenario: 不存在资源
- **WHEN** 服务操作不存在的集群/分组
- **THEN** 抛出对应业务异常且 UserMessage 为中文

#### Scenario: K8s 状态码翻译
- **WHEN** mock 客户端返回 404/409 或抛超时异常
- **THEN** 服务抛出对应翻译后的业务异常

### Requirement: 查询契约经服务测试
`ClusterService.GetPagedAsync` SHALL 经服务层测试覆盖查询契约:GroupId `0`→未分组(IS NULL)、版本 sentinel `__null__`→仅未知版本、日期区间过滤、排序与分页。

#### Scenario: 分组 sentinel
- **WHEN** 查询 `GroupId = 0` 且库中存在已分组与未分组集群
- **THEN** 仅返回未分组集群

#### Scenario: 日期过滤
- **WHEN** 查询指定创建日期区间
- **THEN** 仅返回该区间内创建的集群

### Requirement: bUnit 接线契约测试
组件测试 SHALL 通过 bUnit 断言"组件如何使用 MudBlazor",SHALL NOT 断言 `.mud-*` 内部 DOM 或模拟 MudBlazor 内部行为。至少覆盖:`ClusterFilterBar` 的 `DateRangePicker` 回写、集群页 Admin/Member 门控渲染、状态徽章三态 CSS 类、最近操作卡空态。

#### Scenario: DateRangePicker 回写
- **WHEN** 触发 FilterBar 内 DateRangePicker 的 `DateRangeChanged`
- **THEN** FilterBar 的查询对象 `DateRange` 被回写

#### Scenario: 权限门控
- **WHEN** 以 Member 身份渲染集群页
- **THEN** 不渲染「刷新所有集群」「添加集群」等 Admin 按钮;以 Admin 身份渲染时可见

#### Scenario: 徽章与空态
- **WHEN** 渲染含在线/离线集群的表格及无记录的最近操作卡
- **THEN** 出现对应 `.status-badge` 类与 `.empty-state` 标记

### Requirement: MudBlazor 分析器告警转错误
主项目 csproj SHALL 将 MudBlazor 分析器告警(如 `MUD0002`)配置为构建错误,且存量告警清零后生效。

#### Scenario: API 误用拦截
- **WHEN** 组件把不存在的参数(如 `@bind-Value` 用于无 `Value` 的组件)传给 MudBlazor 组件
- **THEN** 构建失败并指出非法属性

#### Scenario: 存量告警清零
- **WHEN** 全量构建
- **THEN** 无 MudBlazor 分析器告警残留(执行前逐条处理既有告警点)

### Requirement: AGENTS.md 测试约定
AGENTS.md SHALL 记录:`dotnet test` 命令、测试目录镜像结构、服务边界测试口径、bUnit 只测接线契约的口径、TestInfrastructure 用法。

#### Scenario: 命令与口径文档化
- **WHEN** 查看 AGENTS.md
- **THEN** Commands 含 `dotnet test`,Testing 节描述上述约定
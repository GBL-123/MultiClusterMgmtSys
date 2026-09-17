# code-style Delta

## MODIFIED Requirements

### Requirement: C# 成员间空行
四个生产项目(`MultiClusterMgmtSys.Domain` / `MultiClusterMgmtSys.Application` / `MultiClusterMgmtSys.Infrastructure` / `MultiClusterMgmtSys.Web`)内的所有 .cs 类,其成员(字段、属性、方法)之间 SHALL 空一行。类声明后紧邻的首个成员不要求前置空行。

#### Scenario: 字段与属性间隔
- **WHEN** 检查任意 .cs 类的连续成员声明
- **THEN** 相邻成员之间均存在空行(除类声明后的首个成员)

#### Scenario: 审计零命中
- **WHEN** 运行"连续成员行"静态审计脚本
- **THEN** 除类首行成员外无"相邻成员缺空行"报告

### Requirement: 主项目开启 XML 文档生成
四个生产项目(`MultiClusterMgmtSys.Domain.csproj` / `MultiClusterMgmtSys.Application.csproj` / `MultiClusterMgmtSys.Infrastructure.csproj` / `MultiClusterMgmtSys.Web.csproj`)SHALL 开启 `GenerateDocumentationFile`,SHALL NOT 将 `CS1591` 加入 `NoWarn`;构建输出 SHALL 无 CS1591 警告(公共 API 面全量注释)。测试项目 SHALL NOT 开启文档生成。

#### Scenario: 构建无 CS1591
- **WHEN** 运行 `dotnet build MultiClusterMgmtSys.slnx`
- **THEN** 构建 0 错误,输出中无 `warning CS1591`

#### Scenario: 豁免项目不生成文档
- **WHEN** 检查 `MultiClusterMgmtSys.Tests.csproj`
- **THEN** 未开启 `GenerateDocumentationFile`

### Requirement: 公共 API 面中文 XML 注释
`MultiClusterMgmtSys.Domain`(`Entities`/`Enums`/`Exceptions`)、`MultiClusterMgmtSys.Application`(`Services`/`Requests`/`ViewModels` 含 `Mappings` 类型级/`Models`/`Abstractions`/`Common`)、`MultiClusterMgmtSys.Infrastructure`(`Persistence`/`Identity`/`Kubernetes`/`Templates`/`Sync`)、`MultiClusterMgmtSys.Web`(`Components/Common` 下的非 Razor 类型)的 public 类型与其成员 SHALL 具有中文 `<summary>` XML 注释;枚举每个成员 SHALL 单独注释。

#### Scenario: Services 方法注释
- **WHEN** 检查 `Application/Services` 下任一 public 方法
- **THEN** 存在中文 `<summary>`(必要时含 `<param>`/`<returns>`)

#### Scenario: 枚举成员注释
- **WHEN** 检查 `Domain/Enums` 与 `Application/Enums` 下任一枚举成员
- **THEN** 成员之上有中文 `/// <summary>` 注释

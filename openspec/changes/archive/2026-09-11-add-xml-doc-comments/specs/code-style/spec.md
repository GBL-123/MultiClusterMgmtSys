## ADDED Requirements

### Requirement: 主项目开启 XML 文档生成
主项目 csproj SHALL 开启 `GenerateDocumentationFile`,SHALL NOT 将 `CS1591` 加入 `NoWarn`;构建输出 SHALL 无 CS1591 警告(公共 API 面全量注释)。测试项目 SHALL NOT 开启文档生成。

#### Scenario: 构建无 CS1591
- **WHEN** 运行 `dotnet build MultiClusterMgmtSys.slnx`
- **THEN** 构建 0 错误,输出中无 `warning CS1591`

#### Scenario: 豁免项目不生成文档
- **WHEN** 检查 `MultiClusterMgmtSys.Tests.csproj`
- **THEN** 未开启 `GenerateDocumentationFile`

### Requirement: 公共 API 面中文 XML 注释
主项目 `Services/`、`Data/`(Repositories/Entities/ApplicationDbContext)、`Requests/`、`Models/`、`Common/`(Enums/Exceptions)、`ViewModels/`(含 Mappings 类型级)、`Components/Common` 的 public 类型与其成员 SHALL 具有中文 `<summary>` XML 注释;枚举每个成员 SHALL 单独注释。

#### Scenario: Services 方法注释
- **WHEN** 检查 Services/ 下任一 public 方法
- **THEN** 存在中文 `<summary>`(必要时含 `<param>`/`<returns>`)

#### Scenario: 枚举成员注释
- **WHEN** 检查 Common/Enums/ 下任一枚举成员
- **THEN** 成员之上有中文 `/// <summary>` 注释

### Requirement: 注释豁免范围
`.razor` 文件的 @code 块 SHALL NOT 要求新增 XML 注释;`ViewModels/Mappings/` 纯映射方法 SHALL NOT 强制逐方法注释(既有语义注释保留)。

#### Scenario: Razor 生成类零警告
- **WHEN** 开启文档生成后构建
- **THEN** .razor 生成代码不产生 CS1591 警告(源生成器已抑制)

### Requirement: 注释风格统一
XML 注释 SHALL 使用中文、`/// ` 后带空格,沿用 `<see cref>` 引用惯例;契约注释(如哨兵值语义)SHALL 与对应 OpenSpec spec 语义一致。

#### Scenario: 风格审计
- **WHEN** 用 `rg "^\s*///(?=\S)"` 审计
- **THEN** 无 `///` 后缺空格的命中

#### Scenario: 契约注释与 spec 一致
- **WHEN** 对照 `openspec/specs/cluster-query-layering/spec.md` 抽查 `ClusterPageQuery`/`ClusterQueryRequest` 注释
- **THEN** 哨兵值语义(null/0/正数、`__null__`)描述一致

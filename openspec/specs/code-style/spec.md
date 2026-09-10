# code-style

## Purpose

Define the unified code-style contract for Razor and C# sources: dependency injection declared at the top of `.razor` files, parameter annotations and member declarations on their own lines with blank-line separation, blank lines between all C# class members, Chinese XML doc comments covering the public API surface of the main project, and these conventions documented in `AGENTS.md` for future sessions.

## Requirements

### Requirement: Razor 注入统一在页面顶部
Razor 组件的依赖注入 SHALL 使用页面顶部的 `@inject` 指令,紧随 `@page`/`@attribute`/`@using` 之后;`@code` 块内 SHALL NOT 出现 `[Inject]` 属性。同一服务不得重复注入。

#### Scenario: 组件注入位置
- **WHEN** 检查任意 .razor 文件
- **THEN** 所有注入均为文件顶部的 `@inject`,且 `@code` 内无 `[Inject]`

#### Scenario: 迁移去重
- **WHEN** 原 `@code` 的 `[Inject]` 与页面已有顶部 `@inject` 同名
- **THEN** 仅保留顶部一处

### Requirement: Razor 参数注解与成员间隔
`[Parameter]` / `[CascadingParameter]` 注解 SHALL 单独占一行,属性声明 SHALL 独立一行;属性之间、属性与方法之间、方法之间 SHALL 空一行。

#### Scenario: 参数注解格式
- **WHEN** 检查任意 .razor 的 `@code` 块
- **THEN** 每个参数注解独占一行,注解下行为属性声明,相邻成员间有空行

### Requirement: C# 成员间空行
所有 .cs 类(Components/Services/Data/ViewModels/Requests/Models/Common)的成员(字段、属性、方法)之间 SHALL 空一行。类声明后紧邻的首个成员不要求前置空行。

#### Scenario: 字段与属性间隔
- **WHEN** 检查任意 .cs 类的连续成员声明
- **THEN** 相邻成员之间均存在空行(除类声明后的首个成员)

#### Scenario: 审计零命中
- **WHEN** 运行"连续成员行"静态审计脚本
- **THEN** 除类首行成员外无"相邻成员缺空行"报告

### Requirement: 风格约定写入 AGENTS.md
AGENTS.md SHALL 新增 Code style 节,记录:razor 注入顶部化、参数注解独立行、成员间空行(razor 与 C#)、审计验证方式,供后续会话遵守。

#### Scenario: 约定文档化
- **WHEN** 查看 AGENTS.md
- **THEN** 存在 Code style 节且内容与上述约定一致

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
## Why

主项目未开启 `GenerateDocumentationFile`,XML 注释覆盖率极低(114 个 .cs 中仅 26 个文件、约 99 行 `///`,Services/ 99 个 public 方法零方法级注释)。业务语义(优雅降级、哨兵值、schema 约束)散落在 AGENTS.md 与个别注释中,IDE IntelliSense 无法提示。本次统一补齐 XML 注释并让编译器强制"公共 API 面必注释"。

## What Changes

- 主项目 csproj 开启 `GenerateDocumentationFile`(产出 XML 文档,供 IntelliSense 与未来工具消费);**不加** `CS1591` 到 NoWarn
- 清零全部 CS1591 警告(实测开启后 1366 条),让 `dotnet build` 输出无新增警告
- 注释语言统一为**中文**:补齐 Services/、Data/、Requests/、Models/、Common/、ViewModels/ 的类型与方法/成员级 `<summary>`;把 `ClusterPageQuery.cs`、`ClusterQueryRequest.cs` 及 3 个 .razor 中的英文注释翻成中文(语义不变)
- 豁免范围(明确不加):测试项目(不开启文档生成)、全部 .razor 的 @code 块(Razor 源生成器已自行抑制 CS1591,实测 0 条)
- 更新 AGENTS.md 与 openspec specs 记录注释约定

## Capabilities

### New Capabilities

(无)

### Modified Capabilities

- `code-style`: 新增 XML 注释约定要求(范围、语言、风格、CS1591 清零口径)

## Impact

- `MultiClusterMgmtSys/MultiClusterMgmtSys.csproj`(新增 GenerateDocumentationFile)
- 主项目全部公共类型/成员(Services 226 点、Requests 338 点、ViewModels 486 点、Data 174 点、Common 88 点、Models 42 点、Components/Common 12 点,合计约 1366 条警告对应注释点)
- 构建/测试不受影响(仅注释与 csproj 开关);`dotnet build` 0 错误、CS1591 清零,`dotnet test` 全绿
- 无数据库/接口行为变更,无 **BREAKING**

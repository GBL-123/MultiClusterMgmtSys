## Why

项目风格不统一:12 个 Razor 文件在 `@code` 里用 `[Inject]`(部分还混用顶部 `@inject`),`[Parameter]` 注解有单行/多行两种写法;后端 30 个 .cs 文件共 139 处成员间缺空行(字段/属性连排,ViewModels 最严重)。代码风格统一能降低阅读与审查成本,并防止后续改动继续发散。

## What Changes

- **Razor 注入统一**:所有组件将 `[Inject]` 从 `@code` 迁移到页面顶部 `@inject`(紧随 `@page`/`@attribute`/`@using` 之后),无 `[Inject]` 残留;顺带修复迁移处的缩进错乱。
- **Razor 属性格式统一**:`[Parameter]`/`[CascadingParameter]` 注解单独一行,属性声明独立一行,属性之间空一行;方法之间空一行。
- **C# 成员间隔统一**:所有 .cs 类(Components/Services/Data/ViewModels/Requests/Models/Common)的成员(字段/属性/方法)之间一律空一行。
- **AGENTS.md 新增 Code style 节**:记录上述约定,防止回归。

## Capabilities

### New Capabilities

- `code-style`: Razor 注入/参数注解/成员间隔与 C# 成员间隔的代码风格约定。

### Modified Capabilities

<!-- 纯格式重构,不改变任何功能规格 -->

## Impact

- `Components/**` 全部 .razor(~40 个文件):`@inject` 顶部化、参数注解格式化、成员空行
- `Services/**`、`Data/**`、`ViewModels/**`、`Requests/**`、`Models/**`、`Common/**` 全部 .cs(~40 个文件):成员间空行
- `AGENTS.md`:新增 Code style 节
- 无行为变更;验证手段 = `dotnet build` 0 错误 + 脚本化审计(连续成员行扫描)零命中
# code-style

## Purpose

Define the unified code-style contract for Razor and C# sources: dependency injection declared at the top of `.razor` files, parameter annotations and member declarations on their own lines with blank-line separation, blank lines between all C# class members, and these conventions documented in `AGENTS.md` for future sessions.

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
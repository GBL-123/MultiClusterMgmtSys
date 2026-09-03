## Context

现状:12 个 razor 用 `[Inject]`(ConfigMapDetail.razor 等还有顶格缩进错乱),`[Parameter]` 单行/多行混用;30 个 .cs 共 139 处连续成员行(字段/属性)无空行,集中在 ViewModels(~100 处)与 Services(~24 处)。无测试项目,验证靠 build + 静态审计。用户已定:全项目范围、手工逐文件修改、所有成员间空行、AGENTS.md 同步。

## Goals / Non-Goals

**Goals:**
- razor:`@inject` 全部顶部化;`[Parameter]`/`[CascadingParameter]` 注解独立行;成员间空行
- C#:字段/属性/方法间一律空行
- AGENTS.md 固化风格约定

**Non-Goals:**
- 不改命名、不改缩进宽度(沿用 4 空格)、不改组件结构/逻辑
- 不做字段级语义调整、不引入格式化工具/CI 校验

## Decisions

**D1. Razor 注入格式(目标形态)**
```
@page "/clusters"
@attribute [Authorize]
@using ...

@inject ClusterService ClusterService
@inject ISnackbar Snackbar
```
`@inject` 紧跟 `@using` 块之后、标记区之前;`@code` 中不再出现 `[Inject]`。迁移时同名属性直接删除 `[Inject]` 行;若页面已有顶部 `@inject` 同名服务,合并去重。

**D2. Razor 参数注解格式**
```
@code {
    [Parameter]
    public int Id { get; set; }

    [Parameter]
    public string Name { get; set; } = "";

    private bool loading;
}
```
单行 `[Parameter] public X Y` 拆为两行;注解与声明之间不加空行(注解贴属性);属性之间、方法与属性之间空一行。字段(无注解)同样遵守"成员间空行"。

**D3. C# 成员间隔**
字段/属性/方法声明之间一律空一行;主构造函数参数列表保持现状;`public class X(...)` 后的第一行成员不强制前置空行(类声明紧邻首成员可保留),但连续成员之间必须空行。枚举成员、`record` 单行声明、主构造函数体(`...);`)不拆分。

**D4. 修改顺序与验证**
1. 先改 C#(Services → ViewModels → Data/Requests/Models/Common)→ 每批后 `dotnet build`
2. 再改 razor(先 `[Inject]` 迁移 → 再参数注解与空行)→ build
3. 静态审计脚本扫描"连续成员行"模式,期望零命中(除类首行成员)
4. 最后更新 AGENTS.md 并全文抽查 5-8 个文件人工目检

**D5. 审计脚本口径**
沿用探索阶段验证过的正则:成员声明行(public/private/internal/protected + readonly/static + 类型 + 名称 + `=|\{|get`),仅当**相邻两行都命中**且非类声明时标记为"缺空行"。允许的例外:类声明后的首个成员(紧邻 `{`)。审计结果人工复核。

## Risks / Trade-offs

- [手工编辑量大(~180 处)遗漏] → 分文件组逐批 + build + 审计脚本零命中兜底;遗漏可被审计脚本抓住
- [razor 迁移误删/错位 @inject 导致编译错] → 每批立即 build;`[Inject]` 迁移仅 12 个文件,逐个目检
- [正则误报(方法内局部变量连排被当成员)] → 审计脚本仅报告,人工确认;局部变量通常是 `var`,不在成员正则内
- [风格约定再发散] → AGENTS.md 记录 + 未来 apply 会话可参照

## Migration Plan

- 纯格式,无数据/部署迁移;回滚 = git revert
- 提交一次,提交信息按仓库风格(短中文)

## Open Questions

- 无阻塞项。
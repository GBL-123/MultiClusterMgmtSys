## Why

旧单测套件(129 tests,含全部服务/组件测试)已在 `f216acd 删除UT` 整体移除,当前 `MultiClusterMgmtSys.Tests` 只有一个占位测试——业务代码处于零单测覆盖状态。期间项目新增了 `WorkloadService`(579 行,四大工作负载 CRUD)、`ClusterSync*`(定时同步)等大块逻辑,更需要测试保护。本 change 从零重建测试套件,并以 75% 行覆盖率(明确定义的排除口径)作为可验证的完成标准。

## What Changes

- **重建测试项目基础设施**:`MultiClusterMgmtSys.Tests` 在现有 MTP 骨架(xunit.v3.mtp-v2 4.0.0 + bUnit 2.9.0 + Moq + EF SQLite + `Microsoft.Testing.Extensions.CodeCoverage` 18.11.0 + ReportGenerator 5.5.11 包形态)上补齐 csproj 缺项(`FrameworkReference Microsoft.AspNetCore.App`、主项目 `ProjectReference`)与 `TestInfrastructure`(SQLite 内存库工厂、种子数据、K8s mock 工厂、bUnit host)。
- **新建测试三层**:① 逻辑核心(8 个服务 + 4 个仓库 + 6 个 Mapping 扩展 + Common:K8sExceptionMapper/ExceptionPresenter/ThemeManager/哨兵);② 共享 razor 组件(各 feature 的 tables/cards/filterbars/toolbars);③ 页面壳与分支变体(tab 面板、YAML 卡片、对话框交互、门控两态、badge 三态)。
- **覆盖率工具链定型**:`dotnet test MultiClusterMgmtSys.Tests --coverage --coverage-output-format cobertura` → ReportGenerator(测试 csproj 内 PackageReference 形态)转 HTML 报告。
- **覆盖率排除口径**:生产代码 4 处加 `[ExcludeFromCodeCoverage]`——`Program`、`Services/Identity/IdentityComponentsEndpointRouteBuilderExtensions`、`App.razor`、`Routes.razor`(`ChineseIdentityErrorDescriber` 不排除,纳入测试)。
- **仓库卫生**:`.gitignore` 补 `TestResults/` 与 `coveragereport/`;AGENTS.md 刷新测试章节(项目名保持 `.Tests`、MTP 命令、覆盖率口径与命令)。
- **UI 测试断言边界**(硬规则):bUnit 只测接线契约——通过公开参数/事件驱动 MudBlazor 组件实例,断言自有状态/ViewModel/自有 CSS 类;**禁止断言 `.mud-*` 内部 DOM 与 MudBlazor 渲染结果**(判定标准:替换 MudBlazor 后测试应照常通过)。
- 旧套件代码不搬迁,仅作参考(`git show f216acd^:MultiClusterMgmtSys.Tests/...` 可查 K8s mock 惯例)。

## Capabilities

### New Capabilities

(无)

### Modified Capabilities

- `unit-testing`:测试项目迁移到 MTP 运行器(替代 VSTest bridge,名称保持 `MultiClusterMgmtSys.Tests`);新增 75% 行覆盖率目标、排除口径、覆盖率工具链(cobertura + ReportGenerator)与 Infra 要求;测试范围扩至 Workloads/ClusterSync/Nodes 等新功能面;维持并强化既有服务异常契约、查询契约、bUnit 接线契约要求。

## Impact

- **测试项目**:`MultiClusterMgmtSys.Tests/`(csproj 已就位,新建全部测试与 infra 代码)。
- **生产代码(极小侵入)**:4 个文件加 `[ExcludeFromCodeCoverage]` attribute,不改变任何运行时行为。
- **仓库文件**:`.gitignore`(+2 行)、`AGENTS.md`(测试章节刷新)。
- **依赖**:测试 csproj 缺 `FrameworkReference` 与主项目 `ProjectReference`(实施时补);`Microsoft.Testing.Extensions.CodeCoverage` 18.11.0、ReportGenerator 5.5.11 已就位。
- **无 API/数据库/Docker 变更**;主项目行为零改动。
- 工作量重心:M3 阶段约 87 个 razor 组件在分母内,页面级 bUnit 测试是冲覆盖率的主力(渲染页面连带覆盖子组件 markup)。

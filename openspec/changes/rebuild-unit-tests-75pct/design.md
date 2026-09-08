## Context

- 旧 129-test 套件已于 `f216acd` 整体删除(git 历史仍可查:`git show f216acd^:MultiClusterMgmtSys.Tests/...`),决定**全新重写**而非复活。
- 现状:`MultiClusterMgmtSys.Tests/`(net10.0,MTP 骨架)含占位测试;csproj 已含 bUnit 2.9.0 / Moq 4.20.72 / EF SQLite 10.0.11 / `xunit.v3.mtp-v2` 4.0.0 / `Microsoft.Testing.Extensions.CodeCoverage` 18.11.0 / ReportGenerator 5.5.11(包形态);**缺** `FrameworkReference Microsoft.AspNetCore.App` 与主项目 `ProjectReference`。
- 覆盖工具链已冒烟验证:`dotnet test MultiClusterMgmtSys.Tests --coverage --coverage-output-format cobertura` → `TestResults\<guid>.cobertura.xml`(落在解决方案根目录);`dotnet <NuGet缓存>\reportgenerator\5.5.11\tools\net10.0\ReportGenerator.dll -reports:... -targetdir:coveragereport -reporttypes:Html` → HTML 报告。
- dotnet-coverage 为**动态仪器化**:模块被加载时才插桩,分母随真实测试落地逐步显现;主程序集在首个真实测试落地前不出现在报告里。
- 主程序集规模(排除 Program/Identity/App/Routes 前):C# ~3900 行 + razor ~7100 行(87 个组件);razor 是分母大头(~60%),页面渲染连带覆盖子组件 markup 是覆盖率杠杆。

## Goals / Non-Goals

**Goals:**
- 行覆盖率 ≥75%(口径:排除 `Program`、`IdentityComponentsEndpointRouteBuilderExtensions`、`App.razor`、`Routes.razor` 后的全部代码)。
- 测试覆盖三层:逻辑核心(服务/仓库/映射/Common)、共享 razor 组件、页面壳与分支变体。
- 覆盖率报告 HTML 化,能下钻到行级未覆盖明细,指导后续补测。
- AGENTS.md 测试章节与 `.gitignore` 随 change 修正。

**Non-Goals:**
- 不做 CI 门禁(75% 为一次性验收目标,不配置阈值门禁;`--coverage-settings` 精细过滤留待确有需要时再加)。
- 不测 `Program.cs` 启动流程、Identity 脚手架端点、`ReconnectModal` JS 契约。
- 不追求分支覆盖率指标(行覆盖为主;分支覆盖作为测试设计时的自然副产品)。
- 不搬迁旧套件代码;不引入集成测试/E2E。

## Decisions

- **D1 运行器:保持 MTP(xunit.v3.mtp-v2 + `UseMicrosoftTestingPlatformRunner`),不回退 VSTest bridge**
  - 依据根 `global.json` `test.runner: Microsoft.Testing.Platform`;禁令(Microsoft.NET.Test.Sdk / xunit.runner.visualstudio / coverlet)不变。
  - MTP gotchas 沿用:不传 `--nologo`(exit 5);零执行测试 = exit 8;过滤用 `--filter-class`/`--filter-trait`。
- **D2 覆盖率收集:`Microsoft.Testing.Extensions.CodeCoverage`(dotnet-coverage 内核),不用 coverlet**
  - 已冒烟通过;报告落 `TestResults/`(解决方案根)。空报告 ≠ 工具故障(动态仪器化,主程序集未加载)。
- **D3 HTML 报告:ReportGenerator 以 PackageReference(`ReportGenerator` 5.5.11)形态随 csproj 走**,调用 `dotnet $(PkgReportGenerator)\tools\net10.0\ReportGenerator.dll`(NuGet 自动属性消硬编码路径)。
  - 备选(否决):全局工具(版本跟机器走)、tool manifest(多一份文件)。`AfterTargets="Test"` 在 MTP 下是否自动触发需实施时实测;不通则保持两条手工命令。
- **D4 覆盖率排除:`[ExcludeFromCodeCoverage]` 打在生产文件上**(Program / Identity 扩展 / App.razor / Routes.razor),不用 runsettings 模块过滤。
  - 理由:显式、随代码走、评审可见;代价是生产文件 4 处小侵入(仅 attribute,零行为变更)。
- **D5 测试基础设施五件套(重建)**:`SqliteDbFactory`(每测试独立内存库,生产 provider)、`SeedUser`(Identity 用户/角色)、`TestData`(实体/VM 构造器)、`K8sMocks`(Moq `IKubernetes` 的 `*WithHttpMessagesAsync` + 惰性 `ThrowingFactory`)、`BunitHost`(MudServices + `TimeProvider.System` + `JSRuntimeMode.Loose` + `AddAuthorization`)。
  - 旧套件同名件可当参考但代码重写;目录镜像主项目(`Services/`、`Data/`、`Components/` 等)。
- **D6 K8s mock 惯例**:mock 接口 `*WithHttpMessagesAsync` 底层方法(扩展方法不可 mock);异常用 `KubernetesException(new V1Status { Code = ... })` 构造,验证 `K8sExceptionMapper.Translate` 翻译链路(注意:KubernetesClient 19 无状态码属性,状态码在 `Status.Code`)。
- **D7 bUnit 断言边界(硬规则)**:只测接线契约——`FindComponent<T>()` 取 MudBlazor 组件**实例**,经公开参数/事件驱动;断言自有状态/ViewModel/自有 CSS 类(`.status-badge`/`.empty-state` 等);**禁止断言 `.mud-*` 内部 DOM**(判定标准:换掉 MudBlazor 测试应照常通过)。bUnit 2.x API:`BunitContext`、`Render<T>()`、`AddAuthorization()`;创建过 MudBlazor 组件的 ctx 用 `await using` 释放。
- **D8 三阶段实施(M1 逻辑核心 → M2 共享 razor → M3 页面分支)**,每阶段以覆盖率数字校准(M1 后得真实基线,轨迹预估 ±20% 内收敛);razor 覆盖靠"页面渲染连带子组件",分支热点(badge 三态、empty/loading、Admin 门控两态、tab 面板、YAML view/edit、rollout 状态、查询哨兵)显式列变体。

## Risks / Trade-offs

- [razor 占分母 ~60%,页面/组件测试量大] → 优先渲染薄壳页面(连带覆盖)与共享 view;M1 后用真实数字重新校准 M2/M3 范围,允许范围内调整优先级。
- [`AfterTargets="Test"` 在 MTP 下不触发] → 降级为手工两条命令(文档化);不阻塞验收。
- [MTP + bUnit 2.9 组合的 gotcha(KeyInterceptor/IAsyncDisposable 等)散落在各测试] → `BunitHost` 统一封装 `await using` 模式,规避同步 Dispose 崩溃;测试方法一律 `async Task`。
- [sqlite 内存库 + Identity 的密码策略(长度 8 + 数字)] → `SeedUser` 统一生成合规密码,不散落字面量。
- [覆盖率数字依赖口徘认知] → 排除清单写进 spec 与 AGENTS.md;验收以 cobertura 数字 + 排除后口径为准。
- [生产代码 4 处 attribute 属于 prod 侵入] → 评审点;仅 attribute,零行为变更,`dotnet build` 全绿为证。

## Open Questions

- `$(PkgReportGenerator)` 自动属性在 `Microsoft.NET.Sdk`(非 Web SDK)+ PackageReference(非 tool)形态下是否一定可用——实施时验证,否则退化到缓存路径写死(`5.5.11` 已 pin,可接受)。
- `Profile.razor` 等重度依赖 `AuthenticationStateProvider` 的页面在 bUnit 下的注入方式(旧套件有先例思路,重写时按 BunitHost + `AddAuthorization` 组合实测)。

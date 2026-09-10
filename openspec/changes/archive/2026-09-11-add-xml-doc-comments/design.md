## Context

- 单解决方案两个项目:主项目(.cs 114 个 / .razor 98 个)+ 测试项目(xunit.v3)。当前无 `GenerateDocumentationFile`、无 doc 工具。
- 已有 `///` 注释约 99 行,中文为主,风格:`/// ` 带空格 + `<summary>` + `<see cref>`;`ClusterPageQuery.cs`/`ClusterQueryRequest.cs`/3 个 .razor 有英文契约注释(OpenSpec `cluster-query-layering` 锚点)。
- `WarningsAsErrors` 仅 `MUD0002`。临时开启文档生成实测:CS1591 共 **1366 条**,按目录 ViewModels 486 / Requests 338 / Services 226 / Data 174 / Common 88 / Models 42 / Components 12;**.razor 生成类 0 条**(生成代码已含 pragma 抑制)。

## Goals / Non-Goals

**Goals:**

- 主项目开启 `GenerateDocumentationFile`,构建输出 CS1591 清零(不进 NoWarn)
- 全部公共类型/成员补中文 `<summary>`;英文注释统一翻中文
- 约定沉淀到 AGENTS.md + openspec `code-style` spec

**Non-Goals:**

- 不给测试项目加注释、不开启其文档生成
- 不给 .razor @code 块新增注释(仅翻译既有英文注释)
- 不引入 docfx/Sandcastle 等文档站点
- 不改变任何代码行为/公共签名(纯注释 + csproj 开关)

## Decisions

1. **开启 `GenerateDocumentationFile` 且 CS1591 清零而非 NoWarn**:注释强制覆盖可防未来新增公共成员漏注释;IDE IntelliSense 与未来工具可消费 XML 文件。备选"NoWarn 静默"被用户否决(失去强制力)。
2. **CS1591 保持警告级(不加入 WarningsAsErrors)**:全量注释落地后输出即清零;若未来想强制,可另行决策升级。降低一次性引入风险。
3. **豁免口径**:测试项目无外部 API 面;.razor 生成类实测 0 条警告(源生成器自行抑制),无需处理——"排除 Razor"与"清零"不冲突。
4. **英文契约注释翻中文**:仅翻 `ClusterPageQuery.cs`、`ClusterQueryRequest.cs`、`ClusterTable.razor`、`ConfigMaps.razor`、`Nodes.razor` 既有注释,语义不变;OpenSpec spec 描述的是行为契约而非注释文本,翻译不构成 spec 漂移(实施后抽查 spec 对照)。
5. **注释风格延续现状**:中文 `<summary>`,`/// ` 带空格,必要处用 `<param>`/`<returns>`/`<paramref>`/`<see cref>`;枚举每个成员单独注释;不做逐属性机械流水账——按"读者需要知道什么"写。

## Risks / Trade-offs

- [注释量极大(约 1366 点),机械批处理易产生套话/噪音] → 分目录分批实施,每批 `git diff` 抽查语义质量;模糊类型(如纯数据 ViewModel 属性)只做类型级 summary + 关键字段注释,不强求逐属性
- [遗漏个别公共成员导致警告残留] → 以构建输出 CS1591=0 为验收口径,逐条清零,不做模糊豁免
- [英文注释翻译引发 spec 对照疑虑] → 实施后对照 `openspec/specs/cluster-query-layering/spec.md` 抽查语义一致
- [XML 文档文件出现在构建输出] → 无外部消费者,仅 IntelliSense 使用;如未来需要可挂 docfx

## Migration Plan

分 5 批落地(每批后 `dotnet build` 验证):①csproj 开关 + Common/Models/Components 查缺补漏 + 英文中文化 → ②Services → ③Data → ④Requests → ⑤ViewModels,最后全量验证(build 0 错误、CS1591=0、`dotnet test` 全绿)并更新 AGENTS.md + sync specs。回滚 = 还原 csproj 开关 + git 还原注释改动,无数据/接口影响。

## Open Questions

(无——豁免口径、语言、清零标准均由用户拍板)

## 1. 视图模型与服务映射

- [x] 1.1 新增 `ViewModels/NodeResourceViewModel.cs`(Key / Label / CapacityRaw / CapacityText / AllocatableRaw / AllocatableText / AllocatablePercent,中文 XML 注释),并把 `ClusterNodeDetailViewModel` 的 `Capacity` / `Allocatable` 字典替换为 `List<NodeResourceViewModel> Resources`;`dotnet build` 失败清单仅剩已知的 UI/测试消费点
- [x] 1.2 在 `ClusterNodeService` 实现资源合并、固定排序、按 key 语义换算(cpu→核、memory/ephemeral-storage/hugepages-*→IEC、pods→个、未知键原样)、占比计算(缺失/0→null),更新 `MapNodeDetail`;`ClusterNodeServiceTests` 断言 `4 核` / `3.8 核`、Ki→GiB、未知键、缺失侧 `—`、`95%` 与排序 — `dotnet test` 相关用例通过

## 2. 资源容量卡片重写

- [x] 2.1 重写 `NodeResourcesCard.razor`:统一 `MudTable`(资源 / 容量 / 可分配 / 可分配占比),已知资源中文标签 + 原始 key 次要 mono,值带原始串 `title`,`MudProgressLinear` + 百分比,容量缺失或 0 显示 `—`;bUnit 断言 `4 核`、`15.5 GiB`、`95%`、`—`
- [x] 2.2 补齐边界测试:空资源显示 `—` 空态、可分配独有键渲染且容量列 `—`、不可解析值回退原始串 — 对应 bUnit / 服务测试通过

## 3. 标签与注解纵向堆叠

- [x] 3.1 `NodeDetail.razor` 的「标签与注解」tab 移除 `MudGrid` 半宽并排,改为 `MudStack Spacing="3"` 全宽纵向堆叠;页面/卡片测试断言两卡均渲染、标签卡在注解卡之前

## 4. 验证

- [x] 4.1 `dotnet build MultiClusterMgmtSys.slnx` 0 错误、`dotnet test MultiClusterMgmtSys.Tests` 全绿,并确认 CS1591 与“连续成员行”静态审计零命中
- [ ] 4.2 `dotnet run --project MultiClusterMgmtSys` 打开节点详情页,人工确认资源容量换算可读、占比条正常、标签与注解纵向全宽

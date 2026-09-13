## Why

节点详情页「资源容量」tab 直接展示 Kubernetes 原始 quantity 字符串(如 `3800m`、`16297496Ki`),容量与可分配又分作两张并排表,用户需要心算换算并跨表对照;「标签与注解」tab 的两张卡片半宽并排,长值被截断。目标是让这两块信息一眼读懂。

## What Changes

- **资源容量统一表**:`NodeResourcesCard` 由「Capacity / Allocatable 两张并排表」改为单张统一表格,每个资源一行,列为 `资源 / 容量 / 可分配 / 可分配占比`;容量与可分配并排对比,占比以进度条 + 百分比呈现。
- **数值人类可读化**:cpu 显示为「核」(如 `3800m` → `3.8 核`),memory / ephemeral-storage / 大页等按 IEC 单位显示(如 `16297496Ki` → `15.5 GiB`),pods 显示为「个」;原始 quantity 保留在 `title` tooltip 中。已知资源显示中文名(CPU / 内存 / 临时存储 / Pod / 大页),未知资源回退原始 key。
- **视图模型调整**:`ClusterNodeDetailViewModel` 用 `List<NodeResourceViewModel>`(格式化值 + 原始值 + 占比)替换 `Capacity` / `Allocatable` 两个原始字符串字典;`ClusterNodeService` 负责换算与合并。
- **标签与注解纵向堆叠**:「标签与注解」tab 内 `NodeLabelsCard` 与 `NodeAnnotationsCard` 由并排(`xs=12 md=6`)改为纵向堆叠、全宽展示。

## Capabilities

### New Capabilities

(无)

### Modified Capabilities

- `node-detail-layout`: 五 tab 组合中「标签与注解」由并排改为纵向堆叠;新增「资源容量 tab 以统一表格展示人类可读的资源容量与可分配量」要求。

## Impact

- UI:`Components/Nodes/Shared/NodeResourcesCard.razor`(重写)、`Components/Nodes/Pages/NodeDetail.razor`(布局调整)。
- 视图模型/服务:`ViewModels/ClusterNodeDetailViewModel.cs`、新增 `ViewModels/NodeResourceViewModel.cs`、`Services/ClusterNodeService.cs`(`NodeResourcesCard` 是该数据的唯一消费者)。
- 测试:`MultiClusterMgmtSys.Tests/Components/Nodes/NodeListTableTests.cs`、`MultiClusterMgmtSys.Tests/Services/ClusterNodeServiceTests.cs`、`MultiClusterMgmtSys.Tests/Components/Pages/NodeConfigMapsPageTests.cs` 中构造数据的部分。
- 规格:`openspec/specs/node-detail-layout/spec.md`。

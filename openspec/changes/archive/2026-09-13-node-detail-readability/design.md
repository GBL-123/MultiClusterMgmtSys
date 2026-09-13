## Context

参见 `proposal.md` — Why。

现状约束:

- `ClusterNodeDetailViewModel` 目前以 `Dictionary<string,string>` 承载容量/可分配,`ClusterNodeService.MapNodeDetail` 直接写入 `ResourceQuantity.ToString()` 原始串;`NodeResourcesCard` 是这两个字典的唯一消费者。
- `NodeResourcesCard` 用两张并排 `MudTable`(Capacity / Allocatable)展示,无法体现两值关系,也不做单位换算。
- 页面的「标签与注解」tab 在 `NodeDetail.razor` 中用 `MudGrid` 把两张卡按 `xs=12 md=6` 并排。
- 项目约定:服务输出 ViewModel(不返回实体);UI 中文;数值列用 `.font-mono`;进度类视觉使用主题 amber(`MudProgressLinear` 默认 Primary 即 amber);服务层格式化逻辑应可经服务边界测试。

## Goals / Non-Goals

**Goals:**

- 资源容量一屏可读:统一行、并列容量/可分配、人类可读单位、占比可视化。
- 换算与合并逻辑集中在服务层,卡片保持纯展示,bUnit 只测接线契约。
- 标签/注解卡片纵向堆叠、全宽。

**Non-Goals:**

- 不引入实时使用率(metrics-server)数据 —— 只展示 capacity/allocatable 静态关系。
- 不改动标签/注解值的截断行为与表格结构。
- 不改动节点列表或其他页面的任何展示。

## Decisions

### 1. 服务层产出结构化资源行,替换原始字符串字典

新增 `NodeResourceViewModel`(`Key` / `Label` / `CapacityRaw` / `CapacityText` / `AllocatableRaw` / `AllocatableText` / `AllocatablePercent`),`ClusterNodeDetailViewModel.Resources` 以 `List<NodeResourceViewModel>` 取代 `Capacity` / `Allocatable`。合并键集 = capacity ∪ allocatable;缺失侧文本为 `—`。

理由:占比计算与单位换算需要数值语义,且 tooltip 需要原始串;在 `ClusterNodeService` 中一次算好,卡片只绑定,服务测试可直接断言换算结果。备选「卡片内解析 quantity 字符串」被否:把 k8s quantity 解析和除法搬进 Razor,测试只能走 bUnit 且逻辑分散。

### 2. 换算规则按资源键语义分派

- `cpu`:`ResourceQuantity.Value`(核)按 `0.###` 格式化为 `3.8 核` / `4 核`。
- `memory`、`ephemeral-storage`、`hugepages-*`:`Value`(字节)按 1024 进制选 B/KiB/MiB/GiB/TiB/PiB,保留至多一位小数(`16297496Ki` → `16688635904` B → `15.5 GiB`)。
- `pods`:`{value:0} 个`。
- 其他键:不猜测语义,原样显示 `ToString()` 值。

`Value` 为 null(不可解析)时回退原始串。数字格式化使用 `CultureInfo.InvariantCulture`(小数点固定为 `.`)。备选「按后缀统一换算」被否:扩展资源后缀语义不保证。

### 3. 占比计算与展示

`AllocatablePercent = allocatable / capacity * 100`,取一位小数,clamp 到 `[0,100]`;容量缺失/不可解析/为 0 或可分配不可解析时为 `null`。UI 用 `MudProgressLinear`(`Value` = percent)配 `.font-mono` 百分比文本;为 null 时该列显示 `—`。

### 4. 行排序与中文标签

顺序:已知键固定序 `cpu` → `memory` → `ephemeral-storage` → `pods` → `hugepages-*`(按 key 排序),未知键按 key 排序殿后。`Label` 为中文名;`Label != Key` 时卡片在其下方以 `.font-mono` 次要色显示原始 key。

### 5. 标签/注解改为纵向堆叠

`NodeDetail.razor` 中移除 `MudGrid`/`MudItem`,改为 `MudStack Spacing="3"` 顺序放置两张卡(全宽)。两张卡片组件本身不改。

### 6. 测试与文档

- 服务测试:断言 cpu 核换算、IEC 换算、占比、缺失侧、排序、未知键回退、空集。
- bUnit:`NodeResourcesCard` 断言统一表列文本(`4 核`、`15.5 GiB`、占比文本)与空态;`NodeDetail` 接线保持既有 tab 断言。
- 新增公共类型按项目规范补中文 XML 注释(CS1591 清零)。

## Risks / Trade-offs

- [VM 形状破坏性变更] → 已确认 `Capacity`/`Allocatable` 仅被 `NodeResourcesCard` 与测试消费,同步更新即可;不保留旧属性避免双份真相。
- [字符串解析误判] → 仅对已知键做语义换算,未知键原样透传;`Value` 不可解析时回退,不抛异常。
- [极长 `hugepages-*` 名称观感] → 原始 key 用次要小字展示,不占主视觉。

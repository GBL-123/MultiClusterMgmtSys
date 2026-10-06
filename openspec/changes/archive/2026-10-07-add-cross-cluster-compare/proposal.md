# Proposal

## Why

系统名叫多集群管理,但至今没有任何「跨集群视角」的功能——想把 staging 集群的某个 Deployment 带到 prod,只能人肉导出 YAML 再到目标集群手工建一个,既看不到两边的真实差异,也绕开了系统内的归属盖章与审计。跨集群 YAML 对照 + 一键克隆把「多」这个字补成可用能力,并让迁移天然继承既有创建路径的全部安全语义。

## What Changes

- 新增「跨集群对照」页面:选源集群、对照集群、资源族(工作负载四类 + ConfigMap)与命名空间后,双栏展示同名资源两侧的原始 YAML,逐行差异高亮;任一侧资源不存在时明确标注。
- 一键克隆:把源侧对象的 YAML(剥离服务端身份元数据:uid/resourceVersion/creationTimestamp/status/mcms 归属标签)导入对照集群,复用各资源既有创建服务——归属盖章、命名空间黑名单、Admin/Member 权限、审计随之继承。
- 目标集群已存在同名资源时给出中文冲突提示,不做覆盖式更新(v1 只允许「创建」,不允许「同步差异」)。
- 侧边导航新增「跨集群对照」独立入口。

## Capabilities

### New Capabilities

- `cross-cluster-compare`:跨集群资源对照与克隆——页面与资源范围、双栏 YAML 差异标注、克隆语义(元数据剥离/冲突拒绝/走既有创建路径)、可见范围。

### Modified Capabilities

(无——克隆的归属盖章、黑名单、审计语义由既有 `workload-management`/`configmaps-page`/`k8s-resource-ownership` 的 requirement 承接,本 change 不改它们;侧导航入口作为新能力页面自身的组成部分写入新契约。)

## Impact

- `Application/Services/ClusterCompareService.cs`(新):按族调度既有 `WorkloadService`/`ConfigMapService` 读 YAML 与创建;新增对照专用 ViewModel(`ComparePairViewModel`/`YamlDiffRowViewModel`)。
- `Web/Components/Compare/`(新目录,命名空间 `.Web.Components.Compare`):`/compare` 页面 + 差异渲染组件 + 克隆确认对话框;`app.css` 追加对照页样式节(diff 行高亮、双栏布局)。
- Drawer 新增 NavLink「跨集群对照」。
- 复用 `Components/Common/YamlDiff`(纯函数 LCS 行 diff,Web 层)。
- 测试:`ClusterCompareServiceTests`(YAML 元数据剥离/族调度/缺失标注/冲突翻译)、diff 组件与页面 bUnit、克隆走既有创建服务与归属盖章的服务级断言。

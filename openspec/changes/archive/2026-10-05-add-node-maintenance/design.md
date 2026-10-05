# Design

## Context

节点页已具备完整只读栈:`ClusterNodeService` 经 `IClusterClientCache` 取客户端、10s 统一超时、`K8sExceptionMapper` 翻译;`ClusterNodeViewModel` 已带 `Unschedulable` 投影(`MapNode` 读 `node.Spec.Unschedulable`),筛选栏也有可调度性下拉。缺的只是写路径与状态可见化。drain 涉及 policy/v1 Eviction API,这是本仓首次使用的 API 组,签名需用 `sigtool` 反查(k8s 19 的 `*WithHttpMessagesAsync` 参数顺序与直觉不同,照 K8sMocks 惯例先查再用)。

## Goals / Non-Goals

**Goals:**
- 服务层三方法(Cordon/Uncordon/Drain)全部经 `IClusterClientCache`,复用超时/异常翻译契约
- drain 的预检 → 确认 → 自动封锁 → 逐 Pod Eviction → 尽力迁移汇总,全链路可测(Moq mock Eviction)
- 节点页列表行与详情工具栏的 Admin 条件操作入口 + 「已封锁」徽标

**Non-Goals:**
- 节点污点(taints)编辑(独立 change 更干净)
- 排空的取消/回滚(保持「尽力迁移 + 汇报」语义)
- drain 等待 Pod 清零的轮询等待
- 节点级 `k8s-resource-ownership` 适配(节点是集群级资源,不适用)

## Decisions

1. **新服务方法挂在何处**:在 `ClusterNodeService` 内追加三个方法(它已有节点读取与上下文,且节点维护与节点读取天然同城),不为三操作单开 Service。备选:独立 `NodeMaintenanceService`——等操作族群变多(如 taints、节点标签编辑)再拆。
2. **Eviction 逐 Pod 串行,并行度 1**:drain 的语义是「滚动迁移」,PDB 依赖逐个腾挪;并发驱逐会放大 PDB 冲突与抖动。每个 Pod 的 Eviction 调用仍受 10s 超时契约约束。
3. **预检分类口径**:控制器 Pod = `OwnerReferences` 非空(含 Job/CronJob 派生);DaemonSet Pod = owner kind 为 `DaemonSet`;裸 Pod = `OwnerReferences` 为空。OwnerReference 仅看直接父级,不追溯祖父链(与 kubectl drain 的启发一致的第一近似)。
4. **PDB 429 的识别**:Eviction 调用抛出的异常中,`KubernetesException.Status.Code == 429`(Too Many Requests)按「被策略阻塞」 лечении;其余异常走 `K8sExceptionMapper.Translate` 正常翻译但同样计入累计,不中断循环。
5. **执行期间不跨请求后台化**:drain 在页面单次请求内完成(串行逐 Pod、数量级小),用与「刷新全部」相同的 `IProgress`/进度参数模式回报;不引入 Hangfire 式后台任务。备选:后台任务 + 轮询查询——超出第一版收益。
6. **确认对话框不经 MudForm 提交流**(bUnit 下不稳定,项目既有实践),排空确认走渲染断言 + 服务层覆盖;测试用 DOM `.Click()` + provider 冲刷。
7. **徽标词汇**:「已封锁」中文主行;`Unschedulable` 原值经现有 `TextTooltip`(Mono)展示,不新增强调色,用 `.status-badge` 现有 unknown 系淡彩。

## Risks / Trade-offs

- [Eviction API 签名反查错误] → 按 K8sMocks 惯例先 `sigtool` 反查 `CreateNamespacedEviction` 系列签名,mock setup 照抄段注释参数顺序
- [排空大节点(数百 Pod)页面请求超时] → 第一版接受:串行 10s/.Pod 上限实际受 K8s 侧驱逐排队主导;若真实场景超时,后续迭代再后台化(decision 5 已留口)
- [PDB 阻塞被误读为失败] → 汇总口径区分「阻塞」与「失败」,429 与其它异常分报
- [封锁/排空期间节点数据过期] → 操作完成后强制刷新节点详情/列表,以服务器视角为准
- [裸 Pod 分类误判(owner 名与 kind 手工资源)] → 文案已说明「不会自动重建」兜底风险;预检清单可见,误杀由确认环节拦截

## Migration Plan

无 schema 变更、无数据迁移;纯增量代码 + 三个枚举值(AuditAction 新增)。回滚 = 还原代码。

## Open Questions

无(裸 Pod force、异步后台化、taints 已在 Non-Goals 显式排除,均为可独立立项的后续方向)。

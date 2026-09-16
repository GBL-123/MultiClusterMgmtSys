# pod-management Design

## Context

模式高度同构,模板现成:`EventService`(只读、无审计、经 `IClusterClientCache` 取客户端)+ Events 页的「全量拉取 → 内存过滤 → FilterBar/Table 三件套 → 详情」骨架;工作负载四类的 ListView/DetailView 共享组件示范了详情页形态;`K8sDisplayText`(纯静态)是全站枚举双语映射登记处;display-conventions 契约约束徽章/tooltip/双语/mono 字体。既有基线 667 测试全绿。

## Goals / Non-Goals

**Goals:**

- 补齐 Pod 只读视角:列表(全量 + 内存过滤)、详情(基本信息 + 容器状态)、工作负载直达;
- 状态徽章语义把"藏在 Running 里的异常"(CrashLoopBackOff 等)可靠暴露;
- 全部沿用既有契约:统一 10s 超时(kubernetes-call-timeout)、客户端缓存(k8s-client-cache)、异常翻译(exception-handling)、双语展示(display-conventions)。

**Non-Goals:**

- 日志查看(后续 `pod-logs` change);
- Pod 写操作(删除/重建)、exec 终端、自动跟随轮询;
- K8s 原生分页与 field selector 服务端过滤(万级 pod 的后续路径);
- 修改任何既有 spec 的需求。

## Decisions

### D1: fetch-all + 服务侧轻投影

`ListPodsForAllNamespacesAsync` 一次性拉取后,service 立即投影为瘦 ViewModel,UI 与服务内存只见瘦对象。

- **为什么压倒 K8s 原生分页(limit/continue)**:Pod 的 field selector 只支持 `status.phase`/`spec.nodeName` 等,按名称/IP 的关键词搜索无法服务端化,分页会把搜索变成"只在已拉页里搜";而 fetch-all 与全站"一次拉全量、内存里过滤"的既有模式(事件/工作负载/ConfigMap 页)一致。
- **规模语义(诚实边界)**:Pod 是最重的 K8s 对象(约 10~20KB JSON/个)。≤ 数千 pod 时传输 10~40MB 内可接受(局域网 1~3s);万级 pod 会逼近 10s 超时与内存峰值。spec 已把"适用于数千 pod 量级"写为契约;若日后出现万级集群,演进路径 = 命名空间下拉默认收敛(field selector 兜底)→ 原生分页,届时再立 change。
- **轻投影的价值**:SignalR 与渲染永远只见当前页行,瘦模型把"全量"的成本限制在首字节传输。

### D2: 状态徽章 = 容器级信号优先,phase 兜底

判定顺序:任一容器 waiting/terminated 原因有值 → 主文案 = 该原因(未登记值回退原文),样式 offline;否则按 phase:Running→运行中(online)、Failed→失败(offline)、Succeeded→已完成(unknown 中性)、Pending/Unknown→unknown。理由:phase=Running 不代表健康——CrashLoopBackOff/OOMKilled 只在容器状态里;这是本功能最核心的排障语义,测试必须固化"Running + CrashLoopBackOff → 异常徽章"场景。重启次数独立成列,不参与徽章(重启次数高但当前 Running 正常的 Pod 不应吓用户)。

### D3: 详情页单页起步,不预置日志 tab

详情页呈现基本信息 + 容器状态卡;日志归 `pod-logs` change 再以 detail-page-tabs 模式加 tab。理由:避免本期预埋空 tab;detail-page-tabs 契约支持后续加页签,演进路径已验证(node-detail-yaml-tab)。

### D4: 工作负载直达 = label selector 复用列表服务

「查看 Pod」按 workload 的 `spec.selector.matchLabels` 构造 label selector,`ListNamespacedPodAsync(labelSelector)` 一参数即得,结果走同一投影与徽章语义;导航到 Pod 列表页带预置过滤(或直接列表接口),最小实现。

### D5: 只读、无审计

Pod 无任何写操作入口,不写审计(与 EventService 同款);访问仅要求登录。管理员与成员看到的只读视图一致(全站"查看类操作与角色无关"惯例)。

## Risks / Trade-offs

- **[R1] 大集群全量拉取传输/超时** → 规模语义已写入 spec;field selector 兜底与分页路径记录在 D1,超出量级时立新 change。
- **[R2] 容器信号优先的判定复杂度** → 集中在映射层(K8sDisplayText/投影扩展)实现并被测试固化,UI 不重复判定。
- **[R3] 未登记 waiting/terminated reason** → display-conventions 的"未登记回退原文"规则天然覆盖,信息不丢失。
- **[R4] 详情页打开已被删除的 Pod** → K8s 404 → 已有 NotFound 翻译,详情页按"未找到"空状态呈现(spec 已固化)。

## Migration Plan

纯新增能力,无 schema/数据迁移:服务 → 映射 → 列表页 → 详情页 → 直达链接 → 导航注册,每步可独立验证;回滚即 revert。

## Open Questions

- 工作负载直达在列表页的呈现形态(带预置过滤的导航 vs 直接结果列表)——实现期可选,不影响 spec 与任务拆分。

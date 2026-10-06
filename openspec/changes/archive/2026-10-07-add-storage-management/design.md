# Design

## Context

Secrets 域刚落地(add-secret-management 实施完成待归档),其 Service 形状、归属三件套、页面骨架、bUnit 栈全部可直接复制;KubernetesClient 19 的 PVC/PV/SC 方法签名与 YAML roundtrip 已实测确认(k8s-probe,日志 probe-pvc.log/probe-yaml2.log)。依赖方向遵循既有分层契约;K8s 调用一律经 `IClusterClientCache` 享受 10s 超时契约。

## Goals / Non-Goals

**Goals:**

- PVC 命名空间级全生命周期(列表/创建/详情/删除),归属与审计照 Secret 同款。
- PV 只读浏览 + 只读详情(镜像节点详情布局);SC 只读列表。
- 归档路径干净:`k8s-resource-ownership`/`audit-log` 两份 MODIFIED 与 Secrets 归档后的主 spec 无缝衔接。

**Non-Goals:**

- PVC/YAML 编辑页(spec 几乎不可变,仅容量扩展且需 SC 开 allowVolumeExpansion,编辑必然 409 类失败;容量扩展后续单独评估)。
- PV/StorageClass 任何写操作(回收、扩容、删除均系统外处理)。
- CSI/IOPS/吞吐/贴 token 之类高级字段视图(`nodeAffinity`、`mountOptions` 落 YAML 视图兜底)。

## Decisions

**D1 页面形状 = 三页三路由(而非单 storage 页内 tab)。** 与 ConfigMap/Secret/Helm 一页一域的既有布局一致;drawer 组内并列三个入口,URL 语义清晰(`/storage/claims`、`/storage/volumes`、`/storage/classes`)。备选「单页三 tab」被否:详情路由无法承载 tab 上下文,且列表页要复用 `{feature}-table` flex-fill 惯例。

**D2 Service 单类三态(`StorageService`,而非三个类)。** ctor 镜像 SecretService=(IClusterRepository, ResourceOwnershipGuard, AuditService, IClusterClientCache, ILogger);PVC 方法具备写语义(继承审计与归属),PV/SC 方法为纯读不写审计。备选「PV/SC 并为只读子服务」被否:多一层 DI 无收益。方法面:Claims = ListNamespaced/ForAllNamespaces(与 Secret 双口径同款) + Read + Create + Delete + ListNamespacedPod(挂载过滤);Volumes = ListPersistentVolume + ReadPersistentVolume;Classes = ListStorageClass(IStorageV1Operations)。

**D3 不做占位符 shim。** V1PersistentVolumeClaim/V1PersistentVolume/V1StorageClass 均无 byte[] 属性,KubernetesYaml 反序列化占位…不适用;PVC 无「密钥回显」问题,创建即提交 YAML,`ValidatePersistentVolumeClaim` 直接 `KubernetesYaml.Deserialize<V1PersistentVolumeClaim>`(IYamlValidator 新方法,沿用裸调用惯例)。

**D4 PVC 详情对非归属者可见(区别于 Secret)。** Secret 详情整体受限的理由是 base64 值可离线解码;PVC YAML 不含机密值,详情与挂载 Pod 对全员可见,仅删除按钮按 CanOperate 条件渲染。写路径仍服务层强制(RequireOperateAsync fail-closed)。

**D5 phase 展示走既有双层(`K8sDisplayText` + StackedText 状态徽章),不新增组件。** `Application/ViewModels/Mappings/K8sDisplayText.cs` 加 PvPhase/PvcPhase 条目(纯中文映射);排序/过滤提交用 raw 值(display-conventions 契约既有口径)。

**D6 归属 delta 基于「Secrets 归档后」的主 spec 起草。** 该变更的 `k8s-resource-ownership` MODIFIED 全文包含 Secret 场景(逐字取自 Secrets delta)+ 新增 PVC 场景;`audit-log` 同理(事件列表与枚举含密钥并新增存储)。否则 Secrets 先归档会把主 spec 更新到含 Secret 版,storage 再归档时会用旧版本文本回盖丢场景。

**D7 K8sMocks 签名差异点已探明:** `ListNamespacedPersistentVolumeClaim` pretty 在末尾(与 Secret 同款,≠ ConfigMap);`ListPersistentVolumeClaimForAllNamespaces` 与 `ListPersistentVolume` 的 pretty 在位置 6;`StorageV1` 走 `IStorageV1Operations` 独立接口(`ListStorageClass` 10 参,pretty 在末尾)。mock 段新增建 setup 时按此落位,并加 `IStorageV1Operations` mock 属性。

## Risks / Trade-offs

- [挂载 Pod 查询在 Pod 数量大的命名空间有延迟] → v1 直接拉全量 ListNamespacedPod 按 ClaimName 过滤(与事件页同量级),不单独优化;详情页加载态有指示。
- [phase 常量类不可反射读取] → 直接用字符串字面量("Bound" 等,probe 已确认值集),不引入枚举耦合。
- [StorageV1Operations 首次接入可能漏 mock] → K8sMocks 新增 Storage 段独立成块,列表调用走 `StorageV1.ListStorageClassWithHttpMessagesAsync`。
- [PVC 删除遇到 TerminationProtection 触发的对象被删缓慢] → 保持 kubectl 等价语义(delete 即返回),靠 UI 侧刷新观察 phase;不做等待循环。

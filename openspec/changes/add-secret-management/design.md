# Design

## Context

Secret 管理是 ConfigMap 功能的同构扩展,但多一个「值敏感」维度。现状:`ConfigMapService` 已落地完整的「归属盖章/黑名单/CanOperate 投影/审计」链路(契约 `k8s-resource-ownership`);`ClusterOverviewCard` 已有凭据「掩码揭示」模式(credential-redacted/viewer CSS + 揭示/隐藏按钮);`IYamlTemplateService` 从 `wwwroot/templates/{category}/{name}.yaml` 提供创建模板;审计枚举 `AuditCategory`/`AuditAction` 由代码枚举扩展(无 DB 迁移)。See proposal.md - Why。

## Goals / Non-Goals

**Goals**
- Secret 列表/详情/创建/编辑/删除全链路,权限模型与 ConfigMap 完全一致
- 值不可意外泄露:回显掩码、揭示按钮、揭示审计
- 防误清空:占位符提交语义

**Non-Goals**
- 不做 Secret 加密存储或后端级密钥管理(K8s etcd 加密由集群自身负责)
- 不做 secret 生成器/TLS 证书签发/docker-registry 凭据等专用表单(全部 YAML)
- 不做 ConfigMap↔Secret 互转、批量导入
- 卷管理(PV/PVC/StorageClass)留给后续独立 change

## Decisions

**D1 值可见性 = 掩码 + 逐 key 揭示(方案 B)**

- 详情页键值 tab 值一律掩码;每 key 「查看明文」按钮解码 base64 → UTF-8 显示(credential-redacted/viewer 模式复用,新增 CSS 变体 `.credential-redacted` 行内尺寸由设计稿定,不新造机制);揭示写审计(动作「查看明文」,目标含 key 名)
- YAML tab 显示原始 YAML(base64 原文),不审计——它等价于用户 `kubectl get secret -o yaml` 的自然视图,Admin/归属者本就可用 kubectl;审计聚焦「把明文暴露给用户界面」这一步
- 详情页内容对非 CanOperate 用户整体受限(连 YAML tab 的 base64 也不可见)——base64 可 2 秒离线解码,若对非归属者展示 YAML 原文则掩码形同虚设;列表行元数据(名称/命名空间/类型/键数/创建时间)对全员可见
- 备选:整体一个「查看凭据」按钮揭示全部 key——粒度太粗,揭示一个 key 的行为会留下全 secret 的审计痕迹;逐 key 更精确
- 备选:YAML tab 也掩码——用户在 YAML tab 的用途是查看结构/复制到创建表单,全部掩码会破坏可用性;base64 原文已不是明文

**D2 编辑占位符机制 `<REDACTED:KEYNAME>`(Secret 特有逻辑)**

- 服务层 `SecretService` 提供 EditYaml 拼装:读服务器对象 → `KubernetesYaml.Serialize` → 对 `data`/`stringData`/`binaryData` 每 key 值替换为 `<REDACTED:key>`(key 名含特殊字符时原样嵌入);`binaryData` 同样占位
- 提交解析:反序列化用户 YAML 后逐 map 处理——值 == 占位符 → 从服务器现值回填;否则用新值(缺 key = 删除);新增 key = 新值。服务器现值从编辑前 `ReadNamespacedSecret` 取,同一请求内完成(read→RequireOperateAsync→merge→replace)
- 占位符常量 `SecretRedaction.Placeholder(key)`(Application 层 internal 纯函数,可测);冲突场景:用户真实值恰好是 `<REDACTED:key>` 字符串——视为占位符保留现值,概率可忽略且 fail-safe(不会清空),在 design 记录为已知边界
- 备选:不占位、回显 base64 原文——编辑框里明文密码仍可被 base64 解码,且无法区分「用户没改」与「用户想清空」;备选:整表单逐 key 编辑——引入新交互形状,超出 YAML 既有模式

**D3 归属/黑名单/审计照抄 ConfigMap 口径,零新机制**

- `SecretService` ctor(`IClusterRepository, ResourceOwnershipGuard, AuditService, IClusterClientCache, ILogger`);Create = RequireCreator → 黑名单(`EnsureCreationTargetNamespaceAllowed`)→ `ResourceOwnershipStamp.Stamp`;Update/Delete/揭示 = read → `RequireOperateAsync` → 原逻辑;List/Detail 投影 `CanOperate`(复用 `ResourceOwnershipPolicy.CanOperateForIndex` 含 Helm 互通)
- Update 保服务器 metadata(含归属 label,防止用户 YAML 清归属自毁权限);仅合并 data/stringData/binaryData——与 ConfigMap 的「仅覆盖 data」一致,规避 SvcService 提交用户 metadata 的既有 minor
- catch when filter 仅限 k8s 异常族(`KubernetesException`/`HttpOperationException`/`TaskCanceledException`/`OperationCanceledException`/`HttpRequestException`),归属/业务异常不被 translate 吞
- 归属判定失败:LogWarning + `PermissionException`,不写审计(契约口径)

**D4 枚举扩展**

- `AuditCategory.Secret = 10`(显示「密钥」)、`AuditAction.View = 14`(显示「查看明文」);审计页类别/动作中文映射(显示层 mapping)补条目——审计记录落库的是枚举值,展示文本随映射走
- 无 schema 变更、无删库重建

**D5 页面形状与路由照 ConfigMap**

- `/secrets`、`/secrets/{ClusterId:int}`(列表)、`/secrets/{ClusterId:int}/{Namespace}/{Name}`(详情)、`/secrets/{ClusterId:int}/{Namespace}/{Name}/yaml`(编辑);`Components/Secrets/` 目录 + `SecretClusterSidebar`(照 ConfigMapClusterSidebar 模式);`.secrets-table` 登记 app.css flex-fill 规则组(三处选择器)
- 揭示按钮用 `TooltipIconButton` + 图标行内动作(遵守 RZ10010:行内可点击区域内 `@onclick:stopPropagation` 包 span)
- K8s 调用:`CoreV1.ListSecretForAllNamespacesAsync`(命名空间筛选 null)/`ListNamespacedSecretAsync`/`ReadNamespacedSecretAsync`/`CreateNamespacedSecretAsync`/`ReplaceNamespacedSecretAsync`/`DeleteNamespacedSecretAsync`,经 `IClusterClientCache`,10s 超时契约适用(非 Helm 子进程)

## Risks / Trade-offs

- [占位符机制是 Secret 特有新逻辑,是清空/泄露双风险点] → 纯函数抽到 Application 层独立单测(回填/覆盖/删除/新增/伪造归属共存),再服务层集成测试;`ResourceOwnershipPolicy` 已有测试基建照搬
- [揭示明文出现在浏览器 DOM/电路中] → 与 ClusterOverviewCard 揭示同一风险面(Blazor Server diff 网络传输已含它);逐 key + 审计把暴露面和留痕最小化
- [YAML tab base64 对归属者可见] → 已在 D1 论证为 kubectl 等价视图;受限态覆盖非归属者
- [immutable Secret 的 K8s 校验(改值/改 key 被拒)] → K8s API 原生 409/422 经既有异常翻译成中文提示,不特殊处理
- [Secret 类型特殊视图(tls/registry)不做] → 全部走 YAML/键值通用视图,不误判结构

## Migration Plan

无 DB schema 变更,部署即用;回滚 = 退版(Drawer 入口随退版消失)。审计枚举新增值不影响既有记录。

## Open Questions

无(揭示按钮位置/掩码样式实现期照 credential 模式微调,不影响契约)。

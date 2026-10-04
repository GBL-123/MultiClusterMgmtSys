# Proposal

## Why

系统已管理集群的配置(Secret/ConfigMap)与工作负载,但缺少对持久化存储的可见性与管控:PV 整体不可见(哪些卷闲置、谁在用、容量几何),PVC 的创建只能靠系统外 kubectl,且 Member 无自建入口。补齐存储域后「Member 自建资源 → 关联数据」的闭环才完整;同时 Secrets 功能刚落地,其归属/审计/页面骨架可直接复用,边际成本低。

## What Changes

- 新增「存储管理」域,Drawer 导航组(Icon=Storage)插在「密钥管理」之后,三个子页:
  - **/storage/claims(PersistentVolumeClaim,可操作)**:命名空间级列表、YAML 创建(模板 `pvc/default`)、详情(元数据 + 挂载此 PVC 的 Pod 列表 + YAML 视图)、删除;归属/黑名单/盖章/CanOperate 投影全套照抄 Secret,无 YAML 编辑页(spec 几乎不可变,避开 409 坑)。
  - **/storage/volumes(PersistentVolume,只读浏览)**:集群级列表 + **只读详情页**(镜像节点详情:字段行 Phase/容量/回收策略/绑定 claim/Store 形态 + 只读 YAML 视图卡),零写操作、零审计。
  - **/storage/classes(StorageClass,只读列表)**:各列自足(名称/Provisioner/回收策略/绑定模式/卷扩展),无详情页。
- Domain `AuditCategory` 新增 `Storage`(显示「存储」),PVC 创建/删除审计复用 `AuditAction.Create/Delete`。
- `IYamlValidator` 新增 `ValidatePersistentVolumeClaim`(V1PersistentVolumeClaim 直反序列化,无占位符 shim)。

## Capabilities

### New Capabilities

- `storage-management`: 存储域整体契约——PVC 列表/创建/详情(含挂载 Pod)/删除、PV 只读列表与只读详情、SC 只读列表、三路由与 Drawer 入口、归属判定与 CanOperate 投影、审计语义(仅 PVC 写操作)。

### Modified Capabilities

- `k8s-resource-ownership`: 「归属载体与创建盖章」纳入 PersistentVolumeClaim(PVC 创建同样盖章;既有 Secret 措辞并入,见顺序依赖)。
- `audit-log`: 「审计事件写入」与「审计记录内容」纳入 PVC 创建/删除(存储操作)与 `Storage` 类目(既有 Secret 条目并入)。
- `display-conventions`: 新增「PV 与 PVC phase 的中英双语展示」(Bound→已绑定、Available→可用、Released→已释放、Failed→失败、Pending→等待中、Lost→丢失等)。
- `ui-theme`: 新增「PV 与 PVC phase 徽章的语义归属」(在线→ 绿、Pending→ 琥珀、Lost/Failed、Released→灰/未知色系)。

**顺序依赖(规格正确性约束)**`add-secret-management` 尚未归档;主 spec 中 `k8s-resource-ownership`/`audit-log` 还没有 Secret 措辞。本变更的这两份 MODIFIED delta **按「Secrets 归档后的主 spec 版本」起草**(既含 Secret 场景又含 PVC/存储场景),保证两 change 先后归档不互相覆盖场景。

## Impact

- **Domain**:`AuditCategory.cs` 加 `Storage` 显示映射;无 schema 变更。
- **Application**:`StorageService`(PVC 六方法 + PV 列/读 + SC 列表,经 `IClusterClientCache`,10s 超时契约)、Requests(PVC Query/Create/Key)、ViewModels(Claim/PV/SC 列表与详情/挂载 Pod 条目)、`PersistentVolumeClaim` 映射、`YamlValidator.ValidatePersistentVolumeClaim`。
- **Infrastructure**:模板 `wwwroot/templates/pvc/default.yaml`;无 DB 变更。
- **Web**:`Components/Storage/`(Pages + Shared 表格/筛选/工具条/视图卡)、Drawer 导航组、路由三条。
- **Tests**:K8sMocks 加 PVC/PV/SC setup(注意 `ListNamespacedPersistentVolumeClaim` pretty 在末尾、`ListPersistentVolumeClaimForAllNamespaces` pretty 在位置 6)、`AddStorageStack`、服务测试 + 归属矩阵 + bUnit 页面测试。

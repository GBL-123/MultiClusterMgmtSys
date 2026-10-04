# Tasks

## 1. 枚举与归属契约增量

- [x] 1.1 Domain `AuditCategory` 加 `Storage`(中文「存储」XML 注释),`AuditLogMappingExtensions.ToDisplayName` 加映射,`SimpleMappingTests` 两 Theory 各加一行 InlineData;验证 `dotnet build` 0 错 + 相关 Theory 全绿
- [x] 1.2 `K8sDisplayText` 加 PvPhase/PvcPhase 映射(Available→可用/Bound→已绑定/Released→已释放/Failed→失败;Pending→等待中/Lost→丢失,未登记回退原文),纯单测覆盖已登记与未登记值

## 2. StorageService(PVC/PV/SC)

- [x] 2.1 Requests(ClaimsQuery/Create/Key/PodQuery + VolumesQuery/ClassQuery 按 ConfigMapService 同粒度收拢)+ ViewModel(Claim/PV/SC 列表与详情、挂载 Pod 条目、CanOperate 投影)与映射(ToXxx 系列;phase 双语 + raw 保留);验证 build 0 错
- [x] 2.2 `StorageService` PVC 读路径(List 双口径 → CanOperateForIndex 投影 / Read → CanOperateAsync,catch when k8s 异常族 → Translate)+ GetNamespacesAsync;验证 build 0 错
- [x] 2.3 `StorageService` 写路径:CreatePersistentVolumeClaimFromYamlAsync(ValidatePersistentVolumeClaim 前置由 UI 调,服务端 RequireCreator → kind校验 → PrepareForCreation(黑名单+Stamp) → Create → 审计 Create)与 DeletePersistentVolumeClaimAsync(read → RequireOperateAsync → Delete → 审计 Delete);验证 `SecretServiceOwnershipTests` 风格矩阵测试(含伪造盖章覆盖、member 进 kube- 拒、anonymous fail-closed)全绿
- [x] 2.4 只读路径:ListPersistentVolumesAsync/GetPersistentVolumeDetailAsync、ListStorageClassesAsync(零审计、不限命名空间);K8sMocks 增加 PVC/PV/SC 段(pretty 参数位按 design D7)+ `IStorageV1Operations` mock 属性;服务测试镜像 SecretServiceTests 粒度(列表投影/详情/404/409/超时翻译)
- [x] 2.5 `YamlValidator.ValidatePersistentVolumeClaim` 接入 IYamlValidator(无 shim);验证 `KubernetesYaml.Deserialize<V1PersistentVolumeClaim>` 冒烟(构造 5Gi + accessModes)

## 3. UI(三页)

- [x] 3.1 基建:`wwwroot/templates/pvc/default.yaml`(apiVersion v1/kind: PersistentVolumeClaim/metadata name+namespace/spec accessModes/storageClassName/resources.requests.storage)、`Components/Storage/` 目录、Drawer「存储管理」MudNavGroup(Icon=Storage)置于密钥管理后、app.css 列表 flex-fill 三处登记 `.`claims-table`/`.`volumes-table`/`.`classes-table`(与 `.secrets-table` 同款);验证 Web build 0 错
- [x] 3.2 持久卷声明页:Claims.razor(/storage/claims + /{ClusterId:int},镜像 Secrets.razor) + ClaimListTable + ClaimListFilterBar + CreateClaimDialog(模板+ValidatePersistentVolumeClaim+失败保持打开+ConflictException 文案);验证 bUnit:列表渲染/空态/不可达禁用/创建对话框打开
- [x] 3.3 PVC 详情页 ClaimDetail.razor(/storage/claims/{ClusterId:int}/{Namespace}/{Name}):Toolbar(返回/名称/容量/状态 chip/CanOperate→删除)+ 字段卡 + 挂载 Pod 卡(PodQuery,空态「无 Pod 挂载此卷」)+ 只读 YAML 视图卡;删除经 ConfirmDialog → 服务删除 → 审计(镜像 SecretDetail flow 测试);验证 bUnit:挂载 Pod 展示/非归属无删除/删除流程审计
- [x] 3.4 持久卷页:Volumes.razor(/storage/volumes + /{ClusterId:int}) + VolumeListTable(phase 双语徽章 ui-theme 色) + 只读详情 VolumeDetail.razor(/storage/volumes/{ClusterId:int}/{Name},镜像节点详情布局:字段行 Phase/容量/回收策略/绑定 claim/存储类/来源 + 只读 YAML 卡,无任何写按钮);验证 bUnit:列表/详情字段/无编辑删除按钮
- [x] 3.5 存储类页:Classes.razor(/storage/classes + /{ClusterId:int}) + ClassListTable(名称/Provisioner/回收策略/绑定模式/允许卷扩展);验证 bUnit:列表渲染

## 4. 收尾(集成与契约)

- [x] 4.1 `BunitServiceExtensions.AddStorageStack`(镜像 AddSecretStack,含 ownership 三件套 + AddYamlTemplates);归属矩阵页测试(SetRoles 变化不影响 owned/unowned 行为)
- [x] 4.2 AGENTS.md 补记存储管理条目(契约/路由三页/Service 形状/只读零审计/挂载 Pod/无 shim 结论/测试基建);Verification: `openspec validate add-storage-management --type change` zero errors
- [x] 4.3 全量验证:`dotnet build` 0 错、`dotnet test` 全绿(基线 1050+新增)、`./coverage.ps1` ≥75%、Admin/Member 双视角手工走查(/storage 三页与审计页)

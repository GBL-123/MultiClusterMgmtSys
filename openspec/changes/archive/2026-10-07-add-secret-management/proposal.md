# Proposal

## Why

系统目前只能管理 ConfigMap 类配置,而集群中最敏感的一类配置数据——Secret(密码、令牌、证书)——完全没有管理入口。用户(Member)同样需要查看与管理自己应用所依赖的 Secret;相比 ConfigMap,Secret 增加了「值是敏感信息」这一维度,需要额外的可见性控制与揭示审计,而不是照抄 ConfigMap 直接展示。

## What Changes

- 新增「密钥管理」功能(Drawer 新入口,位于「配置管理」之后):Secret 列表页(集群侧栏 + 命名空间/名称筛选)、详情页(键值 | YAML 双 tab)、YAML 创建对话框、YAML 编辑页、删除
- 值可见性(方案 B):YAML 视图照常显示 base64 原文;键值 tab 的值一律掩码显示,每 key 提供「查看明文」揭示按钮(复用 `ClusterOverviewCard` 凭据揭示的 credential-redacted/viewer 模式),仅 Admin/归属者可用,每次揭示写审计
- 编辑占位符机制(Secret 特有):编辑页回显时每个 key 的值替换为 `<REDACTED:KEYNAME>` 占位符;提交时占位符 key 保留服务器现值、真实新值才覆盖——防止把他人/既有 secret 意外清空,也避免把明文回显进编辑框
- 归属复用 `k8s-resource-ownership` 全套机制(盖章/CanOperate 投影/命名空间黑名单/Helm 互通),无新表;受管资源集合加入 Secret
- 审计扩展:新增类别「密钥」(Secret),动作新增 创建/修改/删除/查看明文;「查看明文」是只读不记录原则的唯一例外(敏感信息暴露事件必须留痕)
- 无数据库 schema 变更(AuditCategory/AuditAction 是代码枚举,随枚举扩展无需迁移;审计文本列直接存中文枚举文本)

## Capabilities

### New Capabilities

- `secrets-page`:Secret 列表/详情/创建/编辑/删除页面与值揭示(查看明文)行为契约,含编辑占位符机制、集群侧栏与 ClusterSelectionState 复用、Drawer 导航入口

### Modified Capabilities

- `audit-log`:「审计事件写入」SHALL 列表加入密钥创建、修改、删除、查看明文,并修订「MUST NOT 记录只读操作」条款(查看明文例外);「审计记录内容」Category 枚举加入「密钥」、Action 枚举加入「查看明文」
- `k8s-resource-ownership`:「归属载体与创建盖章」受管资源集合明确加入 Secret(归属判定/黑名单/Helm 互通语义不变,新增 Secret 场景)

## Impact

- **Domain**:`AuditCategory` 加 `Secret=10`;`AuditAction` 加 `View=14`(中文显示「查看明文」)
- **Application**:新增 `SecretService`(复用 `ResourceOwnershipGuard`/`IClusterClientCache`/`AuditService`,口径照抄 `ConfigMapService`);新增 `SecretQueryRequest`/`SecretKeyRequest`/`SecretCreateRequest`/`SecretUpdateRequest` 与 `SecretListViewModel`/`SecretDetailViewModel`/`SecretKeyItemViewModel` + 映射;审计类别/动作中文显示映射补条目
- **Infrastructure**:新增 YAML 模板 `wwwroot/templates/secret/default.yaml`(经既有 `IYamlTemplateService` 读取)
- **Web**:新增 `Components/Secrets/`(列表页 `/secrets`、`/secrets/{ClusterId:int}`;详情页 `/secrets/{ClusterId:int}/{Namespace}/{Name}`;编辑页 `/secrets/{ClusterId:int}/{Namespace}/{Name}/yaml`;创建对话框;列表表格/筛选栏/详情卡片;复用 `SecretClusterSidebar` 按 ConfigMapClusterSidebar 模式);`Drawer.razor` 加「密钥管理」入口;揭示按钮复用 credential-redacted/viewer CSS
- **k8s 调用**:CoreV1 `ListNamespacedSecret`/`ListSecretForAllNamespaces`/`ReadNamespacedSecret`/`CreateNamespacedSecret`/`ReplaceNamespacedSecret`/`DeleteNamespacedSecret`,经 `IClusterClientCache` 取客户端,10s 超时契约适用
- **测试**:`SecretServiceTests`/归属矩阵/页面 bUnit 流程测试;测试基建全部复用(ServiceHarness/K8sMocks/BunitServiceExtensions)

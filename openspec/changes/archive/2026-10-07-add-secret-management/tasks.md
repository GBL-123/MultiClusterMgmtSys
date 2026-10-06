# Tasks

## 1. 枚举与纯函数基础

- [x] 1.1 Domain 枚举扩展:`AuditCategory.Secret = 10`、`AuditAction.View = 14`(中文 XML 注释),审计显示映射补「密钥」「查看明文」条目;验证:新增枚举单测断言显示文本
- [x] 1.2 Application 层占位符纯函数 `SecretRedaction`:`Placeholder(key)` 常量拼装、ApplyPlaceholders(序列化 YAML 后对 data/stringData/binaryData 逐 key 替换)、MergeSubmitted(secret 提交合并:占位符回填服务器现值/新值覆盖/缺 key 删除/新增 key),与 `ResourceOwnershipStamp` 同目录风格;验证:`SecretRedactionTests` 覆盖 回填/覆盖/删除/新增/伪造归属 YAML 中占位符仍生效/非占位值等于现值

## 2. SecretService 服务层

- [x] 2.1 `SecretService` 骨架:ctor(`IClusterRepository, ResourceOwnershipGuard, AuditService, IClusterClientCache, ILogger<SecretService>`)+ `GetNamespacesAsync` + `ListSecretsAsync(SecretQueryRequest)`(命名空间筛选 null = 全部;CoreV1 list 经 `IClusterClientCache`;列表投影 `CanOperate` 走 `CanOperateForIndex` 含 Helm 互通);验证:`SecretServiceTests` 列表/命名空间/404/翻译链路测试
- [x] 2.2 详情与揭示:`GetSecretDetailAsync`(详情 VM 含键值行列表 `SecretKeyItemViewModel` + CanOperate 投影)+ `RevealSecretKeyAsync`(read → RequireOperateAsync → 解码 data/stringData key;binaryData 拒绝揭示;成功写审计「密钥/查看明文」目标含 key);验证:服务层测试——归属者揭示成功且审计落库、非归属者 PermissionException 不写审计、binaryData 揭示被拒
- [x] 2.3 创建:`CreateSecretFromYamlAsync(SecretCreateRequest)`(反序列化 V1Secret → RequireCreator → 黑名单 → Stamp → CoreV1 create → 审计「密钥/创建」);验证:服务层测试——成功盖章、缺 metadata/缺 namespace 中文校验异常、kube-system Member 被拒不调 API、YAML 格式错误
- [x] 2.4 编辑:GetSecretForEditAsync(占位符化 YAML 输出)+ `UpdateSecretFromYamlAsync(SecretUpdateRequest)`(read → RequireOperateAsync → `SecretRedaction.MergeSubmitted` 合并 → 保服务器 metadata → replace → 审计「密钥/修改」);验证:服务层测试——占位符 key 保留现值、新值覆盖、缺 key 删除、归属 label 不被用户 YAML 清除、非归属者被拒
- [x] 2.5 删除:`DeleteSecretAsync(SecretKeyRequest)`(read → RequireOperateAsync → delete → 审计「密钥/删除」;判定失败不写审计);验证:归属矩阵测试(`SecretServiceOwnershipTests` 或并入 2.2-2.4 的矩阵类),K8s 404/409 翻译测试

## 3. UI 页面

- [x] 3.1 基建:`wwwroot/templates/secret/default.yaml` 模板 + `Components/Secrets/` 目录骨架(`SecretClusterSidebar` 照 ConfigMapClusterSidebar 模式、列表页路由 `/secrets` 与 `/secrets/{ClusterId:int}`、`.secrets-table` 登记 app.css flex-fill 三处选择器、`Drawer.razor` 「密钥管理」入口位于「配置管理」之后);验证:bUnit 列表页空态/侧栏高亮/路由解析
- [x] 3.2 列表表格与筛选:`SecretListTable.razor`(名称链接/命名空间/类型 mono/键数/创建时间/操作)+ `SecretListFilterBar`(命名空间下拉 + 名称搜索 + 查询/重置)+ 新建按钮(全体渲染、不可达 Disabled);验证:bUnit 行渲染、CanOperate 条件渲染(owned/unowned 两行)、排序表头存在
- [x] 3.3 详情页 `/secrets/{ClusterId:int}/{Namespace}/{Name}`:键值 | YAML 双 tab;键值行掩码 + `CanOperate` 用户的「查看明文」/「隐藏」揭示(复用 credential-redacted/viewer CSS)、binaryData 仅字节数 + base64 原文;非 CanOperate 渲染「无权查看该 Secret 的内容」受限态;验证:bUnit——掩码默认态、揭示切换调用服务与审计 mock、受限态无揭示按钮、YAML tab 内容
- [x] 3.4 创建对话框 `CreateSecretDialog.razor`(模板 secret/default 载入、YAML 校验、失败保持打开);验证:bUnit 成功关闭/校验失败保持打开
- [x] 3.5 编辑页 `/secrets/{ClusterId:int}/{Namespace}/{Name}/yaml`(全高 YAML 卡、占位符回显、提交走 `UpdateSecretFromYamlAsync`、非归属者由服务端拒);验证:bUnit 回显含 `<REDACTED:`,提交调用合并服务;[Authorize] 页面属性
- [x] 3.6 删除入口(列表行/详情 + ConfirmDialog,同 ConfigMap 交互);验证:bUnit 确认后调用删除服务

## 4. 测试与验证收尾

- [x] 4.1 归属矩阵与页面流程:Secret 写操作归属矩阵(Admin 穿透/owner/无归属 fail-closed/无身份 fail-closed)+ 列表/详情 CanOperate 投影矩阵;页面流程测试(新建→列表刷新、编辑占位符往返)接入既有 BunitServiceExtensions(新增 `AddSecretStack` 或并入 AddClusterStack);验证:`dotnet test MultiClusterMgmtSys.Tests` 相关测试全绿
- [x] 4.2 AGENTS.md 补记「密钥管理」功能条目(路由/占位符机制/揭示审计/契约 secrets-page);验证:文档条目与实现一致
- [ ] 4.3 全量验证:`dotnet build MultiClusterMgmtSys.slnx` 0 错误 + `dotnet test MultiClusterMgmtSys.Tests` 全绿 + `./coverage.ps1` ≥75%;端到端手工走查(Admin/Member 两视角:创建→掩码→揭示→编辑占位符→删除)

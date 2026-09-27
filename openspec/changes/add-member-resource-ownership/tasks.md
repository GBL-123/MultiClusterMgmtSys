# Tasks

## 1. 归属纯函数(Application/Common/Ownership)

- [x] 1.1 定义归属常量与 Helm 托管标识(`mcms.ms/owner-uid` label、`mcms.ms/owner-name` 注解、`meta.helm.sh/release-name`/`-namespace` 注解识别),含中文 XML 注释;验证:`dotnet build` 0 错误 + CS1591 零命中
- [x] 1.2 实现创建盖章纯函数(输入对象元数据 + 当前身份,按 `k8s-resource-ownership` 契约无条件覆盖写入 `owner-uid`/`owner-name`);验证:纯单测覆盖 Admin/Member 盖章、用户 YAML 伪造归属被覆盖
- [x] 1.3 实现归属判定纯函数(Admin 短路、`owner-uid` 匹配、无主 fail-closed、Helm 托管分流接 `IHelmReleaseOwnershipRepository.GetAsync`);验证:纯单测覆盖 Admin/本人/他人/无主/无身份 + Helm 托管命中与未命中
- [x] 1.4 实现创建目标命名空间黑名单(`kube-` 前缀拒绝、`default` 放行);验证:纯单测两态 + 边界(`kube.x` 是否命中走实现口径)

## 2. 服务层强制接入:WorkloadService

- [x] 2.1 注入 `IHttpContextAccessor`,四类 Create{Kind}FromYamlAsync 接入身份校验 + 归属盖章 + 命名空间黑名单(方法签名不变);验证:五类身份矩阵单测(Admin/本人/他人/无主/无身份)、伪造 label 覆盖、`kube-system` 拒绝单测
- [x] 2.2 四类 Update{Kind}FromYamlAsync 接入归属判定(置于既有 spec 覆盖逻辑之前),并断言编辑保留归属元数据;验证:归属者/他人/无主单测 + 编辑后 label 不被改写的断言
- [x] 2.3 Scale{Kind}Async(Deployment/StatefulSet/ReplicaSet)与 Restart{Kind}Async(排除 ReplicaSet)接入归属判定(读一次对象先判定);验证:类型可用性矩阵回归 + 归属矩阵单测
- [x] 2.4 Delete{Kind}Async 四类接入归属判定;验证:归属者删除成功 + 他人/无主拒绝单测
- [x] 2.5 List/Detail 读映射:四类列表 VM 与详情 VM 增加 `CanOperate`(提取 `mcms.ms/owner-uid` label 与 Helm 托管分流计算,Admin 短路);验证:单测覆盖 owner 存在/缺 label/Helm 托管命中/未命中

## 3. 服务层强制接入:ConfigMapService 与 SvcService

- [x] 3.1 `CreateConfigMapFromYamlAsync`/`CreateSvcFromYamlAsync` 接入身份校验 + 盖章 + 黑名单;验证:身份矩阵 + 伪造覆盖 + 黑名单单测
- [x] 3.2 `UpdateConfigMapFromYamlAsync`/`UpdateSvcFromYamlAsync` 接入归属判定(既有 data/binaryData 覆盖与不可变字段守卫不变),断言保留归属元数据;验证:归属者/他人/无主单测
- [x] 3.3 `DeleteConfigMapAsync`/`DeleteSvcAsync` 接入归属判定(先读对象);验证:归属者删除 + 他人/无主拒绝单测
- [x] 3.4 ConfigMap/Svc 的 List/Detail 映射增加 `CanOperate`;验证:同款单测

## 4. UI 投影改造(工作负载/ConfigMap/Service 三族)

- [x] 4.1 `WorkloadListView`/`WorkloadListTable`/`WorkloadDetailToolbar`:拆除 `AuthorizeView Roles="Admin"` 写操作遮挡,「新建」对所有登录用户开放(不可达仍禁用),行内/详情操作按 `CanOperate`;验证:bUnit 断言 Member 可见新建入口、owner/非 owner 操作按钮分支、Admin 全量分支
- [x] 4.2 `ConfigMaps` 页面 + `ConfigMapListTable`/`ConfigMapDetailToolbar`/`EditConfigMapYaml` 页面属性同款改造(编辑页属性降为 `[Authorize]`);验证:bUnit 断言同 4.1 口径
- [x] 4.3 `Svcs` 页面 + `SvcListTable`/`SvcDetailToolbar`/`EditSvcYaml` 页面属性同款改造;验证:bUnit 断言同 4.1 口径
- [x] 4.4 创建对话框语义回归:`CreateWorkloadDialog`/`CreateConfigMapDialog`/`CreateSvcDialog` 打开、预填与提交路径,黑名单错误在对话框内呈现;验证:bUnit 打开对话框 + 服务端拒绝场景单测
- [x] 4.5 AGENTS.md 补记归属模型(label 键名、五类身份矩阵口径、受影响页面清单);验证:文档与实现一致

## 5. 集成验收

- [x] 5.1 `dotnet build` 0 错误、`dotnet test MultiClusterMgmtSys.Tests` 全绿(数量不低于基线)、`ArchitectureTests` 通过;验证:命令输出
- [x] 5.2 `./coverage.ps1` 四程序集覆盖率门禁 ≥75% 不降;验证:脚本退出码
- [ ] 5.3 端到端手工验收:member 建工作负载/ConfigMap/Service 进 `default` → 自己可编辑/扩缩容/删除 → member 操作他人资源被拒 → 进 `kube-system` 创建被拒 → Admin 全量操作 → 审计含 member 操作记录;验证:按步骤走查

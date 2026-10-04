# Design

## Context

权限现状与归属先例见 proposal.md 的 Why。塑造实现方式的既有事实:

- **集群凭据是池化的**:所有 K8s 调用经 `IClusterClientCache` 使用库里存的**集群级**凭据(kubeconfig/admin token),不是用户级凭据——K8s RBAC 无法区分用户,一切权限只能由系统服务层强制。这也意味着 member 与 admin 发出的 K8s API 调用在集群侧无差别,反伪造必须在应用层做。
- **服务层今天没有权限检查**:`WorkloadService`/`ConfigMapService`/`SvcService` 的写方法不查角色,「Admin 门控」是 UI 层(`AuthorizeView` 遮挡 + 编辑页 `[Authorize(Roles="Admin")]` 页面属性)的承诺。open 写路径给 member 前必须补服务端强制,否则是纯放权不是收权。
- **Helm 已趟出模式**:`HelmService` 的 `RequireOperatePermissionAsync`(IHttpContextAccessor 取身份 + 归属判定 + 失败关闭)+ `CanOperate` 进 ViewModel + 五类身份矩阵测试,全部可平移。
- **既有编辑策略天然保留归属**:`UpdateDeploymentFromYamlAsync` 仅覆盖 spec、`UpdateConfigMapFromYamlAsync` 仅覆盖 data/binaryData、服务编辑保留 metadata 整体替换——归属 label 只要不进用户可改面,Admin 编辑不会破坏 member 的归属。
- **注册未限用户名字符集**(Identity 默认),中文用户名是合法输入,label value 却只接受 alnum/-/_/. ≤63——归属值必须用 uid(int)而不是用户名。
- **namespace-management 的保护规则**(`default` + `kube-*` 禁删)与 `IsProtected` 静态方法已存在,可复用于创建黑名单(`default` 对创建放行)。
- 读路径的服务层方法返回 ViewModel(V1 对象映射而来),加 `CanOperate` 不改方法签名与路由。

## Goals / Non-Goals

**Goals:**

- 服务端强制归属:Admin 穿透、owner uid 匹配、无主 fail-closed(Helm 同款语义)。
- 创建即盖章 + 防伪造覆盖,归属完全跟随对象 life cycle,零 schema 变更。
- UI 从「角色遮挡」改造为「`CanOperate` 投影」,新建入口对所有登录用户开放。
- Helm 托管对象互通:资源页按 `helm.sh` 标识分流到 `HelmReleaseOwnership` 判定。

**Non-Goals:**

- 不做归属转让/认领(存量无主资源维持仅 Admin);不做 member 列表过滤(全员可见,与 Helm 一致)。
- 不放开 Namespace 管理(仍 Admin-only);不动 Pod 只读能力、Secret(无管理面)与 Events;`/helm` 模块自身语义不变。
- 不做资源级配额、审计违规告警、按_NAMESPACE_或按角色的细粒度授权配置面。
- 不改 Helm 归属的 revision 降级启发式(资源页互通只按 uid 判,作为已知简化)。

## Decisions

### D1 归属载体 = K8s label/annotation,零 schema 变更

**选择**:label `mcms.ms/owner-uid: "<uid>"` + 注解 `mcms.ms/owner-name: "<用户名>"`。创建成功由服务端**无条件覆盖**写入(防伪造);读路径常驻可取。

**备选**:(a) 数据库归属表(复刻 `HelmReleaseOwnership`);(b) 只用注解不落 label。

**理由**:Helm 被逼上数据库表是因为 `helm list -o json` 不吐 labels;原生资源我们**读得到** label,没有那个约束。label 方案天然规避归属表的最大痛点——漂移(外部删除=归属消失;外部重建=无 label=无主,fail-closed 自动成立)。更直接的是 (a) 会引入**第三次删库重建**(`add-cluster-dashboard`、`add-helm-management` 各一次,用户已痛苦过)。用 uid 而非用户名做 label 值,规避用户名字符集与 63 字符限制;用户名只进注解(注解值无字符集限制)。选 (b) 的动机是注解不参与调度/筛选,可被 GitOps 工具视为元数据;但 label 是可查询面(selector 必须是 label),且 `app.kubernetes.io/managed-by` 也是 label——跟随行业惯例,label + 注解双载体。

### D2 服务端判定为唯一可信来源 + `CanOperate` 投影(Helm D8 平移)

**选择**:新增应用层归属判定纯函数(Admin 短路 / `owner-uid` == 当前 uid / FailClose);三个服务的读方法把 `CanOperate` 算进 ViewModel;写方法在调用 K8s 变更 API **前**完成判定。身份经 `IHttpContextAccessor` 取(与 `HelmService`/`AccountService` 一致,前端不传用户)。UI 删除既有的 `AuthorizeView Roles="Admin"` 写操作遮挡,改按 `CanOperate` 渲染;编辑页 `[Authorize(Roles="Admin")]` 页面属性降为 `[Authorize]`。

**备选**:服务层加构造参数注入当前用户;`AuthorizeView` + 归属查询在 UI 自行组合。

**理由**:归属不是角色,`AuthorizeView` 表达不了;Blazor Server 电路复用下 UI 不可信;`IHttpContextAccessor` 是既有术语表。需要 GET 的 mutation(scale/restart/delete)先读一次再改——多一次读调用,换来归属自证,拥抱变化。

### D3 判定规则函数与收集口径

**选择**:

```
判定输入: 对象元数据(labels/注解) + 发布上下文(clusterId) + 当前用户(id/是否Admin)
规则:
  1. Admin → 通过
  2. 对象带 Helm 托管标识 → 查 HelmReleaseOwnership(clusterId, release-ns, release-name)
       存在且 owner-uid == 当前 uid → 通过;否则拒绝(fail-closed)
  3. 对象带 mcms.ms/owner-uid 且 == 当前 uid → 通过
  4. 其他(无标签/归属他人) → 拒绝
  5. 无身份 → 拒绝(PermissionException)
```

**备选**:把 Helm 判定也做 revision 比对;维护双载体一致性校验。

**理由**:资源页拿不到 release revision,逐对象打 `helm status` 太重;revision 降级防的是「系统外卸载重装」,该场景在 /helm 页有完整判据,资源页仅凭 uid 判定即可闭环(共享的安装者仍同 uid)。双载体一致性(Helm 装的对象没有 `mcms.ms` label)由规则 2 优先短路,不叠加校验。

### D4 创建黑名单与命名空间下拉

**选择**:member/admin 经系统创建时均校验目标命名空间:`kube-` 前缀拒绝(复用 `NamespaceService.IsProtected` 的 `kube-` 分支语义,`default` 放行=仅 `kube-` 参与);`default` 允许创建(用户已拍板)。命名空间下拉的选项源(`GetNamespacesAsync`)不变。

**备选**:黑名单做成可配置(设置面);per-user namespace 分配表。

**理由**:`IsProtected` 是既有静态规则,校验点收敛在服务层(编译期不可绕过);YAML 是直接编辑面,黑名单必须在服务端而非 UI 选项里裁剪。配置化与 per-user 分配都是真实需求但都引入新面,列后续。

### D5 表面积:纯函数落位与复用

**选择**:

- `Application/Common/Ownership/`:归属常量(键名)、盖章纯函数(输入 V1ObjectMeta+身份,输出目标 label/注解)、判定纯函数(D3 规则,输入从 `HelmReleaseOwnership` 查询抽象为函数)、Helm 托管标识识别常量。
- 三个服务(Create/Update/Scale/Restart/Delete 点位)调用盖章/判定;List/Detail 映射扩展方法加 `CanOperate`。
- `IHelmReleaseOwnershipRepository` 已实现的 `GetAsync(clusterId, ns, releaseName)` 直接复用,不新增端口。

**理由**:与 `HelmCommandBuilder`/`K8sExceptionMapper` 一致的「Application/Common 纯函数」模式;测试以纯函数+五类身份矩阵覆盖,不需要真实集群。

### D6 UI 改造最小面

**选择**:

- 列表页标题行「新建」按钮:删除 `AuthorizeView Roles="Admin"` 包裹,改为登录可见(离线禁用逻辑不变)。
- 行内(编辑 YAML/删除/扩缩容/重启)与详情工具栏、编辑页(保存按钮):按每行/每详情 `CanOperate` 条件渲染。
- `CreateWorkloadDialog`/`CreateConfigMapDialog`/`CreateSvcDialog` 交互不变(提交路径本就走服务层)。
- 列表不展示创建者列(与 Helm 一致,追溯走审计)。

**备选**:把归属判定也留给 UI(`AuthorizeView` 自定义策略)。

**理由**:UI 只是投影;按钮级条件由 ViewModel 驱动,风险集中在服务层测试;bUnit 直接断言按钮分支(同 Helm 页测试形态)。

## Risks / Trade-offs

- **[电路内 IHttpContextAccessor 的可用性]** → 仓库既有口径(Helm 归属/审计/账号服务同款,交互服务器渲染)。失败具有单侧安全性:电路内若取不到身份,守卫一律 fail-closed 拒绝(不会误放权),代价可能是合法操作被拒不误;真实电路行为由任务 5.3 端到端走查兜底验证。

- **[admin 级凭据池化 → 系统内伪造/越权面]** → 服务端强制 + 创建盖章无条件覆盖;审计记操作者;若未来要求真隔离,需切用户级凭据/K8s User impersonation(换凭据模型,远超本 change)。
- **[存量无主资源对 member 全屏蔽]** → 用户拍板 v1 不认领;如需,后续加「Admin 转让归属」小动作(写 label 即可)。
- **[外部工具剥离/篡改 `mcms.ms/*` 元数据]** → 扭转即失效:成员失去操作权(fail-closed,无安全损失);篡改为他人 uid 只会被下一轮的「无条件覆盖」在创建/编辑中纠正,但**已篡改对象在编辑路径可能被冒名**——编辑不重写归属 label(语义=保留 metadata),故 label 被外部改写后系统判定按对象上的值执行。这是 label 方案的固有软肋,缓解手段是审计日志留痕+集群侧凭据封闭;比 Helm 的数据库表信任边界稍弱,视为可接受。
- **[每次 Mutation 多一次 GET]** → scale/restart/delete 增加一次读调用;受 `kubernetes-call-timeout` 10s 超时与客户端缓存约束,成本可控。
- **[D4 命名空间黑名单做成硬编码]** → `kube-` 前缀规则来自既有代码语义,不改行为面;如需 per-assignment 配置,列后续。
- **[Helm 互通只按 uid 不看 revision]** → 系统外卸载重装后,属主在其资源页仍可能短暂视为归属者(检测到降级仅发生在 /helm 页);属主变更(LLM 不可感知,owner 变更=重装)的窗口极窄,fail-closed 主路径不受影响。
- **[服务层签名未收拢权限语义]** → 判定发生在服务层方法体首行,与既有 `ValidationException` 在同一层;架构测试(命名空间隔离)不受影响(纯函数位于 Application/Common)。

## Migration Plan

1. 无 schema 变更、无配置变更、无删库重建——直接部署。
2. 部署后存量资源立即变为「无归属 = 仅 Admin」,Member 的既有读能力不受影响。
3. Admin 新建的资源从部署时刻起盖章;member 的新建/编辑按归属生效。
4. 回滚:还原代码即可,无人读 label,归属遗留无害。

## Open Questions

- `mcms.ms` 前缀是否要做成 key 的常量(如 `mcms.ms/owner-uid`、`mcms.ms/owner-name`)且写入 YAML 模板注释提醒用户——影响面小,实现时定。
- Helm 互通对 `helm.sh` 托管标识的识别键名有哪些(3 个注解还是 2 个注解+1 个 label)——实现时按 Helm 3/4 兼容面定,不影响契约语义。

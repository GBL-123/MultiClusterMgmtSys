# cross-cluster-compare

## Purpose

把多集群系统补上「跨集群视角」:对同一命名空间下的同名资源,以双栏 YAML 呈现两个集群间的真实差异,并可将源侧对象一键克隆到对照集群,克隆走系统内既有创建路径,从而自动继承归属盖章、命名空间黑名单、权限与审计语义。
## Requirements

### Requirement: 对照页面与资源族范围

系统 SHALL 提供跨集群对照页面(路由 `/compare`),对所有已登录用户可见。页面 SHALL 支持选择源集群、对照集群(两者不可为同一集群)、资源族与资源所在命名空间。

资源族 SHALL 覆盖工作负载四类(Deployment/StatefulSet/DaemonSet/ReplicaSet)与 ConfigMap,SHALL NOT 在 v1 扩大到其他资源族。侧边导航 SHALL 新增「跨集群对照」独立入口,位于「Helm 应用管理」入口之后。

#### Scenario: 打开对照页面

- **WHEN** 已登录用户访问 `/compare`
- **THEN** 页面渲染集群与资源族选择器,可进入对照视图

- **WHEN** 用户在源集群与对照集群选择同一集群
- **THEN** 页面给出中文提示,不执行对照查询

#### Scenario: 资源族仅含既定集合

- **WHEN** 用户查看资源族选择器
- **THEN** 选项仅为 Deployment、StatefulSet、DaemonSet、ReplicaSet、ConfigMap 五项

#### Scenario: 导航入口激活态

- **WHEN** 用户进入 `/compare` 页面
- **THEN** 侧边导航「跨集群对照」入口呈激活态,位置在「Helm 应用管理」之后

### Requirement: 双栏 YAML 对照与差异标注

对照视图 SHALL 以双栏呈现源侧对象与对照集群同名命名空间同名对象的原始 YAML。任一侧不存在该对象时,对应侧 SHALL 明确标注「不存在」,另一侧仍完整展示。

两侧内容逐行对比,差异行 SHALL 有可见的高亮标注;相同行 SHALL 不与差异行产生视觉混淆。对照视图 SHALL 提供在完整内容与仅差异行之间切换的开关,默认展示完整内容。读取任一侧失败 SHALL 遵循既有异常翻译契约给出中文提示,SHALL NOT 使页面不可用。

资源名 SHALL 在用户从源侧清单选定(或输入)后执行两侧并行读取;查询 SHALL 经 `IClusterClientCache` 取得的客户端完成,SHALL 遵循统一超时契约。

#### Scenario: 两侧都存在且存在差异

- **WHEN** 源集群与对照集群同命名空间下存在同名 Deployment 但镜像或副本数不同
- **THEN** 双栏完整渲染两份 YAML,相异的行有差异高亮,并提供「仅看差异」开关

#### Scenario: 对照侧不存在该对象

- **WHEN** 源集群存在该 ConfigMap 而对照集群同命名空间不存在同名对象
- **THEN** 对照侧标注「不存在」,源侧 YAML 完整渲染

#### Scenario: 读取失败提示

- **WHEN** 对照集群该资源读取时 API 不可达
- **THEN** 页面呈现经异常翻译的中文错误提示,源侧已渲染的内容不消失、页面不整页中断

### Requirement: 一键克隆到对照集群

对照视图 SHALL 为源侧存在、对照侧不存在或两侧内容有差异的组合提供「克隆到对照集群」入口。克隆 SHALL 以源对象 YAML 为基础剥离服务端身份与归属元数据——`uid`、`resourceVersion`、`creationTimestamp`、`status` 段、`managedFields` 与系统归属标签(`mcms.ms/owner-uid`)、归属注解(`mcms.ms/owner-name`)SHALL NOT 被带入目标——再经该资源族既有的创建服务在对照集群创建。

创建 SHALL 复用既有创建路径,SHALL 继承其全部既有语义:创建者归属盖章、Member 目标命名空间 `kube-` 前缀拒绝、黑名单与命名空间校验、审计记录(类别与操作与该族手动创建一致)。克隆 SHALL 在用户确认(展示剥管后的 YAML 预览)之后执行;执行中 SHALL 禁用入口,完成后 SHALL 提示结果并重新加载对照视图。

对照集群已存在同名同命名空间对象且克隆要求创建时,SHALL 给出中文冲突提示(如「对照集群已存在同名对象」),SHALL NOT 执行覆盖式更新或静默改造为更新路径。克隆是创建行为:除调用既有创建服务的失败外,任意失败 SHALL NOT 修改对照集群状态,SHALL NOT 提供自动回滚补偿。

#### Scenario: 无差异不提供克隆

- **WHEN** 两侧同名对象 YAML 完全一致
- **THEN** 克隆入口不展示

#### Scenario: 克隆剥离身份元数据并走既有路径

- **WHEN** 用户把源集群 Deployment "web"(命名空间 default)克隆到对照集群并确认
- **THEN** 提交到既有创建服务的 YAML 不含 uid/resourceVersion/creationTimestamp/status/managedFields 与 `mcms.ms/owner-uid` 标签、`mcms.ms/owner-name` 注解;创建按既有创建服务执行,产生与手动创建一致的审计记录

#### Scenario: 克隆预览与确认

- **WHEN** 用户点击「克隆到对照集群」
- **THEN** 弹出确认对话框展示剥离后的 YAML 预览与目标集群名,确认后才执行创建

#### Scenario: 目标已存在冲突

- **WHEN** 对照集群同命名空间下已存在同名 Deployment,而克隆入口要求创建
- **THEN** 页面展示中文冲突提示,对照集群上该资源不被修改

#### Scenario: Member 克隆受既有限制约束

- **WHEN** Member 用户向命名空间 `kube-system` 克隆 ConfigMap
- **THEN** 既有创建路径抛出权限(或命名空间限制)业务异常,中文提示,不产生任何写入

#### Scenario: 克隆成功提示

- **WHEN** 克隆创建成功
- **THEN** 页面提示成功并重新执行对照查询,对照侧显示该新对象

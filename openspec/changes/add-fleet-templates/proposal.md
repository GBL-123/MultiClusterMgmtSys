# Proposal

## Why

「一个配置每个集群都要有,但各集群略有不同」(同一份基础 YAML,仅镜像地址/副本数/命名空间等变量不同)的场景,现在三条路都不合适:逐个集群手工创建 N 次易抄错;Helm 舰队下发要打包 `.tgz` chart,为一份裸 YAML 写 chart 太重;跨集群克隆 1:1 无参数,无法复用下发。需要 **helm-fleet-deploy 的执行骨架 + cross-cluster-compare 的双栏 diff 眼睛,应用到带变量的裸 YAML**。

## What Changes

- 新增舰队模板下发能力(**即传即用,v1 零持久化、零 schema 变更**):粘贴带 `{{var}}` 占位符的 YAML → 现填变量矩阵(目标集群 × 变量值)→ 逐集群渲染 → 逐集群双栏 diff 预览 → 确认后逐集群下发,完成即消失。
- **`{{var}}` 纯文本替换**(否决 Go template:90% 场景够用且无注入风险;条件逻辑直接写在模板里,让某集群不用该变量即可)。
- **upsert 语义**(区别于跨集群克隆的 create-only——模板天然反复下发,create-only 会让第二次下发全被同名冲突挡死):目标集群上不存在则**新建走既有创建路径**(归属盖章 / kube- 黑名单 / 审计自动继承),已存在则**更新走既有编辑路径**。
- 逐集群双栏 diff 预览复用 `/compare` 的差异视图口径,逐集群标注 **新建 / 更新 / 一致**;确认后逐集群下发(有界并发 4、逐集群失败隔离、进度「已完成 / 总数」,骑 fleet-deploy 先例),逐成功集群写审计(描述含「舰队下发」)。
- v1 资源族 = 工作负载四类(Deployment/StatefulSet/DaemonSet/ReplicaSet)+ ConfigMap(即既有创建路径覆盖的族)。
- Admin-only(v1):下发属集群级批量写操作,与 Helm 舰队下发同权限域。
- **模板库 = v2 北极星**:v1 先趟通「渲染 → diff → upsert」这条有风险的链,v2 在链前加纯 CRUD(「保存为模板」+ 模板列表)即可;远期与跨集群对照(模板 = 期望态,现状 = 实际态)、告警中心(评估器)串联成 GitOps 骨架。v1 一行不浪费,单向平滑演进。

## Capabilities

### New Capabilities

- `fleet-templates`: 舰队模板下发契约——`{{var}}` 纯文本替换的渲染口径(未填变量即校验错误,不留裸占位符)、变量矩阵(目标集群 × 变量值)的输入形态、逐集群双栏 diff 预览(新建/更新/一致标注)、upsert 语义(新建走既有创建路径/更新走既有编辑路径,归属盖章与审计自动继承)、Admin-only、有界并发与失败隔离、即传即用零持久化。

### Modified Capabilities

<!-- 无:cross-cluster-compare 的既有需求不变(diff 视图为组件级复用,不改变 /compare 自身行为);helm-fleet-deploy 不变(仅骑其执行先例);五族创建路径的既有契约(workload/configmaps)仅作为 upsert 的下游被调用,需求本身不变。 -->

## Impact

- **新增代码**:`Application/Services/FleetTemplateService.cs`(渲染 + 逐集群预取现值 + diff + 下发编排)、`Application/Requests/FleetTemplate*.cs`(模板粘贴/变量矩阵/预览与下发请求)、页面或大对话框 `Web/Components/FleetTemplates/`(粘贴 → 矩阵 → 预览 → 执行四态,Admin-only);测试 `FleetTemplateServiceTests`(渲染矩阵 + upsert 分支 + 失败隔离)+ bUnit 流程测试。
- **复用与提取**:`/compare` 的双栏 diff 视图需从 Compare 页面提取为共享组件(Compare 页面行为不变);下发编排复用 Helm 舰队下发的「并发 4 / 失败隔离 / 进度」模式。
- **K8s 调用**:预取现值 + 下发均经既有 `IClusterClientCache`(10s 超时契约适用);五族既有创建/编辑服务自动继承归属、黑名单、审计语义。
- **数据库/Schema**:零变更(即传即用,不落任何表)。
- **路由**:新增 Admin-only 路由(具体入口位置与页面形态在 design 阶段定)。

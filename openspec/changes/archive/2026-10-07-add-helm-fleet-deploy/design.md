# Design

## Context

`HelmService` 已具备完整的单集群能力:`InstallAsync`/`UpgradeAsync` 经 `IHelmCliRunner` 执行 helm 子进程(`BuildInstall`/`BuildUpgrade` 参数表 + 占位符临时文件),成功后写归属(`HelmReleaseOwnership`,InstalledRevision=1)与审计(`AuditCategory.Helm` + `AuditAction.Install/Upgrade`)。`HelmCliRunner` 每次调用创建独立临时目录(kubeconfig + HELM_* 环境隔离),并发调用天然互不干扰(helm-cli-runtime 契约已覆盖)。批量执行形态沿用 `ClusterService.RefreshAllClustersStatusAsync` 的成熟先例:有界 `Parallel.ForEachAsync` + `IProgress<(int current, int total)>` + 逐集群隔离。本 change 无数据库 schema 变更、无新配置项。

## Goals / Non-Goals

**Goals:**

- 一次上传 chart + 一套参数,下发到多个集群;逐集群「不存在安装 / 已存在升级」
- 逐集群失败隔离与结果汇总;进度回报复用「刷新全部」模式
- 复用既有 Helm 服务与 CLI 运行器,不引入新表/新枚举值/新配置

**Non-Goals:**

- 按集群 values 覆盖(统一 values 是用户确认的口径)
- 批量回滚/批量卸载(后续可基于同一骨架扩展)
- 用户侧取消按钮(与「刷新全部」一致,取消仅随停机发生)
- 新路由或侧边导航入口(入口收在 `/helm` 工具栏)

## Decisions

1. **服务方法挂在 `HelmService` 而非新服务**——批量下发与单集群安装/升级共享校验、超时、文件构造与归属逻辑;新方法 `DeployToFleetAsync(HelmFleetDeployRequest, IProgress<(int, int)>?, CancellationToken)` 返回 `HelmFleetDeployResultViewModel`。备选方案:独立 `HelmFleetDeployService`,但会复制 `RequireClusterAsync`/`RunAsync`/归属写入等私有链路,放弃。
2. **逐集群预检用 `helm status` 而非 `helm upgrade --install` 一把梭**——`--install` 无法从结果区分「装了还是升了」,而预检结果同时驱动:a) install/upgrade 分支;b) 结果行的动作标注;c) 归属是否写入(install 写、upgrade 不写)。代价是已存在 release 的集群多一次 60s 读调用,可接受。
3. **预检失败的处理**:`helm status` 抛出的异常分两类——`NotFoundException`(release 不存在 → 走安装)与其它(集群不可达/凭据失效等 → 该集群记失败项,不尝试下发)。判断经异常类型而非解析 stderr。
4. **并发上限取常量 4**(`private const int MaxFleetConcurrency = 4`),与 `ClusterService.MaxProbeConcurrency` 同值同做法;helm 子进程是重量级操作(最长 6 分钟),不提供配置项——如需调整改代码即可,避免配置面扩大。
5. **归属/审计在每个集群成功后立即写**(并行体内直接调用既有 `TryUpsertOwnershipAsync` + `_auditService.LogAsync`),不做批量延迟落库——任意时刻中断,已完成集群的状态都是一致的,无需停机补偿逻辑。
6. **升级路径不查当前 revision 做权限判定**(单集群 `UpgradeAsync` 会查 revision 供 owner 判定)——批量下发已在方法入口强制 Admin,Admin 天然可操作任意 release,省去每集群一次 status 之外的 revision 查询;预检 status 调用顺手解析出 revision,仅用于日志。
7. **统一 values 语义 = `-f` 重新提交**(`includeValues: HasValues(values)`,不使用 `--reuse-values`),与用户确认的「统一 values」口径一致;values 为空白时 install/upgrade 均不带 `-f`。
8. **命名空间为文本输入而非下拉**——批量下发无法为每个目标集群预查命名空间列表(开销 N 次 K8s 调用且语义割裂),以 DNS-1123 校验 + 「不存在时自动创建」选项替代;与单集群安装对话框的下拉形态有差异,是批量语义的合理取舍。
9. **目标集群数据源用 `GetAllForDashboardAsync`(无跟踪 + 含分组)后按所选 Id 过滤**——既完成存在性校验又拿到展示名与状态,不引入新仓库方法。
10. **对话框两态复用 `NodeDrainDialog` 的骨架**:表单态(上传 + 字段 + 集群多选)→ 执行态(进度条 + 已完成/总数)→ 汇总态(结果表 + 成功/失败计数);`MaxWidth.Large`。

## Risks / Trade-offs

- [并发 4 × `--wait` 300s,慢集群拖长整批时长] → 进度逐集群即时回报,对话框始终可见进展;`--wait` 默认关闭,用户可自行权衡
- [预检与下发之间存在竞态(预检不存在 → 他人恰好在窗口期先装)] → helm install 撞名会以非零退出码失败,经既有翻译链路落入该集群的失败项,可重试整批;不做预检锁
- [大 chart(50MB 上限)× 多集群时临时目录瞬时占用] → `HelmCliRunner` 调用结束即删目录(finally),瞬时峰值 ≈ 并发数 × 单包大小,可接受
- [批量下发期间 circuit 断线] → 归属/审计逐集群即时落库,断线不丢已完成集群的记账;剩余集群自然中断(进程随电路终止),与「刷新全部」同风险面
- [SQLite DbContext 并发写] → 归属 upsert 与审计写入均发生在并行体内;audit 内部静默容错、ownership 失败仅告警,不会互相炸;但写并发与既有「刷新全部」快照并发写同构,如出现并发冲突沿用其降级口径(告警不中断),不在本期扩大修复

## Migration Plan

无 schema 变更,部署即用。回滚 = 还原代码,无数据残留(新增数据仅有归属记录与审计行,与单集群安装兼容)。

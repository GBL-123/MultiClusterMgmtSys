# Spec Delta

## ADDED Requirements

### Requirement: 探测采集节点健康快照

系统 SHALL 在每次成功探测集群时,除既有 `Status`/`Version`/`NodeCount`/`LastCheckedAt` 外,额外采集该集群的节点就绪统计,并向节点健康快照表**追加**一条记录,记录集群标识、采集时间、节点总数、就绪数与未就绪数。

节点就绪统计 SHALL 以节点列表调用返回的节点条件为准:就绪 = `Ready` 条件状态为 `True`;未就绪 = `Ready` 条件状态为 `False` 或 `Unknown`,以及缺失 `Ready` 条件的节点。SHALL 满足「节点总数 = 就绪数 + 未就绪数」。

采集 SHALL 复用探测已发起的节点列表调用结果,SHALL NOT 为此新增任何 Kubernetes API 调用。

探测失败或停机取消时 SHALL NOT 追加快照记录;既有降级语义(置 `Offline`、清空 `Version`、`NodeCount` 置 0、更新 `LastCheckedAt`)与取消语义 SHALL 保持不变。

快照记录 SHALL 只追加,SHALL NOT 被后续探测覆盖或修改。集群被删除时其节点健康快照 SHALL 一并删除。

#### Scenario: 成功探测追加快照

- **WHEN** 某集群探测成功且其节点列表返回 8 个节点
- **THEN** 系统追加一条节点健康快照,含集群标识、采集时间与总数 8

#### Scenario: 节点就绪统计口径

- **WHEN** 某集群 8 个节点中 6 个 `Ready` 条件为 `True`、1 个为 `False`、1 个缺失 `Ready` 条件
- **THEN** 该快照记录就绪数为 6、未就绪数为 2,且总数等于两者之和

#### Scenario: 不新增 Kubernetes API 调用

- **WHEN** 一次集群探测成功并写入节点健康快照
- **THEN** 本次探测发起的 Kubernetes API 调用与改动前一致,未额外调用节点列表接口

#### Scenario: 探测失败不追加快照

- **WHEN** 某集群探测时 Kubernetes API 不可达
- **THEN** 不追加任何快照记录,且该集群仍按既有语义置为 `Offline`、`Version` 清空、`NodeCount` 置 0、`LastCheckedAt` 更新

#### Scenario: 停机取消不追加快照

- **WHEN** 某集群的探测因宿主停机取消而中止
- **THEN** 不追加任何快照记录,且该集群的 `Status`/`Version`/`NodeCount`/`LastCheckedAt` 保持不变

#### Scenario: 快照只追加不覆盖

- **WHEN** 同一集群连续多轮探测成功
- **THEN** 每轮各追加一条快照记录,既有记录不被覆盖或修改,可按采集时间区分

#### Scenario: 集群删除级联删除快照

- **WHEN** 管理员删除某集群
- **THEN** 该集群的全部节点健康快照记录一并删除

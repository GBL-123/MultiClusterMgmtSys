# Tasks

## 1. 枚举与请求契约

- [x] 1.1 `Domain/Enums/AuditAction.cs` 新增「封锁/解封/排空」三个枚举值并补中文 XML 注释,验证 CS1591 零命中
- [x] 1.2 `Application/Requests/` 新增 `NodeDrainRequest`(ClusterId/NodeName)与排空预检/执行结果投影所用键对象;`Application/ViewModels/Models` 视需要新增排空汇总 ViewModel(成功/跳过/阻塞计数 + 阻塞 Pod 清单),验证 `dotnet build` 通过

## 2. Eviction Mock 与签名核实

- [x] 2.1 用 sigtool 反查 KubernetesClient 19 的 `CreateNamespacedEviction*WithHttpMessagesAsync` 签名(policy/v1),在 `TestInfrastructure/K8sMocks` 增加 eviction mock setup 扩展段(照抄段注释参数顺序),验证测试项目编译通过
- [x] 2.2 K8sMocks 增加 429(PDB 拦截)的 `KubernetesException(new V1Status{Code=429})` 便捷 setup,验证能注入阻塞场景

## 3. 服务层:封锁/解封

- [x] 3.1 `ClusterNodeService` 新增 CordonAsync/UncordonAsync:Admin 强制(失败 LogWarning + `PermissionException` 中文)、经 `IClusterClientCache` PATCH `spec.unschedulable`、成功后写审计(类别「节点」、操作封锁/解封、目标含集群与节点名),验证新单测:权限拒绝不调 K8s、成功写审计、异常走翻译链路
- [x] 3.2 封锁/解封失败路径测试:K8s 404/403 经 mock 抛 KubernetesException 验证翻译为对应业务异常,验证测试全绿

## 4. 服务层:排空

- [x] 4.1 预检方法:列节点全部 Pod,按 owner 分类(控制器/DaemonSet/裸 Pod),返回预检 ViewModel,验证单测:三种分类各命中、预检失败抛业务异常不写审计
- [x] 4.2 排空执行:先封锁自身(已封锁不重复审计)→ 逐 Pod 串行 Eviction → 429 计阻塞不中断 → 其余异常累计 → 汇总「成功 n/跳过 m/阻塞 k」+ 阻塞清单,写排空审计,验证单测覆盖:尽力迁移、DaemonSet 跳过、裸 Pod 不驱逐、已封锁不重复封锁审计、PDB 429 场景
- [x] 4.3 排空进度:`IProgress<...>` 逐 Pod 回报(完成 k/总数 n),验证单测中进度序列断言

## 5. Web:入口与徽标

- [x] 5.1 节点列表行:Admin 条件渲染封锁/解封/排空入口(按 Unschedulable 条件二态),Member 不渲染;行内操作按惯例包 stopPropagation 包装,验证编译 + 现有 bUnit 节点页测试全绿
- [x] 5.2 「已封锁」小徽标:列表名称相邻与详情工具栏按 Unschedulable 条件渲染(中英词汇走 display-conventions),验证 bUnit:封锁节点渲染徽标、未封锁不渲染
- [x] 5.3 封锁/解封交互:轻量确认(封锁)/直接执行(解封),成功后刷新节点数据,验证 bUnit 点击接线与服务调用(DOM Click + provider 冲刷,不 await Eviction 链)
- [x] 5.4 排空对话框:预检数据展示(迁移清单/DaemonSet 跳过标注/裸 Pod 风险文案)→ 确认后调用排空服务方法,进度展示「正在迁移 k/n」,完成汇总 + 错误走 ExHandler,验证 bUnit 渲染断言
- [x] 5.5 集群不可达时禁用维护入口,验证 bUnit 禁用态断言

## 6. 审计与收尾

- [x] 6.1 审计口径核对:Category=节点、Action=封锁/解封/排空、目标含集群与节点名、排空描述带三计数;预检失败与权限拒绝不写审计,验证服务层已有断言覆盖
- [x] 6.2 `openspec validate --strict` 校验 change 零错误
- [x] 6.3 全量回归:`dotnet build` 0 错误 + `dotnet test` 全绿(MTP,基线 1111+)+ `./coverage.ps1` ≥75% 门禁通过

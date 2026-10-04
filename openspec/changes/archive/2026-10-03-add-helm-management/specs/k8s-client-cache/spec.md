# Spec Delta

## ADDED Requirements

### Requirement: Helm CLI 子进程的缓存边界

本契约适用于进程内经 KubernetesClient 发起、经 `IClusterClientCache` 获取客户端的调用;Helm CLI 子进程自行建立集群连接,SHALL NOT 经客户端缓存与工厂创建,亦不受本契约约束。该边界 SHALL NOT 改变进程内调用"仅经缓存获取客户端"的既有约束。

#### Scenario: Helm 操作不触碰缓存

- **WHEN** 用户执行 Helm 安装、升级、回滚、卸载或 release 列表读取
- **THEN** 客户端缓存不新增、不复用任何条目,且进程内 K8s 调用仍全部经缓存进行

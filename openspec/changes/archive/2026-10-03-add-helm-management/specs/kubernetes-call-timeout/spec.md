# Spec Delta

## ADDED Requirements

### Requirement: Helm CLI 子进程的超时边界

本契约的 10 秒统一超时适用于进程内经 KubernetesClient 发起的 REST 调用;Helm CLI 子进程自身发起的集群访问 SHALL NOT 受该 10 秒限制,而 SHALL 采用 Helm 自身的 `--timeout` 与进程级超时上限;上述操作 SHALL 仍为有界,SHALL NOT 无限期等待。

#### Scenario: 等待就绪超过 10 秒

- **WHEN** 用户选择等待就绪安装,Helm 操作持续超过 10 秒
- **THEN** 该操作不因 10 秒契约被中断,而在 Helm 与进程级超时约束内完成或超时失败

#### Scenario: 进程级上限兜底

- **WHEN** Helm 子进程因异常不退出
- **THEN** 进程级超时终止该操作,不无限等待

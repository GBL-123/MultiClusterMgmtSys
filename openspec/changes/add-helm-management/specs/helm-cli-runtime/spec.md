# Spec Delta

## Purpose

定义系统内 Helm CLI 的运行时契约:二进制随部署产物分发与版本钉定、子进程执行的安全与有界性、凭据物化与临时文件生命周期、运行环境隔离,以及命令构建与结果解析的可测试分层。

## ADDED Requirements

### Requirement: Helm 二进制分发与可配置路径

系统 SHALL 随部署产物(容器镜像)分发钉定版本的 Helm 4.x 二进制;二进制路径 SHALL 可经配置覆盖(开发机可回落到 PATH 查找);二进制缺失或不可执行时 SHALL 以明确中文错误提示,SHALL NOT 导致进程崩溃。

#### Scenario: 镜像内可用

- **WHEN** 检查生产镜像并执行 `helm version`
- **THEN** 输出钉定的 Helm 4.x 版本

#### Scenario: 路径可覆盖

- **WHEN** 配置指定了自定义 helm 路径
- **THEN** 系统使用该路径执行,而非 PATH 查找结果

#### Scenario: 二进制缺失

- **WHEN** helm 二进制不存在或不可执行,用户发起任意 Helm 操作
- **THEN** 系统提示说明 helm 不可用的中文错误,页面不崩溃

### Requirement: 子进程执行安全与有界性

系统 SHALL 以参数表方式启动 Helm 子进程,SHALL NOT 经 shell 拼接命令;SHALL 捕获标准输出、标准错误与退出码;每次执行 SHALL 施加进程级超时并支持取消,超时或取消时 SHALL 终止整个进程树;任何 Helm 操作 SHALL NOT 无限期等待。非零退出码 SHALL 翻译为既有异常体系中的中文业务异常。

#### Scenario: 取消终止进程树

- **WHEN** Helm 操作执行中用户取消或应用停机
- **THEN** Helm 进程及其子进程被终止,不遗留孤儿进程

#### Scenario: 进程级超时兜底

- **WHEN** Helm 子进程超过进程级上限仍未结束
- **THEN** 进程树被终止并以中文超时类错误提示

#### Scenario: 非零退出翻译

- **WHEN** Helm 以非零退出码结束
- **THEN** 依据其输出映射为中文业务异常(冲突/校验/权限/未找到/不可达等),不直出原始输出

### Requirement: 凭据物化与临时文件生命周期

系统 SHALL 将集群连接凭据物化为 Helm 可消费的临时 kubeconfig:KubeConfig 型连接直写既有文本,Token 型连接合成包含 API 地址、Token 与跳过 TLS 校验设置的 kubeconfig。chart 包与 values SHALL 以受管临时文件承载。全部临时文件 SHALL 在操作结束(成功、失败、取消)后清理,SHALL NOT 将凭据内容写入日志。

#### Scenario: Token 型合成 kubeconfig

- **WHEN** 对 Token 型连接的集群执行 Helm 操作
- **THEN** 子进程使用合成出的 kubeconfig 成功连接,临时文件在操作结束后删除

#### Scenario: 失败仍清理

- **WHEN** Helm 操作以失败或取消结束
- **THEN** 该次操作产生的全部临时文件被删除

#### Scenario: 凭据不落日志

- **WHEN** 检查 Helm 操作相关日志与异常信息
- **THEN** 不包含 kubeconfig、Token 等凭据内容

### Requirement: 运行环境隔离

Helm 子进程 SHALL 使用应用管理的可写临时目录作为缓存、配置与数据目录(不依赖容器内不可写的用户主目录),并经 KUBECONFIG 指向本次操作的 kubeconfig;并发 Helm 操作 SHALL 环境互不污染、互不干扰。

#### Scenario: 容器内非 root 可执行

- **WHEN** 应用以非 root 用户运行且用户主目录不可写
- **THEN** Helm 操作仍正常执行,缓存与配置写入受管临时目录

#### Scenario: 并发互不干扰

- **WHEN** 两个用户同时对不同 release 执行 Helm 操作
- **THEN** 两次操作各自使用独立的凭据与临时文件,互不覆盖

### Requirement: 命令构建与结果解析可测试

Helm 命令的参数构建与结果解析 SHALL 与真实进程执行分离,可在不启动 helm 进程的前提下被单元测试;进程执行 SHALL 经应用层端口与基础设施实现分层。

#### Scenario: 无进程测试

- **WHEN** 运行 Helm 服务层单元测试
- **THEN** 测试以假执行结果驱动,断言生成的参数与解析出的视图模型,不启动真实 helm 进程

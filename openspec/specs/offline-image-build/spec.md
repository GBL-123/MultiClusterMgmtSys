# offline-image-build Specification

## Purpose

定义镜像构建的离线能力契约:构建工具链基础镜像的内容与版本钉定、主 Dockerfile 对它的复用、离线机器零网络构建、构建脚本对基础镜像的自动准备,以及离线搬迁包的构成与验证口径。

## Requirements

### Requirement: 构建工具链基础镜像

系统 SHALL 提供构建基础镜像定义(`Dockerfile.builder`,产出 tag `mcms-builder:10.0`),内置 .NET 10 SDK、node 22、钉定版本的 Helm 4.x 二进制(下载时 SHALL 校验官方 sha256),并按已入库的 package-lock 与项目 csproj 预热 npm 依赖缓存与 NuGet 全局包目录。

#### Scenario: 工具链可用

- **WHEN** 在 builder 镜像内执行 `dotnet --info`、`node -v`、`helm version`
- **THEN** 三者均可用,且 helm 输出钉定的 Helm 4.x 版本

#### Scenario: 包缓存预热

- **WHEN** builder 镜像构建完成后检视 npm 缓存与 NuGet 全局包目录
- **THEN** 均已按 package-lock.json 与 csproj 预置对应包

### Requirement: 主 Dockerfile 复用构建基础镜像

主 Dockerfile 的编译阶段 SHALL 基于构建基础镜像,SHALL NOT 保留独立的 node 镜像阶段与 helm 下载阶段;图表 bundle SHALL 在编译阶段内构建;最终运行镜像 SHALL 保持既有内容(两个 vite bundle、helm 二进制、publish 产物)并仍以 aspnet 运行时为基础。

#### Scenario: 产物等价

- **WHEN** 对比重构前后最终镜像内的 `/app/wwwroot/js` 两个 bundle 与 `/usr/local/bin/helm`
- **THEN** 文件构成一致,体积差异仅在构建噪音范围内

#### Scenario: helm 随镜像分发

- **WHEN** 在重构后的生产镜像内执行 `helm version`
- **THEN** 输出钉定的 Helm 4.x 版本(满足 `helm-cli-runtime` 既有分发要求)

### Requirement: 离线零网络构建

在已 load 构建基础镜像与 aspnet 运行时基础镜像的离线机器上,SHALL 能完成主镜像构建,构建过程 SHALL NOT 访问任何 registry、get.helm.sh、npm registry 或 NuGet 源。

#### Scenario: 离线重建

- **WHEN** 离线机器上运行构建脚本重建主镜像
- **THEN** 构建成功且全程无 registry 与包管理器网络访问

#### Scenario: 本地 FROM 解析

- **WHEN** 构建引用仅存在于本地镜像仓库的 `mcms-builder:10.0`
- **THEN** FROM 解析不依赖任何 registry 可达

### Requirement: 构建脚本自动准备基础镜像

构建脚本 SHALL 在构建主镜像前检查本地是否存在 `mcms-builder:10.0`,缺失时 SHALL 自动先构建它;既有参数与镜像导出行为 SHALL 保持不变。

#### Scenario: 缺失自动构建

- **WHEN** 本地没有 `mcms-builder:10.0` 时运行构建脚本
- **THEN** 脚本先构建基础镜像,再构建并导出主镜像

#### Scenario: 已存在直接复用

- **WHEN** 基础镜像已存在于本地
- **THEN** 脚本跳过基础镜像构建,直接进入主镜像构建

### Requirement: 离线搬迁包

文档 SHALL 定义离线搬迁包构成(基础镜像 tar + 主镜像 tar 两个文件)与新机器验证口径(load 后离线重建 + 镜像内冒烟检查)。

#### Scenario: 搬迁可复现

- **WHEN** 按文档在新机器 load 两个 tar 后离线执行构建
- **THEN** 构建成功,产出与源机器语义一致的主镜像

### Requirement: 基础镜像版本续命

升级 SDK、node 或 helm 任一钉定版本 SHALL 仅需修改基础镜像定义并重建基础镜像;主 Dockerfile SHALL NOT 因此需要结构性变更。

#### Scenario: 版本升级路径

- **WHEN** 更新钉定版本后重建基础镜像并重建主镜像
- **THEN** 新版本在主镜像内生效,主 Dockerfile 阶段结构不变

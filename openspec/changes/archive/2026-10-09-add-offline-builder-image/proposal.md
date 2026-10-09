# Proposal

## Why

离线/受污染网络环境下镜像构建不可复现:每次 build 都要向 registry 解析 FROM metadata(docker.io 的 `node:22-alpine` 因 DNS 污染反复超时卡死),层缓存未命中时还需联 get.helm.sh / npm / NuGet 三处下载。需要一个预置完整工具链(.NET SDK + node + helm + npm/NuGet 包缓存)的**构建基础镜像**,离线机器 load 少量 tar 后即可零网络完成构建。

## What Changes

- 新增 `MultiClusterMgmtSys.Web/Dockerfile.builder`,产出 `mcms-builder:10.0`:基于 `mcr.microsoft.com/dotnet/sdk:10.0`,内置 node 22(nodesource 脚本安装)、钉版 helm v4.3.0(sha256 校验)、npm 缓存预热(COPY package.json + lock → npm ci)、NuGet 全局包预热(COPY 4 个 csproj → dotnet restore,包落全局包目录)
- 主 Dockerfile 重构:`helm`/`charts` 两阶段删除;build 阶段改为 `FROM mcms-builder:10.0`,vite 图表 bundle 在 builder 内直接构建;final 仍基于 `aspnet:10.0`,helm 二进制改从 builder 拷入
- `build-image.ps1`:构建前检查本地 `mcms-builder:10.0`,缺失则自动先构建 builder
- 离线搬迁包收敛为 2 个 tar:`mcms-builder:10.0` + 最终应用镜像,附文档化验证命令(新机 load 后构建全程零网络)
- AGENTS.md Docker 节同步更新

## Capabilities

### New Capabilities

- `offline-image-build`: 离线镜像构建契约 —— builder 基础镜像内容与版本钉定、主 Dockerfile 对 builder 的复用、离线机器零网络构建、构建脚本对 builder 的自动准备、搬迁包构成与验证口径

### Modified Capabilities

(无 —— `helm-cli-runtime` 的「Helm 二进制随镜像分发且钉版」要求由 builder 路径同样满足,行为不变;运行时镜像内容不变,`docker-compose.prod.yml` 不动。)

## Impact

- 新文件:`MultiClusterMgmtSys.Web/Dockerfile.builder`
- 修改:`MultiClusterMgmtSys.Web/Dockerfile`(阶段重排)、`build-image.ps1`(builder 自动准备)、AGENTS.md(Docker 节)
- 不变:应用代码与测试、csproj 构建链(`-p:SkipVite=true` 路径保留)、docker-compose.prod.yml、最终镜像运行时内容(两个 vite bundle + helm + publish 产物)
- 代价:builder 镜像需人工续命(升级 SDK/node/helm 版本时重建一次);本地多一个约 2GB 级的 builder 镜像

# Tasks: add-offline-builder-image

## 1. 构建工具链镜像 Dockerfile.builder

- [x] 1.1 新建 `MultiClusterMgmtSys.Web/Dockerfile.builder`(FROM mcr.microsoft.com/dotnet/sdk:10.0;nodesource setup_22.x + apt 安装 nodejs;curl 下载 helm v4.3.0 钉版 + sha256sum 校验 + tar 解包 /usr/local/bin;COPY package.json+package-lock.json → npm ci 预热缓存;COPY 4 个生产 csproj → dotnet restore 预热 NuGet 全局包;ARG HELM_VERSION/TARGETARCH 口径沿用主 Dockerfile 现行写法;UTF-8 无 BOM 但仅 ASCII 字符或加 BOM,避免 PS 5.1 解析坑),验证 `docker build -f MultiClusterMgmtSys.Web/Dockerfile.builder -t mcms-builder:10.0 .` 成功
- [x] 1.2 验证 builder 内工具链可用:`docker run --rm mcms-builder:10.0 sh -c "dotnet --version && node --version && npm --version && helm version"` 输出 dotnet 10.x / node 22.x / helm v4.3.0
- [x] 1.3 验证缓存预热生效:builder 内 `ls ~/.npm/_cacache` 非空、`ls ~/.nuget/packages` 含 KubernetesClient/MudBlazor 等包目录

## 2. 主 Dockerfile 重构

- [x] 2.1 重构 `MultiClusterMgmtSys.Web/Dockerfile`:删除 helm 阶段与 charts 阶段;build 阶段 FROM mcms-builder:10.0,阶段内 dotnet restore → COPY 源码 → npm ci --prefer-offline → COPY vite.config.mjs/vite.topology.mjs/package.json/Assets/Scripts → npm run build(产物落 wwwroot/js)→ dotnet build -c Release -p:SkipVite=true;publish 阶段不变;final 阶段 `COPY --from=builder /usr/local/bin/helm /usr/local/bin/helm`;base 阶段不动,验证 `docker build` 0 错误完成
- [x] 2.2 产物等价性 smoke:`docker run --rm --entrypoint ls multiclustermgmtsys:<Tag> -l /app/wwwroot/js` 包含 dashboard-trend.js/topology-graph.js/reconnect.js/show-password.js 及 .br/.gz 变体;`docker run --rm --entrypoint helm multiclustermgmtsys:<Tag> version` 正常;文件大小与 b17 基线同量级

## 3. build-image.ps1 自动准备

- [x] 3.1 修改 `build-image.ps1`:主构建前 `docker image inspect mcms-builder:10.0` 检查,不存在则先 `docker build -f Dockerfile.builder -t mcms-builder:10.0`,存在则输出「复用已有构建基础镜像」提示;-Tag/-Out 参数语义保持;文件保持 UTF-8 BOM 编码,验证删本地 builder 镜像后跑脚本自动重建、再跑直接复用

## 4. 离线验证

- [x] 4.1 BuildKit 离线 FROM 解析实测:本地已有 mcms-builder:10.0 与 aspnet:10.0,重新 `docker build` 主镜像,检查 BuildKit 输出无「load metadata for docker.io/mcr.microsoft.com」拉取行、构建成功;若仍发起 metadata 请求,按 design D3 兜底链处理(digest 钉定或 DOCKER_BUILDKIT=0)并把结论写进 AGENTS.md
- [x] 4.2 网络依赖归零验证:构建日志全程无 get.helm.sh / registry.npmjs.org / api.nuget.org 拉取行(层缓存 + 预热缓存命中);仅 COPY 层与编译日志
- [x] 4.3 save/load 往返:`docker save mcms-builder:10.0 mcr.microsoft.com/dotnet/aspnet:10.0 -o mcms-base-images.tar` + 应用镜像 tar;`docker load` 回灌后再次构建成功(模拟新机搬迁)

## 5. 文档更新

- [x] 5.1 更新 AGENTS.md Docker/prod deploy 节:builder 工作流(Dockerfile.builder → mcms-builder:10.0)、build-image.ps1 自动准备、2-tar 离线搬迁包口径、版本续命(HELM_VERSION/Node/NuGet 变更仅重建 builder)、BuildKit metadata 兜底结论,验证条目与实际实现一致

## 6. 集成验证

- [x] 6.1 `dotnet build MultiClusterMgmtSys.slnx` 0 错误 + `dotnet test MultiClusterMgmtSys.Tests` 全绿(基线 1259;无 C# 行为变更,纯回归确认)
- [x] 6.2 `./build-image.ps1` 端到端成功出 tar(含 builder 自动准备路径),`docker run --rm --entrypoint ls multiclustermgmtsys:<Tag> -l /app/wwwroot/js` 双 bundle 在位,记录最终镜像 tag 与 tar 路径
- [x] 6.3 `build-image.ps1` 新增 `-IncludeBase` 开关 + `build-image.sh` 同步(`--include-base` 任意参数位扫描式解析;附加导出 `mcms-base-images.tar`(mcms-builder:10.0 + aspnet:10.0,aspnet 缺本地 tag 时自动 pull),与应用 tar 组成完整离线搬迁包;ps1 实跑验证双 tar 产出(843.8MB + 137.8MB),sh 经 Git bash 端到端实跑验证

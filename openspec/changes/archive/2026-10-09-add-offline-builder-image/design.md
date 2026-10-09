# Design: add-offline-builder-image

## Context

构建镜像的三个联网依赖(Dockerfile FROM metadata 解析、helm curl 下载、npm ci、dotnet restore)在 DNS 污染/离线环境下不可复现:node:22-alpine 的 docker.io metadata 解析反复超时(auth.docker.io 被污染解析),helm 二进制下载依赖 get.helm.sh 可达。方案是把「构建工具链」整体预装进一个本地基础镜像 `mcms-builder:10.0`,主 Dockerfile 复用它,使构建在本地已有两个基础镜像(builder + aspnet)时零外网访问。

## Goals / Non-Goals

- Goals: 构建工具链镜像化(单次联网预制);主 Dockerfile 复用 builder;构建脚本自动准备;离线搬迁最小化(2 个 tar);版本续命路径清晰。
- Non-Goals: 不改运行时镜像内容(aspnet 基底、db/logs 目录、helm 分发方式、ENTRYPOINT 全部保持);不引入 CI registry/制品库;不做镜像漏洞扫描自动化;不动 docker-compose.prod.yml。

## Decisions

### D1. builder 以 mcr sdk:10.0 为基底,工具链用发行版/官方脚本安装

`Dockerfile.builder` 阶段:FROM `mcr.microsoft.com/dotnet/sdk:10.0`(Debian bookworm,与 build 阶段同源)。

- **Node 22**:走 nodesource 官方安装脚本(`curl -fsSL https://deb.nodesource.com/setup_22.x | bash -` + `apt-get install -y nodejs`),得到 npm。不用 nvm(需 shell profile,容器内多一层间接)。node 版本与 charts 阶段的 node:22-alpine 大版本一致(22.x),vite 8 构建产物等价。
- **Helm 4.3.0**:沿用主 Dockerfile 现行钉版口径(curl get.helm.sh + sha256sum 校验 + tar 解包到 `/usr/local/bin/helm`),builder 里多装一份;主 Dockerfile 的 helm 阶段整体删除,final 阶段改 `COPY --from=builder /usr/local/bin/helm /usr/local/bin/helm`。
- **npm 缓存预热**:COPY `MultiClusterMgmtSys.Web/package.json` + `package-lock.json` → `npm ci`(产物进 npm 全局缓存 `~/.npm`),后续构建里 `npm ci` 优先走缓存,仅 lock 变更时联网。
- **NuGet 全局包预热**:COPY 4 个生产 csproj(Domain/Application/Infrastructure/Web)→ `dotnet restore`,包落 `$HOME/.nuget/packages`;后续构建 restore 命中全局包目录,零网络。restore 失败应让 builder 构建失败(预热失败 = 离线构建不可达,早失败优于静默半缓存)。
- **tag**:固定 `mcms-builder:10.0`(与 sdk 大版本对齐,不跟 patch 版走)。

### D2. 主 Dockerfile 重构:charts/helm 两阶段消失,build 阶段 FROM mcms-builder

- charts 阶段(现 L20-27)整体删除;其职责(npm ci + COPY vite 配置与 Assets/Scripts + npm run build)并入 build 阶段。
- helm 阶段(现 L7-15)整体删除。
- build 阶段 FROM 改 `mcms-builder:10.0`;阶段内顺序:restore(命中全局包)→ COPY 源码 → npm ci(命中缓存)→ COPY vite 配置与脚本源 → `npm run build`(产物直接落 wwwroot/js,**不再需要 `-p:SkipVite=true`**,也不再需要 `COPY --from=charts`)→ `dotnet build -c Release -p:SkipVite=true`(SkipVite 保留,避免宿主 MSBuild 再触发 charts target 重复构建)。
- base/publish/final 三阶段结构不动;final 的 `COPY --from=helm` 改 `COPY --from=builder`。
- 产物等价性以「镜像内文件清单 + 大小」验证:final 镜像内 `/app/wwwroot/js/{dashboard-trend,topology-graph,reconnect,show-password}.js` 齐全(含 .br/.gz 预压缩变体)、`/usr/local/bin/helm` 存在且可执行、ENTRYPOINT 不变。

### D3. BuildKit 离线 FROM 解析的不确定性,显式记录 + 验证兜底

已知实证(b17/m0367):本地 retag 的镜像不一定让 BuildKit 跳过 registry metadata 解析——BuildKit 默认 `image-resolve-mode` 倾向于校验远端 manifest(除非 `--pull=false` 显式本地优先;Docker build 默认即 pull=false,但 metadata 仍可能请求 registry)。缓解手段按序:

1. 主构建命令保持默认(`docker build -f ...`),验证任务里实测「拔网线式」模拟:禁用除本地 daemon 外的网络后构建,观察是否零 registry 请求。
2. 若实测仍发起 metadata 请求:记录 aspnet:10.0 / sdk:10.0 的 digest(`docker images --digests`),Dockerfile.builder 的 FROM 可改 digest 钉定(如 `mcr.microsoft.com/dotnet/sdk:10.0@sha256:...`)。
3. 终极兜底:`DOCKER_BUILDKIT=0 docker build`(经典 builder 直接用本地镜像,不做 metadata 解析)写入 AGENTS.md 的已知 workaround。

此不确定性是设计风险而非实现缺口,tasks 里有对应验证项;若 digest 钉定被采用,写入 Dockerfile.builder 注释。

### D4. npm 离线口径:预热缓存 + `--prefer-offline`

builder 内 `npm ci` 预热后,主构建的 `npm ci` 加 `--prefer-offline`(缓存命中即不发起 registry 请求)。lock 文件变更时会自然回退联网拉新包——这是预期行为,不属于离线破坏。

### D5. NuGet 离线口径:全局包目录 + restore 幂等

`dotnet restore` 命中 `$HOME/.nuget/packages` 时零网络。csproj 包版本变更 → builder 需重建(spec ⑥ 版本续命条款)。不在 builder 里做 `--source` 禁网开关(过度设计,离线验证任务里用实际构建行为证明)。

### D6. build-image.ps1 自动准备 + 搬迁包

- 脚本前置检查:`docker image inspect mcms-builder:10.0` 失败(镜像不存在)→ 自动 `docker build -f MultiClusterMgmtSys.Web\Dockerfile.builder -t mcms-builder:10.0 $RepoRoot`,再进入主构建;存在则直接复用。`-Tag`/`-Out` 参数与现有行为完全保留。
- 搬迁包口径(写进 AGENTS.md Docker 节):`docker save mcms-builder:10.0 mcr.microsoft.com/dotnet/aspnet:10.0 -o mcms-base-images.tar` + 应用镜像 tar 共 2 个 tar;新机 `docker load` 后按第 4 条契约全流程可离线复现。
- 保留 UTF-8 BOM 编码(b16 的修复成果,不再回退)。

### D7. 验证策略(诚实边界)

- 单元测试不覆盖(纯构建链,无 C# 行为变更;`dotnet build` + 全量 `dotnet test` 全绿作为无回归证据)。
- 验证 = 实际构建:builder 构建成功 → 主镜像构建成功 → 镜像内文件清单 smoke(ls /app/wwwroot/js + helm -v)→ 离线模拟(阻断外部网络的方式在开发机上不可靠,以「构建过程中无 registry 拉取日志 + BuildKit 输出无 fetch 行」作为证据,并保留 D3 的兜底手段)→ docker save/load 往返成功。
- AGENTS.md 更新:Build/targets 节记录 builder 工作流、2-tar 搬迁、版本续命(HELM_VERSION/Node/NuGet 变更 → 仅重建 builder)。

## Risks / Trade-offs

- **builder 需人工续命**(sdk/node/helm 升级或 CVE 修复需重做;spec ⑥ 已成文)。
- **builder 体积 ~2GB**(sdk + node + NuGet 全局包),一次性成本,换取构建确定性。
- **BuildKit metadata 解析行为随版本漂移**(D3 已列兜底链:默认 → digest 钉定 → DOCKER_BUILDKIT=0)。
- **nodesource 脚本一次性联网**(构建 builder 时;主构建不再触网)。
- 主 Dockerfile 里 npm 产物与 dotnet 构建同阶段,阶段缓存粒度变粗(vite 产物层随源码层变化)——构建耗时影响可忽略(本就同次构建)。

## Migration Plan

1. 先建 builder(一次性联网)。
2. 重构主 Dockerfile + build-image.ps1。
3. 全量验证(含 save/load 往返)。
4. 更新 AGENTS.md。
- 回滚:git revert Dockerfile/build-image.ps1/AGENTS.md;builder 镜像不碍事可留可删。

## Open Questions

(无——D3 的 BuildKit 行为不确定性按「实测 + 兜底链」处理,不阻塞设计。)

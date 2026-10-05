# 请参阅 https://aka.ms/customizecontainer 以了解如何自定义调试容器，以及 Visual Studio 如何使用此 Dockerfile 生成映像以更快地进行调试。

# 此阶段用于在快速模式(默认为调试配置)下从 VS 运行时
# Helm CLI 下载阶段:复用构建链中的 SDK 基础镜像,避免额外的 registry 依赖;
# 按 TARGETARCH 下载钉定版本并校验 SHA256,仅把二进制拷入运行镜像。
# 升级步骤:改 HELM_VERSION 为新的 v4.x.y(get.helm.sh 提供配套 .sha256sum 校验文件),重跑构建即可。
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS helm
ARG HELM_VERSION=v4.3.0
ARG TARGETARCH
RUN cd /tmp \
 && curl -fsSLO "https://get.helm.sh/helm-${HELM_VERSION}-linux-${TARGETARCH}.tar.gz" \
 && curl -fsSLO "https://get.helm.sh/helm-${HELM_VERSION}-linux-${TARGETARCH}.tar.gz.sha256sum" \
 && sha256sum -c "helm-${HELM_VERSION}-linux-${TARGETARCH}.tar.gz.sha256sum" \
 && tar -xzf "helm-${HELM_VERSION}-linux-${TARGETARCH}.tar.gz" \
 && cp "linux-${TARGETARCH}/helm" /helm

# 图表 bundle 构建阶段:vite + echarts 独立 node 环境,产物 (wwwroot/js/dashboard-trend.js)
# 在 build 阶段于 dotnet build 之前拷入 wwwroot,随静态资产一起发布。
FROM node:22-alpine AS charts
WORKDIR /charts
COPY ["MultiClusterMgmtSys.Web/package.json", "MultiClusterMgmtSys.Web/package-lock.json", "./"]
# npm install 而非 npm ci:package-lock.json 不入库(.gitignore),fresh clone 无锁文件
RUN npm install --no-audit --no-fund
COPY ["MultiClusterMgmtSys.Web/vite.config.mjs", "./"]
COPY ["MultiClusterMgmtSys.Web/Assets/Scripts/", "./Assets/Scripts/"]
RUN npm run build

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
# SQLite 数据目录 + Serilog 日志目录：需以非 root 用户可写
# （compose 挂载宿主机 ./db 到 /app/db、./logs 到 /app/logs）
RUN mkdir -p /app/db /app/logs && chown -R $APP_UID /app/db /app/logs
USER $APP_UID
EXPOSE 8080
EXPOSE 8081


# 此阶段用于生成服务项目
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MultiClusterMgmtSys.Domain/MultiClusterMgmtSys.Domain.csproj", "MultiClusterMgmtSys.Domain/"]
COPY ["MultiClusterMgmtSys.Application/MultiClusterMgmtSys.Application.csproj", "MultiClusterMgmtSys.Application/"]
COPY ["MultiClusterMgmtSys.Infrastructure/MultiClusterMgmtSys.Infrastructure.csproj", "MultiClusterMgmtSys.Infrastructure/"]
COPY ["MultiClusterMgmtSys.Web/MultiClusterMgmtSys.Web.csproj", "MultiClusterMgmtSys.Web/"]
RUN dotnet restore "./MultiClusterMgmtSys.Web/MultiClusterMgmtSys.Web.csproj"
COPY . .
# 拷入 vite 产物(须在 dotnet build 前,使静态资产清点可见),并跳过宿主 vite target(本镜像无 node)
COPY --from=charts /charts/wwwroot/js ./MultiClusterMgmtSys.Web/wwwroot/js/
WORKDIR "/src/MultiClusterMgmtSys.Web"
RUN dotnet build "./MultiClusterMgmtSys.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build -p:SkipVite=true

# 此阶段用于发布要复制到最终阶段的服务项目
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MultiClusterMgmtSys.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false -p:SkipVite=true

# 此阶段在生产中使用，或在常规模式下从 VS 运行时使用(在不使用调试配置时为默认值)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=helm /helm /usr/local/bin/helm
ENTRYPOINT ["dotnet", "MultiClusterMgmtSys.Web.dll"]
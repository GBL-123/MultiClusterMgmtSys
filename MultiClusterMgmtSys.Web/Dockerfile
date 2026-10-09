# 请参阅 https://aka.ms/customizecontainer 以了解如何自定义调试容器，以及 Visual Studio 如何使用此 Dockerfile 生成映像以更快地进行调试。

# 此阶段用于在快速模式(默认为调试配置)下从 VS 运行时
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
# SQLite 数据目录 + Serilog 日志目录：需以非 root 用户可写
# （compose 挂载宿主机 ./db 到 /app/db、./logs 到 /app/logs）
RUN mkdir -p /app/db /app/logs && chown -R $APP_UID /app/db /app/logs
USER $APP_UID
EXPOSE 8080
EXPOSE 8081


# 此阶段用于生成服务项目:基于离线工具链镜像 mcms-builder:10.0(由 Dockerfile.builder 构建,
# build-image.ps1 会自动准备)——内含 dotnet sdk 10 + node 22 + helm 钉版 + npm/NuGet 预热缓存,
# 构建全程无 helm 下载/npm registry/NuGet 源的网络依赖。
FROM mcms-builder:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MultiClusterMgmtSys.Domain/MultiClusterMgmtSys.Domain.csproj", "MultiClusterMgmtSys.Domain/"]
COPY ["MultiClusterMgmtSys.Application/MultiClusterMgmtSys.Application.csproj", "MultiClusterMgmtSys.Application/"]
COPY ["MultiClusterMgmtSys.Infrastructure/MultiClusterMgmtSys.Infrastructure.csproj", "MultiClusterMgmtSys.Infrastructure/"]
COPY ["MultiClusterMgmtSys.Web/MultiClusterMgmtSys.Web.csproj", "MultiClusterMgmtSys.Web/"]
RUN dotnet restore "./MultiClusterMgmtSys.Web/MultiClusterMgmtSys.Web.csproj"
COPY . .
# 图表 bundle(dashboard-trend.js/topology-graph.js)由两份 vite 配置在本阶段编译进 wwwroot/js;
# npm ci --prefer-offline 优先命中 builder 预热缓存,lock 变更时回退联网
RUN cd MultiClusterMgmtSys.Web && npm ci --no-audit --no-fund --prefer-offline
RUN cd MultiClusterMgmtSys.Web && npm run build
WORKDIR "/src/MultiClusterMgmtSys.Web"
# -p:SkipVite=true 跳过宿主 MSBuild BuildChartBundle target(bundle 已在上方 npm run build 产出)
RUN dotnet build "./MultiClusterMgmtSys.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build -p:SkipVite=true

# 此阶段用于发布要复制到最终阶段的服务项目
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MultiClusterMgmtSys.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false -p:SkipVite=true

# 此阶段在生产中使用，或在常规模式下从 VS 运行时使用(在不使用调试配置时为默认值)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# helm 二进制来自 builder 镜像的钉版安装(版本升级仅重建 builder)
COPY --from=build /usr/local/bin/helm /usr/local/bin/helm
ENTRYPOINT ["dotnet", "MultiClusterMgmtSys.Web.dll"]

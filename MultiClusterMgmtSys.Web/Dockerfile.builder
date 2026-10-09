# Builder base image: dotnet sdk 10 + node 22 + helm CLI + warm package caches.
# Downstream app image builds (MultiClusterMgmtSys.Web/Dockerfile) reuse this image
# so that a build needs zero network access once both base images are local.
# Rebuild THIS image when SDK major version, node major version, HELM_VERSION
# or the package versions (package-lock.json / csproj PackageReference) change.

FROM mcr.microsoft.com/dotnet/sdk:10.0

# Node 22 via the official nodesource script (major version matches the former
# node:22-alpine charts stage; vite output is agnostic to the distro).
RUN curl -fsSL https://deb.nodesource.com/setup_22.x | bash - \
 && apt-get install -y --no-install-recommends nodejs \
 && node --version && npm --version

# Helm CLI pinned version: same source of truth as the app image previously had
# (sha256 verified at build time). Binary lands in /usr/local/bin/helm, which the
# final stage later copies out with COPY --from=builder.
ARG HELM_VERSION=v4.3.0
ARG TARGETARCH
RUN cd /tmp \
 && curl -fsSLO "https://get.helm.sh/helm-${HELM_VERSION}-linux-${TARGETARCH}.tar.gz" \
 && curl -fsSLO "https://get.helm.sh/helm-${HELM_VERSION}-linux-${TARGETARCH}.tar.gz.sha256sum" \
 && sha256sum -c "helm-${HELM_VERSION}-linux-${TARGETARCH}.tar.gz.sha256sum" \
 && tar -xzf "helm-${HELM_VERSION}-linux-${TARGETARCH}.tar.gz" \
 && cp "linux-${TARGETARCH}/helm" /usr/local/bin/helm \
 && helm version

# npm cache warm-up: install locked deps once; later `npm ci --prefer-offline`
# in the app build resolves from ~/.npm without touching the registry.
WORKDIR /warm
COPY ["MultiClusterMgmtSys.Web/package.json", "MultiClusterMgmtSys.Web/package-lock.json", "./"]
RUN npm ci --no-audit --no-fund

# NuGet global-packages warm-up: restore the web project (transitively restores
# Domain/Application/Infrastructure); packages land in ~/.nuget/packages so the
# app build's restore runs fully offline. A failed warm-up fails this build.
WORKDIR /src
COPY ["MultiClusterMgmtSys.Domain/MultiClusterMgmtSys.Domain.csproj", "MultiClusterMgmtSys.Domain/"]
COPY ["MultiClusterMgmtSys.Application/MultiClusterMgmtSys.Application.csproj", "MultiClusterMgmtSys.Application/"]
COPY ["MultiClusterMgmtSys.Infrastructure/MultiClusterMgmtSys.Infrastructure.csproj", "MultiClusterMgmtSys.Infrastructure/"]
COPY ["MultiClusterMgmtSys.Web/MultiClusterMgmtSys.Web.csproj", "MultiClusterMgmtSys.Web/"]
RUN dotnet restore "MultiClusterMgmtSys.Web/MultiClusterMgmtSys.Web.csproj"

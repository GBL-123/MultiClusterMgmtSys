# 触发 Docker 打包项目镜像并导出为 tar 包
#
# 用法：
#   .\build-image.ps1                      # 默认 multiclustermgmtsys:v1.0.0
#   .\build-image.ps1 -Tag v1.1.0          # 指定版本标签
#   .\build-image.ps1 -Tag v1.1.0 -Out .\dist\mcms.tar   # 指定导出路径
#   .\build-image.ps1 -IncludeBase                       # 同时导出离线构建基础镜像包
#
# 镜像命名规则：multiclustermgmtsys:<Tag>（与 docker-compose.prod.yml 保持一致）
#
# -IncludeBase 会额外导出 mcms-base-images.tar（mcms-builder:10.0 + aspnet:10.0，
# 与应用 tar 组成离线搬迁包；新机 docker load 两个 tar 后构建全程零外网）

param(
    [string]$Tag = "v1.0.0",
    [string]$Out = "",
    [switch]$IncludeBase
)

$ErrorActionPreference = "Stop"

# 脚本所在目录（仓库根），保证在任何位置执行都从正确上下文构建
$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Dockerfile = Join-Path $RepoRoot "MultiClusterMgmtSys.Web\Dockerfile"

if (-not (Test-Path -LiteralPath $Dockerfile)) {
    Write-Host "[错误] 未找到 Dockerfile: $Dockerfile" -ForegroundColor Red
    exit 1
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "[错误] 未找到 docker 命令，请先安装并启动 Docker。" -ForegroundColor Red
    exit 1
}

# 构建基础镜像（离线构建链）：不存在时自动构建，存在时直接复用
$BuilderDockerfile = Join-Path $RepoRoot "MultiClusterMgmtSys.Web\Dockerfile.builder"
$BuilderImage = "mcms-builder:10.0"

if (-not (Test-Path -LiteralPath $BuilderDockerfile)) {
    Write-Host "[错误] 未找到构建基础镜像定义: $BuilderDockerfile" -ForegroundColor Red
    exit 1
}

$BuilderExists = (docker images --format "{{.Repository}}:{{.Tag}}") -contains $BuilderImage
if (-not $BuilderExists) {
    Write-Host "[信息] 未找到构建基础镜像 $BuilderImage，开始自动构建..." -ForegroundColor Cyan
    docker build -f $BuilderDockerfile -t $BuilderImage $RepoRoot
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[错误] 构建基础镜像构建失败。" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "[成功] 构建基础镜像已就绪: $BuilderImage" -ForegroundColor Green
}
else {
    Write-Host "[信息] 检测到构建基础镜像已存在，直接复用: $BuilderImage" -ForegroundColor Cyan
}

$ImageName = "multiclustermgmtsys:$Tag"
Write-Host "[信息] 开始构建镜像: $ImageName (上下文: $RepoRoot)" -ForegroundColor Cyan

docker build -f $Dockerfile -t $ImageName $RepoRoot
if ($LASTEXITCODE -ne 0) {
    Write-Host "[错误] 镜像构建失败。" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "[成功] 镜像构建完成: $ImageName" -ForegroundColor Green

if ($Out -eq "") {
    $Out = Join-Path $RepoRoot "multiclustermgmtsys-$Tag.tar"
} else {
    $OutDir = Split-Path -Parent $Out
    if ($OutDir -and -not (Test-Path -LiteralPath $OutDir)) {
        New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
    }
}

Write-Host "[信息] 开始导出镜像: $ImageName -> $Out" -ForegroundColor Cyan
docker save -o $Out $ImageName
if ($LASTEXITCODE -ne 0) {
    Write-Host "[错误] 镜像导出失败。" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "[成功] 镜像导出完成: $Out" -ForegroundColor Green

if ($IncludeBase) {
    $BaseOut = Join-Path (Split-Path -Parent $Out) "mcms-base-images.tar"
    $RuntimeImage = "mcr.microsoft.com/dotnet/aspnet:10.0"

    # aspnet 运行时基础镜像可能仅有 BuildKit 解析用的 digest、未落地本地 tag，缺则先拉取
    $ImageList = @(docker images --format "{{.Repository}}:{{.Tag}}")
    if (-not ($ImageList -contains $RuntimeImage)) {
        Write-Host "[信息] 未找到运行时基础镜像 $RuntimeImage，开始拉取..." -ForegroundColor Cyan
        docker pull $RuntimeImage
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[错误] 运行时基础镜像拉取失败。" -ForegroundColor Red
            exit $LASTEXITCODE
        }
    }

    Write-Host "[信息] 开始导出离线构建基础镜像包: $BuilderImage + $RuntimeImage -> $BaseOut" -ForegroundColor Cyan
    docker save -o $BaseOut $BuilderImage $RuntimeImage
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[错误] 基础镜像包导出失败。" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host "[成功] 基础镜像包导出完成: $BaseOut" -ForegroundColor Green
}
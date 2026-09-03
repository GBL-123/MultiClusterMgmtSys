# 触发 Docker 打包项目镜像并导出为 tar 包
#
# 用法：
#   .\build-image.ps1                      # 默认 multiclustermgmtsys:v1.0.0
#   .\build-image.ps1 -Tag v1.1.0          # 指定版本标签
#   .\build-image.ps1 -Tag v1.1.0 -Out .\dist\mcms.tar   # 指定导出路径
#
# 镜像命名规则：multiclustermgmtsys:<Tag>（与 docker-compose.prod.yml 保持一致）

param(
    [string]$Tag = "v1.0.0",
    [string]$Out = ""
)

$ErrorActionPreference = "Stop"

# 脚本所在目录（仓库根），保证在任何位置执行都从正确上下文构建
$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Dockerfile = Join-Path $RepoRoot "MultiClusterMgmtSys\Dockerfile"

if (-not (Test-Path -LiteralPath $Dockerfile)) {
    Write-Host "[错误] 未找到 Dockerfile: $Dockerfile" -ForegroundColor Red
    exit 1
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "[错误] 未找到 docker 命令，请先安装并启动 Docker。" -ForegroundColor Red
    exit 1
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
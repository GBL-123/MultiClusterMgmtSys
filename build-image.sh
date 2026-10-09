#!/usr/bin/env bash
# 触发 Docker 打包项目镜像并导出为 tar 包
#
# 用法：
#   ./build-image.sh                           # 默认 multiclustermgmtsys:v1.0.0
#   ./build-image.sh v1.1.0                    # 指定版本标签
#   ./build-image.sh v1.1.0 ./dist/mcms.tar    # 指定导出路径
#   ./build-image.sh v1.1.0 --include-base     # 同时导出离线构建基础镜像包(--include-base 放任意参数位)
#
# 镜像命名规则：multiclustermgmtsys:<Tag>（与 docker-compose.prod.yml 保持一致）
#
# --include-base 会额外导出 mcms-base-images.tar（mcms-builder:10.0 + aspnet:10.0，
# 与应用 tar 组成离线搬迁包；新机 docker load 两个 tar 后构建全程零外网）

set -euo pipefail

# 参数解析：--include-base 为开关，其余位置参数依次为 TAG、OUT
INCLUDE_BASE=0
POSITIONAL=()
for arg in "$@"; do
    case "$arg" in
        --include-base) INCLUDE_BASE=1 ;;
        *) POSITIONAL+=("$arg") ;;
    esac
done
TAG="${POSITIONAL[0]:-v1.0.0}"
OUT="${POSITIONAL[1]:-}"

# 脚本所在目录（仓库根），保证在任何位置执行都从正确上下文构建
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOCKERFILE="$REPO_ROOT/MultiClusterMgmtSys.Web/Dockerfile"

if [ ! -f "$DOCKERFILE" ]; then
    echo "[错误] 未找到 Dockerfile: $DOCKERFILE" >&2
    exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
    echo "[错误] 未找到 docker 命令，请先安装并启动 Docker。" >&2
    exit 1
fi

# 构建基础镜像（离线构建链）：不存在时自动构建，存在时直接复用
BUILDER_DOCKERFILE="$REPO_ROOT/MultiClusterMgmtSys.Web/Dockerfile.builder"
BUILDER_IMAGE="mcms-builder:10.0"

if [ ! -f "$BUILDER_DOCKERFILE" ]; then
    echo "[错误] 未找到构建基础镜像定义: $BUILDER_DOCKERFILE" >&2
    exit 1
fi

if ! docker image inspect "$BUILDER_IMAGE" >/dev/null 2>&1; then
    echo "[信息] 未找到构建基础镜像 $BUILDER_IMAGE，开始自动构建..."
    docker build -f "$BUILDER_DOCKERFILE" -t "$BUILDER_IMAGE" "$REPO_ROOT"
    echo "[成功] 构建基础镜像已就绪: $BUILDER_IMAGE"
else
    echo "[信息] 检测到构建基础镜像已存在，直接复用: $BUILDER_IMAGE"
fi

IMAGE_NAME="multiclustermgmtsys:$TAG"
echo "[信息] 开始构建镜像: $IMAGE_NAME (上下文: $REPO_ROOT)"

docker build -f "$DOCKERFILE" -t "$IMAGE_NAME" "$REPO_ROOT"

echo "[成功] 镜像构建完成: $IMAGE_NAME"

if [ -z "$OUT" ]; then
    OUT="$REPO_ROOT/multiclustermgmtsys-$TAG.tar"
else
    OUT_DIR="$(dirname -- "$OUT")"
    if [ -n "$OUT_DIR" ] && [ ! -d "$OUT_DIR" ]; then
        mkdir -p "$OUT_DIR"
    fi
fi

echo "[信息] 开始导出镜像: $IMAGE_NAME -> $OUT"
docker save -o "$OUT" "$IMAGE_NAME"

echo "[成功] 镜像导出完成: $OUT"

if [ "$INCLUDE_BASE" -eq 1 ]; then
    BASE_OUT="$(dirname -- "$OUT")/mcms-base-images.tar"
    RUNTIME_IMAGE="mcr.microsoft.com/dotnet/aspnet:10.0"

    # aspnet 运行时基础镜像可能仅有 BuildKit 解析用的 digest、未落地本地 tag，缺则先拉取
    if ! docker image inspect "$RUNTIME_IMAGE" >/dev/null 2>&1; then
        echo "[信息] 未找到运行时基础镜像 $RUNTIME_IMAGE，开始拉取..."
        docker pull "$RUNTIME_IMAGE"
    fi

    echo "[信息] 开始导出离线构建基础镜像包: $BUILDER_IMAGE + $RUNTIME_IMAGE -> $BASE_OUT"
    docker save -o "$BASE_OUT" "$BUILDER_IMAGE" "$RUNTIME_IMAGE"

    echo "[成功] 基础镜像包导出完成: $BASE_OUT"
fi
#!/usr/bin/env bash
# 触发 Docker 打包项目镜像并导出为 tar 包
#
# 用法：
#   ./build-image.sh                      # 默认 multiclustermgmtsys:v1.0.0
#   ./build-image.sh v1.1.0               # 指定版本标签
#   ./build-image.sh v1.1.0 ./dist/mcms.tar   # 指定导出路径
#
# 镜像命名规则：multiclustermgmtsys:<Tag>（与 docker-compose.prod.yml 保持一致）

set -euo pipefail

TAG="${1:-v1.0.0}"
OUT="${2:-}"

# 脚本所在目录（仓库根），保证在任何位置执行都从正确上下文构建
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOCKERFILE="$REPO_ROOT/MultiClusterMgmtSys/Dockerfile"

if [ ! -f "$DOCKERFILE" ]; then
    echo "[错误] 未找到 Dockerfile: $DOCKERFILE" >&2
    exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
    echo "[错误] 未找到 docker 命令，请先安装并启动 Docker。" >&2
    exit 1
fi

IMAGE_NAME="multiclustermgmtsys:$TAG"
echo "[信息] 开始构建镜像: $IMAGE_NAME (上下文: $REPO_ROOT)"

docker build -f "$DOCKERFILE" -t "$IMAGE_NAME" "$REPO_ROOT"

echo "[成功] 镜像构建完成: $IMAGE_NAME"

if [ -z "$OUT" ]; then
    OUT="$REPO_ROOT/multiclustermgmtsys-$TAG.tar"
else
    OUT_DIR="$(dirname "$OUT")"
    if [ -n "$OUT_DIR" ] && [ ! -d "$OUT_DIR" ]; then
        mkdir -p "$OUT_DIR"
    fi
fi

echo "[信息] 开始导出镜像: $IMAGE_NAME -> $OUT"
docker save -o "$OUT" "$IMAGE_NAME"

echo "[成功] 镜像导出完成: $OUT"
#!/bin/bash
# =============================================================================
# 一键导出脚本 - 导出 Docker 镜像为 tar 文件（不含 MySQL）
# 
# 用法: cd deploy && bash export.sh
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
cd "$SCRIPT_DIR"

echo "=========================================="
echo " Realso 镜像导出脚本"
echo "=========================================="

if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker 未运行"
    exit 1
fi

OUTPUT_FILE="realso-images.tar"

# 检查镜像
echo ""
echo "🔍 检查 Docker 镜像..."
IMAGES=""
for img in realso-webapi:latest realso-auth:latest realso-admin:latest; do
    if docker image inspect "$img" > /dev/null 2>&1; then
        echo "  ✅ $img 已存在"
        IMAGES="$IMAGES $img"
    else
        echo "  ⚠️  $img 不存在，跳过"
    fi
done

if [ -z "$IMAGES" ]; then
    echo "❌ 没有可用的镜像，请先运行 build.sh"
    exit 1
fi

# 导出镜像
echo ""
echo "📤 导出镜像到 $OUTPUT_FILE ..."
cd "$PROJECT_DIR"
docker save $IMAGES -o "$SCRIPT_DIR/$OUTPUT_FILE"

echo ""
echo "🗜️  压缩镜像文件..."
gzip -f "$SCRIPT_DIR/$OUTPUT_FILE"

FINAL_SIZE=$(du -h "$SCRIPT_DIR/${OUTPUT_FILE}.gz" | cut -f1)

echo ""
echo "=========================================="
echo " ✅ 导出完成！"
echo " 输出文件: ${OUTPUT_FILE}.gz ($FINAL_SIZE)"
echo "=========================================="

# 列出需要传输的文件
echo ""
echo "📋 传输到服务器的文件:"
ls -lh "$SCRIPT_DIR/${OUTPUT_FILE}.gz" \
       "$SCRIPT_DIR/docker-compose.yml" \
       "$SCRIPT_DIR/.env" \
       "$SCRIPT_DIR/deploy-on-server.sh" 2>/dev/null

echo ""
echo " 传输命令:"
echo "   scp ${OUTPUT_FILE}.gz  user@server:/opt/realso/"
echo "   scp docker-compose.yml user@server:/opt/realso/"
echo "   scp .env                user@server:/opt/realso/"
echo "   scp deploy-on-server.sh user@server:/opt/realso/"
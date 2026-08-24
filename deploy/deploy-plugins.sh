#!/bin/bash
# =============================================================================
# 一键部署 OnlyOffice 插件到 Docker 容器
# 
# 用法: cd deploy && bash deploy-plugins.sh
# 前提: OnlyOffice 容器已在运行
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
cd "$SCRIPT_DIR"

CONTAINER="${ONLYOFFICE_CONTAINER:-onlyoffice-ds}"
PLUGINS_SOURCE="$PROJECT_DIR/onlyoffice-plugins"

echo "=========================================="
echo " OnlyOffice 插件部署脚本"
echo "=========================================="

# 检查容器
if ! docker ps --format '{{.Names}}' | grep -q "^${CONTAINER}$"; then
    echo "❌ 容器 $CONTAINER 未运行"
    echo "   请先启动 OnlyOffice 容器:"
    echo "   docker run -d --name onlyoffice-ds -p 8080:80 onlyoffice/documentserver"
    exit 1
fi

# 检查插件目录
if [ ! -d "$PLUGINS_SOURCE" ]; then
    echo "❌ 插件目录不存在: $PLUGINS_SOURCE"
    exit 1
fi

echo "📦 部署插件..."
echo "   容器: $CONTAINER"
echo "   源目录: $PLUGINS_SOURCE"

DEPLOYED=0

for plugin_dir in "$PLUGINS_SOURCE"/*/; do
    if [ ! -d "$plugin_dir" ]; then
        continue
    fi

    plugin_name=$(basename "$plugin_dir")
    echo ""
    echo "   部署插件: $plugin_name"

    PLUGIN_PATH="/var/www/onlyoffice/documentserver/sdkjs-plugins/$plugin_name"

    # 拷贝文件
    for file in "$plugin_dir"*; do
        fname=$(basename "$file")
        docker cp "$file" "$CONTAINER:$PLUGIN_PATH/$fname" 2>/dev/null || \
            docker cp "$file" "$CONTAINER:$PLUGIN_PATH/" 2>/dev/null || true
    done

    # 重新生成 gzip
    docker exec "$CONTAINER" bash -c "cd $PLUGIN_PATH && rm -f *.gz && for f in *.js; do gzip -k -9 \"\$f\" 2>/dev/null; done" 2>/dev/null || true

    DEPLOYED=$((DEPLOYED + 1))
    echo "   ✅ $plugin_name 部署完成"
done

# 重启 OnlyOffice
echo ""
echo "🔄 重启 OnlyOffice 服务..."
docker exec "$CONTAINER" supervisorctl restart all 2>/dev/null || \
    docker restart "$CONTAINER" 2>/dev/null || true

echo ""
echo "=========================================="
echo " ✅ 部署完成！共部署 $DEPLOYED 个插件"
echo " 浏览器请 Ctrl+Shift+R 强刷清除缓存"
echo "=========================================="
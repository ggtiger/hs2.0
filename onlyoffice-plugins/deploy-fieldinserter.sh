#!/bin/bash
# 部署 fieldinserter 插件到 OnlyOffice Docker 容器
# 用法: bash deploy-fieldinserter.sh
# 部署后浏览器需 Ctrl+Shift+R 强刷 + 注销 Service Worker

set -e

CONTAINER=onlyoffice-ds
PLUGIN_DIR=/var/www/onlyoffice/documentserver/sdkjs-plugins/fieldinserter
LOCAL_DIR="$(cd "$(dirname "$0")/fieldinserter" && pwd)"

echo "==> 源目录: $LOCAL_DIR"
echo "==> 目标容器: $CONTAINER:$PLUGIN_DIR"

# 拷贝文件到容器
docker cp "$LOCAL_DIR/index.js"     "$CONTAINER:$PLUGIN_DIR/index.js"
docker cp "$LOCAL_DIR/config.json"  "$CONTAINER:$PLUGIN_DIR/config.json"
docker cp "$LOCAL_DIR/index.html"   "$CONTAINER:$PLUGIN_DIR/index.html"

# 重新生成 gzip（OnlyOffice 优先读 .gz）
docker exec "$CONTAINER" bash -c "cd $PLUGIN_DIR && rm -f *.gz && gzip -k -9 index.js"

# 重启 OnlyOffice 服务
docker exec "$CONTAINER" supervisorctl restart all

echo "==> 部署完成，已重启 OnlyOffice 服务"
echo "==> 浏览器端请 Ctrl+Shift+R 强刷 + 注销 Service Worker 清除缓存"

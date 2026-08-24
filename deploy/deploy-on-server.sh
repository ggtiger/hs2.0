#!/bin/bash
# =============================================================================
# 服务器端部署脚本
# 
# 用法: 将此文件和 realso-images.tar.gz、docker-compose.yml、.env 
#       上传到服务器同一目录后执行:
#       chmod +x deploy-on-server.sh && ./deploy-on-server.sh
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

echo "=========================================="
echo " Realso 服务器部署脚本"
echo "=========================================="

# 检查 Docker
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker 未运行"
    exit 1
fi

if ! docker compose version > /dev/null 2>&1; then
    echo "❌ 未检测到 docker compose 插件"
    exit 1
fi

# 加载 .env
if [ -f ".env" ]; then
    echo "📝 加载 .env 配置..."
    set -a
    source .env
    set +a
fi

# 解压并加载镜像
IMAGE_FILE="realso-images.tar"
if [ -f "${IMAGE_FILE}.gz" ]; then
    echo "📦 解压镜像文件..."
    gunzip -f "${IMAGE_FILE}.gz"
fi

if [ -f "$IMAGE_FILE" ]; then
    echo "📥 加载 Docker 镜像..."
    docker load -i "$IMAGE_FILE"
    rm -f "$IMAGE_FILE"
    echo "✅ 镜像加载完成"
else
    echo "⚠️  未找到 $IMAGE_FILE，跳过"
fi

# 拉取 MySQL 镜像
echo "🐳 检查 MySQL 镜像..."
if docker image inspect mysql:8.0 > /dev/null 2>&1; then
    echo "  ✅ mysql:8.0 已存在"
else
    echo "  拉取 mysql:8.0 ..."
    docker pull mysql:8.0
fi

# 停止旧容器
echo "⏹️  停止旧容器..."
docker compose -f docker-compose.yml down 2>/dev/null || true

# 创建目录
mkdir -p upload log

# 启动服务
echo "🚀 启动服务..."
docker compose -f docker-compose.yml up -d

echo ""
echo "⏳ 等待服务就绪..."
sleep 15

# 显示状态
echo ""
echo "=========================================="
echo " ✅ 部署完成！"
echo "=========================================="
echo ""
docker compose -f docker-compose.yml ps

SERVER_IP=$(hostname -I 2>/dev/null | awk '{print $1}' || hostname)
echo ""
echo "🌐 服务地址:"
echo "  管理后台: http://${SERVER_IP}:${ADMIN_PORT:-8080}"
echo "  WebAPI:   http://${SERVER_IP}:${WEBAPI_PORT:-5001}"
echo "  Auth:     http://${SERVER_IP}:${AUTH_PORT:-5000}"
echo "  MySQL:    ${SERVER_IP}:${MYSQL_PORT:-3306} (root: ${MYSQL_ROOT_PASSWORD:-rootpassword})"
echo ""
echo "📋 常用命令:"
echo "  查看日志: docker compose -f docker-compose.yml logs -f"
echo "  停止服务: docker compose -f docker-compose.yml down"
echo "  重启服务: docker compose -f docker-compose.yml restart"
#!/bin/bash
# =============================================================================
# Realso 一键构建脚本
# 构建所有应用: .NET (WebAPI + Auth) + p-admin (前端)
# 
# 用法: cd deploy && bash build.sh
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
NETCORE_DIR="$PROJECT_DIR/netcore"
PADMIN_DIR="$PROJECT_DIR/p-admin"

echo "=========================================="
echo " Realso 一键构建脚本"
echo " 项目根目录: $PROJECT_DIR"
echo "=========================================="

# ---- 1. 构建 .NET 应用 ----
echo ""
echo "📦 [1/3] 构建 .NET Core 应用..."
cd "$NETCORE_DIR"

if ! command -v dotnet > /dev/null 2>&1; then
    echo "❌ 未检测到 dotnet SDK"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version 2>/dev/null || echo "unknown")
echo "   .NET SDK: $DOTNET_VERSION"

# 清理旧的发布产物
rm -rf ./publish/webapi ./publish/auth

echo "   发布 WebAPI (linux-x64)..."
dotnet publish Realso.WebAPI/Realso.WebAPI.csproj \
    -c Release -r linux-x64 -o ./publish/webapi \
    --self-contained false /p:PublishReadyToRun=false 2>&1 | grep -E "->|error|Build" | head -3

echo "   发布 Auth (linux-x64)..."
dotnet publish Realso.Auth/Realso.Auth.csproj \
    -c Release -r linux-x64 -o ./publish/auth \
    --self-contained false /p:PublishReadyToRun=false 2>&1 | grep -E "->|error|Build" | head -3

echo "   ✅ WebAPI: $(du -sh ./publish/webapi | cut -f1)"
echo "   ✅ Auth:   $(du -sh ./publish/auth | cut -f1)"

# ---- 2. 下载 .NET 运行时 ----
echo ""
echo "📦 [2/3] 准备 ASP.NET Core 2.2 运行时..."

if [ -f ./runtime/dotnet/dotnet ] && [ -d ./runtime/dotnet/shared/Microsoft.AspNetCore.App ]; then
    echo "   ✅ 运行时已存在，跳过"
else
    echo "   下载运行时..."
    rm -rf ./runtime && mkdir -p ./runtime/dotnet
    if [ ! -f /tmp/dotnet-install.sh ]; then
        curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    fi
    bash /tmp/dotnet-install.sh \
        --channel 2.2 --install-dir ./runtime/dotnet \
        --runtime aspnetcore --os linux --arch x64 --no-path
    echo "   ✅ 运行时下载完成"
fi

# ---- 3. 构建 Docker 镜像 ----
echo ""
echo "📦 [3/3] 构建 Docker 镜像..."

if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker 未运行"
    exit 1
fi

# 从 netcore 目录构建 (Dockerfile.local 在此目录)
echo "   构建 WebAPI 镜像..."
cd "$NETCORE_DIR"
docker build --platform linux/amd64 -f Dockerfile.local \
    --build-arg APP=webapi -t realso-webapi:latest .

echo "   构建 Auth 镜像..."
docker build --platform linux/amd64 -f Dockerfile.local \
    --build-arg APP=auth -t realso-auth:latest .

# 构建 p-admin (如果有 dist)
if [ -d "$PADMIN_DIR/dist" ]; then
    echo "   构建 p-admin 镜像..."
    cd "$PROJECT_DIR"
    docker build --platform linux/amd64 -f deploy/Dockerfile.p-admin -t realso-admin:latest .
else
    echo "   ⚠️  p-admin/dist 不存在，跳过前端镜像构建"
    echo "      如需构建前端: cd p-admin && npm install && npm run build"
fi

# ---- 完成 ----
echo ""
echo "=========================================="
echo " ✅ 构建完成！"
echo ""
echo " Docker 镜像:"
docker images --format "  {{.Repository}}:{{.Tag}} {{.Size}}" | grep -E "realso"
echo ""
echo " 下一步:"
echo "  本地启动: cd deploy && docker compose -f docker-compose.local.yml up -d"
echo "  导出部署: cd deploy && bash export.sh"
echo "=========================================="
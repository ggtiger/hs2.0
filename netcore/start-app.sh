#!/bin/sh
# =============================================================================
# 应用启动脚本
# 根据 APP_NAME 环境变量启动对应的 .NET 应用
# =============================================================================

set -e

echo "Starting Realso ${APP_NAME:-webapi}..."
echo "ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Production}"
echo "Current directory: $(pwd)"
echo "Files in /app:"
ls -la /app/

if [ -f /app/Realso.Auth.dll ]; then
    echo "Starting Auth service..."
    export ASPNETCORE_URLS=${ASPNETCORE_URLS:-http://0.0.0.0:5000}
    exec dotnet /app/Realso.Auth.dll
elif [ -f /app/Realso.WebAPI.dll ]; then
    echo "Starting WebAPI service..."
    export ASPNETCORE_URLS=${ASPNETCORE_URLS:-http://0.0.0.0:5001}
    exec dotnet /app/Realso.WebAPI.dll
else
    echo "ERROR: No application DLL found in /app/"
    ls -la /app/*.dll 2>/dev/null || echo "No DLL files found"
    exit 1
fi
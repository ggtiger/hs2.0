#!/bin/sh
# =============================================================================
# 应用启动脚本
# 根据容器内 DLL 自动识别 WebAPI 或 Auth
# =============================================================================

set -e

echo "Starting Realso application..."
echo "ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Production}"
echo "Current directory: $(pwd)"

if [ -f /app/Realso.Auth.dll ]; then
    echo "Starting Auth service on port 5000..."
    export ASPNETCORE_URLS=${ASPNETCORE_URLS:-http://0.0.0.0:5000}
    exec dotnet /app/Realso.Auth.dll
elif [ -f /app/Realso.WebAPI.dll ]; then
    echo "Starting WebAPI service on port 5001..."
    export ASPNETCORE_URLS=${ASPNETCORE_URLS:-http://0.0.0.0:5001}
    exec dotnet /app/Realso.WebAPI.dll
else
    echo "ERROR: No application DLL found in /app/"
    echo "Files in /app:"
    ls /app/*.dll 2>/dev/null || echo "  No DLL files found"
    exit 1
fi
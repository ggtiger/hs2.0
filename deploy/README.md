# Realso 部署中心

项目统一构建和部署目录，将 p-admin、netcore、onlyoffice-plugins 整合在一起。

## 目录结构

```
hs2.0/
├── deploy/                          # 🆕 部署中心 (所有 Docker/Compose/构建脚本)
│   ├── build.sh                     # 一键构建: dotnet publish + Docker 镜像
│   ├── export.sh                    # 导出镜像为 tar.gz 用于服务器部署
│   ├── deploy-on-server.sh          # 服务器端一键部署脚本
│   ├── deploy-plugins.sh            # 部署 OnlyOffice 插件
│   ├── docker-compose.yml           # 生产环境 Compose (MySQL + Auth + WebAPI + p-admin)
│   ├── docker-compose.local.yml     # 本地开发 Compose (MySQL + Auth + WebAPI)
│   ├── Dockerfile.dotnet            # .NET 应用 Dockerfile
│   ├── Dockerfile.p-admin           # p-admin 前端 nginx Dockerfile
│   ├── nginx.conf                   # nginx 配置 (API 反向代理)
│   ├── start-app.sh                 # 容器入口启动脚本
│   └── .env                         # 环境变量配置 (端口、密码等)
│
├── netcore/                         # .NET 后端 (源代码)
│   ├── Realso.WebAPI/               # WebAPI 项目
│   ├── Realso.Auth/                 # Auth 认证项目
│   ├── Realso.Core/                 # 核心库
│   ├── Dockerfile.local             # ✅ 实际构建用的 Dockerfile (Debian Buster)
│   ├── start-app.sh                 # 容器启动脚本 (Dockerfile.local 引用)
│   ├── runtime/                     # .NET 2.2 运行时 (构建产物)
│   └── *.sln / *.csproj             # 解决方案和项目文件
│
├── p-admin/                         # Vue 前端 (源代码)
│   └── ...
│
└── onlyoffice-plugins/               # OnlyOffice 插件
    └── fieldinserter/               # 字段插入插件
```

## 快速开始

### 本地开发 (macOS)

```bash
# 1. 一键构建 (NET publish + 下载运行时 + Docker 镜像)
cd deploy
bash build.sh

# 2. 启动所有服务
docker compose -f docker-compose.local.yml up -d

# 3. 查看服务
docker compose -f docker-compose.local.yml ps

# 4. 查看日志
docker compose -f docker-compose.local.yml logs -f
```

### 服务器部署 (Linux)

```bash
# 1. 本地导出镜像
cd deploy
bash export.sh

# 2. 上传到服务器
scp realso-images.tar.gz docker-compose.yml .env deploy-on-server.sh user@server:/opt/realso/

# 3. 服务器上执行
ssh user@server
cd /opt/realso
chmod +x deploy-on-server.sh
./deploy-on-server.sh
```

## 服务端口

| 服务 | 本地开发 | 服务器 | 说明 |
|------|---------|--------|------|
| MySQL | 3306 | 3306 | 数据库 |
| Auth | 6000 | 5000 | 认证服务 |
| WebAPI | 7001 | 5001 | 主 API |
| p-admin | - | 8080 | 前端管理界面 |

> 本地开发使用非默认端口 (6000/7001) 避免与已有服务冲突。服务器部署时使用默认端口。

## 常用命令

```bash
cd deploy

# 停止所有服务
docker compose -f docker-compose.local.yml down

# 重新构建并启动
bash build.sh && docker compose -f docker-compose.local.yml up -d

# 清理所有容器和卷
docker compose -f docker-compose.local.yml down -v

# 部署 OnlyOffice 插件
bash deploy-plugins.sh
```

## 构建流程说明

`build.sh` 执行以下步骤:

1. **dotnet publish** - 发布 WebAPI 和 Auth 为 linux-x64 自包含包
2. **下载运行时** - 下载 ASP.NET Core 2.2 运行时 (仅首次)
3. **构建镜像** - 从 netcore/ 目录使用 Dockerfile.local 构建:
   - `realso-webapi:latest` - WebAPI 镜像
   - `realso-auth:latest` - Auth 镜像
   - `realso-admin:latest` - p-admin 前端镜像 (如有 dist)

> **注意**: Dockerfile.local 使用 Debian Buster 基础镜像，需要网络能访问 Docker Hub。
# .NET Core 2.2 项目在 macOS ARM64 上运行 — 完整踩坑记录

## 环境信息

- **系统**：macOS (Apple Silicon ARM64)
- **项目**：.NET Core 2.2 (x64 only，无 ARM64 版本)
- **IDE**：Trae (VS Code 系)

## 核心矛盾

> .NET Core 2.2 **没有 ARM64 版本**，而 macOS ARM64 上的系统 dotnet 是 ARM64 架构，无法加载 x64 的 2.2 运行时。

## 解决方案架构

```
┌─────────────────────────────────────────────┐
│              ~/.local/bin/dotnet             │  ← 包装脚本（PATH 最优先）
│                  (bash)                      │
└──────────────┬──────────────────────────────┘
               │ exec
               ▼
┌─────────────────────────────────────────────┐
│          ~/.dotnet-2.2/dotnet               │  ← x64 dotnet（通过 Rosetta 2 运行）
│                                             │
│  SDK:  2.2.402  +  10.0.300                │
│  运行时: 2.2.7/2.2.8 + 10.0.8              │
│  (Microsoft.NETCore.App)                    │
│  (Microsoft.AspNetCore.App)                 │
│  (Microsoft.AspNetCore.All)                 │
└─────────────────────────────────────────────┘
```

**关键思路**：把 x64 的 .NET 2.2 和 .NET 10 装到同一个目录，这样一个 x64 dotnet 既能满足 OmniSharp（需要 SDK 6+），又能运行 2.2 应用。

## 完整步骤

### 1. 安装 x64 .NET Core 2.2 SDK

```bash
# 下载安装脚本
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh && chmod +x /tmp/dotnet-install.sh

# 安装 x64 的 .NET 2.2 SDK（含运行时）
/tmp/dotnet-install.sh --channel 2.2 --install-dir $HOME/.dotnet-2.2 --architecture x64
```

### 2. 安装 x64 .NET 10 运行时 + SDK

```bash
# 安装 x64 的 .NET 10 运行时
/tmp/dotnet-install.sh --channel 10.0 --runtime dotnet --install-dir $HOME/.dotnet-2.2 --architecture x64
/tmp/dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir $HOME/.dotnet-2.2 --architecture x64

# 安装 x64 的 .NET 10 SDK（手动下载解压）
curl -L -o /tmp/dotnet-sdk-10-x64.tar.gz "https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.300/dotnet-sdk-10.0.300-osx-x64.tar.gz"
tar xzf /tmp/dotnet-sdk-10-x64.tar.gz -C $HOME/.dotnet-2.2/
```

### 3. 创建包装脚本

```bash
mkdir -p ~/.local/bin
cat > ~/.local/bin/dotnet << 'EOF'
#!/bin/bash
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
exec /Users/wanghu/.dotnet-2.2/dotnet "$@"
EOF
chmod +x ~/.local/bin/dotnet
```

### 4. 移除 macOS 隔离标记（防止安全弹窗）

```bash
xattr -dr com.apple.quarantine $HOME/.dotnet-2.2/
```

### 5. 配置 PATH 优先级

```bash
# ~/.zprofile 最前面加一行
echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.zprofile
```

> **踩坑**：VS Code 的 Solution Explorer Run 按钮用 login shell (`/bin/zsh -l`)，读 `~/.zprofile` 而非 `~/.zshrc`。如果 `~/.local/bin` 不在 `~/.zprofile` 的 PATH 最前面，就会找到系统 arm64 dotnet。

### 6. 修改 launchSettings.json（去掉 IIS Express）

WebAPI 的 `Properties/launchSettings.json`：

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "Realso.WebAPI": {
      "commandName": "Project",
      "launchBrowser": true,
      "launchUrl": "api/values",
      "applicationUrl": "http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

Auth 的 `Properties/launchSettings.json`：

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "Realso.Auth": {
      "commandName": "Project",
      "launchBrowser": true,
      "launchUrl": "api/values",
      "applicationUrl": "http://localhost:5003",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

> **踩坑**：macOS 不支持 IIS Express，必须删掉。只保留 `commandName: "Project"` 的 Kestrel profile。

### 7. 禁用 NuGet 漏洞审计

`netcore/Directory.Build.props`：

```xml
<Project>
  <PropertyGroup>
    <NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
</Project>
```

> **踩坑**：.NET Core 2.2 的运行时包有已知漏洞，新版 SDK 会把漏洞当编译错误。2.2 已停止支持无法升级，只能关闭审计。

### 8. 更新有漏洞的 NuGet 包

| 包 | 旧版本 | 新版本 |
|---|--------|--------|
| log4net | 2.0.8 | 2.0.17 |
| System.Drawing.Common | 5.0.2 | 5.0.3 |

### 9. 修复编译错误（CS1061）

`IViewOperate` 接口缺少 `Query` 方法，需补充接口定义和实现。

### 10. VS Code 配置

`.vscode/settings.json`：

```json
{
  "csharp.experimental.debug.hotReload": false,
  "omnisharp.dotnetPath": "/Users/wanghu/.dotnet-2.2",
  "dotnet.dotnetPath": "/Users/wanghu/.dotnet-2.2",
  "terminal.integrated.env.osx": {
    "PATH": "/Users/wanghu/.local/bin:/usr/local/share/dotnet:${env:PATH}"
  }
}
```

## 踩坑清单

| # | 问题 | 原因 | 解决 |
|---|------|------|------|
| 1 | `command not found: dotnet` | dotnet 不在 PATH | 用完整路径 |
| 2 | CS1061 缺少 Query 方法 | 接口不完整 | 补充方法 |
| 3 | log4net 漏洞 | 版本过旧 | 升级到 2.0.17 |
| 4 | IIS Express 不支持 | macOS 无 IIS | 改用 Kestrel |
| 5 | arm64 找不到 2.2 运行时 | 2.2 无 arm64 版 | 用 x64 dotnet |
| 6 | OmniSharp 需要 SDK 6+ | 2.2 SDK 太旧 | 同目录装 10 SDK |
| 7 | NuGetFallbackFolder 权限 | x64 SDK 首次运行 | DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true |
| 8 | macOS 安全弹窗 | 隔离标记 | xattr -dr com.apple.quarantine |
| 9 | Solution Explorer 用错 dotnet | login shell PATH 顺序 | ~/.zprofile 加 PATH |
| 10 | TLS bad request | 浏览器 HSTS 缓存 | 只用 HTTP，清 HSTS |

## 最终运行

```bash
# WebAPI
dotnet run --project Realso.WebAPI.csproj --launch-profile Realso.WebAPI
# → http://localhost:5001

# Auth
dotnet run --project Realso.Auth.csproj --launch-profile Realso.Auth
# → http://localhost:5003
```

# 睿谱希 — 移动端 (hs-mobile)

睿谱希管理系统（hs2.0）移动端应用，基于 **uni-app + Vue3 + Pinia + Vite**，一套代码同时输出 **微信小程序** 和 **H5**。

完整设计文档见：[../docs/mobile-design.md](../docs/mobile-design.md)

## 技术栈

| 项 | 选型 |
|----|------|
| 框架 | uni-app (Vue3) |
| 状态管理 | Pinia |
| 构建工具 | Vite 7 |
| 样式 | SCSS + rpx |
| 后端 | 复用 hs2.0 后端（.NET Core），**零后端改造** |

## 功能模块（7 大模块）

| 模块 | 页面 | 说明 |
|------|------|------|
| 工作台 | `pages/index` | 待办统计、常用功能、公告、效能 |
| 待办中心 | `pages/todo` | 待复核/待审批/待提交/待签发 |
| 审批中心 | `pages/approve` | 详情审批、批量审批、只读查看 |
| 业务查询 | `pages/query` | 委托/受理/记录/证书/费用/物流 |
| 扫码 | `pages/scan` | 扫码查样品/证书/物流 |
| 个人中心 | `pages/mine` | 个人信息、资质、授权、设置 |
| 客户服务 | `pages/out` | 证书验证、进度查询、物流（免登录） |

## 目录结构

```
hs-mobile/
├── src/
│   ├── api/            # 接口层（复用后端 call/{Module}/{ApiCode}）
│   │   ├── db.js       # 通用调用 query/open/call/flowSave
│   │   ├── auth.js     # 登录认证
│   │   ├── home.js     # 工作台
│   │   ├── approve.js  # 审批操作
│   │   ├── query.js    # 业务查询
│   │   └── outer.js    # 外部接口（免认证）
│   ├── components/     # 公共组件（easycom 自动引入）
│   ├── pages/          # 页面
│   ├── store/          # Pinia（user/app/todo）
│   ├── utils/          # 工具（request/auth/config/state/format/scan）
│   ├── styles/         # 全局样式
│   ├── static/         # 静态资源（tabBar 图标）
│   ├── App.vue         # 应用入口
│   ├── main.js         # Vue3 + Pinia 挂载
│   ├── manifest.json   # 应用配置
│   └── pages.json      # 页面路由 + tabBar
├── index.html          # H5 入口
├── vite.config.js      # Vite 配置（含后端代理）
└── package.json
```

## 快速开始

### 1. 安装依赖

```bash
cd hs-mobile
npm install
```

### 2. 开发

```bash
# H5 端（浏览器，端口 8090）
npm run dev:h5

# 微信小程序端（产物在 dist/dev/mp-weixin，用微信开发者工具打开）
npm run dev:mp-weixin
```

### 3. 构建

```bash
npm run build:h5          # H5 生产构建 → dist/build/h5
npm run build:mp-weixin   # 小程序构建 → dist/build/mp-weixin
```

## 后端对接

移动端 **100% 复用** 现有 hs2.0 后端接口，无需新建后端服务。

### 端口分布（双服务）

| 服务 | 端口 | 用途 | 接口示例 |
|------|------|------|----------|
| Realso.Auth | **5000** | 登录认证（IdentityServer4） | `POST /api/user/login` |
| Realso.WebAPI | **5001** | 业务数据 / 文件 / PDF / 外部接口 | `POST /api/data/call/{Module}/{ApiCode}/` |

- 业务数据：`POST {5001}/api/data/call/{Module}/{ApiCode}/`
- 外部客户：`POST {5001}/api/outer/call/{Module}/{ApiCode}/`（免认证）
- PDF 预览：`GET {5001}/api/file/pdf/{fileId}`（免认证，OnlyOffice 自动转 pdf）
- 登录：`POST {5000}/api/user/login`（表单编码 USERNAME/PASSWORD）

### 请求格式（关键：与桌面端 db.js 完全一致）

后端**不是** JSON RESTful，而是**表单编码 + JSON 嵌套**：

```
POST /api/data/call/LI_M02/A14/
Content-Type: application/x-www-form-urlencoded

params={"FilterParams":{"ID":"xxx"}}&_userInfo_={"ID":"...","EMPNAME":"..."}
```

- 每个字段值经 `JSON.stringify` 后整体 urlencode（`tran` 编码，见 `src/utils/request.js`）
- `_userInfo_` 自动注入当前登录用户（登录后由 store 写入）
- `Authorization: Bearer {token}` 自动注入（登录接口除外）
- 响应 `{ Code:200, Data, Message }`，Code=501 自动跳登录

参数结构遵循桌面端 Store03：query=`{FilterParams, PageSize, PageIndex}`、open=`{FilterParams:{ID}}`、flowSave=`{ID, REMARK, ...}`。

### 接口地址配置

- **H5 开发**：用相对路径 `/api`，由 `vite.config.js` 双代理分流
  - `/api/user/*` → `localhost:5000`（认证）
  - `/api/*` → `localhost:5001`（数据）
- **小程序/App**：直连完整 URL，在 `.env.development` 配置
  - `VITE_DATA_BASE=http://<后端IP>:5001`
  - `VITE_AUTH_BASE=http://<后端IP>:5000`
  - （微信开发者工具勾选「不校验合法域名」）
- **生产**：建议 nginx 统一入口按路径分流到 5000/5001，前端仍用相对路径 `/api`

## 已完成功能

- 登录认证（5000 端口，登录失败/停用/不存在 分态提示）
- 工作台 / 待办 / 审批 / 查询 / 扫码 / 个人中心 / 客户服务（7 模块 22 页面）
- 证书 PDF 预览（`utils/pdf.js`，H5 新窗口 / 小程序 downloadFile+openDocument）
- 复核「下一审批人」选择器（`components/approver-picker.vue`）
- tabBar 图标（透明背景 + 线条图标）
- 审批流操作（复核/审批/驳回/撤销 + 批量）
- 权限点加载（C00/A06，登录后自动）

## 联调待确认（字段名以后端为准）

1. 待办过滤字段：`CHECKID/VERIFYID/CREATEID`（`src/api/approve.js`）是否匹配后端 F01 模板
2. PDF 文件 ID 字段：`FILEID/PDFID/CERTFILEID`（`utils/pdf.js pickFileId`）
3. 响应详情结构：open 返回的 `MAIN/DTSA` 分组字段（`src/pages/approve/detail.vue`）
4. 微信小程序上线前：在小程序后台配置 `request` 合法域名（5000 + 5001 或统一域名）

# Word 模版在线编辑方案设计

## 一、背景与目标

### 现状

当前 Word 模版导出使用 OpenXml Bookmark（书签）机制：
- 模版制作需要在本地 Word 中手动设置书签，门槛高
- 书签命名规则 `FIELDNAME_SUFFIX`（如 `SIGNDATE_YY`、`CREATER_IMG`）
- 表格通过 `_TABLE` 后缀 + 行克隆实现
- 字段与数据的对应关系不直观

### 目标

通过 OnlyOffice Document Editor 实现模版在线编辑：
1. 在线编辑 Word 模版，所见即所得
2. 可视化字段绑定，支持从三种来源获取字段
3. 支持表格（动态行）、图片、富文本
4. 向后兼容现有 Bookmark 模版

## 二、技术方案

### 2.1 字段载体：Content Control (SDT)

选择 **Structured Document Tag (SDT)** 作为字段载体，替代 Bookmark：

| 特性 | Bookmark | Content Control (SDT) |
|------|----------|----------------------|
| OnlyOffice 可编辑 | 不支持 | 原生支持 |
| 可视化 | 不可见 | 灰色标签 + 占位文本 |
| 属性 | Name | Tag（字段名）+ Title（描述）|
| 类型 | 无区分 | text/picture/richText/重复节 |
| 表格 | 需手动克隆 | 重复节 SDT |

### 2.2 SDT Tag 命名约定

延续现有 Bookmark 后缀规则：

| Tag 格式 | SDT 类型 | 含义 | 示例 |
|----------|---------|------|------|
| `CERTCODE` | `sdtText` | 纯文本 | 证书编号 |
| `SIGNDATE_YY` | `sdtText` | 日期-年 | 2024 |
| `SIGNDATE_MM` | `sdtText` | 日期-月 | 06 |
| `SIGNDATE_DD` | `sdtText` | 日期-日 | 15 |
| `CREATER_IMG` | `sdtPicture` | 图片 | 签名 |
| `CHECKQR_IMG2` | `sdtPicture` | 图片(固定尺寸) | 二维码 |
| `TECHINDEX_HTML` | `sdtRichText` | 富文本 | 技术指标 |
| `DETAILS_TABLE` | `sdtRow`(重复节) | 表格循环 | 明细行 |

## 三、字段来源设计

### 3.1 三种字段来源

#### 来源 A：ORM 元数据（自动获取）

从 `tss_resfield` + `tss_resuipc` 提取业务模块的表单字段：

```sql
SELECT f.FIELDNAME as fieldKey, u.LABELNAME as label
FROM tss_resfield f
LEFT JOIN tss_resuipc u ON u.FIELDID = f.ID
WHERE f.RESOURCEID = (
  SELECT RESOURCEID FROM tss_moudlepath
  WHERE MODULEID = (SELECT ID FROM tss_moudle WHERE MODULECODE = @moduleCode)
  AND PATHNAME = 'MAIN'
)
AND u.EDITSORT > 0
ORDER BY u.EDITSORT
```

#### 来源 B：模版管理字段（从 TPMDATA 提取）

模版管理（`tss_template.TPMDATA`）中的 JSON 结构包含：

```
TPMDATA:
├── itemField   → { field: "NAME", labelProps: { label: "设备名称" }, fieldType: "text" }
├── itemEditor  → { fields: [{ field: "FC1", name: "频率", formula: "" }] }
├── itemTable   → { sourceName: "数据源", children: [...] }
└── itemCheckBox → { field: "CHECK1", datas: [{ title: "选项1" }] }
```

递归遍历 JSON 提取所有字段。

#### 来源 C：手动输入

用户在字段面板中手动添加字段 key + label + type。

### 3.2 字段去重合并

三个来源可能产生同名字段：
- `key` 为唯一标识，同名字段只保留一个
- label 互补（ORM 可能有 key 无 label，模版字段有 label）
- 合并后的字段标记所有来源

### 3.3 系统内置字段

签名、二维码等固定字段，由后端根据业务类型自动补充：

```
签名图片: CREATER_IMG, CHECKER_IMG, VERIFIER_IMG
二维码:   CHECKQR_IMG2
```

## 四、API 接口设计

### 4.1 字段获取

| 接口 | 方法 | 说明 |
|------|------|------|
| `/api/word-template/fields` | GET | 合并获取所有来源字段 |
| `/api/word-template/fields/orm` | GET | ORM 元数据字段 |
| `/api/word-template/fields/template` | GET | 模版管理字段 |
| `/api/word-template/fields/system` | GET | 系统内置字段 |

### 4.2 OnlyOffice 编辑

| 接口 | 方法 | 说明 |
|------|------|------|
| `/api/word-template/editor-config/{fileId}` | GET | 生成 OnlyOffice 编辑模式配置 |
| `/api/word-template/download` | GET | 文件下载（供 Document Server 调用） |
| `/api/word-template/callback` | POST | OnlyOffice 保存回调 |
| `/api/word-template/parse-fields/{fileId}` | GET | 解析已有模版中的 SDT/Bookmark 字段 |

## 五、前端交互设计

### 5.1 三栏布局

```
┌────────────┬───────────────────────┬──────────────┐
│  字段面板   │   OnlyOffice 编辑器    │   (属性面板)  │
│  (250px)   │   (flex: 1)           │              │
│            │                       │              │
│ 来源选择    │   Word 文档编辑区域     │              │
│ 字段列表    │                       │              │
│ 手动添加    │                       │              │
│            │                       │              │
│ [点击字段]  │  ← 自动插入 Content    │              │
│            │     Control 占位符     │              │
└────────────┴───────────────────────┴──────────────┘
```

### 5.2 交互流程

```
用户点击字段面板中的字段
  → postMessage 通知 OnlyOffice 插件
  → 插件在光标位置插入 Content Control
  → 设置 Tag（字段名）、Title（字段描述）
  → 文档中显示占位符（如 [证书编号]）
```

### 5.3 OnlyOffice 插件

自定义 OnlyOffice Plugin "字段插入"：
- 监听外部 postMessage
- 在光标位置创建 Content Control
- 设置 SDT 属性（Tag/Title/Type）

## 六、后端替换引擎增强

### 6.1 WordHelper.cs 改造

`ReplaceFromTemplate` 方法同时支持 Bookmark 和 Content Control：

```csharp
public static void ReplaceFromTemplate(WordprocessingDocument doc, Dictionary<string, Object> markInfo)
{
    // 1. 优先 Content Control (SDT) 替换
    ReplaceFromContentControls(doc, markInfo);
    // 2. 兼容 Bookmark 替换（旧模版）
    ReplaceFromBookmarks(doc, markInfo);
}
```

### 6.2 SDT 解析

新增方法：
- `ParseContentControls(docxPath)` — 提取模版中所有 SDT 字段定义
- `ReplaceFromContentControls(doc, markInfo)` — SDT 字段替换
- `ReplaceTableRowSDT(sdt, data)` — 重复节表格替换

## 七、实施阶段

### 第一阶段：基础框架

1. `WordTemplateController.cs` — 复用 ExcelEditor callback/forcesave 模式
2. `word-template-editor.vue` — 三栏布局 + OnlyOffice Word 编辑
3. 字段获取 API — ORM + 模版 + 系统 + 手动输入

### 第二阶段：字段绑定

1. OnlyOffice 插件 — 字段插入
2. Content Control 创建和属性设置
3. 字段解析接口

### 第三阶段：替换引擎

1. `WordHelper` SDT 替换逻辑
2. 兼容旧 Bookmark 模版
3. 表格（重复节）支持
4. 图片/富文本替换

### 第四阶段：业务集成

1. 证书模版（RM11Controller）
2. 委托单/受理单模版（RM15Controller）
3. 模版管理页面集成
4. 旧模版迁移工具（Bookmark → SDT）

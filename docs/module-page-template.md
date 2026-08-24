# 页面配置模板参考（基于 LIB_M05 部门管理）

## 概述

在模块配置页面（s01/m18）新增列表页或表单页时，系统会按模板自动填充默认值。模板参照 **LIB_M05 部门管理**的标准配置，确保新增页面开箱即用。

---

## 列表页模板 (PAGETYPE=list)

### tss_module_page 字段

| 字段 | 默认值 | 说明 |
|------|--------|------|
| PAGETYPE | `list` | 列表页 |
| COMPONENTTYPE | `standard` | 只能填 `standard` 或 `sfc`，其他值导致路由不注册、白屏 |
| QUERYAPICODE | `A01` | 列表查询接口 |
| ADVQUERYAPICODE | 空 | 高级查询接口（需时再配） |
| OPENAPICODE | 空 | 列表页不直接打开数据 |
| SAVEAPICODE | 空 | 列表页不直接保存 |
| PAGECONFIG | `{"QRYPATH":"QRY","QQRYSPATH":"QQRY","defaultFormPageCode":"add"}` | **必须含 defaultFormPageCode**，双击行才能打开表单 |

### 列表页按钮

#### 添加按钮（唯一默认按钮）

| 字段 | 值 | 说明 |
|------|-----|------|
| BTNNAME | 添加 | |
| BTNTYPE | custom | 自定义按钮，走 openForm 分支 |
| BTNCODE | add | 按钮编码 |
| BTNAREA | header | 顶部区域 |
| INTERACTTYPE | direct | 直接执行，无需确认 |
| ICON | h-icon-plus | |
| COLOR | primary | |
| EXTPARAM | `{"action":"openForm","openMode":"add","formPageCode":"add"}` | 三要素缺一不可 |

**EXTPARAM 三要素**：

| 要素 | 值 | 作用 |
|------|-----|------|
| action | openForm | 走打开表单分支 |
| openMode | add | 新增模式（清空 currentId） |
| formPageCode | add | 目标 form 页的 PAGECODE |

> **注意**：列表页默认不配"编辑"行按钮和"删除"行按钮。双击行通过 `defaultFormPageCode` 打开表单编辑，行操作按钮按需手动添加。

---

## 表单页模板 (PAGETYPE=form)

### tss_module_page 字段

| 字段 | 默认值 | 说明 |
|------|--------|------|
| PAGETYPE | `form` | 表单页 |
| COMPONENTTYPE | `standard` | |
| OPENAPICODE | 空 | |
| SAVEAPICODE | `A04` | 保存接口 |
| PARENTID | 列表页 ID | **铁律**：form 页 PARENTID 必须指向 list 页 ID |
| ROUTEPATH | 空 | 表单页是弹窗，不需要路由 |
| PAGECONFIG | `{"MAINPATH":"MAIN"}` | 主表路径 |

### 表单页按钮

#### 保存按钮

| 字段 | 值 | 说明 |
|------|-----|------|
| BTNNAME | 保存 | |
| BTNTYPE | crud | |
| BTNCODE | save | |
| BTNAREA | footer | 底部区域 |
| APICODE | A04 | |
| INTERACTTYPE | direct | |
| ICON | h-icon-save | |
| COLOR | primary | |
| EXTPARAM | `{"action":"api"}` | |

#### 删除按钮

| 字段 | 值 | 说明 |
|------|-----|------|
| BTNNAME | 删除 | |
| BTNTYPE | crud | |
| BTNCODE | delete | |
| BTNAREA | footer | 底部区域 |
| APICODE | A07 | |
| INTERACTTYPE | poptip | 需确认操作 |
| SHOWCOND | ID!=null | 新增模式不显示删除 |
| COLOR | red | |
| EXTPARAM | `{"action":"api"}` | |

> **注意**：表单页 footer 只配保存+删除，**不配取消按钮**（rs-modal 弹窗自带 X 关闭）。

---

## 铁律清单

| # | 规则 | 违反后果 |
|---|------|---------|
| 1 | COMPONENTTYPE 只能填 `standard` 或 `sfc` | 填其他值路由不注册，模块白屏 |
| 2 | form 页 PARENTID 必须指向 list 页 ID | 双击行/添加按钮打不开表单 |
| 3 | list 页 PAGECONFIG 必须含 `defaultFormPageCode` | 双击行打不开表单 |
| 4 | 添加按钮 EXTPARAM 三要素缺一不可 | 点添加按钮没反应或表单不弹出 |
| 5 | 表单页 footer 只配保存+删除 | 取消按钮与 rs-modal X 重复 |
| 6 | 删除按钮 INTERACTTYPE=`poptip` | 防止误操作 |
| 7 | 删除按钮 SHOWCOND=`ID!=null` | 新增模式不显示删除 |
| 8 | 表单页 ROUTEPATH 为空 | 表单是弹窗不走路由 |

---

## LIB_M05 完整配置参考

### 列表页

```sql
-- ID: fb68baa2813c4231b2ebc6ff04271d0f
MODULECODE = 'LIB_M05'
PAGECODE   = 'main'
PAGENAME   = '部门管理'
PAGETYPE   = 'list'
PARENTID   = NULL
ROUTEPATH  = NULL  -- 自动生成 /g/LIB_M05/main
COMPONENTTYPE = 'standard'
QUERYAPICODE  = 'A01'
SAVEAPICODE   = NULL
SORTNO     = 1
PAGECONFIG = '{"SUBPAGES":[...],"defaultFormPageCode":"add"}'
```

### 表单页

```sql
-- ID: 8b1694b50e0f4618b2609c2360d6f1d8
MODULECODE = 'LIB_M05'
PAGECODE   = 'add'
PAGENAME   = '编辑'
PAGETYPE   = 'form'
PARENTID   = 'fb68baa2813c4231b2ebc6ff04271d0f'  -- 指向列表页
ROUTEPATH  = NULL  -- 弹窗，无路由
COMPONENTTYPE = 'standard'
SAVEAPICODE   = 'A04'
SORTNO     = 2
PAGECONFIG = '{"EXTENDJS":"@/modules/LIB_M05/add.js"}'
```

### 按钮配置

```sql
-- 列表页按钮: 添加
ID=a456a71c500544af8fc63055dd802d19, PAGEID=fb68...(列表页), MODULECODE=LIB_M05
BTNNAME=添加, BTNTYPE=custom, BTNCODE=add, BTNAREA=header
INTERACTTYPE=direct, PERMCODE=LIB_M05/A04
ICON=h-icon-plus, COLOR=primary
EXTPARAM={"action":"openForm","openMode":"add","formPageCode":"add"}

-- 表单页按钮: 保存
ID=b6ac6f60c3714fa39ad5b051ffa25fc0, PAGEID=8b16...(表单页), MODULECODE=LIB_M05
BTNNAME=保存, BTNTYPE=crud, BTNCODE=save, BTNAREA=footer
APICODE=A04, INTERACTTYPE=direct, PERMCODE=LIB_M05/A04
ICON=h-icon-save, COLOR=primary
BTNCONFIG={"action":"api"}

-- 表单页按钮: 删除
ID=9edd8efa9ae2471190bd83b5942d261b, PAGEID=8b16...(表单页), MODULECODE=LIB_M05
BTNNAME=删除, BTNTYPE=crud, BTNCODE=delete, BTNAREA=footer
APICODE=A07, INTERACTTYPE=poptip, SHOWCOND=ID!=null, PERMCODE=LIB_M05/A07
COLOR=red
BTNCONFIG={"action":"api"}
```

---

## SQL 模板（新模块复制用）

```sql
-- ============================================================
-- 新模块页面配置模板（参考 LIB_M05 部门管理）
-- 替换 {MC} 为目标模块编码
-- ============================================================

-- 1. 列表页
INSERT INTO tss_module_page (ID, MODULECODE, PAGECODE, PAGENAME, PAGETYPE, PARENTID, ROUTEPATH,
  COMPONENTTYPE, QUERYAPICODE, ADVQUERYAPICODE, OPENAPICODE, SAVEAPICODE, SORTNO, PAGECONFIG, ISDELETED)
VALUES (
  'mp_{mc}_main', '{MC}', 'main', '{页面名称}', 'list', NULL, NULL,
  'standard', 'A01', NULL, NULL, NULL, 1,
  '{"QRYPATH":"QRY","QQRYSPATH":"QQRY","defaultFormPageCode":"add"}', 0
);

-- 2. 列表页按钮: 添加
INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA,
  INTERACTTYPE, POPTIPTEXT, SHOWCOND, PERMCODE, ICON, COLOR, SORTNO, EXTPARAM, ISDELETED)
VALUES (
  'mb_{mc}_add', 'mp_{mc}_main', '{MC}', NULL, '添加', 'custom', 'add', 'header',
  'direct', NULL, NULL, '{MC}/A04', 'h-icon-plus', 'primary', 1,
  '{"action":"openForm","openMode":"add","formPageCode":"add"}', 0
);

-- 3. 表单页（PARENTID 指向列表页）
INSERT INTO tss_module_page (ID, MODULECODE, PAGECODE, PAGENAME, PAGETYPE, PARENTID, ROUTEPATH,
  COMPONENTTYPE, QUERYAPICODE, ADVQUERYAPICODE, OPENAPICODE, SAVEAPICODE, SORTNO, PAGECONFIG, ISDELETED)
VALUES (
  'mp_{mc}_form', '{MC}', 'add', '编辑', 'form', 'mp_{mc}_main', NULL,
  'standard', NULL, NULL, NULL, 'A04', 2,
  '{"MAINPATH":"MAIN"}', 0
);

-- 4. 表单页按钮: 保存
INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA,
  INTERACTTYPE, POPTIPTEXT, SHOWCOND, PERMCODE, ICON, COLOR, SORTNO, EXTPARAM, ISDELETED)
VALUES (
  'mb_{mc}_save', 'mp_{mc}_form', '{MC}', 'A04', '保存', 'crud', 'save', 'footer',
  'direct', NULL, NULL, '{MC}/A04', 'h-icon-save', 'primary', 1,
  '{"action":"api"}', 0
);

-- 5. 表单页按钮: 删除
INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA,
  INTERACTTYPE, POPTIPTEXT, SHOWCOND, PERMCODE, ICON, COLOR, SORTNO, EXTPARAM, ISDELETED)
VALUES (
  'mb_{mc}_delete', 'mp_{mc}_form', '{MC}', 'A07', '删除', 'crud', 'delete', 'footer',
  'poptip', NULL, 'ID!=null', '{MC}/A07', NULL, 'red', 2,
  '{"action":"api"}', 0
);
```

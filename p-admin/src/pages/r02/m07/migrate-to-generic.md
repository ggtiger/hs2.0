# r02/m07 物流管理（R02_M07）→ generic-module + SFC 迁移方案

## 迁移思路

将传统"四件套"（router.js / store.js / main.vue / add.vue）拆解为：

1. **数据库配置** — tss_module_page + tss_module_button + tss_resuipc（m18 可视化配置）
2. **SFC 在线资产**（存入 tss_code_asset，通过 m17 在线开发）：
   - `store.js` — Store 扩展（多 path INIT + loadAcceptRefs）
   - `main.js` — 列表页扩展（极简，按钮走配置）
   - `form.js` — 表单页扩展（onShow 重写 + save 校验 + defaultValues 支持）
3. **multiautocomplete 子表字段配置** — 通过 m18 UI 设置 SELECTDATA，不写 SFC field slot

---

## 一、数据库配置（m18 可视化配置）

### 1.1 模块页面配置（tss_module_page）

| PAGECODE | PAGENAME | PAGETYPE | ROUTEPATH | QUERY_APICODE | OPEN_APICODE | SAVE_APICODE |
|----------|----------|----------|-----------|---------------|--------------|--------------|
| main | 物流管理 | list | /g/R02_M07/main | A01 | - | - |
| add | 物流编辑 | form | /g/R02_M07/add | - | A02 | A04 |

> 说明：R02_M07 物流是简单的单表 + 子表（DTSA 关联受理单）结构。
> 列表页用 list + 标准 A01 查询；表单页用 form + A02 打开 + A04 保存。
> 由于列表行点击直接进入"详情+编辑"合一的弹窗（原 `rs-modal` + `add.vue`），这里保持弹窗模式：
> main 的 PAGECODE 下，按钮区"添加物流"/行点击都会打开 `rs-modal` 内嵌 `generic-form`。

### 1.2 main 页 PAGECONFIG

```json
{
  "QRYPATH": "QRY",
  "QQRYSPATH": "QQRY",
  "defaultFormPageCode": "add",
  "EXTENDJS": "@/modules/R02_M07/main.js"
}
```

### 1.3 add 页 PAGECONFIG

```json
{
  "MAINPATH": "MAIN",
  "FORMLAYOUT": "twocolumn",
  "EXTENDJS": "@/modules/R02_M07/form.js"
}
```

> 不需要 SLOTS 配置：multiautocomplete 子表联动完全由 `tss_resuipc.SELECTDATA` 驱动，
> `rs-form-cell` 内置了 subtable 模式的双向绑定（见 `rs-form-cell.vue:71`、`rs-form-edit.vue:266`）。

### 1.4 按钮配置（tss_module_button）

**main 页按钮**：

| BTNNAME | BTNCODE | BTNAREA | APICODE | EXTPARAM | SHOWCOND |
|---------|---------|---------|---------|----------|----------|
| 添加物流 | add | footer | - | `{"action":"add"}` | - |

> 行点击编辑由 `list-t01` 的 `@list-click-row` 触发，不需要配按钮；
> 框架的 `ensureModuleLoaded` 由 generic-module 自动保证，不需要前端再兜底。

**add 页按钮**：

| BTNNAME | BTNCODE | BTNAREA | APICODE | EXTPARAM | SHOWCOND |
|---------|---------|---------|---------|----------|----------|
| 保存 | save | footer | A04 | `{"beforeAction":"validateDTSA"}` | - |
| 取消 | cancel | footer | - | `{"action":"close"}` | - |

> 关键：保存按钮不直接走标准 save，而是通过 `EXTPARAM.beforeAction="validateDTSA"`
> 在扩展 JS 中先校验 DTSA 子表至少 1 行，再让框架继续走 A04 保存。
> 这样可以复用 generic-form 的 save 流程（校验 + `$callAction save + successCall`）。

---

## 二、关键字段 UI 配置（tss_resuipc）

### 2.1 multiautocomplete 字段（核心）— ACCEPTCODE

物流管理的核心交互是"选择多个受理单关联到当前物流单"。原 `add.vue` 通过 `rs-form-edit` + `multiautocomplete` EDITTYPE，选中项自动同步到 DTSA 子表。

**m18 UI 设置**（RESOURCENAME=VCK_LOGISTICS，FIELDNAME=ACCEPTCODE）：

| 字段 | EDITTYPE | SELECTDATA | LABELNAME | EDITSORT |
|------|----------|-----------|-----------|----------|
| ACCEPTCODE | multiautocomplete | `{"selType":"accept","mode":"subtable","subtable":"DTSA","subMappings":"ACCEPTID,ID;ACCEPTCODE,BILLCODE","keyName":"ID","titleName":"BILLCODE"}` | 关联受理单 | 10 |

**SELECTDATA 参数说明**：

| 参数 | 值 | 含义 |
|------|----|----|
| `selType` | `accept` | 选择器类型，由 `selRegistry` 解析为 VBS_ACCEPT 资源（受理单视图） |
| `mode` | `subtable` | 子表绑定模式（另一可选 `field` 是逗号 id 存单字段） |
| `subtable` | `DTSA` | 目标子表路径（须与 tss_moudlepath 的 DTS 路径名一致） |
| `subMappings` | `ACCEPTID,ID;ACCEPTCODE,BILLCODE` | 子表字段,远程字段 映射（分号分隔多对） |
| `keyName` | `ID` | 远程对象的 key 字段名（用于校验/去重） |
| `titleName` | `BILLCODE` | 远程对象在下拉列表中显示的字段 |

**渲染链路**（不需要写代码）：

```
rs-form-edit.actualFields 解析 EDITTYPE=multiautocomplete + SELECTDATA
  → gen.js:124 注入 cellProps.multSelConfig = {mode:'subtable', subtable:'DTSA', subMappings:...}
  → rs-form-edit.vue:266 注入 cellProps.subtableAccessor = _buildSubtableAccessor(multSelConfig)
    （通过 this.$store.state[ns].dt.DTSA 构造 {getData, rebuild, add, remove} 访问器）
  → rs-form-cell.vue:71 渲染 AutoComplete multiple + type=object
  → multiSelectValue.get(): subtableAccessor.getData() 反向映射成远程对象数组
  → multiSelectValue.set(): subtableAccessor.rebuild(arr) 清空 DTSA + 按映射重建行
  → multiSelectValue watch: 拼逗号 id 同步主表虚拟字段（仅用于必填校验，后台不存）
```

### 2.2 其他字段（普通配置）

| 字段 | EDITTYPE | SELECTDATA | LABELNAME | EDITSORT |
|------|----------|-----------|-----------|----------|
| EXPCOMPANY | select | `{"selType":"dict","dictName":"物流公司"}` 或直接写字典名 | 物流公司 | 20 |
| LOGISTICSNO | textinput | - | 物流单号 | 30 |
| SENDDATE | datepicker | - | 发货日期 | 40 |
| RECEIVENAME | textinput | - | 收货人 | 50 |
| RECEIVEADDR | textarea | - | 收货地址 | 60 |
| STATUS | select | `{"selType":"dict","dictName":"物流状态"}` | 状态 | 70 |
| FILES | fileupload | `{"multifile":true}` | 附件 | 80 |

---

## 三、SFC 在线资产 — store.js

**路径**：`@/modules/R02_M07/store.js`
**资产类型**：js（tss_code_asset.ASSETTYPE='js', MODULEPATH=`/modules/R02_M07/store.js`）

```javascript
/**
 * R02_M07 物流管理 Store 扩展
 *
 * 保留原 store.js 中的两个自定义 action：
 *   1. add — 多 path 初始化（MAIN + DTSA + DTS），否则 DTSA 子表不存在，
 *      multiautocomplete 的 subtableAccessor 取不到数据源
 *   2. loadAcceptRefs — A02 接口单独拉取 DTSA（标准 open 不返回 DTSA）
 *
 * Store03 默认 actions（query/open/save/delete/call/...）已由 generic-store 内置，无需重复定义。
 */
export default {
  actions: {
    // 多 path 初始化：MAIN 主表 + DTSA 受理单关联子表 + DTS 附件子表
    add({ commit }) {
      commit('INIT', { paths: ['MAIN', 'DTSA', 'DTS'] });
      commit('ADD', { path: 'MAIN', item: {} });
    },

    // 编辑回显：标准 A02 open 只返回 MAIN+DTS，DTSA 需要单独拉取
    // 后端 A02 接口已存在（原 store.js 调用 /api/data/call/R02_M07/A02/）
    async loadAcceptRefs({ dispatch }, { id }) {
      return await dispatch('call', {
        APICODE: 'A02',
        params: { ID: id },
      });
    },
  },

  mutations: {},
};
```

**说明**：
- `add` 必须重写：原 Store03 的默认 `add` 只 INIT MAIN 一张表，但 multiautocomplete 字段的 subtableAccessor 会访问 `state.dt.DTSA`，DTSA 不存在会报错。
- `loadAcceptRefs` 调用通用的 `dispatch('call', {APICODE, params})`，apiPath 默认就是 `/api/data/call/R02_M07`，不需要再写完整 URL。

---

## 四、SFC 在线资产 — main.js

**路径**：`@/modules/R02_M07/main.js`
**资产类型**：js（MODULEPATH=`/modules/R02_M07/main.js`）

```javascript
/**
 * R02_M07 物流管理 — 列表页扩展
 *
 * this 上下文（generic-module）:
 *   this.moduleCode     - 'R02_M07'
 *   this.storeName      - 'r02_m07' (或框架分配的命名空间)
 *   this.$refs.list     - 列表组件引用
 *   this.selectedRows   - 当前选中行（虽然此页未启用 checkbox）
 *   this.$callAction / this.$alert / this.$error / this.$confirm / this.$busy / this.$free
 *
 * 此页非常简单：
 *   - 只有一个"添加物流"按钮（配在 tss_module_button 中）
 *   - 不需要选中行校验
 *   - 不需要批量操作
 *   - 不需要打印/下载
 */
export default {
  computed: {
    // 目前无显隐控制需求
  },

  methods: {
    // 添加物流：打开表单弹窗
    // 由按钮 EXTPARAM.action="add" 路由到本方法
    // 框架已保证 R02_M07 模块和 scm 已加载，不再需要 ensureModuleLoaded 兜底
    add(btn, context) {
      // 打开新增表单：清空 ID + 显示弹窗
      // generic-module 的 openForm(pageCode, { ID: '', extraParams }) 已封装弹窗逻辑
      this.openForm('add', { ID: '' });
    },

    // 行点击：打开编辑表单
    // list-t01 的 @list-click-row 事件由 generic-module 转发到 onRowClick
    // 重写 onRowClick 改为打开弹窗（不跳路由）
    onRowClick(row) {
      if (!row || !row.ID) return;
      this.openForm('add', { ID: row.ID });
    },
  },
};
```

**收益对比**：
- 原 `main.vue` 76 行 → 配置 + 30 行扩展 JS
- 删除 `ensureModuleLoaded` 兜底（generic-module 框架保证）
- 删除 `rs-modal` + `rsAdd` 组件注册（generic-module 内置）
- 删除 `mapDateTable('QQRY', ['INPUT'])`（框架自动处理）

---

## 五、SFC 在线资产 — form.js

**路径**：`@/modules/R02_M07/form.js`
**资产类型**：js（MODULEPATH=`/modules/R02_M07/form.js`）

```javascript
/**
 * R02_M07 物流管理 — 表单页扩展
 *
 * this 上下文（generic-form）：
 *   this.moduleCode     - 'R02_M07'
 *   this.storeName      - 'r02_m07'
 *   this.ID             - 当前编辑的记录 ID（来自 prop，新建时为空）
 *   this.STATE / this.CUSTNAME ...  - 主表字段直接读写（mapDateTable 映射）
 *   this.$MAIN          - 主表 DataTable 实例
 *   this.$DTSA          - DTSA 子表 DataTable 实例（multiautocomplete 绑定的目标）
 *   this.defaultValues  - 表单默认值 prop（generic-form 需要扩展支持，见下方说明）
 *   this.save()         - 框架内置保存流程
 *   this.closePage()    - 关闭弹窗/页面
 *   this.$callAction / this.$alert / this.$error / this.$confirm
 *
 * 核心扩展点：
 *   1. onShow 重写：open 后再调 loadAcceptRefs 回填 DTSA（标准 open 不返回 DTSA）
 *   2. save 校验：通过按钮 EXTPARAM.beforeAction="validateDTSA" 拦截
 *   3. defaultValues 支持：新建时注入默认值（含 multiautocomplete 子表）
 */
export default {
  computed: {
    // 暂存：新建 或 STATE===1
    ISSHOWSAVE() {
      return !this.ID || this.STATE === '1' || this.STATE === 1;
    },
  },

  methods: {
    /**
     * onShow 重写：打开表单时的初始化
     *
     * 框架默认 onShow 只做 open/add，但 R02_M07 的 DTSA 子表由 A02 单独返回，
     * 需要在 open 完成后追加 loadAcceptRefs 调用，把受理单关联数据回填到 DTSA。
     * multiautocomplete 字段会通过 subtableAccessor 自动读取 DTSA 显示已选项。
     *
     * 防重入：原 add.vue 的 _onShowPending 标志保留，因为 view-dialog 的 @on-show
     * 和 Add01 mixin 的 $parent.isOpened watch 可能同时触发。
     */
    async onShow() {
      if (this._onShowPending) return;
      this._onShowPending = true;

      try {
        if (this.ID) {
          // 编辑：先 open 拿主表 + DTS 子表
          await this.$store.dispatch(this.storeName + '/open', {
            FilterParams: { ID: this.ID },
          });
          // 再单独拉 DTSA（受理单关联）
          await this.loadAcceptRefs();
        } else {
          // 新建：触发多 path INIT
          await this.$store.dispatch(this.storeName + '/add', {});

          // 应用默认值（如果调用方传入了 defaultValues）
          this._applyDefaultValues();
        }
      } finally {
        this._onShowPending = false;
      }
    },

    /**
     * 编辑回显：拉取 DTSA 并写入 store
     *
     * 原 add.vue 的 loadAcceptRefs 直接操作 DataTable.clear()/add()，
     * 这里改为同样的逻辑，通过 $DTSA 访问器操作。
     */
    async loadAcceptRefs() {
      try {
        var ret = await this.$callAction({
          action: this.storeName + '/loadAcceptRefs',
          param: { id: this.ID },
          isBusy: false,
        });

        var dtsa = this.$DTSA;
        if (!dtsa) return;

        // 清空旧数据
        dtsa.clear();

        // 填充新数据：A02 返回的 DTSA 数组每项含 ACCEPTID + ACCEPTCODE
        var rows = (ret && ret.Data && ret.Data.DTSA) || (ret && ret.DTSA) || [];
        rows.forEach(function(r) {
          dtsa.add({
            ACCEPTID: r.ACCEPTID,
            ACCEPTCODE: r.ACCEPTCODE || r.BILLCODE,
          });
        });
      } catch (e) {
        // 静默失败：不阻塞表单打开
        console.warn('[R02_M07 form] loadAcceptRefs error:', e);
      }
    },

    /**
     * 保存前校验：DTSA 至少 1 行
     *
     * 配置在按钮 EXTPARAM.beforeAction="validateDTSA"，
     * 框架在调用 A04 保存前会调用此方法，返回 false 则中断保存。
     *
     * @param {Object} btn - 按钮配置
     * @param {Object} context - 按钮上下文
     * @returns {boolean|Promise<boolean>} - false 阻断保存
     */
    validateDTSA(btn, context) {
      var dtsa = this.$DTSA;
      if (!dtsa || !dtsa.data || dtsa.data.length === 0) {
        this.$alert('请至少添加一个关联受理单');
        return false;
      }
      return true;
    },

    /**
     * 应用默认值
     *
     * generic-form 当前未内置 defaultValues prop 传递给 rs-form-edit，
     * 需通过以下方式之一支持：
     *
     * 方案 A（推荐）：扩展 generic-form.vue 增加 defaultValues prop，
     *   并在 rs-form-edit 上 :default-values="defaultValues"
     *   然后 rs-form-edit.applyDefaultValues() 会自动处理普通字段 + multiautocomplete 子表
     *
     * 方案 B（零框架改动）：在 form.js 的 onShow 中手动写入
     */
    _applyDefaultValues() {
      var dv = this.defaultValues || {};
      if (!Object.keys(dv).length) return;

      // 普通字段：仅当为空时写入
      var main = this.$MAIN;
      if (main && main.data && main.data[0]) {
        Object.keys(dv).forEach((key) => {
          var val = dv[key];
          // 跳过数组（multiautocomplete 子表）
          if (Array.isArray(val)) return;

          var cur = main.getValue(key);
          if (cur === undefined || cur === null || cur === '') {
            main.setValue(key, val);
          }
        });
      }

      // multiautocomplete 子表：仅当 DTSA 为空时重建
      var dtsa = this.$DTSA;
      if (dtsa && dtsa.data && dtsa.data.length === 0) {
        var acceptList = dv.ACCEPTCODE || dv.ACCEPTID || [];
        if (Array.isArray(acceptList) && acceptList.length) {
          dtsa.clear();
          acceptList.forEach(function(obj) {
            dtsa.add({
              ACCEPTID: obj.ID || obj.ACCEPTID,
              ACCEPTCODE: obj.BILLCODE || obj.ACCEPTCODE,
            });
          });
        }
      }
    },
  },
};
```

### 5.1 generic-form 增加 defaultValues prop（方案 A）

如果选择方案 A（推荐），需要给 `src/components/generic-module/generic-form.vue` 增加：

```javascript
// props 增加
props: {
  // ... 其他 props
  defaultValues: { type: Object, default: () => ({}) },
}

// template 中 rs-form-edit 增加绑定
<rs-form-edit
  ref="form"
  :default-values="defaultValues"
  ...
/>

// onShow 末尾追加：
if (this.$refs.form && this.$refs.form.applyDefaultValues) {
  this.$refs.form.applyDefaultValues();
}
```

调用方（如 LI_M00 的"添加物流"按钮）传 defaultValues：

```javascript
// LI_M00/main.js 的 addLogistics 方法
this.openForm('add', {
  ID: '',
  defaultValues: {
    ACCEPTCODE: this.selectedRows.map(r => ({ ID: r.ID, BILLCODE: r.BILLCODE })),
    EXPCOMPANY: 'SF',
  },
});
```

---

## 六、defaultValues prop 调用方使用方式

物流表单支持 defaultValues 后，其他模块（如受理单 LI_M00）可以通过 `openForm` 注入默认值：

```javascript
// 在 LI_M00 的 main.js 扩展中
async addLogistics() {
  var rows = this.selectedRows || [];
  if (!rows.length) {
    this.$error('请先勾选受理单');
    return;
  }

  // 构建默认值：把选中的受理单作为 DTSA 默认数据
  var defaultValues = {
    ACCEPTCODE: rows.map(function(r) {
      return { ID: r.ID, BILLCODE: r.BILLCODE };
    }),
  };

  // 打开物流表单，传入默认值
  // openForm 是 generic-module 的内置方法，支持 (pageCode, { ID, extraParams, defaultValues })
  await this.$store.dispatch('app/initModule', 'R02_M07');
  this.openForm('add', { ID: '', defaultValues: defaultValues });
}
```

---

## 七、迁移对照表

| 原 r02/m07 文件 | 迁移后 | 说明 |
|----------------|--------|------|
| `router.js` | 删除 | generic-module 路由由 `tss_func.OUTERURL=/g/R02_M07/main` 自动注册 |
| `index.js` | 删除 | 入口文件不再需要 |
| `store.js`（47 行） | `@/modules/R02_M07/store.js`（30 行） | 仅保留 `add`（多 path INIT）+ `loadAcceptRefs` 两个自定义 action |
| `views/main.vue`（76 行） | m18 配置 + `@/modules/R02_M07/main.js`（30 行） | `ensureModuleLoaded` 删除（框架保证）；`rs-modal`/`rsAdd` 删除（框架内置）；`add`/`onRowClick` 进扩展 JS |
| `views/add.vue`（118 行） | m18 配置 + `@/modules/R02_M07/form.js`（150 行） | `onShow`/`loadAcceptRefs`/`validateDTSA` 进扩展 JS；multiautocomplete 走 UI 配置 |
| `mapDateTable('QQRY', ['INPUT'])` | 框架自动处理 | `rs-meta-query-panel` 直接绑定 QQRY DataTable |
| `mapDateTable('MAIN', [])` | 框架自动映射 | generic-form 的 `mapDataTableFields` 自动处理 |
| multiautocomplete 字段（ACCEPTCODE） | `tss_resuipc` 配置 | EDITTYPE=multiautocomplete + SELECTDATA={mode:subtable, subtable:DTSA, ...} |
| `<rs-form-edit :default-values="defaultValues">` | 需扩展 generic-form | 增加 defaultValues prop 并透传给 rs-form-edit |
| `applyDefaultValues()` 调用 | 扩展 JS 的 `_applyDefaultValues` 或框架内置 | 方案 A 走框架内置，方案 B 走扩展 JS 手动写入 |
| save 校验（DTSA 至少 1 行） | 按钮 `EXTPARAM.beforeAction="validateDTSA"` | 不重写 save，复用框架流程，仅在保存前拦截 |
| `close()` 取消按钮 | 按钮 `EXTPARAM.action="close"` | 调用 generic-form 的 closePage |

---

## 八、迁移后目录结构

```
src/modules/R02_M07/          # SFC 扩展资产（数据库 tss_code_asset）
  store.js                    # Store 扩展（add 多 path INIT + loadAcceptRefs）
  main.js                     # 列表页扩展（add + onRowClick）
  form.js                     # 表单页扩展（onShow + loadAcceptRefs + validateDTSA + _applyDefaultValues）

src/pages/r02/m07/            # 原目录可全部删除
  router.js                   # 删除（路由自动注册）
  index.js                    # 删除
  store.js                    # 删除（逻辑移至 @/modules/R02_M07/store.js）
  views/main.vue              # 删除（配置化 + main.js）
  views/add.vue               # 删除（配置化 + form.js）
```

数据库侧：

```
tss_module_page:
  - PAGECODE='main', MODULECODE='R02_M07', PAGETYPE='list', ROUTEPATH='/g/R02_M07/main'
  - PAGECODE='add',  MODULECODE='R02_M07', PAGETYPE='form', ROUTEPATH='/g/R02_M07/add'

tss_module_button:
  - main: 添加物流 (footer, action=add)
  - add:  保存 (footer, A04, beforeAction=validateDTSA) + 取消 (footer, action=close)

tss_resuipc (VCK_LOGISTICS):
  - ACCEPTCODE: EDITTYPE=multiautocomplete, SELECTDATA={mode:subtable, subtable:DTSA, ...}
  - EXPCOMPANY/LOGISTICSNO/SENDDATE/RECEIVENAME/STATUS/FILES: 标准 EDITTYPE

tss_code_asset (通过 m17 在线开发):
  - ASSETTYPE='js', CODE='R02_M07_store',       MODULEPATH='/modules/R02_M07/store.js'
  - ASSETTYPE='js', CODE='R02_M07_main',        MODULEPATH='/modules/R02_M07/main.js'
  - ASSETTYPE='js', CODE='R02_M07_form',        MODULEPATH='/modules/R02_M07/form.js'

tss_func:
  - 物流管理菜单 OUTERURL='/g/R02_M07/main'
```

---

## 九、关键风险点与验证清单

### 9.1 multiautocomplete 子表绑定验证

- [ ] tss_moudlepath 配置了 `DTSA` 路径（PATHNAME=DTSA, RESOURCEID=VCK_ACCEPT 或对应视图）
- [ ] tss_moudlepathrel 配置了 MAIN→DTSA 的外键关系（RFIELDSA=ID, RFIELDSB=LOGISTICSID）
- [ ] tss_resuipc 中 ACCEPTCODE 的 SELECTDATA JSON 合法（`subtable` 值与 PATHNAME 一致）
- [ ] subMappings 的远程字段名与 VBS_ACCEPT 视图列名匹配（ID/BILLCODE）
- [ ] 新建表单时 DTSA 子表已通过 `add` action 的多 path INIT 创建

### 9.2 onShow 回填验证

- [ ] 编辑已有物流单时，multiautocomplete 能显示已关联的受理单列表
- [ ] A02 接口返回的 DTSA 数据结构为 `[{ACCEPTID, ACCEPTCODE}]` 或 `[{ACCEPTID, BILLCODE}]`
- [ ] `_onShowPending` 防重入标志生效（避免 view-dialog + Add01 watch 双触发）

### 9.3 defaultValues 验证（方案 A）

- [ ] generic-form.vue 的 props 增加 `defaultValues`
- [ ] template 中 `:default-values="defaultValues"` 透传给 rs-form-edit
- [ ] onShow 末尾调用 `this.$refs.form.applyDefaultValues()`
- [ ] LI_M00 的 addLogistics 调用 `openForm('add', { defaultValues: {...} })` 能正确传入

### 9.4 save 校验验证

- [ ] 不选任何受理单直接保存 → 弹出"请至少添加一个关联受理单"
- [ ] 选择 1 个或多个受理单后保存 → 正常提交 A04
- [ ] multiautocomplete 字段的虚拟主表字段（逗号 id 串）不会误存到数据库（主表无此物理列）

---

## 十、与 r01/m05（LI_M00）迁移方案的对比

| 维度 | r01/m05（受理单 LI_M00） | r02/m07（物流 R02_M07） |
|------|--------------------------|------------------------|
| 复杂度 | 高（13 个按钮 + 跨模块 + 打印下载 + 联动计算） | 低（1 个添加按钮 + 行点击编辑） |
| SFC field slot | 5 个（客户/模板/员工/部门/文件） | 0 个（multiautocomplete 全走 UI 配置） |
| SFC slot（查询面板） | 1 个（query-panel.vue） | 0 个（用框架默认查询面板） |
| Store 扩展 | 5 个 action（跨模块 + 批量） | 2 个 action（多 path INIT + loadAcceptRefs） |
| onShow | 简单（单据号生成） | 复杂（open + loadAcceptRefs + defaultValues） |
| save 重写 | 无（走标准流程） | beforeAction 拦截（validateDTSA） |
| 核心难点 | 按钮显隐 + 批量操作 | multiautocomplete 子表绑定 + 编辑回填 |

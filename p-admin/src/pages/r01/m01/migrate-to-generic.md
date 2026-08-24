# r01/m01 项目管理（LI_M01）→ generic-module + SFC 扩展迁移方案

## 迁移思路

将传统"四件套"（router.js / store.js / main.vue / add.vue）拆解为：

1. **数据库配置** — tss_module_page + tss_module_button + tss_resuipc（m18 可视化配置）
2. **SFC 扩展 JS** — 按钮显隐 / 自定义操作（endisable / updateTPMDATA）
3. **Store 扩展** — 自定义查询接口（querySel / endisable / updateTPMDATA）
4. **字段配置全走 m18 uiSetFull** — 4 个 AutoComplete + 2 个 rs-uploader-template 均通过 EDITTYPE + SELECTDATA + UPDATEFIELDS 配置，不写 field slot

### 关键信息

| 项 | 值 |
|---|---|
| moduleCode | `LI_M01` |
| 资源主表 | `TBS_PROJECT`（假设，实际以元数据为准） |
| 列表 APICODE | A01（query） |
| 打开 APICODE | A02（open） |
| 保存 APICODE | A04（save） |
| 自定义 actions | add / endisable（A07） / updateTPMDATA（A08） / querySel（A06） |
| 选择器 | dept / tstdd / reguitem / 自定义 querySel（项目/检定项） |
| 审批流 | 有（待提交 → 待审核 → 已审核，无审批环节） |
| 文件上传 | 2 个 rs-uploader-template（YSJL 原始记录模板 + ZS 证书模板） |

---

## 一、数据库配置（m18 可视化配置）

### 1.1 模块页面配置 (tss_module_page)

| PAGECODE | PAGENAME | PAGETYPE | ROUTEPATH | QUERY_APICODE | OPEN_APICODE | SAVE_APICODE |
|----------|----------|----------|-----------|---------------|--------------|--------------|
| main | 项目管理 | list | /g/LI_M01/main | A01 | - | - |
| add | 项目编辑 | form | /g/LI_M01/add | - | A02 | A04 |

### 1.2 main 页 PAGECONFIG

```json
{
  "QRYPATH": "QRY",
  "QQRYSPATH": "QQRY",
  "defaultFormPageCode": "add",
  "EXTENDJS": "@/modules/LI_M01/main.js"
}
```

### 1.3 add 页 PAGECONFIG

```json
{
  "MAINPATH": "MAIN",
  "FORMLAYOUT": "twocolumn",
  "EXTENDJS": "@/modules/LI_M01/form.js"
}
```

### 1.4 按钮配置 (tss_module_button)

#### main 列表页按钮

| BTNNAME | BTNCODE | BTNAREA | BTNTYPE | APICODE | PERMCODE | INTERACTTYPE | SHOWCOND | EXTPARAM |
|---------|---------|---------|---------|---------|----------|--------------|----------|----------|
| 添加 | add | footer | crud | A04 | LI_M01/A03 | - | - | - |
| 启用/禁用 | custom | row | - | A07 | LI_M01/A07 | - | - | `{"action":"endisable"}` |
| 编辑模板 | custom | row | - | A08 | LI_M01/A08 | - | - | `{"action":"edittemplate"}` |

#### add 表单页按钮（审批流）

| BTNNAME | BTNCODE | BTNAREA | APICODE | PERMCODE | INTERACTTYPE | SHOWCOND | COLOR |
|---------|---------|---------|---------|----------|--------------|----------|-------|
| 暂存 | save | footer | A04 | LI_M01/A03 | - | `ISSHOWSAVE` | primary |
| 删除 | delete | footer | A07 | LI_M01/A03 | poptip | `ISSHOWDELETE` | red |
| 提交 | submit | footer | A08 | LI_M01/A08 | - | `ISSHOWSUBMIT` | primary |
| 撤销提交 | reSubmit | footer | A09 | LI_M01/A09 | poptip | `ISSHOWRESUBMIT` | red |
| 审核 | check | footer | A10 | LI_M01/A10 | - | `ISSHOWCHECK` | primary |
| 撤销审核 | reCheck | footer | A11 | LI_M01/A11 | poptip | `ISSHOWRECHECK` | red |

**说明**：

- `BTNCODE` 为 `save` / `delete` / `submit` / `reSubmit` / `check` / `reCheck` 时，generic-form.vue 的 `handleBtn` 会自动分发到 Add01 mixin 的同名方法（`this.save()` / `this.submit(ID)` / `this.check(ID)` 等），无需在扩展 JS 中手写调用。
- `ISSHOWSAVE` / `ISSHOWDELETE` / `ISSHOWSUBMIT` / `ISSHOWRESUBMIT` / `ISSHOWCHECK` / `ISSHOWRECHECK` 已由 Add01 mixin 内置为 computed（依据 `this.STATE` 判断），SHOWCOND 直接引用即可。
- 原代码中审核/撤销审核的 PERMCODE 分别为 `LI_M01/A10` / `LI_M01/A11`，注意原代码 v-per 写的是 `LI_M01/A08`（提交）和 `LI_M01/A09`（撤销提交），但实际 APICODE 是 A10/A12/A13 等，以 tss_funcpoint 配置为准。

---

## 二、字段配置（m18 uiSetFull，不写 field slot）

### 2.1 AutoComplete 选择器字段（4 个）

所有 AutoComplete 通过 `EDITTYPE=autocomplete` + `SELECTDATA` JSON 配置，由 rs-form-cell 内部的 autocomplete 渲染器自动处理。

| 字段名 | LABELNAME | EDITTYPE | SELECTDATA | UPDATEFIELDS | 说明 |
|--------|-----------|----------|------------|--------------|------|
| DEPTNAME | 部门 | autocomplete | `{"selType":"dept"}` | `DEPTID,ID;DEPTNAME,DEPTNAME` | 部门选择器 |
| TSTANDARDNAME | 检校依据 | autocomplete | `{"selType":"tstdd"}` | `TSTANDARDID,ID;TSTANDARDNAME,STDDNAME` | 测量标准选择器 |
| REGUITEMNAME | 检定项 | autocomplete | `{"selType":"reguitem","titleName":"ITEMNAME"}` | `REGUITEMID,ID;REGUITEMCODE,ITEMCODE;REGUITEMNAME,ITEMNAME` | 规程制度选择器 |
| TPMNAME | 项目 | autocomplete | `{"module":"LI_M01","apiCode":"A06","keyName":"ID","titleName":"TPMNAME"}` | `REFTPMID,ID;TPMNAME,TPMNAME` | 自定义查询接口 |

**配置要点**：

1. **DEPTNAME**：使用预设 `dept`（底层 RS_M00/A05），UPDATEFIELDS 中 `DEPTID,ID` 表示选中后将选项的 `ID` 字段写入主表的 `DEPTID`，`DEPTNAME,DEPTNAME` 表示将选项的 `DEPTNAME` 写入主表的 `DEPTNAME`。

2. **TSTANDARDNAME**：使用预设 `tstdd`（底层 RS_M00/A07），注意显示字段是 `STDDNAME`（数据库字段名），但主表字段名是 `TSTANDARDNAME`，所以 UPDATEFIELDS 写 `TSTANDARDNAME,STDDNAME`。

3. **REGUITEMNAME**：使用预设 `reguitem`（底层 RS_M00/A12），但需要覆盖 `titleName` 为 `ITEMNAME`（数据库实际返回字段），UPDATEFIELDS 同时回填 `REGUITEMCODE`。

4. **TPMNAME**：不使用预设（无匹配的 selType），直接配置 `module=LI_M01` + `apiCode=A06`，对应 store.js 中的 `querySel` action。该接口走自定义后端查询。

### 2.2 文件上传字段（2 个 rs-uploader-template）

| 字段名 | LABELNAME | EDITTYPE | SELECTDATA（uploaderTplConfig） | UPDATEFIELDS | 说明 |
|--------|-----------|----------|--------------------------------|--------------|------|
| EXPTEMPFILENAME | 原始记录模板 | fileuploadtpl | `{"templateType":"YSJL","maxFileSize":"1mb","showSelect":true}` | `EXPTEMP,id;EXPTEMPFILENAME,name` | 原始记录模板上传 |
| CERTEMPFILENAME | 证书模板 | fileuploadtpl | `{"templateType":"ZS","maxFileSize":"1mb","showSelect":true}` | `CERTEMP,id;CERTEMPFILENAME,name` | 证书模板上传 |

**配置要点**：

1. `templateType` 对应原代码中 `rs-uploader-template` 的 `template-type` prop（`YSJL` = 原始记录，`ZS` = 证书）。
2. `maxFileSize` 对应原代码中 `options.max_file_size: '1mb'`。
3. `UPDATEFIELDS` 配置 `id→EXPTEMP` + `name→EXPTEMPFILENAME` 的双向映射，rs-uploader-template 选中后返回 `{id, name}` 对象，rs-form-cell 自动按映射写回两个字段。
4. `showSelect: true` 显示"选入模板"按钮（对应原 rs-uploader-template 的默认行为）。

### 2.3 普通字段

| 字段名 | LABELNAME | EDITTYPE | 说明 |
|--------|-----------|----------|------|
| DOCCODE | 文档编号 | text | 单据号 |
| DOCTITLE | 文档标题 | text | - |
| CERTCODE | 证书编号 | text | - |
| EXPDATE | 有效日期 | datepicker | - |
| ISUSE | 是否启用 | select | SELECTDATA: `1:启用,0:停用` |
| STATE | 状态 | text | 隐藏字段（不显示在表单，但需在 DataTable 中） |

### 2.4 隐藏字段（EDITSORT=0 或不配）

以下字段需要在 DataTable 中存在（用于数据保存），但不在表单显示：

`ID` / `REFTPMID` / `TSTANDARDID` / `REGUITEMID` / `REGUITEMCODE` / `DEPTID` / `EXPTEMP` / `CERTEMP` / `CREATEID` / `CREATETIME`

---

## 三、Store 扩展 JS

路径：`@/modules/LI_M01/store.js`

保留原 `store.js` 中的自定义 actions，其余 CRUD 操作（query / open / save / delete / submit / check 等）由 Store03 内置。

```javascript
/**
 * LI_M01 Store 扩展
 *
 * Store03 默认 actions（query/open/add/save/delete/submit/reSubmit/check/reCheck/...）
 * 已内置，此处仅保留自定义 actions。
 *
 * 原 store.js 中的 SelStore.mixActions()（deptSel/tstddSel/reguitemSel）不再需要，
 * 因为字段选择器走 rs-form-cell 的 autocomplete + selRegistry 自动处理。
 */
export default {
  actions: {
    /**
     * 新增（覆盖 Store03 默认 add）
     * 原代码设置 ISUSE=1 默认值
     */
    add({ commit }) {
      commit('INIT', { paths: ['MAIN'] });
      commit('ADD', { path: 'MAIN', item: { ISUSE: 1 } });
    },

    /**
     * 启用/禁用切换（A07）
     * 原 store.js endisable action
     *
     * @param {Object} item - 当前行数据
     */
    async endisable({ commit, dispatch }, { item }) {
      var UPDATE = this.storeHelper.getTable('UPDATE');
      UPDATE.setValue('ISUSE', item.ISUSE === 1 ? 0 : 1);
      UPDATE.setValue('ID', item.ID);
      // 注：storeHelper 在 generic-store 中通过模块上下文获取
      // 实际写法依赖 applyStoreExtend 注入的 helpers
      var ret = await dispatch('call', {
        APICODE: 'A07',
        params: {
          UPDATE: UPDATE.getXML(),
        },
      });
      if (ret && ret.length > 0) {
        for (var key in ret[0]) {
          item[key] = ret[0][key];
        }
      }
    },

    /**
     * 更新模板数据（A08）
     * 原 store.js updateTPMDATA action
     *
     * @param {Object} item - 含 TPMDATA 字段的行数据
     */
    async updateTPMDATA({ commit, dispatch }, { item }) {
      var UPDATE = this.storeHelper.getTable('UPDATE');
      UPDATE.setValue('TPMDATA', item.TPMDATA);
      UPDATE.setValue('ID', item.ID);
      var ret = await dispatch('call', {
        APICODE: 'A08',
        params: {
          UPDATE: UPDATE.getXML(),
        },
      });
      if (ret && ret.length > 0) {
        for (var key in ret[0]) {
          item[key] = ret[0][key];
        }
      }
    },

    /**
     * 自定义选择器查询（A06）
     * 原 store.js querySel action
     * 查询项目（TPM）数据供 AutoComplete 使用
     *
     * 注意：TPMNAME 字段在 uiSetFull 中配置为 module=LI_M01, apiCode=A06，
     * rs-form-cell 的 autocomplete 会自动调用 /api/data/call/LI_M01/A06/，
     * 不再需要此 action。但如果 A06 接口有特殊参数处理，可保留用于扩展调用。
     */
    async querySel({ state, commit }, { INPUT }) {
      // 此 action 保留供 main.js 扩展中手动调用（如有需要）
      // 字段 autocomplete 选择器不再经过这里
    },
  },
};
```

**说明**：

- 原 store.js 使用 `new SelStore()` 注入 `deptSel` / `tstddSel` / `reguitemSel` 等 action，迁移后这些选择器走 `rs-form-cell` 的 autocomplete + `selRegistry.buildAutoCompleteOption` 自动处理，**不再需要 SelStore**。
- 原 store.js 的 `querySel` action 直接调 `db.postData` 查询 LI_M01/A06 接口，迁移后该接口由 `rs-form-cell` 的 autocomplete 自动调用（SELECTDATA 中配置了 `module=LI_M01, apiCode=A06`），**store 扩展中不需要再定义**。
- `endisable` 和 `updateTPMDATA` 是自定义业务逻辑（非标准 CRUD），必须保留。

---

## 四、main 页 SFC 扩展 JS

路径：`@/modules/LI_M01/main.js`

```javascript
/**
 * 项目管理列表页扩展
 *
 * this 上下文 (generic-module):
 *   this.moduleCode            - 'LI_M01'
 *   this.storeName             - Vuex 模块命名空间
 *   this.storeObj              - { mapState, mapGetters, mapDateTable, Constants, storeHelper }
 *   this.$refs.list            - list-t01 组件实例
 *   this.selectedRows          - 选中的行数组
 *   this.$callAction           - 调用 store action
 *   this.$alert / $error / $confirm / $busy / $free
 *
 * 行操作按钮通过 tss_module_button BTNCODE=custom + EXTPARAM.action 分发：
 *   EXTPARAM = {"action":"endisable"}    -> this.endisable(row)
 *   EXTPARAM = {"action":"edittemplate"} -> this.edittemplate(row)
 */
export default {
  computed: {
    // 列表页暂无按钮显隐逻辑（原 main.vue 也无）
  },

  methods: {
    /**
     * 启用/禁用切换（行操作按钮）
     * 对应 tss_module_button: BTNCODE=custom, EXTPARAM={"action":"endisable"}
     *
     * 原 main.vue endisable 方法
     */
    async endisable(row) {
      await this.$callAction({
        action: this.moduleCode + '/endisable',
        param: { item: row },
        successText: '操作成功',
      });
      this.$refs.list.query();
    },

    /**
     * 编辑模板（行操作按钮）
     * 对应 tss_module_button: BTNCODE=custom, EXTPARAM={"action":"edittemplate"}
     *
     * 原 main.vue edittemplate 方法（仅 alert 示例，实际业务待定）
     */
    edittemplate(row) {
      var data = row.TPMDATA;
      // 弹出模板编辑器（具体实现视业务而定）
      this.$alert('编辑模板：' + row.TPMNAME);
    },
  },
};
```

---

## 五、add 页 SFC 扩展 JS

路径：`@/modules/LI_M01/form.js`

```javascript
/**
 * 项目管理表单页扩展
 *
 * this 上下文 (generic-form):
 *   this.ID                    - 主键
 *   this.STATE                 - 单据状态（1=待提交, 2=待审核, 3=已审核）
 *   this.CREATEID              - 创建人ID
 *   this.DOCCODE / this.TPMNAME / ...  - 主表字段直接读写
 *   this.$MAIN                 - 主表 DataTable
 *   this.MAIN                  - 主表数据
 *   this.storeName             - Vuex 命名空间
 *   this.moduleCode            - 'LI_M01'
 *   this.save() / this.del() / this.submit(ID) / this.check(ID) / ...  - Add01 mixin 内置
 *   this.$callAction / this.$alert / this.$error / this.$confirm
 *
 * 审批流按钮显隐（ISSHOWSAVE / ISSHOWDELETE / ISSHOWSUBMIT / ISSHOWRESUBMIT / ISSHOWCHECK / ISSHOWRECHECK）
 * 已由 Add01 mixin 内置，tss_module_button SHOWCOND 直接引用即可，**此处不重复定义**。
 *
 * 表单禁用逻辑：非"待提交"状态时表单只读（对应原代码 :disabled="!ISSHOWSAVE"）
 */
export default {
  computed: {
    /**
     * 表单禁用状态
     * 原代码 rs-form-edit 的 :disabled="!ISSHOWSAVE"
     * Add01 的 ISSHOWSAVE 在 STATE=空/1 时为 true，其余为 false
     */
    formDisabled() {
      // 仅在待提交/新增状态可编辑
      return !this.ISSHOWSAVE;
    },
  },

  mounted() {
    // 设置 rs-form-edit 的 disabled 状态（覆盖 scm 配置）
    // 注：generic-form 的 rs-form-edit 通过 visibilityHost 获取 ISSHOWxxx
    //     formDisabled 可在 PAGECONFIG 中通过 "DISABLED":"formDisabled" 引用
    //     或通过 rs-form-edit 的 overrides 动态设置
  },
};
```

**说明**：

- LI_M01 表单页逻辑较简单，审批流按钮显隐全由 Add01 mixin 处理，无需在 form.js 中重复定义 ISSHOWxxx。
- 原代码中 4 个 AutoComplete 的选择器逻辑（remoteMethod2 / deptSel / tstddSel / reguitemSel）已由 rs-form-cell 的 autocomplete + selRegistry 完全接管，不再需要。
- 原代码中 `TPM` / `TTSTDD` / `TREGUITEM` / `TDEPT` 等 computed（get/set 联动写入 ID + Name）已由 rs-form-cell 的 `UPDATEFIELDS` 机制自动处理，不再需要。

---

## 六、迁移对照表

| 原 r01/m01 文件/逻辑 | 迁移后位置 | 说明 |
|---------------------|-----------|------|
| `router.js` | **删除** | generic-module 路由自动注册，菜单 OUTERURL=`/g/LI_M01/main` |
| `index.js` | **删除** | 不再需要模块入口 |
| `store.js` 中 SelStore.mixActions() | **删除** | deptSel/tstddSel/reguitemSel 由 selRegistry 自动处理 |
| `store.js` 中 SelStore.mixPaths() | **删除** | paths 由 generic-store 自动从模块配置获取 |
| `store.js` 中 `add` action | `@/modules/LI_M01/store.js` | 保留（ISUSE=1 默认值） |
| `store.js` 中 `endisable` action | `@/modules/LI_M01/store.js` | 保留（A07 启用/禁用） |
| `store.js` 中 `updateTPMDATA` action | `@/modules/LI_M01/store.js` | 保留（A08 模板数据更新） |
| `store.js` 中 `querySel` action | **删除** | autocomplete 自动调用 LI_M01/A06 |
| `views/main.vue` 列表模板 | m18 配置（tss_module_page） | PAGETYPE=list |
| `views/main.vue` endisable 方法 | `@/modules/LI_M01/main.js` | 行操作按钮 |
| `views/main.vue` edittemplate 方法 | `@/modules/LI_M01/main.js` | 行操作按钮 |
| `views/main.vue` checkbox=true | m18 按钮配置自动推导 | 有 BTNTYPE=batch 按钮时自动开启 |
| `views/add.vue` 表单模板 | m18 配置（tss_module_page） | PAGETYPE=form, FORMLAYOUT=twocolumn |
| `views/add.vue` 4 个 AutoComplete slot | **m18 uiSetFull 字段配置** | EDITTYPE=autocomplete + SELECTDATA + UPDATEFIELDS |
| `views/add.vue` 2 个 rs-uploader-template slot | **m18 uiSetFull 字段配置** | EDITTYPE=fileuploadtpl + SELECTDATA(uploaderTplConfig) |
| `views/add.vue` TPM/TTSTDD/TREGUITEM/TDEPT computed | **删除** | UPDATEFIELDS 自动处理联动 |
| `views/add.vue` remoteMethod2/deptSel/tstddSel/reguitemSel | **删除** | autocomplete 内部自动 loadData |
| `views/add.vue` CERTEMPFILES/EXPTEMPFILES computed | **删除** | UPDATEFIELDS 自动处理 id↔CERTEMP, name↔CERTEMPFILENAME |
| `views/add.vue` 暂存按钮 | m18 按钮配置 | BTNCODE=save → Add01.save() |
| `views/add.vue` 删除按钮 | m18 按钮配置 | BTNCODE=delete → Add01.del() |
| `views/add.vue` 提交按钮 | m18 按钮配置 | BTNCODE=submit → Add01.submit(ID) |
| `views/add.vue` 撤销提交按钮 | m18 按钮配置 | BTNCODE=reSubmit → Add01.reSubmit(ID) |
| `views/add.vue` 审核按钮 | m18 按钮配置 | BTNCODE=check → Add01.check(ID) |
| `views/add.vue` 撤销审核按钮 | m18 按钮配置 | BTNCODE=reCheck → Add01.reCheck(ID) |
| `mixins/add01.js` | generic-form 内置 mixin | ISSHOWSAVE/ISSHOWDELETE/... + save/submit/check/... |

---

## 七、迁移后目录结构

```
src/modules/LI_M01/           # SFC 扩展资产（数据库 tss_code_asset）
  store.js                    # Store 扩展（add/endisable/updateTPMDATA 自定义 actions）
  main.js                     # 列表页扩展（endisable/edittemplate 行操作）
  form.js                     # 表单页扩展（formDisabled computed）

src/pages/r01/m01/            # 原目录（迁移后可删除）
  router.js                   # 删除
  store.js                    # 删除
  index.js                    # 删除
  views/main.vue              # 删除
  views/add.vue               # 删除
```

---

## 八、迁移收益

| 维度 | 迁移前 | 迁移后 |
|------|--------|--------|
| 文件数 | 5 个（router/store/index/main/add） | 3 个 SFC（store/main/form.js） |
| 代码行数 | ~280 行（store 80 + main 75 + add 275） | ~120 行（store 50 + main 35 + form 35） |
| AutoComplete 代码 | 4 个 slot + 4 个 computed + 4 个 methods | **0 行**（uiSetFull 配置） |
| 文件上传代码 | 2 个 slot + 2 个 computed + options | **0 行**（uiSetFull 配置） |
| 审批流按钮 | 6 个 Button + v-per + v-if + Poptip | **0 行**（m18 按钮配置 + Add01 内置） |
| 路由注册 | router.js + require.ensure | **不需要**（菜单 OUTERURL） |
| 选择器依赖 | SelStore + mixPaths/mixActions | **不需要**（selRegistry 自动） |

---

## 九、注意事项

### 9.1 reguitem 预设的 titleName 差异

`selRegistry.js` 中 `reguitem` 预设的 `titleName` 是 `REGUITEMNAME`，但 LI_M01 实际使用的字段名是 `ITEMNAME`（数据库返回）。在 uiSetFull 配置中必须覆盖：

```json
SELECTDATA: {"selType":"reguitem","titleName":"ITEMNAME"}
```

否则 AutoComplete 的下拉列表显示为空（找不到 `REGUITEMNAME` 字段）。

### 9.2 UPDATEFIELDS 格式

UPDATEFIELDS 使用 `本地字段,远程字段;本地字段,远程字段` 格式（分号分隔多组），rs-form-cell 在 autocomplete 选中后自动写入：

```
DEPTID,ID;DEPTNAME,DEPTNAME
│       │      │        │
│       │      │        └─ 远程字段名（选项对象的 key）
│       │      └─ 本地字段名（主表 DataTable 的字段）
│       └─ 远程字段名
└─ 本地字段名
```

### 9.3 A06 接口（querySel）的兼容性

原 `querySel` action 调用 `/api/data/call/LI_M01/A06/`，参数为 `{PageSize:20, PageIndex:1, FilterParams:{INPUT}}`。

迁移后 autocomplete 自动调用相同接口，但参数格式为 `{PageSize:1, PageIndex:1, FilterParams:{INPUT, ID:'-1'}}`（selRegistry 的 `callModuleApi` 默认注入 `ID:'-1'`）。

需确认 A06 后端接口对 `ID:'-1'` 参数的处理不会导致异常（通常 F01 过滤器中 `ID` 非空时走单条查询，`-1` 表示不匹配任何记录，仅靠 `INPUT` 模糊搜索）。

### 9.4 ISUSE 字段

原 `store.js` 的 `add` action 设置 `ISUSE: 1` 作为默认值。迁移后 `add` action 保留在 store 扩展中，覆盖 Store03 默认行为。

### 9.5 组件 name 与 keep-alive

迁移后 generic-module 的组件 name 由路由自动生成（格式 `{业务码}-{模块码}-main`），需确保与 `cachedViews` 匹配规则一致。`/g/LI_M01/main` 生成的 name 为 `LI_M01-main`（斜杠转连字符）。

### 9.6 菜单配置

`tss_func.OUTERURL` 设置为 `/g/LI_M01/main`，系统在 `app.initModule` 时自动调用 `registerGenericRoute('LI_M01')` 注册路由。

原菜单 `tss_func` 中 LI_M01 的 OUTERURL 可能指向 `/r01/m01/main`，需更新为 `/g/LI_M01/main`。

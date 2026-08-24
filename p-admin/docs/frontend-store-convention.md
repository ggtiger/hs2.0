# 前端 Store 数据流强制规范

> 本文档是 `p-admin/` 前端代码的**强制规范基线**，违反规则的代码无法通过 ESLint 校验。
> 配套 ESLint 规则：`@/api/db` 禁止在 `.vue` 中 import，`this.$store.dispatch` 禁止在业务代码中调用。

## 一、四条强制规则

### 规则 1：接口调用必须通过 Store action

**禁止**在 `.vue` 文件中 `import db from '@/api/db'` 然后调用 `db.postData / db.call / db.open / db.openTables / db.getNewID`。

`@/api/db` 是 Store 层的私有依赖，只能被 `store.js / createStore.js / *-store.js` 引入。

### 规则 2：业务数据存放在 DataTable

业务数据由 Store 通过 `BaseStore.mixState()` 自动创建 `dt.{path}`（DataTable 实例）。读写字段必须用：

- `storeHelper.getTable(path).setValue(field, value)`
- `storeHelper.getTable(path).getValue(field)`
- `storeHelper.getTable(path).getXML()` — 序列化为后端约定的 XML（含 `<a>` `<m>` `<d>` 增改删）

### 规则 3：模板双向绑定用 `mapDateTable`

```js
import { mapDateTable } from '@/pages/{业务}/{模块}/store';

export default {
  computed: {
    ...mapDateTable('MAIN', ['EMPNAME', 'EMPCODE', 'ISUSE']),
  },
};
```

`mapDateTable` 由 `BaseStore.mapGetters` 生成，为每个字段产生 `get/set`，自动读写 DataTable（无需手写 `v-model` + watcher）。

### 规则 4：调用 Store action 用 `$callAction`

```js
// ✅ 正确
this.$callAction({
  action: 's01/m05/endisable',
  param: { item },
  successText: '操作成功',
});

// ❌ 禁止
this.$store.dispatch('s01/m05/endisable', { item });
```

`$callAction` 统一接管：busy loading、登录超时跳转、错误 `$error` 弹窗、成功回调。

> 例外：基础设施组件（`createStore.js / Store03.js / BaseStore.js`）内部允许 `Store.dispatch`，业务页面禁止。

---

## 二、 Vuex 四层分层标准（强制）

按 Vuex 标准结构组织，**禁止在 .vue 中做数据加工（filter/map/transform）**。

| 层 | 职责 | 示例 |
|---|---|---|
| **state** | 原始数据（服务端返回原样存） | `state: { assets: [], moduleAssets: [] }` |
| **getters** | 派生数据（filter/map/sort 等纯函数） | `csharpAssets: s => s.assets.filter(a => a.ASSETTYPE === 'csharp').map(...)` |
| **mutations** | 同步修改 state（唯一入口） | `SET_ASSETS(state, rows) { state.assets = rows }` |
| **actions** | 异步操作（请求 + commit mutation） | `async loadAssets({ commit }) { const ret = await db.postData(...); commit('SET_ASSETS', ret.Items || []); }` |

> **铁律**：服务端返回的 raw 数据先进 state，组件用 `mapGetters` 取派生形态。.vue 的 `<script>` 只剩"事件 → $callAction → 绑定"。

### 反例（错误做法）

```js
// ❌ action 返回 raw ret，组件再加工
async listAssets(ctx, { assetType }) {
  return await db.postData({...});
}
// .vue 里:
const ret = await this.$callAction({...});
this.groups[0].items = ret.Items.filter(...).map(...);  // ❌ 数据加工在 .vue 里
```

### 正例（标准做法）

```js
// ✅ state 存 raw, getters 派生, action 只管 fetch+commit
const { mapState, mapGetters } = createStore.getStore({
  config: { moduleCode: 'CEP' },
  storeName: 'cep',
  state: { assets: [], moduleCode: '' },
  getters: {
    csharpAssets: s => s.assets
      .filter(a => (a.ASSETTYPE || '') === 'csharp')
      .map(a => ({ rid: a.ID, code: a.CODE, name: a.NAME, path: a.MODULEPATH })),
    sqlAssets: s => s.assets
      .filter(a => (a.ASSETTYPE || '') === 'sql')
      .map(a => ({ rid: a.ID, code: a.CODE, name: a.NAME, path: a.MODULEPATH })),
  },
  mutations: {
    SET_ASSETS(s, rows) { s.assets = rows || []; },
    SET_MODULE_CODE(s, code) { s.moduleCode = code; },
  },
  actions: {
    async loadAssets({ commit }) {
      const ret = await db.postData({...});
      commit('SET_ASSETS', (ret && ret.Items) || []);
    },
  },
});

// .vue 里：
computed: {
  ...mapGetters(['csharpAssets', 'sqlAssets']),
}
methods: {
  refresh() {
    this.$callAction({ action: 'cep/loadAssets', isBusy: false });
  },
}
```

### 何时用 DataTable vs Vuex state

| 场景 | 用什么 |
|---|---|
| 列表/表单（用户增删改、提交后端 save） | **DataTable**（通过 `mapDateTable` 双向绑定） |
| 派生数据（下拉、分组、过滤结果） | **Vuex state + getters** |
| 弹窗临时 UI 数据（visible/active 等） | **组件 data** |

> DataTable 是 Store03 通过 `BaseStore.mixState()` 自动创建的 `dt.{path}`，专门承载"可编辑、可序列化为 XML 提交后端"的业务数据。其他只读/派生数据走标准 Vuex state + getters。

### 实战案例：弹窗类组件的 state/getter 分层（code-editor-popup）

弹窗同时有"列表展示"和"命令式 RPC"两种数据流，必须区分对待。

```js
// code-editor-store.js
const { mapGetters } = createStore.getStore({
  config: { moduleCode: 'CEP' },
  storeName: 'cep',
  state: {
    moduleAssets: [],      // RS_M18/A06 返回的原始行（含 KIND=1/2/3）
    allAssets: [],         // RS_M17/A01 返回的原始行（含 ASSETTYPE）
    moduleMode: false,     // 是否处于模块上下文（决定 getter 从哪个源派生）
    selectorAssets: { csharp: [], sql: [], js: [] }, // 选入面板原始行
  },
  getters: {
    // 派生：按 mode 自动从不同源过滤+映射
    groupCsharp: s => s.moduleMode
      ? s.moduleAssets.filter(r => r.KIND === 1).map(toModuleItem)
      : s.allAssets.filter(r => r.ASSETTYPE === 'csharp').map(toAllAssetItem),
    groupSql: /* 同上 */,
    groupJs: /* 同上 */,
    // 选入面板：getter 内完成"排除已在当前组"+映射
    selectorItemsCsharp: (s, g) => filterSelectorItems(s.selectorAssets.csharp, g.groupCsharp),
  },
  mutations: {
    SET_MODULE_ASSETS(s, rows) { s.moduleAssets = rows || []; },
    SET_ALL_ASSETS(s, rows) { s.allAssets = rows || []; },
    SET_SELECTOR_ASSETS(s, { kind, rows }) { s.selectorAssets[kind] = rows || []; },
  },
  actions: {
    // 列表加载：fetch + commit（组件不再拿 ret 自己处理）
    async loadModuleAssets({ commit }, { moduleCode }) {
      const ret = await db.postData({...});
      commit('SET_MODULE_MODE', true);
      commit('SET_MODULE_ASSETS', ret);
    },
    // RPC 类（命令式，调用方需要拿 apiCode/ID/message 做后续逻辑）
    // 直接 return Promise，不进 state
    linkAsset(ctx, { moduleCode, kind, code, apiName }) {
      return db.postData({...});
    },
  },
});
```

```vue
<!-- code-editor-popup.vue -->
<script>
import { Constants as CEP, mapGetters as cepMapGetters } from './code-editor-store';
export default {
  computed: {
    ...cepMapGetters(['groupCsharp', 'groupSql', 'groupJs',
                      'selectorItemsCsharp', 'selectorItemsSql', 'selectorItemsJs']),
    // 左侧三组列表：items 全部由 store getter 派生（filter/map 不在 .vue 里）
    groups() {
      return [
        { kind: 'csharp', label: 'API 脚本', items: this.groupCsharp },
        { kind: 'sql', label: 'SQL 模板', items: this.groupSql },
        { kind: 'js', label: 'JS 模块', items: this.groupJs },
      ];
    },
  },
  methods: {
    async loadGroups() {
      // 组件只剩"选哪个 action"的纯控制逻辑
      if (this.moduleCode) {
        await this.$callAction({ action: 'cep/loadModuleAssets', param: { moduleCode: this.moduleCode } });
      } else {
        await this.$callAction({ action: 'cep/loadAllAssets' });
      }
    },
    async openSelector(kind) {
      await this.$callAction({ action: 'cep/loadSelectorAssets', param: { kind } });
      // _checked 是纯 UI 状态，从 getter 拷贝一份到本地 data 翻转
      const derived = kind === 'csharp' ? this.selectorItemsCsharp : /* ... */;
      this.selectorItems = derived.map(f => ({ ...f, _checked: false }));
    },
  },
};
</script>
```

**关键判断**：action 是否进 state？
- 返回值是"列表/集合/需要多次复用" → 进 state + getters 派生
- 返回值是"一次性的 RPC 结果"（如 linkAsset 返回 apiCode）→ 直接 return Promise，不进 state

### 铁律：严禁自己拼接 XML

后端约定的 XML（`<VSS_xxx l="u" c="..." t="..."><a><r c0="..." /></a></VSS_xxx>`）**必须**由 `DataTable.getXML()` 生成，禁止在 `.vue` 或 store action 里用字符串模板/`buildDataTableXML()` 手拼。

```js
// ❌ 严禁：手拼 XML
async createModuleBare({ commit }, { moduleCode, moduleName }) {
  const xml = `<VSS_MOUDLE l="u" c="MODULECODE,MODULENAME" t="varchar,varchar"><a><r c0="${moduleCode}" c1="${moduleName}"/></a></VSS_MOUDLE>`;
  return db.postData({ api: '/api/data/call/RS_M02/A04/', params: { VSS_MOUDLE: xml } });
}

// ✅ 正确：DataTable.add + getXML
async createModuleBare({ commit }, { moduleCode, moduleName }) {
  const sh = getStoreResult().storeHelper;
  commit('INIT', { paths: ['MAIN'] });
  const MAIN = sh.getTable('MAIN');
  MAIN.add({ MODULECODE: moduleCode, MODULENAME: moduleName });
  return db.postData({
    api: '/api/data/call/RS_M02/A04/',
    params: { VSS_MOUDLE: MAIN.getXML() },
  });
}
```

**DataTable 关键行为**（依赖这些特性，改动时勿破坏）：
- `dt.add(rowObj)` — 把对象追加为新行，生成 `<a><r .../></a>` 段
- `dt.update(rowObj)` — 比较同值，**同值写入是 no-op**（不标脏，不出现在 `<m>` 段），可安全用于"已加载行的幂等拷贝"
- `dt.initData([rows])` — `Object.assign({}, this.dataObj, item)` **克隆**行写入，编辑 dt 不反向污染源数据（如 app store 的 MODAPI 行）
- `dt.getXML()` — 序列化整张表的增/改/删段，自动处理转义、`oc` 旧值、回写 ID

### DataTable 双向别名模式（最小改动迁移）

当历史代码用驼峰/小写字段名（如 `templateCode`），但 ORM 字段全大写（`CODE`），可在 computed 里做 get/set 别名委托，**保留模板和方法里的原字段名**：

```js
import { mapDateTable } from '@/pages/s01/m17/store';

export default {
  computed: {
    // 1) 大写字段直绑（推荐用于新代码）
    ...mapDateTable('MAIN', ['ID', 'CODE', 'NAME', 'SOURCECODE', 'REMARK', 'MODULEPATH']),

    // 2) 双向别名（迁移历史代码用，保留 templateCode/sourceCode 等旧名）
    templateId:   { get() { return this.ID || '' },           set(v) { this.ID = v } },
    templateCode: { get() { return this.CODE || '' },         set(v) { this.CODE = v } },
    sourceCode:   { get() { return this.SOURCECODE || '' },   set(v) { this.SOURCECODE = v } },
    // ...
  },
};
```

> **注意**：get-only 别名（无 setter）的值在模板里赋值会静默失败（Vue computed 无 setter）。涉及 `v-model` 或方法里赋值的字段必须同时提供 set。

### JSON 文档字段例外（明确为例外）

**平坦一行**的业务字段才适合 DataTable。当字段是**嵌套/数组/树形 JSON 文档**（如编排接口的 `steps` 数组），保留组件 `data` 编辑缓冲，保存时序列化到 DataTable 的某个字符串字段（如 `APIPARAM`）：

```js
// script-flow-editor.vue
data() {
  return {
    steps: [],            // 编辑缓冲（数组，非平坦）
  };
},
methods: {
  async doSave() {
    // 序列化到 DataTable 字段 APIPARAM
    this.APIPARAM = JSON.stringify(this.steps);
    this.ACTIONCODE = 'script';
    await this.$callAction({ action: SFE.STORE_NAME + '/saveSteps' });
  },
},
```

> 判别标准：字段值能用 `<input v-model>` 直接编辑 → 进 DataTable；需要 `v-for + 子组件`编辑的嵌套结构 → 组件 data 缓冲 + 保存时序列化。

### $callAction 的 isBusy:false 约定

`isBusy: false` 表示**静默调用**（不显示全局 loading toast）。已修复历史 bug（曾无条件 `$busy()`）：

```js
// 静默调用（弹窗内的二次查询、轮询、后台预加载）
await this.$callAction({
  action: 's01/m22/loadReleases',
  isBusy: false,    // ✅ 现已真正跳过 busy
});

// 默认显示 loading（用户主动触发的主操作）
await this.$callAction({
  action: 'b01/m01/save',
  successText: '保存成功',
  // isBusy 默认 true
});
```

---

## 三、标准 `store.js` 模板

```js
import db from '@/api/db';               // ✅ store 层允许 import
import createStore from '@/store/createStore';
import { SelStore } from '@/store/SelStore';

const oSelStore = new SelStore();

const { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: 'LIB_M01', paths: oSelStore.mixPaths() },
  storeName: 'b01/m01',
  mutations: {
    SET_ENDISABLE(state, { item }) {
      const UPDATE = storeHelper.getTable('UPDATE');
      UPDATE.setValue('ISUSE', item.ISUSE === 1 ? 0 : 1);
      UPDATE.setValue('ID', item.ID);
    },
  },
  actions: {
    add({ commit }) {
      commit('INIT', { paths: ['MAIN', 'DTS'] });
      commit('ADD', { path: 'MAIN', item: { ISUSE: 1 } });
    },
    async endisable({ commit, dispatch }, { item }) {
      commit('SET_ENDISABLE', { item });
      await dispatch('call', {
        APICODE: 'A07',
        params: { UPDATE: storeHelper.getTable('UPDATE').getXML() },
      });
    },
    ...oSelStore.mixActions(),
  },
});

export { mapState, mapGetters, mapDateTable, Constants };
```

**关键点**：
- `createStore.getStore` 自动注册 Vuex 模块、生成 `mapDateTable`、合并 `Store03.mixActions/mixMutations`。
- 不再手写 `Store.registerModule(...)` 和 `mapDateTable = (path, fields) => storeHelper.mapGetters(...)`。
- 标准 CRUD（query/open/save/delete/batch/flowSave/submit/check/verify）由 `Store03.mixActions()` 提供，扩展时只需写差异 action。
- 跨 store 共享的下拉数据走 `SelStore.mixActions()`。

---

## 三、标准列表页 `main.vue` 模板

```vue
<template>
  <ListT01 :storeName="Constants.STORE_NAME" />
</template>

<script>
import ListT01 from '@/components/rs-template/list-t01.vue';
import { Constants } from '@/pages/b01/m01/store';

export default {
  components: { ListT01 },
  data() {
    return { Constants };
  },
};
</script>
```

**关键点**：列表页只把 `storeName` 透传给 `ListT01`；查询/分页/排序全部由 `ListT01 + Store03.query` 自动驱动，业务页无需重复写 `$store.dispatch`。

如需自定义操作（启用/停用、批量审核），在模板里发 `$callAction`：

```vue
<template>
  <Button @click="endisable(row)">启用/停用</Button>
</template>

<script>
export default {
  methods: {
    endisle(item) {
      this.$callAction({
        action: 'b01/m01/endisable',
        param: { item },
        successText: '操作成功',
      });
    },
  },
};
</script>
```

---

## 四、标准表单页 `add.vue` 模板

```vue
<template>
  <AddT01 :storeName="Constants.STORE_NAME">
    <rs-form-edit :columns="columns" />
  </AddT01>
</template>

<script>
import AddT01 from '@/components/rs-template/add-t01.vue';
import Add01 from '@/components/rs-template/mixin/add.js';
import { mapDateTable, Constants } from '@/pages/b01/m01/store';

export default {
  mixins: [Add01],
  computed: {
    ...mapDateTable('MAIN', ['EMPNAME', 'EMP CODE', 'ISUSE']),
    Constants() { return Constants; },
    columns() { /* ... */ },
  },
};
</script>
```

**关键点**：
- `Add01` mixin 自动处理 `ID ? open : add` 时机，并触发 `Store03.open/add`。
- `mapDateTable('MAIN', [...])` 把 MAIN 表字段映射为 `get/set`，与 `rs-form-edit` 的 `v-model` 直接绑定。
- 不需要手写 `this.$store.dispatch(...)` 或 `db.postData(...)`。

---

## 五、$callAction 用法

```js
this.$callAction({
  action: '{storeName}/{actionName}',  // 必填，例如 'b01/m01/save'
  param: { ... },                       // 透传给 action 的第二参数
  successText: '保存成功',              // 成功后 $alert 文案（可选）
  errorText: '保存失败',                // 失败后 $error 文案（可选，默认用 e.message）
  successCall: (ret) => { ... },        // 成功回调（可选）
  errorCall: () => { ... },             // 失败回调（可选）
  isBusy: true,                         // 是否显示全局 loading（默认 true）
  isSuccessBack: true,                  // 成功后自动关闭当前 Tab/Dropdown
  isErrorBack: false,
  timeOut: 0,                           // setTimeout 延迟（默认 0）
  successBackParams: null,              // 透传给 successCall 的额外参数
});
```

源码位置：`src/utils/extends.js:72`。

### `$callAction` 返回 Promise（关键）

`$callAction` **返回一个 Promise**，因此可以 `await`：

```js
// 1) fire-and-forget（旧版语义完全不变）
this.$callAction({ action: 'b01/m01/save', successText: '保存成功' });

// 2) 拿返回值
const ret = await this.$callAction({
  action: 's01/m17/listAssets',
  param: { assetType: 'csharp' },
});
const items = (ret && ret.Items) || [];

// 3) 自定义错误处理（覆盖默认 $error 弹窗语义需自己 catch）
try {
  const ret = await this.$callAction({ action: '...', param: { ... } });
  // 成功
} catch (e) {
  // 失败：默认 $error 弹窗已经弹过，这里再补自定义处理
}
```

> ⚠️ **注意**：当用 `await` 时，框架已经弹过 `$error` 弹窗。如果你不想让框架弹窗（例如测试面板要行内显示错误），请在 **store action 内部** `try/catch` 并返回 `{ ok: false, message }` 信封（参考 `code-test-store.js`），让 action 永不 reject。

### 异步调用三选一

| 场景 | 推荐写法 |
|---|---|
| 普通 fire-and-forget | `this.$callAction({ action, successText })` |
| 需要拿返回值 / 控制后续流程 | `const ret = await this.$callAction({ action, param })` |
| 需要自定义错误展示 | store action 内 try/catch 返回结果信封，组件 `$callAction` 用 `successCall` 接收 |

**不再使用** `await this.$store.dispatch(...)` —— 任何 await 场景都走 `$callAction`，否则 ESLint 规则告警。

> 仅框架内部 action（`app/initScms`、`app/initModule`、`assistant/*`）允许 `this.$store.dispatch`，用 `// eslint-disable-next-line no-restricted-syntax` 标注。

---

## 六、例外白名单

下列组件**不**走 `api/data/call` 通用通道，本轮豁免 ESLint 规则：

| 目录 / 文件 | 涉及接口 | 豁免理由 |
|---|---|---|
| `src/components/rs-uploader/*` | `/api/upload` | 文件上传，专用 endpoint |
| `src/components/rs-uploader-template/*` | 模板上传 | 专用 endpoint |
| `src/components/rs-onlyoffice-preview/*` | `/field-queue` 等 | OnlyOffice DS 协同 |
| `src/components/edit/ueditor/*` | 富文本相关 | UEditor 内置协议 |
| `src/components/rs-word-template-editor/*` | Word 模板 | 专用 endpoint |
| `src/store/createStore.js`、`src/store/Store03.js`、`src/store/BaseStore.js`、`src/store/SelStore.js` | 框架内部 | Store 框架本身就是 `$store.dispatch` 的实现层 |
| `src/api/db.js` | db 自身 | 网络层 |
| `src/utils/extends.js` | `$callAction` 自身 | 内部封装 `$store.dispatch` |

> **判断规则**：
> - 用 `db.getUrl(type)` 取 url 后**非** `postData` 的调用，不算违规。
> - 走 `db.postData({api:'/api/data/call/...'})` 的，**必须**迁移到 Store action。

---

## 七、迁移步骤（针对违规文件）

1. 在模块目录下找/建 `store.js`，确保走 `createStore.getStore`。
2. 把 `.vue` 里的 `db.postData({api:'/api/data/call/{MC}/{AC}', params})` 改写为：
   - 在 `store.js` 中追加 action：
     ```js
     actions: {
       async myAction({ commit, dispatch }, { param1, param2 }) {
         const ret = await db.postData({
           api: '/api/data/call/MC/AC/',
           params: { ... },
         });
         commit('INITDATA', { path: 'MAIN', data: ret.MAIN || [] });
         return ret;
       },
     }
     ```
   - 在 `.vue` 中改：
     ```js
     this.$callAction({
       action: 'storeName/myAction',
       param: { param1, param2 },
       successCall: (ret) => { /* 用返回值 */ },
     });
     ```
3. 把 `this.$store.dispatch('{storeName}/{action}')` 全部替换为 `this.$callAction({ action: '{storeName}/{action}' })`。
4. 跑 `npm run lint`，确认无 `no-restricted-imports / no-restricted-syntax` 报错。
   - **注意**：`.eslintignore` 中包含 `/src/pages/`，所以页面文件默认不参与 lint。
   - 检查页面文件时显式加 `--no-ignore`：
     ```bash
     ./node_modules/.bin/eslint --no-ignore src/pages/{biz}/{module}/views/xxx.vue
     ```
   - 后续批次若要全量校验页面，需要先把 `/src/pages/` 从 `.eslintignore` 移除，并预先清理预存的 `semi / quotes` 等告警。
5. 跑 `npm run dev` 手测。

---

## 八、规范参考实现

| 用途 | 路径 |
|---|---|
| `$callAction` 定义 | `src/utils/extends.js:72` |
| `createStore` 工厂 | `src/store/createStore.js` |
| `BaseStore.mapGetters` | `src/store/BaseStore.js` |
| `Store03.mixActions` | `src/store/Store03.js` |
| 规范列表页 | `src/pages/b01/m01/views/main.vue` |
| 规范表单页 | `src/pages/b01/m01/views/add.vue` |
| 规范 store.js | `src/pages/b01/m01/store.js` |
| 完整违规清单 | `docs/frontend-refactor-todo.md` |

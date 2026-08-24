# 按钮事件扩展方案

## 目标
1. 按钮支持 beforeAction / afterAction 钩子（扩展 JS 中定义）
2. EXTPARAM 字段真正生效：给通用 action 传额外参数
3. 按钮支持"动作"：打开新增表单、打开编辑表单、弹出选入列表（单选/多选）

## 核心设计：EXTPARAM 结构化

EXTPARAM 改为 JSON，统一配置动作和钩子：

```json
{
  "action": "openForm|openSelector|api",
  "openMode": "add|edit",
  "pageCode": "form",
  "selectMode": "single|multiple",
  "selectPageCode": "selList",
  "selectReturn": "ID",
  "selectTarget": "DTSA",
  "beforeAction": "beforeSubmit",
  "afterAction": "afterSubmit",
  "extraParams": {"STATE": "1"}
}
```

## 实现步骤

### 1. 新建 `generic-selector.vue` 选入弹窗组件

路径: `src/components/generic-module/generic-selector.vue`

- 复用 generic-module 渲染列表（带 checkbox 多选 / 行点击单选）
- Props: `moduleCode`, `pageCode`, `selectMode`('single'/'multiple'), `filterParams`
- 确认按钮 emit `selected` 事件，返回选中行数据
- 单选：点击行直接确认；多选：checkbox 勾选 + 确认按钮

### 2. 修改 `generic-module.vue` 按钮处理

#### 2.1 新增 `parseExtparam(btn)` 方法
- JSON.parse(btn.EXTPARAM)，失败返回 {}

#### 2.2 新增 `callBtnHook(hookName, btn, context)` 方法
- 从 extParam 取 hookName（如 beforeAction）
- 调用 `this[hookName]`（扩展 JS 注入的方法），传 `(btn, context)`
- 返回 false 则中止后续动作

#### 2.3 改造 `handleBtnAction(btn, row)`
```
1. parseExparam -> ext
2. callBtnHook('beforeAction', btn, {row, ext}) -> 返回false中止
3. switch(ext.action):
   - 'openForm': 打开表单弹窗 (ext.openMode='add'用add()，'edit'用clickRow(row))
   - 'openSelector': 打开 generic-selector 弹窗
   - 默认(api或无): 走原有 BTNTYPE 分发，但 param 合并 ext.extraParams
4. callBtnHook('afterAction', btn, {row, ext, result})
```

#### 2.4 模板增加选入弹窗
```html
<rs-modal ref="msel" :title="selTitle" :width="selWidth">
  <generic-selector
    v-if="selConfig"
    :moduleCode="selConfig.moduleCode"
    :pageCode="selConfig.selectPageCode"
    :selectMode="selConfig.selectMode"
    @selected="onSelected"
  ></generic-selector>
</rs-modal>
```

#### 2.5 新增 `onSelected(rows)` 方法
- 处理选入结果：写入子表或调 API

### 3. 修改 `generic-form.vue` 按钮处理

同样增加 `parseExparam` + `callBtnHook`，改造 `handleBtn`：
- beforeAction 钩子
- 合并 extraParams 到 $callAction
- afterAction 钩子

### 4. 修改 `config.vue` 按钮配置 UI

#### 4.1 btnForm 增加字段
- ACTIONTYPE: 'api'|'openForm'|'openSelector'（默认 'api'）

#### 4.2 按钮配置弹窗增加动态表单
- ACTIONTYPE='openForm' 时显示：打开模式(新增/编辑)、目标页面
- ACTIONTYPE='openSelector' 时显示：选择模式(单选/多选)、选入页面、返回字段、选入目标
- 始终显示：beforeAction 方法名、afterAction 方法名、额外参数(JSON)

#### 4.3 保存时把 ACTIONTYPE 和相关配置序列化到 EXTPARAM

### 5. 扩展 JS 默认模板更新

`defaultPageJsTemplate` 增加按钮钩子示例：
```javascript
methods: {
  // 按钮点击前钩子，返回 false 中止
  beforeSubmit(btn, { row, ext }) {
    if (!row) { this.$error('请先选择记录'); return false; }
  },
  // 按钮点击后钩子
  afterSubmit(btn, { row, ext, result }) {
    this.$alert('操作完成');
  }
}
```

## 文件改动清单
1. **新建** `src/components/generic-module/generic-selector.vue` - 选入弹窗组件
2. **修改** `src/components/generic-module/generic-module.vue` - 按钮钩子+动作分发+选入弹窗
3. **修改** `src/components/generic-module/generic-form.vue` - 按钮钩子+extraParams
4. **修改** `src/pages/s01/m18/views/config.vue` - 按钮配置UI增加动作类型
5. **修改** `src/components/generic-module/sfc-editor-popup.vue` - 默认模板增加钩子示例
6. **修改** `src/components/generic-module/index.js` - 注册 generic-selector

## 执行流程示例

**点击"新增"按钮** (ACTIONTYPE=openForm, openMode=add):
```
beforeAction('beforeAdd', btn, {}) -> 通过
-> add() 打开空表单弹窗
-> afterAction('afterAdd', btn, {})
```

**点击"编辑"行按钮** (ACTIONTYPE=openForm, openMode=edit):
```
beforeAction('beforeEdit', btn, {row}) -> 通过
-> clickRow(row) 打开编辑弹窗
-> afterAction('afterEdit', btn, {row})
```

**点击"选入"按钮** (ACTIONTYPE=openSelector, selectMode=multiple):
```
beforeAction('beforeSelect', btn, {}) -> 通过
-> 打开 generic-selector 弹窗
-> 用户选择确认 -> onSelected(rows)
-> 将 rows 写入子表 DTSA
-> afterAction('afterSelect', btn, {rows})
```

**点击"提交"按钮** (ACTIONTYPE=api, extraParams={submitMode:select_checker}):
```
beforeAction('beforeSubmit', btn, {row}) -> 通过
-> $callAction({action, param: {...row, ...extraParams}})
-> afterAction('afterSubmit', btn, {row, result})
```

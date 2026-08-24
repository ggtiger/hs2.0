/**
 * SFC 编辑器模板
 * 来源: generic-module/sfc-editor-popup.vue
 */

export var DEFAULT_PAGE_JS_TEMPLATE = `/**
 * 页面扩展 JS
 *
 * 此文件会作为动态 mixin 合并到 generic-module / generic-form 组件实例中。
 * 可扩展 methods、computed、生命周期钩子 (init/mounted)。
 *
 * 约定路径: @/modules/{moduleCode}/{pageCode}.js
 * 也可在页面配置的"扩展JS"字段中指定自定义路径。
 *
 * 可用的实例属性/方法:
 *   this.$store          - Vuex store
 *   this.$router         - Vue Router
 *   this.$alert(msg)     - 成功提示
 *   this.$error(msg)     - 错误提示
 *   this.$confirm(msg)   - 确认对话框 (返回 Promise)
 *   this.$busy()         - 显示加载状态
 *   this.$free()         - 隐藏加载状态
 *   this.$callAction()   - 调用 Vuex action
 *   this.moduleCode      - 当前模块编码
 *   this.pageConfig      - 当前页面配置对象
 *   this.storeObj        - store 辅助对象 (含 storeHelper/mapDateTable 等)
 *
 * 标准接口 (generic-module 上可用，generic-form 会委托给父级):
 *   this.openPage({ pageCode, mode, id, row, title })
 *     - 打开表单页面: mode='add' 新增 / mode='edit' 编辑(需 id 或 row)
 *   this.openSelector({ pageCode, mode, moduleCode, target, onSelected })
 *     - 打开选入列表: mode='single'/'multiple', onSelected(rows) 回调
 *   this.closePage()
 *     - 关闭当前表单弹窗 (generic-form 中使用)
 *
 * 字段访问 (generic-form 已映射主表字段到 this):
 *   this.ID / this.STATE / this.CREATETIME ... 直接读写 DataTable
 *   this.$MAIN            - 主表 DataTable 对象
 *   this.MAIN             - 主表数据数组
 *   this.$DTSA / this.DTSA - 子表 DataTable / 数据
 */
export default {
  // ========== 方法 ==========
  methods: {
    // 自定义方法，可在模板中直接调用，也可通过 this.xxx() 在其他方法中调用
    doSomething() {
      // 示例: 调用模块的查询 action 刷新列表
      // this.$store.dispatch(this.moduleCode + '/query');
    },

    // 示例: 打开自定义表单页面
    openMyForm(row) {
      this.openPage({
        pageCode: 'myForm',       // 目标表单页面编码
        mode: row ? 'edit' : 'add',
        id: row && row.ID,
        row: row,
        title: '我的表单'
      });
    },

    // 示例: 打开选入列表，选入后写入子表
    openMySelector() {
      this.openSelector({
        pageCode: 'selList',      // 选入列表页面编码
        mode: 'multiple',         // 多选
        target: 'DTSA',           // 选入数据写入 DTSA 子表
        onSelected: function(rows) {
          // rows 是选中的行数据数组
          console.log('选入', rows.length, '条');
        }
      });
    },

    // ========== 按钮钩子 ==========
    // 在按钮配置的"前置钩子"字段填方法名(如 beforeSave)，点击按钮前调用
    // 返回 false 中止按钮动作
    // context: { row, ext, btn }  row=当前行, ext=EXTPARAM配置, btn=按钮配置
    beforeSave(btn, context) {
      // 示例: 提交前校验
      // if (!context.row) { this.$error('请先选择记录'); return false; }
    },

    // 在按钮配置的"后置钩子"字段填方法名(如 afterSave)，动作完成后调用
    // context: { row, ext, btn, result, rows }
    afterSave(btn, context) {
      // 示例: 保存后刷新
      // this.$store.dispatch(this.moduleCode + '/query');
    }
  },

  // ========== 计算属性 ==========
  computed: {
    // 自定义计算属性，可在模板中直接使用
    // myValue() {
    //   return this.$store.state[this.moduleCode].MAIN.data.length;
    // }
  },

  // ========== 生命周期 ==========

  // init: 组件初始化时调用，早于 Vue created 钩子，适合设置初始状态
  init() {
    // this.myInitData = {};
  },

  // mounted: 组件已挂载，可访问 DOM，适合发起初始查询、绑定事件等
  mounted() {
    // console.log('[' + this.moduleCode + '] 页面已挂载', this.pageConfig);
  }
};
`;

export var DEFAULT_STORE_JS_TEMPLATE = `/**
 * 模块 Store 扩展
 *
 * 此文件会自动合并到 {moduleCode} 的 Vuex 模块中。
 * 可扩展 actions 和 mutations，与 Store03 的默认 actions/mutations 合并。
 * 同名的 action/mutation 不会被覆盖（默认 action 优先）。
 *
 * 约定路径: @/modules/{moduleCode}/store.js
 *
 * Store03 默认 actions (可直接通过 dispatch 调用):
 *   query    - 列表查询
 *   open     - 打开单条数据 (含子表)
 *   add      - 新增空行
 *   save     - 保存 (新增/修改)
 *   delete   - 删除
 *   submit   - 提交
 *   check    - 审核
 *   verify   - 审批
 *   batch    - 批量操作
 *   call     - 通用调用 (传 APICODE + params)
 *
 * 可用的工具:
 *   db.postData({api, params}) - 直接调 API
 *   Store03 的 this._this (storeHelper) 可通过 storeResult.storeHelper 获取
 */
export default {
  // ========== Actions ==========
  actions: {
    // 自定义 action，通过 this.$store.dispatch('{moduleCode}/myAction', params) 调用
    async myAction({ commit, dispatch }, params) {
      // 示例: 调用自定义后端接口
      // var ret = await db.postData({
      //   api: '/api/data/call/' + 'MODULE_CODE' + '/A51/',
      //   params: params
      // });
      // commit('MY_MUTATION', ret);
      // return ret;
    }
  },

  // ========== Mutations ==========
  mutations: {
    // 自定义 mutation，通过 commit('MY_MUTATION', payload) 调用
    MY_MUTATION(state, payload) {
      // 直接修改 state
      // state.myField = payload;
    }
  }
};
`;

export var SLOT_BUTTON_TEMPLATE = `<template>
  <div class="slot-buttons">
    <!-- 渲染配置的按钮 -->
    <Button v-for="btn in buttons" :key="btn.ID || btn.BTNCODE"
      class="ml5" v-per="btn.PERMCODE" :icon="btn.ICON" :color="btn.COLOR"
      @click="host.handleBtnAction(btn)">{{btn.BTNNAME}}</Button>
    <!-- 自定义按钮 -->
    <Button class="ml5" color="primary" @click="doCustom">自定义按钮</Button>
  </div>
</template>
<script>
export default {
  props: {
    host: { type: Object, required: true },
    buttons: { type: Array, default: function() { return [] } }
  },
  methods: {
    doCustom() {
      // host 上下文:
      //   host.selectedRows       - 选中的行列表
      //   host.$refs.list.query(1) - 刷新列表
      //   host.$store              - Vuex store
      //   host.$callAction({...})  - 调用 action
      this.host.$alert('自定义按钮点击');
    }
  }
};
<\/script>
`;

export var SLOT_TABLE_ACTION_TEMPLATE = `<template>
  <div class="slot-table-action">
    <!-- 渲染配置的行按钮 -->
    <Button v-for="btn in buttons" :key="btn.ID || btn.BTNCODE"
      size="s" v-per="btn.PERMCODE" :color="btn.COLOR"
      @click="host.handleBtnAction(btn, row)">{{btn.BTNNAME}}</Button>
    <!-- 自定义行操作 -->
    <Button size="s" @click="doView(row)">详情</Button>
  </div>
</template>
<script>
export default {
  props: {
    host: { type: Object, required: true },
    buttons: { type: Array, default: function() { return [] } },
    row: { type: Object, default: function() { return {} } }
  },
  methods: {
    doView(row) {
      // row 是当前行数据
      // host.openPage({ mode: 'edit', id: row.ID }) - 打开编辑页面
      this.host.$alert('查看: ' + row.ID);
    }
  }
};
<\/script>
`;

export var SLOT_QUERY_TEMPLATE = `<template>
  <div class="slot-query" style="display:flex;gap:6px;align-items:center;">
    <!-- 自定义查询条件 -->
    <input v-model="keyword" placeholder="关键词搜索" @keyup.enter="doSearch" />
    <Button size="s" @click="doSearch">搜索</Button>
  </div>
</template>
<script>
export default {
  props: {
    host: { type: Object, required: true }
  },
  data() {
    return { keyword: '' };
  },
  methods: {
    doSearch() {
      // 通过 host 访问查询条件并触发查询:
      //   host.storeObj.storeHelper.getTable('QQRY') - 查询条件 DataTable
      //   host.$refs.list.query(1) - 触发列表查询
      this.host.$refs.list.query(1);
    }
  }
};
<\/script>
`;

export var SLOT_FORM_AREA_TEMPLATE = `<template>
  <div class="slot-form-area" style="padding:8px 0;">
    <!-- 表单上方/下方自定义内容 -->
    <div v-if="host.ID" style="color:#999;font-size:12px;">
      当前编辑: {{host.ID}}
    </div>
  </div>
</template>
<script>
export default {
  props: {
    host: { type: Object, required: true }
  },
  mounted() {
    // host 上下文:
    //   host.ID / host.STATE / host.CREATETIME - 主表字段
    //   host.$MAIN - 主表 DataTable
    //   host.save() - 保存
    //   host.closePage() - 关闭表单
  }
};
<\/script>
`;

export var SLOT_FIELD_TEMPLATE = `<template>
  <div class="slot-field">
    <!-- 自定义字段控件，通过 value 接收值，input 事件回传 -->
    <input :value="value" @input="$emit('input', $event.target.value)"
      placeholder="自定义字段控件" class="slot-field-input" />
  </div>
</template>
<script>
export default {
  props: {
    host: { type: Object, required: true },
    value: { default: '' }
  }
};
<\/script>
`;

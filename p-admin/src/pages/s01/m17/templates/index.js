/**
 * SFC 在线开发 — 模板代码生成器
 *
 * 把项目标准模块（list-t01 + Add01 mixin + createStore + mapDateTable）的样板代码
 * 固化成生成函数, 在线编辑器"插入模板"时调用, 开发者只需改 moduleCode/列定义/字段
 *
 * 四套模板覆盖常见业务形态:
 *   1. 单表 CRUD      — genSimpleCrud  (对齐 s01/m16)
 *   2. 主子表 CRUD    — genMasterDetail (对齐 b01/m01)
 *   3. 审批流单据     — genBillFlow    (对齐 r01/m02)
 *   4. 纯展示/自定义页 — genCustomPage  (不走 Store03, 直接调 db)
 *
 * 每套模板提供 genXxxMain / genXxxAdd / genXxxStore 三个生成函数,
 * 返回标准 SFC 源码字符串。在线编辑器单文件编辑模型下, 用户分三次新建三个文件,
 * 每次选对应子项生成。
 *
 * 注意: 在线 SFC 不能用 SelStore (依赖 RS_M00 已加载, chunk 执行时机不确定),
 * 所以模板里不引入 SelStore/Sel01, 也不生成 empParam/deptParam 等选择器配置。
 * 需要下拉选择器的字段, 由 rs-form-edit 通过 scm SELECTDATA 自动渲染。
 */

// ====== 工具函数 ======

/**
 * moduleCode → 业务码/模块码 推导
 * RS_M16 → { biz: 's01', mod: 'm16' }
 * LI_M02 → { biz: 'r01', mod: 'm02' }
 * LIB_M01 → { biz: 'b01', mod: 'm01' }
 * 未知前缀默认 biz='s01'
 */
var PREFIX_MAP = {
  RS: 's01', // 系统管理
  LI: 'r01', // 记录/报告
  LIB: 'b01', // 基础数据
  TBS: 'b01',
  CG: 'cgdd', // 采购
};

export function parseModuleCode(moduleCode) {
  if (!moduleCode) return { biz: 's01', mod: 'm00' };
  var parts = String(moduleCode).split('_');
  var prefix = parts[0] ? parts[0].toUpperCase() : '';
  var biz = PREFIX_MAP[prefix] || 's01';
  var mod = parts.slice(1).join('_').toLowerCase() || 'm00';
  // 标准化为 mXX 格式 (m01, m02 ...)
  if (/^m\d+$/.test(mod)) {
    // 已是 m01 格式, 保持
  } else if (/^\d+$/.test(mod)) {
    mod = 'm' + mod;
  }
  return { biz: biz, mod: mod };
}

/**
 * 推导三个文件的 modulePath
 * RS_M16 → main: @/pages/s01/m16/views/main.vue
 */
export function derivePaths(moduleCode) {
  var info = parseModuleCode(moduleCode);
  var dir = '@/pages/' + info.biz + '/' + info.mod;
  return {
    main: dir + '/views/main.vue',
    add: dir + '/views/add.vue',
    store: dir + '/store.js',
    dir: dir,
  };
}

/**
 * storeName 默认值: RS_M16 → s01/m16
 */
export function deriveStoreName(moduleCode) {
  var info = parseModuleCode(moduleCode);
  return info.biz + '/' + info.mod;
}

/**
 * 组件 name: s01/m16 → s01-m16-main
 */
function dashedName(storeName, suffix) {
  return storeName.replace(/\//g, '-') + '-' + suffix;
}

// ====== 1. 单表 CRUD ======

export function genSimpleCrudStore(opts) {
  var o = opts || {};
  var moduleCode = o.moduleCode || 'RS_MXX';
  var storeName = o.storeName || deriveStoreName(moduleCode);
  return `import createStore from "@/store/createStore";
let { mapState, mapGetters, mapDateTable, Constants } = createStore.getStore({
  config: { moduleCode: '${moduleCode}' },
  storeName: '${storeName}',
  mutations: {},
  actions: {
    add({ commit }) {
      commit('INIT', { paths: ['MAIN'] });
      commit('ADD', { path: 'MAIN', item: {} });
    },
  }
});
export { mapState, mapGetters, mapDateTable, Constants };
`;
}

export function genSimpleCrudMain(opts) {
  var o = opts || {};
  var moduleCode = o.moduleCode || 'RS_MXX';
  var storeName = o.storeName || deriveStoreName(moduleCode);
  var title = o.title || '业务管理';
  var name = dashedName(storeName, 'main');
  return `<template>
  <list-t01
    title="${title}"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    addper="${moduleCode}/A04"
    expper="${moduleCode}/A09"
  >
    <!-- 列定义: prop 对应 scm 字段名, 按需增删 -->
    <TableItem title="编码" prop="CODE" :width="200"/>
    <TableItem title="名称" prop="NAME"/>
    <TableItem title="更新时间" prop="UPDATETIME" :width="160"/>

    <!-- 新增/编辑弹窗 -->
    <rs-modal ref="madd" :width="800">
      <rsAdd :storeName="store.Constants.STORE_NAME" title="${title}" :ID="CDID"></rsAdd>
    </rs-modal>

    <!-- 头部操作按钮 (可选, 不写则用 list-t01 默认的新增按钮) -->
    <template slot="header-action">
      <Button color="primary" v-per="'${moduleCode}/A04'" icon="h-icon-plus" @click="add">添加</Button>
    </template>
  </list-t01>
</template>
<script>
import rsAdd from './add.vue';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: '${name}',
  components: { rsAdd },
  data() {
    return {
      CDID: '',
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [{ title: '业务管理' }, { title: '${title}' }],
    };
  },
  methods: {
    add() {
      this.CDID = '';
      this.$refs.madd.show();
    },
    clickRow(row) {
      this.CDID = row.ID;
      this.$refs.madd.show();
    },
    listAction(action, param) {
      switch (action) {
        case 'add': this.add(param); break;
        default: break;
      }
    },
  },
};
</script>
`;
}

export function genSimpleCrudAdd(opts) {
  var o = opts || {};
  var moduleCode = o.moduleCode || 'RS_MXX';
  var storeName = o.storeName || deriveStoreName(moduleCode);
  var name = dashedName(storeName, 'add');
  return `<template>
  <view-dialog :title="title">
    <template slot="body">
      <ToolBar label="基本信息" :size="16"></ToolBar>
      <!-- rs-form-edit 根据 scm 自动渲染字段; mode: single/twocolumn -->
      <rs-form-edit
        ref="form"
        class="maxModalH rs-flex-col"
        :label-width="100"
        mode="single"
        :path="$MAIN"
      ></rs-form-edit>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'${moduleCode}/A07'" v-if="ID" @confirm="del">
        <Button class="ml5" color="red">删除</Button>
      </Poptip>
      <Button class="ml5" v-per="'${moduleCode}/A04'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Add01 from '@/mixins/add01';
export default {
  name: '${name}',
  mixins: [Add01],
  computed: {
    ...mapDateTable('MAIN', []),
  },
};
</script>
`;
}

// ====== 2. 主子表 CRUD ======

export function genMasterDetailStore(opts) {
  var o = opts || {};
  var moduleCode = o.moduleCode || 'RS_MXX';
  var storeName = o.storeName || deriveStoreName(moduleCode);
  var dtsName = o.dtsName || 'DTS';
  return `import createStore from "@/store/createStore";
let { mapState, mapGetters, mapDateTable, Constants } = createStore.getStore({
  config: { moduleCode: '${moduleCode}' },
  storeName: '${storeName}',
  mutations: {},
  actions: {
    add({ commit }) {
      commit('INIT', { paths: ['MAIN', '${dtsName}'] });
      commit('ADD', { path: 'MAIN', item: {} });
    },
  }
});
export { mapState, mapGetters, mapDateTable, Constants };
`;
}

export function genMasterDetailMain(opts) {
  // 主子表的列表页与单表一致, 复用单表 main (只是 add.vue 内部不同)
  return genSimpleCrudMain(opts);
}

export function genMasterDetailAdd(opts) {
  var o = opts || {};
  var moduleCode = o.moduleCode || 'RS_MXX';
  var storeName = o.storeName || deriveStoreName(moduleCode);
  var dtsName = o.dtsName || 'DTS';
  var name = dashedName(storeName, 'add');
  var dtsUpper = dtsName.toUpperCase();
  return `<template>
  <view-dialog :title="title" class="d-width">
    <template slot="body">
      <ToolBar label="基本信息" :size="16"></ToolBar>
      <rs-form-edit
        ref="form"
        class="maxModalH rs-flex-col"
        :label-width="80"
        mode="twocolumn"
        :path="$MAIN"
      >
        <!-- 子表区域: slot 名等于子表字段名 (在 scm 里把该字段配成子表类型) -->
        <template slot="${dtsName}">
          <div class="rr-flex-1">
            <ToolBar label="明细" :size="16">
              <div slot="right">
                <Button color="primary" icon="h-icon-plus" size="s" @click="addDts('${dtsUpper}')">选入</Button>
                <Button color="primary" icon="h-icon-minus" size="s" @click="removeDts('${dtsUpper}', $refs.${dtsUpper})">移除</Button>
              </div>
            </ToolBar>
            <rs-table-edit border ref="${dtsUpper}" :path="$${dtsUpper}" :datas="${dtsUpper}"></rs-table-edit>
          </div>
        </template>
      </rs-form-edit>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'${moduleCode}/A07'" v-if="ID" @confirm="del">
        <Button class="ml5" color="red">删除</Button>
      </Poptip>
      <Button class="ml5" v-per="'${moduleCode}/A04'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Add01 from '@/mixins/add01';
export default {
  name: '${name}',
  mixins: [Add01],
  computed: {
    ...mapDateTable('MAIN', []),
    ...mapDateTable('${dtsUpper}', []),
  },
  methods: {
    addDts(path) {
      this.$store.commit(\`\${Constants.STORE_NAME}/ADD\`, { path });
    },
    removeDts(path, table) {
      if (table.currentRow === -1) return;
      this.$store.commit(\`\${Constants.STORE_NAME}/DEL\`, { path, item: table.currentRow });
    },
  },
};
</script>
`;
}

// ====== 3. 审批流单据 ======

export function genBillFlowStore(opts) {
  var o = opts || {};
  var moduleCode = o.moduleCode || 'LI_MXX';
  var storeName = o.storeName || deriveStoreName(moduleCode);
  return `import createStore from "@/store/createStore";
let { mapState, mapGetters, mapDateTable, Constants } = createStore.getStore({
  config: { moduleCode: '${moduleCode}' },
  storeName: '${storeName}',
  mutations: {},
  actions: {
    add({ commit }) {
      commit('INIT', { paths: ['MAIN'] });
      commit('ADD', { path: 'MAIN', item: {} });
    },
  }
});
export { mapState, mapGetters, mapDateTable, Constants };
`;
}

export function genBillFlowMain(opts) {
  var o = opts || {};
  var moduleCode = o.moduleCode || 'LI_MXX';
  var storeName = o.storeName || deriveStoreName(moduleCode);
  var title = o.title || '业务单据';
  var name = dashedName(storeName, 'main');
  return `<template>
  <list-t01
    title="${title}"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    addper="${moduleCode}/A04"
    expper="${moduleCode}/A09"
  >
    <TableItem title="单据号" prop="BILLCODE" :width="200"/>
    <TableItem title="状态" prop="STATE" :width="100"/>
    <TableItem title="创建时间" prop="CREATEDTIME" :width="160"/>

    <rs-modal ref="madd" :width="900">
      <rsAdd :storeName="store.Constants.STORE_NAME" title="${title}" :ID="CDID"></rsAdd>
    </rs-modal>

    <!-- 批量操作按钮 (可选) -->
    <template slot="header-action">
      <Button color="primary" v-per="'${moduleCode}/A04'" icon="h-icon-plus" @click="add">新增</Button>
    </template>
  </list-t01>
</template>
<script>
import rsAdd from './add.vue';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: '${name}',
  components: { rsAdd },
  data() {
    return {
      CDID: '',
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [{ title: '业务管理' }, { title: '${title}' }],
    };
  },
  methods: {
    add() {
      this.CDID = '';
      this.$refs.madd.show();
    },
    clickRow(row) {
      this.CDID = row.ID;
      this.$refs.madd.show();
    },
    listAction(action, param) {
      switch (action) {
        case 'add': this.add(param); break;
        default: break;
      }
    },
  },
};
</script>
`;
}

export function genBillFlowAdd(opts) {
  var o = opts || {};
  var moduleCode = o.moduleCode || 'LI_MXX';
  var storeName = o.storeName || deriveStoreName(moduleCode);
  var name = dashedName(storeName, 'add');
  // 审批流 footer: 按 STATE 显示 暂存/删除/提交/撤销提交/审核撤销/审批撤销
  // Add01 mixin 提供 save/del/submit/reSubmit/check/reCheck/verify/reVerify/reject
  // ISSHOW* 计算属性也由 Add01 提供
  return `<template>
  <view-dialog :title="title" class="d-width">
    <template slot="body">
      <ToolBar label="基本信息" :size="16"></ToolBar>
      <rs-form-edit
        ref="form"
        class="maxModalH rs-flex-col"
        :label-width="100"
        mode="twocolumn"
        :path="$MAIN"
      ></rs-form-edit>
    </template>
    <template slot="footer">
      <!-- 暂存 (待提交状态) -->
      <Button class="ml5" v-per="'${moduleCode}/A04'" v-if="ISSHOWSAVE" color="primary" @click.native="save">暂存</Button>
      <Poptip content="确定删除？" v-per="'${moduleCode}/A07'" v-if="ISSHOWDELETE" @confirm="del">
        <Button class="ml5" color="red">删除</Button>
      </Poptip>
      <!-- 提交 (待提交 → 待审核) -->
      <Poptip content="确定提交？" v-per="'${moduleCode}/A17'" v-if="ISSHOWSUBMIT" @confirm="submit(ID)">
        <Button class="ml5" color="primary">提交</Button>
      </Poptip>
      <!-- 撤销提交 (待审核 → 待提交) -->
      <Poptip content="确定撤销提交？" v-per="'${moduleCode}/A18'" v-if="ISSHOWRESUBMIT" @confirm="reSubmit(ID)">
        <Button class="ml5" color="red" icon="h-icon-close">撤销提交</Button>
      </Poptip>
      <!-- 审核 (待审核 → 待审批) -->
      <Poptip content="确定审核通过？" v-per="'${moduleCode}/A12'" v-if="ISSHOWCHECK" @confirm="check(ID)">
        <Button class="ml5" color="primary">审核</Button>
      </Poptip>
      <!-- 撤销审核 -->
      <Poptip content="确定撤销审核？" v-per="'${moduleCode}/A13'" v-if="ISSHOWRECHECK" @confirm="reCheck(ID)">
        <Button class="ml5" color="red" icon="h-icon-close">撤销审核</Button>
      </Poptip>
      <!-- 审批 (待审批 → 已审批) -->
      <Poptip content="确定审批通过？" v-per="'${moduleCode}/A14'" v-if="ISSHOWVERIFY" @confirm="verify(ID)">
        <Button class="ml5" color="primary">审批</Button>
      </Poptip>
      <!-- 撤销审批 -->
      <Poptip content="确定撤销审批？" v-per="'${moduleCode}/A15'" v-if="ISSHOWREVERIFY" @confirm="reVerify(ID)">
        <Button class="ml5" color="red" icon="h-icon-close">撤销审批</Button>
      </Poptip>
      <Button class="ml5" @click.native="closeW">取消</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Add01 from '@/mixins/add01';
export default {
  name: '${name}',
  mixins: [Add01],
  computed: {
    ...mapDateTable('MAIN', []),
  },
  data() {
    return {
      REMARK: '',
    };
  },
};
</script>
`;
}

// ====== 4. 纯展示/自定义页面 ======

export function genCustomPageMain(opts) {
  var o = opts || {};
  var title = o.title || '自定义页面';
  var moduleCode = o.moduleCode || 'RS_MXX';
  var name = dashedName(o.storeName || deriveStoreName(moduleCode), 'main');
  // 不走 Store03, 直接用 db.postData 调接口, 用 h-panel 撑满布局
  return `<template>
  <div class="custom-page">
    <h-panel>
      <h-panel-header :style="{ padding: '10px 16px' }">
        <Breadcrumb :datas="datas"></Breadcrumb>
      </h-panel-header>
      <h-panel-body>
        <rsTableList
          ref="table"
          :datas="list"
          :loading="loading"
          @list-click-row="clickRow"
        >
          <TableItem title="编码" prop="CODE" :width="200"/>
          <TableItem title="名称" prop="NAME"/>
        </rsTableList>
      </h-panel-body>
      <h-panel-footer>
        <Pagination :cur="page.pageIndex" :total="page.totalCount" :size="page.pageSize" @change="onPage" align="right"/>
      </h-panel-footer>
    </h-panel>
  </div>
</template>
<script>
import db from '@/api/db';

export default {
  name: '${name}',
  data() {
    return {
      datas: [{ title: '业务管理' }, { title: '${title}' }],
      list: [],
      loading: false,
      page: { pageIndex: 1, pageSize: 20, totalCount: 0 },
      query: {},
    };
  },
  mounted() {
    this.loadList();
  },
  methods: {
    async loadList() {
      this.loading = true;
      try {
        var ret = await db.postData({
          api: '/api/data/call/${moduleCode}/A01/',
          params: {
            FilterParams: this.query,
            PageSize: this.page.pageSize,
            PageIndex: this.page.pageIndex,
          },
        });
        this.list = (ret && ret.Items) || [];
        this.page.totalCount = (ret && ret.TotalCount) || 0;
      } catch (e) {
        this.$error('加载失败: ' + (e.message || e));
      } finally {
        this.loading = false;
      }
    },
    onPage({ page, size }) {
      this.page.pageIndex = page;
      this.page.pageSize = size;
      this.loadList();
    },
    clickRow(row) {
      this.$alert('点击行: ' + (row.CODE || row.ID));
    },
  },
};
</script>
<style scoped>
.custom-page {
  height: 100%;
}
</style>
`;
}

// ====== 模板元数据 (供编辑器 UI 枚举) ======

export var TEMPLATES = [
  {
    key: 'simple',
    name: '单表 CRUD',
    desc: '列表 + 弹窗新增/编辑/删除 (对齐 s01/m16)',
    files: [
      { key: 'main', label: '生成 main.vue (列表页)', gen: genSimpleCrudMain, fileType: 'VUE' },
      { key: 'add', label: '生成 add.vue (编辑弹窗)', gen: genSimpleCrudAdd, fileType: 'VUE' },
      { key: 'store', label: '生成 store.js', gen: genSimpleCrudStore, fileType: 'JS' },
    ],
  },
  {
    key: 'master-detail',
    name: '主子表 CRUD',
    desc: '主表 + 明细子表 (对齐 b01/m01)',
    files: [
      { key: 'main', label: '生成 main.vue (列表页)', gen: genMasterDetailMain, fileType: 'VUE' },
      { key: 'add', label: '生成 add.vue (主+子表弹窗)', gen: genMasterDetailAdd, fileType: 'VUE' },
      { key: 'store', label: '生成 store.js', gen: genMasterDetailStore, fileType: 'JS' },
    ],
  },
  {
    key: 'bill-flow',
    name: '审批流单据',
    desc: '带提交/审核/审批状态流转 (对齐 r01/m02)',
    files: [
      { key: 'main', label: '生成 main.vue (列表页)', gen: genBillFlowMain, fileType: 'VUE' },
      { key: 'add', label: '生成 add.vue (审批流弹窗)', gen: genBillFlowAdd, fileType: 'VUE' },
      { key: 'store', label: '生成 store.js', gen: genBillFlowStore, fileType: 'JS' },
    ],
  },
  {
    key: 'custom',
    name: '纯展示/自定义页面',
    desc: '不走 Store03, 直接调接口 (单文件)',
    files: [
      { key: 'main', label: '生成 main.vue', gen: genCustomPageMain, fileType: 'VUE' },
    ],
  },
];

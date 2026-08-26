/**
 * 生成 SFC 测试数据 SQL 并执行
 * 用法: node sql/gen-sfc-test-data.js | docker exec -i labone-mysql mysql -ulabone -plabone123 D0001
 */
const fs = require('fs');

// SQL 字符串转义
function esc(str) {
  if (str == null) return 'NULL';
  return "'" + String(str)
    .replace(/\\/g, '\\\\')
    .replace(/'/g, "''")
    .replace(/\r/g, '\\r')
    .replace(/\n/g, '\\n')
    .replace(/\t/g, '\\t') + "'";
}

// 文件 1: main.vue 列表页
const mainVue = `<template>
  <div class="page-wrap">
    <div class="breadcrumb">
      <span v-for="(item, idx) in bcDatas" :key="idx">{{ item.title }}<i v-if="idx < bcDatas.length - 1"> / </i></span>
    </div>
    <div class="list-header">
      <h2 class="title">{{ pageTitle }}</h2>
      <div class="header-btns">
        <button class="btn btn-primary" @click="handleAdd">+ 新增</button>
        <button class="btn" @click="handleRefresh">刷新</button>
      </div>
    </div>
    <div class="table-wrap">
      <table class="data-table">
        <thead>
          <tr>
            <th v-for="col in columns" :key="col.prop" :style="{ width: col.width + 'px' }">{{ col.title }}</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="(row, idx) in tableData"
            :key="row.ID"
            :class="{ 'is-selected': selectedId === row.ID }"
            @click="clickRow(row)"
          >
            <td>{{ row.PROMPTKEY }}</td>
            <td>{{ row.DESCRIPTION }}</td>
            <td>{{ row.UPDATETIME }}</td>
            <td class="col-action">
              <span class="link" @click.stop="handleEdit(row)">编辑</span>
              <span class="link danger" @click.stop="handleDelete(row, idx)">删除</span>
            </td>
          </tr>
          <tr v-if="tableData.length === 0">
            <td colspan="4" class="empty">暂无数据</td>
          </tr>
        </tbody>
      </table>
    </div>
    <div class="footer-info">共 {{ tableData.length }} 条记录</div>
    <rs-modal ref="madd" :width="600">
      <add-form :id="currentId" @saved="onSaved"></add-form>
    </rs-modal>
  </div>
</template>
<script>
import addForm from "./add.vue";
import { formatTime } from "./utils.js";
export default {
  name: "s01-m16-main-mock",
  components: { addForm },
  data() {
    return {
      pageTitle: "提示词管理",
      selectedId: "",
      currentId: "",
      bcDatas: [{ title: "系统管理" }, { title: "提示词管理" }],
      columns: [
        { title: "提示词键", prop: "PROMPTKEY", width: 200 },
        { title: "说明", prop: "DESCRIPTION" },
        { title: "更新时间", prop: "UPDATETIME", width: 160 },
        { title: "操作", prop: "__action", width: 140 },
      ],
      tableData: [
        { ID: "1", PROMPTKEY: "ai_translate", DESCRIPTION: "AI 翻译提示词", UPDATETIME: formatTime("2026-07-01 10:23") },
        { ID: "2", PROMPTKEY: "ai_summary", DESCRIPTION: "AI 摘要提示词", UPDATETIME: formatTime("2026-07-02 14:05") },
        { ID: "3", PROMPTKEY: "ai_classify", DESCRIPTION: "AI 分类提示词", UPDATETIME: formatTime("2026-07-03 09:11") },
        { ID: "4", PROMPTKEY: "ai_extract", DESCRIPTION: "AI 信息抽取提示词", UPDATETIME: formatTime("2026-07-03 16:48") },
        { ID: "5", PROMPTKEY: "ai_check", DESCRIPTION: "AI 校对提示词", UPDATETIME: formatTime("2026-07-04 08:30") },
      ],
    };
  },
  methods: {
    handleAdd() {
      this.currentId = "";
      this.$refs.madd.show();
    },
    handleRefresh() {
      this.$alert && this.$alert("刷新成功 (mock)");
    },
    clickRow(row) {
      this.selectedId = row.ID;
      this.currentId = row.ID;
      this.$refs.madd.show();
    },
    handleEdit(row) {
      this.currentId = row.ID;
      this.$refs.madd.show();
    },
    handleDelete(row, idx) {
      this.tableData.splice(idx, 1);
      this.$alert && this.$alert("已删除: " + row.PROMPTKEY);
    },
    onSaved() {
      this.$refs.madd.close && this.$refs.madd.close();
      this.$alert && this.$alert("保存成功 (mock)");
    },
  },
};
</script>
<style lang="less" scoped>
.page-wrap { padding: 16px; background: #fff; font-size: 13px; color: #333; min-height: 100%; }
.breadcrumb { margin-bottom: 12px; color: #999; i { margin: 0 4px; } }
.list-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px;
  .title { font-size: 16px; margin: 0; } }
.header-btns { .btn { padding: 4px 12px; border: 1px solid #ddd; background: #fff; border-radius: 4px; cursor: pointer; margin-left: 8px; font-size: 12px;
    &:hover { border-color: #0a84ff; color: #0a84ff; }
    &.btn-primary { background: #0a84ff; color: #fff; border-color: #0a84ff; } } }
.table-wrap { border: 1px solid #e8eaec; border-radius: 4px; overflow: hidden; }
.data-table { width: 100%; border-collapse: collapse;
  thead th { background: #f8f8f9; padding: 8px 12px; text-align: left; font-weight: 600; border-bottom: 1px solid #e8eaec; }
  tbody td { padding: 8px 12px; border-bottom: 1px solid #f0f0f0; }
  tbody tr { cursor: pointer; &:hover { background: #f5f7fa; } &.is-selected { background: #ebf5ff; } }
  .col-action .link { color: #0a84ff; cursor: pointer; margin-right: 12px; &:hover { text-decoration: underline; } &.danger { color: #ed4014; } }
  .empty { text-align: center; color: #999; padding: 24px; } }
.footer-info { margin-top: 8px; color: #999; font-size: 12px; }
</style>`;

// 文件 2: add.vue 编辑页
const addVue = `<template>
  <div class="add-wrap">
    <h3 class="form-title">{{ isEdit ? "编辑提示词" : "新增提示词" }}</h3>
    <div class="form-row">
      <label class="lbl">提示词键</label>
      <input class="inp" v-model="form.PROMPTKEY" placeholder="如 ai_translate" />
    </div>
    <div class="form-row">
      <label class="lbl">说明</label>
      <textarea class="inp textarea" v-model="form.DESCRIPTION" placeholder="提示词用途说明"></textarea>
    </div>
    <div class="form-row">
      <label class="lbl">内容</label>
      <textarea class="inp textarea lg" v-model="form.CONTENT" placeholder="提示词内容"></textarea>
    </div>
    <div class="form-footer">
      <button class="btn btn-primary" @click="handleSave">保存</button>
      <button class="btn" @click="handleCancel">取消</button>
    </div>
  </div>
</template>
<script>
import { genId, formatTime } from "./utils.js";
export default {
  name: "s01-m16-add-mock",
  props: { id: { type: String, default: "" } },
  data() {
    return {
      form: { PROMPTKEY: "", DESCRIPTION: "", CONTENT: "" },
    };
  },
  computed: {
    isEdit() { return !!this.id; },
  },
  watch: {
    id: {
      immediate: true,
      handler(v) {
        if (v) {
          this.form = { PROMPTKEY: "ai_" + v, DESCRIPTION: "编辑模式加载", CONTENT: "提示词内容示例" };
        } else {
          this.form = { PROMPTKEY: "", DESCRIPTION: "", CONTENT: "" };
        }
      },
    },
  },
  methods: {
    handleSave() {
      if (!this.form.PROMPTKEY) {
        this.$alert && this.$alert("请填写提示词键");
        return;
      }
      var payload = Object.assign({}, this.form, {
        ID: this.id || genId(),
        UPDATETIME: formatTime(new Date()),
      });
      this.$emit("saved", payload);
    },
    handleCancel() {
      this.$emit("saved", null);
    },
  },
};
</script>
<style lang="less" scoped>
.add-wrap { padding: 16px; background: #fff; }
.form-title { font-size: 15px; margin: 0 0 16px; }
.form-row { display: flex; align-items: flex-start; margin-bottom: 12px;
  .lbl { width: 80px; text-align: right; padding-right: 12px; line-height: 32px; color: #666; }
  .inp { flex: 1; height: 32px; border: 1px solid #dddee1; border-radius: 4px; padding: 0 8px; font-size: 13px; outline: none;
    &:focus { border-color: #0a84ff; }
    &.textarea { height: 80px; padding: 6px 8px; resize: vertical; line-height: 1.5; }
    &.lg { height: 160px; } } }
.form-footer { text-align: right; padding-top: 12px;
  .btn { padding: 4px 16px; border: 1px solid #ddd; background: #fff; border-radius: 4px; cursor: pointer; margin-left: 8px; font-size: 13px;
    &:hover { border-color: #0a84ff; color: #0a84ff; }
    &.btn-primary { background: #0a84ff; color: #fff; border-color: #0a84ff; } } }
</style>`;

// 文件 3: utils.js 工具函数 (纯 JS 模块, 被 main.vue/add.vue import)
const utilsJs = `/**
 * SFC 测试模块 - 工具函数
 */

export function formatTime(d) {
  if (!d) return "";
  var dt = typeof d === "string" ? new Date(d) : d;
  if (isNaN(dt.getTime())) return String(d);
  var pad = function(n) { return n < 10 ? "0" + n : "" + n; };
  return dt.getFullYear() + "-" + pad(dt.getMonth() + 1) + "-" + pad(dt.getDate()) +
    " " + pad(dt.getHours()) + ":" + pad(dt.getMinutes());
}

export function genId() {
  return "id_" + Date.now() + "_" + Math.floor(Math.random() * 10000);
}

export function deepClone(obj) {
  if (obj === null || typeof obj !== "object") return obj;
  return JSON.parse(JSON.stringify(obj));
}

export default {
  formatTime: formatTime,
  genId: genId,
  deepClone: deepClone,
};
`;

// 文件 4: store.js Store 模块 (import 桥梁模块 @/api/db, @/store/createStore)
const storeJs = `/**
 * SFC 测试模块 - Store (mock 版本, 不真正调用后端)
 * 真实场景应 import db from "@/api/db" 调用 ORM API
 */
import db from "@/api/db";

const STORE_NAME = "s01/m16";
const MODULE_CODE = "RS_M16";

var _cache = null;

function getStore() {
  if (_cache) return _cache;
  _cache = {
    STORE_NAME: STORE_NAME,
    MODULE_CODE: MODULE_CODE,
    query: function() {
      return Promise.resolve([
        { ID: "1", PROMPTKEY: "ai_translate", DESCRIPTION: "AI 翻译提示词" },
        { ID: "2", PROMPTKEY: "ai_summary", DESCRIPTION: "AI 摘要提示词" },
      ]);
    },
    save: function(data) {
      console.log("[mock store] save", data);
      return Promise.resolve({ success: true });
    },
  };
  return _cache;
}

export default getStore();
export { STORE_NAME, MODULE_CODE, getStore };
`;

// 文件 5: 纯 JS 模块 (无 SFC, 测试 JS 文件类型)
const configJs = `/**
 * SFC 测试模块 - 配置常量
 */
export const APP_NAME = "睿谱希管理系统";
export const VERSION = "2.0.0";

export const STATUS_MAP = {
  1: "待提交",
  2: "待审核",
  5: "待审批",
  6: "已审批",
  10: "已签发",
  12: "已驳回",
};

export function getStatusLabel(code) {
  return STATUS_MAP[code] || "未知";
}

export default { APP_NAME, VERSION, STATUS_MAP, getStatusLabel };
`;

// 组装 SQL
const files = [
  {
    code: "SFC_TEST_MAIN",
    name: "提示词管理-列表页",
    path: "@/pages/s01/m16/views/main.vue",
    type: "VUE",
    source: mainVue,
    desc: "SFC 测试 - 列表页 (参考 s01/m16)",
  },
  {
    code: "SFC_TEST_ADD",
    name: "提示词管理-编辑页",
    path: "@/pages/s01/m16/views/add.vue",
    type: "VUE",
    source: addVue,
    desc: "SFC 测试 - 编辑表单页",
  },
  {
    code: "SFC_TEST_UTILS",
    name: "提示词管理-工具函数",
    path: "@/pages/s01/m16/utils.js",
    type: "JS",
    source: utilsJs,
    desc: "SFC 测试 - JS 工具模块 (被 main/add import)",
  },
  {
    code: "SFC_TEST_STORE",
    name: "提示词管理-Store",
    path: "@/pages/s01/m16/store.js",
    type: "JS",
    source: storeJs,
    desc: "SFC 测试 - Store 模块 (import @/api/db 桥梁)",
  },
  {
    code: "SFC_TEST_CONFIG",
    name: "系统配置常量",
    path: "@/pages/s01/m16/config.js",
    type: "JS",
    source: configJs,
    desc: "SFC 测试 - 纯 JS 配置模块",
  },
];

let sql = "-- SFC 测试数据 (自动生成)\n";
sql += "-- 清除旧测试数据\n";
sql += "DELETE FROM tbs_sfc_template WHERE MODULEPATH LIKE '@/pages/s01/m16/%';\n\n";

files.forEach(function(f, i) {
  sql += "-- " + (i + 1) + ". " + f.name + " (" + f.path + ")\n";
  sql += "INSERT INTO tbs_sfc_template (\n";
  sql += "  ID, TEMPLATECODE, TEMPLATENAME, MODULEPATH, FILETYPE,\n";
  sql += "  SOURCECODE, COMPILEDCODE, DEPS, DESCRIPTION,\n";
  sql += "  ISDELETED, CREATEDBY, CREATEDTIME\n";
  sql += ") VALUES (\n";
  sql += "  REPLACE(UUID(), '-', ''),\n";
  sql += "  " + esc(f.code) + ",\n";
  sql += "  " + esc(f.name) + ",\n";
  sql += "  " + esc(f.path) + ",\n";
  sql += "  " + esc(f.type) + ",\n";
  sql += "  " + esc(f.source) + ",\n";
  sql += "  NULL,\n";
  sql += "  '[]',\n";
  sql += "  " + esc(f.desc) + ",\n";
  sql += "  0,\n";
  sql += "  'system',\n";
  sql += "  NOW()\n";
  sql += ");\n\n";
});

sql += "-- 查询验证\n";
sql += "SELECT TEMPLATECODE, TEMPLATENAME, MODULEPATH, FILETYPE FROM tbs_sfc_template WHERE MODULEPATH LIKE '@/pages/s01/m16/%';\n";

process.stdout.write(sql);

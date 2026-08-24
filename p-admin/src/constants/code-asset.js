/**
 * 代码资产元配置 + 默认模板
 * 来源: s01/m17/code-asset.js（仅常量部分，函数保留在原文件）
 */

// 三类资产的元配置（数据源/字段映射/默认值）
// 统一视图 VSS_CODE_ASSET 字段: ID/ASSETTYPE/CODE/NAME/MODULEPATH/FILETYPE/SOURCECODE/
//   COMPILEDCODE/DEPS/SQLTYPE/VERSION/REMARK/CREATEID/CREATER/MODIFYID/MODIFER/ISDELETED/CREATETIME/MODIFYTIME
export var ASSET_META = {
  csharp: {
    fileType: 'CSHARP',
    kindBadge: 'API 脚本',
    idField: 'ID',
    codeField: 'CODE',
    nameField: 'NAME',
    sourceField: 'SOURCECODE',
    remarkField: 'REMARK',
    codePrefix: 'SC_',
  },
  sql: {
    fileType: 'SQL',
    kindBadge: 'SQL 模板',
    idField: 'ID',
    codeField: 'CODE',
    nameField: 'NAME',
    sourceField: 'SOURCECODE',
    remarkField: 'REMARK',
    codePrefix: 'SS_',
  },
  // JS 模块：Store 扩展(@/modules/{MC}/store.js) + 扩展JS(@/modules/{MC}/{pageCode}.js)
  // 编码(CODE, 如 RS_M21_STORE) 与 路径(MODULEPATH) 是两个独立字段
  js: {
    fileType: 'JS',
    kindBadge: 'JS 模块',
    idField: 'ID',
    codeField: 'CODE',
    pathField: 'MODULEPATH',
    nameField: 'NAME',
    sourceField: 'SOURCECODE',
    remarkField: 'REMARK',
    codePrefix: '@/modules/',
  },
  vue: {
    fileType: 'VUE',
    kindBadge: 'Vue 组件',
    idField: 'ID',
    codeField: 'CODE',
    pathField: 'MODULEPATH',
    nameField: 'NAME',
    sourceField: 'SOURCECODE',
    remarkField: 'REMARK',
    codePrefix: '@/pages/',
  },
};

// API 脚本 (C#) 默认模板
export var DEFAULT_CSHARP_TEMPLATE = `// API C# 脚本（Roslyn 运行时编译，保存即生效）
// 上下文: P("参数") / UserId / Db / DbFirst / DbScalar / DbExec / Sql("SQLCODE") / Trans / MD / Operate / Log / Response
var id = P("ID");
if (id == "") { Response.SetError("ID 不能为空"); return; }
using (var t = Trans()) {
  DbExec("UPDATE 表名 SET 字段=@v WHERE ID=@id", new { v = "值", id });
  t.Commit();
}
Response.SetData(new { affected = 1 });
`;

// SQL 模板默认模板
export var DEFAULT_SQL_TEMPLATE = `-- SQL 模板（NVelocity 引擎，运行时 SQLManage.ParseSQL 注参）
-- 铁律: 禁止单引号(用 @参数 或 CHAR(39)); LIKE 用 CONCAT(CHAR(37),@P,CHAR(37));
--       禁 DDL(DROP/ALTER/TRUNCATE/CREATE); 系统变量 @_USERID_/@_EMPID_/@_DEPTID_
SELECT *
FROM 表名 A
WHERE 1=1
#if("$!{PARAM}"!="")
AND A.字段=@PARAM
#end
`;

// JS 模块默认模板（扩展 JS：methods/computed/data/hooks 合并进宿主组件）
export var DEFAULT_JS_TEMPLATE = `// JS 模块（扩展 JS / Store 扩展）
// 扩展 JS: methods/computed/data/init/mounted 会合并进宿主组件(generic-module/generic-form)
// Store 扩展: 导出 { actions, mutations } 合并进模块 Vuex store
export default {
  methods: {
    // async myAction(row) { ... }
  },
  computed: {
    // ISSHOWMYBTN() { return true }
  },
  // data() { return {} },
  // init() {},
  // mounted() {},
};
`;

// Vue 组件默认模板（sfc-editor-popup 的详细版，覆盖 code-asset.js 的简版）
export var DEFAULT_VUE_TEMPLATE = `<template>
  <div class="my-sfc-page">
    <p>SFC 页面内容</p>
    <!-- 使用 HeyUI 组件: Button / Form / Select / Table 等 -->
    <!-- 可访问 this.$store / this.$router / this.moduleCode -->
  </div>
</template>
<script>
export default {
  data() {
    return {
      // 页面数据
    };
  },
  computed: {
    // 计算属性
  },
  methods: {
    // 方法
  },
  mounted() {
    // 组件挂载后执行
  }
};
<\/script>
`;

-- SFC 在线开发平台测试数据
-- 执行后, 在编辑器文件树选择该文件, 点"编译"再"保存"生成 COMPILEDCODE

INSERT INTO tbs_sfc_template (
  ID, TEMPLATECODE, TEMPLATENAME, MODULEPATH, FILETYPE,
  SOURCECODE, COMPILEDCODE, DEPS, DESCRIPTION,
  ISDELETED, CREATEDBY, CREATEDTIME
) VALUES (
  REPLACE(UUID(), '-', ''),
  'SFC_TEST_001',
  '提示词管理(测试)',
  '@/pages/s01/m16/views/main.vue',
  'VUE',
  '<template>
  <div class=\"page-wrap\">
    <div class=\"breadcrumb\">
      <span v-for=\"(item, idx) in bcDatas\" :key=\"idx\">{{ item.title }}<i v-if=\"idx < bcDatas.length - 1\"> / </i></span>
    </div>
    <div class=\"list-header\">
      <h2 class=\"title\">{{ pageTitle }}</h2>
      <div class=\"header-btns\">
        <button class=\"btn btn-primary\" @click=\"handleAdd\">+ 新增</button>
        <button class=\"btn\" @click=\"handleRefresh\">刷新</button>
      </div>
    </div>
    <div class=\"table-wrap\">
      <table class=\"data-table\">
        <thead>
          <tr>
            <th v-for=\"col in columns\" :key=\"col.prop\" :style=\"{ width: col.width + ''px'' }\">{{ col.title }}</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for=\"(row, idx) in tableData\"
            :key=\"row.ID\"
            :class=\"{ ''is-selected'': selectedId === row.ID }\"
            @click=\"clickRow(row)\"
          >
            <td>{{ row.PROMPTKEY }}</td>
            <td>{{ row.DESCRIPTION }}</td>
            <td>{{ row.UPDATETIME }}</td>
            <td class=\"col-action\">
              <span class=\"link\" @click.stop=\"handleEdit(row)\">编辑</span>
              <span class=\"link danger\" @click.stop=\"handleDelete(row, idx)\">删除</span>
            </td>
          </tr>
          <tr v-if=\"tableData.length === 0\">
            <td colspan=\"4\" class=\"empty\">暂无数据</td>
          </tr>
        </tbody>
      </table>
    </div>
    <div class=\"footer-info\">共 {{ tableData.length }} 条记录</div>
  </div>
</template>
<script>
export default {
  name: ''s01-m16-main-mock'',
  data() {
    return {
      pageTitle: ''提示词'',
      selectedId: '''',
      bcDatas: [
        { title: ''系统管理'' },
        { title: ''提示词管理'' },
      ],
      columns: [
        { title: ''提示词键'', prop: ''PROMPTKEY'', width: 200 },
        { title: ''说明'', prop: ''DESCRIPTION'' },
        { title: ''更新时间'', prop: ''UPDATETIME'', width: 160 },
        { title: ''操作'', prop: ''__action'', width: 140 },
      ],
      tableData: [
        { ID: ''1'', PROMPTKEY: ''ai_translate'', DESCRIPTION: ''AI 翻译提示词'', UPDATETIME: ''2026-07-01 10:23'' },
        { ID: ''2'', PROMPTKEY: ''ai_summary'', DESCRIPTION: ''AI 摘要提示词'', UPDATETIME: ''2026-07-02 14:05'' },
        { ID: ''3'', PROMPTKEY: ''ai_classify'', DESCRIPTION: ''AI 分类提示词'', UPDATETIME: ''2026-07-03 09:11'' },
        { ID: ''4'', PROMPTKEY: ''ai_extract'', DESCRIPTION: ''AI 信息抽取提示词'', UPDATETIME: ''2026-07-03 16:48'' },
        { ID: ''5'', PROMPTKEY: ''ai_check'', DESCRIPTION: ''AI 校对提示词'', UPDATETIME: ''2026-07-04 08:30'' },
      ],
    };
  },
  methods: {
    handleAdd() {
      this.$alert && this.$alert(''新增功能 (mock)'');
    },
    handleRefresh() {
      this.$alert && this.$alert(''刷新成功 (mock)'');
    },
    clickRow(row) {
      this.selectedId = row.ID;
    },
    handleEdit(row) {
      this.$alert && this.$alert(''编辑: '' + row.PROMPTKEY);
    },
    handleDelete(row, idx) {
      this.tableData.splice(idx, 1);
      this.$alert && this.$alert(''已删除: '' + row.PROMPTKEY);
    },
  },
};
</script>
<style lang=\"less\" scoped>
.page-wrap {
  padding: 16px;
  background: #fff;
  font-size: 13px;
  color: #333;
  min-height: 100%;
}
.breadcrumb {
  margin-bottom: 12px;
  color: #999;
  i {
    margin: 0 4px;
  }
}
.list-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
  .title {
    font-size: 16px;
    margin: 0;
  }
}
.header-btns {
  .btn {
    padding: 4px 12px;
    border: 1px solid #ddd;
    background: #fff;
    border-radius: 4px;
    cursor: pointer;
    margin-left: 8px;
    font-size: 12px;
    &:hover {
      border-color: #0a84ff;
      color: #0a84ff;
    }
    &.btn-primary {
      background: #0a84ff;
      color: #fff;
      border-color: #0a84ff;
    }
  }
}
.table-wrap {
  border: 1px solid #e8eaec;
  border-radius: 4px;
  overflow: hidden;
}
.data-table {
  width: 100%;
  border-collapse: collapse;
  thead th {
    background: #f8f8f9;
    padding: 8px 12px;
    text-align: left;
    font-weight: 600;
    border-bottom: 1px solid #e8eaec;
  }
  tbody td {
    padding: 8px 12px;
    border-bottom: 1px solid #f0f0f0;
  }
  tbody tr {
    cursor: pointer;
    &:hover {
      background: #f5f7fa;
    }
    &.is-selected {
      background: #ebf5ff;
    }
  }
  .col-action {
    .link {
      color: #0a84ff;
      cursor: pointer;
      margin-right: 12px;
      &:hover {
        text-decoration: underline;
      }
      &.danger {
        color: #ed4014;
      }
    }
  }
  .empty {
    text-align: center;
    color: #999;
    padding: 24px;
  }
}
.footer-info {
  margin-top: 8px;
  color: #999;
  font-size: 12px;
}
</style>',
  NULL,
  '[]',
  'SFC 在线开发平台测试页面 (参考 s01/m16)',
  0,
  'system',
  NOW()
);

<template>
  <view-dialog :title="title" >
    <template slot="body">
      <ToolBar label="基本信息" :size="16"></ToolBar>
      <rs-form-edit
        ref="form"
        class="maxModalH rs-flex-col"
        :label-width="80"
        mode="twocolumn"
        :path="$MAIN"
      ></rs-form-edit>
      <ToolBar label="SQL内容" :size="16"></ToolBar>
      <div class="sqlEditArea">
        <div class="sqlParams" v-if="sqlParams.length > 0">
          <span class="paramLabel">检测到参数：</span>
          <span class="paramTag" v-for="(p, i) in sqlParams" :key="i">{{ p }}</span>
        </div>
        <div class="sqlPreview" v-if="SQLTXT">
          <pre class="sqlCode">{{ SQLTXT }}</pre>
        </div>
        <div class="sqlEmpty" v-else>
          <span class="emptyTip">请在"SQL内容"中输入SQL语句，支持NVelocity模板语法（如 #if、@参数名）</span>
        </div>
      </div>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'RS_M13/A04'" v-if="ID" @confirm="del"><Button class="ml5" color="red">删除</Button></Poptip>
      <Button class="ml5" v-per="'RS_M13/A03'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapDateTable } from '../store';
import Add01 from '@/mixins/add01';
export default {
  name: 's01-m13-add',
  mixins: [Add01],
  data() {
    return {
      sqlParams: []
    };
  },
  computed: {
    ...mapDateTable('MAIN', ['SQLTXT', 'SQLCODE', 'SQLTYPE'])
  },
  watch: {
    SQLTXT: {
      handler(newVal) {
        this.extractParams(newVal);
      },
      immediate: true
    }
  },
  methods: {
    extractParams(sqlText) {
      if (!sqlText) {
        this.sqlParams = [];
        return;
      }
      // 提取 @参数名 格式的参数
      const paramPattern = /@([A-Za-z_][A-Za-z0-9_]*)/g;
      const matches = new Set();
      let match;
      while ((match = paramPattern.exec(sqlText)) !== null) {
        // 排除系统变量
        if (!match[1].startsWith('_')) {
          matches.add('@' + match[1]);
        }
      }
      this.sqlParams = [...matches];
    }
  }
};
</script>
<style lang="less" scoped>
.sqlEditArea {
  padding: 10px;
  background: #f8f8f8;
  border-radius: 4px;
  margin: 0 10px 10px 10px;
}
.sqlParams {
  padding: 8px 10px;
  background: #fff;
  border-radius: 4px;
  margin-bottom: 10px;
  font-size: 13px;
}
.paramLabel {
  color: #666;
  margin-right: 8px;
}
.paramTag {
  display: inline-block;
  padding: 2px 8px;
  margin: 2px 4px;
  background: #e8f4fd;
  color: #3c8dbc;
  border-radius: 3px;
  font-family: Consolas, Monaco, monospace;
  font-size: 12px;
}
.sqlPreview {
  background: #1e1e1e;
  border-radius: 4px;
  padding: 10px;
}
.sqlCode {
  color: #d4d4d4;
  font-family: Consolas, Monaco, monospace;
  font-size: 13px;
  margin: 0;
  white-space: pre-wrap;
  word-break: break-all;
}
.sqlEmpty {
  padding: 20px;
  text-align: center;
}
.emptyTip {
  color: #999;
  font-size: 12px;
}
</style>

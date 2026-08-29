<template>
  <view-dialog :title="ISADD?'新增模块':'编辑模块'">
    <template slot="body">
      <Form :label-width="80" mode="twocolumn">
        <ToolBar label="基本信息" :size="16"></ToolBar>
        <Row>
          <FormItem label="模块编码">
            <input type="text" v-model="MODULECODE" placeholder="请输入模块编码" />
          </FormItem>
          <FormItem label="模块名称">
            <input type="text" v-model="MODULENAME" placeholder="请输入模块名称" />
          </FormItem>
          <FormItem label="流程名称" single>
            <Select v-model="FLOWCODE" dict="单据流程" placeholder="请选择流程"></Select>
          </FormItem>
          <FormItem label="说明" single>
            <input type="text" v-model="REMARK" placeholder="请输入说明" />
          </FormItem>
        </Row>
        <ToolBar label="数据源" :size="16">
          <div slot="right">
            <Button color="primary" icon="h-icon-plus" size="s" @click="addDts('DTSA')">新增</Button>
            <Button color="primary" icon="h-icon-minus" size="s" @click="removeDts('DTSA',$refs.DTSA)">移除</Button>
            <Button color="primary" icon="h-icon-top" size="s" @click="moveUp('DTSA',$refs.DTSA)">上移</Button>
            <Button color="primary" icon="h-icon-down" size="s" @click="moveDown('DTSA',$refs.DTSA)">下移</Button>
          </div>
        </ToolBar>
        <rs-table-edit border ref="DTSA" :height="200" :path="$DTSA" :datas="DTSA" :getProps="getProps"></rs-table-edit>
        <ToolBar label="数据源关系" :size="16">
          <div slot="right">
            <Button color="primary" icon="h-icon-plus" size="s" @click="addDts('DTSB')">新增</Button>
            <Button color="primary" icon="h-icon-minus" size="s" @click="removeDts('DTSB',$refs.DTSB)">移除</Button>
            <Button color="primary" icon="h-icon-top" size="s" @click="moveUp('DTSB',$refs.DTSB)">上移</Button>
            <Button color="primary" icon="h-icon-down" size="s" @click="moveDown('DTSB',$refs.DTSB)">下移</Button>
          </div>
        </ToolBar>
        <rs-table-edit border ref="DTSB" :height="200" :path="$DTSB" :datas="DTSB" :getProps="getDTSBProps"></rs-table-edit>
        <ToolBar label="接口" :size="16">
          <div slot="right">
            <Button color="primary" icon="h-icon-plus" size="s" @click="addDts('DTSC')">新增</Button>
            <Button color="primary" icon="h-icon-minus" size="s" @click="removeDts('DTSC',$refs.DTSC)">移除</Button>
            <Button color="primary" icon="h-icon-top" size="s" @click="moveUp('DTSC',$refs.DTSC)">上移</Button>
            <Button color="primary" icon="h-icon-down" size="s" @click="moveDown('DTSC',$refs.DTSC)">下移</Button>
          </div>
        </ToolBar>
        <rs-table-edit border ref="DTSC" :height="200" :path="$DTSC" :datas="DTSC" :getProps="getDTSCProps"></rs-table-edit>
      </Form>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'RS_M02/A07'" v-if="!ISADD" @confirm="del"><Button class="ml5" color="red">删除</Button></Poptip>
      <Button class="ml5" v-per="'RS_M02/A04'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>

<script>
import { mapDateTable, Constants } from '../store';
import { queryFieldsByResourceId, queryFiltersByResourceId, querySqlTemplates } from '@/utils/selRegistry';
export default {
  name: 'zyadd',
  data() {
    return {
      currentStartIndex: 0,
      currentEndIndex: 0,
      selfShow: false,
      selfShowLoading: false,
      selfShow2: false,
      selfShowLoading2: false,
      ruleValidate: {
        name: [
          {
            required: true,
            message: '不可为空',
            trigger: 'blur',
          },
        ],
        yygx: [
          {
            required: true,
            message: '不可为空',
            trigger: 'blur',
          },
        ],
      },
      editInfo: {
        column: null,
        index: null,
      },
    };
  },
  computed: {
    ...mapDateTable('MAIN', ['ID', 'MODULECODE', 'MODULENAME', 'REMARK', 'FLOWCODE']),
    ...mapDateTable('DTSA', []),
    ...mapDateTable('SEL', []),
    ...mapDateTable('DTSB', []),
    ...mapDateTable('DTSC', []),
    ISDATAVIEW() {
      return this.RESOURCETYPE == 'DATAVIEW';
    },
    ISTABLE() {
      return this.RESOURCETYPE == 'TABLE';
    },
    TDTSA() {
      return JSON.parse(JSON.stringify(this.DTSA));
    },
    ISADD() {
      return !this.ID;
    },
    pathNameOptions() {
      return (this.DTSA || [])
        .filter(item => item.PATHNAME)
        .map(item => ({ key: item.PATHNAME, title: item.PATHNAME }));
    },
    apiCodeOptions() {
      return (this.DTSC || [])
        .filter(item => item.APICODE)
        .map(item => ({ key: item.APICODE, title: item.APICODE + (item.APINAME ? '(' + item.APINAME + ')' : '') }));
    },
  },
  mounted() {
    // 监听 DTSA 数据变化，动态更新 DTSB/DTSC 的下拉选项
    this.$nextTick(() => {
      if (this.$refs.DTSA) {
        this.$refs.DTSA.$on('data-change', () => {
          this.$nextTick(() => {
            if (this.$refs.DTSB) this.$refs.DTSB.setColumns();
            if (this.$refs.DTSC) this.$refs.DTSC.setColumns();
          });
        });
      }
    });
  },
  methods: {
    async onDTSACellSearch({ value, field }) {
      await this.$callAction({ action: `${Constants.STORE_NAME}/querySel`, param: { INPUT: value }, isBusy: false });
    },
    closeW() {
      this.$parent.setvalue(false);
    },
    applyEdit(row, column, index, path, event) {
      this['$' + path].setValue(column, event || row[column], this[path][index]);
      this.editClick(null, index);
    },
    editClick: function(column, index, path) {
      this.editInfo.column = column;
      this.editInfo.index = index;
      let _this = this;
      this.$nextTick(() => {
        _this.$refs[path + '-' + column + '-' + index].focus();
      });
    },
    show(index) {
      if (this.data1[index].ISFORBID === '0') {
        this.$Modal.info({
          title: '信息显示',
          content: `Name：${this.data1[index].name}<br>Age：${this.data1[index].age}<br>Address：${this.data1[index].address}`,
        });
      }
    },
    addDts(path) {
      this.$store.commit(`${Constants.STORE_NAME}/ADD`, { path });
    },
    moveUp(path, table) {
      this[`$${path}`].upItem({ item: table.currentRow });
      this.$nextTick(() => {
        table.clickCurrentRow(this[path].indexOf(table.currentRow));
      });
    },
    moveDown(path, table) {
      this[`$${path}`].downItem({ item: table.currentRow });
      this.$nextTick(() => {
        table.clickCurrentRow(this[path].indexOf(table.currentRow));
      });
    },
    removeDts(path, table) {
      if (table.currentRow == -1) {
        return;
      }
      this.$store.commit(`${Constants.STORE_NAME}/DEL`, { path, item: table.currentRow });
    },
    async remoteMethod2(INPUT, callback) {
      await this.$callAction({ action: `${Constants.STORE_NAME}/querySelT`,
        param: {
          INPUT,
        },
        isBusy: false });
      callback(this.SEL);
    },
    save() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/save`,
        successText: '保存成功',
        isSuccessBack: true,
      });
    },
    async del() {
      await this.$confirm('确认删除？');
      this.$callAction({
        action: `${Constants.STORE_NAME}/delete`,
        successText: '删除成功',
        isSuccessBack: true,
      });
    },
    close(nodesc) {},
    clickDtsA(row, index) {
      this.editInfo.index = index;
    },
    getProps(field) {
      if (field === 'RESOURCENAME') {
        return {
          cellProps: {
            option: { loadData: this.remoteMethod2, keyName: 'ID', titleName: 'RESOURCENAME' },
          },
          cellOn: {},
        };
      }
    },
    getResourceIdByPathName(pathName) {
      const dtsa = (this.DTSA || []).find(item => item.PATHNAME === pathName);
      return dtsa ? dtsa.RESOURCEID : null;
    },
    loadFieldsByPathName(pathName, text, callback) {
      const rid = this.getResourceIdByPathName(pathName);
      if (!rid) { callback([]); return }
      queryFieldsByResourceId(rid).then(fields => {
        const list = fields || [];
        const lower = (text || '').toLowerCase();
        const filtered = lower ?
          list.filter(f => f.key.toLowerCase().indexOf(lower) >= 0 || f.title.toLowerCase().indexOf(lower) >= 0) :
          list;
        callback(filtered);
      }).catch(() => {
        callback([]);
      });
    },
    loadFiltersByPathName(pathName, text, callback) {
      const rid = this.getResourceIdByPathName(pathName);
      if (!rid) { callback([]); return }
      queryFiltersByResourceId(rid).then(filters => {
        const list = filters || [];
        const lower = (text || '').toLowerCase();
        const filtered = lower ?
          list.filter(f => f.key.toLowerCase().indexOf(lower) >= 0 || f.title.toLowerCase().indexOf(lower) >= 0) :
          list;
        callback(filtered);
      }).catch(() => {
        callback([]);
      });
    },
    loadSqlTemplates(text, callback) {
      querySqlTemplates(text || '').then(list => {
        callback(list || []);
      }).catch(() => {
        callback([]);
      });
    },
    getDTSBProps(field) {
      if (field === 'PATHNAMEA' || field === 'PATHNAMEB') {
        return {
          type: 'select',
          cellProps: { datas: this.pathNameOptions },
        };
      }
      if (field === 'RFIELDSA') {
        return {
          type: 'autocomplete',
          cellProps: {
            option: {
              loadData: (text, callback, rowData) => this.loadFieldsByPathName(rowData.PATHNAMEA, text, callback),
              keyName: 'key',
              titleName: 'title',
            },
          },
        };
      }
      if (field === 'RFIELDSB') {
        return {
          type: 'autocomplete',
          cellProps: {
            option: {
              loadData: (text, callback, rowData) => this.loadFieldsByPathName(rowData.PATHNAMEB, text, callback),
              keyName: 'key',
              titleName: 'title',
            },
          },
        };
      }
      return {};
    },
    getDTSCProps(field) {
      if (field === 'PATHNAME') {
        return {
          type: 'select',
          cellProps: { datas: this.pathNameOptions },
        };
      }
      if (field === 'FILTERCODE') {
        return {
          type: 'autocomplete',
          cellProps: {
            option: {
              loadData: (text, callback, rowData) => this.loadFiltersByPathName(rowData.PATHNAME, text, callback),
              keyName: 'key',
              titleName: 'title',
            },
          },
        };
      }
      if (field === 'BEFOREAPICODE' || field === 'AFTERAPICODE') {
        return {
          type: 'autocomplete',
          cellProps: {
            option: {
              loadData: (text, callback) => {
                const list = this.apiCodeOptions || [];
                const lower = (text || '').toLowerCase();
                const filtered = lower ?
                  list.filter(f => f.key.toLowerCase().indexOf(lower) >= 0 || f.title.toLowerCase().indexOf(lower) >= 0) :
                  list;
                callback(filtered);
              },
              keyName: 'key',
              titleName: 'title',
            },
          },
        };
      }
      if (field === 'SQLID') {
        return {
          type: 'autocomplete',
          cellProps: {
            option: {
              loadData: (text, callback) => this.loadSqlTemplates(text, callback),
              keyName: 'key',
              titleName: 'title',
            },
          },
        };
      }
      return {};
    },
  },
  watch: {
    DTSA: {
      handler(newVal) {
        if (newVal && newVal.length > 0) {
          this.$nextTick(() => {
            if (this.$refs.DTSB) this.$refs.DTSB.setColumns();
            if (this.$refs.DTSC) this.$refs.DTSC.setColumns();
          });
        }
      },
      immediate: true,
    },
  },
};
</script>

<style scoped>
.maxModalH {
  max-height: calc(100vh - 185px);
  overflow-y: auto;
  overflow-x: hidden;
}
</style>

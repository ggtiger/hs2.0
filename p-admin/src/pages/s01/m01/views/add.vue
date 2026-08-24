<template>
  <view-dialog :title="!ID?'新增资源':'编辑资源'" >
    <template slot="body" class="maxModalH">
      <Form :rules="ruleValidate" mode="twocolumn" :label-width="80">
        <ToolBar label="基本信息" :size="16"></ToolBar>
        <Row>
          <FormItem label="资源名称" prop="RESOURCENAME">
            <input type="text" v-model="RESOURCENAME" placeholder="请输入" />
          </FormItem>
          <FormItem label="资源类型">
            <Select v-model="RESOURCETYPE" dict="资源类型"></Select>
          </FormItem>
          <FormItem label="来源表名" v-if="RESOURCETYPE=='DATAVIEW'">
            <AutoComplete :option="param" v-model="TABLERESOURCE" type="object">
              <template slot="item" slot-scope="{item}">
                <div>{{item.value.RESOURCENAME}}</div>
              </template>
            </AutoComplete>
          </FormItem>
          <FormItem label="资源别名" v-if="RESOURCETYPE=='DATAVIEW'">
            <input type="text" v-model="RESOURCEANAME" placeholder="请输入" />
          </FormItem>
          <FormItem label="说明" single>
            <input type="text" v-model="COMMENTS" placeholder="请输入" />
          </FormItem>
        </Row>
        <ToolBar label="字段信息" :size="16">
          <div slot="right">
            <Button v-if="ISTABLE && ID" color="green" size="s" icon="h-icon-compare" @click="doCompare">对比表结构</Button>
            <Button v-if="ISTABLE && ID" color="blue" size="s" icon="h-icon-refresh" @click="doRefresh">刷新元数据</Button>
            <Button color="primary" size="s" icon="h-icon-search" @click="showSelf('MAIN')">选入资源字段</Button>
            <Button color="primary" icon="h-icon-plus" size="s" @click="addDts('DTSA')">新增</Button>
            <Button color="primary" size="s" icon="h-icon-minus" @click="removeDtsA('DTSA',$refs.DTSA)">移除</Button>
            <Button color="primary" size="s" icon="h-icon-top" @click="moveUp('DTSA',$refs.DTSA)">上移</Button>
            <Button color="primary" size="s" icon="h-icon-down" @click="moveDown('DTSA',$refs.DTSA)">下移</Button>
          </div>
        </ToolBar>
        <Modal v-model="selfShow" title="引入来源表" hasCloseIcon middle>
          <refSet showType="MAIN" @on-ok="selfOK"></refSet>
        </Modal>
        <Modal v-model="selfShow2" title="设置引用资源" hasCloseIcon middle>
          <refSet showType @on-ok="selfOK2"></refSet>
        </Modal>
        <rs-table-edit border ref="DTSA" :height="300" :path="$DTSA" :datas="DTSA" :getProps="getDTSAProps">
          <TableItem title="操作" :width="150" align="center" fixed="right">
            <template slot-scope="{data}">
              <button
                v-if="!data.UPFIELDID"
                class="h-btn h-btn-s h-btn-blue"
                @click="showSelf('',data)"
              >设置引用</button>
            </template>
          </TableItem>
        </rs-table-edit>
        <!-- 表结构对比区域 -->
        <div v-if="ISTABLE && ID && compareResult" class="compare-section">
          <ToolBar label="表结构对比" :size="16">
            <div slot="right">
              <Button size="s" icon="h-icon-refresh" @click="doCompare">刷新</Button>
            </div>
          </ToolBar>
          <div class="compare-status">
            <span v-if="!compareResult.tableExists" class="compare-alert compare-alert-red">物理表不存在</span>
            <span v-else-if="metaOnlyFields.length > 0" class="compare-alert compare-alert-yellow">有差异（{{ metaOnlyFields.length }}个字段待同步）</span>
            <span v-else-if="physicalOnlyFields.length > 0" class="compare-alert compare-alert-yellow">有差异（{{ physicalOnlyFields.length }}个字段仅在物理表中）</span>
            <span v-else class="compare-alert compare-alert-green">完全一致</span>
          </div>
          <Table :datas="compareResult.columns" :height="200" border>
            <TableItem title="字段名" prop="FIELDNAME" :width="150"></TableItem>
            <TableItem title="元数据类型" :width="150">
              <template slot-scope="{data}">
                <span v-if="data.IN_META">{{ formatFieldType(data.META_FIELDTYPE, data.META_FIELDLENGTH, data.META_PREC) }}</span>
                <span v-else>-</span>
              </template>
            </TableItem>
            <TableItem title="物理表类型" :width="150">
              <template slot-scope="{data}">
                <span v-if="data.IN_PHYSICAL">{{ formatPhysicalType(data) }}</span>
                <span v-else>-</span>
              </template>
            </TableItem>
            <TableItem title="状态" :width="120">
              <template slot-scope="{data}">
                <span v-if="data.STATUS==='matched'" class="h-tag h-tag-green">一致</span>
                <span v-else-if="data.STATUS==='meta_only'" class="h-tag h-tag-yellow">仅元数据</span>
                <span v-else class="h-tag h-tag-red">仅物理表</span>
              </template>
            </TableItem>
          </Table>
          <div v-if="metaOnlyFields.length > 0 || !compareResult.tableExists" style="margin-top:10px;text-align:right;">
            <Button color="primary" icon="h-icon-plus" :loading="syncLoading" @click="doSync">同步到物理表</Button>
          </div>
        </div>
        <ToolBar label="过滤器" :size="16" v-if="RESOURCETYPE=='DATAVIEW'||RESOURCETYPE=='SQL'">
          <div slot="right">
            <Button color="primary" icon="h-icon-plus" size="s" @click="addDts('DTSB')">新增</Button>
            <Button color="primary" icon="h-icon-minus" size="s" @click="removeDts('DTSB',$refs.DTSB)">移除</Button>
            <Button color="primary" icon="h-icon-top" size="s" @click="moveUp('DTSB',$refs.DTSB)">上移</Button>
            <Button color="primary" icon="h-icon-down" size="s" @click="moveDown('DTSB',$refs.DTSB)">下移</Button>
          </div>
        </ToolBar>
        <rs-table-edit
          border
          ref="DTSB"
          :path="$DTSB"
          :datas="DTSB"
          v-show="RESOURCETYPE=='DATAVIEW'||RESOURCETYPE=='SQL'"
        ></rs-table-edit>
      </Form>
    </template>
    <div slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'RS_M01/A07'" v-if="!ISADD" @confirm="del"><Button class="ml5" color="red">删除</Button></Poptip>
      <Button class="ml5" v-per="'RS_M01/A03'" color="primary" @click.native="save">确定</Button>
    </div>
  </view-dialog>
</template>
<script>
import { mapDateTable, Constants } from '../store';
import { queryFieldsByResourceId } from '@/utils/selRegistry';
import refSet from './refSet.vue';
export default {
  name: 'zyadd',
  props: { DID: { Type: String, default: '' } },
  components: {
    refSet,
  },
  data() {
    return {
      currentStartIndex: 0,
      currentEndIndex: 0,
      selfShow: false,
      selfShowLoading: false,
      selfShow2: false,
      selfShowLoading2: false,
      compareResult: null,
      syncLoading: false,

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
      param: {
        loadData: this.remoteMethod2,
        keyName: 'ID',
        titleName: 'RESOURCENAME',
      },
      storeCall: this.$getStoreCall({ STORE_NAME: Constants.STORE_NAME }),
    };
  },
  computed: {
    ...mapDateTable('MAIN', [
      'ID',
      'RESOURCENAME',
      'TABLERESOURCEID',
      'TABLERESOURCENAME',
      'TABLENAME',
      'RESOURCEANAME',
      'RESOURCETYPE',
      'SQLCODE',
      'ISFORBID',
      'ISCREATE',
      'COMMENTS',
    ]),
    ...mapDateTable('DTSA', []),
    ...mapDateTable('SEL', []),
    ...mapDateTable('DTSB', []),
    TABLERESOURCE: {
      get() {
        return { ID: this.TABLERESOURCEID, RESOURCENAME: this.TABLERESOURCENAME };
      },
      set(v) {
        this.TABLERESOURCEID = v.ID;
        this.TABLERESOURCENAME = v.RESOURCENAME;
        this.TABLENAME = v.RESOURCENAME;
      },
    },
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
    metaOnlyFields() {
      if (!this.compareResult || !this.compareResult.columns) return [];
      return this.compareResult.columns.filter(c => c.STATUS === 'meta_only');
    },
    physicalOnlyFields() {
      if (!this.compareResult || !this.compareResult.columns) return [];
      return this.compareResult.columns.filter(c => c.STATUS === 'physical_only');
    },
  },
  created() {
    this.storeCall({ action: 'init', timeOut: 0 });
  },
  methods: {
    handelLoadmore(currentStartIndex, currentEndIndex) {
      this.currentStartIndex = currentStartIndex;
      this.currentEndIndex = currentEndIndex;
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
      // this.TDTSA.push({})
      this.$store.commit(`${Constants.STORE_NAME}/ADD_${path}`);
    },
    removeDtsA(path, table) {
      let item = table.currentRow;
      this.$store.commit(`${Constants.STORE_NAME}/DEL_DTSA`, {
        item,
      });
    },
    removeDtsB(item) {
      this.$store.commit(`${Constants.STORE_NAME}/DEL_DTSB`, {
        item,
      });
    },
    async remoteMethod2(INPUT, callback) {
      await this.$callAction({ action: `${Constants.STORE_NAME}/querySelT`,
        param: {
          INPUT,
        },
        isBusy: false });
      callback(this.SEL);
    },
    moveUp(path, table) {
      debugger;
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
    showSelf(TYPE, item) {
      this.$callAction({ action: `${Constants.STORE_NAME}/querySelF`,
        param: {
          RESOURCEID: this.TABLERESOURCEID,
          TYPE,
          item,
        },
        isBusy: false });
      if (TYPE == 'MAIN') this.selfShow = true;
      else this.selfShow2 = true;
    },
    selfOK(items) {
      this.$callAction({ action: `${Constants.STORE_NAME}/setDtsA`,
        param: {
          TYPE: 'MAIN',
          items,
        },
        isBusy: false });
    },
    selfOK2(items) {
      this.$callAction({ action: `${Constants.STORE_NAME}/setDtsA`,
        param: {
          TYPE: '',
          items,
        },
        isBusy: false });
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
    getDTSAProps(field) {
      if (field === 'UPDATEFIELDS') {
        return {
          type: 'autocomplete',
          cellProps: {
            option: {
              loadData: (text, callback, rowData) => {
                const rid = rowData.REFRESOURCEID;
                if (!rid) { callback([]); return }
                queryFieldsByResourceId(rid).then(fields => {
                  const list = fields || [];
                  const lower = (text || '').toLowerCase();
                  const filtered = lower ?
                    list.filter(f => f.key.toLowerCase().indexOf(lower) >= 0 || f.title.toLowerCase().indexOf(lower) >= 0) :
                    list;
                  callback(filtered);
                }).catch(() => { callback([]) });
              },
              keyName: 'key',
              titleName: 'title',
            },
          },
        };
      }
      return {};
    },
    async doRefresh() {
      if (!this.TABLENAME || !this.ID) {
        this.$Message.warn('表名或资源ID为空');
        return;
      }
      try {
        await this.$confirm('确认刷新元数据？将根据物理表列更新字段定义（新增缺失字段、 更新已有字段类型）。');
        this.$Loading();
        let ret = await this.$callAction({ action: `${Constants.STORE_NAME}/refreshTable`,
          param: {
            TABLENAME: this.TABLENAME,
            RESOURCEID: this.ID,
          },
          isBusy: false });
        this.$Loading.close();
        this.$Message.success(`刷新成功： 新增${ret.addedCount}个， 更新${ret.updatedCount}个`);
        // 重新打开数据以刷新页面
        this.storeCall({ action: 'open', param: { DID: this.DID }, timeOut: 0 });
        this.compareResult = null;
      } catch (e) {
        this.$Loading.close();
        if (e.message !== '您的手速太快了') {
          this.$Message.error(e.message || '刷新失败');
        }
      }
    },
    async doCompare() {
      if (!this.TABLENAME || !this.ID) {
        this.$Message.warn('表名或资源ID为空');
        return;
      }
      try {
        this.$Loading();
        let ret = await this.$callAction({ action: `${Constants.STORE_NAME}/compareTable`,
          param: {
            TABLENAME: this.TABLENAME,
            RESOURCEID: this.ID,
          },
          isBusy: false });
        this.compareResult = ret;
        this.$Loading.close();
      } catch (e) {
        this.$Loading.close();
        this.$Message.error(e.message || '对比失败');
      }
    },
    async doSync() {
      if (!this.compareResult) return;
      let fields;
      if (!this.compareResult.tableExists) {
        // 表不存在，同步所有元数据字段
        fields = this.compareResult.columns
          .filter(c => c.IN_META)
          .map(c => ({
            FIELDNAME: c.FIELDNAME,
            FIELDTYPE: c.META_FIELDTYPE,
            FIELDLENGTH: c.META_FIELDLENGTH,
            PREC: c.META_PREC,
            NULLABLE: c.META_NULLABLE,
            DEFAULTVALUE: c.META_DEFAULTVALUE,
            COMMENTS: c.META_COMMENTS,
            ISKEY: c.META_ISKEY,
          }));
      } else {
        // 表存在，仅同步 meta_only 字段
        fields = this.metaOnlyFields.map(c => ({
          FIELDNAME: c.FIELDNAME,
          FIELDTYPE: c.META_FIELDTYPE,
          FIELDLENGTH: c.META_FIELDLENGTH,
          PREC: c.META_PREC,
          NULLABLE: c.META_NULLABLE,
          DEFAULTVALUE: c.META_DEFAULTVALUE,
          COMMENTS: c.META_COMMENTS,
        }));
      }
      if (fields.length === 0) {
        this.$Message.info('没有需要同步的字段');
        return;
      }
      try {
        this.syncLoading = true;
        await this.$callAction({ action: `${Constants.STORE_NAME}/syncTable`,
          param: {
            TABLENAME: this.TABLENAME,
            FIELDS: fields,
          },
          isBusy: false });
        this.$Message.success('同步成功');
        // 刷新对比
        await this.doCompare();
      } catch (e) {
        this.$Message.error(e.message || '同步失败');
      } finally {
        this.syncLoading = false;
      }
    },
    formatFieldType(type, length, prec) {
      if (!type) return '';
      if (type === 'varchar') return length > 0 ? `varchar(${length})` : 'varchar(255)';
      if (type === 'decimal') return prec > 0 ? `decimal(${length},${prec})` : 'decimal(18,2)';
      if (['text', 'int', 'bigint', 'float', 'datetime', 'date', 'tinyint'].indexOf(type) > -1) return type;
      return length > 0 ? `varchar(${length})` : type;
    },
    formatPhysicalType(data) {
      let type = data.PHYSICAL_DATA_TYPE;
      if (!type) return '';
      if (type === 'varchar' || type === 'char') {
        return data.PHYSICAL_MAX_LENGTH ? `${type}(${data.PHYSICAL_MAX_LENGTH})` : type;
      }
      if (type === 'decimal' || type === 'numeric') {
        if (data.PHYSICAL_PRECISION && data.PHYSICAL_SCALE) {
          return `${type}(${data.PHYSICAL_PRECISION},${data.PHYSICAL_SCALE})`;
        }
        return type;
      }
      return type;
    },
  },
  watch: {
    DID: function(v) {
      if (v !== '') {
        this.$parent.title = '编辑资源';
      } else {
        this.$parent.title = '新增资源';
      }
    },
    '$parent.isOpened': {
      handler(v) {
        if (v) {
          this.storeCall({ action: 'open', param: { DID: this.DID }, timeOut: 0 });
        }
      },
    },
  },
};
</script>

<style scoped>
.maxModalH {
  max-height: calc(100vh - 185px);
  overflow: auto;
}
.h-modal .h-notify-content {
  padding: 10px 15px;
}
.compare-section {
  margin-top: 10px;
  border: 1px solid #e8e8e8;
  border-radius: 4px;
  padding: 10px;
  background: #fafafa;
}
.compare-status {
  margin-bottom: 10px;
}
.compare-alert {
  display: inline-block;
  padding: 4px 12px;
  border-radius: 3px;
  font-size: 13px;
}
.compare-alert-green {
  background: #e8f8e8;
  color: #52c41a;
  border: 1px solid #b7eb8f;
}
.compare-alert-yellow {
  background: #fffbe6;
  color: #faad14;
  border: 1px solid #ffe58f;
}
.compare-alert-red {
  background: #fff2f0;
  color: #f5222d;
  border: 1px solid #ffccc7;
}
</style>

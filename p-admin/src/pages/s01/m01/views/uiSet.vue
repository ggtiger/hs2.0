<template>
  <view-dialog title="界面设置" @on-show="onShow">
    <template slot="body">
      <Form :label-width="80">
        <Row :space-x="19" :space-y="5">
          <Cell width="24">
            <div>
              <FormItem label="资源">
                <input type="text" v-model="item.RESOURCENAME" />
              </FormItem>
            </div>
          </Cell>
        </Row>
        <ToolBar label="UI字段" :size="16">
          <div slot="right">
            <Button color="primary" icon="h-icon-plus" size="s" @click="addDts('DTSC')">新增</Button>
            <Button color="primary" icon="h-icon-minus" size="s" @click="removeDts('DTSC',$refs.DTSC)">移除</Button>
            <Button color="primary" icon="h-icon-top" size="s" @click="moveUp('DTSC',$refs.DTSC)">上移</Button>
            <Button color="primary" icon="h-icon-down" size="s" @click="moveDown('DTSC',$refs.DTSC)">下移</Button>
            <Button color="primary" icon="h-icon-search" size="s" @click="showFieldSel">选入资源字段</Button>
          </div>
        </ToolBar>
        <div class="rr-flex-1">
          <rs-table-edit border ref="DTSC" :path="$DTSC" :datas="DTSC" :height="500"></rs-table-edit>
        </div>
        <Modal
          v-model="modal"
          title="选入资源字段"
          :styles="{top: '20px'}"
          width="80%"
          :loading="loading"
          :footer-hide="true"
          :closeOnMask="false"
          hasCloseIcon
          middle
        >
          <div>
            <fieldSel v-if="modal" @on-select="selectFields" :item="item"></fieldSel>
          </div>
        </Modal>
      </Form>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="close">取消</Button>
      <Button class="ml5" color="primary" @click.native="ok">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import fieldSel from './fieldSel.vue';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 'uiSet',
  props: {
    showType: {
      type: [String, Number],
    },
    item: { Type: Object },
  },
  components: {
    fieldSel,
  },
  data() {
    return {
      modal: false,
      loading: false,
      editInfo: { index: -1 },
      tableH: 300,
    };
  },
  methods: {
    onSelect(row, index) {
      this.editInfo.index = index;
    },
    editClick: function(column, index, path) {
      this.editInfo.column = column;
      this.editInfo.index = index;
      let _this = this;
      this.$nextTick(() => {
        _this.$refs[path + '-' + column + '-' + index].focus();
      });
    },
    applyEdit(row, column, index, path, event) {
      this['$' + path].setValue(column, event || row[column], this[path][index]);
      this.editClick(null, index);
    },
    clickCheck(row) {
      debugger;
    },
    addDts(path) {
      this.$store.commit(`${Constants.STORE_NAME}/ADD_DTSC`, { RESOURCEID: this.item.ID });
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
    selectFields(items) {
      this.$store.commit(`${Constants.STORE_NAME}/SET_DTSC`, {
        items,
      });
    },
    async showFieldSel() {
      this.loading = true;
      await this.$callAction({ action: `${Constants.STORE_NAME}/queryFIELDSEL`,
        param: {
          RESOURCEID: this.item.ID,
        },
        isBusy: false });
      this.loading = false;
      this.modal = true;
    },
    close() {
      this.$parent.close();
    },
    async ok() {
      await this.$callAction({
        action: `${Constants.STORE_NAME}/saveDTSC`,
        successText: '保存成功',
        isSuccessBack: true,
        successCall: this.$parent.close,
      });
    },
  },
  computed: {
    ...mapDateTable('SELF', ['REFRESOURCENAME', 'REFRESOURCEANAME', 'REFRELATION', 'REFRESOURCEID', 'TYPE']),
    ...mapDateTable('DTSC', []),
    ...mapDateTable('SEL', []),
  },
  watch: {
    '$parent.isOpened': {
      immediate: true,
      handler(v) {
        if (v) {
          this.$callAction({ action: `${Constants.STORE_NAME}/queryDTSC`,
            param: {
              RESOURCEID: this.item.ID,
            },
            isBusy: false });
        }
      },
    },
  },
  mounted() {},
};
</script>
<style scoped lang="less">
.ivu-table-cell {
  padding-left: 1px;
  padding-right: 1px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: normal;
  word-break: break-all;
  box-sizing: border-box;
}
</style>

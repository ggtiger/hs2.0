<template>
  <view-dialog title="选入字段">
    <template slot="body">
      <Form ref="formValidate" :model="SELF[0]" :label-width="80" class="rs-flex-col">
        <Row>
          <Col span="11">
            <FormItem label="资源名称">
              <AutoComplete
                :option="param"
                v-model="REFRESOURCE"
                @change="selectMethod"
                type="object"
              >
                <template slot="item" slot-scope="{item}">
                  <div>{{item.value.RESOURCENAME}}</div>
                </template>
              </AutoComplete>
            </FormItem>
          </Col>
        </Row>
        <div class="rs-flex-1 rr-overflow-hidden">
          <Table border ref="selection" :datas="SELFDTS" checkbox>
            <TableItem title="#" prop="$serial" align="center" :width="80"></TableItem>
            <TableItem title="名称" prop="COMMENTS" :width="150"></TableItem>
            <TableItem title="字段名" prop="FIELDNAME" :width="150"></TableItem>
            <TableItem title="允许空" align="center" :width="150">
              <template slot-scope="{data}">{{data.NULLABLE?'√':''}}</template>
            </TableItem>
            <TableItem title="主键否" align="center" :width="150">
              <template slot-scope="{data}">{{data.ISKEY?'√':''}}</template>
            </TableItem>
            <TableItem title="字段类型" prop="FIELDTYPE" :width="150"></TableItem>
          </Table>
        </div>
      </Form>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="close">取消</Button>
      <Button class="ml5" color="primary" @click.native="ok">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 'fieldSel',
  props: {
    showType: {
      type: [String, Number],
    },
    item: { Type: Object },
  },
  data() {
    return {
      param: {
        loadData: this.remoteMethod2,
        keyName: 'ID',
        titleName: 'RESOURCENAME',
      },
    };
  },
  methods: {
    clickCheck(row) {
      debugger;
    },
    async remoteMethod2(INPUT, callback) {
      await this.$callAction({ action: `${Constants.STORE_NAME}/querySelT`, param: { INPUT }, isBusy: false });
      callback(this.SEL);
    },
    selectMethod({ value }) {
      this.REFRESOURCENAME = value.RESOURCENAME;
      this.REFRESOURCEID = value.ID;
      this.$callAction({ action: `${Constants.STORE_NAME}/querySelsDts`,
        param: {
          RESOURCEID: this.REFRESOURCEID,
        },
        isBusy: false });
    },
    close() {
      this.$parent.setvalue(false);
    },
    ok() {
      this.$emit('on-select', this.$refs.selection.getSelection());
      this.$parent.setvalue(false);
    },
  },
  mounted() {
    // 打开弹窗时，如果已传入资源 ID，自动加载该资源的字段列表
    if (this.item && this.item.ID) {
      this.$callAction({ action: `${Constants.STORE_NAME}/querySelsDts`,
        param: {
          RESOURCEID: this.item.ID,
        },
        isBusy: false });
    }
  },
  computed: {
    titem() {
      return Object.assign({}, this.item);
    },
    ...mapDateTable('SELF', ['REFRESOURCENAME', 'REFRESOURCEANAME', 'REFRELATION', 'REFRESOURCEID', 'TYPE']),
    ...mapDateTable('SELFDTS', []),
    ...mapDateTable('SEL', []),
    REFRESOURCE: {
      get() {
        return { ID: this.titem.ID, RESOURCENAME: this.titem.RESOURCENAME };
      },
      set(v) {
        v = v || {};
        this.titem.ID = v.ID;
        this.titem.RESOURCENAME = v.RESOURCENAME;
      },
    },
  },
};
</script>
<style scoped>
.maxModalH {
  height: calc(100vh - 185px);
  overflow: auto;
}
</style>

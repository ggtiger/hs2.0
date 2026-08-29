<template>
  <view-dialog title="选入原始记录模版" @on-show="onShow" style="max-width:800px;" :loading="loading">
    <div slot="body" style="height: calc(100vh - 167px);">
      <Form :label-width="80" class="maxModalH rs-flex-col" style="overflow: hidden;">
        <Row>
          <Col span="11">
            <Search placeholder="请输入关键字" v-model="INPUT" style="width:100%;" @search="ptmpSel" />
          </Col>
        </Row>
        <div class="rs-flex-1 rr-overflow-hidden maxModalH">
          <rs-table-list :datas="PTMP" :path="$PTMP" border checkbox class ref="selection" @select="onCheckSelect" @trclick="onRowClick"></rs-table-list>
        </div>
      </Form>
    </div>
    <template slot="footer">
      <Button class="ml5" @click.native="close">取消</Button>
      <Button class="ml5" color="primary" @click.native="ok">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 'ptmpSel',
  props: {
    showType: {
      type: [String, Number],
    },
    item: { Type: Object },
  },
  data() {
    return {
      INPUT: '',
    };
  },
  computed: {
    ...mapDateTable('PTMP', []),
  },
  methods: {
    async ptmpSel(INPUT, callback) {
      await this.$callAction({ action: `${Constants.STORE_NAME}/ptmpSel`,
        param: {
          INPUT,
          DEPTID: this.item.ADEPTID || -1,
        },
        isBusy: false });
      // setRowSelect 只是高亮不会勾选 checkbox，getSelection 只取勾选行，必须用 setSelection
      if (this.PTMP && this.PTMP[0]) this.$refs.selection.setSelection([this.PTMP[0]]);
    },
    // 单选效果：勾选新行时自动取消其它行，保证 getSelection 只有一行
    onCheckSelect(rows) {
      if (rows && rows.length > 1) {
        this.$refs.selection.setSelection([rows[rows.length - 1]]);
      }
    },
    // 点行即选中该行（单选）
    onRowClick(data) {
      this.$refs.selection.setSelection([data]);
    },
    close() {
      this.$parent.setvalue(false);
    },
    ok() {
      this.$emit('on-select', this.$refs.selection.getSelection());
      this.$parent.setvalue(false);
    },
    onShow() {
      this.ptmpSel('');
    },
  },
  mounted() {},
};
</script>
<style scoped>
.maxModalH {
  height: calc(100vh - 185px);
  overflow: auto;
}
</style>

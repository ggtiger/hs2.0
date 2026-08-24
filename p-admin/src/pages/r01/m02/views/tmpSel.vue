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
          <rs-table-list :datas="PTMP" :path="$PTMP" border checkbox class ref="selection"></rs-table-list>
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
      if (this.PTMP) this.$refs.selection.$refs.table.setRowSelect(this.PTMP[0]);
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

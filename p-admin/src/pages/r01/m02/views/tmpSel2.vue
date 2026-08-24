<template>
   <div class="h-panel h-panel-no-border">
    <div class="h-panel-bar">
      <span class="h-panel-title">选入原始记录模版</span>
    </div>
    <div class="h-panel-body" ref="tableH">
      <Form :label-width="80" class="maxModalH rs-flex-col">
        <Row>
          <Col span="11">
            <Search placeholder="请输入关键字" v-model="INPUT" style="width:100%;" @search="ptmpSel" />
          </Col>
        </Row>
        <div class="rs-flex-1 rr-overflow-hidden">
          <rs-table-list :datas="PTMP" :path="$PTMP" border checkbox class ref="selection"></rs-table-list>
        </div>
      </Form>
      <div class="rs-modal-footer rs-text-right" slot="footer">
        <Button class="ml5" @click.native="close">取消</Button>
        <Button class="ml5" color="primary" @click.native="ok">确定</Button>
      </div>
    </div>
  </div>
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
  mounted() {
    this.ptmpSel('');
  },
};
</script>
<style scoped>
.maxModalH {
  height: calc(100vh - 185px);
  overflow: auto;
}
</style>

<template>
  <view-dialog title="选入接口">
    <template slot="body">
      <div class="rs-flex-1 rr-overflow-hidden">
        <Table border ref="selection" :datas="SELFDTS" checkbox>
          <TableItem title="#" prop="$serial" align="center" :width="80"></TableItem>
          <TableItem title="接口编码" prop="APICODE" :width="150"></TableItem>
          <TableItem title="接口名称" prop="APINAME" :width="150"></TableItem>
          <TableItem title="事件码" prop="ACTIONCODE" align="center" :width="150"></TableItem>
          <TableItem title="接口类型" prop="APITYPE" align="center" :width="150"></TableItem>
        </Table>
      </div>
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
  name: 'apiSel',
  props: {
    showType: {
      type: [String, Number],
    },
    item: { Type: Object },
  },
  data() {
    return {
      param: {},
    };
  },
  methods: {
    selectMethod() {
      this.$callAction({ action: `${Constants.STORE_NAME}/querySelsDts`, param: {}, isBusy: false });
    },
    close() {
      this.$parent.setvalue(false);
    },
    ok() {
      this.$emit('on-select', this.$refs.selection.getSelection());
      this.$parent.setvalue(false);
    },
  },
  computed: {
    ...mapDateTable('SELFDTS', []),
  },
  created() {
    this.selectMethod();
  },
};
</script>
<style scoped>
.maxModalH {
  height: calc(100vh - 185px);
  overflow: auto;
}
</style>

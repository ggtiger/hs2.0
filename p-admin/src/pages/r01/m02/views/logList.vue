<template>
  <view-dialog title="原始记录变更记录" @on-show="onShow" style="width:900px;" :loading="loading">
    <div slot="body">
      <div class="rr-flex-col">
        <div class="rs-flex-1 rr-overflow-hidden">
          <rs-table-list :datas="OPLOGS" class="maxModalH" :path="$OPLOGS" border ref="selection"></rs-table-list>
        </div>
      </div>
    </div>
    <template slot="footer">
      <Button class="ml5" color="primary" @click.native="ok">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 'logList',
  props: {
    showType: {
      type: [String, Number],
    },
    ID: { Type: String, default: '' },
  },
  data() {
    return {
      INPUT: '',
    };
  },
  computed: {
    ...mapDateTable('OPLOGS', []),
  },
  methods: {
    async queryLog(I) {
      await this.$callAction({ action: `${Constants.STORE_NAME}/queryLog`,
        param: {
          ID: this.ID,
        },
        isBusy: false });
    },
    ok() {
      this.$parent.setvalue(false);
    },
    onShow() {
      this.queryLog();
    },
  },
  mounted() {},
};
</script>
<style scoped>
.maxModalH {
  height: calc(100vh - 165px);
  overflow: hidden;
}
</style>

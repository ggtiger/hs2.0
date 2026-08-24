<template>
  <view-dialog title="设置部门" @on-show="onShow">
    <template slot="body">
      <Transfer v-model="DTSBDATA" :datas="DEPT" keyName="ID">
        <template slot="sourceHeader">
          <div class="h-transfer-header">部门</div>
        </template>
        <template slot="targetHeader">
          <div class="h-transfer-header">已设置</div>
        </template>
        <template slot-scope="{option}" slot="item">{{option.DEPTNAME}}</template>
      </Transfer>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Button class="ml5" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Gen from '@/utils/gen';
export default {
  name: 'dept-set',
  props: {
    params: { Type: Object },
  },
  components: {},
  data() {
    return {
      DTSBDATA: [],
    };
  },
  computed: {
    ...mapDateTable('DEPT', []),
    ...mapDateTable('DTSB', []),
  },
  mounted() {},
  methods: {
    closeW() {
      this.$parent.setvalue(false);
    },
    save() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/saveDept`,
        param: {},
        successText: '保存成功',
        isSuccessBack: true,
      });
    },
    async onShow() {
      this.$callAction({ action: `${Constants.STORE_NAME}/openDeptSel`, param: { ID: this.params.ID }, isBusy: false });
      await this.$callAction({ action: `${Constants.STORE_NAME}/openDtsB`, param: { ID: this.params.ID }, isBusy: false });
      this.DTSBDATA = this.DTSB.map(item => item.DEPTID);
    },
  },
  async mounted() {},
  watch: {
    DTSBDATA: {
      handler(v) {
        this.$store.commit(`${Constants.STORE_NAME}/SET_DTSB`, { data: v, USERID: this.params.ID });
      },
    },
  },
};
</script>

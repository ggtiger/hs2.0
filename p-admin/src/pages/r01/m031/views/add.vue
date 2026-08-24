<template>
  <view-dialog :title="title"  >
    <template slot="body">
      <ToolBar label="基本信息" :size="16"></ToolBar>
      <rs-form-edit
        ref="form"
        class="rs-flex-col"
        :label-width="100"
        mode="twocolumn"
        :path="$MAIN"
      ></rs-form-edit>
    </template>
    <template slot="footer">
      <Button class="ml5" v-per="'LI_M031/A13'" v-if="!this.CHARGEID" color="primary" @click.native="save">修改</Button>
      <Button class="ml5" v-per="'LI_M031/A13'" color="primary" @click.native="mySave">收费</Button>
    </template>
  </view-dialog>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import RsUploader from '@/components/rs-uploader';
import Add01 from '@/mixins/add01';
export default {
  name: 'r01-m031-add',
  data() {
    return {
      options: {
        max_file_size: '1mb',
      },
      file: null,
    };
  },
  mixins: [Add01],
  computed: {
    ...mapDateTable('MAIN', ['DEPTID', 'DEPTNAME', 'CAMT', 'CNT', 'BAMT', 'OAMT', 'AMT', 'DISCOUNT', 'CHARGEID']),
  },
  components: { RsUploader },
  watch: {
    OAMT() {
      this.AMT = parseFloat((1 * this.CNT * this.CAMT) * this.DISCOUNT + 1 * this.OAMT + 1 * this.BAMT, 2);
    },
    BAMT() {
      this.AMT = parseFloat((1 * this.CNT * this.CAMT) * this.DISCOUNT + 1 * this.OAMT + 1 * this.BAMT, 2);
    },
    DISCOUNT() {
      this.AMT = parseFloat((1 * this.CNT * this.CAMT) * this.DISCOUNT + 1 * this.OAMT + 1 * this.BAMT, 2);
    }
  },
  methods: {
    mySave() {
      debugger;
      this.$store.commit(`${Constants.STORE_NAME}/SET_CHARGEDATA`, { userInfo: this.$store.state.user.userInfo });
      this.save();
    },
  },
};
</script>

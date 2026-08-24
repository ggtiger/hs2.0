<template>
  <view-dialog title="详细公告" style="width:1000px;" @on-show="onShow">
    <template slot="body" v-padding="40">
      <div class="title">
        <h3 class="rr-text-center">{{NOTITLE}}</h3>
      </div>
      <div class="w-e-text" v-html="NOCONTENT"></div>
      <div class="rr-text-right">发文日期：{{BILLDATE}}</div>
      <div class="shenpi">
        <Row :space="9">
          <Cell width="8">
            申请人：{{SUBMITER}}
            <br />
            申请时间：{{SUMBMITTIME}}
          </Cell>
          <Cell width="8">
            审核人：{{CHECKER}}
            <br />
            审核时间：{{CHECKTIME}}
          </Cell>
          <Cell width="8">
            审批人：{{VERIFIER}}
            <br />
            审批时间：{{VERIFYTIME}}
          </Cell>
        </Row>
      </div>
      <div class="fujianList">
        <div>附件:</div>
        <RsUploader :options="options" type="files" data-type="file" v-model="FILES" readonly></RsUploader>
      </div>
    </template>
  </view-dialog>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import RsUploader from '@/components/rs-uploader';
export default {
  name: 'gonggaoDetail',
  props: {
    ID: {
      Type: String,
    },
  },
  data() {
    return {};
  },
  components: { RsUploader },
  computed: {
    ...mapDateTable('MAINNOTICE', [
      'NOTITLE',
      'BILLDATE',
      'NOCONTENT',
      'SUBMITER',
      'SUMBMITTIME',
      'CHECKER',
      'CHECKTIME',
      'VERIFIER',
      'VERIFYTIME',
    ]),
    ...mapDateTable('DTSNOTICE', []),
    FILES: {
      get() {
        let dts = [...this.DTSNOTICE];
        dts.map(d => {
          d.id = d.FILEID;
          d.name = d.FILENAME;
        });
        return dts;
      },
    },
  },
  methods: {
    close() {
      this.$emit('close');
    },
    onShow() {
      if (this.ID) {
        this.$callAction({ action: 'c02/openNotice', param: { ID: this.ID }, isBusy: false });
      }
    },
  },
};
</script>
<style lang="less" scoped>
.title {
  margin: 10px;
  h3 {
    font-size: 24px;
  }
}
.body {
  margin: 1em 0;
  min-height: 48px;
}
.shenpi {
  margin: 3em 0;
}
.fujianList {
  .list-item {
    margin: 1em 0 0 0;
  }
}
</style>

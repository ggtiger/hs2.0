<template>
  <view-dialog :title="title"  style="width:960px;">
    <template slot="body">
      <ToolBar label="基本信息" :size="16"></ToolBar>
      <rs-form-edit
        ref="form"
        class="rs-flex-col"
        :label-width="100"
        mode="twocolumn"
        :path="$MAIN"
        :disabled="!ISSHOWSAVE"
      >
        <template slot="FILES">
          <RsUploader
            :options="options"
            type="files"
            data-type="file"
            v-model="FILES"
            :readonly="!ISSHOWSAVE"
          ></RsUploader>
        </template>
      </rs-form-edit>
    </template>
    <template slot="footer">
      <Button
        class="ml5"
        v-per="'RS_M08/A04'"
        v-if="ISSHOWSAVE"
        color="primary"
        @click.native="save"
      >暂存</Button>
      <Poptip content="确定删除？" v-per="'RS_M08/A07'" v-if="ISSHOWDELETE" @confirm="del"><Button
        class="ml5"
        color="red"
      >删除</Button></Poptip>
      <Button
        class="ml5"
        v-per="'RS_M08/A08'"
        v-if="ISSHOWSUBMIT"
        color="primary"
        @click.native="submit(ID)"
      >提交</Button>
      <Poptip content="确定撤销提交？" v-per="'RS_M08/A09'" v-if="ISSHOWRESUBMIT" @confirm="reSubmit(ID)"><Button
        class="ml5"
        color="red"
      >撤销提交</Button></Poptip>
      <Button
        class="ml5"
        v-per="'RS_M08/A10'"
        v-if="ISSHOWCHECK"
        color="primary"
        @click.native="check(ID)"
      >审核</Button>
      <Poptip content="确定撤销审核？" v-per="'RS_M08/A11'" v-if="ISSHOWRECHECK" @confirm="reCheck(ID)"><Button
        class="ml5"
        color="red"
      >撤销审核</Button></Poptip>
      <Button
        class="ml5"
        v-per="'RS_M08/A12'"
        v-if="ISSHOWVERIFY"
        color="primary"
        @click.native="verify(ID)"
      >审批</Button>
      <Poptip content="确定撤销审批？" v-per="'RS_M08/A13'" v-if="ISSHOWREVERIFY" @confirm="reVerify(ID)"><Button
        class="ml5"
        color="red"
      >撤销审批</Button></Poptip>
    </template>
  </view-dialog>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import RsUploader from '@/components/rs-uploader';
import Add01 from '@/mixins/add01';
import WangEditor from 'wangeditor';
export default {
  name: 's01-m05-add',
  data() {
    return {
      options: {
        max_file_size: '20mb',
      },
      file: null,
      param: {
        loadData: this.remoteMethod2,
        keyName: 'ID',
        titleName: 'TPMNAME',
      },
      param4: {
        loadData: this.tstddSel,
        keyName: 'ID',
        titleName: 'STDDNAME',
      },
      param5: {
        loadData: this.reguitemSel,
        keyName: 'ID',
        titleName: 'ITEMNAME',
      },
      isAutoRefresh: true,
    };
  },
  mixins: [Add01],
  computed: {
    ...mapDateTable('MAIN', ['FILES', 'STATE']),
    ...mapDateTable('DTS', []),
    FILES: {
      get() {
        let dts = [...this.DTS];
        dts.map(d => {
          d.id = d.FILEID;
          d.name = d.FILENAME;
        });
        return dts;
      },
      set(files) {
        files = files || [];
        this.$store.commit(`${Constants.STORE_NAME}/SETFILEDATA`, { files });
      },
    },
  },
  components: { RsUploader, WangEditor },
  methods: {
    async remoteMethod2(INPUT, callback) {
      await this.$callAction({ action: `${Constants.STORE_NAME}/querySel`,
        param: {
          INPUT,
        },
        isBusy: false });
      callback(this.SEL);
    },
    async tstddSel(INPUT, callback) {
      if (this.TSTANDARDNAME === INPUT) {
        INPUT = '';
      }
      await this.$callAction({ action: `${Constants.STORE_NAME}/tstddSel`,
        param: {
          INPUT,
        },
        isBusy: false });
      callback(this.TSTDD);
    },
    async reguitemSel(INPUT, callback) {
      if (this.REGUITEMNAME === INPUT) {
        INPUT = '';
      }
      await this.$callAction({ action: `${Constants.STORE_NAME}/reguitemSel`,
        param: {
          INPUT,
        },
        isBusy: false });
      callback(this.REGUITEM);
    },
  },
};
</script>

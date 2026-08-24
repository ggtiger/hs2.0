<template>
  <view-dialog :title="title"  class="d-width" :loading="loading">
    <template slot="body">
      <ToolBar label="基本信息" :size="16"></ToolBar>
      <rs-form-edit
        ref="form"
        class="rs-flex-col"
        :label-width="100"
        mode="twocolumn"
        :path="$MAIN"
      >
        <template slot="AEMPNAME">
          <AutoComplete :option="param4" v-model="TEMP" type="object">
            <template slot="item" slot-scope="{item}">
              <div>{{item.value.EMPNAME}}</div>
            </template>
          </AutoComplete>
        </template>
         <template slot="FILES">
          <RsUploader :options="options" type="files" data-type="file" v-model="FILES"></RsUploader>
        </template>
        <template slot="CUSTNAME">
          <AutoComplete :option="param" v-model="TCUST" type="object">
            <template slot="item" slot-scope="{item}">
              <div>{{item.value.CUSTNAME}}</div>
            </template>
          </AutoComplete>
        </template>
        <template slot="PTEMPLATENAME">
          <AutoComplete :option="param2" v-model="TPTMP" type="object">
            <template slot="item" slot-scope="{item}">
              <div>{{item.value.DOCTITLE}}</div>
            </template>
          </AutoComplete>
        </template>
        <template slot="ADEPTNAME">
          <AutoComplete :option="param3" v-model="TDEPT" type="object">
            <template slot="item" slot-scope="{item}">
              <div>{{item.value.DEPTNAME}}</div>
            </template>
          </AutoComplete>
        </template>
      </rs-form-edit>
    </template>
    <template slot="footer">
      <Button
        class="ml5"
        v-per="'LI_M00/A04'"
        v-if="ISSHOWSAVE"
        color="primary"
        @click.native="save"
      >暂存</Button>
      <Poptip content="确定删除？" v-per="'LI_M00/A07'" v-if="ISSHOWDELETE" @confirm="del">
        <Button class="ml5" color="red">删除</Button>
      </Poptip>
      <Button
        class="ml5"
        v-per="'LI_M00/A08'"
        v-if="ISSHOWSUBMIT"
        color="primary"
        @click.native="submit(ID)"
      >提交</Button>
      <Poptip content="确定撤销提交？" v-per="'LI_M00/A09'" v-if="ISSHOWRESUBMIT" @confirm="reSubmit(ID)">
        <Button class="ml5" color="red">撤销提交</Button>
      </Poptip>
    </template>
  </view-dialog>
</template>


<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import RsUploader from '@/components/rs-uploader';
import Add01 from '@/mixins/add01';
export default {
  name: 's01-m05-add',
  data() {
    return {
      options: {
        max_file_size: '1mb',
      },
      file: null,
      param: {
        loadData: this.custSel,
        keyName: 'ID',
        titleName: 'CUSTNAME',
      },
      param4: {
        loadData: this.empSel,
        keyName: 'ID',
        titleName: 'EMPNAME',
      },
      param2: {
        loadData: this.ptmpSel,
        keyName: 'ID',
        titleName: 'DOCTITLE',
      },
      param3: {
        loadData: this.deptSel,
        keyName: 'ID',
        titleName: 'DEPTNAME',
      },
    };
  },
  props: {
    item: {
      Type: Object,
      default: {},
    },
  },
  mixins: [Add01],
  computed: {
    ...mapDateTable('MAIN', [
      'STATE',
      'AEMPID',
      'AEMPNAME',
      'CUSTID',
      'CUSTNAME',
      'LINKER',
      'WCUSTNAME',
      'SLINKER',
      'MOBILE',
      'ADDR',
      'CAMT',
      'PTEMPLATEID',
      'PTEMPLATENAME',
      'ADEPTID',
      'ADEPTNAME',
      'SENDNAME',
      'WTCODE',
      'FILES',
      'CNT',
      'CAMT',
      'OAMT',
      'BAMT',
      'PTEMPLATECAMT',
    ]),
    ...mapDateTable('EMP', []),
    ...mapDateTable('DEPT', []),
    ...mapDateTable('CUST', []),
    ...mapDateTable('PTMP', []),
    ...mapDateTable('DTS', []),
    TEMP: {
      get() {
        return { ID: this.AEMPID, EMPNAME: this.AEMPNAME };
      },
      set(v) {
        v = v || {};
        this.AEMPID = v.ID;
        this.AEMPNAME = v.EMPNAME;
      },
    },
    TCUST: {
      get() {
        if (!this.CUSTID) {
          return null;
        }
        return { ID: this.CUSTID, CUSTNAME: this.CUSTNAME };
      },
      set(v) {
        v = v || {};
        this.CUSTID = v.ID;
        this.CUSTNAME = v.CUSTNAME;
        this.LINKER = v.LINKER;
        this.SENDNAME = v.LINKER;
        this.WCUSTNAME = v.CUSTNAME;
        this.SLINKER = v.MOBILE;
        this.ADDR = v.ADDR;
        this.MOBILE = v.MOBILE;
      },
    },
    TPTMP: {
      get() {
        if (!this.PTEMPLATEID) {
          return null;
        }
        return { ID: this.PTEMPLATEID, DOCTITLE: this.PTEMPLATENAME };
      },
      set(v) {
        v = v || {};
        this.PTEMPLATEID = v.ID;
        this.PTEMPLATENAME = v.DOCTITLE;
        // 优先从 TSS_PROJECT_FEE 查询项目费用，降级用模板 CAMT
        this.loadProjectFee(v.ID, v.CAMT);
      },
    },
    ISSHOWRESUBMIT(state, getters, rootState, rootGetters) {
      // 主键、待审核
      return this.ID && (this.STATE === 8 || this.STATE === 7);
    },
    disabled(state, getters, rootState, rootGetters) {
      return !(!this.STATE || this.STATE === 1);
    },
    TDEPT: {
      get() {
        if (!this.ADEPTID) {
          return null;
        }
        return { ID: this.ADEPTID, DEPTNAME: this.ADEPTNAME };
      },
      set(v) {
        v = v || {};
        this.ADEPTID = v.ID;
        this.ADEPTNAME = v.DEPTNAME;
        this.TPTMP = {};
      },
    },
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
  components: { RsUploader },
  watch:{
    CNT(){
      this.CAMT = this.PTEMPLATECAMT*this.CNT+this.OAMT*1+this.BAMT*1;
    },
    OAMT(){
      this.CAMT = this.PTEMPLATECAMT*this.CNT+this.OAMT*1+this.BAMT*1;
    },
    BAMT(){
      this.CAMT = this.PTEMPLATECAMT*this.CNT+this.OAMT*1+this.BAMT*1;
    },
    PTEMPLATECAMT(){
      this.CAMT = this.PTEMPLATECAMT*this.CNT+this.OAMT*1+this.BAMT*1;
    }
  },
  methods: {
    // 选择模板后，从 TSS_PROJECT_FEE 查询项目费用自动填充
    async loadProjectFee(templateId, fallbackCAMT) {
      if (!templateId) {
        this.PTEMPLATECAMT = fallbackCAMT || 0;
        return;
      }
      try {
        let row = await this.$callAction({
          action: Constants.STORE_NAME + '/loadProjectFee',
          param: { templateId },
          isBusy: false,
        });
        if (row) {
          // 有项目费用配置，优先使用
          this.PTEMPLATECAMT = row.CAMT || 0;
          this.OAMT = row.OAMT || 0;
          this.BAMT = row.BAMT || 0;
        } else {
          // 无项目费用配置，降级用模板的 CAMT
          this.PTEMPLATECAMT = fallbackCAMT || 0;
          this.BAMT = 0;
        }
      } catch (e) {
        // 查询失败，降级用模板 CAMT
        this.PTEMPLATECAMT = fallbackCAMT || 0;
        this.BAMT = 0;
      }
    },
    async empSel(INPUT, callback) {
      if (this.TSTANDARDNAME === INPUT) {
        INPUT = '';
      }
      await this.$callAction({
        action: `${Constants.STORE_NAME}/empSel`,
        param: { INPUT },
        isBusy: false,
      });
      callback(this.EMP);
    },
    async custSel(INPUT, callback) {
      if (this.CUSTNAME === INPUT) {
        INPUT = '';
      }
      await this.$callAction({
        action: `${Constants.STORE_NAME}/custSel`,
        param: { INPUT },
        isBusy: false,
      });
      callback(this.CUST);
    },
    async ptmpSel(INPUT, callback) {
      if (this.PTEMPLATENAME === INPUT) {
        INPUT = '';
      }
      await this.$callAction({
        action: `${Constants.STORE_NAME}/ptmpSel`,
        param: { INPUT, DEPTID: this.ADEPTID },
        isBusy: false,
      });
      callback(this.PTMP);
    },
    async deptSel(INPUT, callback) {
      if (this.DEPTNAME === INPUT) {
        INPUT = '';
      }
      await this.$callAction({
        action: `${Constants.STORE_NAME}/deptSel`,
        param: { INPUT },
        isBusy: false,
      });
      callback(this.DEPT);
    },
    async onShow() {
      this.loading = true;
      try {
        if (this.ID) {
          await this.$callAction({
            action: `${this.storeName}/open`,
            param: { ID: this.ID },
            isBusy: false,
          });
        } else {
          await this.$callAction({
            action: `${this.storeName}/add`,
            param: { item: this.item },
            isBusy: false,
          });
          if (!this.WTCODE) {
            this.WTCODE = await this.$callAction({
              action: `${this.storeName}/getBillCode2`,
              param: { TCODE: 'WT|%Y%m%d|' },
              isBusy: false,
            });
          }
        }
      } finally {
        this.loading = false;
      }
    },
  },
};
</script>

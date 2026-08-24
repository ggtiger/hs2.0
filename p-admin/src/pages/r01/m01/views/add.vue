<template>
  <view-dialog :title="title"  class="d-width">
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
        <template slot="EXPTEMPFILENAME">
          <rs-uploader-template
            :options="options"
            v-model="EXPTEMPFILES"
            template-type="YSJL"
            :readonly="!ISSHOWSAVE"
          ></rs-uploader-template>
        </template>
        <template slot="CERTEMPFILENAME">
          <rs-uploader-template
            :options="options"
            v-model="CERTEMPFILES"
            template-type="ZS"
            :readonly="!ISSHOWSAVE"
          ></rs-uploader-template>
        </template>

        <template slot="DEPTNAME">
          <AutoComplete :option="param2" v-model="TDEPT" type="object" :disabled="!ISSHOWSAVE">
            <template slot="item" slot-scope="{item}">
              <div>{{item.value.DEPTNAME}}</div>
            </template>
          </AutoComplete>
        </template>
        <template slot="TSTANDARDNAME">
          <AutoComplete :option="param4" v-model="TTSTDD" type="object" :disabled="!ISSHOWSAVE">
            <template slot="item" slot-scope="{item}">
              <div>{{item.value.STDDNAME}}</div>
            </template>
          </AutoComplete>
        </template>
        <template slot="REGUITEMNAME">
          <AutoComplete :option="param5" v-model="TREGUITEM" type="object" :disabled="!ISSHOWSAVE">
            <template slot="item" slot-scope="{item}">
              <div>{{item.value.ITEMNAME}}</div>
            </template>
          </AutoComplete>
        </template>
        <template slot="TPMNAME">
          <AutoComplete :option="param" v-model="TPM" type="object" :disabled="!ISSHOWSAVE">
            <template slot="item" slot-scope="{item}">
              <div>{{item.value.TPMNAME}}</div>
            </template>
          </AutoComplete>
        </template>
      </rs-form-edit>
    </template>
    <template slot="footer">
      <Button
        class="ml5"
        v-per="'LI_M01/A03'"
        v-if="ISSHOWSAVE"
        color="primary"
        @click.native="save"
      >暂存</Button>
      <Poptip content="确定删除？" v-if="ISSHOWDELETE" @confirm="del">
        <Button class="ml5" color="red">删除</Button>
      </Poptip>
      <Button
        class="ml5"
        v-per="'LI_M01/A08'"
        v-if="ISSHOWSUBMIT"
        color="primary"
        @click.native="submit(ID)"
      >提交</Button>
      <Poptip content="确定撤销提交？" v-per="'LI_M01/A09'" v-if="ISSHOWRESUBMIT" @confirm="reSubmit(ID)">
        <Button
          class="ml5"
          color="red"
        >撤销提交</Button>
      </Poptip>
      <Button
        class="ml5"
        v-per="'LI_M01/A10'"
        v-if="ISSHOWCHECK"
        color="primary"
        @click.native="check(ID)"
      >审核</Button>
      <Poptip content="确定撤销审核？" v-per="'LI_M01/A11'" v-if="ISSHOWRECHECK" @confirm="reCheck(ID)">
        <Button
          class="ml5"
          color="red"
        >撤销审核</Button>
      </Poptip>
    </template>
  </view-dialog>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
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
        loadData: this.remoteMethod2,
        keyName: 'ID',
        titleName: 'TPMNAME',
      },
      param2: {
        loadData: this.deptSel,
        keyName: 'ID',
        titleName: 'DEPTNAME',
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
    };
  },
  mixins: [Add01],
  computed: {
    ...mapDateTable('MAIN', [
      'DOCCODE',
      'REFTPMID',
      'TPMNAME',
      'CERTCODE',
      'DOCTITLE',
      'EXPDATE',
      'ISUSE',
      'CERTEMP',
      'CERTEMPFILENAME',
      'EXPTEMP',
      'EXPTEMPFILENAME',
      'STATE',
      'TSTANDARDID',
      'TSTANDARDNAME',
      'REGUITEMID',
      'REGUITEMCODE',
      'REGUITEMNAME',
      'DEPTID',
      'DEPTNAME',
    ]),
    ...mapDateTable('SEL', []),
    ...mapDateTable('TSTDD', []),
    ...mapDateTable('REGUITEM', []),
    ...mapDateTable('DEPT', []),
    TPM: {
      get() {
        return { ID: this.REFTPMID, TPMNAME: this.TPMNAME };
      },
      set(v) {
        this.REFTPMID = v.ID;
        this.TPMNAME = v.TPMNAME;
      },
    },
    CERTEMPFILES: {
      get() {
        if (this.CERTEMP) return { id: this.CERTEMP, name: this.CERTEMPFILENAME };
        else return null;
      },
      set({ id, name }) {
        this.CERTEMP = id;
        this.CERTEMPFILENAME = name;
      },
    },
    EXPTEMPFILES: {
      get() {
        if (this.EXPTEMP) return { id: this.EXPTEMP, name: this.EXPTEMPFILENAME };
        else return null;
      },
      set({ id, name }) {
        this.EXPTEMP = id;
        this.EXPTEMPFILENAME = name;
      },
    },
    TTSTDD: {
      get() {
        if (!this.TSTANDARDID) {
          return null;
        }
        return { ID: this.TSTANDARDID, STDDNAME: this.TSTANDARDNAME };
      },
      set(v) {
        v = v || {};
        this.TSTANDARDID = v.ID;
        this.TSTANDARDNAME = v.STDDNAME;
      },
    },
    TREGUITEM: {
      get() {
        if (!this.REGUITEMID) {
          return null;
        }
        return { ID: this.REGUITEMID, ITEMCODE: this.REGUITEMCODE, ITEMNAME: this.REGUITEMNAME };
      },
      set(v) {
        v = v || {};
        this.REGUITEMID = v.ID;
        this.REGUITEMCODE = v.ITEMCODE;
        this.REGUITEMNAME = v.ITEMNAME;
      },
    },
    TDEPT: {
      get() {
        if (!this.DEPTID) {
          return null;
        }
        return { ID: this.DEPTID, DEPTNAME: this.DEPTNAME };
      },
      set(v) {
        v = v || {};
        this.DEPTID = v.ID;
        this.DEPTNAME = v.DEPTNAME;
      },
    },
  },
  components: {},
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
    async deptSel(INPUT, callback) {
      if (this.DEPTNAME === INPUT) {
        INPUT = '';
      }
      await this.$callAction({ action: `${Constants.STORE_NAME}/deptSel`,
        param: {
          INPUT,
        },
        isBusy: false });
      callback(this.DEPT);
    },
  },
};
</script>

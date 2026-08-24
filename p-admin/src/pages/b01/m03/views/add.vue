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
      >
        <template slot="DEPTNAME">
          <AutoComplete :option="param2" v-model="TDEPT" type="object">
            <template slot="item" slot-scope="{item}">
              <div>{{item.value.DEPTNAME}}</div>
            </template>
          </AutoComplete>
        </template>
        <template slot="GEMPNAME">
          <AutoComplete :option="param3" v-model="TEMP" type="object">
            <template slot="item" slot-scope="{item}">
              <div>{{item.value.EMPNAME}}</div>
            </template>
          </AutoComplete>
        </template>
        <template slot="FILES">
          <RsUploader :options="options" type="files" data-type="file" v-model="FILES"></RsUploader>
        </template>
        <template slot="TSTANDARDNAME">
          <AutoComplete :option="param4" v-model="TTSTDD" type="object">
            <template slot="item" slot-scope="{item}">
              <div>{{item.value.STDDNAME}}</div>
            </template>
          </AutoComplete>
        </template>
      </rs-form-edit>
      </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'LIB_M03/A07'" v-if="ID" @confirm="del">
        <Button class="ml5" color="red">删除</Button>
      </Poptip>
      <Button class="ml5" v-per="'LIB_M03/A04'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Add01 from '@/mixins/add01';
import RsUploader from '@/components/rs-uploader';
export default {
  name: 'b01-m05-add',
  mixins: [Add01],
  components: { RsUploader },
  data() {
    return {
      options: {
        max_file_size: '20mb',
      },
      param: {
        keyName: 'ID',
        parentName: 'UPDEPTID',
        titleName: 'DEPTNAME',
        dataMode: 'list',
        getTotalDatas: this.updeptSel,
      },
      param2: {
        loadData: this.deptSel,
        keyName: 'ID',
        titleName: 'DEPTNAME',
      },
      param3: {
        loadData: this.empSel,
        keyName: 'ID',
        titleName: 'EMPNAME',
      },
      param4: {
        loadData: this.tstddSel,
        keyName: 'ID',
        titleName: 'STDDNAME',
      },
      options: {
        max_file_size: '1mb',
      },

    };
  },
  computed: {
    ...mapDateTable('MAIN', [
      'DEPTID',
      'DEPTNAME',
      'GEMPID',
      'GEMPNAME',
      'CERTID',
      'TSTANDARDID',
      'TSTANDARDNAME',
      'FILES',
    ]),
    ...mapDateTable('DEPT', []),
    ...mapDateTable('EMP', []),
    ...mapDateTable('TSTDD', []),
    ...mapDateTable('DTS', []),
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
    TEMP: {
      get() {
        if (!this.GEMPID) {
          return null;
        }
        return { ID: this.GEMPID, EMPNAME: this.GEMPNAME };
      },
      set(v) {
        v = v || {};
        this.GEMPID = v.ID;
        this.GEMPNAME = v.EMPNAME;
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
    TCERTFILENAME: {
      get() {
        if (this.CERTID) {
          return {
            id: this.CERTID,
            name: this.CERTFILENAME,
          };
        } else return null;
      },
      set({ id, name }) {
        this.CERTID = id;
        this.CERTFILENAME = name;
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
  methods: {
    async empSel(INPUT, callback) {
      if (this.CEMPNAME === INPUT) {
        INPUT = '';
      }
      await this.$callAction({ action: `${Constants.STORE_NAME}/empSel`, param: { INPUT }, isBusy: false });
      callback(this.EMP);
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
  },
};
</script>

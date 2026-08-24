<template>
  <view-dialog :title="title" class="d-width">
    <template slot="body">
      <Tabs :datas="tabDatas" class-name="h-tabs-card" v-model="selectedTab"></Tabs>
      <div v-show="selectedTab === 'base'">
        <ToolBar label="基本信息" :size="16"></ToolBar>
        <rs-form-edit
          ref="form"
          class="rs-flex-col"
          :label-width="80"
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
          <template slot="FILENAME">
            <RsUploader :options="options" type="image" data-type="file" v-model="FILENAMES"></RsUploader>
          </template>
        </rs-form-edit>
      </div>
      <div v-show="selectedTab === 'cert'" class="rs-flex-col" style="min-height:200px;">
        <rs-table-edit border ref="DTSA" :path="$DTSA" :datas="DTSA"></rs-table-edit>
      </div>
      <div v-show="selectedTab === 'auth'" class="rs-flex-col" style="min-height:200px;">
        <rs-table-edit border ref="DTSB" :path="$DTSB" :datas="DTSB"></rs-table-edit>
      </div>
      <div v-show="selectedTab === 'train'" class="rs-flex-col" style="min-height:200px;">
        <rs-table-edit border ref="DTSC" :path="$DTSC" :datas="DTSC" :readonly="true"></rs-table-edit>
      </div>
      <div v-show="selectedTab === 'comp'" class="rs-flex-col" style="min-height:200px;">
        <rs-table-edit border ref="DTSD" :path="$DTSD" :datas="DTSD"></rs-table-edit>
      </div>
      <div v-show="selectedTab === 'super'" class="rs-flex-col" style="min-height:200px;">
        <rs-table-edit border ref="DTSE" :path="$DTSE" :datas="DTSE"></rs-table-edit>
      </div>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'LIB_M06/A07'" v-if="ID" @confirm="del">
        <Button class="ml5" color="red">删除</Button>
      </Poptip>
      <Button class="ml5" v-per="'LIB_M06/A04'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Add01 from '@/mixins/add01';
import RsUploader from '@/components/rs-uploader';
export default {
  name: 'b01-m06-add',
  mixins: [Add01],
  components: { RsUploader },
  data() {
    return {
      selectedTab: 'base',
      tabDatas: {
        base: '基本信息',
        cert: '资质证书',
        auth: '人员授权',
        train: '培训记录',
        comp: '能力确认',
        super: '人员监督'
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
      options: {
        max_file_size: '1mb',
      },
    };
  },
  computed: {
    ...mapDateTable('MAIN', ['DEPTID', 'DEPTNAME', 'ESIGNID', 'FILENAME']),
    ...mapDateTable('UPDEPT', []),
    ...mapDateTable('DEPT', []),
    ...mapDateTable('DTSA', []),
    ...mapDateTable('DTSB', []),
    ...mapDateTable('DTSC', []),
    ...mapDateTable('DTSD', []),
    ...mapDateTable('DTSE', []),
    TDEPT: {
      get() {
        if (!this.DEPTID) {
          return null;
        }
        return { ID: this.DEPTID, DEPTNAME: this.DEPTNAME };
      },
      set(v) {
        this.DEPTID = v.ID;
        this.DEPTNAME = v.DEPTNAME;
      },
    },
    FILENAMES: {
      get() {
        if (this.ESIGNID) {
          return {
            id: this.ESIGNID,
            name: this.FILENAME,
          };
        } else return null;
      },
      set({ id, name }) {
        this.ESIGNID = id;
        this.FILENAME = name;
      },
    },
  },
  methods: {
    addDts(path) {
      this.$callAction({ action: `${Constants.STORE_NAME}/addDts`, param: { path }, isBusy: false });
    },
    removeDts(path, ref) {
      if (ref && ref.getSelected()) {
        this.$callAction({ action: `${Constants.STORE_NAME}/removeDts`, param: { path, rows: ref.getSelected() }, isBusy: false });
      }
    },
    async updeptSel(callback) {
      await this.$callAction({ action: `${Constants.STORE_NAME}/updeptSel`, param: { ID: this.ID }, isBusy: false });
      callback(this.UPDEPT);
    },
    async deptSel(INPUT, callback) {
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

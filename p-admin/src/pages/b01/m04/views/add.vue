<template>
  <view-dialog :title="title" class="d-width">
    <template slot="body">
      <Tabs :datas="param5" class-name="h-tabs-card" v-model="selected"></Tabs>
      <div v-show="this.selected === 'module2'"  class="rs-flex-col">
      <rs-modal ref="ard">
        <ard-sel @on-select="onSelectArd" ></ard-sel>
      </rs-modal>
          <div class="h-panel-body rr-flex-1 " >
              <ToolBar label="" :size="16">
          <div slot="right">
            <Button color="primary" icon="h-icon-plus" size="s" @click="addDts('DTSA')">选入</Button>
            <Button color="primary" icon="h-icon-minus" size="s" @click="removeDts('DTSA',$refs.DTSA)">移除</Button>
          </div>
        </ToolBar>
         <rs-table-edit
          border
          ref="DTSA"
          :path="$DTSA"
          :datas="DTSA"
        ></rs-table-edit>
          </div>
      </div>
      <div v-show="this.selected === 'module1'">
        <rs-form-edit ref="form" class="rs-flex-col" :label-width="100" mode="twocolumn" :path="$MAIN">
          <template slot="DEPTNAME">
            <AutoComplete :option="param2" v-model="TDEPT" type="object">
              <template slot="item" slot-scope="{ item }">
                <div>{{ item.value.DEPTNAME }}</div>
              </template>
            </AutoComplete>
          </template>
          <template slot="CEMPNAME">
            <AutoComplete :option="param3" v-model="TEMP" type="object">
              <template slot="item" slot-scope="{ item }">
                <div>{{ item.value.EMPNAME }}</div>
              </template>
            </AutoComplete>
          </template>
          <template slot="FILES">
            <RsUploader :options="options" type="files" data-type="file" v-model="FILES"></RsUploader>
          </template>
        </rs-form-edit>
      </div>
    </template>
    <template slot="footer">
        <Button class="ml5" @click.native="closeW">取消</Button>
        <Button class="ml5" v-per="'LIB_M04/A04'" v-if="ISSHOWSAVE" color="primary" @click.native="save">暂存</Button>
        <Poptip content="确定删除？" v-per="'LIB_M04/A07'" v-if="ISSHOWDELETE" @confirm="del">
          <Button class="ml5" color="red">删除</Button>
        </Poptip>
        <Button class="ml5" v-per="'LIB_M04/A08'" v-if="ISSHOWSUBMIT" color="primary" @click.native="submit(ID)"
          >提交</Button
        >
        <Poptip content="确定撤销提交？" v-per="'LIB_M04/A09'" v-if="ISSHOWRESUBMIT" @confirm="reSubmit(ID)">
          <Button class="ml5" color="red">撤销提交</Button>
        </Poptip>
        <Button class="ml5" v-per="'LIB_M04/A10'" v-if="ISSHOWCHECK" color="primary" @click.native="check(ID)"
          >审核</Button
        >
        <Poptip content="确定撤销审核？" v-per="'LIB_M04/A11'" v-if="ISSHOWRECHECK" @confirm="reCheck(ID)">
          <Button class="ml5" color="red">撤销审核</Button>
        </Poptip>
      </template>
  </view-dialog>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Add01 from '@/mixins/add01';
import RsUploader from '@/components/rs-uploader';
import ardSel from './ardSel';
export default {
  name: 'b01-m05-add',
  mixins: [Add01],
  components: { RsUploader, ardSel },
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
      options: {
        max_file_size: '1mb',
      },
      param5: {
        module1: '基本信息',
        module2: '标准器具',
      },
      selected: 'module1',
    };
  },
  computed: {
    ...mapDateTable('MAIN', ['DEPTID', 'STATE', 'DEPTNAME', 'CEMPID', 'CEMPNAME', 'CERTID', 'CERTFILENAME', 'FILES']),
    ...mapDateTable('DEPT', []),
    ...mapDateTable('EMP', []),
    ...mapDateTable('DTS', []),
    ...mapDateTable('DTSA', []),
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
        if (!this.CEMPID) {
          return null;
        }
        return { ID: this.CEMPID, EMPNAME: this.CEMPNAME };
      },
      set(v) {
        v = v || {};
        this.CEMPID = v.ID;
        this.CEMPNAME = v.EMPNAME;
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
        dts.map((d) => {
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
    addDts() {
      debugger;
      this.$refs.ard.show();
    },
    removeDts(path, table) {
      if (table.currentRow == -1) {
        return;
      }
      this.$store.commit(`${Constants.STORE_NAME}/DEL`, { path, item: table.currentRow });
    },
    onSelectArd(items) {
      let titems = this.DTSA || [];
      items = items.filter(item => {
        return !titems.find(i => {
          return i.ARDID == item.ID;
        });
      });
      this.$callAction({ action: `${Constants.STORE_NAME}/setDtsA`,
        param: {
          items,
        },
        isBusy: false });
    }
  },
};
</script>

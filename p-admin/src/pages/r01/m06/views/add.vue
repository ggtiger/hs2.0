<template>
  <view-dialog :title="title">
    <template slot="body">
      <ToolBar label="基本信息" :size="16"></ToolBar>
      <rs-form-edit
        ref="form"
        class="rs-flex-col"
        :label-width="100"
        mode="twocolumn"
        :path="$MAIN"
      >
        <template slot="CUSTNAME">
          <AutoComplete :option="this.custParam" v-model="TCUST" type="object">
            <template slot="item" slot-scope="{ item }">
              <div>{{ item.value.CUSTNAME }}</div>
            </template>
          </AutoComplete>
        </template>
        <template slot="DTS">
          <div class="rr-flex-1">
            <ToolBar label="委托项目" :size="16">
              <div slot="right">
                <label style="width: 60px">
                  <input
                    class="upload"
                    type="file"
                    @change="onChange"
                    accept=".csv, application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, application/vnd.ms-excel"
                  />
                  <Button color="primary" icon="h-icon-plus" size="s"> 导入 </Button>
                </label>
                <Button
                  color="primary"
                  icon="h-icon-plus"
                  size="s"
                  @click="addDts('DTS')"
                  >新增</Button
                >
                <Button
                  color="primary"
                  icon="h-icon-minus"
                  size="s"
                  @click="removeDts('DTS', $refs.DTS)"
                  >移除</Button
                >
              </div>
            </ToolBar>
            <rs-table-edit
              border
              ref="DTS"
              :path="$DTS"
              :datas="DTS"
              :getProps="getProps"
              @on-row-click="onRowClick"
            ></rs-table-edit>
          </div>
        </template>
      </rs-form-edit>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Button
        class="ml5"
        v-per="'LI_M06/A03'"
        v-if="ISSHOWSAVE"
        color="primary"
        @click.native="save"
        >暂存</Button
      >
      <Poptip
        content="确定删除？"
        v-per="'LIB_M04/A07'"
        v-if="ISSHOWDELETE"
        @confirm="del"
      >
        <Button class="ml5" color="red">删除</Button>
      </Poptip>
      <Button
        class="ml5"
        v-per="'LI_M06/A05'"
        v-if="ISSHOWSUBMIT"
        color="primary"
        @click.native="submit(ID)"
        >提交</Button
      >
      <Poptip
        content="确定撤销提交？"
        v-per="'LI_M06/A06'"
        v-if="ISSHOWRESUBMIT"
        @confirm="reSubmit(ID)"
      >
        <Button class="ml5" color="red">撤销提交</Button>
      </Poptip>
      <Button
        class="ml5"
        v-per="'LI_M06/A08'"
        v-if="ISSHOWCHECK"
        color="primary"
        @click.native="check()"
        >审核</Button
      >
      <Poptip
        content="确定撤销审核？"
        v-per="'LI_M06/A09'"
        v-if="ISSHOWRECHECK"
        @confirm="reCheck(ID)"
      >
        <Button class="ml5" color="red">撤销审核</Button>
      </Poptip>
    </template>
  </view-dialog>
</template>

<script>
import { mapDateTable, Constants } from '../store';
import RsUploader from '@/components/rs-uploader';
import Add01 from '@/mixins/add01';
import Sel01 from '@/mixins/sel01';
import { read as xlsxRead, utils as xlsxUtils } from 'xlsx';
export default {
  name: 'r01-m06-add',
  data() {
    return {
      options: {
        max_file_size: '1mb',
      },
      file: null,
      rowIndex: -1,
      currentRow: null,
      option: {
        onBeforeUpload(file) {
          if (file.size > 30 * 1024) {
            message.error(`${file.name} 文件大小超出30KB限制`);
            return false;
          }
          return true;
        },
        async onChange(file) {
          try {
            const url = await upload(file);
            return url;
          } catch (error) {
            message.error(`${file.name}上传失败`);
            throw new Error();
          }
        },
      },
    };
  },
  mixins: [Add01, Sel01],
  computed: {
    ...mapDateTable('MAIN', [
      'DEPTID',
      'DEPTNAME',
      'CUSTID',
      'CUSTNAME',
      'LINKER',
      'SLINKER',
      'ADDR',
      'MOBILE',
      'SIGNSTATE',
      'SIGNIMG',
      'STATE',
      'BILLDATE',
    ]),
    ...mapDateTable('DTS', []),
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
        this.EMAIL = v.EMAIL;
      },
    },
    ISSHOWSENDCUST(state, getters, rootState, rootGetters) {
      // 主键、待审核
      return this.ID && this.SIGNSTATE == '16' && this.STATE == '18';
    },
  },
  components: { RsUploader },
  methods: {
    onClick(info) {
      message.info('处理文件' + info.name);
    },
    async onChange(file) {
      console.log(xlsxRead);
      let dataBinary = await this.readFile(file.target.files[0]);
      let workBook = xlsxRead(dataBinary, { type: 'binary', cellDates: true });
      let workSheet = workBook.Sheets[workBook.SheetNames[0]];
      const data = xlsxUtils.sheet_to_json(workSheet);
      this.$store.commit(`${Constants.STORE_NAME}/IMPORT_DTS`, {
        items: data,
        columns: this.$refs.DTS.columns,
      });
    },
    readFile(file) {
      return new Promise((resolve) => {
        let reader = new FileReader();
        reader.readAsBinaryString(file);
        reader.onload = (ev) => {
          resolve(ev.target.result);
        };
      });
    },
    mySave() {
      this.$store.commit(`${Constants.STORE_NAME}/SET_CHARGEDATA`, {
        userInfo: this.$store.state.user.userInfo,
      });
      this.save();
    },
    getProps(key) {
      if (key === 'DEPTNAME') {
        return {
          cellProps: { option: this.deptParam },
        };
      } else if (key === 'AEMPNAME') {
        return {
          cellProps: { option: this.empParam },
        };
      } else if (key === 'PTEMPLATENAME') {
        return {
          cellProps: {
            option: {
              loadData: this.ptmpSel,
              keyName: 'ID',
              titleName: 'DOCTITLE',
            },
          },
        };
      } else {
        return {};
      }
    },
    async ptmpSel(INPUT, callback) {
      if (this.PTEMPLATENAME === INPUT) {
        INPUT = '';
      }
      let ret = await this.$callAction({ action: `${Constants.STORE_NAME}/ptmpSel`,
        param: {
          INPUT,
          DEPTID: this.DTS[this.rowIndex]['ADEPTID'],
        },
        isBusy: false });
      callback(ret);
    },
    addDts(path) {
      const item = {
        SLINKER: this.MOBILE,
        SENDNAME: this.LINKER,
        WCUSTNAME: this.CUSTNAME,
        SENDDATE: this.BILLDATE.split(' ')[0],
      };
      this.$store.commit(`${this.storeName}/ADD`, { path, item });
    },
    onRowClick(row, rowIndex) {
      this.rowIndex = rowIndex;
      this.currentRow = row;
    },
  },
};
</script>
<style lang="postcss" scoped>
.upload {
  position: relative;
  left: 80px;
  height: 100%;
  opacity: 0;
  cursor: pointer;
  z-index: 5;
}
</style>

<template>
  <view-dialog :title="title" >
    <div slot="body" style="height: calc(100vh - 107px);">
      <div class="rr-flex-row" style="background:#eee;height: 100%;">
        <div class="left-list rr-flex-1" v-if="!REFTPMDATA&&!ID">
          <div>
            <Search placeholder="请输入关键字" style="width:'100%'" v-model="INPUT" @search="query" />
          </div>
          <div></div>
          <div style="height: calc(100% - 70px);margin-top:10px;">
            <rs-table-list :datas="TPM" :path="$TPM" border ref="table">
              <TableItem title="操作" :width="120" align="center" fixed="right">
                <template slot-scope="{data}">
                  <button class="h-btn h-btn-s h-btn-blue" @click.stop="accept(data)">选择</button>
                </template>
              </TableItem>
            </rs-table-list>
          </div>
        </div>
        <div class="edit rr-scroll-bar" v-show="REFTPMDATA!=''">
          <rs-edit-item
            ref="edit"
            :layouts="REFTPMDATA"
            :select="{}"
            :parent="-1"
            :inLayout="false"
            @clickAtion="clickAtion"
          ></rs-edit-item>附件列表
          <RsUploader
            :readonly="!ISSHOWSUBMIT||!ISSHOWSAVE"
            :options="options"
            type="files"
            data-type="file"
            v-model="FILES"
          ></RsUploader>
        </div>
      </div>
    </div>
    <template slot="footer" class="rr-text-left" v-if="REFTPMDATA!=''">
      <!--
      <Button
        class="ml5"
        v-per="'RS_M10/A04'"
        v-if="ISSHOWSAVE"
        color="primary"
        @click.native="save"
      >暂存</Button>
      <Poptip v-per="'RS_M10/A07'" content="确定删除？" v-if="ISSHOWDELETE" @confirm="del">
        <Button class="ml5" color="red">删除</Button>
      </Poptip>
      <Button
        class="ml5"
        v-per="'RS_M10/A08'"
        v-if="ISSHOWSAVE"
        color="primary"
        @click.native="submit(ID)"
      >提交</Button>
      <Poptip content="确定撤销提交？" v-per="'RS_M10/A09'" v-if="ISSHOWRESUBMIT" @confirm="reSubmit(ID)">
        <Button class="ml5" color="red" icon="h-icon-close">撤销提交</Button>
      </Poptip>
      -->
      <Tooltip
        theme="white"
        v-per="'RS_M10/A10'"
        trigger="click"
        editable
        v-if="ISSHOWCHECK"
        ref="checkTip"
      >
        <Button class="ml5" color="primary">审核</Button>
        <div slot="content">
          <div v-padding="10">
            <textarea dict="simple" placeholder="输入审核说明" v-model="REMARK" style="width: 200px;"></textarea>
            <AutoComplete
              placeholder="请选择下一审批人"
              :option="empParam1"
              v-model="VERIFYID"
              @change="v=>this.VERIFYER = v.title"
            ></AutoComplete>
          </div>
          <div v-padding="10" class="text-center">
            <Button color="primary" @click.native="check(ID)">通过</Button>
            <Button class="ml5" color="red" @click.native="reject(ID)">驳回</Button>
          </div>
        </div>
      </Tooltip>
      <Poptip content="确定撤销审核？" v-per="'RS_M10/A11'" v-if="ISSHOWRECHECK" @confirm="reCheck(ID)">
        <Button class="ml5" color="red" icon="h-icon-close">撤销审核</Button>
      </Poptip>
      <!--
      <Tooltip
        theme="white"
        v-per="'RS_M10/A12'"
        trigger="click"
        editable
        v-if="ISSHOWVERIFY"
        ref="verifyTip"
      >
        <Button class="ml5" color="primary">审批</Button>
        <div slot="content">
          <div v-padding="10">
            <textarea dict="simple" v-model="REMARK" style="width: 200px;"></textarea>
          </div>
          <div v-padding="10" class="text-center">
            <Button color="primary" @click.native="verify(ID)">通过</Button>
            <Button class="ml5" color="red" @click.native="reject(ID)">驳回</Button>
          </div>
        </div>
      </Tooltip>
      <Poptip content="确定撤销审批？" v-per="'RS_M10/A13'" v-if="ISSHOWREVERIFY" @confirm="reVerify(ID)">
        <Button class="ml5" color="red" icon="h-icon-close">撤销审批</Button>
      </Poptip>
      -->
    </template>
  </view-dialog>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store1';
import RsUploader from '@/components/rs-uploader';
import Add01 from '@/mixins/add01';
export default {
  name: 's01-m10-add1',
  data() {
    return {
      options: {
        max_file_size: '20mb',
      },
      empParam1: {
        loadData: this.empSel1,
        keyName: 'ID',
        titleName: 'EMPNAME',
      },
      param2: {
        loadData: this.ptmpSel,
        keyName: 'ID',
        titleName: 'DOCTITLE',
      },
      deptParam: {
        loadData: this.deptSel,
        keyName: 'ID',
        titleName: 'DEPTNAME',
      },
      fieldsConfig: [],
      idObj: {},
      tableObj: {},
      inputObj: {},
      editorObj: [],
      REMARK: '',
      INPUT: '',
    };
  },
  mixins: [Add01],
  computed: {
    ...mapDateTable('MAIN', ['REFTPMDATA', 'STATE', 'DEPTNAME', 'PTEMPLATEID', 'DEPTID']),
    ...mapDateTable('PTMP', []),
    ...mapDateTable('TPM', []),
    ...mapDateTable('DEPT', []),
    ...mapDateTable('DTSB', []),
    ...mapDateTable('EMPUSER', []),
    ISSHOWSAVE(state, getters, rootState, rootGetters) {
      // 主键、状态空、待提交
      return !this.STATE || this.STATE === 1 || this.STATE === 12;
    },
    ISSHOWDINVALID() {
      return this.STATE === 10;
    },
    FILES: {
      get() {
        let dts = [...this.DTSB];
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
  methods: {
    dealConfigSelect(nodes, that) {
      nodes.map(n => {
        if (n.path) {
          n.fieldProps = n.fieldProps || {};
          n.cellOn = n.cellOn || {};
          if (n.path === '文件类别') {
            n.fieldProps.dict = n.path;
          }
          if (n.path === 'DEPT') {
            n.fieldProps.option = that.deptParam;
          }
        }
        if (n.field) {
          this.inputObj[n.field] = n;
        }
        if (n.sourceName) {
          this.tableObj[n.sourceName] = n;
          n.value = [];
        }
        if (n.type === 'itemEditor') {
          this.editorObj.push(n);
        }

        if (n.children && n.children.length > 0) {
          this.dealConfigSelect(n.children, that);
        }
      });
    },
    initTree() {
      debugger;
      this.fieldsConfig = this.REFTPMDATA || [];
      this.tableObj = {};
      this.inputObj = {};
      this.editorObj = [];
      this.dealConfigSelect(this.REFTPMDATA || [], this);
      this.$forceUpdate();
    },
    async clickRow(row) {
      await this.$callAction({ action: `${Constants.STORE_NAME}/openPTEMP`,
        param: {
          ID: row.PTEMPLATEID,
          item: row,
        },
        isBusy: false });
      this.initTree();
    },
    async ptmpSel(INPUT, callback) {
      if (this.PTEMPLATENAME === INPUT) {
        INPUT = '';
      }
      await this.$callAction({ action: `${Constants.STORE_NAME}/ptmpSel`,
        param: {
          INPUT,
        },
        isBusy: false });
      callback(this.PTMP);
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
    async query(INPUT) {
      await this.$callAction({ action: `${this.storeName}/openTMP`,
        param: {
          INPUT: this.INPUT,
        },
        isBusy: false });
    },
    async onShow() {
      if (this.ID) {
        await this.$callAction({ action: `${this.storeName}/open`, param: { ID: this.ID }, isBusy: false });
        await this.showByTemplate();
      } else {
        this.query();
        await this.$callAction({ action: `${this.storeName}/add`, param: {}, isBusy: false });
        await this.$callAction({ action: `${this.storeName}/acceptSel`,
          param: {
            INPUT: '',
            STATE: this.selected,
          },
          isBusy: false });
      }
    },
    async showByTemplate() {
      debugger;
      await this.$callAction({ action: `${Constants.STORE_NAME}/openTMP`,
        param: {
          ID: this.PTEMPLATEID,
          ISEDIT: true,
        },
        isBusy: false });
      this.initTree();
      let { inputObj, tableObj, editorObj } = this;
      this.$store.commit(`${Constants.STORE_NAME}/SETSHOWTPMDATA`, { inputObj, tableObj, editorObj });
      this.editorObj.map(n => {
        n.value += ' ';
      });
      this.$forceUpdate();
    },
    save() {
      console.log(this.inputObj);
      // 处理主表
      // 处理标准器表
      // 处理其他字段
      let { inputObj, tableObj, editorObj } = this;
      this.$callAction({
        action: `${this.storeName}/doMySave`,
        successText: '操作成功',
        isSuccessBack: true,
        param: { inputObj, tableObj, editorObj },
        successCall: () => {
          if (this.showQuery) {
            this.$callAction({ action: `${this.storeName}/advQuery`, timeOut: 0 });
          } else {
            this.$callAction({ action: `${this.storeName}/query`, timeOut: 0 });
          }
        },
      });
    },
    submit() {
      console.log(this.inputObj);
      // 处理主表
      // 处理标准器表
      // 处理其他字段
      let { inputObj, tableObj, editorObj } = this;
      this.$callAction({
        action: `${this.storeName}/doMySubmit`,
        successText: '操作成功',
        isSuccessBack: true,
        param: { inputObj, tableObj, editorObj },
        successCall: () => {
          if (this.showQuery) {
            this.$callAction({ action: `${this.storeName}/advQuery`, timeOut: 0 });
          } else {
            this.$callAction({ action: `${this.storeName}/query`, timeOut: 0 });
          }
        },
      });
    },
    async reSubmit(ID) {
      await this.$callAction({
        action: `${Constants.STORE_NAME}/reSubmit`,
        successText: '操作成功',
        isSuccessBack: false,
        param: { ID },
        successCall: () => {
          this.onShow();
        },
      });
    },
    async accept(item) {
      await this.$callAction({ action: `${Constants.STORE_NAME}/add`,
        param: {
          item,
        },
        isBusy: false });

      await this.$callAction({ action: `${Constants.STORE_NAME}/setTmpData`,
        param: {
          item,
        },
        isBusy: false });
      this.initTree();
    },
    check(item) {
      if (!this.VERIFYID) {
        this.$error('请选择审批人！');
        return;
      }
      let { ID, REMARK } = this;
      this.$callAction({
        action: `${this.storeName}/check`,
        successText: '操作成功',
        isSuccessBack: true,
        param: { ID, REMARK, item: this.citem, VERIFYID: this.VERIFYID, VERIFYER: this.VERIFYER },
        successCall: () => {},
      });
      this.$refs.checkTip.hide();
    },
    reject(item) {
      let { ID, REMARK } = this;
      this.$callAction({
        action: `${this.storeName}/reject`,
        successText: '操作成功',
        isSuccessBack: true,
        param: { ID, REMARK, item: this.citem },
        successCall: () => {},
      });
      this.$refs.checkTip.hide();
    },
    reCheck(item) {
      let { ID, REMARK } = this;
      this.$callAction({
        action: `${this.storeName}/reCheck`,
        successText: '操作成功',
        isSuccessBack: false,
        param: { ID, REMARK, item: this.citem },
        successCall: () => {
          this.onShow();
        },
      });
      this.$refs.checkTip.hide();
    },
    verify(item) {
      let { ID, REMARK } = this;
      this.$callAction({
        action: `${this.storeName}/verify`,
        successText: '操作成功',
        isSuccessBack: true,
        param: { ID, REMARK, item: this.citem },
        successCall: () => {},
      });
      this.$refs.verifyTip.hide();
    },
    reVerify(item) {
      let { ID, REMARK } = this;
      this.$callAction({
        action: `${this.storeName}/reVerify`,
        successText: '操作成功',
        isSuccessBack: false,
        param: { ID, REMARK, item: this.citem },
        successCall: () => {
          this.onShow();
        },
      });
      this.$refs.verifyTip.hide();
    },
    clickAtion() {
      this.$refs.madd.show();
    },
    onSelectArd(items) {
      let titems = this.tableObj['VBS_ARD_4TPL'].value || [];
      items.forEach(item => {
        if (
          !titems.find(i => {
            return i.ID == item.ID;
          })
        ) {
          titems.push(item);
        }
      });
      this.tableObj['VBS_ARD_4TPL'].value = titems;
      this.$forceUpdate();
    },
    async empSel1(INPUT, callback) {
      if (this.TEMP1 === INPUT) {
        INPUT = '';
      }
      await this.$callAction({ action: `${Constants.STORE_NAME}/empSel1`,
        param: {
          INPUT,
          FUNCID: 'b5f561591c8947e5ae94245933887cfe',
          DEPTID: this.DEPTID,
        },
        isBusy: false });
      callback(this.EMPUSER);
    },
  },
};
</script>
<style lang="less" scoped>
.list-item {
  margin-bottom: 10px;
  line-height: 33px;
  overflow: auto;
  span {
    float: left;
  }
  .list-right {
    margin-left: 3.5em;
  }
}
.left-list {
  background: #fff;
  border: 1px solid #eee;
  padding: 10px 10px;
  margin: 10px;
  height: calc(100% - 20px);
}
.edit {
  background: #fff;
  border: 1px solid #eee;
  padding: 20px 50px;
  min-height: 100%;
  width: 900px;
  margin: 0 auto;
}
/deep/ .h-dropdowncustom-show-content {
  width: 100%;
}
/deep/ .h-table {
  max-height: calc(100%);
  height: calc(100%);
  .h-table-border {
    border-left: 1px solid #eee;
  }
  /deep/ .h-table-container {
    max-height: calc(100% - 40px);
    overflow-y: auto;
    height: calc(100% - 20px);
  }
}
</style>

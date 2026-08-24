<template>
  <view-dialog :title="title">
    <div slot="body">
      <div class="rr-flex-row" style="background:#eee;height: 100%;">
        <div class="left-list" v-if="!ID&&!PTEMPLATEID">
          <div>
            <Search placeholder="请输入关键字" style="width:'100%'" v-model="INPUT" @search="query" />
          </div>
          <div></div>
          <div >
            <Tabs :datas="param" v-model="selected" @change="query" class-name="h-tabs-card"></Tabs>
            <rs-table-list :datas="ACCEPT" :path="$ACCEPT" border ref="table">
              <TableItem title="操作" :width="120" align="center" fixed="right">
                <template slot-scope="{data}">
                   <Poptip content="确定受理检验？" v-if="selected=='7'" @confirm="accept(data)">
                    <button
                    class="h-btn h-btn-s h-btn-blue"
                  >受理检验</button>
                  </Poptip>

                  <button
                    class="h-btn h-btn-s h-btn-blue"
                    v-if="selected=='8'"
                    @click.stop="clickRow(data)"
                  >检验</button>
                  <Poptip content="确定撤销受理？" v-if="selected=='8'" @confirm="reAccept(data)">
                    <button class="h-btn h-btn-s h-btn-red">撤销</button>
                  </Poptip>
                </template>
              </TableItem>
            </rs-table-list>
          </div>
        </div>
        <div class="edit rr-scroll-bar" :class="{'rr-wide-mode': isWideLayout}">
          <attach-flow-panel
            :files="FILES"
            :logs="DTSC"
            :wide="isWideLayout"
            :readonly="true"
            v-show="PTEMPLATEID"
          >
            <rs-edit-item
              ref="edit"
              :layouts="REFTPMDATA"
              :select="{}"
              :parent="-1"
              :inLayout="false"
              @clickAtion="clickAtion"
            ></rs-edit-item>
          </attach-flow-panel>
          <rs-edit-item
            v-if="!PTEMPLATEID"
            ref="editNoPanel"
            :layouts="REFTPMDATA"
            :select="{}"
            :parent="-1"
            :inLayout="false"
            @clickAtion="clickAtion"
          ></rs-edit-item>
        </div>
      </div>
      <rs-modal ref="madd">
        <ard-sel @on-select="onSelectArd"></ard-sel>
      </rs-modal>
      <rs-modal ref="mtmp">
        <tmp-sel @on-select="onSelectTmp" :item="currentItem"></tmp-sel>
      </rs-modal>
    </div>
    <template slot="footer">
      <Tooltip
        theme="white"
        v-per="'LI_M02/A14'"
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
      <Poptip content="确定撤销审批？" v-per="'LI_M02/A15'" v-if="ISSHOWREVERIFY" @confirm="reVerify(ID)">
        <Button class="ml5" color="red" icon="h-icon-close">撤销审批</Button>
      </Poptip>
      <!--
      <Button
        class="ml5"
        v-per="'LI_M02/A21'"
        color="primary"
        v-if="ISSHOWREVERIFY"
        @click.native="genCert(ID)"
      >生成证书</Button>
      -->
      <Poptip content="确定作废？" v-per="'LI_M02/A22'" v-if="ISSHOWDINVALID" @confirm="invalid(ID)">
        <Button class="ml5" color="red">作废</Button>
      </Poptip>
    </template>
  </view-dialog>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store2';
import Add01 from '@/mixins/add01';
import RsUploader from '@/components/rs-uploader';
import ardSel from './ardSel';
import tmpSel from './tmpSel';
import attachFlowPanel from './attach-flow-panel';
export default {
  name: 'r01-m02-add2',
  data() {
    return {
      param: {
        '7': '待受理',
        '8': '待检验',
      },
      selected: '8',
      isWideLayout: false,
      reguitemParam: {
        loadData: this.reguitemSel,
        keyName: 'ID',
        titleName: 'ITEMNAME',
      },
      fieldsConfig: [],
      idObj: {},
      tableObj: {},
      inputObj: {},
      editorObj: [],
      REMARK: '',
      INPUT: '',
      currentItem: {},
    };
  },
  mixins: [Add01],
  mounted() {
    this.isWideLayout = window.innerWidth >= 1280;
    this._resizeHandler = () => { this.isWideLayout = window.innerWidth >= 1280 };
    window.addEventListener('resize', this._resizeHandler);
  },
  beforeDestroy() {
    window.removeEventListener('resize', this._resizeHandler);
  },
  computed: {
    ...mapDateTable('MAIN', [
      'AEMPID',
      'AEMPNAME',
      'CUSTID',
      'CUSTNAME',
      'PTEMPLATEID',
      'REGUITEMID',
      'REGUITEMCODE',
      'REGUITEMNAME',
      'REFTPMDATA',
      'STATE',
    ]),
    ...mapDateTable('ACCEPT', []),
    ...mapDateTable('PTEMPSEL', []),
    ...mapDateTable('REGUITEM', []),
    ...mapDateTable('DTSC', []),
    ...mapDateTable('DTSD', []),
    ...mapDateTable('EMPUSER', []),
    ISSHOWSAVE(state, getters, rootState, rootGetters) {
      // 主键、状态空、待提交
      return (!this.STATE && this.PTEMPLATEID) || this.STATE === 1 || this.STATE === 12;
    },
    ISSHOWSUBMIT(state, getters, rootState, rootGetters) {
      // 主键、状态空、待提交
      return (!this.STATE && this.PTEMPLATEID) || this.STATE === 1 || this.STATE === 12;
    },
    ISSHOWDINVALID() {
      return this.STATE === 10;
    },
    FILES: {
      get() {
        let dts = [...this.DTSD];
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
  components: { ardSel, tmpSel, RsUploader, attachFlowPanel },
  methods: {
    dealConfigSelect(nodes, that) {
      nodes.map(n => {
        if (n.path) {
          n.fieldProps = n.fieldProps || {};
          n.cellOn = n.cellOn || {};
          if (n.path === 'REGUITEM') {
            n.fieldProps.option = that.reguitemParam;
            /*
            n.cellOn.change = v => {
              that.REGUITEMID = v.value.ID;
              that.REGUITEMCODE = v.value.ITEMCODE;
              that.REGUITEMNAME = v.value.ITEMNAME;
            };
            */
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
      this.fieldsConfig = this.REFTPMDATA || [];
      this.tableObj = {};
      this.inputObj = {};
      this.editorObj = [];
      this.dealConfigSelect(this.REFTPMDATA || [], this);
      this.$forceUpdate();
    },
    async clickRow(row) {
      this.currentItem = row;
      if (row.PTEMPLATEID) {
        await this.$callAction({ action: `${Constants.STORE_NAME}/openPTEMP`,
          param: {
            ID: row.PTEMPLATEID,
            item: row,
          },
          isBusy: false });
        this.initTree();
      } else {
        this.$refs.mtmp.show();
      }
    },
    onSelectTmp(items) {
      if (items.length > 0) {
        this.currentItem.PTEMPLATEID = items[0].ID;
        this.clickRow(this.currentItem);
      }
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
    async query(INPUT) {
      await this.$callAction({ action: `${this.storeName}/acceptSel`,
        param: {
          INPUT: this.INPUT,
          STATE: this.selected,
        },
        isBusy: false });
    },
    async onShow() {
      if (this._onShowLoading) return;
      this._onShowLoading = true;
      try {
        if (this.ID) {
          await this.$callAction({ action: `${this.storeName}/open`, param: { ID: this.ID }, isBusy: false });
          await this.showByTemplate();
        } else {
          await this.$callAction({ action: `${this.storeName}/add`, param: {}, isBusy: false });
          await this.$callAction({ action: `${this.storeName}/acceptSel`,
            param: {
              INPUT: '',
              STATE: this.selected,
            },
            isBusy: false });
        }
      } catch (e) {
        // Cancel错误（防重复请求）忽略，第二次请求会成功
        if (!e || !e.message || !e.message.includes('手速太快')) {
          console.error('onShow error:', e);
        }
      } finally {
        this._onShowLoading = false;
      }
    },
    async showByTemplate() {
      await this.$callAction({ action: `${Constants.STORE_NAME}/openPTEMP`,
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
        param: { ID, item: this.citem },
        successCall: () => {
          this.showByTemplate();
        },
      });
    },
    genCert(ID) {
      this.$callAction({
        action: `${this.storeName}/genCert`,
        successText: '操作成功',
        isSuccessBack: true,
        param: { ID },
      });
    },
    invalid(ID) {
      this.$callAction({
        action: `${this.storeName}/invalid`,
        successText: '操作成功',
        isSuccessBack: true,
        param: { ID },
      });
    },
    accept(item) {
      this.$callAction({
        action: `${this.storeName}/accept`,
        successText: '',
        isSuccessBack: false,
        param: { items: [item] },
        successCall: async() => {
          this.currentItem = item;
          item.ADEPTID = this.$store.state.user.userInfo.DEPTID;
          if (item.PTEMPLATEID) {
            await this.$callAction({ action: `${Constants.STORE_NAME}/openPTEMP`,
              param: {
                ID: item.PTEMPLATEID,
                item,
              },
              isBusy: false });
            this.initTree();
            let { inputObj, tableObj, editorObj } = this;
            this.$store.commit(`${Constants.STORE_NAME}/SETSHOWTPMDATA`, { inputObj, tableObj, editorObj });
          } else {
            this.$refs.mtmp.show();
          }
        },
      });
    },
    reAccept(item) {
      this.$callAction({
        action: `${this.storeName}/reAccept`,
        successText: '操作成功',
        isSuccessBack: false,
        param: { items: [item] },
        successCall: async() => {
          this.query();
        },
      });
    },

    check(item) {
      let { ID, REMARK } = this;
      this.$callAction({
        action: `${this.storeName}/check`,
        successText: '操作成功',
        isSuccessBack: true,
        param: { ID, REMARK, item: this.citem },
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
      });
      this.$refs.checkTip.hide();
    },
    async reCheck(item) {
      let { ID, REMARK } = this;
      await this.$callAction({
        action: `${this.storeName}/reCheck`,
        successText: '操作成功',
        isSuccessBack: false,
        param: { ID, REMARK, item: this.citem },
        successCall: () => {
          this.onShow();
        },
      });
      await this.$refs.checkTip.hide();
    },
    async verify(item) {
      let { ID, REMARK } = this;
      await this.$callAction({
        action: `${this.storeName}/verify`,
        successText: '操作成功',
        isSuccessBack: false,
        param: { ID, REMARK, item: this.citem },
        successCall: () => {
          this.onShow();
        },
      });
      this.$refs.verifyTip.hide();
    },
    async reVerify(item) {
      let { ID, REMARK } = this;
      await this.$callAction({
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
  padding: 20px 20px;
  min-height: 100%;
  width: 900px;
  margin: 0 auto;
}
.rr-wide-mode {
  width: auto;
  flex: 1;
  min-width: 0;
  margin: 10px;
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

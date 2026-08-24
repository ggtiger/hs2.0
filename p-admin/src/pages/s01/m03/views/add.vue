<template>
  <view-dialog :title="ISADD?'新增功能':'编辑功能'" style="width:960px;" @on-show="onShow">
    <template slot="body">
       <ToolBar label="基本信息" :size="16"></ToolBar>
      <Form :label-width="80" mode="twocolumn" class="maxModalH rs-flex-col">
        <FormItem label="功能类型">
          <Radio :value="1" v-model="FUNCTYPE">目录</Radio>
          <Radio :value="2" v-model="FUNCTYPE">模块</Radio>
        </FormItem>
        <FormItem label="使用">
          <h-switch v-model="ISUSE" :trueValue="1" :falseValue="0">
            <span slot="open">是</span>
            <span slot="close">否</span>
          </h-switch>
        </FormItem>
        <FormItem label="上级目录">
          <TreePicker :option="param" ref="UPFUNCID" v-model="UPFUNCID"></TreePicker>
        </FormItem>
        <FormItem label="功能图标">
          <iconSel v-model="FUNCICON" />
        </FormItem>
        <FormItem label="功能编码">
          <input type="text" v-model="FUNCCODE" placeholder="请输入" />
        </FormItem>
        <FormItem label="排序码">
          <input type="number" v-model="SORTCODE" placeholder="请输入" />
        </FormItem>
        <FormItem label="功能名称">
          <input type="text" v-model="FUNCNAME" placeholder="请输入" />
        </FormItem>
        <FormItem label="菜单隐藏">
          <h-switch v-model="ISHIDE" :trueValue="1" :falseValue="0">
            <span slot="open">是</span>
            <span slot="close">否</span>
          </h-switch>
        </FormItem>
        <FormItem label="外部地址">
          <h-switch v-model="ISOUTERURL" :trueValue="1" :falseValue="0">
            <span slot="open">是</span>
            <span slot="close">否</span>
          </h-switch>
        </FormItem>
        <FormItem label="地址URL" single>
          <input type="text" v-model="OUTERURL" placeholder="请输入" />
        </FormItem>
        <FormItem label="说明" single>
          <input type="text" v-model="REMARK" placeholder="请输入" />
        </FormItem>
      </Form>
      <ToolBar label="功能点信息" :size="16">
      <div slot="right">
        <Button color="primary" icon="h-icon-plus" size="s" @click="addDts('DTSA')">新增</Button>
        <Button color="primary" icon="h-icon-minus" size="s" @click="removeDts('DTSA',$refs.DTSA)">移除</Button>
        <Button color="primary" icon="h-icon-top" size="s" @click="moveUp('DTSA',$refs.DTSA)">上移</Button>
        <Button color="primary" icon="h-icon-down" size="s" @click="moveDown('DTSA',$refs.DTSA)">下移</Button>
        <Button color="primary" icon="h-icon-search" size="s" @click="selfShow=true">引入接口</Button>
      </div>
      </ToolBar>
      <Modal v-model="selfShow" title="引入接口" hasCloseIcon middle>
        <apiSel @on-select="selectApi"></apiSel>
      </Modal>
      <rs-table-edit border ref="DTSA" :path="$DTSA" :datas="DTSA"></rs-table-edit>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'RS_M03/A07'" v-if="!ISADD" @confirm="del"><Button class="ml5" color="red">删除</Button></Poptip>
      <Button class="ml5" v-per="'RS_M03/A04'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Gen from '@/utils/gen';
import apiSel from './apiSel.vue';
import iconSel from './iconSel.vue';
export default {
  name: 'zyadd',
  data() {
    return {
      currentStartIndex: 0,
      currentEndIndex: 0,
      selfShow: false,
      selfShowLoading: false,
      selfShow2: false,
      selfShowLoading2: false,
      ruleValidate: {
        name: [
          {
            required: true,
            message: '不可为空',
            trigger: 'blur',
          },
        ],
        yygx: [
          {
            required: true,
            message: '不可为空',
            trigger: 'blur',
          },
        ],
      },
      columns1: [],
      columns2: [],
      columns3: [],
      param: {
        keyName: 'ID',
        parentName: 'UPFUNCID',
        titleName: 'FUNCNAME',
        dataMode: 'list',
        getTotalDatas: this.remoteMethod2,
      },
    };
  },
  computed: {
    ...mapDateTable('MAIN', [
      'ID',
      'FUNCTYPE',
      'FUNCCODE',
      'FUNCNAME',
      'ISHIDE',
      'ISUSE',
      'REMARK',
      'UPFUNCID',
      'FUNCICON',
      'OUTERURL',
      'ISOUTERURL',
      'SORTCODE',
    ]),
    ...mapDateTable('DTSA', []),
    ...mapDateTable('SEL', []),
    ISDATAVIEW() {
      return this.RESOURCETYPE == 'DATAVIEW';
    },
    ISTABLE() {
      return this.RESOURCETYPE == 'TABLE';
    },
    TDTSA() {
      return JSON.parse(JSON.stringify(this.DTSA));
    },
    ISADD() {
      return !this.ID;
    },
  },
  mounted() {},
  components: { apiSel, iconSel },
  methods: {
    async remoteMethod2(callback) {
      await this.$callAction({ action: `${Constants.STORE_NAME}/querySel`, param: {}, isBusy: false });
      callback(this.SEL);
    },
    async onDTSACellSearch({ value, field }) {
      await this.$callAction({ action: `${Constants.STORE_NAME}/querySel`, param: { INPUT: value }, isBusy: false });
    },
    closeW() {
      this.$parent.setvalue(false);
    },
    applyEdit(row, column, index, path, event) {
      this['$' + path].setValue(column, event || row[column], this[path][index]);
      this.editClick(null, index);
    },
    editClick: function(column, index, path) {
      this.editInfo.column = column;
      this.editInfo.index = index;
      let _this = this;
      this.$nextTick(() => {
        _this.$refs[path + '-' + column + '-' + index].focus();
      });
    },
    show(index) {
      if (this.data1[index].ISFORBID === '0') {
        this.$Modal.info({
          title: '信息显示',
          content: `Name：${this.data1[index].name}<br>Age：${this.data1[index].age}<br>Address：${this.data1[index].address}`,
        });
      }
    },
    addDts(path) {
      // this.TDTSA.push({})
      this.$store.commit(`${Constants.STORE_NAME}/ADD`, { path });
    },
    moveUp(path, table) {
      this[`$${path}`].upItem({ item: table.currentRow });
      this.$nextTick(() => {
        table.clickCurrentRow(this[path].indexOf(table.currentRow));
      });
    },
    moveDown(path, table) {
      this[`$${path}`].downItem({ item: table.currentRow });
      this.$nextTick(() => {
        table.clickCurrentRow(this[path].indexOf(table.currentRow));
      });
    },
    removeDts(path, table) {
      if (table.currentRow == -1) {
        return;
      }
      this.$store.commit(`${Constants.STORE_NAME}/DEL`, { path, item: table.currentRow });
    },
    save() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/save`,
        successText: '保存成功',
        isSuccessBack: true,
      });
    },
    async del() {
      // await this.$confirm('确认删除？');
      this.$callAction({
        action: `${Constants.STORE_NAME}/delete`,
        successText: '删除成功',
        isSuccessBack: true,
      });
    },
    close(nodesc) {},
    clickDtsA(row, index) {
      this.editInfo.index = index;
    },
    selectApi(items) {
      this.$store.commit(`${Constants.STORE_NAME}/SET_SELECTAPI`, { items });
    },
  },
  async mounted() {},
  watch: {
    ID: function(v) {
      if (v != '') {
        this.$parent.title = '编辑模块';
      } else {
        this.$parent.title = '新增模块';
      }
      this.$refs.UPFUNCID.refresh();
    },
    ISADD: {
      handler(v) {
        if (v + '' === '1') {
          this.$callAction({ action: `${Constants.STORE_NAME}/add`, param: {}, isBusy: false });
        }
      },
      immediate: true,
    },
  },
};
</script>

<style scoped>
.maxModalH {
  overflow: auto;
}
</style>

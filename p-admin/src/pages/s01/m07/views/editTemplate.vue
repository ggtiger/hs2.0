<template>
  <view-dialog :title="title"  :loading="loading">
    <div slot="body" style="height: calc(100vh - 147px);" @click="click">
      <div class="rr-flex-row" style="background:#eee">
        <div v-width="200" class="rr-scroll-bar">
          <div class="tree" style="display:none">
            <div class="h-panel">
              <div class="h-panel-bar">
                <span class="h-panel-title">控件</span>
                <span class="h-panel-right">
                  <span :class="showKjian?'h-icon-top':'h-icon-down'" @click="showKjian=!showKjian"></span>
                </span>
              </div>
              <div class="h-panel-body rr-text-center" v-show="showKjian">
                <div v-for="(item,index) in list" :key="index">
                  <Button
                    size="s"
                    @click.native="addField(item)"
                    style="margin-bottom:10px;"
                  >{{item.name}}</Button>
                </div>
              </div>
            </div>
          </div>
          <div class="tree">
            <div class="h-panel">
              <div class="h-panel-bar">
                <span class="h-panel-title">树形结构</span>
                <span class="h-panel-right">
                  <span :class="showTree?'h-icon-top':'h-icon-down'" @click="showTree=!showTree"></span>
                </span>
              </div>
              <div class="h-panel-body" v-show="showTree">
                <Tree
                  :option="treeData"
                  ref="tree"
                  :toggleOnSelect="toggleOnSelect"
                  v-model="attr.id"
                  @open="open"
                  @select="selectTree"
                ></Tree>
              </div>
            </div>
          </div>
        </div>
        <div class="edit rr-flex-1 rr-scroll-bar">
          <DropdownMenu
            :datas="[{ title: '插入元素', key: 'add', icon: 'h-icon-plus' }, { title: '删除元素', key: 'delete', icon: 'h-icon-trash' }]"
            trigger="contextMenu"
            :toggleIcon="false"
            style="width:100%"
            @click="contextmenu"
          >
            <rs-edit-item
              ref="edit"
              :layouts="fieldsConfig"
              :parent="-1"
              :select="select"
              @selectItem="selectItem"
            ></rs-edit-item>
          </DropdownMenu>
        </div>
        <rs-modal ref="addField" style="display:none">
          <rs-add-field :itemType="attr.type" title="新增控件" @success="success" @close="close"></rs-add-field>
        </rs-modal>
        <div v-width="300" class="right rr-scroll-bar">
          <div class="tree">
            <div class="h-panel">
              <div class="h-panel-bar">
                <span class="h-panel-title">属性设置</span>
                <span class="h-panel-right">
                  <span :class="showAttr?'h-icon-top':'h-icon-down'" @click="showAttr=!showAttr"></span>
                </span>
              </div>
              <set-attr :value="attr" v-show="showAttr" :tpmType="TPMTYPE"></set-attr>
            </div>
          </div>
        </div>
      </div>
    </div>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Button class="ml5" v-per="'RS_M07/A08'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>

<script>
import setAttr from './components/rs-set-attr.vue';
import rsAddField from './components/rs-add-field.vue';
import getFields from '../fieldsConfig1';
import getFieldsTem from './components/fieldTem';
import { mapDateTable, Constants } from '../store';
import Add01 from '@/mixins/add01';
export default {
  name: 'editTem',
  mixins: [Add01],
  components: {
    setAttr,
    rsAddField,
  },
  computed: {
    ...mapDateTable('MAIN', ['TPMDATA', 'TPMTYPE']),
  },
  data() {
    return {
      // 左侧控件
      list: [
        { name: '布局', type: 'layout' },
        { name: '标签', type: 'label' },
        { name: '输入框', type: 'field' },
        { name: '勾选框', type: 'checkBox' },
        { name: '表格', type: 'table' },
        { name: '富文本框', type: 'mechiter' },
      ],
      fieldsTem: getFieldsTem(this),
      fieldsConfig: [],
      showKjian: true, // 左侧控件是否展开
      // 右侧tree树
      treeData: {
        keyName: 'id',
        childrenName: 'children',
        titleName: 'title',
        dataMode: 'fieldsConfig',
        datas: [],
      },
      showTree: true, // 右侧tree树是否展开
      toggleOnSelect: true,
      attr: {}, // 选中的控件
      currentTree: null, // 选中控件的ID
      // 组件属性
      showAttr: true, // 右侧组件属性是都展开
      select: {},
      idObj: {},
    };
  },
  watch: {
    fieldsConfig: {
      handler(val) {
        this.treeData.datas = val;
      },
      immediate: true,
      deep: true,
    },
    currentValue(v) {
      this.$emit('input', v);
    },
  },
  mounted() {
    this.$nextTick(function() {
      // this.initTree();
    });
    // bus.$on('selectItem', this.selectItem);
  },
  methods: {
    initTree() {
      if (this.TPMDATA) {
        this.fieldsConfig = this.dealTreeData(JSON.parse(this.TPMDATA));
      } else {
        this.fieldsConfig = this.dealTreeData(getFields(this));
      }
      this.attr = this.fieldsConfig[0] || {};
      this.currentTree = this.fieldsConfig[0].id || null;
      this.select = this.attr;
    },
    dealTreeData(node, parent) {
      if (parent == null) {
        node.map(n => {
          this.dealTreeData(n, {});
        });
      }
      parent = parent || {};
      node.parent = parent.id;
      node.id = this.get_uuid();
      this.idObj[node.id] = node;
      if (node.children) {
        node.children.map(n => {
          this.dealTreeData(n, node);
        });
      }
      return node;
    },
    get_uuid() {
      var s = [];
      var hexDigits = '0123456789abcdef';
      for (var i = 0; i < 36; i++) {
        s[i] = hexDigits.substr(Math.floor(Math.random() * 0x10), 1);
      }
      s[14] = '4';
      s[19] = hexDigits.substr((s[19] & 0x3) | 0x8, 1);
      s[8] = s[13] = s[18] = s[23] = '-';
      var uuid = s.join('');
      return uuid;
    },
    addField(item) {
      var len = this.fieldsConfig.length;
      var field = {
        id: item.type + len,
        type: item.type,
      };
      this.fieldsConfig.push(field);
    },
    selectItem(value) {
      // 点击模板切换属性
      this.attr = value;
      this.currentTree = value.id;
      this.select = value;
      this.$refs.tree.expand([value.parent]);
    },
    contextmenu(key) {
      if (key === 'add') {
        this.$refs.addField.show();
      } else if (key === 'delete') {
        this.delField();
      }
    },
    delField() {
      let arr = this.fieldsConfig;
      if (this.idObj[this.select.parent]) {
        arr = this.idObj[this.select.parent].children;
      }
      let cindex = arr.indexOf(this.select);
      if (cindex > 0) {
        this.select = arr[cindex - 1];
      } else if (this.idObj[this.select.parent]) {
        this.select = arr;
      }
      arr.splice(cindex, 1);
      this.$refs.tree.refresh();
    },
    success(value) {
      var item = JSON.parse(JSON.stringify(this.fieldsTem[value.type]));
      item.id = this.get_uuid();
      this.idObj[item.id] = item;
      let arr = this.fieldsConfig;
      if (this.idObj[this.select.parent]) {
        arr = this.idObj[this.select.parent].children;
      }
      switch (value.position) {
        case 'top':
          item.parent = this.select.parent;
          arr.splice(arr.indexOf(this.select), 0, item);
          break;
        case 'bottom':
          item.parent = this.select.parent;
          arr.splice(arr.indexOf(this.select) + 1, 0, item);
          break;
        case 'inset':
          if (this.select.type === 'itemLayout') {
            item.parent = this.select.id;
            if (!this.select.children) {
              this.select.children = [];
            }
            this.select.children.push(item);
          } else {
            this.$toast('请在布局内插入元素');
          }
          break;
      }
      this.select = item;
      this.$refs.tree.refresh();
      this.$refs.tree.expand([this.select.parent]);
      console.log('生成', this.fieldsConfig);
    },
    close() {
      this.$refs.addField.hide();
    },
    closeW() {
      this.$parent.setvalue(false);
    },
    selectTree(value) {
      this.attr = this.idObj[value.id];
      this.select = this.idObj[value.id];
      console.log('生成', this.fieldsConfig);
    },
    async onShow() {
      this.loading = true;
      try {
        if (this.ID) {
          await this.$callAction({ action: `${this.storeName}/open`, param: { ID: this.ID }, isBusy: false });
          this.initTree();
        }
      } finally {
        this.loading = false;
      }
    },
    dealSaveData(fields) {
      fields.forEach(f => {
        f.id = '';
        f.parent = '';
        delete f.id;
        delete f.parent;
        if (f.children && f.children.length > 0) {
          this.dealSaveData(f.children);
        }
      });
    },
    save() {
      this.TPMDATA = JSON.stringify(this.fieldsConfig);
      this.$callAction({
        action: `${this.storeName}/save`,
        successText: '操作成功',
        isSuccessBack: true,
      });
    },
    click() {
      console.log('click');
      debugger;
    },
  },
};
</script>
<style lang="less" scoped>
.rr-flex-row {
  height: 100%;
  overflow: hidden;
  .tree {
    margin: 10px;
    background: #f8f8f8;
  }
}
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
.edit {
  background: #fff;
  border: 1px solid #eee;
  padding: 20px 50px;
  min-height: calc(100vh - 157px);
}
/deep/ .h-dropdowncustom-show-content {
  width: 100%;
}
</style>

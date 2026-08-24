<template>
  <view-dialog title="权限设置" >
    <template slot="body">
      <FuncItem :listArr="TREEQRY"></FuncItem>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Button class="ml5" v-per="'RS_M04/A07'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Gen from '@/utils/gen';
import FuncItem from '../components/func-item';
import bus from '../eventbus';
export default {
  name: 'zyadd',
  props: {
    params: { Type: Object },
  },
  components: { FuncItem },
  data() {
    return {};
  },
  computed: {
    ...mapDateTable('SEL', []),
    ...mapDateTable('SELDTS', []),
    ...mapDateTable('DTSA', []),
    TREEQRY() {
      return this.getTreeData(this.SEL, '', 1);
    },
    MAPDTSA() {
      let v = {};
      this.DTSA.forEach(vv => (v[vv.FUNCID] = vv));
      return v;
    },
  },
  mounted() {},
  methods: {
    getTreeData(datas, up, level) {
      let aobj = [];
      aobj = datas.filter(item => (item.UPFUNCID || '') === up);
      aobj.forEach(element => {
        element.ISCHECK = !!this.MAPDTSA[element.ID];
        element.level = level;
        let tobj = this.getTreeData(datas, element.ID, level + 1);
        let pobj = this.getPoint(element.ID);
        if (tobj.length > 0) {
          element.children = tobj;
          tobj.forEach(t => (t.UPITEM = element));
        }
        if (pobj.length > 0) {
          element.point = pobj;
          pobj.forEach(t => {
            t.UPITEM = element;
            t.ISCHECK = !!this.MAPDTSA[t.ID];
          });
        }
      });
      return aobj;
    },
    getPoint(FUNCID) {
      return this.SELDTS.filter(item => item.FUNCID === FUNCID);
    },
    closeW() {
      this.$parent.setvalue(false);
    },
    save() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/savePower`,
        param: { ROLEID: this.params.ID, treeData: this.TREEQRY },
        successText: '保存成功',
        isSuccessBack: true,
      });
    },
    async del() {
      await this.$confirm('确认删除？');
      this.$callAction({
        action: `${Constants.STORE_NAME}/delete`,
        successText: '删除成功',
        isSuccessBack: true,
      });
    },
    change(item, ISCHECK) {
      if (item.point) {
        item.point.forEach(p => {
          p.ISCHECK = ISCHECK;
        });
      }
      if (item.children) {
        item.children.forEach(p => {
          this.change(p, ISCHECK);
          p.ISCHECK = ISCHECK;
        });
      }
      if (ISCHECK) {
        this.changeUpItem(item);
      }
    },
    changeUpItem(item) {
      if (item.UPITEM) {
        item.UPITEM.ISCHECK = true;
        this.changeUpItem(item.UPITEM);
      }
    },
  },
  async mounted() {
    bus.$on('change', (item, ISCHECK) => {
      this.change(item, ISCHECK);
    });
    bus.$on('change-up-item', item => {
      this.changeUpItem(item);
    });
  },
  watch: {
    '$parent.isOpened': {
      handler(v) {
        if (v) {
          this.$callAction({ action: `${Constants.STORE_NAME}/openSel`, param: { ID: this.params.ID }, isBusy: false });
          this.$callAction({ action: `${Constants.STORE_NAME}/openDts`, param: { ID: this.params.ID }, isBusy: false });
        }
      },
    },
  },
};
</script>

<style scoped>
.maxModalH {
  overflow: auto;
}
</style>

<template>
  <div class="h-panel h-panel-no-border rr-flex-col">
    <div class="h-panel-bar rr-flex-row">
      <span class="h-panel-title" width="400px">
        <Breadcrumb :datas="bcDatas"></Breadcrumb>
      </span>
      <Row :space="9" class="rr-flex-1">
        <Cell width="12"></Cell>
        <Cell width="12" style="text-align: right;">
          <Search placeholder="请输入关键字" v-model="INPUT" style="width:300px" @search="query" />
          <Button v-per="'RS_M03/A04'" color="primary" class="ml5"  @click="add">新增功能</Button>
        </Cell>
      </Row>
    </div>
    <div class="h-panel-body rr-flex-1">
      <div class="rs-flex-1" style="overflow:hidden">
        <Table border ref="selection" @trclick="clickRow" :datas="TREEQRY">
          <TableItem title="#" prop="$serial" :width="40"></TableItem>
          <TableItem title="功能类型" prop="FUNCTYPE" dict="功能类型" :width="70"></TableItem>
          <TableItem title="功能编码" prop="FUNCCODE" :width="70"></TableItem>
          <TableItem title="排序码" prop="SORTCODE" :width="70"></TableItem>
          <TableItem title="功能名称" :width="180" treeOpener>
            <template slot-scope="{data}">
              <i :class="data.FUNCICON"></i>
              {{data.FUNCNAME}}
            </template>
          </TableItem>
          <TableItem title="隐藏否" prop="ISHIDE" :width="70"></TableItem>
          <TableItem title="使用否" prop="ISUSE" :width="70"></TableItem>
          <TableItem title="地址" prop="OUTERURL" :width="150"></TableItem>
          <TableItem title="备注" prop="REMARK"></TableItem>
        </Table>
      </div>
    </div>
    <Modal
      v-model="modal1"
      title="重置"
      :styles="{top: '20px'}"
      width="80%"
      :loading="loading"
      :mask-closable="false"
      @on-cancel="close(false)"
    >
      <div></div>
    </Modal>

    <Modal
      v-model="modal2"
      title="新增功能"
      :styles="{top: '20px'}"
      width="80%"
      :loading="loading"
      :closeOnMask="false"
      hasCloseIcon
      middle
    >
      <div>
        <rsAdd></rsAdd>
      </div>
    </Modal>
  </div>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Gen from '@/utils/gen';
import rsAdd from './add.vue';
export default {
  name: 's01-m03-main',
  components: {
    rsAdd,
  },
  computed: {
    ...mapDateTable('QRY', []),
    ...mapDateTable('QQRY', ['INPUT', 'TotalCount', 'PageSize', 'PageIndex']),
    TREEQRY() {
      return this.getTreeData(this.QRY, '');
    },
  },
  data() {
    return {
      modal1: false,
      modal2: false,
      loading: true,
      bcDatas: [
        {
          title: '系统管理',
        },
        {
          title: '功能管理',
        },
      ],
    };
  },
  methods: {
    getTreeData(datas, up) {
      let aobj = [];
      aobj = datas.filter(item => (item.UPFUNCID || '') === up);
      // 按 FUNCCODE + SORTCODE 排序
      aobj.sort(function(a, b) {
        let codeA = (a.FUNCCODE || '') + '';
        let codeB = (b.FUNCCODE || '') + '';
        if (codeA < codeB) return -1;
        if (codeA > codeB) return 1;
        let sortA = a.SORTCODE || 0;
        let sortB = b.SORTCODE || 0;
        return sortA - sortB;
      });
      aobj.forEach(element => {
        let tobj = this.getTreeData(datas, element.ID);
        if (tobj.length > 0) {
          element.children = tobj;
        }
      });
      console.log('aobj', aobj);
      return aobj;
    },
    hasClass(obj, cls) {
      var cls = cls || '';
      if (cls.replace(/\s/g, '').length == 0) {
        return false; // 当cls没有参数时,返回false;
      } else {
        return new RegExp(' ' + cls + '').test(' ' + obj.className);
      }
    },
    add() {
      // this.$store.dispatch(`${Constants.STORE_NAME}/add`);
      this.$callAction({ action: `${Constants.STORE_NAME}/add`, timeOut: 0 });
      this.modal2 = true;
    },
    query() {
      this.$callAction({ action: `${Constants.STORE_NAME}/query`, timeOut: 0 });
    },
    clickRow(row, $event) {
      console.log(
        '$event.srcElement.querySelector(".h-table-tree-icon")',
        $event.srcElement.querySelector('.h-table-tree-icon')
      );
      if (
        $event.srcElement.querySelector('.h-table-tree-icon') ||
        this.hasClass($event.srcElement, 'h-table-tree-icon')
      ) {
        return;
      }
      this.$callAction({ action: `${Constants.STORE_NAME}/open`, param: { ID: row.ID }, isBusy: false });
      this.modal2 = true;
    },
    renderFuncName(data) {
      return `<i class="${data.FUNCICON}"></i>${data.FUNCNAME}`;
    },
  },
  async mounted() {
    // eslint-disable-next-line no-restricted-syntax
    await this.$store.dispatch('app/initScms', ['VSS_FUNC']);
    this.columns4 = Gen.getTableColumns(this.$store.state.app.scms['VSS_FUNC'], {});
    this.query();
  },
};
</script>

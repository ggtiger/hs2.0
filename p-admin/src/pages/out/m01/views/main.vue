<template>
  <list-t02
    :title="$route.meta.title"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    :showQuery="showQuery"
    ref="list"
  >
    <rs-modal ref="mpdf">
      <rs-print-pdf :src="pdfSrc" type="preview"></rs-print-pdf>
    </rs-modal>
    <template slot="body-query">
      <Row :space="9">
        <Cell width="16">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width: 60px">委托单号</label>
            <input type="text" class="rr-flex-1" v-model="WTCODE" />
          </div>
        </Cell>
        <Cell width="16">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width: 60px">送检人手机</label>
            <input type="text" class="rr-flex-1" v-model="SLINKER" />
          </div>
        </Cell>
        <Cell width="8">
          <div style="width: 100%; text-align: right; padding-right: 10px">
            <Button class="ml5" @click="advQuery">查询</Button>
          </div>
        </Cell>
      </Row>
    </template>
    <template slot="header-action">
      <Button class="ml5" @click="showQuery = !showQuery">高级查询</Button>
    </template>
    <TableItem title="操作" :width="100" align="center" fixed="right" slot="table-action">
      <template slot-scope="{data}">
        <button
          v-if="data.CERTID"
          class="h-btn h-btn-s h-btn-blue"
          @click.stop="print(data)"
        >查看证书</button>
        <label v-else>
          证书未生成
        </label>
      </template>
    </TableItem>
  </list-t02>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
// eslint-disable-next-line no-restricted-imports
import db from '@/api/db';
export default {
  name: 'out-m01-main',
  components: {},
  computed: {
    ...mapDateTable('QQRY', ['WTCODE', 'SLINKER']),
  },
  data() {
    return {
      CDID: '',
      showQuery: false,
      citem: {},
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [
        {
          title: '检测进度查询',
        },
      ],
      pdfSrc: '',
    };
  },

  methods: {
    add() {
      this.CDID = '';
      this.$refs.madd.show();
    },
    print(item) {
      this.pdfSrc = db.getUrl('pdfsy') + item.CERTID;
      this.$refs.mpdf.show();
    },
    clickRow(row) {
      this.CDID = row.ID;
      this.$refs.madd.show();
    },
    listAction(action, param) {
      switch (action) {
        case 'add':
          this.add(param);
          break;
        case 'uiset':
          this.clickUiSet(param);
          break;
        default:
          break;
      }
    },
    endisable(row, $event) {
      this.$callAction({
        action: `${Constants.STORE_NAME}/endisable`,
        param: { item: row },
        successText: '操作成功',
      });
    },
    advQuery(param) {
      this.$refs.list.advQuery();
    },
  },
  mounted(){
    this.WTCODE = this.$route.query.WTCODE;
    this.SLINKER = this.$route.query.SLINKER;
    this.advQuery();
  }
};
</script>

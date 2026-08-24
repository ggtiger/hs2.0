<template>
  <list-t01
    :title="$route.meta.title"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    :showQuery="showQuery"
    addper="LIB_M01/A04"
    expper="LIB_M01/A09"
    ref="list"
  >
    <rs-modal ref="madd">
      <rsAdd :storeName="store.Constants.STORE_NAME" :title="$route.meta.title" :ID="CDID"></rsAdd>
    </rs-modal>
    <template slot="body-query">
      <Row :space="9">
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">单位编码</label>
            <input type="text" class="rr-flex-1" v-model="DCUSTCODE" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">单位名称</label>
            <input type="text" class="rr-flex-1" v-model="DCUSTNAME" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">联系人</label>
            <input type="text" class="rr-flex-1" v-model="LINKER" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">联系电话</label>
            <input type="text" class="rr-flex-1" v-model="MOBILE" />
          </div>
        </Cell>

        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">省份</label>
            <input type="text" class="rr-flex-1" v-model="PROVINCENAME" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">城市</label>
            <input type="text" class="rr-flex-1" v-model="CITYNAME" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">县区</label>
            <input type="text" class="rr-flex-1" v-model="COUNTYNAME" />
          </div>
        </Cell>
        <Cell width="2">
          <div style="width:100%;text-align:right;padding-right:10px">
            <Button class="ml5" @click="advQuery">查询</Button>
          </div>
        </Cell>
      </Row>
    </template>
    <template slot="header-action">
      <Button class="ml5" @click="showQuery=!showQuery">高级查询</Button>
      <Button color="primary" v-per="'LIB_M01/A04'" icon="h-icon-plus" @click="add">添加</Button>
    </template>
  </list-t01>
</template>
<script>
import rsAdd from './add.vue';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 'b01-m011-main',
  components: {
    rsAdd,
  },
  computed: {
    ...mapDateTable('QQRY', [
      'DCUSTCODE',
      'DCUSTNAME',
      'LINKER',
      'MOBILE',
      'PROVINCENAME',
      'CITYNAME',
      'COUNTYNAME',
      'CUSTTYPE',
    ]),
  },
  data() {
    return {
      CDID: '',
      showQuery: false,
      citem: {},
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [
        {
          title: '系统管理',
        },
        {
          title: this.$route.meta.title,
        },
      ],
    };
  },

  methods: {
    add() {
      this.CDID = '';
      this.$refs.madd.show();
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
};
</script>

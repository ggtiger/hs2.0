<template>
  <list-t01
    title="物流管理"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    :showQuery="showQuery"
    :checkbox="true"
    @list-select="selectRow"
    ref="list"
  >
    <rs-modal ref="madd">
      <rsAdd :storeName="store.Constants.STORE_NAME" title="物流信息" :ID="CDID"></rsAdd>
    </rs-modal>
    <template slot="footer-action">
      <Button color="primary" icon="h-icon-plus" @click="add">添加物流</Button>
    </template>
  </list-t01>
</template>
<script>
import rsAdd from './add.vue';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 'r02-m07-main',
  components: {
    rsAdd,
  },
  computed: {
    ...mapDateTable('QQRY', ['INPUT']),
  },
  data() {
    return {
      CDID: '',
      citem: {},
      showQuery: false,
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [
        { title: '记录/报告管理' },
        { title: '物流管理' },
      ],
      checks: [],
    };
  },
  methods: {
    // 确保 R02_M07 模块配置和 scm（字段配置）已加载，
    // 否则 rs-form-edit 的 created 钩子读 app.scms 会报错
    async ensureModuleLoaded() {
      if (!this.$store.state['app'].modules['R02_M07']) {
        await this.$store.dispatch('app/initModule', 'R02_M07');
      }
      let modData = this.$store.state['app'].modules['R02_M07'];
      if (modData && modData.MODPATH) {
        let resNames = [...new Set(modData.MODPATH.map(p => p.RESOURCENAME))];
        await this.$store.dispatch('app/initScms', resNames);
      }
    },
    async add() {
      this.CDID = '';
      await this.ensureModuleLoaded();
      this.$refs.madd.show();
    },
    async clickRow(row) {
      this.CDID = row.ID;
      await this.ensureModuleLoaded();
      this.$refs.madd.show();
    },
    selectRow(checks) {
      this.checks = checks;
    },
    simpleQuery() {
      this.$refs.list.query();
    },
  },
};
</script>

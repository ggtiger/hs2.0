<template>
  <list-t01
    title="升级管理"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
  >
    <div slot="list-toolbar">
      <button class="h-btn h-btn-primary" @click="goImport">导入升级包</button>
    </div>
    <TableItem title="操作" :width="180" align="center" fixed="right" slot="table-action">
      <template slot-scope="{data}">
        <button
          class="h-btn h-btn-s h-btn-blue"
          @click.stop="goDetail(data)"
        >详情</button>
        <button
          class="h-btn h-btn-s h-btn-red"
          v-per="'RS_MAIDEVUPG/A04'"
          @click.stop="del(data)"
        >删除</button>
      </template>
    </TableItem>
  </list-t01>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 's01-mAIDevUPG-main',
  components: {},
  computed: {},
  data() {
    return {
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [
        { title: '系统管理' },
        { title: '升级管理' },
      ],
    };
  },

  methods: {
    clickRow(row) {
      this.goDetail(row);
    },
    listAction(action, param) {
      switch (action) {
        default:
          break;
      }
    },
    goImport() {
      this.$router.push('/s01/mAIDevUPG/import');
    },
    goDetail(row) {
      this.$router.push(`/s01/mAIDevUPG/detail/${row.ID}`);
    },
    del(row) {
      this.$confirm('确认删除该升级记录？').then(() => {
        this.$callAction({
          action: `${Constants.STORE_NAME}/delete`,
          param: { item: row },
          successText: '删除成功',
        });
      });
    },
  },
};
</script>

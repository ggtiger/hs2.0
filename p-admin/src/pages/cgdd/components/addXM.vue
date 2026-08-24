<template>
  <div class="h-panel h-panel-no-border rr-flex-col">
    <div class="h-panel-bar">
      <span class="h-panel-title">选择项目</span>
      <div class="h-panel-right">
        <Button color="primary" @click="close">确定</Button>
      </div>
    </div>
    <div class="h-panel-body rr-flex-1">
      <Table :datas="list" ref="table" :height="200" @select="onselect" checkbox selectWhenClickTr>
        <TableItem title="编码" prop="code" :width="100"></TableItem>
        <TableItem title="项目名称" prop="name" align="center" :width="200"></TableItem>
        <TableItem title="说明" prop="shuoming"></TableItem>
      </Table>
    </div>
  </div>
</template>
<script>
export default {
  name: 'addXm',
  props: {},
  components: {},
  data() {
    return {
      list: [],
      data: [],
    };
  },
  computed: {},
  watch: {},
  mounted() {
    this.$nextTick(function() {
      this.getList();
    });
  },
  methods: {
    getList() {
      let data = [
        { code: 'G0101', name: '股份公司', shuoming: '没有说明' },
        { code: 'G010102', name: '安徽百味露酒有限公司' },
        { code: 'G010103', name: '安徽源清环保有限公司' },
        { code: 'G010104', name: '安徽瑞思威尔有限公司' },
      ];
      this.list = data;
      // this.gongsi.isShowNext = gongsiData.length === this.gongsi.pageSize;
    },
    onselect(data) {
      this.data = data;
    },
    close() {
      this.$emit('select', this.data);
    },
    // 分页的变换修改
    changeDate(value) {
      console.log(value);
    },
  },
};
</script>

<style lang="less" scoped>
</style>

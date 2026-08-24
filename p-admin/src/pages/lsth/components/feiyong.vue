<template>
  <div class="app-header rr-flex-col">
    <div class="rr-table-header">
      <Button color="primary" icon="h-icon-plus" @click="add">选入</Button>
      <Button color="primary" icon="h-icon-minus" @click="del">移除</Button>
      <Button color="primary" icon="h-icon-trash" @click="datas=[]">清空</Button>
    </div>
    <div class="rr-flex-1 rr-overflow-hidden" ref="tableH">
      <Table
        :datas="datas"
        ref="table"
        :height="tableH"
        @select="onselect"
        checkbox
        selectWhenClickTr
        :style="{height:tableH1+'px'}"
      >
        <TableItem title="ID" prop="id" align="center" :width="80" fixed="left"></TableItem>
        <TableItem title="age" prop="age" :width="150"></TableItem>
        <TableItem title="address" prop="address" align="center" :width="150"></TableItem>
        <TableItem title="name" prop="name" :width="150"></TableItem>
        <TableItem title="age" prop="age" :width="150"></TableItem>
        <TableItem title="address" prop="address" align="center" :width="150"></TableItem>
        <TableItem title="name" prop="name" :width="150"></TableItem>
        <TableItem title="age" prop="age" :width="150"></TableItem>
        <TableItem title="address" prop="address" align="center" :width="150"></TableItem>
        <TableItem title="operating" align="center" :width="120" fixed="right">
          <template slot-scope="{data}">
            <button class="h-btn h-btn-s h-btn-red" @click="remove(datas, data)">
              <i class="h-icon-trash"></i>
            </button>
            <button class="h-btn h-btn-s h-btn-primary" @click="edit( data)">
              <i class="h-icon-edit"></i>
            </button>
          </template>
        </TableItem>
        <div slot="empty" :style="{height:tableH-40+'px'}">Custom reminder: no data at this time</div>
      </Table>
    </div>
    <Pagination v-model="pagination" align="left" @change="changeDate" style="padding-top:20px;"></Pagination>
  </div>
</template>
<script>
export default {
  name: 'feiyong',
  props: {
    pageStatus: {
      type: Number,
      default: 1,
    },
  },
  components: {},
  data() {
    return {
      datas: [], // 表格中数据
      tableH: 200, // 表格默认高度
      tableH1: 200,
      selectTable: [], // 表格选中的数据可以多行
      pagination: {
        // 分页
        page: 1,
        size: 10,
        total: 0,
      },
      columns: [
        { title: 'id', type: 'text', prop: 'id', align: 'center', width: '80', fixed: 'left' },
        { title: 'age', type: 'text', prop: 'id', width: '150' },
        { title: 'address', type: 'text', prop: 'address', align: 'center', width: '150' },
        { title: 'name', type: 'text', prop: 'name', align: 'center', width: '150' },
        { title: 'operating', type: 'text', prop: 'id', align: 'center', width: '120', fixed: 'right' },
      ],
    };
  },
  computed: {},
  watch: {
    datas: {
      handler(val) {
        this.pagination.total = val.length;
      },
      immediate: true,
      deep: true,
    },
  },
  mounted() {
    this.$nextTick(function() {
      this.getTable(); // 初始化表格数据
      this.initTableH(); // 初始化表格高度
    });
  },
  methods: {
    getTable() {
      let datas = [
        { id: 1, name: 'Test 5', age: 12, address: 'Shanghai' },
        { id: 2, name: 'Test 6', age: 12, address: 'Shanghai' },
        { id: 3, name: 'Test 7', age: 12, address: 'Shanghai' },
        { id: 4, name: 'Test 5', age: 12, address: 'Shanghai' },
        { id: 5, name: 'Test 6', age: 12, address: 'Shanghai' },
        { id: 6, name: 'Test 7', age: 12, address: 'Shanghai' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai' },
        { id: 8, name: 'Test 5', age: 12, address: 'Shanghai' },
        { id: 9, name: 'Test 6', age: 12, address: 'Shanghai' },
        { id: 10, name: 'Test 7', age: 12, address: 'Shanghai' },
        { id: 11, name: 'Test 7', age: 12, address: 'Shanghai' },
        { id: 12, name: 'Test 7', age: 12, address: 'Shanghai' },
        { id: 13, name: 'Test 5', age: 12, address: 'Shanghai' },
        { id: 14, name: 'Test 6', age: 12, address: 'Shanghai' },
        { id: 15, name: 'Test 7', age: 12, address: 'Shanghai' },
      ];
      this.datas = datas;
    },
    initTableH() {
      let height = this.$refs.tableH.clientHeight || 0;
      if (height > this.tableH) {
        this.tableH = height - 40;
      }
      this.tableH1 = height;
    },
    // 选入
    add() {},
    // 移除
    del() {
      let selectTable = this.selectTable;
      selectTable.forEach(item => {
        if (this.datas.indexOf(item) !== -1) {
          this.datas.splice(this.datas.indexOf(item), 1);
        }
      });
    },
    // 删除表格中的一条数据
    remove(datas, data) {
      this.datas.splice(this.datas.indexOf(data), 1);
    },
    edit(data) {},
    onselect(data, event) {
      this.selectTable = data;
    },
    onRowClick() {
      console.log('!');
    },
    // 分页的变换修改
    changeDate(value) {
      console.log(value);
    },
  },
};
</script>

<style lang="less" scoped>
@import '~heyui/themes/index.less';
</style>

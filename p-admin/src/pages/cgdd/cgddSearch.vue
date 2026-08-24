<template>
  <div class="h-panel h-panel-no-border rr-flex-col">
    <div class="h-panel-bar">
      <span class="h-panel-title">
        <Breadcrumb :datas="routeDatas"></Breadcrumb>
      </span>
      <div class="h-panel-right">
        <Button color="primary" icon="h-icon-check" @click="daochu">打印</Button>
        <Button color="primary" icon="h-icon-inbox" @click="daochu">导入</Button>
        <Button color="primary" icon="h-icon-outbox" @click="daochu">导出</Button>
      </div>
    </div>
    <div class="h-panel-body rr-flex-1">
      <div class="rr-flex-col">
        <Form
          :label-width="110"
          mode="threecolumn"
          :model="data"
          ref="form"
          :top="0.2"
          showErrorTip
        >
          <FormItem label="退货仓库" prop="inputData">
            <div>
              <AutoComplete
                :option="param"
                v-model="data.tuihuocangku"
                @change="onChange"
                type="title"
                placeholder="请输入退货仓库"
              ></AutoComplete>
            </div>
          </FormItem>
          <FormItem label="部门" prop="inputData">
            <div>
              <AutoComplete
                :option="param"
                v-model="data.bumen"
                @change="onChange"
                type="title"
                placeholder="请输入部门"
              ></AutoComplete>
            </div>
          </FormItem>
          <FormItem label="单据状态" prop="select2Data">
            <Select v-model="data.danjuzhuangtai" :datas="status"></Select>
          </FormItem>
          <FormItem label="单据日期" ref="datapicker" prop="dateData">
            <DateRangePicker v-model="data.danjuriqi"></DateRangePicker>
          </FormItem>
          <FormItem label="退货确认状态" prop="select2Data">
            <Select v-model="data.tuihuozhuangtai" :datas="status1"></Select>
          </FormItem>
          <template ref="searchMore" v-if="searchMore">
            <FormItem label="退货仓库" prop="inputData">
              <div>
                <AutoComplete
                  :option="param"
                  v-model="data.tuihuocangku"
                  @change="onChange"
                  type="title"
                  placeholder="请输入退货仓库"
                ></AutoComplete>
              </div>
            </FormItem>
            <FormItem label="部门" prop="inputData">
              <div>
                <AutoComplete
                  :option="param"
                  v-model="data.bumen"
                  @change="onChange"
                  type="title"
                  placeholder="请输入部门"
                ></AutoComplete>
              </div>
            </FormItem>
            <FormItem label="单据状态" prop="select2Data">
              <Select v-model="data.danjuzhuangtai" :datas="status"></Select>
            </FormItem>
          </template>
          <FormItem :showLabel="false" class="rr-text-center">
            <Button color="primary" @click="submit">查询</Button>
            <Button
              color="primary"
              @click="searchMore=!searchMore"
              v-text="searchMore===true?'收起':'更多'"
            ></Button>
          </FormItem>
        </Form>
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
            <div slot="empty">Custom reminder: no data at this time</div>
          </Table>
        </div>
        <table-tool-bar v-model="pagination" @change="changeDate">
          <div>
            <Button color="primary" icon="h-icon-plus" @click="daochu">添加</Button>
            <Button color="red" icon="h-icon-trash" @click="daochu">删除</Button>
          </div>
        </table-tool-bar>
      </div>
    </div>
  </div>
</template>

<script>
import jsonp from 'fetch-jsonp';
const loadData = function(filter, callback) {
  jsonp(`https://suggest.taobao.com/sug?code=utf-8&q=${filter}`)
    .then(response => response.json())
    .then(d => {
      callback(
        d.result.map(r => {
          return {
            name: r[0],
            code: r[1] + Math.random(),
          };
        })
      );
    });
};
export default {
  data() {
    return {
      routeDatas: [
        {
          icon: 'h-icon-home',
          route: { name: 'wodezhuye' },
        },
        {
          title: '采购订单查询',
        },
      ],
      searchMore: false,
      data: {
        intData: null,
        tuihuocangku: '',
        bumen: '',
        danjuriqi: {},
        danjuzhuangtai: '',
        tuihuozhuangtai: '',
      },
      param: {
        keyName: 'code',
        titleName: 'name',
        orgId: 1, // 自定义参数传递
        loadData,
        minWord: 1,
      },
      status: {
        0: '制单',
        1: '审核',
        2: '关闭',
      },
      status1: {
        0: '已确认',
        1: '未确认',
      },
      isLoading: false,
      modeParam: {
        single: '一个区块一行',
        twocolumn: '两列一行',
        threecolumn: '三列一行',
        block: '标题独立一行',
      },
      isInputAsyncError: false,
      datas: [], // 表格中数据
      tableH: 200, // 表格默认高度
      tableH1: 200,
      pagination: {
        // 分页
        page: 1,
        size: 10,
        total: 0,
      },
    };
  },
  watch: {},
  mounted() {
    this.$nextTick(function() {
      this.getTable();
      this.initTableH();
    });
  },
  methods: {
    initTableH() {
      let height = this.$refs.tableH.clientHeight;
      if (height > this.tableH) {
        this.tableH = height - 40;
      }
      this.tableH1 = height;
    },
    loadData() {
      let datas = [];
      return datas;
    },
    submit() {
      this.$Message('提交成功');
    },
    submitMore() {
      this.searchMore = true;
    },
    open() {
      this.$Modal({
        title: '处理',
        content: '我要去做特殊的处理',
      });
    },
    // 初始化数据
    getTable() {
      let datas = [
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai' },
      ];
      this.datas = datas;
      this.pagination.total = datas.length;
      console.log(this.pagination);
    },
    onselect(data) {
      // this.$router.push({ name: 'lsthAdd1', query: { item: data } });
    },
    daochu() {
      console.log('导出');
    },
    // 分页的变换修改
    changeDate(value) {
      console.log(value);
    },
    onChange(data, trigger) {
      console.log(data, trigger);
    },
  },
};
</script>
<style lang="less" scoped>
</style>

<template>
  <div class="h-panel h-panel-no-border rr-flex-col">
    <div class="h-panel-bar">
      <span class="h-panel-title">
        <Breadcrumb :datas="routeDatas"></Breadcrumb>
      </span>
    </div>
    <div class="h-panel-body rr-flex-1">
      <div class="rr-flex-col">
        <transition name="fade">
          <Form
            :label-width="110"
            mode="threecolumn"
            :model="data"
            ref="form"
            :top="0.2"
            showErrorTip
            v-if="isSearch"
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
            <FormItem label="单据日期" ref="datapicker" prop="dateData">
              <DateRangePicker v-model="data.danjuriqi"></DateRangePicker>
            </FormItem>
            <FormItem label="单据状态" prop="select2Data">
              <Select v-model="data.danjuzhuangtai" :datas="status"></Select>
            </FormItem>
            <FormItem label="退货确认状态" prop="select2Data">
              <Select v-model="data.tuihuozhuangtai" :datas="status1"></Select>
            </FormItem>
            <FormItem :showLabel="false" class="rr-text-center">
              <Button color="primary" @click="submit">查询</Button>
              <Button color="primary" @click="searchMore=true">高级查询</Button>
            </FormItem>
          </Form>
        </transition>
        <Modal v-model="searchMore" :middle="true">
          <div slot="header">Vue</div>
          <div style="padding:20px">
            <Form
              :label-width="110"
              mode="twocolumn"
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
              <FormItem label="单据日期" ref="datapicker" prop="dateData">
                <DateRangePicker v-model="data.danjuriqi"></DateRangePicker>
              </FormItem>
              <FormItem label="单据状态" prop="select2Data">
                <Select v-model="data.danjuzhuangtai" :datas="status"></Select>
              </FormItem>
              <FormItem label="退货确认状态" prop="select2Data">
                <Select v-model="data.tuihuozhuangtai" :datas="status1"></Select>
              </FormItem>
            </Form>
          </div>
          <div slot="footer">
            <Button color="primary" @click="modalConfirm">确认</Button>
            <Button color="red" @click="searchMore=false">关闭</Button>
          </div>
        </Modal>
        <div class="rr-flex-row rr-table-header1">
          <div class="rr-flex-1">
            <Button color="primary" icon="h-icon-plus" @click="add">添加</Button>
            <Button color="primary" icon="h-icon-minus" @click="del">移除</Button>
            <Button color="primary" icon="h-icon-trash" @click="datas=[]">删除</Button>
          </div>
          <div class="h-btn-group">
            <button class="h-btn" @click="isSearch=!isSearch">
              <i class="h-icon-search"></i>
            </button>
            <button class="h-btn">
              <i class="h-icon-refresh"></i>
            </button>
            <DropdownCustom class="h-btn" button placement="bottom-end">
              <i class="h-icon-menu"></i>
              <div slot="content" v-width="143" v-height="200" style="overflow:auto">
                <div v-for="(item,index) in colTable" :key="index" style="padding:5px 10px">
                  <Checkbox v-model="checkTable" @change="change" :value="item.prop">{{item.title}}</Checkbox>
                </div>
              </div>
            </DropdownCustom>
          </div>
        </div>
        <div class="rr-flex-1 rr-overflow-hidden" ref="tableH">
          <Table :datas="datas" ref="table" @select="onselect" checkbox selectWhenClickTr>
            <template v-for="(item,index) in colTable">
              <TableItem
                :key="index"
                :title="item.title"
                :prop="item.prop"
                :align="item.align"
                :width="item.width"
                :fixed="item.fixed"
                v-if="item.isShow"
              ></TableItem>
            </template>
            <TableItem title="operating" align="center" :width="80">
              <template slot-scope="{data}">
                <button class="h-btn h-btn-s h-btn-red" @click="remove(datas, data)">
                  <i class="h-icon-trash"></i>
                </button>
              </template>
            </TableItem>
            <div slot="empty">Custom reminder: no data at this time</div>
          </Table>
        </div>
        <table-tool-bar v-model="pagination" @change="changeDate"></table-tool-bar>
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
          title: '零售退货查询',
        },
      ],
      searchMore: false,
      isSearch: true,
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
        pagerSize: 2,
      },
      colTable: [
        { title: 'ID', prop: 'id', align: 'center', width: 60, fixed: 'left', isShow: true },
        { title: '年龄', prop: 'age', align: 'center', width: 60, isShow: true },
        { title: '客户', prop: 'name', align: 'center', width: 100, isShow: true },
        { title: '部门', prop: 'bumen', align: 'center', width: 150, isShow: true },
        { title: '单据状态', prop: 'danjuzhuangtai', align: 'center', width: 100, isShow: true },
        { title: '地址', prop: 'address', align: 'center', width: 150, isShow: true },
      ],
    };
  },
  watch: {
    isSearch(val) {
      this.initTableH();
    },
  },
  computed: {
    checkTable: {
      get() {
        let checked = [];
        this.colTable.forEach(function(item) {
          if (item.isShow) {
            checked.push(item.prop);
          }
        });
        return checked;
      },
      set() {},
    },
  },
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
        this.tableH = height - 42;
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
    modalConfirm() {
      this.searchMore = false;
      this.getTable();
      this.initTableH();
    },
    open() {
      this.$Modal({
        title: '处理',
        content: '我要去做特殊的处理',
      });
    },
    change(val) {
      this.colTable.forEach(function(item) {
        var isCheck = val.find(function(value) {
          return value === item.prop;
        });
        if (!isCheck) {
          item.isShow = false;
        } else {
          item.isShow = true;
        }
      });
      this.initTableH();
    },
    // 初始化数据
    getTable() {
      let datas = [
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 6, name: 'Test 6', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 7, name: 'Test 7', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
        { id: 5, name: 'Test 5', age: 12, address: 'Shanghai', bumen: '后勤部', danjuzhuangtai: '制单' },
      ];
      this.datas = datas;
      this.pagination.total = datas.length;
    },
    // 删除表格中的一条数据
    remove(datas, data) {
      datas.splice(datas.indexOf(data), 1);
      this.datas = datas;
      this.pagination.total = datas.length;
    },
    onselect(data) {
      // this.$router.push({ name: 'lsthAdd1', query: { item: data } });
    },
    add() {
      console.log('add');
    },
    del() {
      console.log('del');
    },
    daochu() {
      console.log('daochu');
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
.rr-table-header1 {
  margin-bottom: 10px;
}
.h-table {
  height: calc(100% - 20px);
  /deep/ .h-table-container {
    height: calc(100% - 40px);
    overflow-y: auto;
  }
}
</style>

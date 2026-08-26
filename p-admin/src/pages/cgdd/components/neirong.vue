<template>
  <div>
    <Form
      :label-width="110"
      mode="threecolumn"
      :model="data"
      :rules="validationRules"
      ref="form"
      :top="0.2"
      showErrorTip
    >
      <FormItem label="业务类型" prop="inputData">
        <input type="text" v-model="data.yewuleixing" :disabled="pageStatus===1" />
      </FormItem>
      <FormItem label="对象性质" prop="inputData">
        <input type="text" v-model="data.duixiangxingzhi" :disabled="pageStatus===1" />
      </FormItem>
      <FormItem label="填报日期" prop="dateData">
        <DatePicker v-model="data.tianbaoriqi" type="date" :disabled="pageStatus===1"></DatePicker>
      </FormItem>
      <FormItem label="公司" single>
        <Select
          v-model="data.gongsi"
          :datas="gongsi.list"
          :filterable="true"
          @change="onselect"
          keyName="id"
          titleName="gongsi"
          :disabled="pageStatus===1"
        >
          <template slot="show" slot-scope="{value}">自定义展示: {{value}}</template>
          <template slot="top">
            <div
              class="text-center"
              v-show="gongsi.pageIndex>1"
              @click.native="addGongsi('prev')"
            >上一页</div>
          </template>
          <template slot-scope="{item}" slot="item">
            <div>
              {{item.code}}
              {{item.gongsi}}
            </div>
          </template>
          <template slot="bottom">
            <div
              class="text-center"
              v-show="gongsi.isShowNext"
              @click.native="addGongsi('next')"
            >下一页</div>
          </template>
        </Select>
      </FormItem>
      <FormItem label="部门" single>
        <Select
          v-model="data.bumen"
          :datas="bumen.list"
          :filterable="true"
          @change="onselect"
          keyName="id"
          titleName="name"
          :disabled="pageStatus===1"
        >
          <template slot="show" slot-scope="{value}">自定义展示: {{value}}</template>
          <template slot="top">
            <div class="text-center" v-show="bumen.pageIndex>1" @click.native="addBumen('prev')">上一页</div>
          </template>
          <template slot-scope="{item}" slot="item">
            <div>
              {{item.code}}
              {{item.name}}
              {{item.shangji}}
            </div>
          </template>
          <template slot="bottom">
            <div class="text-center" v-show="bumen.isShowNext" @click.native="addBumen('next')">下一页</div>
          </template>
        </Select>
      </FormItem>
      <FormItem label="说明" :single="true">
        <textarea
          rows="3"
          v-autosize
          v-wordcount="150"
          v-model="data.shuoming"
          :disabled="pageStatus===1"
        ></textarea>
      </FormItem>
    </Form>
    <div>
      <div class="rr-table-header">
        <Button color="primary" icon="h-icon-plus" @click="openModal=true" :disabled="pageStatus===1">添加</Button>
        <Button color="primary" icon="h-icon-minus" @click="del" :disabled="pageStatus===1">移除</Button>
        <Button color="primary" icon="h-icon-trash" @click="datas=[]" :disabled="pageStatus===1">删除</Button>
      </div>
      <Table
        :datas="data.mxxm"
        ref="table"
        :height="200"
        @select="onselect"
        checkbox
        selectWhenClickTr
      >
        <TableItem title="项目" prop="xiangmu" :width="150"></TableItem>
        <TableItem title="填报金额" prop="tianbaojine" align="center" :width="150"></TableItem>
        <TableItem title="核准金额" prop="hezhunjine" :width="150"></TableItem>
        <TableItem title="说明" prop="shuoming"></TableItem>
      </Table>
      <Modal v-model="openModal">
        <addXm @select="select"></addXM>
      </Modal>
    </div>
  </div>
</template>
<script>
import addXm from './addXM';
export default {
  name: 'neirong',
  props: {
    pageStatus: {
      type: Number,
      default: 1,
    },
  },
  components: {addXm},
  data() {
    return {
      openModal: false,
      data: {
        yewuleixing: '',
        duixiangxingzhi: '',
        tianbaoriqi: '',
        gongsi: '',
        bumen: '',
        shuoming: '',
        mxxm: [],
      },
      gongsi: {
        list: [], // 公司下拉中数据
        pageIndex: 1, // 公司下拉中页数
        pageSize: 10, // 公司下拉列表一页条数
        isShowNext: false, // 是否显示加载下一页
      },
      bumen: {
        // 部门
        list: [],
        pageIndex: 1,
        pageSize: 10,
        isShowNext: false,
      },
      validationRules: {
        rules: {
          textareaData: {
            maxLen: 50,
            minLen: 10,
          },
          inputData: {
            // 这里的判断不会影响最终的valid结果，所以也可以作为一些验证提示
            validAsync(value, next, parent, data) {
              setTimeout(() => {
                if (value === '15') {
                  next();
                } else {
                  next('ID不等于15');
                }
              }, 1000);
            },
          },
        },
        required: [
          'autocompleteData',
          'select2Data',
          'select3Data',
          'inputsData[].value',
          'inputData',
          'radioData',
          'rateData',
          'checkboxData',
          'moneyData',
          'dateData',
          'taginputsData',
          'money.minData',
          'money.maxData',
          'intData',
          'numberData',
          'urlData',
          'emailData',
          'telData',
          'mobileData',
          'textareaData',
        ],
        int: ['intData'],
        number: ['numberData', 'money.minData', 'money.maxData'],
        url: ['urlData'],
        email: ['emailData'],
        tel: ['telData'],
        mobile: ['mobileData'],
        combineRules: [
          {
            parentRef: 'money',
            refs: ['minData', 'maxData'],
            valid: {
              valid: 'lessThan',
              message: '起始金额不能大于结束金额',
            },
          },
        ],
      },
    };
  },
  computed: {},
  watch: {},
  mounted() {
    this.$nextTick(function() {
      this.initData();
      this.getGongsi(this.gongsi.pageIndex);
      this.getBumen(this.bumen.pageIndex);
    });
  },
  methods: {
    initData() {},
    // 加载更多公司信息
    addGongsi(val) {
      if (val === 'next') {
        this.gongsi.pageIndex++;
      } else {
        this.gongsi.pageIndex--;
      }
      this.getGongsi(this.gongsi.pageIndex);
    },
    getGongsi(pageIndex) {
      let gongsiData = [
        { id: pageIndex + '1', code: 'G0101', gongsi: '股份公司' },
        { id: pageIndex + '2', code: 'G010102', gongsi: '安徽百味露酒有限公司' },
        { id: pageIndex + '3', code: 'G010103', gongsi: '安徽源清环保有限公司' },
        { id: pageIndex + '4', code: 'G010104', gongsi: '安徽瑞思威尔有限公司' },
      ];
      this.gongsi.list = gongsiData;
      this.gongsi.isShowNext = gongsiData.length === this.gongsi.pageSize;
    },
    getBumen(pageIndex) {
      let data = [
        { id: pageIndex + '1', code: 'GF', name: '安徽古井贡酒年份原浆有限公司', shangji: '公司' },
        { id: pageIndex + '2', code: 'GF01', name: '股份公司高管人员', shangji: '一级中心' },
        { id: pageIndex + '3', code: 'GF02', name: '董事会秘书处', shangji: '一级中心' },
        { id: pageIndex + '4', code: 'GF03', name: '行政服务中心', shangji: '一级中心' },
        { id: pageIndex + '5', code: 'GF0301', name: '协同部', shangji: '部门' },
        { id: pageIndex + '6', code: 'GF030101', name: '信息研究室', shangji: '部门' },
        { id: pageIndex + '7', code: 'GF030102', name: '档案馆', shangji: '部门' },
        { id: pageIndex + '8', code: 'GF0302', name: '法律事务部', shangji: '部门' },
        { id: pageIndex + '9', code: 'GF0303', name: '后勤部', shangji: '部门' },
        { id: pageIndex + '10', code: 'GF0304', name: '班车队', shangji: '部门' },
      ];
      this.bumen.list = data;
      this.bumen.isShowNext = data.length === this.bumen.pageSize;
    },
    // 添加
    select(data) {
      var _this = this;
      data.forEach(function(item){
        var ar = _this.data.mxxm.find(function(value) {
          return value.xiangmu === item.name
        })
        if(!ar) {
          let xm = {
            xiangmu: item.name,
            tianbaojine: '',
            hezhunjine: '',
            shuoming: '',
          }
          _this.data.mxxm.push(xm)
        }

      })
      this.openModal = false;
    },
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
    onselect(data) {
      console.log(data);
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
.rr-table-header {
  padding: 10px;
  text-align: right;
}
.scoll{
  height: 100%;
  overflow-y: auto;
}
</style>

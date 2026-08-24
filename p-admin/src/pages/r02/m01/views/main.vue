<template>
  <!-- 检测情况统计表 -->
  <report-t01
    :bcDatas="bcDatas"
    :datas="datas"
    :columns="columns"
    :options="options"
    :initOption="initOption"
    @query="query"
    ref="report"
  >
    <template slot="query">
      <DatePicker v-model="SDATE" placeholder="开始日期" :option="{end:EDATE}"></DatePicker>
      <span>-</span>
      <DatePicker v-model="EDATE" placeholder="结束日期" :option="{start:SDATE}"></DatePicker>
      <Button color="primary" @click.native.stop="query">搜索</Button>
    </template>
  </report-t01>
</template>
<script>
// import db from '@/api/db';
import { dateToString } from 'rs-vcore/utils/Date';
import chart from '@/components/echarts/chart';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 'r02-m01-main',
  components: { chart },
  computed: {
    ...mapDateTable('QQRY', ['SDATE', 'EDATE']),
    ...mapDateTable('QRY', []),
  },
  data() {
    return {
      INPUT: '',
      serchStarDate: {},
      serchEndDate: {},
      bcDatas: [
        {
          title: '报表管理',
        },
        {
          title: '检测情况统计表',
        },
      ],
      initOption: {},
      options: {
        color: ['#77a2dc', '#3b9a9c', '#4bc2c5', '#78fee0'],
        tooltip: {
          trigger: 'axis',
          axisPointer: {
            type: 'shadow',
          },
        },
        legend: {
          right: 0,
          orient: 'vertical',
          formatter: function(name) {
            let arr = name.split(',');
            if (arr[1]) {
              return arr[0] + '\n' + arr[1];
            } else {
              return arr[0];
            }
          },
          data: [],
        },
        grid: {
          right: 200,
          bottom: 100,
          top: 20,
        },
        xAxis: [
          {
            type: 'category',
            data: [],
            axisTick: {
              alignWithLabel: true,
            },
            axisLabel: {
              interval: 0,
              rotate: 45,
            },
          },
        ],
        yAxis: [
          {
            type: 'value',
          },
        ],
        series: [],
      },
      namePrevIndex: 1,
      columns: [
        {
          title: '部门名称',
          prop: 'DEPTNAME',
          attrs(data, index) {
            return {
              rowspan: data.namePrevIndex,
            };
          },
        },
        {
          title: '项目',
          prop: 'STDDNAME',
          width: 200,
          tooltip: { placement: 'top-start', content: '<div class="table-tr-tooltip">项目说明：这是一个项目</div>' },
        },
        {
          title: '收件',
          prop: 'F1',
        },
        {
          title: '检毕',
          prop: 'CN1',
        },
        {
          title: '未检数',
          prop: 'CN2',
        },
        {
          title: '积压数',
          prop: 'CN3',
        },
        {
          title: '完成率',
          prop: 'WCL',
        },
        {
          title: '及时率',
          prop: 'JSL',
        },
        {
          title: '平均检测时长(天)',
          prop: 'TN1',
        },
        {
          title: '应收收入',
          prop: 'AMT',
        },
        {
          title: '实收收入',
          prop: 'RAMT',
        },
        {
          title: '项目累计',
          prop: 'S1',
        },
        {
          title: '部门小计',
          prop: 'S2',
          attrs(data, index) {
            return {
              rowspan: data.namePrevIndex,
            };
          },
        },
        {
          title: '部门累计',
          prop: 'S3',
          attrs(data, index) {
            return {
              rowspan: data.namePrevIndex,
            };
          },
        },
      ],
      datas: [],
    };
  },
  mounted() {
    let date = new Date();
    date.setDate(1);
    this.SDATE = dateToString(date);
    this.EDATE = dateToString(new Date());
  },
  methods: {
    initData() {
      this.datas = this.QRY;
      let nameIndex = 0;
      let length = this.datas.length - 1;
      this.namePrevIndex = 1;
      this.datas.forEach((item, index) => {
        if (index > 0) {
          if (item.DEPTNAME === this.datas[index - 1].DEPTNAME) {
            this.namePrevIndex = this.namePrevIndex + 1;
            this.$set(item, 'namePrevIndex', 0);
            if (length === index) {
              this.$set(this.datas[nameIndex], 'namePrevIndex', this.namePrevIndex);
              this.namePrevIndex = 1;
            }
          } else {
            this.$set(this.datas[nameIndex], 'namePrevIndex', this.namePrevIndex);
            nameIndex = index;
            this.namePrevIndex = 1;
          }
        }
      });

      let fitems = this.datas.filter(f => {
        return f.DEPTNAME === '部门汇总';
      });
      this.options.xAxis[0].data = [];
      let data1 = [];
      let data2 = [];
      let data3 = [];
      let data4 = [];
      fitems.map(f => {
        this.options.xAxis[0].data.push(f.STDDNAME);
        data1.push(f.F1);
        data2.push(f.CN2);
        data3.push(f.RAMT);
        data4.push(f.S3);
      });
      this.options.legend.data = [
        { name: '收件,该标准受理单台件数' },
        { name: '未检数,尚未办结的台件数' },
        { name: '实收收入' },
        { name: '项目累计,该项目今年1月1日来累计收入' },
      ];
      this.options.series = [
        {
          name: '收件,该标准受理单台件数',
          type: 'bar',
          // barWidth: '60%',
          data: data1,
        },
        {
          name: '未检数,尚未办结的台件数',
          type: 'bar',
          // barWidth: '60%',
          data: data2,
        },
        {
          name: '实收收入',
          type: 'bar',
          // barWidth: '60%',
          data: data3,
        },
        {
          name: '项目累计,该项目今年1月1日来累计收入',
          type: 'bar',
          // barWidth: '60%',
          data: data4,
        },
      ];
      this.$refs.report.$refs.charts.init();
    },
    query() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/query`,
        successCall: () => {
          this.initData();
        },
      });
    },
  },
};
</script>
<style scoped>
.f13 {
  font-size: 13px;
}
</style>

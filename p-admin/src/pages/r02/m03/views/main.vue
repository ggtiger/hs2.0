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
      <input type="text" class="rr-flex-1" placeholder="客户名称" v-model="CUSTNAME" />
      <input type="text" class="rr-flex-1" placeholder="显示条数" v-model="SHOWNUM" />
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
  name: 'r02-m03-main',
  components: { chart },
  computed: {
    ...mapDateTable('QQRY', ['SDATE', 'EDATE',"CUSTNAME", 'SHOWNUM', 'PageSize']),
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
          title: '客户统计',
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
          title: '客户名称',
          prop: 'CUSTNAME',
          width: 200,
        },
        {
          title: '联系人',
          prop: 'LINKER',
        },
        {
          title: '联系方式',
          prop: 'MOBILE',
        },
        {
          title: '实收费用',
          prop: 'RAMT1',
        },
        {
          title: '台件数',
          prop: 'F11',
        },
        {
          title: '收费同比去年',
          prop: 'RAMT2',
        },
        {
          title: '收费同比前年',
          prop: 'RAMT3',
        },
        {
          title: '台件数同比去年',
          prop: 'F12',
        },
        {
          title: '台件数同比前年',
          prop: 'F13',
        },
      ],
      datas: [],
    };
  },
  mounted() {
    this.SHOWNUM = 20;
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

      let fitems = this.datas;
      this.options.xAxis[0].data = [];
      let data1 = [];
      let data2 = [];
      let data3 = [];
      let data4 = [];
      fitems.map(f => {
        this.options.xAxis[0].data.push(f.CUSTNAME);
        data1.push(f.RAMT1);
        data2.push(f.RAMT2);
        data3.push(f.RAMT3);
      });
      this.options.legend.data = [{ name: '当年' }, { name: '去年' }, { name: '前年' }];
      this.options.series = [
        {
          name: '当年',
          type: 'bar',
          data: data1,
        },
        {
          name: '去年',
          type: 'bar',
          data: data2,
        },
        {
          name: '前年',
          type: 'bar',
          data: data3,
        },
      ];
      this.$refs.report.$refs.charts.init();
    },
    query() {
      this.PageSize = this.SHOWNUM;
      let t = this.SHOWNUM;
      this.$callAction({
        action: `${Constants.STORE_NAME}/query`,
        successCall: () => {
          this.initData();
          this.SHOWNUM = t;
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

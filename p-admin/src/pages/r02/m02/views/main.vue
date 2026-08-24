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
  name: 'r02-m02-main',
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
          title: '人员效能表',
        },
      ],
      initOption: {},
      options: {
        color: ['#77a2dc', '#3b9a9c', '#4bc2c5', '#78fee0'],
        title: {
          text: '效能系数',
          subtext: '（及时率-错误率）*检毕/平均检测时长',
          left: 'center',
        },
        tooltip: {
          trigger: 'item',
          formatter: '{a} <br/>{b} : {c} ({d}%)',
        },
        legend: {
          orient: 'vertical',
          left: 'left',
          data: [],
        },
        series: [
          {
            name: '访问来源',
            type: 'pie',
            radius: '55%',
            center: ['50%', '60%'],
            data: [],
            emphasis: {
              itemStyle: {
                shadowBlur: 10,
                shadowOffsetX: 0,
                shadowColor: 'rgba(0, 0, 0, 0.5)',
              },
            },
          },
        ],
      },

      namePrevIndex: 1,
      columns: [
        {
          title: '部门',
          prop: 'DEPTNAME',
          attrs(data, index) {
            return {
              rowspan: data.namePrevIndex,
            };
          },
        },
        {
          title: '检验员',
          prop: 'EMPNAME',
          width: 200,
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
          title: '重做数',
          prop: 'CN4',
        },
        {
          title: '平均检测时长(天)',
          prop: 'TN1',
        },
        {
          title: '错误率',
          prop: 'CWL',
        },
        {
          title: '效能系数',
          prop: 'XNXS',
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
      this.options.legend.data = [];
      this.options.series[0].data = [];
      this.datas.map(f => {
        this.options.legend.data.push(f.DEPTNAME + ' ' + f.EMPNAME);
        this.options.series[0].data.push({ name: f.DEPTNAME + ' ' + f.EMPNAME, value: f.XNXS });
      });

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

<template>
  <div class="dashboard">
    <!-- 顶部区域：大数字卡片 + 图表 -->
    <Row :space="16" style="margin-bottom:16px">
      <Cell :width="8">
        <div class="hero-card">
          <div class="hero-card-label">累计数据量(件)</div>
          <div class="hero-card-value">
            <i class="h-icon-complete"></i>
            <span>{{ totalData }}</span>
            <span class="hero-card-trend">+16</span>
          </div>
          <div class="hero-card-stats">
            <div class="hero-stat-item">
              <div class="hero-stat-value">{{ TotalCount || 0 }}</div>
              <div class="hero-stat-label">公告数</div>
            </div>
            <div class="hero-stat-item">
              <div class="hero-stat-value">{{ cmenums.length }}</div>
              <div class="hero-stat-label">常用模块</div>
            </div>
            <div class="hero-stat-item">
              <div class="hero-stat-value">{{ efficiencyScore }}</div>
              <div class="hero-stat-label">效能系数</div>
            </div>
          </div>
        </div>
      </Cell>
      <Cell :width="16">
        <div class="dashboard-card" style="height:100%">
          <div class="dashboard-card-header">
            <span class="dashboard-card-title">效能统计</span>
          </div>
          <div class="dashboard-card-body">
            <chart
              key="chart2"
              ref="chart2"
              width="100%"
              height="220px"
              :options="options2"
              :initOption="initOption"
            ></chart>
          </div>
        </div>
      </Cell>
    </Row>

    <!-- 常用模块区 -->
    <div class="dashboard-card" style="margin-bottom:16px" v-if="cmenums.length > 0">
      <div class="dashboard-card-header">
        <span class="dashboard-card-title">常用模块</span>
        <div class="dashboard-card-actions" v-if="false">
          <span class="action-link" @click="setMenu">
            <i class="h-icon-setting"></i> 设置
          </span>
        </div>
      </div>
      <div class="dashboard-card-body">
        <div class="module-grid">
          <div
            class="module-item"
            v-for="(item,index) in cmenums"
            :key="index"
            @click="link(item.route)"
          >
            <div class="module-icon">
              <span :class="item.icon"></span>
            </div>
            <div class="module-label">{{item.label}}</div>
          </div>
        </div>
      </div>
    </div>

    <!-- 公告 + 分类统计区 -->
    <Row :space="16">
      <Cell :width="14">
        <div class="dashboard-card" style="height:100%">
          <div class="dashboard-card-header">
            <span class="dashboard-card-title">系统公告</span>
            <div class="dashboard-card-actions">
              <span class="action-link" @click="refreshNotice">
                <i class="h-icon-refresh"></i> 刷新
              </span>
              <span class="action-link" @click="getList">
                查看更多 >
              </span>
            </div>
          </div>
          <div class="dashboard-card-body">
            <scroll-notice :list="QRY1" @click="getDetail" />
          </div>
        </div>
      </Cell>
      <Cell :width="10">
        <div class="dashboard-card" style="height:100%">
          <div class="dashboard-card-header">
            <span class="dashboard-card-title">效能分布</span>
          </div>
          <div class="dashboard-card-body">
            <chart
              key="chart3"
              ref="chart3"
              width="100%"
              height="260px"
              :options="options3"
              :initOption="initOption"
            ></chart>
          </div>
        </div>
      </Cell>
    </Row>

    <!-- 弹窗 -->
    <rs-modal ref="setMenu">
      <set-menu
        class="rr-flex-1 rr-scroll-bar"
        style="padding:0 5px;"
        :menus="menus"
        @close="close"
        @ok="ok"
      ></set-menu>
    </rs-modal>
    <rs-modal ref="showDetail">
      <gonggaoDetail
        class="rr-flex-1 rr-scroll-bar"
        style="padding:0 5px;"
        :ID="gonggaoId"
        @close="closeDetail"
      ></gonggaoDetail>
    </rs-modal>
    <rs-modal ref="showList">
      <gonggaoList
        class="rr-flex-1 rr-scroll-bar"
        style="padding:0 5px;"
        @getDetail="getDetail"
        @close="closeList"
      ></gonggaoList>
    </rs-modal>
  </div>
</template>

<script>
import gonggaoDetail from './views/gonggaoDetail.vue';
import gonggaoList from './views/gonggaoList.vue';
import setMenu from './views/setMenu.vue';
import chart from '@/components/echarts/chart';
import ScrollNotice from '@/components/scroll-notice';
import bus from '@/utils/eventbus';
import { mapState, mapGetters, mapDateTable, Constants } from './store';
export default {
  name: 'wodezhuye',
  data() {
    return {
      menus: [
        { icon: 'h-icon-home', label: '受理送检' },
        { icon: 'h-icon-link', label: '委托单位' },
        { icon: 'h-icon-task', label: '原始记录' },
        { icon: 'h-icon-star', label: '费用管理' },
        { icon: 'h-icon-star', label: '费用管理' },
      ],
      totalData: 0,
      efficiencyScore: '-',
      initOption: {},
      options1: {
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
      options2: {
        color: ['#21AB6E', '#4FCB8F'],
        tooltip: {
          trigger: 'axis',
          backgroundColor: '#fff',
          borderColor: '#F0F0F0',
          borderWidth: 1,
          textStyle: { color: '#434343' }
        },
        grid: {
          left: 40,
          right: 20,
          top: 20,
          bottom: 30
        },
        xAxis: {
          type: 'category',
          boundaryGap: false,
          data: [],
          axisLine: { lineStyle: { color: '#E8E8E8' } },
          axisLabel: { color: '#8C8C8C', fontSize: 11 }
        },
        yAxis: {
          type: 'value',
          axisLine: { show: false },
          splitLine: { lineStyle: { color: '#F0F0F0', type: 'dashed' } },
          axisLabel: { color: '#8C8C8C', fontSize: 11 }
        },
        series: [
          {
            name: '效能系数',
            type: 'line',
            smooth: true,
            data: [],
            areaStyle: {
              color: {
                type: 'linear', x: 0, y: 0, x2: 0, y2: 1,
                colorStops: [
                  { offset: 0, color: 'rgba(47,84,235,0.25)' },
                  { offset: 1, color: 'rgba(47,84,235,0.02)' }
                ]
              }
            },
            lineStyle: { width: 2.5 },
            symbol: 'circle',
            symbolSize: 6
          }
        ]
      },
      options3: {
        color: ['#21AB6E', '#1E6FE8', '#4FCB8F', '#36CFC9', '#FFC53D', '#FF7A45', '#722ED1', '#F5222D'],
        tooltip: {
          trigger: 'item',
          formatter: '{b}: {c} ({d}%)',
          backgroundColor: '#fff',
          borderColor: '#F0F0F0',
          borderWidth: 1,
          textStyle: { color: '#434343' }
        },
        legend: {
          orient: 'horizontal',
          bottom: 0,
          textStyle: { color: '#434343', fontSize: 11 },
          data: [],
        },
        series: [
          {
            name: '效能分布',
            type: 'pie',
            radius: ['45%', '70%'],
            center: ['50%', '45%'],
            data: [],
            itemStyle: {
              borderRadius: 6,
              borderColor: '#fff',
              borderWidth: 2
            },
            label: {
              show: true,
              formatter: '{b} {d}%',
              fontSize: 11,
              color: '#8C8C8C'
            },
            emphasis: {
              itemStyle: {
                shadowBlur: 10,
                shadowOffsetX: 0,
                shadowColor: 'rgba(0, 0, 0, 0.15)',
              },
            },
          },
        ],
      },
      componentData: [],
      gonggaoId: '',
      activeTab: '效能图',
    };
  },
  components: { chart, gonggaoDetail, setMenu, gonggaoList, ScrollNotice },
  computed: {
    ...mapDateTable('QRY1', []),
    ...mapDateTable('QQRY1', ['TotalCount']),
    ...mapDateTable('QRY2', []),
    ...mapDateTable('QRY4', []),
    ...mapDateTable('QRY5', []),
    cmenums() {
      let menus = [];
      let omenus = this.$store.state.app.omenus;
      this.QRY2.map(f => {
        let lm = omenus.find(of => {
          return (
            of.FUNCNAME === f.FUNCNAME &&
            of.FUNCTYPE === 2 &&
            ['资源管理', '模块管理', '首页'].indexOf(of.FUNCNAME) === -1
          );
        });
        if (lm && menus.length < 4) {
          menus.push({ icon: lm.FUNCICON || 'h-icon-task', label: lm.FUNCNAME, route: { name: lm.OUTERURL } });
        }
      });
      return menus;
    },
  },
  watch: {
    activeTab: {
      handler: function() {
        if (this.activeTab === '检测统计') {
          //this.query4();
        }
        if (this.activeTab === '效能图') {
          this.query5();
        }
      },
      immediate: true,
    },
  },
  mounted() {
    this.$nextTick(function() {
      bus.$emit('main-change-bell');
    });
  },
  methods: {
    query4() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/query4`,
        param: {},
        successCall: () => {
          let fitems = this.QRY4.filter(f => {
            return f.DEPTNAME === '部门汇总';
          });
          this.options1.xAxis[0].data = [];
          let data1 = [];
          let data2 = [];
          let data3 = [];
          let data4 = [];
          fitems.map(f => {
            this.options1.xAxis[0].data.push(f.STDDNAME);
            data1.push(f.F1);
            data2.push(f.CN2);
            data3.push(f.RAMT);
            data4.push(f.S3);
          });
          this.options1.legend.data = [
            { name: '收件,该标准受理单台件数' },
            { name: '未检数,尚未办结的台件数' },
            { name: '实收收入' },
            { name: '项目累计,该项目今年1月1日来累计收入' },
          ];
          this.options1.series = [
            {
              name: '收件,该标准受理单台件数',
              type: 'bar',
              data: data1,
            },
            {
              name: '未检数,尚未办结的台件数',
              type: 'bar',
              data: data2,
            },
            {
              name: '实收收入',
              type: 'bar',
              data: data3,
            },
            {
              name: '项目累计,该项目今年1月1日来累计收入',
              type: 'bar',
              data: data4,
            },
          ];
          //this.$refs.chart1.init();
        },
      });
    },
    query5() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/query5`,
        param: {},
        successCall: () => {
          let total = 0;
          let lineData = [];
          let lineLabels = [];
          // 填充折线图数据
          this.QRY5.map((f, index) => {
            lineLabels.push(f.EMPNAME || ('人员' + (index + 1)));
            lineData.push(parseFloat(f.XNXS || 0));
            total += parseFloat(f.XNXS || 0);
          });
          this.options2.xAxis.data = lineLabels;
          this.options2.series[0].data = lineData;
          // 填充饼图数据：按效能值排序，最多10个，其余归入"其他"
          this.options3.series[0].data = [];
          let pieData = this.QRY5.map(f => {
            return { name: f.DEPTNAME + ' ' + f.EMPNAME, value: parseFloat(f.XNXS || 0) };
          });
          pieData.sort((a, b) => b.value - a.value);
          if (pieData.length > 10) {
            let rest = pieData.slice(10);
            let restValue = rest.reduce((sum, item) => sum + item.value, 0);
            pieData = pieData.slice(0, 10).concat([{ name: '其他', value: restValue }]);
          }
          // 最多的一项用主题色
          let colors = ['#21AB6E', '#4FCB8F', '#1E6FE8', '#36CFC9', '#FFC53D', '#FF7A45', '#722ED1', '#F5222D', '#13C2C2', '#EB2F96', '#A0A0A0'];
          pieData.forEach((item, i) => {
            item.itemStyle = { color: colors[i % colors.length] };
          });
          this.options3.series[0].data = pieData;
          this.efficiencyScore = this.QRY5.length > 0 ? (total / this.QRY5.length).toFixed(1) : '-';
          this.totalData = this.QRY5.reduce((sum, f) => sum + parseInt(f.F1 || 0), 0);
          this.$refs.chart2.init();
          this.$nextTick(() => {
            if (this.$refs.chart3) this.$refs.chart3.init();
          });
        },
      });
    },
    async initModule(name) {
      let store = this.$store;
      let omenus = store.state['app'].omenus;
      let menu = omenus.find(t => t.OUTERURL === name);
      if (menu) {
        if (!store.state['app'].modules['RS_M00']) {
          // await store.dispatch('app/initModule', 'RS_M00');
          await this.$callAsync({ method: this.$store.dispatch, params: ['app/initModule', 'RS_M00'] });
        }
        if (!store.state['app'].modules[menu.FUNCCODE]) {
          // await store.dispatch('app/initModule', menu.FUNCCODE);
          await this.$callAsync({ method: this.$store.dispatch, params: ['app/initModule', menu.FUNCCODE] });
        }
      }
    },
    async link(route) {
      console.log(route);
      await this.initModule(route.name);
      this.$router.push({ path: route.name });
      return false;
    },
    refreshNotice() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/query1`,
        param: {},
      });
    },
    getCommonFunc() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/query2`,
        param: {},
      });
    },
    getList() {
      this.$refs.showList.show();
    },
    closeList() {
      this.$refs.showList.hide();
    },
    getDetail(item) {
      this.$refs.showDetail.show();
      this.gonggaoId = item.ID;
    },
    closeDetail() {
      this.$refs.showDetail.hide();
      this.$refs.showList.hide();
    },
    setMenu(component) {
      this.$refs.setMenu.show();
    },
    close() {
      this.$refs.setMenu.hide();
    },
    ok(value) {
      this.menus = value;
    },
  },
  beforeCreate() {
    console.log('创建前：');
    console.log(this.$el);
    console.log(this.$data);
  },
  created() {
    console.log('创建完成：');
    console.log(this.$el);
    console.log(this.$data);
    this.refreshNotice();
    this.getCommonFunc();
  },
  beforeMount() {
    console.log('挂载前：');
    console.log(this.$el);
    console.log(this.$data);
  },
};
</script>
<style lang="less" scoped>
@import '~@/theme/index.less';

.dashboard {
  min-height: 100%;
}

// Hero 渐变卡片（logo 翡翠绿大数字卡片）
.hero-card {
  background: linear-gradient(135deg, #4FCB8F 0%, #21AB6E 50%, #157A4E 100%);
  border-radius: @card-border-radius;
  padding: 24px;
  color: #fff;
  height: 100%;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  box-shadow: 0 4px 16px rgba(33, 171, 110, 0.25);
}
.hero-card-label {
  font-size: 13px;
  opacity: 0.8;
  margin-bottom: 8px;
}
.hero-card-value {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 20px;
  i {
    font-size: 28px;
    opacity: 0.8;
  }
  span:first-of-type {
    font-size: 40px;
    font-weight: 700;
    letter-spacing: -1px;
  }
}
.hero-card-trend {
  font-size: 14px;
  background: rgba(255,255,255,0.2);
  padding: 2px 8px;
  border-radius: 12px;
  font-weight: 500;
}
.hero-card-stats {
  display: flex;
  gap: 24px;
  padding-top: 16px;
  border-top: 1px solid rgba(255,255,255,0.2);
}
.hero-stat-item {
  flex: 1;
}
.hero-stat-value {
  font-size: 20px;
  font-weight: 700;
  margin-bottom: 2px;
}
.hero-stat-label {
  font-size: 12px;
  opacity: 0.7;
}

// Dashboard 卡片
.dashboard-card {
  background: #fff;
  border-radius: @card-border-radius;
  box-shadow: @shadow-card;
  transition: box-shadow 0.3s;
  &:hover {
    box-shadow: @shadow-card-hover;
  }
  &-header {
    padding: 16px 20px;
    border-bottom: 1px solid @gray2-color;
    display: flex;
    align-items: center;
    justify-content: space-between;
  }
  &-title {
    font-size: 15px;
    font-weight: 600;
    color: @dark-color;
    &::before {
      content: '';
      display: inline-block;
      width: 3px;
      height: 14px;
      background: @primary-color;
      border-radius: 2px;
      margin-right: 8px;
      vertical-align: middle;
    }
  }
  &-body {
    padding: 16px 20px;
  }
  &-actions {
    display: flex;
    align-items: center;
    gap: 16px;
  }
}

// 操作链接
.action-link {
  cursor: pointer;
  font-size: 13px;
  color: @dark3-color;
  transition: color 0.2s;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  &:hover {
    color: @primary-color;
  }
}

// 模块网格
.module-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(100px, 1fr));
  gap: 16px;
  padding: 8px 0;
}

.module-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 16px 8px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.3s;
  &:hover {
    background: @primary-color-bg;
    transform: translateY(-2px);
    .module-icon {
      color: @primary-color;
      background: rgba(47, 84, 235, 0.1);
    }
  }
}

.module-icon {
  width: 44px;
  height: 44px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: @gray3-color;
  margin-bottom: 8px;
  transition: all 0.3s;
  span {
    font-size: 22px;
    color: @dark2-color;
  }
}

.module-label {
  font-size: 13px;
  color: @dark2-color;
  text-align: center;
}
</style>

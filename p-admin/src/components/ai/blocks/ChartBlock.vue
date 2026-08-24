<template>
  <div ref="box" class="asst-chart"></div>
</template>

<script>
import echarts from 'echarts';

export default {
  name: 'ChartBlock',
  props: {
    option: { type: Object, required: true },
    height: { type: String, default: '300px' }
  },
  data() {
    return { chart: null };
  },
  mounted() {
    this.$nextTick(() => {
      this.$refs.box.style.height = this.height;
      this.chart = echarts.init(this.$refs.box);
      this.chart.setOption(this.option);
      this.resizeHandler = () => this.chart && this.chart.resize();
      window.addEventListener('resize', this.resizeHandler);
    });
  },
  beforeDestroy() {
    if (this.resizeHandler) window.removeEventListener('resize', this.resizeHandler);
    if (this.chart) {
      this.chart.dispose();
      this.chart = null;
    }
  },
  watch: {
    option: {
      deep: true,
      handler(v) {
        if (this.chart) this.chart.setOption(v, true);
      }
    }
  }
};
</script>

<style scoped>
.asst-chart {
  width: 100%;
  margin: 8px 0;
  min-height: 200px;
}
</style>

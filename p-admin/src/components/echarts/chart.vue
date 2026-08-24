<template>
  <div class="echarts-vue" :style="{height:height}" id="echarts-vue"></div>
</template>
<script>
import echarts from 'echarts';
export default {
  name: 'chart',
  props: {
    options: {
      type: Object,
      default: () => ({}),
    },
    height: {
      type: String,
      default: '100%',
    },
  },
  components: {},
  data() {
    return {};
  },
  computed: {},
  watch: {},
  mounted() {
    this.$nextTick(function() {
      this.init();
    });
  },
  methods: {
    init() {
      let myChart = echarts.init(this.$el);
      myChart.setOption(this.options);
      // 建议加上以下这一行代码，不加的效果图如下（当浏览器窗口缩小的时候）。超过了div的界限（红色边框）
      window.addEventListener('resize', function() {
        myChart.resize();
      });
    },
  },
};
</script>

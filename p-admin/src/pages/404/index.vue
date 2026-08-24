<template>
  <div class="page rr-text-center">
    <div class="rr-flex-col">
      <div class="rr-flex-1 rs-bk-size30" :style="{background: 'url('+ src + ') no-repeat 50%'}"></div>
      <div style="height:40vh">
        <Button color="primary" @click="backHome">返回首页</Button>
        <Button color="primary" @click="backPrev">返回上一页({{ second }}s)</Button>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: '404',
  data() {
    return {
      second: 5,
      timer: null,
      src: require('@/assets/img/T404.png'),
    };
  },
  methods: {
    backHome() {
      this.$router.replace({
        name: 'wodezhuye',
      });
    },
    backPrev() {
      this.$router.go(-1);
    },
  },
  mounted() {
    this.timer = setInterval(() => {
      if (this.second === 0) this.backPrev();
      else this.second--;
    }, 1000);
  },
  beforeDestroy() {
    clearInterval(this.timer);
  },
};
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>
.page {
  position: relative;
  height: 100vh;
  overflow: auto;
}
.rs-bk-size30 {
  background-size: 25% !important;
}
</style>

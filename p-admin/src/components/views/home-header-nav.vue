<template>
  <div class="rr-header-nav rr-flex-row">
    <div class="rr-nav-bar" @click="prev">
      <span class="rr-font rr-font-prev"></span>
    </div>
    <div class="rr-flex-1" ref="scroll">
      <div
        class="rr-nav-content"
        ref="scrollContent"
        :style="{right:(isPrev===0?'0':'auto'),left:scrollLeft}"
      >
        <span
          class="rr-nav-span"
          :class="{'active': currentValue===item.key}"
          v-for="(item,index) in routers"
          :key="index"
          @click.stop="select(item,$event)"
        >
          {{item.title}}
          <i
            class="h-icon-error"
            v-if="item.key!=='wodezhuye'"
            @click.stop="close(index,item.key)"
          ></i>
        </span>
      </div>
    </div>
    <div class="rr-nav-bar" @click="next">
      <span class="rr-font rr-font-next"></span>
    </div>
  </div>
</template>
<script>
export default {
  name: 'home-header-nav',
  props: {
    routers: {
      Type: Array,
    },
    value: '',
  },
  data() {
    return {
      isPrev: 0,
      currentValue: this.value,
      scrollLeft: 'auto',
    };
  },
  watch: {
    currentValue: {
      handler(val) {
        this.$emit('input', val);
      },
    },
    value(v) {
      this.currentValue = v;
    },
  },
  mounted() {
    this.$nextTick(function() {});
  },
  methods: {
    prev() {
      this.scrollLeft = 0;
      this.isPrev = 1;
    },
    next() {
      this.scrollLeft = 'auto';
      this.isPrev = 0;
    },
    select(item, e) {
      this.currentValue = item.key;
      this.setLeft(e);
    },
    setLeft(e) {
      // 设置左移宽度
      let scroll = (2 * this.$refs.scroll.clientWidth) / 3;
      let offsetLeft = e.target.offsetLeft;
      let left = 0;
      if (scroll < offsetLeft) {
        left = offsetLeft - scroll;
      }
      this.scrollLeft = '-' + left + 'px';
    },
    close(index, itemKey) {
      this.routers.splice(index, 1);
      let key = this.routers[index - 1].key;
      if (itemKey === this.currentValue) {
        this.currentValue = key;
      }
      this.$emit('close-tab', itemKey);
    },
  },
};
</script>

<style lang="less" scoped>
@import '~@/theme/index.less';
.rr-header-nav {
  line-height: 40px;
  height: 40px;
  background: transparent;
  overflow: hidden;
  padding: 0 4px;
}
.rr-nav-bar {
  padding: 0 8px;
  cursor: pointer;
  display: flex;
  align-items: center;
  color: @dark3-color;
  transition: color 0.2s;
  &:hover {
    color: @primary-color;
  }
}
.rr-flex-1 {
  height: 40px;
  overflow: hidden;
  position: relative;
}
.rr-nav-content {
  white-space: nowrap;
  overflow-x: auto;
  overflow-y: hidden;
  position: absolute;
  min-width: 100%;
  display: flex;
  align-items: center;
  height: 100%;
  .rr-nav-span {
    display: inline-flex;
    align-items: center;
    padding: 0 16px;
    height: 32px;
    line-height: 32px;
    border-radius: 6px;
    cursor: pointer;
    font-size: 14px;
    color: @dark2-color;
    margin: 0 2px;
    transition: all 0.2s;
    position: relative;
    i {
      padding: 0 0 0 6px;
      font-size: 12px;
      opacity: 0;
      transition: opacity 0.2s;
    }
    &:hover {
      background: @gray3-color;
      color: @primary-color;
      i {
        opacity: 1;
      }
    }
    &.active {
      background: @primary-color-bg;
      color: @primary-color;
      font-weight: 500;
      i {
        opacity: 1;
      }
    }
  }
}
</style>

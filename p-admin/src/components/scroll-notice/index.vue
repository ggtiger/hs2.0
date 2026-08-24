<template>
  <div class="scroll-notice" @mouseenter="paused=true" @mouseleave="paused=false">
    <div class="scroll-notice-slogan" v-if="slogan">
      <i class="h-icon-bell"></i> {{slogan}}
    </div>
    <div class="scroll-notice-wrapper" :style="{height: height}">
      <ul class="scroll-notice-list" :class="{'is-paused': paused}" v-if="list.length > 0">
        <li v-for="(item, index) in list" :key="index" class="scroll-notice-item" @click="$emit('click', item)">
          <span class="scroll-notice-badge">{{index + 1}}</span>
          <span class="scroll-notice-title">{{item.NOTITLE}}</span>
          <span class="scroll-notice-date">{{item.BILLDATE}}</span>
        </li>
        <li v-for="(item, index) in list" :key="'dup-'+index" class="scroll-notice-item" @click="$emit('click', item)">
          <span class="scroll-notice-badge">{{index + 1}}</span>
          <span class="scroll-notice-title">{{item.NOTITLE}}</span>
          <span class="scroll-notice-date">{{item.BILLDATE}}</span>
        </li>
      </ul>
      <div v-else style="text-align:center;color:#999;padding:20px 0;">暂无公告</div>
    </div>
  </div>
</template>

<script>
export default {
  props: {
    list: { type: Array, default: () => [] },
    height: { type: String, default: '280px' },
    slogan: { type: String, default: '' }
  },
  data() {
    return { paused: false };
  }
};
</script>

<style lang="less" scoped>
@import '~@/theme/index.less';
.scroll-notice {
  &-slogan {
    text-align: center;
    padding: 8px 0;
    font-weight: bold;
    color: @primary-color;
    font-size: 15px;
    border-bottom: 1px solid #f0f0f0;
    margin-bottom: 8px;
  }
  &-wrapper {
    overflow: hidden;
  }
  &-list {
    animation: scrollUp 20s linear infinite;
    &.is-paused {
      animation-play-state: paused;
    }
  }
  &-item {
    display: flex;
    align-items: center;
    padding: 8px 0;
    cursor: pointer;
    &:hover .scroll-notice-title {
      color: @primary-color;
    }
  }
  &-badge {
    width: 22px;
    height: 22px;
    border-radius: 3px;
    line-height: 22px;
    text-align: center;
    color: #fff;
    background: @primary-color;
    margin-right: 10px;
    flex-shrink: 0;
    font-size: 12px;
  }
  &-title {
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    color: #666;
  }
  &-date {
    color: #999;
    font-size: 12px;
    margin-left: 10px;
    flex-shrink: 0;
  }
}
@keyframes scrollUp {
  0% { transform: translateY(0); }
  100% { transform: translateY(-50%); }
}
</style>

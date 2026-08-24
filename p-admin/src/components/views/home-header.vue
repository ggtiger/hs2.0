<template>
  <div class="app-header">
    <div class="app-header-left">
      <Button
        :icon="siderCollapsed ? 'rr-font rr-font-shouqi':'rr-font rr-font-zhankai'"
        size="l"
        noBorder
        class="collapse-btn"
        @click="siderCollapsed=!siderCollapsed"
      ></Button>
    </div>
    <div class="app-header-tabs">
      <home-header-nav :value="innerNav" :routers="routers" @input="onNavChange" @close-tab="onCloseTab"></home-header-nav>
    </div>
    <div class="app-header-right">
      <DropdownMenu
        class="app-header-item"
        trigger="hover"
        offset="0,5"
        placement="bottom-end"
        :datas="getBell"
        :toggleIcon="false"
        @onclick="trigger"
      >
        <Badge :count="getBellCount">
          <i class="h-icon-bell header-icon"></i>
        </Badge>
      </DropdownMenu>
      <DropdownCustom
        class="app-header-item"
        trigger="hover"
        offset="0,5"
        placement="bottom-end"
        :toggleIcon="false"
      >
        <i @click="fullscreen" v-if="screenfullBut==false" class="h-icon-fullscreen header-icon"></i>
        <i @click="fullscreen" v-else class="h-icon-fullscreen header-icon active"></i>
      </DropdownCustom>
    </div>
  </div>
</template>
<script>
import store from '@/store';
import screenfull from 'screenfull';
import homeHeaderNav from './home-header-nav.vue';
export default {
  name: 'home-header',
  props: {
    value: Boolean,
    infoBell: [],
    routers: {
      type: Array,
      default: () => [],
    },
    navValue: {
      type: String,
      default: '',
    },
  },
  inject: ['reload'],
  components: {
    homeHeaderNav,
  },
  data() {
    return {
      siderCollapsed: false,
      screenfullBut: false,
      innerNav: this.navValue,
    };
  },
  computed: {
    getBellCount() {
      let c = 0;
      this.infoBell.map(t => {
        c += t.CNT;
      });
      return c;
    },
    getBell() {
      this.infoBell.map(t => {
        t.title = t.TITLE + '(' + t.CNT + ')';
        t.key = t.TITLE;
      });
      return this.infoBell;
    },
  },
  watch: {
    value: {
      handler(val) {
        this.siderCollapsed = val;
      },
      immediate: true,
      deep: true,
    },
    siderCollapsed: {
      handler(val) {
        this.$emit('input', val);
      },
      immediate: true,
      deep: true,
    },
    navValue(v) {
      this.innerNav = v;
    },
  },
  mounted() {
    this.listenResize();
  },
  methods: {
    fullscreen() {
      if (!screenfull.isEnabled) {
        this.$notification.open({
          message: '温馨提示',
          description:
            '您的浏览器无法使用全屏功能，请更换谷歌浏览器或者请手动点击F11按钮全屏展示！',
          duration: 10,
          placement: 'bottomLeft',
        });
        return false;
      }
      screenfull.toggle();
      if (screenfull.isFullscreen) {
        this.screenfullBut = false;
      } else {
        this.screenfullBut = true;
      }
    },
    listenResize() {
      let windowWidth = window.innerWidth;
      const resizeEvent = window.addEventListener('resize', () => {
        if (windowWidth === window.innerWidth) {
          return;
        }
        if (this.siderCollapsed && window.innerWidth > 900) {
          this.siderCollapsed = false;
        } else if (!this.siderCollapsed && window.innerWidth < 900) {
          this.siderCollapsed = true;
        }
        windowWidth = window.innerWidth;
      });
      this.$once('hook:beforeDestroy', () => {
        window.removeEventListener('resize', resizeEvent);
      });
      window.dispatchEvent(new Event('resize'));
    },
    onNavChange(val) {
      this.innerNav = val;
      this.$emit('update:navValue', val);
    },
    onCloseTab(itemKey) {
      this.$emit('close-tab', itemKey);
    },
    async trigger(data) {
      let tt = this.infoBell.find(t => {
        return t.TITLE === data;
      });
      if (!tt) return;
      // eslint-disable-next-line no-unused-vars
      let params = {};
      try {
        params = JSON.parse(tt.PARAMS);
      } catch (e) {}
      await this.initModule(tt.LURL.replace('/main', ''));
      if (this.$route.name === tt.LURL) {
        this.reload();
      }
      this.$router.push({ name: tt.LURL, params });
      this.$route.meta.params = params;
    },
    async initModule(name) {
      let omenus = store.state['app'].omenus;
      let menu = omenus.find(t => t.OUTERURL === name);
      if (menu) {
        if (!store.state['app'].modules['RS_M00']) {
          await this.$callAsync({ method: this.$store.dispatch, params: ['app/initModule', 'RS_M00'] });
        }
        if (!store.state['app'].modules[menu.FUNCCODE]) {
          await this.$callAsync({ method: this.$store.dispatch, params: ['app/initModule', menu.FUNCCODE] });
        }
      }
    },
  },
};
</script>

<style lang="less" scoped>
@import '~heyui/themes/index.less';
@import '~@/theme/modern.less';
.app-header {
  height: 50px;
  display: flex;
  align-items: center;
  padding: 0;
  background: #fff;
}

.app-header-left {
  display: flex;
  align-items: center;
  flex-shrink: 0;
  .collapse-btn {
    font-size: 18px;
    color: @dark2-color;
    border: none;
    background: transparent;
    cursor: pointer;
    padding: 8px;
    border-radius: @border-radius;
    transition: background 0.2s;
    &:hover {
      background: @gray3-color;
    }
  }
}

.app-header-tabs {
  flex: 1;
  min-width: 0;
  height: 50px;
  display: flex;
  align-items: center;
}

.app-header-right {
  display: flex;
  align-items: center;
  gap: 4px;
  height: 50px;
  flex-shrink: 0;
  /deep/ .h-dropdownmenu {
    height: 50px;
    line-height: 50px;
    display: inline-flex;
    align-items: center;
  }
}

.app-header-item {
  display: inline-flex;
  align-items: center;
  padding: 0 12px;
  height: 50px;
  line-height: 50px;
  border-radius: @border-radius;
  cursor: pointer;
  transition: background 0.2s;
  &:hover {
    background: @gray3-color;
  }
  .header-icon {
    font-size: 18px;
    color: @dark2-color;
    line-height: 1;
    &.active {
      color: @primary-color;
    }
  }
  .h-badge {
    display: inline-flex;
    align-items: center;
    line-height: 1;
  }
}
</style>

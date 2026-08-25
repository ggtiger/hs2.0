<template>
  <div class="layout-demo-3-vue">
    <Layout :siderFixed="siderFixed" :siderCollapsed="siderCollapsed">
      <Sider theme="dark" class="rr-scroll-bar sider-wrapper">
        <div class="layout-logo">
          <img v-if="siderCollapsed" src="@/assets/logo_small-1.png" style="height: 50px;"/>
          <img v-else src="@/assets/logo-1.png" style="height: 50px;"/>
        </div>
        <Menu
          class="h-menu-dark"
          :datas="menuDatas"
          :inlineCollapsed="siderCollapsed"
          ref="menu"
          @select="select"
        ></Menu>
        <div class="sider-user" :class="{'sider-user-collapsed': siderCollapsed}">
          <DropdownMenu
            trigger="hover"
            placement="right-end"
            :datas="userMenuDatas"
            :toggleIcon="false"
            @onclick="triggerUserMenu"
          >
            <div class="sider-user-info">
              <span class="sider-user-avatar"><i class="h-icon-user"></i></span>
              <span class="sider-user-name" v-show="!siderCollapsed">{{userInfo.NICKNAME}}</span>
            </div>
          </DropdownMenu>
        </div>
      </Sider>
      <Layout :headerFixed="headerFixed">
        <HHeader theme="white" class="myheader">
          <home-header
            v-model="siderCollapsed"
            :infoBell="infoBell"
            :routers="routerS"
            :nav-value.sync="activeNav"
            @close-tab="onCloseTab"
          ></home-header>
        </HHeader>
        <Content
          :style="{ height: 'calc(100vh - 50px)', margin: '0', overflow: 'hidden', padding: '10px 12px'}"
        >
          <div style="height:100%;overflow-y:auto;overflow-x:hidden;">
            <keep-alive :include="cachedViews">
            <router-view></router-view>
          </keep-alive>
          </div>
        </Content>
      </Layout>
    </Layout>
    <rs-modal ref="msetpass" autoWidth>
      <setPass></setPass>
    </rs-modal>
  </div>
</template>

<script>
import homeHeader from './views/home-header';
import setPass from './views/setPass.vue';
import store from '@/store';
import heyui from 'heyui';
import bus from '@/utils/eventbus';
import { registerOnlineRoute } from '@/router';
export default {
  components: {
    homeHeader,
    setPass,
  },
  provide() {
    return {
      reload: this.reload
    };
  },
  data() {
    return {
      cachedViews: [],
      headerFixed: true,
      siderFixed: true,
      siderCollapsed: true,
      routerS: [{ title: '首页', key: 'wodezhuye', icon: 'h-icon-home' }],
      activeNav: 'wodezhuye',
      menuDatas2: [
        { title: '首页', key: 'wodezhuye', icon: 'h-icon-home' },
        {
          title: '案例',
          key: 'search',
          icon: 'h-icon-menu',
          children: [
            { title: '零售退货查询', key: 'lsthSearch' },
            { title: '零售退货新增', key: 'lsthAdd' },
            { title: '采购订单查询', key: 'cgddSearch' },
            { title: '采购订单新增', key: 'cgddAdd' },
            { title: '查询1', key: 'lsthSearch1' },
          ],
        },
        {
          title: '收藏',
          key: 'favor',
          icon: 'h-icon-star',
          count: 100,
          children: [{ title: '收藏-1', key: 'favor2-1' }],
        },
        { title: '任务', icon: 'h-icon-task', key: 'task' },
        {
          title: '系统管理',
          key: 's01',
          icon: 'h-icon-setting',
          children: [
            { title: '资源管理', key: 's01/m01' },
            { title: '模块管理', key: 's01/m02' },
            { title: '功能管理', key: 's01/m03' },
            { title: '角色管理', key: 's01/m04' },
            { title: '用户管理', key: 's01/m05' },
            { title: '字典管理', key: 's01/m06' },
          ],
        },
      ],
      datas: [
        { icon: 'h-icon-home' },
        { title: 'Component', icon: 'h-icon-complete', route: { name: 'Component' } },
        { title: 'Breadcrumb', icon: 'h-icon-star' },
      ],
      infoBell: [],
      userMenuDatas: [
        { key: 'logout', title: '注销' },
        { key: 'setPass', title: '修改密码' },
      ],
    };
  },
  computed: {
    menuDatas() {
      return this.$store.state['app'].menus;
    },
    userInfo() {
      return this.$store.state.user.userInfo;
    },
  },
  async beforeCreate() {
    console.log('beforeCreate');
  },
  async mounted() {
    this.$nextTick(function() {
      this.initHeadNav();
    });

    // eslint-disable-next-line space-before-function-paren
    this._timer2 = setInterval(() => {
      this.loadBell();
    }, 5 * 60 * 1000);
    bus.$on('main-change-bell', () => {
      this.loadBell();
    });
    bus.$on('close-tab', this.onCloseTab);
    // this.menuDatas = this.$store.state['app'].menus;
  },
  beforeDestroy() {
    clearInterval(this._timer2);
    bus.$off('main-change-bell');
    bus.$off('close-tab', this.onCloseTab);
  },
  methods: {
    initHeadNav() {
      if (name === 'wodezhuye') {
        return false;
      }
    },
    async loadBell() {
      let ret = await this.$callAsync({ method: this.$store.dispatch, params: ['c02/query3'] });
      this.infoBell = ret.Items;
    },
    async select(data) {
      await this.initModule(data.key);
      // 在线模块: push 前确保路由已动态注册 (按约定 s01/m16/online/main → MODULEPATH)
      registerOnlineRoute(data.key);
      // 菜单 OUTERURL 指定什么就跳什么:
      //   generic 路由(g/ 或 /g/ 开头, 如 g/LIB_M05/main) 按 path 跳转(自动补前导/)
      //   传统/在线路由(如 b01/m05) 按 name 跳转
      if (data.key && /^\/?g\//.test(data.key)) {
        var path = data.key.charAt(0) === '/' ? data.key : '/' + data.key;
        this.$router.push({ path: path });
      } else {
        this.$router.push({ name: data.key });
      }
    },
    closeNav(key) {
      this.$router.push({ name: key });
    },
    selectNav(data) {
      this.$router.push({ name: data.key });
    },
    getRouteName(name) {
      if (name) {
        if (name.indexOf('/') !== -1) {
          name = name.substring(0, name.lastIndexOf('/'));
        }
      }
      return name;
    },
    async initModule(name) {
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
    reload(c) {
      var routeName = this.$route.name;
      var componentName = routeName ? routeName.replace(/\//g, '-') : null;
      if (componentName && this.cachedViews.includes(componentName)) {
        const idx = this.cachedViews.indexOf(componentName);
        this.cachedViews.splice(idx, 1);
        this.$nextTick(() => {
          this.cachedViews.push(componentName);
          if (c) c();
        });
      } else {
        this.$nextTick(() => {
          if (c) c();
        });
      }
    },
    onCloseTab(itemKey) {
      var componentName = itemKey ? itemKey.replace(/\//g, '-') : null;
      if (componentName) {
        var idx = this.cachedViews.indexOf(componentName);
        if (idx !== -1) {
          this.cachedViews.splice(idx, 1);
        }
      }
    },
    triggerUserMenu(data) {
      if (data === 'logout') {
        this.$router.push({ name: 'loginout' });
      } else if (data === 'setPass') {
        this.$refs.msetpass.show();
      }
    },
  },
  watch: {
    activeNav(key) {
      let omenus = store.state['app'].omenus;
      let route = this.routerS.find(t => t.key === key);
      if (route) {
        this.$router.push({ name: route.key });
      } else {
        let title = this.$route.meta.title;
        let key = this.$route.name;
        let menu = omenus.find(t => t.OUTERURL === this.getRouteName(key));
        this.routerS.push({ title: menu ? menu.FUNCNAME : title, key });
        this.$route.meta.title = menu ? menu.FUNCNAME : title;
      }
    },
    siderFixed() {
      if (!this.siderFixed) {
        this.headerFixed = false;
      }
    },
    $route: {
      handler() {
        let name = this.$route.name;
        this.activeNav = name;
        if (name) {
          var componentName = name.replace(/\//g, '-');
          if (!this.cachedViews.includes(componentName)) {
            this.cachedViews.push(componentName);
          }
          this.$nextTick(function() {
            var route = this.$route;
            var menuKey = null;
            // generic 路由 name 用下划线(g_LIB_M05_main), 与菜单 OUTERURL(g/LIB_M05/main 斜杠)不一致, 用 path 反查菜单 key 高亮
            var gm = route.path && route.path.match(/^\/g\/(.+)$/);
            if (gm) {
              var omenus = store.state['app'].omenus;
              var pathNoSlash = route.path.replace(/^\//, '');
              var menu = omenus.find(t => (t.OUTERURL || '').replace(/^\//, '') === pathNoSlash);
              menuKey = menu ? (menu.OUTERURL || menu.FUNCCODE) : null;
            } else if (name.indexOf('/') !== -1) {
              menuKey = name.substring(0, name.lastIndexOf('/'));
            } else {
              menuKey = name;
            }
            if (menuKey) {
              this.$refs.menu.select(menuKey);
            }
            // Tab 切换后触发 resize 事件，让表格重新计算布局
            setTimeout(function() {
              window.dispatchEvent(new Event('resize'));
            }, 300);
          });
        }
      },
      immediate: true,
    },
  },
  async beforeRouteUpdate(to, from, next) {
    console.log('beforeRouteUpdate');
    next();
  },
  async afterRouteEnter() {
    console.log('afterRouteEnter');
  },
  async beforeRouteEnter(to, from, next) {
    console.log('beforeRouteEnter');
    if (store.state['app'].omenus.length === 0) {
      try {
        await store.dispatch('app/initMenu', store.state['user'].userInfo.ID);
        await store.dispatch('app/initDict');
      } catch (e) {
        next({ path: '/login' });
        return;
      }
    }
    Object.keys(store.state['app'].dicts).forEach(key => {
      heyui.addDict(key, store.state['app'].dicts[key]);
    });
    next();
  },
};
</script>
<style lang="less" scoped>
@import '~@/theme/index.less';
.layout-demo-3-vue {
  .h-layout {
    background: #F0F2F5;
  }
  .h-layout.h-layout-header-fixed {
    padding-top: 50px;
  }
  .h-layout-header {
    height: 50px;
  }
  .sider-wrapper {
    background: linear-gradient(180deg, #2F54EB 0%, #1D39C4 100%) !important;
    box-shadow: 2px 0 12px rgba(47, 84, 235, 0.15);
    display: flex;
    flex-direction: column;
    overflow: visible !important;
  }
  .layout-logo {
    height: 56px;
    min-height: 56px;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 12px 16px;
    flex-shrink: 0;
    img {
      height: 36px;
      display: block;
    }
  }
  /deep/ .h-menu-dark {
    flex: 1;
    overflow-y: auto;
    overflow-x: hidden;
  }
  /deep/ .h-menu-size-collapse {
    overflow: visible !important;
  }
  .h-layout-footer {
    padding: 24px 50px;
    color: @dark2-color;
    font-size: 14px;
  }
  .h-menu-white {
    color: @dark1-color;
  }
}
/deep/ .h-menu-li-opened > .h-menu-show {
  background-color: rgba(255, 255, 255, 0.15);
  color: #fff;
  font-weight: 500;
  &:after {
    width: 0;
  }
}
.sider-user {
  border-top: 1px solid rgba(255, 255, 255, 0.15);
  padding: 12px 16px;
  flex-shrink: 0;
}
.sider-user-info {
  display: flex;
  align-items: center;
  gap: 10px;
  cursor: pointer;
}
.sider-user-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.2);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  i {
    font-size: 16px;
    color: #fff;
  }
}
.sider-user-name {
  font-size: 14px;
  color: #fff;
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.sider-user-collapsed {
  padding: 12px 0;
  display: flex;
  justify-content: center;
  /deep/ .h-dropdownmenu {
    width: auto;
  }
  .sider-user-info {
    justify-content: center;
  }
  .sider-user-name {
    display: none;
  }
}
</style>

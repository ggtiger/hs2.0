<template>
  <div class="layout-demo-3-vue">
    <Layout :siderFixed="siderFixed" :siderCollapsed="siderCollapsed">
      <Sider theme="white">
        <div class="layout-logo">
          <img v-if="siderCollapsed" src="@/assets/logo_small.png" />
          <img v-else src="@/assets/logo.png" />
        </div>
        <Menu
          style="margin-top: 20px;"
          class="h-menu-white"
          :datas="menuDatas"
          :inlineCollapsed="siderCollapsed"
          ref="menu"
          @select="select"
        ></Menu>
      </Sider>
      <Layout :headerFixed="headerFixed">
        <HHeader theme="white">
          <home-header v-model="siderCollapsed"></home-header>
        </HHeader>
        <Content
          :style="{ height: 'calc(100vh - 64px)', margin: '0',overflow: 'hidden',padding: '30px'}"
        >
          <home-body :routerName="routerName" :routerMeta="routerMeta" :routeDatas="routeDatas"></home-body>
        </Content>
      </Layout>
    </Layout>
  </div>
</template>

<script>
import homeHeader from './views/home-header';
import homeBody from './views/home-body';
export default {
  components: {
    homeHeader,
    homeBody,
  },
  data() {
    return {
      headerFixed: true,
      siderFixed: true,
      siderCollapsed: true,
      routerMeta: {},
      routerName: 'wodezhuye',
      menuDatas: [
        { title: '首页', key: 'wodezhuye', icon: 'h-icon-home' },
        {
          title: '案例',
          key: 'search',
          icon: 'h-icon-search',
          children: [{ title: '查询页面', key: 'lsthSearch' }, { title: '编辑页面', key: 'lsthAdd' }],
        },
        {
          title: '系统管理',
          key: 's01',
          icon: 'h-icon-search',
          children: [
            { title: '资源管理', key: 's01/m01' },
            { title: '模块管理', key: 's01/m02' },
            { title: '功能管理', key: 's01/m03' },
            { title: '角色管理', key: 's01/m04' },
            { title: '用户管理', key: 's01/m05' },
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
      ],
      datas: [
        { icon: 'h-icon-home' },
        { title: 'Component', icon: 'h-icon-complete', route: { name: 'Component' } },
        { title: 'Breadcrumb', icon: 'h-icon-star' },
      ],
    };
  },
  computed: {
    routeDatas() {
      let routeDatas = [
        {
          icon: 'h-icon-home',
          route: { name: 'wodezhuye' },
        },
        {
          title: this.routerMeta.title,
        },
      ];
      return routeDatas;
    },
  },
  methods: {
    select(data) {
      this.$router.push({ name: data.key });
    },
  },
  watch: {
    siderFixed() {
      if (!this.siderFixed) {
        this.headerFixed = false;
      }
    },
    $route() {
      this.routerMeta = this.$route.meta;
      this.routerName = this.$route.name;
      if (this.$route.name) {
        this.$refs.menu.select(this.$route.name);
      }
    },
  },
};
</script>
<style lang="less" scoped>
.layout-demo-3-vue {
  .h-layout {
    background: #f0f2f5;
  }
  .layout-logo {
    height: 40px;
    margin: 16px 24px;
    img {
      height: 40px;
      display: block;
      margin: 0 auto;
    }
  }
  .h-layout-footer {
    padding: 24px 50px;
    color: rgba(0, 0, 0, 0.65);
    font-size: 14px;
  }
  .h-menu-white {
    color: #2c3e50;
  }
}
</style>

import Vue from 'vue';
import Store from '@/store';
import Router from 'vue-router';
import HeyUI from 'heyui';
// eslint-disable-next-line no-unused-vars
import RemoteRoute from '@/sfc-loader/remote-route.vue';
import GenericModule from '@/components/generic-module/generic-module.vue';
/*
路由管理对象
1.模块路由加载
2.登陆判断：router 设置 {meta:{isAuth:false}} 不需要判断
3.权限判断
4.主页面禁用按键 {meta:{isMain:true}}
*/

// const paths = {
//   MAIN: '/wodezhuye',
//   T404: '/404',
//   T503: '/503',
//   LOGIN: '/login',
// };

Vue.use(Router);

// 解决重复导航到当前路由的 NavigationDuplicated 报错
const originalPush = Router.prototype.push;
Router.prototype.push = function push(location) {
  return originalPush.call(this, location).catch(err => {
    if (err.name !== 'NavigationDuplicated') throw err;
  });
};
const originalReplace = Router.prototype.replace;
Router.prototype.replace = function replace(location) {
  return originalReplace.call(this, location).catch(err => {
    if (err.name !== 'NavigationDuplicated') throw err;
  });
};

let routes = [];

// 通过webpack加载router
let requireAll = requireContext => requireContext.keys().map(requireContext);
let req = require.context('../pages', true, /router.js$/);
let ret = requireAll(req);
ret.forEach(item => {
  if (item.default instanceof Array) {
    routes = routes.concat(item.default);
  } else {
    routes.push(item.default);
  }
});
const router = new Router({
  routes: routes,
});

const paths = {
  'MAIN': '/main',
  'T404': '/404',
  'T503': '/503',
  'LOGIN': '/login'
};

router.onError((callback) => {
  HeyUI.$LoadingBar.fail();
  console.log(callback);
});

// 已注册的在线模块路由 (route name 集合, 避免重复 addRoutes)
var registeredOnlineRoutes = {};
// 已注册的通用模块路由
var registeredGenericRoutes = {};

/**
 * 从路由 name 推导在线模块的 MODULEPATH
 * 约定: route name 必须包含 '/online/' 段, 其左侧为 业务/模块号, 右侧为 view 名
 *   s01/m16/online/main  →  @/pages/s01/m16/views/main.vue
 *   s01/m16/online/add   →  @/pages/s01/m16/views/add.vue
 *   r02/m07/online/main  →  @/pages/r02/m07/views/main.vue
 * 返回 null 表示不是在线模块路由
 */
function deriveOnlineModulePath(routeName) {
  if (!routeName || typeof routeName !== 'string') return null;
  var marker = '/online/';
  var idx = routeName.indexOf(marker);
  if (idx < 0) return null;
  var prefix = routeName.substring(0, idx); // s01/m16
  var viewName = routeName.substring(idx + marker.length); // main / add / sub/edit
  if (!prefix || !viewName) return null;
  return '@/pages/' + prefix + '/views/' + viewName + '.vue';
}

/**
 * 动态注册在线模块路由
 * 结构参考各业务模块 router.js 的"父级 shell + 子页"模式:
 *   parent: path=/s01/m16/online, component=@/components/main (外壳: 侧边栏/头部/Tab)
 *   child:  path=main (相对), name=s01/m16/online/main, component=Wrapper
 * 完整 URL: /s01/m16/online/main
 *
 * 为什么包一层 Wrapper (render 函数) 而不是直接用 RemoteRoute:
 *   main.vue 的 <keep-alive :include="cachedViews"> 按"组件 name 字段"匹配缓存
 *   cachedViews 里存的是 routeName 转破折号 (如 's01-m16-online-main')
 *   RemoteRoute 的 name 固定为 'remote-route', 永远匹配不上 → 每次重建, 无缓存
 *   用 Wrapper 把 name 设为 's01-m16-online-main' → keep-alive 命中 → 整棵子树 (含 RemoteRoute 和 SFC) 都被缓存
 *   效果与本地页面完全一致: 第一次进入加载, 后续切回直接显示缓存状态, 不再重新请求
 *
 * @returns true 表示本次新注册了路由 (调用方需重新触发导航以应用新路由)
 */
export function registerOnlineRoute(routeName) {
  if (registeredOnlineRoutes[routeName]) return false;
  var modulePath = deriveOnlineModulePath(routeName);
  if (!modulePath) return false;

  var marker = '/online/';
  var idx = routeName.indexOf(marker);
  var prefix = routeName.substring(0, idx); // s01/m16
  var viewName = routeName.substring(idx + marker.length); // main
  var parentPath = '/' + prefix + '/online';
  var parentName = prefix + '/online';
  var dashedName = routeName.replace(/\//g, '-'); // s01-m16-online-main (与 cachedViews 的 key 一致)

  // 用 render 函数包一层, 关键是 name 字段匹配 keep-alive 的 include
  var Wrapper = {
    name: dashedName,
    props: { modulePath: { type: String, required: true } },
    render: function(h) {
      return h(RemoteRoute, { props: { modulePath: this.modulePath } });
    },
  };

  router.addRoutes([{
    path: parentPath,
    name: parentName,
    redirect: '/' + routeName,
    meta: { hideInMenu: true, title: '在线模块', notCache: true },
    component: () => import('@/components/main'),
    children: [
      {
        path: viewName, // 相对路径: main
        name: routeName, // s01/m16/online/main
        component: Wrapper,
        props: { modulePath: modulePath },
        meta: { hideInMenu: true, title: '在线模块', notCache: true },
      }
    ]
  }]);
  registeredOnlineRoutes[routeName] = true;
  return true;
}

/**
 * 动态注册通用模块路由
 * 从 initModule 加载的 MODPAGE 数据中读取页面配置，注册 GenericModule 组件路由
 * 路由结构:
 *   parent: path=/b01/m07/generic, component=@/components/main (外壳)
 *   child:  path=main, name=b01/m07/generic/main, component=GenericModule
 *
 * @param {string} moduleCode - 模块编码如 'LIB_M07'
 * @param {Object} moduleData - initModule 返回的数据（含 MODPAGE/MODBUTTON）
 * @returns {string[]} 本次注册的路由 name 列表
 */
export function registerGenericRoute(moduleCode, moduleData) {
  if (!moduleData || !moduleData.MODPAGE) return [];
  // ROUTEPATH 未配置时按 /g/{MODULECODE}/{PAGECODE} 约定生成，保证 /g/xxx 直接访问可匹配
  // 注意：返回新对象，不污染 app store 的 MODPAGE 缓存
  // select 页面只在弹窗中使用，不注册路由
  var pages = moduleData.MODPAGE.filter(p =>
    (p.ISDELETED || 0) === 0 && p.PAGETYPE !== 'select' && (p.COMPONENTTYPE === 'standard' || (p.COMPONENTTYPE === 'sfc' && p.SFCMODULEPATH))
  ).map(function(p) {
    return p.ROUTEPATH ? p : Object.assign({}, p, { ROUTEPATH: '/g/' + moduleCode + '/' + p.PAGECODE });
  });
  // 按 SORTNO 优先, PAGECODE 次之排序, 保证路由注册顺序与 parent redirect 目标确定 (不依赖后端返回顺序)
  pages.sort(function(a, b) {
    var sa = a.SORTNO || 0;
    var sb = b.SORTNO || 0;
    if (sa !== sb) return sa - sb;
    var pa = (a.PAGECODE || '') + '';
    var pb = (b.PAGECODE || '') + '';
    if (pa < pb) return -1;
    if (pa > pb) return 1;
    return 0;
  });
  if (pages.length === 0) return [];

  // 收集所有有效页面的 route name (无论分组是否已注册), 供调用方判断 generic 路由是否可用
  var registeredNames = pages.map(function(page) {
    return page.ROUTEPATH.replace(/^\//, '').replace(/\//g, '_');
  });

  // 按路由前缀分组，同前缀共用一个 parent 路由
  var groups = {};
  pages.forEach(function(page) {
    var routePath = page.ROUTEPATH;
    // 从路由路径提取前缀: /b01/m07/generic/main → parentPath=/b01/m07/generic
    var lastSlash = routePath.lastIndexOf('/');
    var parentPath = lastSlash > 0 ? routePath.substring(0, lastSlash) : routePath;
    if (!groups[parentPath]) groups[parentPath] = [];
    groups[parentPath].push(page);
  });

  Object.keys(groups).forEach(function(parentPath) {
    var parentName = parentPath.replace(/^\//, '').replace(/\//g, '_') + '_generic';
    if (registeredGenericRoutes[parentName]) return;

    var children = groups[parentPath].map(function(page) {
      var routePath = page.ROUTEPATH;
      var lastSlash = routePath.lastIndexOf('/');
      var childPath = lastSlash > 0 ? routePath.substring(lastSlash + 1) : routePath;
      var routeName = routePath.replace(/^\//, '').replace(/\//g, '_');
      var dashedName = routeName.replace(/\//g, '-');

      // SFC 页面使用 RemoteRoute，标准页面使用 GenericModule
      var isSfc = page.COMPONENTTYPE === 'sfc' && page.SFCMODULEPATH;
      var Wrapper;
      if (isSfc) {
        Wrapper = {
          name: dashedName,
          props: {
            modulePath: { type: String, required: true }
          },
          render: function(h) {
            return h(RemoteRoute, { props: { modulePath: this.modulePath } });
          }
        };
      } else {
        Wrapper = {
          name: dashedName,
          props: {
            moduleCode: { type: String, default: moduleCode },
            pageCode: { type: String, default: page.PAGECODE }
          },
          render: function(h) {
            return h(GenericModule, {
              props: {
                moduleCode: this.moduleCode,
                pageCode: this.pageCode
              }
            });
          }
        };
      }

      var wrapperProps = isSfc
        ? { modulePath: page.SFCMODULEPATH }
        : { moduleCode: moduleCode, pageCode: page.PAGECODE };

      return {
        path: childPath,
        name: routeName,
        component: Wrapper,
        props: wrapperProps,
        meta: {
          title: page.PAGENAME || '',
          isAuth: true,
          isMain: true
        }
      };
    });

    router.addRoutes([{
      path: parentPath,
      name: parentName,
      redirect: children[0] ? '/' + children[0].name : parentPath,
      meta: { hideInMenu: true, title: '通用模块', notCache: true },
      component: function() { return import('@/components/main') },
      children: children
    }]);

    registeredGenericRoutes[parentName] = true;
  });

  return registeredNames;
}

router.beforeEach((to, from, next) => {
  HeyUI.$LoadingBar.start();
  // 路由未匹配 → 尝试动态注册
  if (!to.matched || to.matched.length === 0) {
    // 1. 尝试在线模块路由 (需要 to.name)
    if (to.name) {
      var added = registerOnlineRoute(to.name);
      if (added) {
        next({ name: to.name, params: to.params, query: to.query, replace: true });
        return;
      }
    }
    // 2. 尝试通用模块路由: 路径格式 /g/{MODULECODE}/{pageCode}
    var genericMatch = to.path && to.path.match(/^\/g\/([^/]+)\/(.+)$/);
    if (genericMatch) {
      var gModuleCode = genericMatch[1];
      Store.dispatch('app/initModule', gModuleCode).then(function() {
        var modData = Store.state['app'].modules[gModuleCode];
        if (modData && modData.MODPAGE && modData.MODPAGE.length > 0) {
          var added = registerGenericRoute(gModuleCode, modData);
          // 有有效 generic 页面则重新导航(路由已由 app.js initModule 或本次注册); 否则 404
          if (added && added.length > 0) {
            next({ path: to.path, replace: true });
          } else {
            next({ path: paths.T404 });
          }
          return;
        }
        next({ path: paths.T404 });
      });
      return;
    }
    // 既未匹配也不是在线/通用模块 → 404
    next({ path: paths.T404 });
    return;
  }
  try {
    if (!to.matched || to.matched.length === 0) {
      next({ path: paths.T404 });
      return;
    }
  } catch (e) {
    // ignore
  }

  if (to.FUNCCODE && !Store.state['app'].modules[to.FUNCCODE]) {
    Store.dispatch('app/initModule', to.FUNCCODE).then((params) => {
      next(true);
    });
  } else {
    next(true);
  }
});
router.afterEach(() => {
  HeyUI.$LoadingBar.success();
});
window.addEventListener(
  'popstate',
  function(e) {
    this.$isDeviceChange = true;
  },
  false
);
window.addEventListener(
  'hashchange',
  function(e) {
    this.$isDeviceChange = false;
  },
  false
);

export default router;

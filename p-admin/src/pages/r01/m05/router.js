import store from '@/store';

// 预加载 R02_M07 模块配置后，再加载页面组件
// main.vue 静态 import r02/m07/add.vue，其 computed 在模块加载时
// 会触发 createStore.getStore('R02_M07')，需要 app.modules['R02_M07'] 已存在
async function loadR01M05() {
  if (!store.state['app'].modules || !store.state['app'].modules['R02_M07']) {
    await store.dispatch('app/initModule', 'R02_M07');
  }
  return import('@/pages/r01/m05');
}

export default [
  {
    path: '/r01/m05',
    name: 'r01/m05',
    redirect: '/r01/m05/main',
    meta: {
      hideInMenu: true,
      title: '受理单',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/r01/m05/main',
        name: 'r01/m05/main',
        meta: {
          hideInMenu: true,
          title: '受理单',
          notCache: true,
          icon: 'md-home'
        },
        component: () => loadR01M05(),
      }
    ]
  },{
    path: '/r01/m051',
    name: 'r01/m051',
    redirect: '/r01/m051/main',
    meta: {
      hideInMenu: true,
      title: '证书查询打印',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/r01/m051/main',
        name: 'r01/m051/main',
        meta: {
          hideInMenu: true,
          title: '证书查询打印',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/r01/m05/index1')), "r01/m05"),
      }
    ]
  }]

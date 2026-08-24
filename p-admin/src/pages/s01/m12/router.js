export default [
  {
    path: '/s01/m12',
    name: 's01/m12',
    redirect: '/s01/m12/main',
    meta: {
      hideInMenu: true,
      title: 'Word模版定义',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m12/main',
        name: 's01/m12/main',
        meta: {
          hideInMenu: true,
          title: 'Word模版定义',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m12')), 'm12'),
      },
      {
        path: '/s01/m12/preview',
        name: 's01/m12/preview',
        meta: {
          hideInMenu: true,
          title: '模版预览',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('./views/preview.vue')), 'm12-preview'),
      }
    ]
  }];

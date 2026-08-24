export default [
  {
    path: '/s01/m02',
    name: 's01/m02',
    redirect: '/s01/m02/main',
    meta: {
      hideInMenu: true,
      title: '模块管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m02/main',
        name: 's01/m02/main',
        meta: {
          hideInMenu: true,
          title: '模块管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m02')), 'm02'),
      }
    ],
  }];

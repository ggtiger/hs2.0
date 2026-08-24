export default [
  {
    path: '/s01/m03',
    name: 's01/m03',
    redirect: '/s01/m03/main',
    meta: {
      hideInMenu: true,
      title: '功能管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m03/main',
        name: 's01/m03/main',
        meta: {
          hideInMenu: true,
          title: '功能管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m03')), 'm03'),
      }
    ]
  }];

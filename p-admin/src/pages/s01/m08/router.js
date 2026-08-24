export default [
  {
    path: '/s01/m08',
    name: 's01/m08',
    redirect: '/s01/m08/main',
    meta: {
      hideInMenu: true,
      title: '记录模版',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m08/main',
        name: 's01/m08/main',
        meta: {
          hideInMenu: true,
          title: '公文管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m08')), 's01/m08'),
      }
    ]
  }];

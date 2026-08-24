export default [
  {
    path: '/s01/m09',
    name: 's01/m09',
    redirect: '/s01/m09/main',
    meta: {
      hideInMenu: true,
      title: '日志管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m09/main',
        name: 's01/m09/main',
        meta: {
          hideInMenu: true,
          title: '日志管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m09')), 's01/m09'),
      }
    ]
  }];

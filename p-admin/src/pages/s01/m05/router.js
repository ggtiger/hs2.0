export default [
  {
    path: '/s01/m05',
    name: 's01/m05',
    redirect: '/s01/m05/main',
    meta: {
      hideInMenu: true,
      title: '用户管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m05/main',
        name: 's01/m05/main',
        meta: {
          hideInMenu: true,
          title: '用户管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m05')), 'm05'),
      }
    ]
  }];

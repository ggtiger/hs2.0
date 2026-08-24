export default [
  {
    path: '/s01/m04',
    name: 's01/m04',
    redirect: '/s01/m04/main',
    meta: {
      hideInMenu: true,
      title: '角色管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m04/main',
        name: 's01/m04/main',
        meta: {
          hideInMenu: true,
          title: '角色管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m04')), 'm03'),
      }
    ]
  }];

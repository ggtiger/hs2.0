export default [
  {
    path: '/b01/m04',
    name: 'b01/m04',
    redirect: '/b01/m04/main',
    meta: {
      hideInMenu: true,
      title: '标准管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/b01/m04/main',
        name: 'b01/m04/main',
        meta: {
          hideInMenu: true,
          title: '标准管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/b01/m04')), 'm04'),
      }
    ]
  }];

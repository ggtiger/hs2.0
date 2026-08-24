export default [
  {
    path: '/b01/m06',
    name: 'b01/m06',
    redirect: '/b01/m06/main',
    meta: {
      hideInMenu: true,
      title: '员工管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/b01/m06/main',
        name: 'b01/m06/main',
        meta: {
          hideInMenu: true,
          title: '员工管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/b01/m06')), 'm06'),
      }
    ]
  }];

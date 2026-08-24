export default [
  {
    path: '/r01/m06',
    name: 'r01/m06',
    redirect: '/r01/m06/main',
    meta: {
      hideInMenu: true,
      title: '委托管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/r01/m06/main',
        name: 'r01/m06/main',
        meta: {
          hideInMenu: true,
          title: '委托管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/r01/m06')), 'r01/m06'),
      }
    ]
  }];

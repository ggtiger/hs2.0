export default [
  {
    path: '/r01/m031',
    name: 'r01/m031',
    redirect: '/r01/m031/main',
    meta: {
      hideInMenu: true,
      title: '费用管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/r01/m031/main',
        name: 'r01/m031/main',
        meta: {
          hideInMenu: true,
          title: '费用管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/r01/m031')), 'r01/m031'),
      }
    ]
  }];

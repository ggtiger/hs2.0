export default [
  {
    path: '/r01/m03',
    name: 'r01/m03',
    redirect: '/r01/m03/main',
    meta: {
      hideInMenu: true,
      title: '费用管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/r01/m03/main',
        name: 'r01/m03/main',
        meta: {
          hideInMenu: true,
          title: '费用管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/r01/m03')), 'r01/m03'),
      }
    ]
  }];

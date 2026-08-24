export default [
  {
    path: '/r01/m01',
    name: 'r01/m01',
    redirect: '/r01/m01/main',
    meta: {
      hideInMenu: true,
      title: '项目管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/r01/m01/main',
        name: 'r01/m01/main',
        meta: {
          hideInMenu: true,
          title: '项目管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/r01/m01')), 'r01/m01'),
      }
    ]
  }];

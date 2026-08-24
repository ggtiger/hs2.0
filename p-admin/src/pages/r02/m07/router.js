export default [
  {
    path: '/r02/m07',
    name: 'r02/m07',
    redirect: '/r02/m07/main',
    meta: {
      hideInMenu: true,
      title: '物流管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/r02/m07/main',
        name: 'r02/m07/main',
        meta: {
          hideInMenu: true,
          title: '物流管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/r02/m07')), "r02/m07"),
      }
    ]
  }];

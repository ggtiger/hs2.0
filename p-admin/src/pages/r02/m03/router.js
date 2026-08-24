export default [
  {
    path: '/r02/m03',
    name: 'r02/m03',
    redirect: '/r02/m03/main',
    meta: {
      hideInMenu: true,
      title: '人员客户统计效能表',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/r02/m03/main',
        name: 'r02/m03/main',
        meta: {
          hideInMenu: true,
          title: '客户统计',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/r02/m03')), "r02/m03"),
      }
    ]
  }]

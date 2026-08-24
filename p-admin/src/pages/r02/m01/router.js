export default [
  {
    path: '/r02/m01',
    name: 'r02/m01',
    redirect: '/r02/m01/main',
    meta: {
      hideInMenu: true,
      title: '检测情况统计表',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/r02/m01/main',
        name: 'r02/m01/main',
        meta: {
          hideInMenu: true,
          title: '检测情况统计表',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/r02/m01')), "r02/m01"),
      }
    ]
  }]

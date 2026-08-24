export default [
  {
    path: '/r02/m02',
    name: 'r02/m02',
    redirect: '/r02/m02/main',
    meta: {
      hideInMenu: true,
      title: '人员效能表',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/r02/m02/main',
        name: 'r02/m02/main',
        meta: {
          hideInMenu: true,
          title: '人员效能表',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/r02/m02')), "r02/m02"),
      }
    ]
  }]

export default [
  {
    path: '/b01/m012',
    name: 'b01/m012',
    redirect: '/b01/m012/main',
    meta: {
      hideInMenu: true,
      title: '人员监督',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/b01/m012/main',
        name: 'b01/m012/main',
        meta: {
          hideInMenu: true,
          title: '人员监督',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/b01/m012')), "m012"),
      }
    ]
  }]

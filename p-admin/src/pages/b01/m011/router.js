export default [
  {
    path: '/b01/m011',
    name: 'b01/m011',
    redirect: '/b01/m011/main',
    meta: {
      hideInMenu: true,
      title: '委托单位',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/b01/m011/main',
        name: 'b01/m011/main',
        meta: {
          hideInMenu: true,
          title: '分包单位',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/b01/m011')), "m01"),
      }
    ]
  }]

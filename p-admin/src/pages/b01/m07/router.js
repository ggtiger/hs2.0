export default [
  {
    path: '/b01/m07',
    name: 'b01/m07',
    redirect: '/b01/m07/main',
    meta: {
      hideInMenu: true,
      title: '资质证书管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/b01/m07/main',
        name: 'b01/m07/main',
        meta: {
          hideInMenu: true,
          title: '资质证书管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/b01/m07')), "m07"),
      }
    ]
  }]

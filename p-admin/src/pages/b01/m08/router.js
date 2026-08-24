export default [
  {
    path: '/b01/m08',
    name: 'b01/m08',
    redirect: '/b01/m08/main',
    meta: {
      hideInMenu: true,
      title: '人员授权管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/b01/m08/main',
        name: 'b01/m08/main',
        meta: {
          hideInMenu: true,
          title: '人员授权管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/b01/m08')), "m08"),
      }
    ]
  }]

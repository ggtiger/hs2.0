export default [
  {
    path: '/s01/m06',
    name: 's01/m06',
    redirect: '/s01/m06/main',
    meta: {
      hideInMenu: true,
      title: '字典管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m06/main',
        name: 's01/m06/main',
        meta: {
          hideInMenu: true,
          title: '字典管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m06')), "m06"),
      }
    ]
  }]

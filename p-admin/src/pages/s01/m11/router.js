export default [
  {
    path: '/s01/m11',
    name: 's01/m11',
    redirect: '/s01/m11/main',
    meta: {
      hideInMenu: true,
      title: '公式定义',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m11/main',
        name: 's01/m11/main',
        meta: {
          hideInMenu: true,
          title: '公式定义',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m11')), "m11"),
      }
    ]
  }]

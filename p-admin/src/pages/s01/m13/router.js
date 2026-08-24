export default [
  {
    path: '/s01/m13',
    name: 's01/m13',
    redirect: '/s01/m13/main',
    meta: {
      hideInMenu: true,
      title: 'SQL配置',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m13/main',
        name: 's01/m13/main',
        meta: {
          hideInMenu: true,
          title: 'SQL配置',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m13')), "m13"),
      }
    ]
  }]

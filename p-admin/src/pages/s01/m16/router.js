export default [
  {
    path: '/s01/m16',
    name: 's01/m16',
    redirect: '/s01/m16/main',
    meta: {
      hideInMenu: true,
      title: '提示词管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m16/main',
        name: 's01/m16/main',
        meta: {
          hideInMenu: true,
          title: '提示词管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m16')), "m16"),
      }
    ]
  }]

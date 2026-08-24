export default [
  {
    path: '/s01/m25',
    name: 's01/m25',
    redirect: '/s01/m25/main',
    meta: {
      hideInMenu: true,
      title: '模板市场',
      notCache: true,
      icon: 'md-cube',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m25/main',
        name: 's01/m25/main',
        meta: {
          hideInMenu: true,
          title: '模板市场',
          notCache: true,
          icon: 'md-cube'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m25/views/main')), 'm25'),
      }
    ]
  }];

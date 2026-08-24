export default [
  {
    path: '/s01/m28',
    name: 's01/m28',
    redirect: '/s01/m28/main',
    meta: {
      hideInMenu: false,
      title: '模块开发中心',
      notCache: true,
      icon: 'md-cube'
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m28/main',
        name: 's01/m28/main',
        meta: {
          hideInMenu: false,
          title: '模块开发中心',
          notCache: true,
          icon: 'md-cube'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m28')), 'm28')
      }
    ]
  }
];

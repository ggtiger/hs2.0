export default [
  {
    path: '/s01/m07',
    name: 's01/m07',
    redirect: '/s01/m07/main',
    meta: {
      hideInMenu: true,
      title: '模板管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m07/main',
        name: 's01/m07/main',
        meta: {
          hideInMenu: true,
          title: '模板管理',
          notCache: true,
          icon: 'md-home',
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m07')), 'm07'),
      },
    ],
  },
];

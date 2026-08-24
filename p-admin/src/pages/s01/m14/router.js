export default [
  {
    path: '/s01/m14',
    name: 's01/m14',
    redirect: '/s01/m14/main',
    meta: {
      hideInMenu: true,
      title: 'LLM配置',
      notCache: true,
      icon: 'md-home'
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m14/main',
        name: 's01/m14/main',
        meta: {
          hideInMenu: true,
          title: 'LLM配置',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m14')), 'm14')
      }
    ]
  }
];

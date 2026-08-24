export default [
  {
    path: '/s01/m15',
    name: 's01/m15',
    redirect: '/s01/m15/main',
    meta: {
      hideInMenu: true,
      title: 'LLM用量',
      notCache: true,
      icon: 'md-home'
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m15/main',
        name: 's01/m15/main',
        meta: {
          hideInMenu: true,
          title: 'LLM用量',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m15')), 'm15')
      }
    ]
  }
];

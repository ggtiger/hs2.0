export default [
  {
    path: '/s01/m27',
    name: 's01/m27',
    redirect: '/s01/m27/main',
    meta: {
      hideInMenu: true,
      title: 'AI配置中心',
      notCache: true,
      icon: 'md-settings',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m27/main',
        name: 's01/m27/main',
        meta: {
          hideInMenu: true,
          title: 'AI配置中心',
          notCache: true,
          icon: 'md-settings'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m27')), 'm27'),
      }
    ]
  }];

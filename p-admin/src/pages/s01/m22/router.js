export default [
  {
    path: '/s01/m22',
    name: 's01/m22',
    redirect: '/s01/m22/main',
    meta: {
      hideInMenu: true,
      title: '版本中心',
      notCache: true,
      icon: 'md-time',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m22/main',
        name: 's01/m22/main',
        meta: {
          hideInMenu: true,
          title: '版本中心',
          notCache: true,
          icon: 'md-time'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m22/views/main')), 'm22'),
      }
    ]
  }];

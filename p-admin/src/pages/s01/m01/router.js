export default [
  {
    path: '/s01/m01',
    name: 's01/m01',
    redirect: '/s01/m01/main',
    meta: {
      hideInMenu: true,
      title: '资源管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m01/main',
        name: 's01/m01/main',
        meta: {
          hideInMenu: true,
          title: '资源管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m01')), 'm01'),
      }
    ],
  }];

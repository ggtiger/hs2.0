export default [
  {
    path: '/s01/m17',
    name: 's01/m17',
    redirect: '/s01/m17/edit',
    meta: {
      hideInMenu: true,
      title: '代码在线开发',
      notCache: true,
      icon: 'md-code',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m17/main',
        name: 's01/m17/main',
        meta: {
          hideInMenu: true,
          title: '代码在线开发',
          notCache: true,
          icon: 'md-code'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m17')), 'm17'),
      },
      {
        path: '/s01/m17/edit',
        name: 's01/m17/edit',
        meta: {
          hideInMenu: true,
          title: '代码编辑器',
          notCache: true,
          icon: 'md-code'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m17/views/edit')), 'm17-edit'),
      }
    ]
  }];

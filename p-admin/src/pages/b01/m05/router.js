export default [
  {
    path: '/b01/m05',
    name: 'b01/m05',
    redirect: '/b01/m05/main',
    meta: {
      hideInMenu: true,
      title: '部门管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/b01/m05/main',
        name: 'b01/m05/main',
        meta: {
          hideInMenu: true,
          title: '部门管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/b01/m05')), 'm05'),
      }
    ]
  }];

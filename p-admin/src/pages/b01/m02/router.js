export default [
  {
    path: '/b01/m02',
    name: 'b01/m02',
    redirect: '/b01/m02/main',
    meta: {
      hideInMenu: true,
      title: '规程项目',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/b01/m02/main',
        name: 'b01/m02/main',
        meta: {
          hideInMenu: true,
          title: '规程项目',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/b01/m02')), 'm02'),
      }
    ]
  }];

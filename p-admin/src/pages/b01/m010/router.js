export default [
  {
    path: '/b01/m010',
    name: 'b01/m010',
    redirect: '/b01/m010/main',
    meta: {
      hideInMenu: true,
      title: '能力确认',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/b01/m010/main',
        name: 'b01/m010/main',
        meta: {
          hideInMenu: true,
          title: '能力确认',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/b01/m010')), "m010"),
      }
    ]
  }]

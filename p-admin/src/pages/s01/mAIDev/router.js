export default [
  {
    path: '/s01/mAIDev',
    name: 's01/mAIDev',
    redirect: '/s01/mAIDev/main',
    meta: {
      hideInMenu: true,
      title: 'AI开发助理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/mAIDev/main',
        name: 's01/mAIDev/main',
        meta: {
          hideInMenu: true,
          title: 'AI开发助理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/mAIDev')), 'mAIDev'),
      }
    ]
  }]

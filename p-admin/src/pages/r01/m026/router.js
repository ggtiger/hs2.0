export default [
  {
    path: '/r01/m026',
    name: 'r01/m026',
    redirect: '/r01/m026/main',
    meta: {
      hideInMenu: true,
      title: '委托审批',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/r01/m026/main',
        name: 'r01/m026/main',
        meta: {
          hideInMenu: true,
          title: '委托审批',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/r01/m026')), "r01/m026"),
      },
      {
        path: '/r01/m026/review',
        name: 'r01/m026/review',
        meta: {
          hideInMenu: true,
          title: '审批',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/r01/m026/views/review.vue')), "r01/m026"),
      }
    ]
  }
];

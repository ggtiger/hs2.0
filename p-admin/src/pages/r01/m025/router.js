export default [
  {
    path: '/r01/m025',
    name: 'r01/m025',
    redirect: '/r01/m025/main',
    meta: {
      hideInMenu: true,
      title: '委托审核',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/r01/m025/main',
        name: 'r01/m025/main',
        meta: {
          hideInMenu: true,
          title: '委托审核',
          notCache: true,
          icon: 'md-home',
        },
        component: r => require.ensure([], () => r(require('@/pages/r01/m025')), "r01/m025"),
      },
      {
        path: '/r01/m025/review',
        name: 'r01/m025/review',
        meta: {
          hideInMenu: true,
          title: '审核分屏',
          notCache: true,
          icon: 'md-home',
        },
        component: r => require.ensure([], () => r(require('@/pages/r01/m025/views/review.vue')), "r01/m025"),
      }
    ]
  }
];
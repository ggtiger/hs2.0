export default [
  {
    path: '/s01/m10',
    name: 's01/m10',
    redirect: '/s01/m10/main',
    meta: {
      hideInMenu: true,
      title: '文件管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m10/main',
        name: 's01/m10/main',
        meta: {
          hideInMenu: true,
          title: '文件管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m10')), 's01/m10'),
      }
    ]
  },
  {
    path: '/s01/m101',
    name: 's01/m101',
    redirect: '/s01/m101/main',
    meta: {
      hideInMenu: true,
      title: '文件审核',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m101/main',
        name: 's01/m101/main',
        meta: {
          hideInMenu: true,
          title: '文件审核',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m10/index1')), 's01/m10'),
      }
    ]
  },
  {
    path: '/s01/m102',
    name: 's01/m102',
    redirect: '/s01/m102/main',
    meta: {
      hideInMenu: true,
      title: '文件审批',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/m102/main',
        name: 's01/m102/main',
        meta: {
          hideInMenu: true,
          title: '文件审批',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/m10/index2')), 's01/m10'),
      }
    ]
  }
];

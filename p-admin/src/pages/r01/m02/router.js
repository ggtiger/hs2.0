export default [
  {
    path: '/r01/m02',
    name: 'r01/m02',
    redirect: '/r01/m02/main',
    meta: {
      hideInMenu: true,
      title: '原始记录',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/r01/m02/main',
        name: 'r01/m02/main',
        meta: {
          hideInMenu: true,
          title: '原始记录',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/r01/m02')), 'r01/m02'),
      }
    ]
  }, {
    path: '/r01/m021',
    name: 'r01/m021',
    redirect: '/r01/m021/main',
    meta: {
      hideInMenu: true,
      title: '记录审核',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/r01/m021/main',
        name: 'r01/m021/main',
        meta: {
          hideInMenu: true,
          title: '记录审核',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/r01/m02/index1')), 'r01/m02'),
      }
    ]
  }, {
    path: '/r01/m022',
    name: 'r01/m022',
    redirect: '/r01/m022/main',
    meta: {
      hideInMenu: true,
      title: '记录审批',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/r01/m022/main',
        name: 'r01/m022/main',
        meta: {
          hideInMenu: true,
          title: '记录审批',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/r01/m02/index2')), 'r01/m02'),
      }
    ]
  }, {
    path: '/r01/m023',
    name: 'r01/m023',
    redirect: '/r01/m023/main',
    meta: {
      hideInMenu: true,
      title: '记录签发',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/r01/m023/main',
        name: 'r01/m023/main',
        meta: {
          hideInMenu: true,
          title: '记录签发',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/r01/m02/index3')), 'r01/m02'),
      }
    ]
  }, {
    path: '/r01/m024',
    name: 'r01/m024',
    redirect: '/r01/m024/main',
    meta: {
      hideInMenu: true,
      title: '记录查询下载',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/r01/m024/main',
        name: 'r01/m024/main',
        meta: {
          hideInMenu: true,
          title: '记录查询下载',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/r01/m02/index4')), 'r01/m02'),
      }
    ]
  }];

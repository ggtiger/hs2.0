export default [
  {
    path: '/out/m01/load',
    name: 'out/m01/load',
    meta: {
      title: '受理查询',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/pages/out/m01'),
  },
  {
    path: '/out/m01/main',
    name: 'out/m01/main',
    meta: {
      hideInMenu: true,
      title: '受理查询',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/pages/out/m01/views/main.vue'),
  },
  {
    path: '/out/m01/show',
    name: 'out/m01/show',
    meta: {
      hideInMenu: true,
      title: '证书核验',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/pages/out/m01/views/show.vue'),
  }
]

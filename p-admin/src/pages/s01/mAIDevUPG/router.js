export default [
  {
    path: '/s01/mAIDevUPG',
    name: 's01/mAIDevUPG',
    redirect: '/s01/mAIDevUPG/main',
    meta: {
      hideInMenu: true,
      title: '升级管理',
      notCache: true,
      icon: 'md-home',
    },
    component: () => import('@/components/main'),
    children: [
      {
        path: '/s01/mAIDevUPG/main',
        name: 's01/mAIDevUPG/main',
        meta: {
          hideInMenu: true,
          title: '升级管理',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/mAIDevUPG')), "mAIDevUPG"),
      },
      {
        path: '/s01/mAIDevUPG/import',
        name: 's01/mAIDevUPG/import',
        meta: {
          hideInMenu: true,
          title: '导入升级包',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/mAIDevUPG/views/import.vue')), "mAIDevUPG-imp"),
      },
      {
        path: '/s01/mAIDevUPG/detail/:id',
        name: 's01/mAIDevUPG/detail',
        meta: {
          hideInMenu: true,
          title: '升级详情',
          notCache: true,
          icon: 'md-home'
        },
        component: r => require.ensure([], () => r(require('@/pages/s01/mAIDevUPG/views/detail.vue')), "mAIDevUPG-det"),
      }
    ]
  }]

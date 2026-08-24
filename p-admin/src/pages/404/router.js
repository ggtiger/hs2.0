export default [
  {
    path: '/404',
    name: '404',
    component: () => import('@/pages/404/index'),
    meta: {
      hideInMenu: true,
    },
  },
];

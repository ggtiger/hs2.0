export default [
  {
    path: '/cgdd',
    name: 'cgdd',
    component: () => import('@/components/main'),
    redirect: '/cgddSearch',
    meta: {},
    children: [
      {
        path: '/cgddSearch',
        name: 'cgddSearch',
        meta: {
          title: '采购订单查询',
          isAdd: false,
          icon: 'md-home',
        },
        component: r => require.ensure([], () => r(require('@/pages/cgdd/cgddSearch')), 'cgdd'),
      },
      {
        path: '/cgddAdd',
        name: 'cgddAdd',
        meta: {
          title: '采购订单新增',
          isAdd: true,
          icon: 'md-home',
        },
        component: r => require.ensure([], () => r(require('@/pages/cgdd/cgddAdd')), 'cgdd'),
      },
    ],
  },
];

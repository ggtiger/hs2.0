export default [
  {
    path: '/lsth',
    name: 'lsth',
    component: () => import('@/components/main'),
    redirect: '/lsthSearch',
    meta: {},
    children: [
      {
        path: '/lsthSearch',
        name: 'lsthSearch',
        meta: {
          title: '零售退货查询',
          isAdd: false,
          icon: 'md-home',
        },
        component: r => require.ensure([], () => r(require('@/pages/lsth/lsthSearch1')), 'lsth'),
      },
      {
        path: '/lsthSearch1',
        name: 'lsthSearch1',
        meta: {
          title: '零售退货查询',
          isAdd: false,
          icon: 'md-home',
        },
        component: r => require.ensure([], () => r(require('@/pages/lsth/lsthSearch1')), 'lsth'),
      },
      {
        path: '/lsthAdd',
        name: 'lsthAdd',
        meta: {
          title: '零售退货新增',
          isAdd: true,
          icon: 'md-home',
        },
        component: r => require.ensure([], () => r(require('@/pages/lsth/lsthAdd')), 'lsth'),
      },
    ],
  },
];

export default [{
  path: '/s01/m18',
  name: 's01/m18',
  redirect: '/s01/m18/config',
  meta: { hideInMenu: true, title: '模块配置' },
  component: () => import('@/components/main'),
  children: [{
    path: '/s01/m18/config',
    name: 's01/m18/config',
    meta: { hideInMenu: true, title: '模块配置' },
    component: r => require.ensure([], () => r(require('@/pages/s01/m18/views/config')), 'm18'),
  }]
}];

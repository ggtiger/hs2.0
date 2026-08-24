export default [
  {
    path: '/out/ecert',
    name: '/out/ecert',
    meta: {
      title: '电子证书验证',
      notCache: true,
      isAuth: false,
    },
    component: resolve => require(['./index.vue'], resolve)
  }
];

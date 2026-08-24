import Store from "@/store";
export default [
  {
    path: '/login',
    name: 'login',
    meta: {
      title: 'Login - 登录',
      hideInMenu: true,
    },
    component: () => import('@/pages/login/login.vue'),
  },
  {
    path: '/loginout',
    name: 'loginout',
    meta: {
      isAuth: false
    },
    beforeEnter(to, from, next) {
      Store.dispatch("user/loginOut").then(function() {
        next({ path: '/login', replace: true });
      });
    }
  }
];

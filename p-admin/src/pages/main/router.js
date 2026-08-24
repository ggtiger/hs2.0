export default [
  {
    path: "/",
    name: "home",
    component: () => import("@/components/main"),
    redirect: "/home",
    meta: {
      hideInMenu: true,
      notCache: true
    },
    children: [
      {
        path: "/home",
        name: "wodezhuye",
        meta: {
          hideInMenu: true,
          title: "首页",
          notCache: true,
          icon: "md-home"
        },
        component: r =>
          require.ensure(
            [],
            () => r(require("@/pages/main/wodezhuye")),
            "wodezhuye"
          )
      }
    ]
  }
];

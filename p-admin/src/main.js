// The Vue build version to load with the `import` command
// (runtime-only or standalone) has been set in webpack.base.conf with an alias.
import Vue from 'vue';
import App from './App';
import router from './router';
import store from './store/index';
import HeyUI from 'heyui';
import 'heyui/themes/index.less';
import '@/theme/index.less';
import '@/assets/public.css'; // 公共替换样式
import '@/assets/style.css'; // 公共替换样式
import '@/assets/fonts/rrfont.css'; // 字体样式
import Components from './components';
import filters from './utils/filters';
import './utils/extends';
import '@/sfc-loader/module-bridge.js';
import Print from '@/utils/print';
import AIAgentPlugin from '@/utils/aiAgentPlugin';

Vue.use(Print);

// 注册过滤器
Object.keys(filters).forEach(key => {
  Vue.filter(key, filters[key]);
});
Vue.use(HeyUI);
// 自定义组件
Vue.use(Components);

// 注册AI Agent插件
Vue.use(AIAgentPlugin, { store, router });

/* eslint-disable no-new */
new Vue({
  el: '#app',
  router,
  store,
  components: { App },
  template: '<App/>',
});

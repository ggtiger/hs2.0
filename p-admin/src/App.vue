<template>
  <div id="app">
    <router-view  v-if="isShow"/>
    <AssistantDrawer/>
  </div>
</template>

<script>
import AssistantDrawer from '@/components/assistant/AssistantDrawer.vue';

export default {
  name: 'App',
  components: { AssistantDrawer },

  data() {
    return {
      isShow: true
    };
  },
  methods: {

  }
};
document.body.addEventListener('touchstart', function(e) {
  e.preventDefault();
  e.stopPropagation();
});
const signalR = require('@aspnet/signalr');
let connection = new signalR.HubConnectionBuilder().withUrl('http://192.168.56.1:5011/chatHub').build();
connection.on('send', data => {
  console.log(data);
});
connection.start().then(() => {
  connection.invoke('send', 'Hello').catch(function(err) {
    return console.error(err.toString());
  });
});
</script>

<style>
#app {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'PingFang SC', 'Hiragino Sans GB', 'Microsoft YaHei', 'Helvetica Neue', Arial, sans-serif;
  font-size: 14px;
  line-height: 1.5;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  color: #434343;
}
</style>

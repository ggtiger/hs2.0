<template>
<div>
</div>
</template>
<script>
import heyui from 'heyui';
export default {
 async mounted() {
    let store = this.$store;
    if (!store.state['app'].modules['RS_M00']) {
          await this.$callAsync({ method: this.$store.dispatch, params: ['app/initModule', 'RS_M00'] });
          await this.$callAsync({ method: this.$store.dispatch, params: ['app/initDict'] });
    }
    Object.keys(store.state['app'].dicts).forEach(key => {
          heyui.addDict(key, store.state['app'].dicts[key]);
     });

    if (!store.state['app'].modules['OUT_M01']) {
     await this.$callAsync({ method: this.$store.dispatch, params: ['app/initModule', 'OUT_M01'] });
    }
    window.$heyui = heyui;
    let view = this.$route.query.view||'main';
    this.$router.push({ path: `/out/m01/${view}`,query:this.$route.query });
  },
};
</script>
<style>
</style>

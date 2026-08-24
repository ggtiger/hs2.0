<template>
  <div class="mod-add-adapter">
    <rs-mod-add :DID="did" ref="modAdd"></rs-mod-add>
  </div>
</template>

<script>
import RsModAdd from '@/pages/s01/m02/views/add.vue';

export default {
  name: 'ModAddAdapter',
  components: { RsModAdd: RsModAdd },
  props: {
    did: { type: String, default: '' }
  },
  data() {
    return {
      isOpened: false
    };
  },
  watch: {
    did(v) {
      if (v) this.loadModule(v);
    }
  },
  mounted() {
    var self = this;
    setTimeout(function() {
      self.isOpened = true;
      if (self.did) self.loadModule(self.did);
    }, 150);
  },
  methods: {
    loadModule(code) {
      this.$callAction({ action: 's01/m02/open', param: { DID: code }, isBusy: false });
    },
    setvalue(val) {
      if (!val) {
        this.$emit('saved');
      }
    }
  }
};
</script>

<style lang="less" scoped>
.mod-add-adapter {
  height: 100%;
  display: flex;
  flex-direction: column;
}
</style>

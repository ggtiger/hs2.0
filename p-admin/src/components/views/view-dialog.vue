<template>
  <div class="h-panel h-panel-no-border" v-bind="params">
    <div class="h-panel-bar vd-bar">
      <span class="h-panel-title">{{title}}</span>
       <div class="vd-header rr-text-right">
          <slot name="header"></slot>
       </div>
    </div>
    <div class="h-panel-body maxModalH rr-scroll-bar">
      <slot name="body"></slot>
    </div>
    <div class="rs-modal-footer rr-text-right h-panel-footer vd-footer" slot="footer">
      <slot name="footer"></slot>
    </div>
    <Loading :loading="loading"></Loading>
  </div>
</template>

<script>
export default {
  name: 'view-dialog',
  props: {
    title: { Type: String },
    params: { Type: Object },
    loading: { Type: Boolean, default: false },
  },
  components: {},
  data() {
    return {};
  },
  computed: {},
  methods: {},
  mounted() {},
  watch: {
    '$parent.$parent.isOpened': {
      async handler(v) {
        if (v) {
          this.$emit('on-show');
        }
      },
    },
  },
};
</script>
<style scoped>
.vd-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 20px;
  background: #fff !important;
  border-bottom: 1px solid #e8e8e8;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
  flex-shrink: 0;
}
.vd-bar >>> .h-panel-title {
  font-size: 16px;
  line-height: normal;
  font-weight: bold;
  color: #333;
  border-radius: 0 0 0 0;
}
.maxModalH {
  max-height: calc(100vh - 87px);
  overflow: auto;
}
.vd-header {
  padding-right: 10px;
}

.vd-footer {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  padding: 5px 10px;
  background: #fff;
  border-top: 1px solid #e8e8e8;
  box-shadow: 0 -1px 4px rgba(0, 0, 0, 0.08);
  flex-shrink: 0;
}
</style>

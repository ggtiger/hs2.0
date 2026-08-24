<template>
  <div class="rs-modal">
    <Modal
      class="d-width"
      v-model="modal"
      :mask-closable="false"
      :footer-hide="true"
      :closeOnMask="false"
      :fullScreen="fullScreen"
      :width="width"
      hasCloseIcon
      :middle="!fullScreen"
    >
      <div v-if="modal" :class="{'d-width':!fullScreen&&!autoWidth}">
        <slot></slot>
      </div>
    </Modal>
  </div>
</template>
<script>
export default {
  name: 'rs-modal',
  data() {
    return {
      modal: false,
      isOpened: false,
    };
  },
  props: {
    fullScreen: {
      type: Boolean,
      default: false,
    },
    autoWidth: {
      type: Boolean,
      default: false,
    },
    value: {
      type: Boolean,
      default: false,
    },
    width: {
      type: [Number, String],
      default: null,
    },
  },
  watch: {
    value: {
      handler(val) {
        this.modal = val;
      },
      immediate: true,
    },
    modal(val) {
      this.$emit('input', val);
      if (val) {
        // 与 HeyUI Modal 行为一致：延迟设置 isOpened，确保 DOM 渲染完成
        setTimeout(() => {
          this.isOpened = true;
        }, 100);
      } else {
        this.isOpened = false;
      }
    },
  },
  methods: {
    show() {
      this.modal = true;
    },
    hide() {
      this.modal = false;
    },
  },
};
</script>
<style lang="less" scoped>
</style>

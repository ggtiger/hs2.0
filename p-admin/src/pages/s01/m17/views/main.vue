<template>
  <div class="sfc-main-redirect">
    <span class="sfc-main-spinner"></span>
  </div>
</template>
<script>
export default {
  name: 's01-m17-main',
  created() {
    this.$router.replace({ name: 's01/m17/edit' });
  },
};
</script>
<style scoped>
.sfc-main-redirect {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100vh;
}
.sfc-main-spinner {
  display: inline-block;
  width: 28px;
  height: 28px;
  border: 3px solid #ddd;
  border-top-color: #0a84ff;
  border-radius: 50%;
  animation: sfc-main-spin 0.6s linear infinite;
}
@keyframes sfc-main-spin {
  to { transform: rotate(360deg); }
}
</style>

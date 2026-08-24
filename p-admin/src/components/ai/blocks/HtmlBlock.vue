<template>
  <div class="asst-html" v-html="safe"></div>
</template>

<script>
import DOMPurify from 'dompurify';

export default {
  name: 'HtmlBlock',
  props: {
    html: { type: String, default: '' }
  },
  computed: {
    safe() {
      // 白名单 sanitize，剥离 script/事件/iframe 等
      return DOMPurify.sanitize(this.html, { ADD_ATTR: ['target'] });
    }
  }
};
</script>

<style scoped>
.asst-html {
  margin: 6px 0;
  word-break: break-word;
}
.asst-html >>> table {
  border-collapse: collapse;
  margin: 6px 0;
}
.asst-html >>> th,
.asst-html >>> td {
  border: 1px solid #ddd;
  padding: 4px 8px;
}
</style>

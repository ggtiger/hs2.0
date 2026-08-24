<template>
  <div class="preview-page">
    <div class="preview-header">
      <span class="preview-title">模版模拟预览</span>
      <span class="preview-tip">以下文档由模拟数据填充生成，仅用于预览字段绑定效果</span>
      <Button size="s" @click.native="goBack">关闭</Button>
    </div>
    <div class="preview-body">
      <rs-onlyoffice-preview
        v-if="fileId"
        :fileId="fileId"
        fileType="docx"
        title="模版预览"
      ></rs-onlyoffice-preview>
    </div>
  </div>
</template>
<script>
export default {
  name: 's01-m12-preview',
  data() {
    return {
      fileId: '',
    };
  },
  created() {
    this.fileId = this.$route.query.fileId || '';
  },
  methods: {
    goBack() {
      // 优先关闭 Tab，否则回退
      if (this.$store && this.$store.state['app'] && this.$store.state['app'].closeTag) {
        this.$store.state['app'].closeTag(this.$route.fullPath);
      }
      this.$router.go(-1);
    },
  },
};
</script>
<style lang="less" scoped>
.preview-page {
  width: 100%;
  height: 100vh;
  display: flex;
  flex-direction: column;
  background: #f5f5f5;
}
.preview-header {
  display: flex;
  align-items: center;
  padding: 8px 16px;
  background: #fff;
  border-bottom: 1px solid #e8e8e8;
  flex-shrink: 0;
}
.preview-title {
  font-size: 15px;
  font-weight: 500;
  margin-right: 12px;
}
.preview-tip {
  font-size: 12px;
  color: #999;
  flex: 1;
}
.preview-body {
  flex: 1;
  overflow: hidden;
}
</style>

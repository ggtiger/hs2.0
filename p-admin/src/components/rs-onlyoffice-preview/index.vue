<template>
  <div class="rs-onlyoffice-preview">
    <div v-if="loading" class="rs-onlyoffice-loading">
      <i class="h-icon-loading" style="font-size: 32px;"></i>
      <p>正在加载文档...</p>
    </div>
    <div v-if="error" class="rs-onlyoffice-error">
      <i class="h-icon-error" style="font-size: 32px; color: #ed4014;"></i>
      <p>{{ error }}</p>
    </div>
    <div :id="editorId" class="rs-onlyoffice-editor"></div>
  </div>
</template>

<script>
import db from '@/api/db';
import { loadOnlyOfficeScript } from '@/components/rs-onlyoffice-shared';

export default {
  name: 'RsOnlyofficePreview',
  props: {
    // 文件ID（对应后端 TSS_FILE 表的 ID）
    fileId: {
      type: String,
      default: '',
    },
    // 文件类型（docx, xlsx, pptx 等）
    fileType: {
      type: String,
      default: 'docx',
    },
    // 文件标题
    title: {
      type: String,
      default: '',
    },
  },
  data: function() {
    return {
      editorId: 'onlyoffice-editor-' + Math.random().toString(36).substr(2, 9),
      loading: false,
      error: '',
      docEditor: null,
    };
  },
  watch: {
    fileId: {
      handler: function(val) {
        if (val) {
          this.initEditor();
        } else {
          this.destroyEditor();
        }
      },
      immediate: true,
    },
  },
  beforeDestroy: function() {
    this.destroyEditor();
  },
  methods: {
    initEditor: async function() {
      var self = this;
      if (!self.fileId) {
        return;
      }
      self.loading = true;
      self.error = '';
      self.destroyEditor();

      try {
        // 加载 OnlyOffice API 脚本
        await loadOnlyOfficeScript();

        if (!window.DocsAPI) {
          throw new Error('OnlyOffice API 不可用');
        }

        // 构建文件下载 URL，Document Server 需要能访问此 URL
        var uploadUrl = db.getUrl('upload');
        var fileUrl = uploadUrl.replace('127.0.0.1', 'host.docker.internal').replace('localhost', 'host.docker.internal') + self.fileId;
        var docKey = self.fileId + '_' + Date.now();
        var docTitle = self.title || ('\u6587\u6863.' + self.fileType);

        // 使用 embedded 模式，只读预览，界面更简洁
        // type: "embedded" 会让 DocsAPI 使用 embed/index.html 页面
        var config = {
          type: 'embedded',
          document: {
            fileType: self.fileType,
            key: docKey,
            title: docTitle,
            url: fileUrl,
            permissions: {
              edit: false,
              download: true,
              print: true,
            },
          },
          documentType: self.getDocumentType(self.fileType),
          editorConfig: {
            lang: 'zh-CN',
            embedded: {
              toolbarDocked: 'top',
            },
            customization: {
              layout: {
                toolbar: false,
              },
            },
          },
        };

        await self.$nextTick();

        self.docEditor = new window.DocsAPI.DocEditor(self.editorId, config);
        self.loading = false;
      } catch (e) {
        console.error('OnlyOffice 初始化失败', e);
        self.error = e.message || '文档加载失败';
        self.loading = false;
      }
    },
    destroyEditor: function() {
      if (this.docEditor) {
        try {
          this.docEditor.destroyEditor();
        } catch (e) {
          // 忽略销毁错误
        }
        this.docEditor = null;
      }
    },
    getDocumentType: function(fileType) {
      var typeMap = {
        doc: 'word',
        docx: 'word',
        docm: 'word',
        dot: 'word',
        dotx: 'word',
        dotm: 'word',
        odt: 'word',
        fodt: 'word',
        rtf: 'word',
        txt: 'word',
        html: 'word',
        htm: 'word',
        mht: 'word',
        pdf: 'word',
        djvu: 'word',
        fb2: 'word',
        epub: 'word',
        xps: 'word',
        xls: 'cell',
        xlsx: 'cell',
        xlsm: 'cell',
        xlt: 'cell',
        xltx: 'cell',
        xltm: 'cell',
        ods: 'cell',
        fods: 'cell',
        csv: 'cell',
        ppt: 'slide',
        pptx: 'slide',
        pptm: 'slide',
        pot: 'slide',
        potx: 'slide',
        potm: 'slide',
        odp: 'slide',
        fodp: 'slide',
      };
      return typeMap[fileType.toLowerCase()] || 'word';
    },
  },
};
</script>

<style lang="less" scoped>
.rs-onlyoffice-preview {
  top: -28px;
  width: 100%;
  height: calc(100% + 28px);
  position: relative;
}
.rs-onlyoffice-loading,
.rs-onlyoffice-error {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  height: 100%;
  color: #999;
  font-size: 14px;
  p {
    margin-top: 10px;
  }
}
.rs-onlyoffice-editor {
  width: 100%;
  height: 100%;
  overflow: hidden;
}
</style>

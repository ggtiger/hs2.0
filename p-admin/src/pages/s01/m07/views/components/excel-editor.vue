<template>
  <rs-modal ref="modal" :width="1200" style="z-index:9999">
    <div class="excel-editor-header">
      <span class="excel-editor-title">Excel 编辑表格<span v-if="statusText" style="color:#999;font-size:12px;margin-left:8px;">{{ statusText }}</span></span>
      <div class="excel-editor-actions">
        <Button size="s" @click.native="cancel">取消</Button>
        <Button size="s" color="primary" @click.native="saveAndClose">保存并返回</Button>
      </div>

    </div>
    <div class="excel-editor-wrapper">
      <div v-if="loading" class="excel-editor-overlay">
        <i class="h-icon-loading" style="font-size: 32px;"></i>
        <p>{{ loadingText }}</p>
      </div>
      <div v-if="error" class="excel-editor-overlay">
        <i class="h-icon-error" style="font-size: 32px; color: #ed4014;"></i>
        <p>{{ error }}</p>
        <Button @click.native="retry">重试</Button>
      </div>
      <div :id="editorId" class="excel-editor-container"></div>
    </div>
  </rs-modal>
</template>

<script>
// eslint-disable-next-line no-restricted-imports
import db from '@/api/db';
import { loadOnlyOfficeScript } from '@/components/rs-onlyoffice-shared';

// excel-editor 使用自身特有的 httpGet/httpPost（含 forceSave 逻辑），不使用共享模块

function httpPost(url, data, token) {
  return new Promise(function(resolve, reject) {
    var xhr = new XMLHttpRequest();
    xhr.open('POST', url);
    xhr.setRequestHeader('Content-Type', 'application/json');
    if (token) xhr.setRequestHeader('Authorization', 'Bearer ' + token);
    xhr.onload = function() {
      if (xhr.status === 200) {
        resolve(JSON.parse(xhr.responseText));
      } else {
        try {
          var err = JSON.parse(xhr.responseText);
          reject(new Error(err.Message || '请求失败: ' + xhr.status));
        } catch (e) {
          reject(new Error('请求失败: ' + xhr.status));
        }
      }
    };
    xhr.onerror = function() { reject(new Error('网络错误')) };
    xhr.send(JSON.stringify(data));
  });
}

function httpGet(url, token) {
  return new Promise(function(resolve, reject) {
    var xhr = new XMLHttpRequest();
    xhr.open('GET', url);
    if (token) xhr.setRequestHeader('Authorization', 'Bearer ' + token);
    xhr.onload = function() {
      if (xhr.status === 200) {
        resolve(JSON.parse(xhr.responseText));
      } else if (xhr.status === 202) {
        // 202 = OnlyOffice 尚未保存完成，需要继续等待
        var err = new Error('保存中，请稍后');
        err.status = 202;
        reject(err);
      } else {
        reject(new Error('请求失败: ' + xhr.status));
      }
    };
    xhr.onerror = function() { reject(new Error('网络错误')) };
    xhr.send();
  });
}

/**
 * 从 HTML 中提取 ${字段名} 占位符，生成 fields 数组
 */
function extractFieldsFromHtml(html, existingFields) {
  var fields = [];
  var seen = {};
  var FIELD_RE = /\$\{([^\}]+)\}/g;
  var match;
  while ((match = FIELD_RE.exec(html)) !== null) {
    var fieldName = match[1];
    if (seen[fieldName]) continue;
    seen[fieldName] = true;

    var existingField = null;
    if (existingFields) {
      existingField = existingFields.find(function(f) { return f.field === fieldName });
    }

    if (existingField) {
      fields.push(Object.assign({}, existingField, { value: '' }));
    } else {
      fields.push({
        field: fieldName,
        name: '',
        value: '',
        width: '100%',
        height: '100%',
        fieldType: 'text',
        textMore: false,
        readonly: false,
        isnotnull: false,
        dvalue: '',
        formula: '',
        minv: '',
        maxv: '',
        data: '',
        helpInfo: ''
      });
    }
  }

  return { value: html, fields: fields };
}

export default {
  name: 'ExcelEditor',
  data: function() {
    return {
      editorId: 'excel-editor-' + Math.random().toString(36).substr(2, 9),
      loading: false,
      loadingText: '正在加载编辑器...',
      statusText: '',
      error: '',
      docEditor: null,
      fileKey: '',
      itemEditorData: null
    };
  },
  beforeDestroy: function() {
    this.destroyEditor();
  },
  methods: {
    /**
     * 打开编辑器
     * @param {Object} data - {value: HTML字符串, fields: 字段数组}
     */
    open: function(data) {
      var self = this;
      self.itemEditorData = data;
      self.loading = true;
      self.loadingText = '正在转换表格内容...';
      self.error = '';
      self.statusText = '';

      // 使用 rs-modal 的 show() 方法打开弹窗
      self.$refs.modal.show();

      self.$nextTick(function() {
        self.initEditor();
      });
    },

    /**
     * 用 OnlyOffice 配置加载编辑器
     */
    loadEditorWithConfig: function(configResponse) {
      var self = this;
      return loadOnlyOfficeScript().then(function() {
        if (!window.DocsAPI) {
          throw new Error('OnlyOffice API 不可用');
        }
        return configResponse;
      }).then(function(configResponse) {
        self.destroyEditor();

        var config = Object.assign({}, configResponse, {
          events: {
            onDocumentReady: function() {
              self.loading = false;
              self.statusText = '编辑器已就绪';
            },
            onError: function(event) {
              self.error = '编辑器错误: ' + (event.data.errorDescription || '未知错误');
              self.loading = false;
            }
          }
        });

        self.docEditor = new window.DocsAPI.DocEditor(self.editorId, config);
      });
    },

    /**
     * 初始化编辑器
     * 流程：HTML → 发送到后端 html-to-xlsx API → NPOI 转换 → OnlyOffice 打开
     */
    initEditor: function() {
      var self = this;
      var apiUrl = db.getUrl('url');
      var token = self.$store.state['user'].access_token;

      try {
        var htmlValue = self.itemEditorData.value || '';

        // 1. 发送 HTML 到后端，由 NPOI 转为 xlsx
        //    如果没有 HTML 表格，使用空白 xlsx
        if (htmlValue && htmlValue.indexOf('<table') >= 0) {
          self.loadingText = '正在转换表格...';

          httpPost(apiUrl + '/api/exceleditor/html-to-xlsx', {
            html: htmlValue,
            fileName: 'template.xlsx',
            fields: self.itemEditorData.fields || []
          }, token).then(function(uploadResult) {
            if (!uploadResult.key) {
              throw new Error('HTML转Excel失败');
            }
            self.fileKey = uploadResult.key;
            self.loadingText = '正在加载编辑器...';
            return httpGet(apiUrl + '/api/exceleditor/editor-config?key=' + self.fileKey, token);
          }).then(function(configResponse) {
            return self.loadEditorWithConfig(configResponse);
          }).catch(function(e) {
            console.error('Excel 编辑器初始化失败', e);
            self.error = e.message || '编辑器加载失败';
            self.loading = false;
          });
        } else {
          // 没有 HTML 表格，创建空白 xlsx
          self.loadingText = '正在创建空白工作表...';
          httpPost(apiUrl + '/api/exceleditor/create-blank', {
            fileName: 'template.xlsx'
          }, token).then(function(createResult) {
            if (!createResult.key) {
              throw new Error('创建工作表失败');
            }
            self.fileKey = createResult.key;
            self.loadingText = '正在加载编辑器...';
            return httpGet(apiUrl + '/api/exceleditor/editor-config?key=' + self.fileKey, token);
          }).then(function(configResponse) {
            return self.loadEditorWithConfig(configResponse);
          }).catch(function(e) {
            console.error('Excel 编辑器初始化失败', e);
            self.error = e.message || '编辑器加载失败';
            self.loading = false;
          });
        }
      } catch (e) {
        console.error('Excel 转换失败', e);
        self.error = '表格转换失败: ' + e.message;
        self.loading = false;
      }
    },

    /**
     * 保存并关闭
     * 流程：OnlyOffice 保存 → export-html API（后端解析xlsx生成HTML） → extractFieldsFromHtml → HTML + fields
     */
    saveAndClose: function() {
      var self = this;
      if (!self.docEditor) {
        self.$error('编辑器未就绪');
        return;
      }

      var busy = self.$busy('正在保存...');
      self.statusText = '正在保存...';

      var apiUrl = db.getUrl('url');
      var token = self.$store.state['user'].access_token;
      var maxRetries = 10;
      var retryDelay = 2000;

      // 轮询 export-html：后端会先调用 OnlyOffice forcesave，然后转 HTML 返回
      function tryExport(retryCount) {
        httpGet(apiUrl + '/api/exceleditor/export-html?key=' + self.fileKey, token).then(function(exportResult) {
          if (!exportResult.data) {
            throw new Error('获取编辑后的文件失败');
          }

          var htmlValue = exportResult.data;
          var result = extractFieldsFromHtml(htmlValue, self.itemEditorData.fields);

          // 将后端返回的公式定义合并到 fields 中
          if (exportResult.formulas) {
            result.fields.forEach(function(f) {
              var formulaKey = f.field;
              if (exportResult.formulas[formulaKey]) {
                f.formula = exportResult.formulas[formulaKey];
              }
            });
          }

          self.$emit('save', {value: result.value, fields: result.fields});
          self.destroyEditor();
          self.$free(busy);
          self.$alert('保存成功');
          self.close();
        }).catch(function(e) {
          if (retryCount < maxRetries) {
            self.statusText = '正在保存...(' + (retryCount + 1) + ')';
            setTimeout(function() { tryExport(retryCount + 1) }, retryDelay);
          } else {
            self.$free(busy);
            self.$error('保存失败: ' + e.message);
          }
        });
      }

      // 首次尝试延迟 1 秒
      setTimeout(function() { tryExport(0) }, 1000);
    },

    cancel: function() {
      this.close();
    },

    close: function() {
      this.destroyEditor();
      this.$refs.modal.hide();
    },

    retry: function() {
      this.error = '';
      this.loading = true;
      this.initEditor();
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
    }
  }
};
</script>

<style lang="less" scoped>
.excel-editor-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 16px;
  border-bottom: 1px solid #e8e8e8;
  background: #fff;
  position: relative;
  z-index: 20;
}
.excel-editor-title {
  font-size: 16px;
  font-weight: 500;
  color: #333;
}
.excel-editor-actions {
  display: flex;
  gap: 8px;
}
.excel-editor-wrapper {
  width: 100%;
  height: 600px;
  position: relative;
  overflow: hidden;
}
.excel-editor-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  background: #fff;
  z-index: 10;
  color: #999;
  font-size: 14px;
  p {
    margin-top: 10px;
  }
}
.excel-editor-container {
  width: 100%;
  height: 600px;
  overflow: hidden;
}
</style>

<template>
<div>
  <rs-modal ref="modal" :fullScreen="true" style="z-index:9999">
    <div class="word-template-header">
      <span class="word-template-title">Word 模版编辑<span v-if="statusText" style="color:#999;font-size:12px;margin-left:8px;">{{ statusText }}</span></span>
      
      <div class="word-template-actions">
        <button class="h-btn h-btn-s" @click="triggerUpload">
          <i class="h-icon-upload" style="margin-right:4px;"></i>上传模版
        </button>
        <button class="h-btn h-btn-s" @click="exportFile">
          <i class="h-icon-download" style="margin-right:4px;"></i>导出
        </button>
        <button class="h-btn h-btn-s h-btn-green" @click="previewTemplate">
          <i class="h-icon-search" style="margin-right:4px;"></i>模拟预览
        </button>
        <Button size="s" @click.native="cancel">取消</Button>
        <Button size="s" color="primary" @click.native="saveAndClose">保存并返回</Button>
      </div>
     
    </div>
    <input ref="fileInput" type="file" accept=".docx" style="display:none" @change="handleFileUpload" />
    <div class="word-template-wrapper">
      <div v-if="loading" class="word-template-overlay">
        <i class="h-icon-loading" style="font-size: 32px;"></i>
        <p>{{ loadingText }}</p>
      </div>
      <div v-if="error" class="word-template-overlay">
        <i class="h-icon-error" style="font-size: 32px; color: #ed4014;"></i>
        <p>{{ error }}</p>
        <Button @click.native="retry">重试</Button>
      </div>

      <!-- 左侧字段面板 -->
      <div class="word-template-fields-panel">
        <div class="fields-panel-header">
          <span class="h-icon-search" style="margin-right:4px;"></span>
          <input
            type="text"
            v-model="fieldSearch"
            placeholder="搜索字段..."
            class="fields-search-input"
          />
        </div>

        <!-- 字段来源选择 -->
        <div class="fields-source-section">
          <div class="fields-source-title">字段来源</div>
          <div class="fields-source-item">
            <label>业务模块</label>
            <Select
              v-model="selectedModule"
              :datas="moduleOptions"
              @change="loadOrmFields"
              placeholder="选择模块"
              style="width:100%"
              size="s"
            ></Select>
          </div>
          <div class="fields-source-item">
            <label>模版</label>
            <Select
              v-model="selectedTemplate"
              :datas="templateOptions"
              @change="loadTemplateFields"
              placeholder="选择模版"
              style="width:100%"
              size="s"
            ></Select>
          </div>
        </div>

        <!-- 字段列表 -->
        <div class="fields-list rr-scroll-bar">
          <div
            v-for="group in filteredFieldGroups"
            :key="group.name"
            class="fields-group"
          >
            <div class="fields-group-title" @click="toggleGroup(group.name)">
              <span :class="collapsedGroups[group.name] ? 'h-icon-right' : 'h-icon-down'" style="font-size:12px;margin-right:4px;"></span>
              {{ group.name }}
              <span class="fields-count">{{ group.fields.length }}</span>
            </div>
            <div v-show="!collapsedGroups[group.name]" class="fields-group-body">
              <template v-for="field in group.fields">
                <!-- 表格字段：点击标记循环行，右侧图标展开子字段（用于插入到各列单元格） -->
                <div
                  v-if="field.type === 'table'"
                  :key="field.key"
                  class="field-item field-item-table"
                  :title="'①在 Word 中选中表格数据行(模板行) → ②点击此处标记为循环行 → ③展开后把字段插入到各列单元格完成列绑定: ' + field.key"
                  @click="bindTableField(field)"
                >
                  <span class="field-type-icon type-table">{{ getTypeIcon('table') }}</span>
                  <span class="field-key">{{ field.label }}</span>
                  <span class="field-bind-btn" @click.stop="bindTableField(field)">标记循环行</span>
                  <span
                    class="field-toggle-btn"
                    @click.stop="toggleTableExpand(field.key)"
                  >
                    <span :class="tableExpanded[field.key] ? 'h-icon-down' : 'h-icon-right'" style="font-size:12px;"></span>
                  </span>
                </div>
                <!-- 列绑定提示行（展开时显示） -->
                <div
                  v-if="field.type === 'table' && tableExpanded[field.key] && field.children"
                  :key="field.key + '_tip'"
                  class="field-col-bind-tip"
                >
                  把下列字段插入到表格各列单元格 = 定义列绑定
                </div>
                <!-- 表格子字段 -->
                <template v-if="field.type === 'table' && tableExpanded[field.key] && field.children">
                  <div
                    v-for="child in field.children"
                    :key="field.key + '_' + child.key"
                    class="field-item field-item-child"
                    :class="{ 'field-highlighted': isFieldHighlighted(child.key) }"
                    :title="'点击右侧 + 插入子字段: ' + child.key"
                  >
                    <span
                      class="field-type-icon type-icon-editable"
                      :class="'type-' + (child.type || 'text')"
                      :title="'当前类型: ' + getTypeLabel(child.type) + '（点击修改）'"
                      @click.stop="toggleTypeMenu(child, field.key)"
                    >{{ getTypeIcon(child.type) }}<i class="type-edit-caret"></i></span>
                    <span class="field-key">{{ child.key }}</span>
                    <span class="field-label">{{ child.label }}</span>
                    <span class="field-insert-btn h-icon-plus" title="插入此字段" @click.stop="insertField(child)"></span>
                    <div
                      v-if="activeTypeFieldKey === field.key + '_' + child.key"
                      class="field-type-menu"
                      @click.stop=""
                    >
                      <div
                        v-for="opt in fieldTypeOptions"
                        :key="opt.key"
                        class="field-type-menu-item"
                        :class="{ active: (child.type || 'text') === opt.key }"
                        @click.stop="changeFieldType(child, opt.key)"
                      >
                        <span class="field-type-icon" :class="'type-' + opt.key" style="width:18px;height:18px;line-height:18px;font-size:9px;">{{ getTypeIcon(opt.key) }}</span>
                        <span>{{ opt.title }}</span>
                        <i v-if="(child.type || 'text') === opt.key" class="h-icon-check" style="margin-left:auto;font-size:12px;"></i>
                      </div>
                    </div>
                  </div>
                </template>
                <!-- 日期字段：点击 + 插入完整日期，右侧图标展开年/月/日后缀 -->
                <div
                  v-if="field.type === 'date'"
                  :key="field.key"
                  class="field-item"
                  :class="{ 'field-highlighted': isFieldHighlighted(field.key) }"
                  :title="'点击 + 插入完整日期；点击右侧图标展开年/月/日: ' + field.key"
                >
                  <span class="field-type-icon type-date">D</span>
                  <span class="field-key">{{ field.key }}</span>
                  <span class="field-label">{{ field.label }}</span>
                  <span class="field-insert-btn h-icon-plus" title="插入完整日期" @click.stop="insertField(field)"></span>
                  <span class="field-toggle-btn" @click.stop="toggleDateExpand(field.key)" title="展开年/月/日">
                    <span :class="dateExpanded[field.key] ? 'h-icon-down' : 'h-icon-right'" style="font-size:12px;"></span>
                  </span>
                </div>
                <!-- 日期子选项：年/月/日 -->
                <template v-if="field.type === 'date' && dateExpanded[field.key]">
                  <div
                    v-for="suf in [{k:'_YY',label:'年'},{k:'_MM',label:'月'},{k:'_DD',label:'日'}]"
                    :key="field.key + suf.k"
                    class="field-item field-item-child"
                  >
                    <span class="field-type-icon type-suffix">S</span>
                    <span class="field-key">{{ field.key }}{{ suf.k }}</span>
                    <span class="field-label">{{ suf.label }}</span>
                    <span class="field-insert-btn h-icon-plus" :title="'插入' + suf.label" @click.stop="insertDateSuffix(field, suf.k, suf.label)"></span>
                  </div>
                </template>
                <!-- 普通字段（非 table、非 date） -->
                <div
                  v-if="field.type !== 'table' && field.type !== 'date'"
                  :key="field.key"
                  class="field-item"
                  :class="{ 'field-highlighted': isFieldHighlighted(field.key) }"
                  :title="'点击右侧 + 插入: ' + field.key"
                >
                  <span
                    class="field-type-icon type-icon-editable"
                    :class="'type-' + field.type"
                    :title="'当前类型: ' + getTypeLabel(field.type) + '（点击修改）'"
                    @click.stop="toggleTypeMenu(field)"
                  >{{ getTypeIcon(field.type) }}<i class="type-edit-caret"></i></span>
                  <span class="field-key">{{ field.key }}</span>
                  <span class="field-label">{{ field.label }}</span>
                  <span class="field-insert-btn h-icon-plus" title="插入此字段" @click.stop="insertField(field)"></span>
                  <!-- 类型选择浮层 -->
                  <div
                    v-if="activeTypeFieldKey === field.key"
                    class="field-type-menu"
                    @click.stop=""
                  >
                    <div
                      v-for="opt in fieldTypeOptions"
                      :key="opt.key"
                      class="field-type-menu-item"
                      :class="{ active: (field.type || 'text') === opt.key }"
                      @click.stop="changeFieldType(field, opt.key)"
                    >
                      <span class="field-type-icon" :class="'type-' + opt.key" style="width:18px;height:18px;line-height:18px;font-size:9px;">{{ getTypeIcon(opt.key) }}</span>
                      <span>{{ opt.title }}</span>
                      <i v-if="(field.type || 'text') === opt.key" class="h-icon-check" style="margin-left:auto;font-size:12px;"></i>
                    </div>
                  </div>
                </div>
              </template>
            </div>
          </div>

          <!-- 手动添加 -->
          <div class="fields-group">
            <div class="fields-group-title" @click="toggleGroup('manual')">
              <span :class="collapsedGroups['manual'] ? 'h-icon-right' : 'h-icon-down'" style="font-size:12px;margin-right:4px;"></span>
              手动添加
            </div>
            <div v-show="!collapsedGroups['manual']" class="fields-group-body">
              <div class="manual-field-form">
                <input type="text" v-model="manualField.key" placeholder="字段标识 (如 CERTCODE)" class="manual-input" />
                <input type="text" v-model="manualField.label" placeholder="字段名称 (如 证书编号)" class="manual-input" />
                <Select v-model="manualField.type" :datas="fieldTypeOptions" style="width:100%" size="s"></Select>
                <Button size="s" color="primary" @click.native="addManualField" style="width:100%">添加字段</Button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 中间 OnlyOffice 编辑器 -->
      <div class="word-template-editor-area">
        <div :id="editorId" class="word-template-editor-container"></div>
      </div>
    </div>
  </rs-modal>

  <!-- 模拟预览弹窗 -->
  <rs-modal ref="previewModal" :fullScreen="true" style="z-index:10000">
    <div class="preview-header">
      <span class="preview-title">模版模拟预览</span>
      <span class="preview-tip">以下文档由模拟数据填充生成，仅用于预览字段绑定效果</span>
    </div>
    <div class="preview-body">
      <rs-onlyoffice-preview
        v-if="previewFileId"
        :fileId="previewFileId"
        fileType="docx"
        title="模版预览"
      ></rs-onlyoffice-preview>
    </div>
  </rs-modal>
</div>
</template>
<script>
import db from '@/api/db';
import { loadOnlyOfficeScript, httpGet, httpPost } from '@/components/rs-onlyoffice-shared';

export default {
  name: 'RsWordTemplateEditor',
  props: {
    moduleCode: {
      type: String,
      default: ''
    },
    templateId: {
      type: String,
      default: ''
    },
    businessType: {
      type: String,
      default: 'cert'
    }
  },
  data: function() {
    return {
      editorId: 'word-template-' + Math.random().toString(36).substr(2, 9),
      loading: false,
      loadingText: '正在加载编辑器...',
      statusText: '',
      error: '',
      docEditor: null,
      previewFileId: '',
      fileKey: '',
      tempKey: '',
      fileId: '',
      fileName: '',

      fieldSearch: '',
      fieldGroups: [],
      collapsedGroups: {},
      tableExpanded: {},
      dateExpanded: {},
      selectedModule: '',
      selectedTemplate: '',
      moduleOptions: [],
      templateOptions: [],

      manualField: { key: '', label: '', type: 'text' },
      fieldTypeOptions: [
        { key: 'text', title: '文本' },
        { key: 'date', title: '日期' },
        { key: 'image', title: '图片' },
        { key: 'html', title: '富文本' },
        { key: 'table', title: '表格' }
      ],

      activeTypeFieldKey: '',
      activeHighlightKey: '',
      existingFields: [],

      // 手动添加字段（独立保存，loadFields 不覆盖）
      manualFields: []
    };
  },
  computed: {
    filteredFieldGroups: function() {
      var self = this;
      if (!self.fieldSearch) return self.fieldGroups;
      var keyword = self.fieldSearch.toLowerCase();
      return self.fieldGroups.map(function(group) {
        return {
          name: group.name,
          source: group.source,
          fields: group.fields.filter(function(f) {
            if (f.key.toLowerCase().indexOf(keyword) >= 0
              || f.label.toLowerCase().indexOf(keyword) >= 0) {
              return true;
            }
            // 表格字段：子字段命中时也保留父项，并自动展开
            if (f.type === 'table' && f.children) {
              var childHit = f.children.some(function(c) {
                return (c.key || '').toLowerCase().indexOf(keyword) >= 0
                  || (c.label || '').toLowerCase().indexOf(keyword) >= 0;
              });
              if (childHit) {
                self.$set(self.tableExpanded, f.key, true);
                return true;
              }
            }
            return false;
          })
        };
      }).filter(function(g) { return g.fields.length > 0; });
    }
  },
  beforeDestroy: function() {
    this.destroyEditor();
    document.removeEventListener('click', this.onGlobalClickCloseTypeMenu);
  },
  mounted: function() {
    document.addEventListener('click', this.onGlobalClickCloseTypeMenu);
  },
  methods: {
    open: function(fileId) {
      var self = this;
      self.fileId = fileId || '';
      self.loading = true;
      self.loadingText = '正在加载模版...';
      self.error = '';
      self.statusText = '';

      self.$refs.modal.show();

      self.$nextTick(function() {
        self.initEditor();
        self.loadFieldConfig();
        self.loadModuleOptions();
        self.loadTemplateOptions();
      });
    },

    /**
     * 从数据库加载字段来源配置
     */
    loadFieldConfig: function() {
      var self = this;
      var apiUrl = db.getUrl('url');
      var token = self.$store.state['user'].access_token;

      // 先清空上一次的选择
      self.selectedModule = '';
      self.selectedTemplate = '';
      self.manualFields = [];

      httpGet(apiUrl + '/api/word-template/field-config?fileId=' + self.fileId, token)
        .then(function(result) {
          // 已保存的配置优先；无配置时用 props(moduleCode/templateId)作初始值，与字段列表保持一致
          self.selectedModule = result.moduleCode || self.moduleCode || '';
          self.selectedTemplate = result.templateId || self.templateId || '';
          if (result.manualFields && Array.isArray(result.manualFields)) {
            self.manualFields = result.manualFields;
          }
          // 配置加载完成后再加载字段列表
          self.loadFields();
        }).catch(function(e) {
          console.error('加载字段配置失败', e);
          self.loadFields();
        });
    },

    /**
     * 保存字段来源配置到数据库
     */
    savePrefs: function() {
      var self = this;
      var apiUrl = db.getUrl('url');
      var token = self.$store.state['user'].access_token;

      httpPost(apiUrl + '/api/word-template/field-config', {
        fileId: self.fileId,
        moduleCode: self.selectedModule,
        templateId: self.selectedTemplate,
        manualFields: self.manualFields
      }, token).catch(function(e) {
        console.error('保存字段配置失败', e);
      });
    },

    initEditor: function() {
      var self = this;
      var apiUrl = db.getUrl('url');
      var token = self.$store.state['user'].access_token;

      if (!self.fileId) {
        self.error = '未指定模版文件';
        self.loading = false;
        return;
      }

      self.loadingText = '正在获取编辑器配置...';

      httpGet(apiUrl + '/api/word-template/editor-config/' + self.fileId, token)
        .then(function(configResponse) {
          self.fileKey = configResponse.document.key;
          self.tempKey = configResponse._tempKey || '';
          self.fileName = configResponse.document.title || '';
          self.loadingText = '正在加载编辑器...';

          if (configResponse._existingFields) {
            self.existingFields = configResponse._existingFields;
          }

          return loadOnlyOfficeScript().then(function() {
            if (!window.DocsAPI) {
              throw new Error('OnlyOffice API 不可用');
            }

            self.destroyEditor();

            var config = Object.assign({}, configResponse, {
              events: {
                onDocumentReady: function() {
                  self.loading = false;
                  self.statusText = '编辑器已就绪';
                  self.setupContentControlSync();
                },
                onError: function(event) {
                  self.error = '编辑器错误: ' + (event.data.errorDescription || '未知错误');
                  self.loading = false;
                }
              }
            });

            delete config._existingFields;
            delete config._tempKey;

            self.docEditor = new window.DocsAPI.DocEditor(self.editorId, config);
          });
        })
        .catch(function(e) {
          console.error('Word 模版编辑器初始化失败', e);
          self.error = e.message || '编辑器加载失败';
          self.loading = false;
        });
    },

    loadFields: function() {
      var self = this;
      var apiUrl = db.getUrl('url');
      var token = self.$store.state['user'].access_token;

      var params = [];
      // 用户当前选择优先，props(moduleCode/templateId)仅作为初始兜底
      // 否则保存配置后再打开、改选下拉时字段列表会被 prop 锁死不变
      if (self.selectedModule || self.moduleCode) {
        params.push('moduleCode=' + (self.selectedModule || self.moduleCode));
      }
      if (self.selectedTemplate || self.templateId) {
        params.push('templateId=' + (self.selectedTemplate || self.templateId));
      }
      if (self.businessType) {
        params.push('type=' + self.businessType);
      }

      var url = apiUrl + '/api/word-template/fields';
      if (params.length > 0) {
        url += '?' + params.join('&');
      }

      httpGet(url, token).then(function(result) {
        if (result.groups) {
          self.fieldGroups = result.groups;
        }
        // 追加手动添加分组
        if (self.manualFields.length > 0) {
          self.fieldGroups.push({
            name: '手动添加',
            source: 'manual',
            fields: self.manualFields.slice()
          });
        }
      }).catch(function(e) {
        console.error('加载字段失败', e);
      });
    },

    loadOrmFields: function() {
      if (!this.selectedModule) return;
      this.loadFields();
    },

    loadTemplateFields: function() {
      if (!this.selectedTemplate) return;
      this.loadFields();
    },

    loadModuleOptions: function() {
      var self = this;
      var apiUrl = db.getUrl('url');
      var token = self.$store.state['user'].access_token;
      httpGet(apiUrl + '/api/word-template/modules', token).then(function(result) {
        if (result.modules) {
          self.moduleOptions = result.modules;
        }
      }).catch(function(e) {
        console.error('加载模块列表失败', e);
      });
    },

    loadTemplateOptions: function() {
      var self = this;
      var apiUrl = db.getUrl('url');
      var token = self.$store.state['user'].access_token;
      httpGet(apiUrl + '/api/word-template/templates', token).then(function(result) {
        if (result.templates) {
          self.templateOptions = result.templates;
        }
      }).catch(function(e) {
        console.error('加载模版列表失败', e);
      });
    },

    insertField: function(field) {
      if (!this.docEditor) {
        this.$error('编辑器未就绪');
        return;
      }
      var self = this;

      // 表格字段：跳转到表格绑定流程
      if (field.type === 'table') {
        self.bindTableField(field);
        return;
      }

      var apiUrl = db.getUrl('url');
      var token = self.$store.state['user'].access_token;

      // html 类型：Tag 带 _HTML 后缀，替换引擎才能识别为富文本并用 HTML 渲染
      var insertKey = field.key;
      if (field.type === 'html' && !insertKey.toUpperCase().endsWith('_HTML')) {
        insertKey = insertKey + '_HTML';
      }

      httpPost(apiUrl + '/api/word-template/field-queue', {
        docKey: self.tempKey,
        field: { key: insertKey, label: field.label, type: field.type }
      }, token).then(function() {
        self.$alert('已插入字段: ' + field.label);
      }).catch(function(e) {
        console.log('插入字段失败:', e.message);
        self.$alert('请手动在文档中插入占位符: {{' + field.key + '}}');
      });
    },

    /**
     * 将子表字段绑定到 Word 中选中的表格行（循环区域）
     * 流程：
     *   1. 用户在 Word 中选中表格的"数据行"（要被循环克隆的模板行，不含表头）
     *   2. 点击字段面板中的子表字段 → 用 Block 内容控件包裹选中行，Tag=子表key，Alias=子表名
     *   3. 展开子字段后，用户把各字段插入到表格各列单元格 → 完成列绑定
     * 替换引擎遍历时按 Tag 找到循环区域，按子表数据克隆行，每行按字段 Tag 替换单元格内容
     */
    bindTableField: function(field) {
      if (!this.docEditor) {
        this.$error('编辑器未就绪');
        return;
      }
      var self = this;
      var apiUrl = db.getUrl('url');
      var token = self.$store.state['user'].access_token;
      var tag = field.key;
      // Alias 用 sourceName（如 VBS_ARD_4TPL），不显示"绑定表格"等自定义字样
      var alias = field.sourceName || field.label || field.key;

      var tipMsg = '将文档中当前光标所在的表格行标记为「' + alias + '」的数据循环模板行。\n\n' +
                   '操作步骤：\n' +
                   '1. 点击表格中要循环的数据行（不要选中表头/标题行）\n' +
                   '2. 光标定位到该行的任意单元格即可，不要框选多行\n' +
                   '3. 点击下方"确定"完成标记\n\n' +
                   '注意：光标在哪一行，就会标记哪一行为循环模板行。';

      this.$confirm(tipMsg).then(function() {
        httpPost(apiUrl + '/api/word-template/field-queue', {
          docKey: self.tempKey,
          field: { key: tag, label: alias, type: 'table' }
        }, token).then(function() {
          self.$alert('已标记循环行: ' + alias + '\n\n下一步：把展开的子字段依次插入到表格各列单元格中，完成列绑定。');
          self.$set(self.tableExpanded, field.key, true);
        }).catch(function(e) {
          console.log('表格绑定失败:', e.message);
          self.$error('表格绑定失败: ' + e.message);
        });
      }).catch(function() {});
    },

    addManualField: function() {
      if (!this.manualField.key) {
        this.$error('请输入字段标识');
        return;
      }

      var manualGroup = this.fieldGroups.find(function(g) { return g.name === '手动添加'; });
      if (!manualGroup) {
        manualGroup = { name: '手动添加', source: 'manual', fields: [] };
        this.fieldGroups.push(manualGroup);
      }

      var exists = this.manualFields.some(function(f) { return f.key === this.manualField.key; }.bind(this));
      if (exists) {
        this.$error('字段已存在');
        return;
      }

      var newField = {
        key: this.manualField.key,
        label: this.manualField.label || this.manualField.key,
        type: this.manualField.type,
        source: 'manual'
      };

      this.manualFields.push(newField);
      manualGroup.fields.push(newField);

      this.manualField = { key: '', label: '', type: 'text' };
      this.$alert('字段已添加');
    },

    /**
     * 上传新模版文件替换当前文件
     */
    triggerUpload: function() {
      this.$refs.fileInput.value = '';
      this.$refs.fileInput.click();
    },

    handleFileUpload: function(e) {
      var self = this;
      var file = e.target.files && e.target.files[0];
      if (!file) return;

      var busy = self.$busy('正在上传...');
      var apiUrl = db.getUrl('url');
      var token = self.$store.state['user'].access_token;

      var formData = new FormData();
      formData.append('file', file);
      formData.append('uploadType', 'template');
      formData.append('chunks', '1');
      formData.append('chunk', '0');

      var xhr = new XMLHttpRequest();
      xhr.open('POST', apiUrl + '/api/file');
      xhr.setRequestHeader('Authorization', 'Bearer ' + token);
      xhr.onload = function() {
        self.$free(busy);
        if (xhr.status === 200) {
          try {
            var result = JSON.parse(xhr.responseText);
            var newFileId = result.id;
            if (newFileId) {
              self.fileId = newFileId;
              self.fileName = file.name;
              self.$emit('upload', { fileId: newFileId, fileName: file.name });
              self.destroyEditor();
              self.initEditor();
              self.$alert('模版已更新');
            }
          } catch (ex) {
            self.$error('上传解析失败');
          }
        } else {
          self.$error('上传失败: ' + xhr.status);
        }
      };
      xhr.onerror = function() {
        self.$free(busy);
        self.$error('上传失败');
      };
      xhr.send(formData);
    },

    /**
     * 导出/下载当前模版文件
     */
    exportFile: function() {
      if (!this.fileId) {
        this.$error('未指定模版文件');
        return;
      }
      var apiUrl = db.getUrl('url');
      var token = this.$store.state['user'].access_token;
      var fileName = this.fileName || 'template.docx';

      var xhr = new XMLHttpRequest();
      xhr.open('GET', apiUrl + '/api/file/' + this.fileId);
      xhr.setRequestHeader('Authorization', 'Bearer ' + token);
      xhr.responseType = 'blob';
      xhr.onload = function() {
        if (xhr.status === 200) {
          var blob = xhr.response;
          var url = URL.createObjectURL(blob);
          var a = document.createElement('a');
          a.href = url;
          a.download = fileName;
          document.body.appendChild(a);
          a.click();
          document.body.removeChild(a);
          URL.revokeObjectURL(url);
        } else {
          this.$error('导出失败');
        }
      }.bind(this);
      xhr.onerror = function() {
        this.$error('导出失败');
      }.bind(this);
      xhr.send();
    },

    previewTemplate: function() {
      var self = this;
      if (!self.docEditor) {
        self.$error('编辑器未就绪');
        return;
      }
      if (!self.tempKey) {
        self.$error('请先打开模版文件');
        return;
      }

      self.$confirm('将用模拟数据填充模版并预览替换效果。\n\n当前编辑内容会先保存。确定预览吗？').then(function() {
        self.statusText = '正在生成预览...';
        var apiUrl = db.getUrl('url');
        var token = self.$store.state['user'].access_token;

        // 1. 先 force-save 保存当前编辑内容
        httpPost(apiUrl + '/api/word-template/force-save', { key: self.tempKey }, token).then(function() {
          // 等待回调保存完成
          setTimeout(function() {
            // 2. 调用预览 API
            httpPost(apiUrl + '/api/word-template/preview', { key: self.tempKey }, token).then(function(result) {
              self.statusText = '';
              if (result.success && result.fileId) {
                // 3. 在弹窗中打开预览
                self.previewFileId = result.fileId;
                self.$nextTick(function() {
                  self.$refs.previewModal.show();
                });
              } else {
                self.$error(result.Message || '预览生成失败');
              }
            }).catch(function(e) {
              self.statusText = '';
              self.$error('预览失败: ' + (e.message || '网络错误'));
            });
          }, 1500);
        }).catch(function(e) {
          self.statusText = '';
          self.$error('保存失败: ' + (e.message || '网络错误'));
        });
      }).catch(function() {});
    },

    closePreview: function() {
      this.previewFileId = '';
      this.$refs.previewModal.close();
    },

    saveAndClose: function() {
      var self = this;
      if (!self.docEditor) {
        self.$error('编辑器未就绪');
        return;
      }

      var busy = self.$busy('正在保存...');
      self.statusText = '正在保存...';

      // 先保存字段来源配置到数据库
      self.savePrefs();

      var apiUrl = db.getUrl('url');
      var token = self.$store.state['user'].access_token;
      var maxRetries = 3;
      var retryDelay = 1500;

      httpPost(apiUrl + '/api/word-template/force-save', { key: self.tempKey }, token)
        .then(function() {
          self.statusText = '等待服务器保存...';
          function trySave(retryCount) {
            httpPost(apiUrl + '/api/word-template/save', {
              key: self.tempKey,
              fileId: self.fileId
            }, token).then(function(result) {
              self.$emit('save', result);
              self.destroyEditor();
              self.$free(busy);
              self.$alert('保存成功');
              self.close();
            }).catch(function(e) {
              if (retryCount < maxRetries) {
                self.statusText = '正在保存...(' + (retryCount + 1) + ')';
                setTimeout(function() { trySave(retryCount + 1); }, retryDelay);
              } else {
                self.$free(busy);
                self.$error('保存失败: ' + e.message);
              }
            });
          }
          // force-save 后短暂等待回调，save 端点已兼容未回调的情况
          setTimeout(function() { trySave(0); }, 500);
        })
        .catch(function(e) {
          self.$free(busy);
          self.$error('强制保存失败: ' + e.message);
        });
    },

    toggleGroup: function(name) {
      this.$set(this.collapsedGroups, name, !this.collapsedGroups[name]);
    },

    toggleTableExpand: function(key) {
      this.$set(this.tableExpanded, key, !this.tableExpanded[key]);
    },

    toggleDateExpand: function(key) {
      this.$set(this.dateExpanded, key, !this.dateExpanded[key]);
    },

    // 插入日期的年/月/日后缀字段（Tag = 字段名_YY/_MM/_DD）
    insertDateSuffix: function(field, suffix, suffixLabel) {
      if (!this.docEditor) {
        this.$error('编辑器未就绪');
        return;
      }
      var self = this;
      var apiUrl = db.getUrl('url');
      var token = self.$store.state['user'].access_token;
      var suffixKey = field.key + suffix;
      httpPost(apiUrl + '/api/word-template/field-queue', {
        docKey: self.tempKey,
        field: { key: suffixKey, label: (field.label || field.key) + '-' + suffixLabel, type: 'date' }
      }, token).then(function() {
        self.$alert('已插入: ' + suffixKey);
      }).catch(function(e) {
        console.log('插入失败:', e.message);
        self.$error('插入失败: ' + e.message);
      });
    },

    getTypeIcon: function(type) {
      var icons = {
        text: 'T',
        date: 'D',
        image: 'I',
        html: 'H',
        table: 'Tb',
        suffix: 'S'
      };
      return icons[type] || 'T';
    },

    getTypeLabel: function(type) {
      var t = type || 'text';
      var found = this.fieldTypeOptions.find(function(o) { return o.key === t; });
      return found ? found.title : t;
    },

    /**
     * 切换字段类型菜单显隐
     * @param field 字段对象
     * @param parentKey 子字段的父表格 key（可选）
     */
    toggleTypeMenu: function(field, parentKey) {
      var menuKey = parentKey ? (parentKey + '_' + field.key) : field.key;
      if (this.activeTypeFieldKey === menuKey) {
        this.activeTypeFieldKey = '';
      } else {
        this.activeTypeFieldKey = menuKey;
      }
    },

    /**
     * 修改字段类型（用 $set 保证响应式更新）
     */
    changeFieldType: function(field, newType) {
      this.$set(field, 'type', newType);
      this.activeTypeFieldKey = '';
      this.$message && this.$message({ type: 'info', text: field.key + ' 类型已改为 ' + this.getTypeLabel(newType) });
    },

    /**
     * 全局点击：关闭类型菜单
     */
    onGlobalClickCloseTypeMenu: function() {
      if (this.activeTypeFieldKey) {
        this.activeTypeFieldKey = '';
      }
    },

    /**
     * OnlyOffice 内容控件被选中时，高亮左侧对应字段
     * 通过插件 postMessage + 后端轮询双通道获取选中 Tag
     */
    onContentControlSelect: function(event) {
      try {
        var tag = '';
        if (event && event.data) {
          tag = event.data.Tag || event.data.tag || event.data.TagVal || '';
        }
        if (!tag && event && typeof event === 'object') {
          tag = event.Tag || event.tag || '';
        }
        this.highlightByTag(tag);
      } catch (e) {
        console.log('[highlight] error:', e);
      }
    },

    /**
     * 建立内容控件选中监听：
     * 1. 监听插件 postMessage（直接通道，延迟最低）
     * 2. 轮询后端 /current-selection（兜底通道，postMessage 被拦截时仍可用）
     */
    setupContentControlSync: function() {
      var self = this;
      if (self._ccSyncSetup) return;
      self._ccSyncSetup = true;
      self._lastCCTag = '';

      // 通道 1：监听插件的 postMessage
      self._ccMessageHandler = function(event) {
        try {
          if (event.data && event.data.type === 'onlyoffice-cc-selection') {
            var tag = event.data.tag || '';
            if (tag !== self._lastCCTag) {
              self._lastCCTag = tag;
              if (tag) {
                self.highlightByTag(tag);
              } else {
                self.activeHighlightKey = '';
              }
            }
          }
        } catch(e) {}
      };
      window.addEventListener('message', self._ccMessageHandler);

      // 通道 2：轮询后端兜底
      var apiUrl = db.getUrl('url');
      var token = self.$store.state['user'].access_token;
      self._ccPollTimer = setInterval(function() {
        try {
          httpGet(apiUrl + '/api/word-template/current-selection?key=default', token)
            .then(function(result) {
              var tag = (result && result.tag) || '';
              if (tag !== self._lastCCTag) {
                self._lastCCTag = tag;
                if (tag) {
                  self.highlightByTag(tag);
                } else {
                  self.activeHighlightKey = '';
                }
              }
            }).catch(function() {});
        } catch(e) {}
      }, 700);
    },

    highlightByTag: function(tag) {
      if (!tag) {
        this.activeHighlightKey = '';
        return;
      }

      // 去掉后缀匹配基础字段名
      var baseKey = tag;
      var suffixes = ['_TABLE', '_IMG2', '_IMG', '_HTML', '_YY', '_MM', '_DD'];
      for (var i = 0; i < suffixes.length; i++) {
        if (baseKey.endsWith(suffixes[i])) {
          baseKey = baseKey.substring(0, baseKey.length - suffixes[i].length);
          break;
        }
      }

      var matched = this.findFieldKey(tag) || this.findFieldKey(baseKey);
      if (matched) {
        this.activeHighlightKey = matched;
        this.scrollToField(matched);
      } else {
        this.activeHighlightKey = '';
      }
    },

    /**
     * 在所有分组中查找字段 key（含子表子字段）
     */
    findFieldKey: function(tag) {
      if (!tag) return '';
      var upperTag = tag.toUpperCase();
      for (var i = 0; i < this.fieldGroups.length; i++) {
        var fields = this.fieldGroups[i].fields || [];
        for (var j = 0; j < fields.length; j++) {
          if ((fields[j].key || '').toUpperCase() === upperTag) {
            return fields[j].key;
          }
          var children = fields[j].children || [];
          for (var k = 0; k < children.length; k++) {
            if ((children[k].key || '').toUpperCase() === upperTag) {
              return children[k].key;
            }
          }
        }
      }
      return '';
    },

    isFieldHighlighted: function(key) {
      return this.activeHighlightKey === key;
    },

    /**
     * 滚动到高亮字段
     */
    scrollToField: function(key) {
      this.$nextTick(function() {
        var el = document.querySelector('.field-highlighted');
        if (el) {
          el.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }
      });
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
      // 清理内容控件监听
      if (this._ccPollTimer) {
        clearInterval(this._ccPollTimer);
        this._ccPollTimer = null;
      }
      if (this._ccMessageHandler) {
        window.removeEventListener('message', this._ccMessageHandler);
        this._ccMessageHandler = null;
      }
      this._ccSyncSetup = false;
      this._lastCCTag = '';
      this.activeHighlightKey = '';

      // 主动清理后端选中状态和字段队列，防止内存泄漏
      if (this.tempKey) {
        var apiUrl = db.getUrl('url');
        var token = this.$store.state['user'].access_token;
        httpGet(apiUrl + '/api/word-template/current-selection?key=' + encodeURIComponent(this.tempKey), token).catch(function() {});
        try {
          var xhr = new XMLHttpRequest();
          xhr.open('DELETE', apiUrl + '/api/word-template/current-selection?key=' + encodeURIComponent(this.tempKey));
          if (token) xhr.setRequestHeader('Authorization', 'Bearer ' + token);
          xhr.send();
        } catch (e) {}
      }

      if (this.docEditor) {
        try {
          this.docEditor.destroyEditor();
        } catch (e) { }
        this.docEditor = null;
      }
    }
  }
};
</script>
<style lang="less" scoped>
.word-template-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 16px;
  border-bottom: 1px solid #e8e8e8;
  background: #fff;
  position: relative;
  z-index: 20;
}
.word-template-title {
  font-size: 16px;
  font-weight: 500;
  color: #333;
}
.word-template-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}
.word-template-wrapper {
  display: flex;
  width: 100%;
  height: calc(100vh - 52px);
  position: relative;
  overflow: hidden;
}
.word-template-overlay {
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
  p { margin-top: 10px; }
}

/* 左侧字段面板 */
.word-template-fields-panel {
  width: 260px;
  min-width: 260px;
  border-right: 1px solid #e8e8e8;
  display: flex;
  flex-direction: column;
  background: #fafafa;
}
.fields-panel-header {
  padding: 8px 12px;
  border-bottom: 1px solid #eee;
}
.fields-search-input {
  width: 100%;
  padding: 4px 8px;
  border: 1px solid #ddd;
  border-radius: 3px;
  font-size: 13px;
  outline: none;
  &:focus {
    border-color: #1890ff;
  }
}
.fields-source-section {
  padding: 8px 12px;
  border-bottom: 1px solid #eee;
}
.fields-source-title {
  font-size: 12px;
  color: #999;
  margin-bottom: 6px;
}
.fields-source-item {
  margin-bottom: 6px;
  label {
    display: block;
    font-size: 12px;
    color: #666;
    margin-bottom: 2px;
  }
}
.fields-list {
  flex: 1;
  overflow-y: auto;
  padding: 4px 0;
}
.fields-group-title {
  padding: 6px 12px;
  font-size: 13px;
  font-weight: 500;
  color: #333;
  background: #f0f0f0;
  cursor: pointer;
  user-select: none;
  display: flex;
  align-items: center;
  &:hover {
    background: #e8e8e8;
  }
}
.fields-count {
  margin-left: auto;
  font-size: 11px;
  color: #999;
  font-weight: normal;
}
.fields-group-body {
  padding: 2px 0;
}
.field-item {
  display: flex;
  align-items: center;
  padding: 5px 12px;
  cursor: pointer;
  font-size: 12px;
  position: relative;
  &:hover {
    background: #e6f7ff;
  }
}
.field-item-child {
  padding-left: 28px;
  color: #666;
}
.field-item-table {
  cursor: pointer;
  font-weight: bold;
  &:hover {
    background: #e6f7ff;
  }
}
.field-bind-btn {
  margin-left: auto;
  margin-right: 4px;
  padding: 1px 6px;
  font-size: 10px;
  font-weight: normal;
  color: #fff;
  background: #eb2f96;
  border-radius: 2px;
  cursor: pointer;
  white-space: nowrap;
  display: none;
  .field-item-table:hover & {
    display: inline-block;
  }
  &:hover {
    background: #d4237c;
  }
}
.field-col-bind-tip {
  padding: 4px 12px 4px 32px;
  font-size: 11px;
  color: #999;
  font-style: italic;
  background: #fafafa;
  border-bottom: 1px dashed #eee;
}
.field-item-child {
  padding-left: 28px;
  font-size: 11px;
  color: #666;
}
.field-toggle-btn {
  margin-left: auto;
  cursor: pointer;
  padding: 2px 6px;
  &:hover {
    color: #1890ff;
  }
}
.field-type-icon {
  display: inline-block;
  width: 20px;
  height: 20px;
  line-height: 20px;
  text-align: center;
  border-radius: 3px;
  font-size: 10px;
  font-weight: 600;
  color: #fff;
  margin-right: 6px;
  flex-shrink: 0;
  &.type-text { background: #1890ff; }
  &.type-date { background: #52c41a; }
  &.type-image { background: #faad14; color: #333; }
  &.type-html { background: #722ed1; }
  &.type-table { background: #eb2f96; }
  &.type-suffix { background: #999; }
}
/* 可编辑类型图标 */
.type-icon-editable {
  position: relative;
  cursor: pointer;
  &:hover {
    opacity: 0.85;
    box-shadow: 0 0 0 2px rgba(24,144,255,0.3);
  }
  .type-edit-caret {
    position: absolute;
    right: 1px;
    bottom: 1px;
    width: 0;
    height: 0;
    border-left: 3px solid transparent;
    border-right: 3px solid transparent;
    border-top: 3px solid rgba(255,255,255,0.9);
  }
}
/* 类型选择浮层 */
.field-type-menu {
  position: absolute;
  left: 32px;
  top: 100%;
  z-index: 1000;
  background: #fff;
  border: 1px solid #e8e8e8;
  border-radius: 4px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.15);
  min-width: 120px;
  padding: 4px 0;
}
.field-type-menu-item {
  display: flex;
  align-items: center;
  padding: 6px 10px;
  font-size: 12px;
  color: #333;
  cursor: pointer;
  gap: 8px;
  &:hover {
    background: #f0f8ff;
  }
  &.active {
    color: #1890ff;
    background: #e6f7ff;
    font-weight: 500;
  }
}
.field-key {
  color: #666;
  font-family: monospace;
  max-width: 80px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.field-label {
  color: #333;
  margin-left: 4px;
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.field-insert-btn {
  color: #1890ff;
  font-size: 14px;
  padding: 2px 4px;
  border-radius: 2px;
  cursor: pointer;
  margin-left: auto;
  flex-shrink: 0;
  &:hover {
    background: #1890ff;
    color: #fff;
  }
}
.field-highlighted {
  background: #fff3cd !important;
  border-left: 3px solid #ffc107;
  padding-left: 9px !important;
  animation: fieldHighlightPulse 1s ease-in-out;
}
@keyframes fieldHighlightPulse {
  0%, 100% { background: #fff3cd; }
  50% { background: #ffe69c; }
}
.manual-field-form {
  padding: 8px 12px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.manual-input {
  width: 100%;
  padding: 4px 8px;
  border: 1px solid #ddd;
  border-radius: 3px;
  font-size: 12px;
  outline: none;
}

/* 中间编辑器 */
.word-template-editor-area {
  flex: 1;
  position: relative;
  overflow: hidden;
}
.word-template-editor-container {
  width: 100%;
  height: 100%;
  overflow: hidden;
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
  height: calc(100vh - 52px);
  overflow: hidden;
}
</style>

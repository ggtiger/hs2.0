<template>
  <view-dialog :title="title" :loading="loading">
    <template slot="body">
      <ToolBar label="基本信息" :size="16"></ToolBar>
      <rs-form-edit
        ref="form"
        class="maxModalH rs-flex-col"
        :label-width="100"
        mode="twocolumn"
        :path="$MAIN"
      >
        <template slot="FILEID">
          <div class="upload-row">
            <rs-uploader-template v-model="TEMPLATE_FILE" :options="uploadOptions" @select="onTemplateSelect"></rs-uploader-template>
            <Button
              v-if="TEMPLATE_FILE && TEMPLATE_FILE.id"
              size="s"
              color="primary"
              @click.native="previewTemplate"
              style="margin-left:8px"
            >预览模版</Button>
          </div>
        </template>
      </rs-form-edit>

      <rs-word-template-editor ref="previewEditor" business-type="template"></rs-word-template-editor>

      <!-- 模版已有字段 -->
      <ToolBar label="模版字段" :size="16" v-if="parsedFields.length > 0"></ToolBar>
      <div class="fields-preview" v-if="parsedFields.length > 0">
        <table class="fields-table">
          <thead>
            <tr>
              <th>字段标识</th>
              <th>类型</th>
              <th>来源</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="f in parsedFields" :key="f.key">
              <td><code>{{ f.key }}</code></td>
              <td><span :class="'tag tag-' + f.type">{{ f.type }}</span></td>
              <td>{{ f.source || '-' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'RS_M12/A04'" v-if="ID" @confirm="del">
        <Button class="ml5" color="red">删除</Button>
      </Poptip>
      <Button class="ml5" v-per="'RS_M12/A03'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapDateTable } from '../store';
import Add01 from '@/mixins/add01';
// eslint-disable-next-line no-restricted-imports
import db from '@/api/db';

export default {
  name: 's01-m12-add',
  mixins: [Add01],
  props: {
    storeName: String,
    title: String,
    ID: String,
  },
  data() {
    return {
      uploadOptions: {
        max_file_size: '10mb',
      },
      parsedFields: [],
    };
  },
  computed: {
    ...mapDateTable('MAIN', ['FILEID', 'FILENAME', 'TEMPLATETYPE']),
    TEMPLATE_FILE: {
      get() {
        if (this.FILEID) return { id: this.FILEID, name: this.FILENAME };
        return null;
      },
      set({ id, name }) {
        this.FILEID = id;
        this.FILENAME = name;
        if (id) {
          this.parseFields(id);
        }
      },
    },
  },
  watch: {
    FILEID: {
      handler(val) {
        if (val && this.parsedFields.length === 0) {
          this.parseFields(val);
        }
      },
      immediate: true,
    },
  },
  methods: {
    closeW() {
      this.$parent.setvalue(false);
    },
    onTemplateSelect(template) {
      // 选入模版后，同步关联字段
      if (template.MODULECODE) this.MODULECODE = template.MODULECODE;
      if (template.TEMPLATEID) this.TEMPLATEID = template.TEMPLATEID;
    },
    parseFields(fileId) {
      var self = this;
      var apiUrl = db.getUrl('url');
      var token = self.$store.state['user'].access_token;

      var xhr = new XMLHttpRequest();
      xhr.open('GET', apiUrl + '/api/word-template/parse-fields/' + fileId);
      xhr.setRequestHeader('Authorization', 'Bearer ' + token);
      xhr.onload = function() {
        if (xhr.status === 200) {
          try {
            var result = JSON.parse(xhr.responseText);
            var fields = [];
            if (result.contentControls) {
              result.contentControls.forEach(function(f) {
                fields.push({ key: f.Key, type: f.BaseType, source: 'Content Control' });
              });
            }
            if (result.bookmarks) {
              result.bookmarks.forEach(function(f) {
                if (!fields.some(function(existing) { return existing.key === f.Key })) {
                  fields.push({ key: f.Key, type: f.BaseType, source: 'Bookmark' });
                }
              });
            }
            self.parsedFields = fields;
          } catch (e) {
            // 解析失败不影响使用
          }
        }
      };
      xhr.send();
    },
    previewTemplate() {
      if (!this.FILEID) return;
      this.$refs.previewEditor.open(this.FILEID);
    },
  },
  components: {},
};
</script>
<style lang="less" scoped>
.upload-row {
  display: flex;
  align-items: center;
}
.fields-preview {
  padding: 0 10px 10px 10px;
}
.fields-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
  th, td {
    padding: 6px 10px;
    border: 1px solid #e8e8e8;
    text-align: left;
  }
  th {
    background: #f5f5f5;
    font-weight: 500;
  }
  code {
    background: #f0f0f0;
    padding: 1px 4px;
    border-radius: 2px;
    font-family: Consolas, monospace;
    font-size: 12px;
  }
}
.tag {
  display: inline-block;
  padding: 1px 6px;
  border-radius: 3px;
  font-size: 11px;
  color: #fff;
  &.tag-text { background: #1890ff; }
  &.tag-image { background: #faad14; color: #333; }
  &.tag-html { background: #722ed1; }
  &.tag-table { background: #eb2f96; }
  &.tag-date { background: #52c41a; }
}
</style>

<template>
  <div class="prompts-part">
    <!-- 左侧: 列表 -->
    <div class="pp-left">
      <div class="pp-filter">
        <input class="pp-search" v-model="keyword" placeholder="搜索 PROMPTKEY / 描述" />
        <Button size="s" color="primary" @click="addNew" style="width:100%">+ 新增提示词</Button>
      </div>
      <div class="pp-list" v-loading="loading">
        <div
          v-for="it in filteredList"
          :key="it.ID"
          :class="['pp-item', { active: currentId === it.ID }]"
          @click="editRow(it)"
        >
          <div class="pp-item-key">{{ it.PROMPTKEY }}<span class="pp-item-ver" v-if="it.VERSION && it.VERSION !== 'v1'">{{ it.VERSION }}</span></div>
          <div class="pp-item-desc">{{ it.DESCRIPTION || '(无描述)' }}</div>
        </div>
        <div v-if="!loading && filteredList.length === 0" class="pp-empty">无匹配提示词</div>
      </div>
    </div>

    <!-- 右侧: 编辑区 -->
    <div class="pp-right" v-if="editing">
      <div class="pp-form-row">
        <div class="pp-field">
          <label>PROMPTKEY</label>
          <input v-model="form.PROMPTKEY" :disabled="!isNew" placeholder="如 system_general / tool:query_data" />
        </div>
        <div class="pp-field">
          <label>描述</label>
          <input v-model="form.DESCRIPTION" placeholder="这个提示词用在哪里" />
        </div>
        <div class="pp-field">
          <label>版本</label>
          <input v-model="form.VERSION" placeholder="v1" style="width:80px" />
        </div>
        <div class="pp-field">
          <label>权重</label>
          <NumberInput v-model="form.WEIGHT" :min="0" :max="1000" style="width:100px" />
          <span class="pp-weight-hint">同KEY多版本按权重分配流量</span>
        </div>
        <div class="pp-actions">
          <Button size="s" color="primary" @click="save" :loading="saving">保存</Button>
          <Poptip content="确定删除该提示词？" @confirm="del" v-if="!isNew">
            <Button size="s" color="red">删除</Button>
          </Poptip>
          <Button size="s" @click="cancelEdit">关闭</Button>
        </div>
      </div>
      <MdEditor v-model="form.CONTENT" title="提示词内容（支持 Markdown，变量用 {moduleCode} 形式）" class="pp-editor" />
      <!-- 占位符提示 -->
      <div class="pp-placeholders" v-if="placeholders.length > 0">
        <div class="pp-ph-title">可用占位符</div>
        <div v-for="ph in placeholders" :key="ph.name" class="pp-ph-item" @click="insertPlaceholder(ph.name)" :title="ph.desc">
          <code>{{ '{' + ph.name + '}' }}</code>
          <span>{{ ph.desc }}</span>
        </div>
      </div>
    </div>

    <div class="pp-right pp-placeholder" v-else>
      <div class="pp-ph-inner">
        <p>← 从左侧选择提示词进行编辑</p>
        <p class="pp-tip">提示词按 PROMPTKEY 被各 AI 场景引用：system_xxx 是系统提示词，tool:xxx 是工具描述</p>
      </div>
    </div>
  </div>
</template>

<script>
import { getGenericStore } from '@/components/generic-module/generic-store';
import MdEditor from '../../components/md-editor.vue';
import { PROMPT_PLACEHOLDERS } from '@/constants';

const MC = 'RS_M16';

export default {
  name: 'PromptsPart',
  components: { MdEditor },
  data() {
    return {
      storeName: MC,
      loading: false,
      saving: false,
      keyword: '',
      list: [],
      editing: false,
      isNew: false,
      currentId: '',
      form: { ID: '', PROMPTKEY: '', VERSION: 'v1', WEIGHT: 100, DESCRIPTION: '', CONTENT: '' }
    };
  },
  computed: {
    filteredList() {
      var kw = (this.keyword || '').trim().toLowerCase();
      if (!kw) return this.list;
      return this.list.filter(it =>
        (it.PROMPTKEY || '').toLowerCase().indexOf(kw) >= 0 ||
        (it.DESCRIPTION || '').toLowerCase().indexOf(kw) >= 0
      );
    },
    placeholders() {
      var key = (this.form.PROMPTKEY || '').trim();
      if (!key) return [];
      // 精确匹配
      if (PROMPT_PLACEHOLDERS[key]) return PROMPT_PLACEHOLDERS[key];
      // 前缀匹配: tool:xxx → tool 前缀
      var prefix = key.split(':')[0];
      if (PROMPT_PLACEHOLDERS[prefix]) return PROMPT_PLACEHOLDERS[prefix];
      return [];
    }
  },
  created() {
    this.storeObj = getGenericStore(MC);
  },
  mounted() {
    this.loadList();
  },
  methods: {
    async loadList() {
      this.loading = true;
      try {
        var QQRY = this.storeObj.storeHelper.getTable('QQRY');
        if (QQRY) {
          QQRY.setValue('PageSize', 200);
          QQRY.setValue('PageIndex', 1);
        }
        await this.$callAction({ action: MC + '/query' });
        var st = this.$store.state[MC];
        this.list = (st && st.dt && st.dt.QRY && st.dt.QRY.data) || [];
        this.$emit('count', { key: 'prompts', n: this.list.length });
      } finally {
        this.loading = false;
      }
    },
    async editRow(it) {
      this.currentId = it.ID;
      this.isNew = false;
      this.editing = true;
      this.form = { ID: it.ID, PROMPTKEY: it.PROMPTKEY, VERSION: it.VERSION || 'v1', WEIGHT: it.WEIGHT || 100, DESCRIPTION: it.DESCRIPTION, CONTENT: it.CONTENT };
      await this.$callAction({ action: MC + '/open', param: { ID: it.ID } });
    },
    addNew() {
      this.currentId = '';
      this.isNew = true;
      this.editing = true;
      this.form = { ID: '', PROMPTKEY: '', VERSION: 'v1', WEIGHT: 100, DESCRIPTION: '', CONTENT: '' };
      this.$store.commit(MC + '/INIT', { paths: ['MAIN'] });
      this.$store.commit(MC + '/ADD', { path: 'MAIN', item: {} });
    },
    cancelEdit() {
      this.editing = false;
      this.currentId = '';
    },
    async save() {
      if (!this.form.PROMPTKEY) { this.$Message('PROMPTKEY 不能为空'); return }
      if (!this.form.CONTENT) { this.$Message('内容不能为空'); return }
      this.saving = true;
      try {
        var MAIN = this.storeObj.storeHelper.getTable('MAIN');
        MAIN.setValue('PROMPTKEY', this.form.PROMPTKEY);
        MAIN.setValue('VERSION', this.form.VERSION);
        MAIN.setValue('WEIGHT', this.form.WEIGHT);
        MAIN.setValue('DESCRIPTION', this.form.DESCRIPTION);
        MAIN.setValue('CONTENT', this.form.CONTENT);
        await this.$callAction({
          action: MC + '/save',
          successText: '保存成功',
          successCall: () => {
            this.editing = false;
            this.loadList();
          }
        });
      } finally {
        this.saving = false;
      }
    },
    insertPlaceholder(name) {
      var tag = '{' + name + '}';
      this.form.CONTENT = (this.form.CONTENT || '') + tag;
    },
    async del() {
      this.$store.commit(MC + '/INIT', { paths: ['MAIN'] });
      this.$store.commit(MC + '/ADD', { path: 'MAIN', item: { ID: this.currentId } });
      await this.$callAction({
        action: MC + '/delete',
        successText: '删除成功',
        successCall: () => {
          this.editing = false;
          this.loadList();
        }
      });
    }
  }
};
</script>

<style lang="less" scoped>
.prompts-part {
  flex: 1;
  display: flex;
  min-height: 0;
}
.pp-left {
  width: 280px;
  border-right: 1px solid #e8e8e8;
  background: #fff;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
}
.pp-filter {
  padding: 8px;
  border-bottom: 1px solid #f0f0f0;
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.pp-search {
  border: 1px solid #d9d9d9;
  border-radius: 4px;
  padding: 5px 8px;
  font-size: 12px;
  outline: none;
  &:focus { border-color: #2F54EB; }
}
.pp-list {
  flex: 1;
  overflow-y: auto;
  padding: 4px;
}
.pp-item {
  padding: 8px 10px;
  border-radius: 4px;
  cursor: pointer;
  margin-bottom: 2px;
  border: 1px solid transparent;
  &:hover { background: #f5f7fa; }
  &.active { background: #e6f7ff; border-color: #91d5ff; }
}
.pp-item-key {
  font-size: 13px;
  font-weight: 600;
  color: #333;
  font-family: Consolas, monospace;
}
.pp-item-ver {
  font-size: 10px;
  font-weight: 400;
  color: #2F54EB;
  background: #f0f5ff;
  padding: 0 4px;
  border-radius: 2px;
  margin-left: 4px;
  font-family: inherit;
}
.pp-item-desc {
  font-size: 11px;
  color: #999;
  margin-top: 2px;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
.pp-empty { text-align: center; color: #bbb; padding: 30px 0; }
.pp-right {
  flex: 1;
  min-width: 0;
  padding: 12px 16px;
  background: #fff;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.pp-editor {
  flex: 1;
  min-height: 0;
}
.pp-form-row {
  display: flex;
  gap: 10px;
  align-items: center;
  margin-bottom: 10px;
}
.pp-field {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 6px;
  label { font-size: 12px; color: #666; white-space: nowrap; }
  input {
    flex: 1;
    border: 1px solid #d9d9d9;
    border-radius: 4px;
    padding: 6px 8px;
    font-size: 13px;
    outline: none;
    min-width: 0;
    &:focus { border-color: #2F54EB; }
    &:disabled { background: #f5f5f5; color: #999; }
  }
}
.pp-actions { display: flex; gap: 6px; }
.pp-weight-hint { font-size: 11px; color: #999; white-space: nowrap; }
.pp-placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
}
.pp-ph-inner {
  text-align: center;
  color: #999;
  .pp-tip { font-size: 12px; color: #bbb; margin-top: 8px; }
}
.pp-placeholders {
  border-top: 1px solid #f0f0f0;
  padding: 8px 0 0;
  margin-top: 8px;
}
.pp-ph-title {
  font-size: 12px;
  font-weight: 600;
  color: #666;
  margin-bottom: 6px;
}
.pp-ph-item {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 3px 8px;
  border: 1px solid #d9d9d9;
  border-radius: 3px;
  margin: 0 4px 4px 0;
  cursor: pointer;
  font-size: 12px;
  &:hover { border-color: #2F54EB; background: #f0f5ff; }
  code { font-family: Consolas, monospace; color: #2F54EB; font-size: 11px; }
  span { color: #999; }
}
</style>

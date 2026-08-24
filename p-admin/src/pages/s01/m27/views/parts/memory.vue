<template>
  <div class="memory-part">
    <!-- 左侧: 筛选 + 列表 -->
    <div class="mp-left">
      <div class="mp-filter">
        <Select v-model="filterType" :datas="typeOptions" placeholder="全部类型" @change="loadList" />
        <input class="mp-search" v-model="keyword" placeholder="搜索标题/内容/标签" @input="onSearchInput" />
      </div>
      <div class="mp-list" v-loading="loading">
        <div
          v-for="it in filteredList"
          :key="it.ID"
          :class="['mp-item', { active: currentId === it.ID }]"
          @click="editRow(it)"
        >
          <div class="mp-item-head">
            <span :class="['mp-badge', 't-' + it.MEMORYTYPE]">{{ typeLabel(it.MEMORYTYPE) }}</span>
            <span class="mp-priority" v-if="it.PRIORITY >= 9">P{{ it.PRIORITY }}</span>
          </div>
          <div class="mp-item-title">{{ it.TITLE }}</div>
          <div class="mp-item-meta">
            <span>{{ it.ASSETTYPE }}</span>
            <span v-if="it.HITCOUNT">命中 {{ it.HITCOUNT }}</span>
          </div>
        </div>
        <div v-if="!loading && filteredList.length === 0" class="mp-empty">无匹配记忆</div>
      </div>
    </div>

    <!-- 右侧: 编辑区 -->
    <div class="mp-right" v-if="editing">
      <div class="mp-form-head">
        <input class="mp-title-input" v-model="form.TITLE" placeholder="记忆标题（一句话说清规则/坑）" />
        <div class="mp-head-actions">
          <Button size="s" color="primary" @click="save" :loading="saving">保存</Button>
          <Poptip content="确定删除这条记忆？" @confirm="del" v-if="!isNew">
            <Button size="s" color="red">删除</Button>
          </Poptip>
          <Button size="s" @click="cancelEdit">关闭</Button>
        </div>
      </div>
      <div class="mp-form-row">
        <div class="mp-field">
          <label>类型</label>
          <Select v-model="form.MEMORYTYPE" :datas="typeEditOptions" />
        </div>
        <div class="mp-field">
          <label>资产维度</label>
          <Select v-model="form.ASSETTYPE" :datas="assetOptions" />
        </div>
        <div class="mp-field small">
          <label>优先级</label>
          <NumberInput v-model="form.PRIORITY" :min="0" :max="10" />
        </div>
      </div>
      <div class="mp-form-row">
        <div class="mp-field">
          <label>标签(逗号分隔)</label>
          <input v-model="form.TAGS" placeholder="如: resuipc,FIELDNAME,铁律" />
        </div>
        <div class="mp-field">
          <label>场景</label>
          <input v-model="form.SCENE_CODES" placeholder="assistant,aidev,wizard" />
        </div>
        <div class="mp-field small">
          <label>向导步骤</label>
          <input v-model="form.WIZARD_STEPS" placeholder="0,1,2,3,4,5" />
        </div>
      </div>
      <MdEditor v-model="form.CONTENT" title="记忆内容（支持 Markdown）" class="mp-editor" />
    </div>

    <!-- 空态 -->
    <div class="mp-right mp-placeholder" v-else>
      <div class="mp-ph-inner">
        <p>← 从左侧选择一条记忆进行编辑</p>
        <Button color="primary" @click="addNew">+ 新增记忆</Button>
      </div>
    </div>
  </div>
</template>

<script>
import { getGenericStore } from '@/components/generic-module/generic-store';
import MdEditor from '../../components/md-editor.vue';

const MC = 'RS_M26';

export default {
  name: 'MemoryPart',
  components: { MdEditor },
  data() {
    return {
      storeName: MC,
      loading: false,
      saving: false,
      filterType: '',
      keyword: '',
      list: [],
      editing: false,
      isNew: false,
      currentId: '',
      form: this.emptyForm()
    };
  },
  computed: {
    typeOptions() {
      return [
        { key: '', title: '全部类型' },
        { key: 'rule', title: 'rule 规则' },
        { key: 'pitfall', title: 'pitfall 坑' },
        { key: 'glossary', title: 'glossary 术语' },
        { key: 'example', title: 'example 示例' }
      ];
    },
    typeEditOptions() {
      return this.typeOptions.filter(o => o.key !== '');
    },
    assetOptions() {
      return ['wizard', 'metadata', 'sfc', 'csharp', 'sql', 'general', 'frontend'].map(k => ({ key: k, title: k }));
    },
    filteredList() {
      var kw = (this.keyword || '').trim().toLowerCase();
      if (!kw) return this.list;
      return this.list.filter(it =>
        (it.TITLE || '').toLowerCase().indexOf(kw) >= 0 ||
        (it.CONTENT || '').toLowerCase().indexOf(kw) >= 0 ||
        (it.TAGS || '').toLowerCase().indexOf(kw) >= 0
      );
    }
  },
  created() {
    this.storeObj = getGenericStore(MC);
  },
  mounted() {
    this.loadList();
  },
  methods: {
    emptyForm() {
      return {
        ID: '',
        TITLE: '',
        MEMORYTYPE: 'pitfall',
        ASSETTYPE: 'wizard',
        PRIORITY: 5,
        TAGS: '',
        SCENE_CODES: 'assistant,aidev,wizard',
        WIZARD_STEPS: '0,1,2,3,4,5',
        CONTENT: ''
      };
    },
    typeLabel(t) {
      return { rule: '规则', pitfall: '坑', glossary: '术语', example: '示例' }[t] || t;
    },
    async loadList() {
      this.loading = true;
      try {
        var QQRY = this.storeObj.storeHelper.getTable('QQRY');
        if (QQRY) {
          QQRY.setValue('PageSize', 500);
          QQRY.setValue('PageIndex', 1);
        }
        await this.$callAction({ action: MC + '/query' });
        var st = this.$store.state[MC];
        var rows = (st && st.dt && st.dt.QRY && st.dt.QRY.data) || [];
        // 前端过滤类型(F01 INPUT 是模糊搜索, 类型筛选在前端做更直观)
        this.list = rows.filter(r => (r.ISDELETED || 0) !== 1);
        if (this.filterType) {
          this.list = this.list.filter(r => r.MEMORYTYPE === this.filterType);
        }
        this.$emit('count', { key: 'memory', n: this.list.length });
      } finally {
        this.loading = false;
      }
    },
    onSearchInput() {
      // 前端即时过滤, 无需请求
    },
    async editRow(it) {
      this.currentId = it.ID;
      this.isNew = false;
      this.editing = true;
      this.form = Object.assign(this.emptyForm(), {
        ID: it.ID,
        TITLE: it.TITLE,
        MEMORYTYPE: it.MEMORYTYPE,
        ASSETTYPE: it.ASSETTYPE,
        PRIORITY: it.PRIORITY,
        TAGS: it.TAGS,
        SCENE_CODES: it.SCENE_CODES,
        WIZARD_STEPS: it.WIZARD_STEPS,
        CONTENT: it.CONTENT
      });
      // 打开完整行进 store(保存时走 <m> 更新, 铁律: 不手拼 XML)
      await this.$callAction({ action: MC + '/open', param: { ID: it.ID } });
    },
    addNew() {
      this.currentId = '';
      this.isNew = true;
      this.editing = true;
      this.form = this.emptyForm();
      // INIT+ADD 空行(Add 状态, 保存走 <a> 插入)
      this.$store.commit(MC + '/INIT', { paths: ['MAIN'] });
      this.$store.commit(MC + '/ADD', { path: 'MAIN', item: {} });
    },
    cancelEdit() {
      this.editing = false;
      this.currentId = '';
    },
    writeToStore() {
      // 表单值写回 DataTable(Store03 save 自动生成 XML)
      var MAIN = this.storeObj.storeHelper.getTable('MAIN');
      var keys = ['TITLE', 'MEMORYTYPE', 'ASSETTYPE', 'PRIORITY', 'TAGS', 'SCENE_CODES', 'WIZARD_STEPS', 'CONTENT'];
      keys.forEach(k => MAIN.setValue(k, this.form[k]));
    },
    async save() {
      if (!this.form.TITLE) { this.$Message('标题不能为空'); return }
      if (!this.form.CONTENT) { this.$Message('内容不能为空'); return }
      this.saving = true;
      try {
        this.writeToStore();
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
    async del() {
      // 物理删除: INIT+ADD 仅带 ID 的行 + dispatch delete(参照 generic-module doDelete)
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
.memory-part {
  flex: 1;
  display: flex;
  min-height: 0;
}
.mp-left {
  width: 300px;
  border-right: 1px solid #e8e8e8;
  background: #fff;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
}
.mp-filter {
  padding: 8px;
  border-bottom: 1px solid #f0f0f0;
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.mp-search {
  border: 1px solid #d9d9d9;
  border-radius: 4px;
  padding: 5px 8px;
  font-size: 12px;
  outline: none;
  &:focus { border-color: #2F54EB; }
}
.mp-list {
  flex: 1;
  overflow-y: auto;
  padding: 4px;
}
.mp-item {
  padding: 8px 10px;
  border-radius: 4px;
  cursor: pointer;
  margin-bottom: 2px;
  border: 1px solid transparent;
  &:hover { background: #f5f7fa; }
  &.active { background: #e6f7ff; border-color: #91d5ff; }
}
.mp-item-head {
  display: flex;
  gap: 6px;
  align-items: center;
  margin-bottom: 3px;
}
.mp-badge {
  font-size: 11px;
  padding: 0 6px;
  border-radius: 3px;
  &.t-rule { background: #e6f7ff; color: #2F54EB; }
  &.t-pitfall { background: #fff1f0; color: #f5222d; }
  &.t-glossary { background: #f6ffed; color: #52c41a; }
  &.t-example { background: #fff7e6; color: #fa8c16; }
}
.mp-priority {
  font-size: 11px;
  color: #f5222d;
  font-weight: 600;
}
.mp-item-title {
  font-size: 13px;
  color: #333;
  line-height: 1.4;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
.mp-item-meta {
  display: flex;
  gap: 10px;
  font-size: 11px;
  color: #999;
  margin-top: 3px;
}
.mp-empty { text-align: center; color: #bbb; padding: 30px 0; }
.mp-right {
  flex: 1;
  min-width: 0;
  padding: 12px 16px;
  background: #fff;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.mp-form-head {
  display: flex;
  gap: 10px;
  align-items: center;
  margin-bottom: 10px;
}
.mp-title-input {
  flex: 1;
  border: 1px solid #d9d9d9;
  border-radius: 4px;
  padding: 7px 10px;
  font-size: 14px;
  outline: none;
  &:focus { border-color: #2F54EB; }
}
.mp-head-actions { display: flex; gap: 6px; }
.mp-form-row {
  display: flex;
  gap: 10px;
  margin-bottom: 10px;
}
.mp-field {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 6px;
  &.small { flex: 0 0 160px; }
  label {
    font-size: 12px;
    color: #666;
    white-space: nowrap;
  }
  input {
    flex: 1;
    border: 1px solid #d9d9d9;
    border-radius: 4px;
    padding: 5px 8px;
    font-size: 12px;
    outline: none;
    min-width: 0;
    &:focus { border-color: #2F54EB; }
  }
}
.mp-editor {
  margin-top: 4px;
  flex: 1;
  min-height: 0;
}
.mp-placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
}
.mp-ph-inner {
  text-align: center;
  color: #999;
  p { margin-bottom: 14px; }
}
</style>

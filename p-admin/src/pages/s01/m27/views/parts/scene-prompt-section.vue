<template>
  <div class="scene-prompt-section sp-section">
    <div class="sps-header" @click="toggleSection">
      <i :class="sectionExpanded ? 'h-icon-down' : 'h-icon-right'"></i>
      <span class="sps-title">提示词</span>
      <span class="sps-count" v-if="promptKeys.length">{{ promptKeys.length }} 项</span>
    </div>
    <div class="sps-body" v-if="sectionExpanded">
      <!-- 无映射的场景 -->
      <div v-if="promptKeys.length === 0" class="sps-empty">
        此场景无关联提示词映射（PROMPTKEY 字段可手动填写）
      </div>
      <!-- 主 prompt -->
      <div v-if="promptMap.main" class="sps-prompt-block">
        <div class="sps-prompt-head">
          <span class="sps-prompt-key">{{ promptMap.main }}</span>
          <span class="sps-prompt-label">主提示词</span>
          <span class="sps-force-badge" v-if="isForce(promptMap.main)" title="RegisterDefaultForce: 启动时强制覆盖数据库">强制同步</span>
          <Button size="s" @click="savePrompt(promptMap.main)" :loading="savingKey === promptMap.main">保存</Button>
        </div>
        <MdEditor
          v-model="promptData[promptMap.main]"
          :title="getPromptTitle(promptMap.main)"
          class="sps-editor-main"
        />
        <!-- 占位符 -->
        <div class="sps-placeholders" v-if="getPlaceholders(promptMap.main).length">
          <span class="sps-ph-label">占位符:</span>
          <span
            v-for="ph in getPlaceholders(promptMap.main)"
            :key="ph.name"
            class="sps-ph-tag"
            @click="insertPlaceholder(promptMap.main, ph.name)"
            :title="ph.desc"
          >
            <code>{{ '{' + ph.name + '}' }}</code> {{ ph.desc }}
          </span>
        </div>
      </div>
      <!-- 子 prompt (可折叠) -->
      <div v-for="subKey in promptMap.sub" :key="subKey" class="sps-prompt-block">
        <div class="sps-prompt-head" @click="toggleSub(subKey)">
          <i :class="expandedSubs[subKey] ? 'h-icon-down' : 'h-icon-right'"></i>
          <span class="sps-prompt-key">{{ subKey }}</span>
          <span class="sps-prompt-label sub">子提示词</span>
          <span class="sps-force-badge" v-if="isForce(subKey)" title="RegisterDefaultForce: 启动时强制覆盖数据库">强制同步</span>
          <Button size="s" @click.stop="savePrompt(subKey)" :loading="savingKey === subKey">保存</Button>
        </div>
        <div v-if="expandedSubs[subKey]" class="sps-sub-body">
          <MdEditor
            v-model="promptData[subKey]"
            :title="getPromptTitle(subKey)"
            :height="300"
          />
          <div class="sps-placeholders" v-if="getPlaceholders(subKey).length">
            <span class="sps-ph-label">占位符:</span>
            <span
              v-for="ph in getPlaceholders(subKey)"
              :key="ph.name"
              class="sps-ph-tag"
              @click="insertPlaceholder(subKey, ph.name)"
              :title="ph.desc"
            >
              <code>{{ '{' + ph.name + '}' }}</code> {{ ph.desc }}
            </span>
          </div>
        </div>
      </div>
      <!-- 工具描述 (只读) -->
      <div v-if="promptMap.toolDesc && promptMap.toolDesc.length" class="sps-tool-descs">
        <div class="sps-td-title">工具描述（只读，由工具集自动注入）</div>
        <div v-for="tdKey in promptMap.toolDesc" :key="tdKey" class="sps-td-item">
          <span class="sps-td-key">{{ tdKey }}</span>
          <span class="sps-td-content">{{ promptData[tdKey] || '(未配置)' }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { getGenericStore } from '@/components/generic-module/generic-store';
import MdEditor from '../../components/md-editor.vue';
import { SCENE_PROMPT_MAP, PROMPT_PLACEHOLDERS, isForceKey } from './scenePromptMap';

const MC16 = 'RS_M16';

export default {
  name: 'ScenePromptSection',
  components: { MdEditor },
  props: {
    sceneCode: { type: String, default: '' },
    promptList: { type: Array, default: function() { return [] } }
  },
  data() {
    return {
      sectionExpanded: true,
      expandedSubs: {},
      promptData: {},
      savingKey: ''
    };
  },
  computed: {
    promptMap() {
      return SCENE_PROMPT_MAP[this.sceneCode] || { main: '', sub: [], toolDesc: [], forceKeys: [] };
    },
    promptKeys() {
      var m = this.promptMap;
      var keys = [];
      if (m.main) keys.push(m.main);
      if (m.sub) keys = keys.concat(m.sub);
      return keys;
    }
  },
  watch: {
    promptList: {
      handler: 'buildPromptData',
      immediate: true
    }
  },
  created() {
    this.m16Store = getGenericStore(MC16);
  },
  methods: {
    isForce(key) {
      return isForceKey(this.sceneCode, key);
    },
    toggleSection() {
      this.sectionExpanded = !this.sectionExpanded;
    },
    toggleSub(key) {
      this.$set(this.expandedSubs, key, !this.expandedSubs[key]);
    },
    buildPromptData() {
      var data = {};
      // 从 promptList 构建 key→content 映射
      (this.promptList || []).forEach(function(it) {
        data[it.PROMPTKEY] = it.CONTENT || '';
      });
      // 确保映射中的 key 都有值(即使列表里没有)
      var allKeys = this.promptKeys.concat(this.promptMap.toolDesc || []);
      allKeys.forEach(function(k) {
        if (!(k in data)) data[k] = '';
      });
      this.promptData = data;
    },
    getPromptTitle(key) {
      var item = (this.promptList || []).find(function(it) { return it.PROMPTKEY === key; });
      return item ? (item.DESCRIPTION || key) : key;
    },
    getPlaceholders(key) {
      if (PROMPT_PLACEHOLDERS[key]) return PROMPT_PLACEHOLDERS[key];
      return [];
    },
    insertPlaceholder(promptKey, phName) {
      var tag = '{' + phName + '}';
      this.$set(this.promptData, promptKey, (this.promptData[promptKey] || '') + tag);
    },
    async savePrompt(key) {
      var content = this.promptData[key];
      if (content === undefined || content === null) return;
      this.savingKey = key;
      try {
        // 先 open 加载完整行（不能 INIT+ADD，会撞 NOT NULL）
        var item = (this.promptList || []).find(function(it) { return it.PROMPTKEY === key; });
        if (item && item.ID) {
          await this.$callAction({ action: MC16 + '/open', param: { ID: item.ID } });
          var MAIN = this.m16Store.storeHelper.getTable('MAIN');
          if (MAIN) {
            MAIN.setValue('CONTENT', content);
            await this.$callAction({
              action: MC16 + '/save',
              successText: '提示词已保存'
            });
          }
        } else {
          // 新增: key 在映射中但数据库还没有记录
          this.$store.commit(MC16 + '/INIT', { paths: ['MAIN'] });
          this.$store.commit(MC16 + '/ADD', { path: 'MAIN', item: {} });
          var MAIN2 = this.m16Store.storeHelper.getTable('MAIN');
          if (MAIN2) {
            MAIN2.setValue('PROMPTKEY', key);
            MAIN2.setValue('VERSION', 'v1');
            MAIN2.setValue('WEIGHT', 100);
            MAIN2.setValue('DESCRIPTION', key);
            MAIN2.setValue('CONTENT', content);
            await this.$callAction({
              action: MC16 + '/save',
              successText: '提示词已创建',
              successCall: () => this.$emit('refresh-prompts')
            });
          }
        }
      } finally {
        this.savingKey = '';
      }
    }
  }
};
</script>

<style lang="less" scoped>
.scene-prompt-section {
  border: 1px solid #e8e8e8;
  border-radius: 6px;
  background: #fff;
  overflow: hidden;
}
.sps-header {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 12px;
  background: #fafafa;
  border-bottom: 1px solid #f0f0f0;
  cursor: pointer;
  flex-shrink: 0;
  &:hover { background: #f5f7fa; }
  i { color: #999; font-size: 12px; }
}
.sps-title { font-size: 13px; font-weight: 600; }
.sps-count {
  font-size: 11px; background: #f0f5ff; color: #2F54EB;
  padding: 0 6px; border-radius: 8px;
}
.sps-body { padding: 12px; }
.sps-empty { font-size: 12px; color: #999; padding: 8px 0; }
.sps-prompt-block {
  margin-bottom: 12px;
  border: 1px solid #f0f0f0;
  border-radius: 4px;
  overflow: hidden;
}
.sps-prompt-head {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 10px;
  background: #fafafa;
  border-bottom: 1px solid #f0f0f0;
  cursor: pointer;
  flex-shrink: 0;
  &:hover { background: #f5f7fa; }
  i { color: #999; font-size: 12px; }
}
.sps-prompt-key {
  font-size: 12px; font-weight: 600; color: #2F54EB;
  font-family: Consolas, monospace;
}
.sps-prompt-label {
  font-size: 11px; background: #e6f7ff; color: #1890ff;
  padding: 0 6px 6px; border-radius: 3px;
  &.sub { background: #f6ffed; color: #52c41a; }
}
.sps-force-badge {
  font-size: 10px; background: #fff7e6; color: #fa8c16;
  padding: 0px 5px; border-radius: 3px; border: 1px solid #ffe58f;
}
.sps-editor-main { flex: 1; min-height: 0; }
.sps-sub-body { padding: 8px; }
.sps-placeholders {
  padding: 6px 10px;
  border-top: 1px solid #f0f0f0;
  background: #fafafa;
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  align-items: center;
}
.sps-ph-label { font-size: 11px; color: #999; }
.sps-ph-tag {
  font-size: 11px; padding: 1px 6px; border: 1px solid #d9d9d9;
  border-radius: 3px; cursor: pointer;
  &:hover { border-color: #2F54EB; background: #f0f5ff; }
  code { font-family: Consolas, monospace; color: #2F54EB; font-size: 10px; }
}
.sps-tool-descs {
  margin-top: 8px;
  border: 1px dashed #e8e8e8;
  border-radius: 4px;
  padding: 8px 10px;
  background: #fafafa;
}
.sps-td-title { font-size: 12px; font-weight: 600; color: #666; margin-bottom: 6px; }
.sps-td-item {
  display: flex;
  gap: 8px;
  margin-bottom: 4px;
  font-size: 12px;
}
.sps-td-key {
  font-family: Consolas, monospace;
  color: #2F54EB;
  white-space: nowrap;
  min-width: 160px;
}
.sps-td-content {
  color: #999;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
</style>

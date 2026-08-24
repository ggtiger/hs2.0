<template>
  <div class="ai-console">
    <!-- 顶部: 标题 + 配置引导 -->
    <div class="ac-header">
      <div class="ac-title">
        <i class="h-icon-setting"></i>
        <span>AI 配置中心</span>
        <span class="ac-sub">场景为中心：提示词 + 工具 + 模型一体化配置</span>
      </div>
      <div class="ac-checklist">
        <div
          v-for="(s, i) in sections"
          :key="s.key"
          :class="['ac-check', { active: current === s.key }]"
          @click="current = s.key"
        >
          <span class="ac-check-no">{{ i + 1 }}</span>
          <span class="ac-check-name">{{ s.name }}</span>
          <span class="ac-check-count" v-if="counts[s.key] !== undefined">{{ counts[s.key] }}</span>
        </div>
      </div>
    </div>

    <div class="ac-body">
      <!-- 左侧导航 -->
      <div class="ac-nav">
        <div
          v-for="s in sections"
          :key="s.key"
          :class="['ac-nav-item', { active: current === s.key }]"
          @click="current = s.key"
        >
          <i :class="s.icon"></i>
          <div class="ac-nav-text">
            <div class="ac-nav-name">{{ s.name }}</div>
            <div class="ac-nav-desc">{{ s.desc }}</div>
          </div>
          <span class="ac-nav-count" v-if="counts[s.key] !== undefined">{{ counts[s.key] }}</span>
        </div>
      </div>

      <!-- 右侧内容 -->
      <div class="ac-content">
        <keep-alive>
          <component :is="currentComponent" @count="onCount" />
        </keep-alive>
      </div>
    </div>
  </div>
</template>

<script>
import SettingPart from './parts/setting.vue';
import PromptsPart from './parts/prompts.vue';
import ScenesPart from './parts/scenes.vue';
import ToolsPart from './parts/tools.vue';
import MemoryPart from './parts/memory.vue';
import UsagePart from './parts/usage.vue';

export default {
  name: 's01-m27-main',
  components: {
    SettingPart,
    PromptsPart,
    ScenesPart,
    ToolsPart,
    MemoryPart,
    UsagePart
  },
  data() {
    return {
      current: 'scenes',
      counts: {},
      sections: [
        { key: 'scenes', name: '场景', desc: '提示词+工具+模型一体化', icon: 'h-icon-home', comp: 'ScenesPart' },
        { key: 'setting', name: 'AI 设置', desc: 'LLM 服务商/Key/模型', icon: 'h-icon-setting', comp: 'SettingPart' },
        { key: 'memory', name: '记忆', desc: '规则/坑/示例知识库', icon: 'h-icon-star', comp: 'MemoryPart' },
        { key: 'usage', name: '调用记录', desc: 'tokens/成本/成功率', icon: 'h-icon-task', comp: 'UsagePart' },
        { key: 'prompts', name: '提示词管理', desc: '高级: 独立管理所有提示词', icon: 'h-icon-edit', comp: 'PromptsPart' },
        { key: 'tools', name: '工具管理', desc: '高级: 独立管理所有工具', icon: 'h-icon-link', comp: 'ToolsPart' }
      ]
    };
  },
  computed: {
    currentComponent() {
      var s = this.sections.find(x => x.key === this.current);
      return s ? s.comp : 'ScenesPart';
    }
  },
  methods: {
    onCount(payload) {
      if (payload && payload.key) {
        this.$set(this.counts, payload.key, payload.n);
      }
    }
  }
};
</script>

<style lang="less" scoped>
.ai-console {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: #f5f6f8;
}
.ac-header {
  background: #fff;
  border-bottom: 1px solid #e8e8e8;
  padding: 10px 16px 0;
  flex-shrink: 0;
}
.ac-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 16px;
  font-weight: 600;
  i { font-size: 18px; color: #2F54EB; }
  .ac-sub { font-size: 12px; color: #999; font-weight: 400; margin-left: 8px; }
}
.ac-checklist {
  display: flex;
  gap: 4px;
  margin-top: 10px;
}
.ac-check {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 14px;
  cursor: pointer;
  font-size: 13px;
  color: #666;
  border-bottom: 2px solid transparent;
  &:hover { color: #2F54EB; }
  &.active { color: #2F54EB; border-bottom-color: #2F54EB; font-weight: 600; }
  .ac-check-no {
    width: 16px; height: 16px;
    border-radius: 50%;
    background: #e8e8e8;
    color: #999;
    font-size: 11px;
    display: flex;
    align-items: center;
    justify-content: center;
  }
  &.active .ac-check-no { background: #2F54EB; color: #fff; }
  .ac-check-count {
    background: #f0f5ff;
    color: #2F54EB;
    border-radius: 8px;
    padding: 0 6px;
    font-size: 11px;
  }
}
.ac-body {
  flex: 1;
  display: flex;
  min-height: 0;
}
.ac-nav {
  width: 200px;
  background: #fff;
  border-right: 1px solid #e8e8e8;
  padding: 8px 0;
  flex-shrink: 0;
  overflow-y: auto;
}
.ac-nav-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 14px;
  cursor: pointer;
  border-left: 3px solid transparent;
  i { font-size: 16px; color: #999; }
  .ac-nav-name { font-size: 13px; font-weight: 600; color: #333; }
  .ac-nav-desc { font-size: 11px; color: #999; margin-top: 1px; }
  .ac-nav-count {
    margin-left: auto;
    background: #f0f5ff;
    color: #2F54EB;
    border-radius: 8px;
    padding: 0 6px;
    font-size: 11px;
  }
  &:hover { background: #f8f9fa; }
  &.active {
    background: #e6f7ff;
    border-left-color: #2F54EB;
    i { color: #2F54EB; }
    .ac-nav-name { color: #2F54EB; }
  }
}
.ac-content {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}
</style>

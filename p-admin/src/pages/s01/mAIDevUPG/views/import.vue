<template>
  <div class="upg-import">
    <div class="header">
      <div class="title">
        <span class="back-btn" @click="back">&lt; 返回</span>
        <h3>导入升级包</h3>
      </div>
    </div>

    <div class="steps">
      <div :class="['step', { active: step >= 1, done: step > 1 }]">
        <span class="step-num">1</span><span>上传脚本</span>
      </div>
      <div :class="['step', { active: step >= 2, done: step > 2 }]">
        <span class="step-num">2</span><span>预览变更项</span>
      </div>
      <div :class="['step', { active: step >= 3 }]">
        <span class="step-num">3</span><span>执行</span>
      </div>
    </div>

    <!-- Step1: 上传 -->
    <div v-if="step === 1" class="step-panel">
      <div class="upload-area">
        <input type="file" ref="fileInput" accept=".sql,.aidev.sql,.txt" @change="onFileChange" style="display:none" />
        <button class="h-btn h-btn-primary" @click="$refs.fileInput.click()">选择 .aidev.sql 文件</button>
        <span class="file-name" v-if="fileName">{{ fileName }}</span>
      </div>
      <div class="paste-area" v-if="!fileName">
        <p class="paste-tip">或粘贴脚本内容：</p>
        <textarea v-model="pasteContent" placeholder="粘贴 .aidev.sql 脚本内容" rows="10"></textarea>
      </div>
      <div class="actions">
        <button class="h-btn h-btn-primary" @click="doImport" :disabled="loading || (!fileName && !pasteContent.trim())">
          {{ loading ? '导入中...' : '导入' }}
        </button>
      </div>
    </div>

    <!-- Step2: 预览 -->
    <div v-if="step === 2" class="step-panel">
      <div class="upgrade-meta">
        <div class="meta-row"><span>会话编号:</span><b>{{ upgrade.SESSIONCODE }}</b></div>
        <div class="meta-row"><span>会话名称:</span><b>{{ upgrade.SESSIONNAME }}</b></div>
        <div class="meta-row"><span>类型:</span><b>{{ upgrade.SESSIONTYPE }}</b></div>
        <div class="meta-row"><span>目标模块:</span><b>{{ upgrade.TARGETMODULE }}</b></div>
        <div class="meta-row"><span>变更项数:</span><b>{{ items.length }}</b></div>
        <div class="meta-row"><span>意图:</span><span>{{ upgrade.INTENT }}</span></div>
      </div>
      <div class="items-list">
        <div v-for="(it, i) in items" :key="i" class="item-row">
          <div class="item-head">
            <span class="item-seq">#{{ i + 1 }}</span>
            <span class="item-cat">{{ it.category }}</span>
            <span class="item-action">{{ it.action }}</span>
            <span class="item-target">{{ it.target }}</span>
          </div>
          <pre class="item-sql">{{ it.sql }}</pre>
        </div>
      </div>
      <div class="actions">
        <button class="h-btn" @click="reset">重新上传</button>
        <button class="h-btn h-btn-primary" @click="doExecute" :disabled="executing">
          {{ executing ? '执行中...' : '确认执行' }}
        </button>
      </div>
    </div>

    <!-- Step3: 执行结果 -->
    <div v-if="step === 3" class="step-panel">
      <div :class="['result', executeResult.status === 'SUCCESS' ? 'success' : 'failed']">
        <div class="result-status">{{ executeResult.status === 'SUCCESS' ? '✓ 执行成功' : '✗ 执行失败' }}</div>
        <div class="result-detail" v-if="executeResult.status === 'SUCCESS'">
          共执行 {{ executeResult.itemCount }} 个变更项，耗时 {{ executeResult.durationMs }}ms
        </div>
        <div class="result-detail" v-else>
          <p>失败项: {{ executeResult.failedItemId }}</p>
          <p>错误: {{ executeResult.errorMsg }}</p>
        </div>
      </div>
      <div class="actions">
        <button class="h-btn h-btn-primary" @click="goDetail">查看详情</button>
        <button class="h-btn" @click="back">返回列表</button>
      </div>
    </div>
  </div>
</template>

<script>
import upg from '@/api/aidev-upg';

export default {
  name: 's01-mAIDevUPG-import',
  data() {
    return {
      step: 1,
      fileName: '',
      pasteContent: '',
      loading: false,
      executing: false,
      upgradeId: '',
      upgrade: {},
      items: [],
      executeResult: {},
    };
  },
  methods: {
    back() {
      this.$router.push('/s01/mAIDevUPG/main');
    },
    onFileChange(e) {
      const file = e.target.files[0];
      if (!file) return;
      this.fileName = file.name;
      const reader = new FileReader();
      reader.onload = ev => {
        this.pasteContent = ev.target.result;
      };
      reader.readAsText(file);
    },
    async doImport() {
      if (!this.pasteContent.trim()) {
        this.$Notice('请选择文件或粘贴脚本');
        return;
      }
      this.loading = true;
      try {
        const ret = await upg.importScript(this.pasteContent);
        this.upgradeId = (ret && ret.upgradeId) || ret;
        const previewRet = await upg.preview(this.upgradeId);
        this.upgrade = (previewRet && previewRet.upgrade) || {};
        this.items = (previewRet && previewRet.items) || [];
        this.step = 2;
        this.$alert('导入成功，请预览变更项');
      } catch (e) {
        this.$error('导入失败: ' + (e.message || e));
      } finally {
        this.loading = false;
      }
    },
    async doExecute() {
      this.executing = true;
      try {
        const ret = await upg.execute(this.upgradeId);
        this.executeResult = ret || {};
        this.step = 3;
        if (this.executeResult.status === 'SUCCESS') {
          this.$alert('升级执行成功');
        } else {
          this.$error('执行失败: ' + this.executeResult.errorMsg);
        }
      } catch (e) {
        this.executeResult = { status: 'FAILED', errorMsg: e.message || e };
        this.step = 3;
        this.$error('执行失败: ' + (e.message || e));
      } finally {
        this.executing = false;
      }
    },
    reset() {
      this.step = 1;
      this.fileName = '';
      this.pasteContent = '';
      this.upgradeId = '';
      this.upgrade = {};
      this.items = [];
    },
    goDetail() {
      this.$router.push(`/s01/mAIDevUPG/detail/${this.upgradeId}`);
    },
  },
};
</script>

<style lang="less" scoped>
.upg-import { padding: 16px; max-width: 1000px; margin: 0 auto; }
.header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.title { display: flex; align-items: center; gap: 12px; }
.back-btn { cursor: pointer; color: #1890ff; }
.back-btn:hover { text-decoration: underline; }
.title h3 { margin: 0; font-size: 16px; }
.steps { display: flex; gap: 0; margin-bottom: 24px; }
.step { flex: 1; display: flex; align-items: center; gap: 8px; padding: 12px; background: #f0f0f0; color: #999; position: relative; }
.step:not(:last-child)::after { content: ''; position: absolute; right: -8px; top: 50%; transform: translateY(-50%); border: 8px solid transparent; border-left-color: #f0f0f0; z-index: 1; }
.step.active { background: #1890ff; color: #fff; }
.step.active:not(:last-child)::after { border-left-color: #1890ff; }
.step.done { background: #52c41a; color: #fff; }
.step.done:not(:last-child)::after { border-left-color: #52c41a; }
.step-num { width: 22px; height: 22px; border-radius: 50%; background: rgba(255,255,255,0.3); display: inline-flex; align-items: center; justify-content: center; font-size: 12px; }
.step-panel { background: #fff; padding: 24px; border-radius: 4px; border: 1px solid #e8e8e8; }
.upload-area { display: flex; align-items: center; gap: 12px; margin-bottom: 16px; }
.file-name { color: #1890ff; }
.paste-area { margin-top: 16px; }
.paste-tip { color: #999; font-size: 13px; margin-bottom: 8px; }
.paste-area textarea { width: 100%; border: 1px solid #d9d9d9; border-radius: 4px; padding: 8px; font-family: monospace; font-size: 12px; }
.actions { margin-top: 24px; display: flex; gap: 8px; justify-content: flex-end; }
.upgrade-meta { background: #fafafa; padding: 16px; border-radius: 4px; margin-bottom: 16px; }
.meta-row { display: flex; gap: 8px; padding: 4px 0; font-size: 13px; }
.meta-row span:first-child { color: #999; min-width: 80px; }
.meta-row b { color: #333; }
.items-list { max-height: 400px; overflow-y: auto; }
.item-row { border: 1px solid #e8e8e8; border-radius: 4px; padding: 10px; margin-bottom: 10px; }
.item-head { display: flex; gap: 8px; font-size: 12px; margin-bottom: 6px; }
.item-seq { font-weight: bold; color: #1890ff; }
.item-cat { background: #e6f7ff; color: #1890ff; padding: 1px 6px; border-radius: 3px; }
.item-action { background: #f0f0f0; padding: 1px 6px; border-radius: 3px; }
.item-target { color: #666; }
.item-sql { background: #1e1e1e; color: #d4d4d4; padding: 8px; border-radius: 4px; font-size: 12px; overflow-x: auto; margin: 0; white-space: pre-wrap; word-break: break-all; }
.result { padding: 24px; border-radius: 4px; text-align: center; }
.result.success { background: #f6ffed; border: 1px solid #52c41a; }
.result.failed { background: #fff1f0; border: 1px solid #f5222d; }
.result-status { font-size: 20px; font-weight: bold; margin-bottom: 8px; }
.result.success .result-status { color: #52c41a; }
.result.failed .result-status { color: #f5222d; }
.result-detail { color: #666; font-size: 14px; }
</style>

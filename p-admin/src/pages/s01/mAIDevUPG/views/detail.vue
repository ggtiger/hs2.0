<template>
  <div class="upg-detail">
    <div class="header">
      <div class="title">
        <span class="back-btn" @click="back">&lt; 返回</span>
        <h3>升级详情 — {{ upgrade.UPGRADECODE }}</h3>
        <span :class="['status-tag', 'st-' + (upgrade.STATUS || '').toLowerCase()]">{{ statusLabel(upgrade.STATUS) }}</span>
      </div>
      <div class="actions">
        <button class="h-btn h-btn-red" v-if="upgrade.STATUS === 'SUCCESS'" @click="doRollback" :disabled="rollingBack">
          {{ rollingBack ? '回滚中...' : '回滚' }}
        </button>
      </div>
    </div>

    <div class="meta-panel">
      <div class="meta-grid">
        <div class="meta-item"><label>会话编号</label><span>{{ upgrade.SESSIONCODE }}</span></div>
        <div class="meta-item"><label>会话名称</label><span>{{ upgrade.SESSIONNAME }}</span></div>
        <div class="meta-item"><label>类型</label><span>{{ upgrade.SESSIONTYPE }}</span></div>
        <div class="meta-item"><label>目标模块</label><span>{{ upgrade.TARGETMODULE }}</span></div>
        <div class="meta-item"><label>变更项数</label><span>{{ upgrade.ITEMCOUNT }}</span></div>
        <div class="meta-item"><label>执行人</label><span>{{ upgrade.EXECUTEDBYNAME || upgrade.EXECUTEDBY }}</span></div>
        <div class="meta-item"><label>执行时间</label><span>{{ upgrade.EXECUTEDTIME }}</span></div>
        <div class="meta-item"><label>耗时</label><span>{{ upgrade.DURATIONMS }}ms</span></div>
      </div>
      <div class="intent" v-if="upgrade.INTENT"><label>意图:</label><span>{{ upgrade.INTENT }}</span></div>
      <div class="error-msg" v-if="upgrade.ERRORMSG"><label>错误信息:</label><span>{{ upgrade.ERRORMSG }}</span></div>
    </div>

    <div class="logs-panel">
      <h4>执行明细 ({{ logs.length }})</h4>
      <div class="logs-table">
        <div class="log-head">
          <span class="col-seq">#</span>
          <span class="col-cat">类别</span>
          <span class="col-action">动作</span>
          <span class="col-target">目标</span>
          <span class="col-status">状态</span>
          <span class="col-rows">影响行</span>
          <span class="col-time">时间</span>
        </div>
        <div v-for="(log, i) in logs" :key="i" :class="['log-row', 'st-' + (log.STATUS || '').toLowerCase()]">
          <span class="col-seq">{{ i + 1 }}</span>
          <span class="col-cat">{{ log.ITEMCATEGORY }}</span>
          <span class="col-action">{{ log.ITEMACTION }}</span>
          <span class="col-target" :title="log.ITEMTARGET">{{ log.ITEMTARGET }}</span>
          <span :class="['col-status', 'st-' + (log.STATUS || '').toLowerCase()]">{{ log.STATUS }}</span>
          <span class="col-rows">{{ log.ROWSAFFECTED }}</span>
          <span class="col-time">{{ log.EXECUTEDTIME }}</span>
          <div class="log-error" v-if="log.ERRORMSG">{{ log.ERRORMSG }}</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import upg from '@/api/aidev-upg';

export default {
  name: 's01-mAIDevUPG-detail',
  data() {
    return {
      upgradeId: this.$route.params.id,
      upgrade: {},
      logs: [],
      rollingBack: false,
    };
  },
  async mounted() {
    await this.load();
  },
  methods: {
    statusLabel(s) {
      const map = { PENDING: '待执行', RUNNING: '执行中', SUCCESS: '成功', FAILED: '失败', ROLLEDBACK: '已回滚' };
      return map[s] || s;
    },
    back() {
      this.$router.push('/s01/mAIDevUPG/main');
    },
    async load() {
      try {
        const ret = await upg.openUpgrade(this.upgradeId);
        // A02 open 返回 [{MAIN:[...], DTSA:[...]}] 结构, DTSA 是 upgrade_log 子表
        const main = (ret && ret[0] && ret[0].MAIN) || [];
        this.upgrade = main[0] || {};
        // 子表数据, 路径名可能是 DTSA 或其他, 遍历找
        const keys = ret && ret[0] ? Object.keys(ret[0]) : [];
        for (const k of keys) {
          if (k !== 'MAIN' && Array.isArray(ret[0][k])) {
            this.logs = ret[0][k];
            break;
          }
        }
      } catch (e) {
        this.$error('加载失败: ' + (e.message || e));
      }
    },
    async doRollback() {
      try {
        await this.$confirm('确认回滚此升级？回滚将执行反向脚本，可能删除已建的表/数据。');
        this.rollingBack = true;
        const ret = await upg.rollback(this.upgradeId);
        if (ret && ret.status === 'ROLLEDBACK') {
          this.$alert('回滚成功');
        } else {
          this.$error('回滚失败: ' + (ret && ret.errorMsg));
        }
        await this.load();
      } catch (e) {
        if (e !== 'cancel') {
          this.$error('回滚失败: ' + (e.message || e));
        }
      } finally {
        this.rollingBack = false;
      }
    },
  },
};
</script>

<style lang="less" scoped>
.upg-detail { padding: 16px; }
.header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.title { display: flex; align-items: center; gap: 12px; }
.back-btn { cursor: pointer; color: #1890ff; }
.back-btn:hover { text-decoration: underline; }
.title h3 { margin: 0; font-size: 16px; }
.status-tag { padding: 2px 10px; border-radius: 10px; font-size: 12px; }
.status-tag.st-success { background: #f6ffed; color: #52c41a; }
.status-tag.st-failed { background: #fff1f0; color: #f5222d; }
.status-tag.st-pending { background: #fff7e6; color: #fa8c16; }
.status-tag.st-running { background: #e6f7ff; color: #1890ff; }
.status-tag.st-rolledback { background: #f0f0f0; color: #999; }
.meta-panel { background: #fff; padding: 16px; border-radius: 4px; border: 1px solid #e8e8e8; margin-bottom: 16px; }
.meta-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; }
.meta-item { display: flex; flex-direction: column; }
.meta-item label { font-size: 12px; color: #999; margin-bottom: 2px; }
.meta-item span { font-size: 13px; color: #333; }
.intent { margin-top: 12px; padding-top: 12px; border-top: 1px solid #f0f0f0; font-size: 13px; }
.intent label { color: #999; margin-right: 8px; }
.error-msg { margin-top: 8px; padding: 8px 12px; background: #fff1f0; border-radius: 4px; font-size: 13px; color: #f5222d; }
.error-msg label { font-weight: bold; margin-right: 8px; }
.logs-panel { background: #fff; padding: 16px; border-radius: 4px; border: 1px solid #e8e8e8; }
.logs-panel h4 { margin: 0 0 12px 0; font-size: 14px; }
.logs-table { font-size: 12px; }
.log-head { display: flex; gap: 8px; padding: 8px; background: #fafafa; border-bottom: 2px solid #e8e8e8; font-weight: bold; }
.log-row { display: flex; gap: 8px; padding: 8px; border-bottom: 1px solid #f0f0f0; flex-wrap: wrap; }
.log-row.st-failed { background: #fff1f0; }
.col-seq { width: 30px; }
.col-cat { width: 100px; }
.col-action { width: 70px; }
.col-target { flex: 1; min-width: 150px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.col-status { width: 70px; }
.col-status.st-success { color: #52c41a; }
.col-status.st-failed { color: #f5222d; }
.col-rows { width: 60px; text-align: right; }
.col-time { width: 150px; color: #999; }
.log-error { width: 100%; color: #f5222d; padding: 4px 0 0 30px; }
</style>

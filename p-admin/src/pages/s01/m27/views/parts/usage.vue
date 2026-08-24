<template>
  <div class="usage-part">
    <!-- 统计卡片 -->
    <div class="up-stats">
      <div class="up-stat">
        <div class="up-stat-num">{{ stats.total }}</div>
        <div class="up-stat-label">总调用次数</div>
      </div>
      <div class="up-stat">
        <div class="up-stat-num">{{ formatTokens(stats.tokens) }}</div>
        <div class="up-stat-label">总 Tokens</div>
      </div>
      <div class="up-stat">
        <div class="up-stat-num">¥{{ (stats.cost || 0).toFixed(2) }}</div>
        <div class="up-stat-label">总成本(估算)</div>
      </div>
      <div class="up-stat">
        <div class="up-stat-num">{{ stats.successRate }}%</div>
        <div class="up-stat-label">成功率</div>
      </div>
    </div>

    <!-- 筛选 -->
    <div class="up-filter">
      <Select v-model="filters.OPERATIONTYPE" :datas="sceneOptions" placeholder="全部场景" @change="loadList(1)" />
      <input v-model="filters.USERNAME" placeholder="用户名" @keyup.enter="loadList(1)" />
      <input v-model="filters.MODULECODE" placeholder="模块编码" @keyup.enter="loadList(1)" />
      <Select v-model="filters.ISSUCCESS" :datas="successOptions" placeholder="全部状态" @change="loadList(1)" />
      <Button size="s" color="primary" @click="loadList(1)">查询</Button>
    </div>

    <!-- 明细表格 -->
    <div class="up-table-wrap" v-loading="loading">
      <table class="up-table">
        <thead>
          <tr>
            <th>时间</th><th>场景</th><th>用户</th><th>模块</th><th>工具</th>
            <th class="num">Prompt</th><th class="num">Completion</th><th class="num">Total</th>
            <th class="num">成本</th><th class="num">耗时(ms)</th><th>结果</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="it in list" :key="it.ID">
            <td class="time">{{ formatTime(it.REQUESTTIME) }}</td>
            <td><span :class="['up-scene', it.OPERATIONTYPE]">{{ sceneLabel(it.OPERATIONTYPE) }}</span></td>
            <td>{{ it.USERNAME }}</td>
            <td>{{ it.MODULECODE }}</td>
            <td>{{ it.TOOLNAME }}</td>
            <td class="num">{{ it.PROMPTTOKENS }}</td>
            <td class="num">{{ it.COMPLETIONTOKENS }}</td>
            <td class="num">{{ it.TOTALTOKENS }}</td>
            <td class="num">¥{{ it.COST }}</td>
            <td class="num">{{ it.DURATIONMS }}</td>
            <td>
              <span :class="['up-result', it.ISSUCCESS === 1 ? 'ok' : 'fail']" :title="it.ERRORMSG">
                {{ it.ISSUCCESS === 1 ? '成功' : '失败' }}
              </span>
            </td>
          </tr>
          <tr v-if="!loading && list.length === 0">
            <td colspan="11" class="up-empty">暂无调用记录</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- 分页 -->
    <div class="up-pager">
      <Pagination v-model="pager" align="right" @change="loadList(pager.page)" />
    </div>
  </div>
</template>

<script>
import { getGenericStore } from '@/components/generic-module/generic-store';

const MC = 'RS_M15';

export default {
  name: 'UsagePart',
  data() {
    return {
      storeName: MC,
      loading: false,
      list: [],
      totalCount: 0,
      pageIndex: 1,
      pageSize: 30,
      pager: { page: 1, size: 30, total: 0 },
      stats: { total: 0, tokens: 0, cost: 0, successRate: 100 },
      filters: { OPERATIONTYPE: '', USERNAME: '', MODULECODE: '', ISSUCCESS: '' },
      sceneOptions: [
        { key: '', title: '全部场景' },
        { key: 'chat', title: 'chat 通用助理' },
        { key: 'form', title: 'form 表单填报' },
        { key: 'aidev', title: 'aidev 开发助理' },
        { key: 'wizard', title: 'wizard 模块向导' },
        { key: 'sfc', title: 'sfc SFC代码' },
        { key: 'optimize', title: 'optimize 提示词优化' },
        { key: 'vision', title: 'vision 图片识别' }
      ],
      successOptions: [
        { key: '', title: '全部状态' },
        { key: '1', title: '成功' },
        { key: '0', title: '失败' }
      ]
    };
  },
  created() {
    this.storeObj = getGenericStore(MC);
  },
  mounted() {
    this.loadList(1);
  },
  methods: {
    formatTokens(n) {
      n = n || 0;
      if (n >= 1000000) return (n / 1000000).toFixed(1) + 'M';
      if (n >= 1000) return (n / 1000).toFixed(1) + 'K';
      return n + '';
    },
    formatTime(t) {
      return (t || '').replace('T', ' ').substring(5, 16);
    },
    sceneLabel(type) {
      var map = { chat: '助理', form: '填报', aidev: '开发', wizard: '向导', sfc: 'SFC', optimize: '优化', vision: '视觉' };
      return map[type] || type || '-';
    },
    async loadList(page) {
      this.loading = true;
      this.pageIndex = page;
      try {
        var QQRY = this.storeObj.storeHelper.getTable('QQRY');
        if (QQRY) {
          QQRY.setValue('PageSize', this.pageSize);
          QQRY.setValue('PageIndex', page);
          if (this.filters.OPERATIONTYPE) QQRY.setValue('OPERATIONTYPE', this.filters.OPERATIONTYPE);
          if (this.filters.USERNAME) QQRY.setValue('USERNAME', this.filters.USERNAME);
          if (this.filters.MODULECODE) QQRY.setValue('MODULECODE', this.filters.MODULECODE);
          if (this.filters.ISSUCCESS !== '') QQRY.setValue('ISSUCCESS', this.filters.ISSUCCESS);
        }
        await this.$callAction({ action: MC + '/query' });
        var st = this.$store.state[MC];
        this.list = (st && st.dt && st.dt.QRY && st.dt.QRY.data) || [];
        this.totalCount = (st && st.dt && st.dt.QQRY && st.dt.QQRY.getValue('TotalCount')) || this.list.length;
        this.pager.total = this.totalCount;
        this.pager.page = page;
        this.pager.size = this.pageSize;
        this.$emit('count', { key: 'usage', n: this.totalCount });
        this.computeStats();
      } finally {
        this.loading = false;
      }
    },
    computeStats() {
      // 基于当前页估算(精确统计需后端聚合, 先简单处理)
      var total = 0;
      var tokens = 0;
      var cost = 0;
      var ok = 0;
      this.list.forEach(it => {
        total++;
        tokens += it.TOTALTOKENS || 0;
        cost += parseFloat(it.COST || 0);
        if (it.ISSUCCESS === 1) ok++;
      });
      this.stats = {
        total: this.totalCount,
        tokens: tokens,
        cost: cost,
        successRate: total > 0 ? Math.round(ok * 100 / total) : 100
      };
    }
  }
};
</script>

<style lang="less" scoped>
.usage-part { flex: 1; display: flex; flex-direction: column; min-height: 0; background: #fff; }
.up-stats {
  display: flex;
  gap: 12px;
  padding: 14px 16px;
  border-bottom: 1px solid #f0f0f0;
}
.up-stat {
  flex: 1;
  background: linear-gradient(135deg, #f0f5ff 0%, #fafcff 100%);
  border: 1px solid #e6f0ff;
  border-radius: 8px;
  padding: 12px 16px;
}
.up-stat-num { font-size: 22px; font-weight: 700; color: #2F54EB; }
.up-stat-label { font-size: 12px; color: #999; margin-top: 2px; }
.up-filter {
  display: flex;
  gap: 8px;
  padding: 10px 16px;
  border-bottom: 1px solid #f0f0f0;
  input {
    border: 1px solid #d9d9d9;
    border-radius: 4px;
    padding: 5px 8px;
    font-size: 12px;
    outline: none;
    width: 140px;
    &:focus { border-color: #2F54EB; }
  }
}
.up-table-wrap { flex: 1; overflow: auto; padding: 0 16px; }
.up-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 12px;
  th, td { padding: 7px 10px; border-bottom: 1px solid #f0f0f0; text-align: left; }
  th { background: #fafafa; font-weight: 600; color: #666; position: sticky; top: 0; }
  .num { text-align: right; font-family: Consolas, monospace; }
  .time { color: #999; font-family: Consolas, monospace; }
}
.up-result {
  padding: 1px 8px; border-radius: 3px; font-size: 11px;
  &.ok { background: #f6ffed; color: #52c41a; }
  &.fail { background: #fff1f0; color: #f5222d; cursor: help; }
}
.up-scene {
  font-size: 11px; padding: 1px 6px; border-radius: 3px;
  &.chat { background: #f0f5ff; color: #2F54EB; }
  &.form { background: #f6ffed; color: #52c41a; }
  &.aidev { background: #fff7e6; color: #fa8c16; }
  &.wizard { background: #fff0f6; color: #eb2f96; }
  &.sfc { background: #e6fffb; color: #13c2c2; }
  &.optimize { background: #f9f0ff; color: #722ed1; }
  &.vision { background: #f5f5f5; color: #666; }
}
.up-empty { text-align: center; color: #bbb; padding: 30px; }
.up-pager { padding: 10px 16px; border-top: 1px solid #f0f0f0; display: flex; justify-content: flex-end; }
</style>

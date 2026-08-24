<template>
  <div :class="['vdv', { fill }]">
    <!-- HeyUI Tabs: :datas + v-model（无 TabPane 组件） -->
    <Tabs :datas="tabDatas" class-name="h-tabs-card" v-model="activeTab"></Tabs>
    <!-- 字段对比表 -->
    <div v-if="activeTab === 'fields'" class="vdv-pane" :style="paneStyle">
      <table class="vdv-table">
        <thead>
          <tr><th class="col-field">字段</th><th>变更前</th><th>变更后</th></tr>
        </thead>
        <tbody>
          <tr v-for="r in fieldRows" :key="r.field" :class="{ changed: r.changed }">
            <td class="col-field">{{ r.field }}</td>
            <td class="cell">{{ r.before }}</td>
            <td class="cell">{{ r.after }}</td>
          </tr>
          <tr v-if="!fieldRows || fieldRows.length === 0">
            <td colspan="3" class="vdv-empty">无字段级变化</td>
          </tr>
        </tbody>
      </table>
    </div>
    <!-- 文本行级 diff -->
    <div v-else-if="activeTextTab" class="vdv-pane" :style="paneStyle">
      <div class="vdv-stat">
        <span class="add">+{{ activeTextTab.stat.add }}</span>
        <span class="del">-{{ activeTextTab.stat.del }}</span>
      </div>
      <pre class="vdv-lines"><div v-for="(l, i) in activeTextTab.ops" :key="i" :class="'ln-' + l.type"><span class="ln-sign">{{ l.type === 'add' ? '+' : (l.type === 'del' ? '-' : ' ') }}</span>{{ l.text }}</div></pre>
    </div>
  </div>
</template>
<script>
import { lineDiff, splitSnapshot, diffStat } from '@/utils/simpleDiff';

// 版本快照对比视图（纯展示）：before/after 两个 JSON 快照 → 字段对比表 + 文本行级 diff
// 版本对比弹窗(m22)与通用历史弹窗(generic-module)共用
// fill=true 时占满父容器高度（flex），内容内部滚动；否则按 height 固定高度
export default {
  name: 'version-diff-view',
  props: {
    beforeContent: { type: String, default: null },
    afterContent: { type: String, default: null },
    height: { type: Number, default: 480 },
    fill: { type: Boolean, default: false },
  },
  data() {
    return {
      textTabs: [],
      fieldRows: [],
      activeTab: 'fields',
    };
  },
  computed: {
    paneStyle() {
      return this.fill ? null : { height: this.height + 'px' };
    },
    // HeyUI Tabs datas: [{key, title}]
    tabDatas() {
      var datas = [{ key: 'fields', title: '字段对比' + (this.fieldRows.filter(function(r) { return r.changed }).length ? '' : '(无变化)') }];
      this.textTabs.forEach(function(t) {
        datas.push({ key: t.key, title: t.name + ' (+' + t.stat.add + '/-' + t.stat.del + ')' });
      });
      return datas;
    },
    activeTextTab() {
      var self = this;
      return this.textTabs.find(function(t) { return t.key === self.activeTab }) || null;
    },
  },
  watch: {
    beforeContent() {
      this.buildTabs();
    },
    afterContent() {
      this.buildTabs();
    },
  },
  mounted() {
    this.buildTabs();
  },
  methods: {
    // diff(BEFORECONTENT, AFTERCONTENT): 字段级 + 大文本字段行级
    buildTabs() {
      var split = splitSnapshot(this.beforeContent, this.afterContent);
      this.fieldRows = split.fieldRows;
      this.textTabs = split.textFields.map(function(tf) {
        var ops = lineDiff(tf.before, tf.after);
        return { key: 'text_' + tf.name, name: tf.name, ops: ops, stat: diffStat(ops) };
      });
      // 有大文本变化时默认看第一个文本 diff，否则看字段表
      this.activeTab = this.textTabs.length > 0 ? this.textTabs[0].key : 'fields';
    },
  },
};
</script>
<style lang="less" scoped>
.vdv.fill {
  height: 100%;
  display: flex;
  flex-direction: column;
  .vdv-pane {
    flex: 1;
    min-height: 0;
  }
}
.vdv-pane {
  position: relative;
  overflow: auto;
  border: 1px solid #e8eaec;
  border-radius: 4px;
}
.vdv-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 12px;
  th, td {
    border-bottom: 1px solid #f0f0f0;
    padding: 6px 10px;
    text-align: left;
    vertical-align: top;
  }
  th {
    background: #f8f8f9;
    color: #515a6e;
    position: sticky;
    top: 0;
  }
  .col-field {
    width: 180px;
    font-weight: 600;
    color: #515a6e;
  }
  .cell {
    word-break: break-all;
    white-space: pre-wrap;
  }
  tr.changed td {
    background: #fffbe6;
  }
}
.vdv-empty {
  text-align: center;
  color: #9ea7b4;
  padding: 30px 0;
}
.vdv-stat {
  position: absolute;
  top: 6px;
  right: 14px;
  z-index: 2;
  font-size: 12px;
  font-weight: 600;
  .add { color: #52c41a; margin-right: 8px; }
  .del { color: #f5222d; }
}
.vdv-lines {
  margin: 0;
  padding: 6px 0;
  font-family: Consolas, Monaco, monospace;
  font-size: 12px;
  line-height: 1.6;
  > div {
    padding: 0 10px;
    white-space: pre-wrap;
    word-break: break-all;
  }
  .ln-sign {
    display: inline-block;
    width: 16px;
    color: #9ea7b4;
  }
  .ln-add { background: #f6ffed; }
  .ln-add .ln-sign { color: #52c41a; }
  .ln-del { background: #fff1f0; }
  .ln-del .ln-sign { color: #f5222d; }
  .ln-same { color: #515a6e; }
}
</style>

<!--
  rs-meta-query-panel 查询面板组件

  基于 rs-query-panel/index.vue 的模式，增加：
  - overrides 字段级覆盖
  - path 支持（DataTable/数组/路径名）
  - moduleCode/resourceName 自动加载字段
  - rs-meta-query-panel-field 子组件支持

  用法:
    1. 自动渲染所有查询字段:
       <rs-meta-query-panel :path="qqryDt" module-code="LI_M00" @query="onQuery" />

    2. 手动指定字段:
       <rs-meta-query-panel :path="qqryDt" module-code="LI_M00" :show-buttons="false">
         <rs-meta-query-panel-field field-name="BUSTYPEID" />
         <rs-meta-query-panel-field field-name="STATE" :override="{ type: 'select', dict: 'D0701' }" />
         <Button color="primary" @click="$parent.onQuery">查询</Button>
       </rs-meta-query-panel>

    3. 字段覆盖:
       <rs-meta-query-panel :path="qqryDt" module-code="LI_M00" :overrides="{ BUSTYPEID: { type: 'select', dict: 'D0701' } }" />
-->
<template>
  <div v-if="fields.length" class="rs-meta-query-panel" style="padding:10px 0;">
    <Row :space="9">
      <Cell v-for="f in fields" :key="f.ID || (f.RESFIELDNAME || f.FIELDNAME)" :width="cellWidth">
        <rs-meta-query-panel-field
          :field-config="f"
          :override="fieldOverrides[fieldKey(f)]"
        />
      </Cell>
      <Cell :width="cellWidth" v-if="fields.length && showButtons" style="float: right;">
        <div style="text-align:right;">
          <Button color="primary" @click="onQuery">查询</Button>
          <Button class="ml5" @click="onReset">重置</Button>
          <slot name="extra-buttons" />
        </div>
      </Cell>
      <!-- 手动字段插槽 -->
      <slot v-if="!fields.length" />
    </Row>
  </div>
</template>

<script>
import { buildAutoCompleteOption } from '@/utils/selRegistry';

export default {
  name: 'rs-meta-query-panel',
  provide() {
    return {
      // 给 rs-meta-query-panel-field 注入读写能力
      queryPanel: this,
    };
  },
  props: {
    // === 数据源 ===
    // QQRY DataTable 对象 / 数组(mapDateTable getter) / 路径名字符串
    path: { type: [Object, String, Array], default: null },
    storeName: { type: String, default: '' },

    // === 字段配置 ===
    // 直接传入 scm 原始字段数组（tss_resuipc 格式，含 QUERYSORT/QUERYTYPE/QUERYMODE 等）
    fieldsConfig: { type: Array, default: null },
    // 按资源名从 scm 加载
    resourceName: { type: String, default: '' },
    // 按模块编码推导 resourceName
    moduleCode: { type: String, default: '' },

    // === 字段级覆盖 ===
    // { BUSTYPEID: { type: 'select', dict: 'D0701' } }
    overrides: { type: Object, default: function() { return {} } },

    // === 面板配置 ===
    showButtons: { type: Boolean, default: true },
    cellWidth: { type: Number, default: 6 },
  },
  data() {
    return {
      queryValues: {},
      scmLoaded: false,
      loadedScmData: null,
    };
  },
  computed: {
    // 解析 DataTable（用于查询时同步）
    resolvedPath() {
      if (this.path && typeof this.path === 'object' && this.path.setValue) {
        return this.path;
      }
      if (Array.isArray(this.path)) {
        return this._findDtByData(this.path);
      }
      if (typeof this.path === 'string' && this.path) {
        var sn = this.storeName;
        if (sn) {
          var storeState = this.$store.state[sn];
          if (storeState && storeState.dt) {
            return storeState.dt[this.path] || null;
          }
        }
      }
      return null;
    },
    // scm 原始数据（tss_resuipc 格式）
    scmData() {
      if (this.fieldsConfig) return this.fieldsConfig;
      if (this.loadedScmData) return this.loadedScmData;
      var resName = this._resolveResourceName();
      if (!resName) return [];
      return this.$store.state.app.scms[resName] || [];
    },
    // 过滤出查询字段（QUERYSORT > 0）
    fields() {
      var src = this.scmData;
      if (!src || !src.length) return [];
      return src
        .filter(f => +f.QUERYSORT > 0)
        .sort((a, b) => (+a.QUERYSORT || 0) - (+b.QUERYSORT || 0));
    },
    // 合并 overrides
    fieldOverrides() {
      return this.overrides || {};
    },
  },
  watch: {
    resourceName: { handler: function() { this.loadScm() }, immediate: true },
    moduleCode: { handler: function() { this.loadScm() }, immediate: true },
    fields: {
      immediate: true,
      handler(arr) {
        (arr || []).forEach(f => {
          var k = this.fieldKey(f);
          if (!k) return;
          if (this.queryValues[k] === undefined) {
            this.$set(this.queryValues, k, this.defaultVal(f));
          }
        });
      },
    },
  },
  methods: {
    // 从 Vuex store 查找 data === arr 的 DataTable
    _findDtByData(arr) {
      var state = this.$store.state;
      for (var key in state) {
        var modState = state[key];
        if (modState && modState.dt) {
          for (var pathName in modState.dt) {
            if (modState.dt[pathName] && modState.dt[pathName].data === arr) {
              return modState.dt[pathName];
            }
          }
        }
      }
      return null;
    },

    async loadScm() {
      var resName = this._resolveResourceName();
      if (!resName) {
        this.scmLoaded = true;
        return;
      }
      this.scmLoaded = false;
      try {
        // eslint-disable-next-line no-restricted-syntax
        await this.$store.dispatch('app/initScms', [resName]);
        this.loadedScmData = this.$store.state.app.scms[resName] || [];
      } catch (e) {
        console.error('[rs-meta-query-panel] scm 加载失败:', resName, e);
      } finally {
        this.scmLoaded = true;
      }
    },

    _resolveResourceName() {
      if (this.resourceName) return this.resourceName;
      if (this.moduleCode) {
        var modData = this.$store.state.app.modules[this.moduleCode];
        if (modData && modData.MODPATH) {
          // 查询面板用 QQRY 路径
          var mpItem = modData.MODPATH.find(function(p) {
            return p.PATHNAME === 'QQRY' || p.PATHNAME === 'QRY';
          });
          if (mpItem) return mpItem.RESOURCENAME;
        }
      }
      return '';
    },

    // === 给 rs-meta-query-panel-field 用的方法 ===
    fieldKey(f) {
      return f.RESFIELDNAME || f.FIELDNAME || '';
    },
    typeOf(f) {
      return f.QUERYTYPE || f.EDITTYPE || 'input';
    },
    modeOf(f) {
      if (f.QUERYMODE) return f.QUERYMODE;
      var t = this.typeOf(f);
      if (t === 'select' || t === 'datepicker' || t === 'autocomplete' || t === 'number') return 'eq';
      if (t === 'daterange') return 'range';
      return 'like';
    },
    defaultVal(f) {
      if (this.typeOf(f) === 'daterange' || this.modeOf(f) === 'range') return { start: '', end: '' };
      if (this.modeOf(f) === 'in') return [];
      return '';
    },
    getValue(f) {
      return this.queryValues[this.fieldKey(f)];
    },
    setValue(f, v) {
      this.$set(this.queryValues, this.fieldKey(f), v);
    },
    rangeVal(f, key) {
      var v = this.getValue(f);
      return (v && v[key]) || '';
    },
    setRangeVal(f, key, value) {
      var v = Object.assign({}, this.getValue(f), { [key]: value });
      this.setValue(f, v);
    },
    parseSelectDatas(raw) {
      if (!raw) return [];
      try {
        var parsed = JSON.parse(raw);
        if (Array.isArray(parsed)) return parsed;
      } catch (e) {}
      if (typeof raw === 'string' && raw.indexOf(':') > 0) {
        return raw.split(',').map(seg => {
          var parts = seg.split(':');
          return { key: (parts[0] || '').trim(), title: (parts[1] || parts[0] || '').trim() };
        });
      }
      return [];
    },
    parseSelectDict(raw) {
      if (!raw) return '';
      try {
        JSON.parse(raw);
        return '';
      } catch (e) {
        return raw;
      }
    },
    buildOption(f) {
      return buildAutoCompleteOption(f.SELECTDATA);
    },

    // === 查询/重置 ===
    onQuery() {
      // 同步缓存到 QQRY DataTable
      var dt = this.resolvedPath;
      if (dt) {
        Object.keys(this.queryValues).forEach(k => {
          dt.setValue(k, this.queryValues[k]);
        });
      }
      this.$emit('query', Object.assign({}, this.queryValues));
    },
    onReset() {
      this.fields.forEach(f => {
        var k = this.fieldKey(f);
        if (!k) return;
        this.$set(this.queryValues, k, this.defaultVal(f));
      });
      this.$emit('reset');
    },
  },
};
</script>

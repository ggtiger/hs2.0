<!--
  rs-meta-query-panel-field 查询单字段组件

  两种模式：
  1. 放在 rs-meta-query-panel 内（通过 inject 注入读写能力）
  2. 独立使用（自带 path/value/字段配置）

  用法:
    1. 放在 panel 内:
       <rs-meta-query-panel :path="qqryDt" module-code="LI_M00" :show-buttons="false">
         <rs-meta-query-panel-field field-name="BUSTYPEID" />
       </rs-meta-query-panel>

    2. 独立使用:
       <rs-meta-query-panel-field :path="qqryDt" field-name="BUSTYPEID" module-code="LI_M00" />

    3. 独立使用 + v-model:
       <rs-meta-query-panel-field v-model="keyword" field-name="KEYWORD" :field-config="{ LABELNAME: '关键词', QUERYTYPE: 'text' }" />

    4. 带覆盖:
       <rs-meta-query-panel-field :path="qqryDt" field-name="BUSTYPEID" :override="{ type: 'select', dict: 'D0701' }" />
-->
<template>
  <div class="rr-flex-row" v-if="resolvedFieldConfig">
    <label class="rr-justify" :style="{ width: labelWidth + 'px' }">{{ resolvedLabel }}</label>
    <!-- 按字段名插槽 -->
    <slot v-if="resolvedType === 'slot'" :name="fieldName" :field="resolvedFieldConfig"></slot>
    <input
      v-else-if="resolvedType === 'input' || resolvedType === 'text'"
      type="text"
      class="rr-flex-1"
      :value="val"
      @input="setVal($event.target.value)"
    />
    <textarea
      v-else-if="resolvedType === 'textarea'"
      class="rr-flex-1"
      :value="val"
      @input="setVal($event.target.value)"
    ></textarea>
    <NumberInput
      v-else-if="resolvedType === 'number'"
      class="rr-flex-1"
      :value="val"
      @input="setVal($event)"
    ></NumberInput>
    <DatePicker
      v-else-if="resolvedType === 'datepicker'"
      class="rr-flex-1"
      :value="val"
      @input="setVal($event)"
    ></DatePicker>
    <DateRangePicker
      v-else-if="resolvedType === 'daterange'"
      class="rr-flex-1"
      :value="val"
      @input="setVal($event)"
    ></DateRangePicker>
    <Select
      v-else-if="resolvedType === 'select' && resolvedMode === 'in'"
      class="rr-flex-1"
      :value="val"
      @input="setVal($event)"
      :datas="resolvedDatas"
      :dict="resolvedDict"
      keyName="key"
      titleName="title"
      multiple
    ></Select>
    <Select
      v-else-if="resolvedType === 'select'"
      class="rr-flex-1"
      :value="val"
      @input="setVal($event)"
      :datas="resolvedDatas"
      :dict="resolvedDict"
      keyName="key"
      titleName="title"
    ></Select>
    <AutoComplete
      v-else-if="resolvedType === 'autocomplete'"
      class="rr-flex-1"
      :value="val"
      @input="setVal($event)"
      :option="resolvedOption"
    ></AutoComplete>
    <div v-else-if="resolvedMode === 'range' && resolvedType !== 'daterange'" class="rr-flex-1" style="display:flex;gap:4px;">
      <input type="text" class="rr-flex-1" placeholder="最小" :value="rangeVal('start')" @input="setRangeVal('start', $event.target.value)" />
      <input type="text" class="rr-flex-1" placeholder="最大" :value="rangeVal('end')" @input="setRangeVal('end', $event.target.value)" />
    </div>
    <input
      v-else
      type="text"
      class="rr-flex-1"
      :value="val"
      @input="setVal($event.target.value)"
    />
  </div>
</template>

<script>
import { buildAutoCompleteOption } from '@/utils/selRegistry';

export default {
  name: 'rs-meta-query-panel-field',
  inject: {
    queryPanel: { default: null },
  },
  props: {
    // === 模式1: 放在 panel 内 ===
    // 字段名（在 panel 的 fields 中按 RESFIELDNAME/FIELDNAME 匹配）
    fieldName: { type: String, default: '' },
    // 直接传 scm 字段配置对象（优先于 fieldName 查找）
    fieldConfig: { type: Object, default: null },

    // === 模式2: 独立使用 ===
    // DataTable 对象 / 数组 / 路径名字符串
    path: { type: [Object, String, Array], default: null },
    // v-model 绑定的值（无 path 时用）
    value: { type: [String, Number, Object, Array], default: '' },
    storeName: { type: String, default: '' },
    // 字段配置数组（从中按 fieldName 查找）
    fields: { type: Array, default: null },
    resourceName: { type: String, default: '' },
    moduleCode: { type: String, default: '' },

    // === 公共 ===
    override: { type: Object, default: function() { return {} } },
    labelWidth: { type: Number, default: 60 },
  },
  data() {
    return {
      scmLoaded: false,
      loadedFields: null,
      // 独立模式的本地缓存（daterange/range/in 的结构化值）
      localValue: undefined,
    };
  },
  computed: {
    // 是否独立模式
    isStandalone() {
      return !this.queryPanel;
    },
    // 解析字段配置
    resolvedFieldConfig() {
      // 1. 直接传 fieldConfig 优先
      if (this.fieldConfig) return this.fieldConfig;
      // 2. 从 panel 的 fields 查找
      if (this.queryPanel && this.fieldName) {
        var fields = this.queryPanel.fields || [];
        var self = this;
        return fields.find(function(f) {
          return (f.RESFIELDNAME || f.FIELDNAME) === self.fieldName;
        }) || null;
      }
      // 3. 独立模式：从 fields prop 或 loadedFields 查找
      if (this.fieldName) {
        var allFields = this.fields || this.loadedFields;
        if (!allFields) return null;
        var self2 = this;
        return allFields.find(function(f) {
          return (f.RESFIELDNAME || f.FIELDNAME) === self2.fieldName;
        }) || null;
      }
      return null;
    },
    // 合并 override
    mergedConfig() {
      if (!this.resolvedFieldConfig) return null;
      var f = Object.assign({}, this.resolvedFieldConfig);
      var ov = this.override || {};
      if (ov.type) {
        f.QUERYTYPE = ov.type;
        f.EDITTYPE = ov.type;
      }
      if (ov.mode) f.QUERYMODE = ov.mode;
      if (ov.dict) f.SELECTDATA = ov.dict;
      if (ov.label !== undefined) f.LABELNAME = ov.label;
      if (ov.placeholder) f.PLACEHOLDER = ov.placeholder;
      // 选择器快捷属性
      var selKeys = ['selType', 'apiCode', 'module', 'keyName', 'titleName', 'paramMappings', 'defaultParams'];
      var hasSel = selKeys.some(function(k) { return ov[k] !== undefined });
      if (hasSel) {
        var selCfg = {};
        if (f.SELECTDATA) {
          try { selCfg = JSON.parse(f.SELECTDATA) } catch (e) {}
        }
        selKeys.forEach(function(k) {
          if (ov[k] !== undefined) selCfg[k] = ov[k];
        });
        f.SELECTDATA = JSON.stringify(selCfg);
      }
      return f;
    },
    resolvedLabel() {
      return this.mergedConfig ? (this.mergedConfig.LABELNAME || '') : '';
    },
    resolvedType() {
      if (!this.mergedConfig) return 'input';
      if (this.queryPanel) return this.queryPanel.typeOf(this.mergedConfig);
      return this.mergedConfig.QUERYTYPE || this.mergedConfig.EDITTYPE || 'input';
    },
    resolvedMode() {
      if (!this.mergedConfig) return 'like';
      if (this.queryPanel) return this.queryPanel.modeOf(this.mergedConfig);
      if (this.mergedConfig.QUERYMODE) return this.mergedConfig.QUERYMODE;
      var t = this.resolvedType;
      if (t === 'select' || t === 'datepicker' || t === 'autocomplete' || t === 'number') return 'eq';
      if (t === 'daterange') return 'range';
      return 'like';
    },
    resolvedDatas() {
      if (this.override && this.override.datas) return this.override.datas;
      // dict + items: 从字典筛选部分选项
      if (this.override && this.override.dict && this.override.items) {
        return this.filterDictItems(this.override.dict, this.override.items);
      }
      if (!this.mergedConfig) return [];
      return this.parseSelectDatas(this.mergedConfig.SELECTDATA);
    },
    resolvedDict() {
      // dict + items 时返回空（用 datas 代替）
      if (this.override && this.override.dict && this.override.items) return '';
      if (this.override && this.override.dict) return this.override.dict;
      if (!this.mergedConfig) return '';
      return this.parseSelectDict(this.mergedConfig.SELECTDATA);
    },
    resolvedOption() {
      if (!this.mergedConfig) return {};
      return buildAutoCompleteOption(this.mergedConfig.SELECTDATA);
    },
    // 当前值
    val() {
      // 模式1: 从 panel 读
      if (this.queryPanel && this.mergedConfig) {
        return this.queryPanel.getValue(this.mergedConfig);
      }
      // 模式2: 独立模式
      if (this.isStandalone) {
        // 有 path 时从 DataTable 读
        var dt = this.resolvedPath;
        if (dt && dt.data && dt.data.length > 0) {
          return dt.data[0][this.fieldName];
        }
        // 无 path 用本地缓存
        if (this.localValue !== undefined) return this.localValue;
        return this.defaultVal();
      }
      return '';
    },
    // 独立模式解析 DataTable
    resolvedPath() {
      if (!this.isStandalone) return null;
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
  },
  watch: {
    resourceName: { handler: function() { this.loadScm() }, immediate: true },
    moduleCode: { handler: function() { this.loadScm() }, immediate: true },
    value: {
      handler: function(val) {
        if (this.isStandalone && !this.path) {
          this.localValue = val;
        }
      },
      immediate: true,
    },
  },
  methods: {
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
      if (!this.isStandalone) return;
      var resName = this._resolveResourceName();
      if (!resName) {
        this.scmLoaded = true;
        return;
      }
      this.scmLoaded = false;
      try {
        // eslint-disable-next-line no-restricted-syntax
        await this.$store.dispatch('app/initScms', [resName]);
        var scm = this.$store.state.app.scms[resName];
        if (scm && Array.isArray(scm)) {
          // 查询面板用 QUERYSORT 过滤
          this.loadedFields = scm.filter(function(f) { return +f.QUERYSORT > 0 });
        }
      } catch (e) {
        console.error('[rs-meta-query-panel-field] scm 加载失败:', resName, e);
      } finally {
        this.scmLoaded = true;
      }
    },

    _resolveResourceName() {
      if (this.resourceName) return this.resourceName;
      if (this.moduleCode) {
        var modData = this.$store.state.app.modules[this.moduleCode];
        if (modData && modData.MODPATH) {
          var mpItem = modData.MODPATH.find(function(p) {
            return p.PATHNAME === 'QQRY' || p.PATHNAME === 'QRY';
          });
          if (mpItem) return mpItem.RESOURCENAME;
        }
      }
      return '';
    },

    defaultVal() {
      if (this.resolvedType === 'daterange' || this.resolvedMode === 'range') return { start: '', end: '' };
      if (this.resolvedMode === 'in') return [];
      return '';
    },

    setVal(v) {
      // 模式1: 写入 panel
      if (this.queryPanel && this.mergedConfig) {
        this.queryPanel.setValue(this.mergedConfig, v);
      } else if (this.isStandalone) {
        // 模式2: 有 path 写 DataTable
        var dt = this.resolvedPath;
        if (dt && dt.setValue) {
          dt.setValue(this.fieldName, v);
        } else {
          // 无 path 用本地缓存
          this.localValue = v;
          this.$emit('input', v);
        }
      }
      this.$emit('change', { field: this.fieldName, value: v });
    },

    rangeVal(key) {
      var v = this.val;
      return (v && v[key]) || '';
    },

    setRangeVal(key, value) {
      var v = Object.assign({}, this.val, { [key]: value });
      this.setVal(v);
    },

    // 从字典中按 items 筛选部分选项
    filterDictItems(dictName, items) {
      var dict = (this.$store.state.app.dicts && this.$store.state.app.dicts[dictName]) || {};
      var arr = Array.isArray(items) ? items : [items];
      return arr.map(function(k) {
        return { key: k, title: dict[k] != null ? dict[k] : k };
      });
    },

    parseSelectDatas(raw) {
      if (!raw) return [];
      try {
        var parsed = JSON.parse(raw);
        if (Array.isArray(parsed)) return parsed;
      } catch (e) {}
      if (typeof raw === 'string' && raw.indexOf(':') > 0) {
        return raw.split(',').map(function(seg) {
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
  },
};
</script>

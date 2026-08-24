<!--
  rs-meta-field 单字段编辑组件

  和 rs-meta-form 相同的数据源/字段加载机制，但只渲染 fieldName 指定的单个字段。

  用法:
    1. DataTable对象:  <rs-meta-field :path="$MAIN" field-name="CUSTNAME" module-code="LIB_M07" />
    2. 路径名+store:   <rs-meta-field path="MAIN" store-name="b01/m01" field-name="CUSTNAME" resource-name="VBS_CUST" />
    3. v-model普通值:  <rs-meta-field v-model="name" :field="fieldConfig" />
    4. 简单模式:       <rs-meta-field v-model="name" field-name="CUSTNAME" :fields="allFields" />
    5. 带覆盖:         <rs-meta-field :path="$MAIN" field-name="CUSTNAME" :override="{ readonly: true }" />
-->
<template>
  <div class="rs-meta-field-wrapper" v-if="resolvedField">
    <Form v-if="wrapForm" :mode="mode" :label-width="labelWidth" :label-position="labelPosition" :disabled="disabled">
      <rs-form-cell
        :type="resolvedField.props.type"
        :dict="resolvedField.props.dict"
        :form-item-props="resolvedField.props.formItemProps"
        :cell-props="resolvedField.props.cellProps"
        :cell-on="resolvedField.props.cellOn || {}"
        :value="currentValue"
        :update-fields="resolvedField.props.updateFields"
        :row-data="rowData"
        @input="onInput"
        @update-fields="onUpdateFields"
      />
    </Form>
    <rs-form-cell
      v-else
      :type="resolvedField.props.type"
      :dict="resolvedField.props.dict"
      :form-item-props="resolvedField.props.formItemProps"
      :cell-props="resolvedField.props.cellProps"
      :cell-on="resolvedField.props.cellOn || {}"
      :value="currentValue"
      :update-fields="resolvedField.props.updateFields"
      :row-data="rowData"
      @input="onInput"
      @update-fields="onUpdateFields"
    />
  </div>
  <div v-else-if="!scmLoaded" class="rs-meta-field-loading">加载中...</div>
</template>

<script>
import Gen from '@/utils/gen';
import RsFormCell from './rs-form-cell';

export default {
  name: 'rs-meta-field',
  components: { RsFormCell },
  inject: {
    aiFormStoreName: { default: null },
  },
  // 兜底 provide：HeyUI FormItem 在 beforeDestroy 中调 removeProp，
  // 如果包裹的 Form 先销毁会导致 this.removeProp 为 undefined，这里提供兜底
  provide() {
    return {
      removeProp: function() {},
      updateProp: function() {},
      updateErrorMessage: function() {},
      setConfig: function() {},
      validField: function() {},
    };
  },
  props: {
    // === 数据源（和 rs-meta-form 一致）===
    // DataTable 对象($MAIN) 或 路径名字符串('MAIN') 或数组(mapDateTable getter 返回的 dt.data)
    path: { type: [Object, String, Array], default: null },
    // v-model 绑定的值（普通模式：直接绑定字段值）
    value: { type: [String, Number, Object, Array], default: '' },
    // path 为字符串时的 Vuex 命名空间
    storeName: { type: String, default: '' },

    // === 字段配置（和 rs-meta-form 一致）===
    // 完整字段配置数组（Gen.getFormFields 格式），从中找 fieldName 对应的
    fields: { type: Array, default: null },
    // 按资源名从 scm 加载
    resourceName: { type: String, default: '' },
    // 按模块编码推导 resourceName
    moduleCode: { type: String, default: '' },

    // === 单字段特有 ===
    // 要渲染的字段名（在 fields 数组中按 props.key 匹配）
    fieldName: { type: String, default: '' },
    // 也可直接传单个 field 配置对象（优先于 fields 数组查找）
    field: { type: Object, default: null },

    // === 字段级覆盖（直接针对此字段，不需要按 fieldName 做 key）===
    // { readonly: true, label: '客户名称' }
    override: { type: Object, default: function() { return {} } },

    // === Form 包裹 ===
    // 默认 true 自动包裹 <Form>（独立使用时不报错）
    // 放在已有 <Form> 内时传 :wrap-form="false" 关闭
    wrapForm: { type: Boolean, default: true },
    mode: { type: String, default: 'twocolumn' },
    labelWidth: { type: Number, default: 80 },
    labelPosition: { type: String, default: 'right' },
    disabled: { type: Boolean, default: false },
  },
  data() {
    return {
      scmLoaded: false,
      loadedFields: null,
      adapter: null,
    };
  },
  computed: {
    // 解析 DataTable（和 rs-meta-form 逻辑一致）
    resolvedPath() {
      // 模式1a: DataTable 对象（有 setValue/data 等方法）
      if (this.path && typeof this.path === 'object' && this.path.setValue) {
        console.log('[rs-meta-field] resolvedPath: DataTable 对象', this.path);
        return this.path;
      }
      // 模式1b: 数组（mapDateTable 的 getter 返回的是 dt.data 数组）-> 包装成适配器
      if (Array.isArray(this.path)) {
        console.log('[rs-meta-field] resolvedPath: 数组', this.path);
        return this._wrapArrayPath(this.path);
      }
      // 模式2: 路径名字符串 + storeName
      if (typeof this.path === 'string' && this.path) {
        console.log('[rs-meta-field] resolvedPath: 路径名字符串', this.path);
        var sn = this.storeName || this.aiFormStoreName;
        if (sn) {
          var storeState = this.$store.state[sn];
          if (storeState && storeState.dt) {
            return storeState.dt[this.path] || null;
          }
        }
        return null;
      }
      // 模式3: v-model 普通值模式：创建单字段适配器
      if (this.hasVModel) {
        return this.adapter;
      }
      return null;
    },
    // v-model 模式判断：有 value 且 path 为空
    hasVModel() {
      return !this.path && this.fieldName;
    },
    // 当前字段值
    currentValue() {
      var dt = this.resolvedPath;
      if (!dt) {
        console.log('[rs-meta-field] currentValue: dt 为空, 返回 value=', this.value);
        return this.value;
      }
      if (dt.data && dt.data.length > 0) {
        var val = dt.data[0][this.fieldName];
        console.log('[rs-meta-field] currentValue:', this.fieldName, '=', val, '| dt.data[0]=', JSON.parse(JSON.stringify(dt.data[0])));
        return val;
      }
      console.log('[rs-meta-field] currentValue: dt.data 为空');
      return this.value;
    },
    // 整行数据（autocomplete/treepicker 联动需要）
    rowData() {
      var dt = this.resolvedPath;
      if (!dt) return null;
      if (dt.data && dt.data.length > 0) {
        return dt.data[0];
      }
      return null;
    },
    // 从 fields 数组或 field prop 中找到目标字段配置
    targetField() {
      // 优先用 field prop
      if (this.field) return this.field;
      // 从 fields 数组中按 fieldName 查找
      var allFields = this.fields;
      if (!allFields || !allFields.length) {
        allFields = this.loadedFields;
      }
      if (!allFields || !allFields.length) return null;
      var self = this;
      return allFields.find(function(f) {
        return f.props && f.props.key === self.fieldName;
      }) || null;
    },
    // 合并 override 后的字段配置
    resolvedField() {
      if (!this.targetField) return null;
      return this._applyOverride(this.targetField);
    },
  },
  watch: {
    resourceName: { handler: function() { this.loadScm() }, immediate: true },
    moduleCode: { handler: function() { this.loadScm() }, immediate: true },
    value: {
      handler: function(val) {
        debugger;
        console.log('value', val);
        if (this.hasVModel) {
          this.adapter = this._createAdapter(val);
        }
      },
      immediate: true,
    },
  },
  methods: {
    // 包装数组为 DataTable-like 对象（mapDateTable 的 getter 返回的是 dt.data 数组）
    // setValue 需要通过 Vuex commit 写回 store，保证变更被追踪
    _wrapArrayPath(arr) {
      // 从数组元素读取 _path_（DataTable 行自带路径名标识）
      var pathName = (arr.length > 0 && arr[0]._path_) ? arr[0]._path_ : 'MAIN';
      // 尝试从 store 找到对应的 DataTable（通过数组引用反查）
      var dt = this._findDtByData(arr);
      console.log('[rs-meta-field] _wrapArrayPath: 找到 DataTable', pathName, dt);
      return dt;
    },

    // 从 Vuex store 中所有模块的 dt 里查找 data === arr 的 DataTable
    _findDtByData(arr) {
      var state = this.$store.state;
      for (var key in state) {
        var modState = state[key];
        if (modState && modState.dt) {
          for (var pathName in modState.dt) {
            if (modState.dt[pathName] && modState.dt[pathName].data === arr) {
              console.log('[rs-meta-field] _findDtByData: 找到 DataTable', pathName, modState.dt[pathName]);
              return modState.dt[pathName];
            }
          }
        }
      }
      return null;
    },

    // 创建单字段适配器（v-model 模式）
    _createAdapter(val) {
      var self = this;
      var obj = {};
      obj[this.fieldName] = val;
      return {
        source: val,
        data: [obj],
        scm: '',
        _path_: 'MAIN',
        path: 'MAIN',
        add: function(item) { this.data[0] = item || obj },
        setValue: function(field, value) {
          debugger;
          console.log('setValue', field, value);
          if (field === self.fieldName) {
            obj[field] = value;
            self.$emit('input', value);
          }
        },
        getValue: function(field) {
          debugger;
          console.log('getValue', field);
          return obj[field];
        },
      };
    },

    // 按 resourceName/moduleCode 加载字段配置（和 rs-meta-form 一致）
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
        var scm = this.$store.state.app.scms[resName];
        if (scm && Array.isArray(scm)) {
          this.loadedFields = Gen.getFormFields(scm);
        }
      } catch (e) {
        console.error('[rs-meta-field] scm 加载失败:', resName, e);
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
            return p.PATHNAME === 'MAIN';
          });
          if (mpItem) return mpItem.RESOURCENAME;
        }
      }
      if (this.resolvedPath && this.resolvedPath.scm) {
        return this.resolvedPath.scm;
      }
      return '';
    },

    // 合并 override 到单个字段（复用 rs-meta-form 的 _applyOverrides 逻辑）
    _applyOverride(field) {
      if (!this.override || !Object.keys(this.override).length) {
        return field;
      }
      var merged = JSON.parse(JSON.stringify(field));
      var props = merged.props;
      var formItemProps = props.formItemProps || {};
      var cellProps = props.cellProps || {};
      var ov = this.override;

      if (ov.label !== undefined) formItemProps.label = ov.label;
      if (ov.readonly !== undefined) cellProps.disabled = ov.readonly;
      if (ov.required !== undefined) {
        formItemProps.required = ov.required;
        props.nullable = ov.required ? 0 : 1;
      }
      if (ov.type) props.type = ov.type;
      if (ov.visibleIf !== undefined) props.visibleIf = ov.visibleIf;
      if (ov.placeholder) cellProps.placeholder = ov.placeholder;
      if (ov.single !== undefined) formItemProps.single = ov.single;
      if (ov.dict) {
        props.dict = ov.dict;
        // dict + items: 从字典筛选部分选项，转为 datas（不设 dict 用 datas 渲染）
        if (ov.items) {
          var dictMap = (this.$store.state.app.dicts && this.$store.state.app.dicts[ov.dict]) || {};
          var itemArr = Array.isArray(ov.items) ? ov.items : [ov.items];
          cellProps.datas = itemArr.map(function(k) {
            return { key: k, title: dictMap[k] != null ? dictMap[k] : k };
          });
        } else {
          cellProps.dict = ov.dict;
        }
      }
      if (ov.updateFields) props.updateFields = ov.updateFields;

      // 选择器快捷属性：selType/apiCode/module/keyName/titleName/paramMappings/defaultParams
      // 基于预设改单个属性，无需手写完整 JSON
      var selKeys = ['selType', 'apiCode', 'module', 'keyName', 'titleName', 'parentName', 'paramMappings', 'defaultParams'];
      var hasSelOverride = selKeys.some(function(k) { return ov[k] !== undefined });
      if (hasSelOverride) {
        var selCfg = {};
        // 保留原 selConfig 中的基础配置
        if (cellProps.selConfig) {
          try { selCfg = JSON.parse(cellProps.selConfig) } catch (e) {}
        }
        selKeys.forEach(function(k) {
          if (ov[k] !== undefined) selCfg[k] = ov[k];
        });
        cellProps.selConfig = JSON.stringify(selCfg);
        // 同步 titleName/keyName 给 cellProps（rs-form-cell 解析用）
        if (selCfg.titleName) cellProps.titleName = selCfg.titleName;
        if (selCfg.keyName) cellProps.keyName = selCfg.keyName;
      }

      if (ov.cellProps) {
        Object.keys(ov.cellProps).forEach(function(k) { cellProps[k] = ov.cellProps[k] });
      }
      if (ov.formItemProps) {
        Object.keys(ov.formItemProps).forEach(function(k) { formItemProps[k] = ov.formItemProps[k] });
      }

      props.formItemProps = formItemProps;
      props.cellProps = cellProps;
      return merged;
    },

    onInput(val) {
      console.log('[rs-meta-field] onInput:', this.fieldName, '=', val);
      var dt = this.resolvedPath;
      console.log('[rs-meta-field] onInput dt=', dt, 'has setValue=', !!(dt && dt.setValue));
      if (dt && dt.setValue) {
        dt.setValue(this.fieldName, val);
        console.log('[rs-meta-field] onInput setValue 完成, dt.data[0][' + this.fieldName + ']=', dt.data[0][this.fieldName]);
      } else {
        this.$emit('input', val);
      }
      this.$emit('change', { field: this.fieldName, value: val, source: dt && dt.data ? dt.data[0] : null });
    },
    onUpdateFields(payload) {
      this.$emit('update-fields', payload);
    },
  },
};
</script>

<style scoped>
.rs-meta-field-loading {
  padding: 4px;
  color: #999;
  font-size: 12px;
}
</style>

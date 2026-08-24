<!--
  rs-meta-form 元数据驱动表单组件

  包装 rs-form-edit，支持三种数据源 + 字段级覆盖，不依赖 generic-module 框架。

  用法:
    1. DataTable对象:  <rs-meta-form :path="$MAIN" :fields="fields" />
    2. 路径名+store:   <rs-meta-form path="MAIN" store-name="b01/m01" resource-name="VBS_CUST" />
    3. v-model普通对象: <rs-meta-form v-model="formData" :fields="fields" />
    4. 按moduleCode加载: <rs-meta-form :path="$MAIN" module-code="LIB_M07" />

  字段覆盖:
    :overrides="{ CUSTNAME: { readonly: true, label: '客户名称' } }"
-->
<template>
  <div class="rs-meta-form-wrapper">
    <rs-form-edit
      v-if="resolvedPath && resolvedFields.length"
      ref="formEdit"
      :key="formKey"
      :path="resolvedPath"
      :fields="resolvedFields"
      :mode="mode"
      :label-width="labelWidth"
      :label-position="labelPosition"
      :disabled="disabled"
      :show-error-tip="showErrorTip"
      :valid-on-change="validOnChange"
      :default-values="defaultValues"
      v-on="$listeners"
    >
      <template v-for="slot in Object.keys($slots)" :slot="slot">
        <slot :name="slot" />
      </template>
    </rs-form-edit>
    <div v-else-if="!scmLoaded" class="rs-meta-form-loading">加载中...</div>
    <div v-else-if="!resolvedFields.length" class="rs-meta-form-empty">暂无字段配置</div>
  </div>
</template>

<script>
import Gen from '@/utils/gen';

export default {
  name: 'rs-meta-form',
  inject: {
    // 允许父级注入 storeName（如 Add01 mixin），path 为字符串时自动取用
    aiFormStoreName: { default: null },
  },
  provide() {
    return {
      // rs-form-edit inject：无 ISSHOW 方法时字段恒显 (visibility.js:20)
      visibilityHost: this,
      subTableButtonsMap: {},
      aiFormModuleCode: this.moduleCode || null,
      aiFormStoreName: this.storeName || this.aiFormStoreName || null,
    };
  },
  props: {
    // === 数据源（三选一）===
    // DataTable 对象($MAIN) 或 路径名字符串('MAIN') 或数组(mapDateTable getter 返回的 dt.data)
    path: { type: [Object, String, Array], default: null },
    // v-model 普通对象模式
    value: { type: Object, default: null },
    // path 为字符串时的 Vuex 命名空间（可选，默认从 inject.aiFormStoreName 取）
    storeName: { type: String, default: '' },

    // === 字段配置（优先级: fields > resourceName > moduleCode）===
    // 直接传入字段配置数组（Gen.getFormFields 格式）
    fields: { type: Array, default: null },
    // 按资源名从 store.state.app.scms 加载
    resourceName: { type: String, default: '' },
    // 按模块编码推导 resourceName（从 MODPATH.MAIN 查找）
    moduleCode: { type: String, default: '' },

    // === 字段级覆盖 ===
    // { CUSTNAME: { readonly: true, label: '客户名称' } }
    overrides: { type: Object, default: function() { return {} } },

    // === 透传 rs-form-edit 布局 props ===
    mode: { type: String, default: 'twocolumn' },
    labelWidth: { type: Number, default: 80 },
    labelPosition: { type: String, default: 'right' },
    disabled: { type: Boolean, default: false },
    showErrorTip: { type: Boolean, default: false },
    validOnChange: { type: Boolean, default: true },
    defaultValues: { type: Object, default: function() { return {} } },
  },
  data() {
    return {
      scmLoaded: false,
      loadedFields: null,
      adapter: null,
    };
  },
  computed: {
    // 解析最终传给 rs-form-edit 的 DataTable 对象
    resolvedPath() {
      // 模式1: DataTable 对象直接传入
      // 模式1a: DataTable 对象（有 setValue/data 等方法）
      if (this.path && typeof this.path === 'object' && this.path.setValue) {
        return this.path;
      }
      // 模式1b: 数组（mapDateTable 的 getter 返回的是 dt.data 数组）-> 包装成适配器
      if (Array.isArray(this.path)) {
        return this._wrapArrayPath(this.path);
      }
      // 模式2: 路径名字符串 + storeName
      if (typeof this.path === 'string' && this.path) {
        var sn = this.storeName || this.aiFormStoreName;
        if (sn) {
          var storeState = this.$store.state[sn];
          if (storeState && storeState.dt) {
            return storeState.dt[this.path] || null;
          }
        }
        return null;
      }
      // 模式3: v-model 普通对象 -> 适配器
      if (this.value) {
        return this.adapter;
      }
      return null;
    },
    // 合并字段配置 + overrides
    resolvedFields() {
      var baseFields = this.fields;
      if (!baseFields || !baseFields.length) {
        baseFields = this.loadedFields;
      }
      if (!baseFields || !baseFields.length) return [];
      return this._applyOverrides(baseFields);
    },
    // scm 加载完成后强制 rs-form-edit 重建
    formKey() {
      return this.resolvedFields.length + '_' + this.scmLoaded;
    },
  },
  watch: {
    resourceName: { handler: function() { this.loadScm() }, immediate: true },
    moduleCode: { handler: function() { this.loadScm() }, immediate: true },
    value: {
      handler: function(val) {
        if (val) {
          this.adapter = this._createAdapter(val);
        }
      },
      immediate: true,
    },
  },
  methods: {
    // 创建普通对象的 DataTable 适配器
    // 包装数组为 DataTable-like 对象（mapDateTable 的 getter 返回的是 dt.data 数组）
    _wrapArrayPath(arr) {
      var self = this;
      var pathName = (arr.length > 0 && arr[0]._path_) ? arr[0]._path_ : 'MAIN';
      var dt = this._findDtByData(arr);
      return {
        data: arr,
        scm: dt ? dt.scm : '',
        _path_: pathName,
        path: pathName,
        setValue: function(field, value) {
          if (dt && dt.setValue) {
            dt.setValue(field, value);
          } else if (arr.length > 0) {
            self.$set(arr[0], field, value);
          }
        },
        getValue: function(field) {
          return arr.length > 0 ? arr[0][field] : undefined;
        },
      };
    },

    // 从 Vuex store 中所有模块的 dt 里查找 data === arr 的 DataTable
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

    // 创建普通对象的 DataTable 适配器
    _createAdapter(obj) {
      var self = this;
      if (!obj || typeof obj !== 'object') {
        obj = {};
      }
      var adapter = {
        source: obj,
        data: [obj],
        scm: '',
        _path_: 'MAIN',
        path: 'MAIN',
        add: function(item) {
          adapter.data[0] = item || {};
        },
        setValue: function(field, value) {
          obj[field] = value;
          self.$emit('input', obj);
          self.$emit('change', { field: field, value: value, source: obj });
        },
        getValue: function(field) {
          return obj[field];
        },
        initData: function(data) {
          if (data && data.length > 0) {
            adapter.data[0] = data[0];
          }
        },
        clear: function() {
          Object.keys(obj).forEach(function(k) {
            if (k.indexOf('_') !== 0) delete obj[k];
          });
        },
        isModify: function() { return true },
        getXML: function() { return '' },
      };
      return adapter;
    },

    // 按 resourceName/moduleCode 加载字段配置
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
        console.error('[rs-meta-form] scm 加载失败:', resName, e);
      } finally {
        this.scmLoaded = true;
      }
    },

    // 推导资源名（优先级: resourceName prop > moduleCode 推导 > path.scm）
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

    // 合并字段配置 + overrides
    _applyOverrides(fields) {
      if (!this.overrides || !Object.keys(this.overrides).length) {
        return fields;
      }
      var self = this;
      return fields.map(function(f) {
        var key = f.props && f.props.key;
        var override = self.overrides[key];
        if (!override) return f;

        // 深拷贝避免污染 scm 缓存
        var merged = JSON.parse(JSON.stringify(f));
        var props = merged.props;
        var formItemProps = props.formItemProps || {};
        var cellProps = props.cellProps || {};

        // 快捷属性映射
        if (override.label !== undefined) {
          formItemProps.label = override.label;
        }
        if (override.readonly !== undefined) {
          cellProps.disabled = override.readonly;
        }
        if (override.required !== undefined) {
          formItemProps.required = override.required;
          props.nullable = override.required ? 0 : 1;
        }
        if (override.type) {
          props.type = override.type;
        }
        if (override.visibleIf !== undefined) {
          props.visibleIf = override.visibleIf;
        }
        if (override.placeholder) {
          cellProps.placeholder = override.placeholder;
        }
        if (override.single !== undefined) {
          formItemProps.single = override.single;
        }
        if (override.dict) {
          props.dict = override.dict;
          // dict + items: 从字典筛选部分选项，转为 datas
          if (override.items) {
            var dictMap = (self.$store.state.app.dicts && self.$store.state.app.dicts[override.dict]) || {};
            var itemArr = Array.isArray(override.items) ? override.items : [override.items];
            cellProps.datas = itemArr.map(function(k) {
              return { key: k, title: dictMap[k] != null ? dictMap[k] : k };
            });
          } else {
            cellProps.dict = override.dict;
          }
        }
        if (override.updateFields) {
          props.updateFields = override.updateFields;
        }
        // 选择器快捷属性：selType/apiCode/module/keyName/titleName/paramMappings/defaultParams
        // 基于预设改单个属性，无需手写完整 JSON
        var selKeys = ['selType', 'apiCode', 'module', 'keyName', 'titleName', 'parentName', 'paramMappings', 'defaultParams'];
        var hasSelOverride = selKeys.some(function(k) { return override[k] !== undefined });
        if (hasSelOverride) {
          var selCfg = {};
          if (cellProps.selConfig) {
            try { selCfg = JSON.parse(cellProps.selConfig) } catch (e) {}
          }
          selKeys.forEach(function(k) {
            if (override[k] !== undefined) selCfg[k] = override[k];
          });
          cellProps.selConfig = JSON.stringify(selCfg);
          if (selCfg.titleName) cellProps.titleName = selCfg.titleName;
          if (selCfg.keyName) cellProps.keyName = selCfg.keyName;
        }
        // 允许直接覆盖 cellProps / formItemProps 的任意子属性
        if (override.cellProps) {
          Object.keys(override.cellProps).forEach(function(k) {
            cellProps[k] = override.cellProps[k];
          });
        }
        if (override.formItemProps) {
          Object.keys(override.formItemProps).forEach(function(k) {
            formItemProps[k] = override.formItemProps[k];
          });
        }

        props.formItemProps = formItemProps;
        props.cellProps = cellProps;
        return merged;
      });
    },

    // === 暴露方法 ===
    valid() {
      return this.$refs.formEdit ? this.$refs.formEdit.valid() : { result: true };
    },
    applyFill(fields) {
      if (this.$refs.formEdit) this.$refs.formEdit.applyFill(fields);
    },
    getModel() {
      return this.resolvedPath ? this.resolvedPath.data[0] : null;
    },
    getDataTable() {
      return this.resolvedPath;
    },
  },
};
</script>

<style scoped>
.rs-meta-form-wrapper {
  width: 100%;
}
.rs-meta-form-loading,
.rs-meta-form-empty {
  padding: 20px;
  text-align: center;
  color: #999;
  font-size: 13px;
}
</style>

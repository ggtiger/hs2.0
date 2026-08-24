<template>
  <div v-if="fields.length" class="rs-query-panel" style="padding:10px 0;">
    <Row :space="9">
      <Cell v-for="f in fields" :key="f.ID || (f.RESFIELDNAME || f.FIELDNAME)" width="6">
        <div class="rr-flex-row">
          <label class="rr-justify" style="width:60px">{{ f.LABELNAME }}</label>
          <!-- 按字段名插槽：QUERYTYPE=slot 时业务页可覆盖 -->
          <slot v-if="typeOf(f) === 'slot'" :name="f.RESFIELDNAME || f.FIELDNAME" :field="f"></slot>
          <input
            v-else-if="typeOf(f) === 'input' || typeOf(f) === 'text'"
            type="text"
            class="rr-flex-1"
            :value="val(f)"
            @input="setVal(f, $event.target.value)"
          />
          <textarea
            v-else-if="typeOf(f) === 'textarea'"
            class="rr-flex-1"
            :value="val(f)"
            @input="setVal(f, $event.target.value)"
          ></textarea>
          <NumberInput
            v-else-if="typeOf(f) === 'number'"
            class="rr-flex-1"
            :value="val(f)"
            @input="setVal(f, $event)"
          ></NumberInput>
          <DatePicker
            v-else-if="typeOf(f) === 'datepicker'"
            class="rr-flex-1"
            :value="val(f)"
            @input="setVal(f, $event)"
          ></DatePicker>
          <DateRangePicker
            v-else-if="typeOf(f) === 'daterange'"
            class="rr-flex-1"
            :value="val(f)"
            @input="setVal(f, $event)"
          ></DateRangePicker>
          <!-- QUERYMODE=in 多选下拉 -->
          <Select
            v-else-if="typeOf(f) === 'select' && modeOf(f) === 'in'"
            class="rr-flex-1"
            :value="val(f)"
            @input="setVal(f, $event)"
            :datas="parseSelectDatas(f.SELECTDATA)"
            :dict="parseSelectDict(f.SELECTDATA)"
            keyName="key"
            titleName="title"
            multiple
          ></Select>
          <Select
            v-else-if="typeOf(f) === 'select'"
            class="rr-flex-1"
            :value="val(f)"
            @input="setVal(f, $event)"
            :datas="parseSelectDatas(f.SELECTDATA)"
            :dict="parseSelectDict(f.SELECTDATA)"
            keyName="key"
            titleName="title"
          ></Select>
          <AutoComplete
            v-else-if="typeOf(f) === 'autocomplete'"
            class="rr-flex-1"
            :value="val(f)"
            @input="setVal(f, $event)"
            :option="buildOption(f)"
          ></AutoComplete>
          <!-- QUERYMODE=range 非日期范围：渲染 min/max 输入框 -->
          <div v-else-if="modeOf(f) === 'range' && typeOf(f) !== 'daterange'" class="rr-flex-1" style="display:flex;gap:4px;">
            <input type="text" class="rr-flex-1" placeholder="最小" :value="rangeVal(f, 'start')" @input="setRangeVal(f, 'start', $event.target.value)" />
            <input type="text" class="rr-flex-1" placeholder="最大" :value="rangeVal(f, 'end')" @input="setRangeVal(f, 'end', $event.target.value)" />
          </div>
          <input
            v-else
            type="text"
            class="rr-flex-1"
            :value="val(f)"
            @input="setVal(f, $event.target.value)"
          />
        </div>
      </Cell>
      <Cell width="6" v-if="fields.length" style="float: right;">
        <div style="text-align:right;">
          <Button color="primary" @click="onQuery">查询</Button>
          <Button class="ml5" @click="onReset">重置</Button>
        </div>
      </Cell>
    </Row>
  </div>
</template>

<script>
import { buildAutoCompleteOption } from '@/utils/selRegistry';

export default {
  name: 'rs-query-panel',
  props: {
    // scm 资源名（读取查询字段配置）
    scm: { type: String, default: '' },
    // QQRY DataTable 对象（含 setValue，查询时同步值）
    qqryPath: { type: Object, default: null },
    // 可选：直接传入字段配置数组（优先于 scm 读取）
    fieldsConfig: { type: Array, default: null },
  },
  data() {
    return {
      queryValues: {},
    };
  },
  computed: {
    scmData() {
      if (!this.scm) return [];
      return this.$store.state.app.scms[this.scm] || [];
    },
    fields() {
      const src = this.fieldsConfig || this.scmData;
      if (!src || !src.length) return [];
      return src
        .filter(f => +f.QUERYSORT > 0)
        .sort((a, b) => (+a.QUERYSORT || 0) - (+b.QUERYSORT || 0));
    },
  },
  watch: {
    fields: {
      immediate: true,
      handler(arr) {
        (arr || []).forEach(f => {
          const k = this.fieldKey(f);
          if (!k) return;
          if (this.queryValues[k] === undefined) {
            this.$set(this.queryValues, k, this.defaultVal(f));
          }
        });
      },
    },
  },
  methods: {
    // 类型规则：有 QUERYTYPE 用 QUERYTYPE（查询专属），没设沿用 EDITTYPE（表单类型），默认 input
    typeOf(f) {
      return f.QUERYTYPE || f.EDITTYPE || 'input';
    },
    // 查询匹配方式：QUERYMODE 优先，否则按 typeOf 推导
    modeOf(f) {
      if (f.QUERYMODE) return f.QUERYMODE;
      const t = this.typeOf(f);
      if (t === 'select' || t === 'datepicker' || t === 'autocomplete' || t === 'number') return 'eq';
      if (t === 'daterange') return 'range';
      return 'like';
    },
    // 字段默认值
    defaultVal(f) {
      if (this.typeOf(f) === 'daterange' || this.modeOf(f) === 'range') return { start: '', end: '' };
      if (this.modeOf(f) === 'in') return [];
      return '';
    },
    fieldKey(f) {
      return f.RESFIELDNAME || f.FIELDNAME || '';
    },
    val(f) {
      return this.queryValues[this.fieldKey(f)];
    },
    setVal(f, v) {
      this.$set(this.queryValues, this.fieldKey(f), v);
    },
    // range 模式子值读写
    rangeVal(f, key) {
      const v = this.val(f);
      return (v && v[key]) || '';
    },
    setRangeVal(f, key, value) {
      const v = { ...this.val(f), [key]: value };
      this.setVal(f, v);
    },
    // select 数据源：JSON 数组 / k:v,k:v / 字典名
    parseSelectDatas(raw) {
      if (!raw) return [];
      try {
        const parsed = JSON.parse(raw);
        if (Array.isArray(parsed)) return parsed;
      } catch (e) {}
      if (typeof raw === 'string' && raw.indexOf(':') > 0) {
        return raw.split(',').map(seg => {
          const [k, title] = seg.split(':');
          return { key: (k || '').trim(), title: (title || k || '').trim() };
        });
      }
      return [];
    },
    // select 字典名（raw 是已注册的字典名时）
    parseSelectDict(raw) {
      if (!raw) return '';
      try {
        JSON.parse(raw);
        return '';
      } catch (e) {
        // 非合法 JSON，当作字典名
        return raw;
      }
    },
    buildOption(f) {
      return buildAutoCompleteOption(f.SELECTDATA);
    },
    onQuery() {
      // 同步缓存到 QQRY DataTable（Store03.advQuery 自动收集 FilterParams）
      if (this.qqryPath) {
        Object.keys(this.queryValues).forEach(k => {
          this.qqryPath.setValue(k, this.queryValues[k]);
        });
      }
      this.$emit('query');
    },
    onReset() {
      this.fields.forEach(f => {
        const k = this.fieldKey(f);
        if (!k) return;
        this.$set(this.queryValues, k, this.defaultVal(f));
      });
      this.$emit('reset');
    },
  },
};
</script>

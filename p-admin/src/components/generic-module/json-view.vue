<template>
  <div class="jv">
    <template v-if="isArray">
      <div v-for="(v, i) in data" :key="i" class="jv-row">
        <span class="jv-key" :class="{ clickable: isExpandable(v) }" @click="toggle(i)">
          <span v-if="isExpandable(v)" class="jv-arrow">{{ collapsed[i] ? '▶' : '▼' }}</span>
          <span class="jv-idx">[{{ i }}]</span>
          <span v-if="isExpandable(v) && collapsed[i]" class="jv-brief">{{ brief(v) }}</span>
        </span>
        <json-view v-if="isExpandable(v)" v-show="!collapsed[i]" :data="v" class="jv-child" />
        <span v-else class="jv-val" :class="valType(v)">{{ fmt(v) }}</span>
      </div>
    </template>
    <template v-else-if="isObject">
      <div v-for="(v, k) in data" :key="k" class="jv-row">
        <span class="jv-key" :class="{ clickable: isExpandable(v) }" @click="toggle(k)">
          <span v-if="isExpandable(v)" class="jv-arrow">{{ collapsed[k] ? '▶' : '▼' }}</span>
          <span class="jv-name">{{ k }}</span>
          <span v-if="isExpandable(v) && collapsed[k]" class="jv-brief">{{ brief(v) }}</span>
        </span>
        <json-view v-if="isExpandable(v)" v-show="!collapsed[k]" :data="v" class="jv-child" />
        <span v-else class="jv-val" :class="valType(v)">{{ fmt(v) }}</span>
      </div>
    </template>
    <span v-else class="jv-val" :class="valType(data)">{{ fmt(data) }}</span>
  </div>
</template>
<script>
// 轻量 JSON 树（可折叠）：对象/数组递归展开，叶子值按类型着色
export default {
  name: 'json-view',
  props: {
    data: { type: [Object, Array, String, Number, Boolean], default: null },
  },
  data() {
    return { collapsed: {} };
  },
  computed: {
    isArray() {
      return Array.isArray(this.data);
    },
    isObject() {
      return this.data !== null && typeof this.data === 'object' && !Array.isArray(this.data);
    },
  },
  methods: {
    isExpandable(v) {
      return v !== null && typeof v === 'object';
    },
    toggle(k) {
      this.$set(this.collapsed, k, !this.collapsed[k]);
    },
    brief(v) {
      return Array.isArray(v) ? 'Array(' + v.length + ')' : 'Object(' + Object.keys(v).length + ')';
    },
    valType(v) {
      if (v === null) return 'null';
      return 'jv-' + typeof v;
    },
    fmt(v) {
      if (v === null) return 'null';
      if (typeof v === 'string') return '"' + v + '"';
      return String(v);
    },
  },
};
</script>
<style lang="less" scoped>
.jv {
  font-family: Consolas, Monaco, monospace;
  font-size: 12px;
  line-height: 1.7;
}
.jv-row {
  display: flex;
  align-items: flex-start;
}
.jv-key {
  flex-shrink: 0;
  &.clickable {
    cursor: pointer;
    &:hover .jv-name, &:hover .jv-idx {
      text-decoration: underline;
    }
  }
}
.jv-arrow {
  display: inline-block;
  width: 14px;
  color: #9ea7b4;
  font-size: 10px;
  user-select: none;
}
.jv-name {
  color: #9c3dcf;
}
.jv-idx {
  color: #808695;
}
.jv-brief {
  color: #9ea7b4;
  margin-left: 6px;
  font-size: 11px;
}
.jv-child {
  margin-left: 18px;
  border-left: 1px dotted #dcdee2;
  padding-left: 8px;
}
.jv-val {
  margin-left: 6px;
  word-break: break-all;
  white-space: pre-wrap;
  &.jv-string {
    color: #16a085;
  }
  &.jv-number {
    color: #2d8cf0;
  }
  &.jv-boolean {
    color: #fa8c16;
  }
  &.null {
    color: #c0c4cc;
  }
}
</style>

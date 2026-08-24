<template>
  <div class="sfc-tree-node">
    <div
      class="sfc-tree-node-label"
      :class="{ 'is-selected': isSelected, 'is-folder': node.type === 'folder' }"
      :style="{ paddingLeft: (depth * 14 + 8) + 'px' }"
      @click="handleClick"
    >
      <span v-if="node.type === 'folder'" class="sfc-tree-arrow" :class="{ 'is-expanded': isExpanded }">
        ▶
      </span>
      <span v-else class="sfc-tree-arrow sfc-tree-arrow-placeholder"></span>
      <span class="sfc-tree-icon" v-html="fileIcon"></span>
      <span class="sfc-tree-name">{{ node.name }}</span>
      <span v-if="node.type === 'file'" class="sfc-tree-node-actions" @click.stop="onDeleteClick">
        <i class="h-icon-trash" title="删除"></i>
      </span>
    </div>
    <div v-if="node.type === 'folder' && isExpanded && node.children && node.children.length > 0" class="sfc-tree-children">
      <tree-node
        v-for="child in node.children"
        :key="child.path"
        :node="child"
        :depth="depth + 1"
        :selectedPath="selectedPath"
        :expandedPaths="expandedPaths"
        @select="$emit('select', $event)"
        @toggle="$emit('toggle', $event)"
        @delete-file="$emit('delete-file', $event)"
      ></tree-node>
    </div>
  </div>
</template>
<script>
export default {
  name: 'tree-node',
  props: {
    node: { type: Object, required: true },
    depth: { type: Number, default: 0 },
    selectedPath: { type: String, default: '' },
    expandedPaths: { type: Object, default: function() { return {} } },
  },
  computed: {
    isExpanded() {
      return !!this.expandedPaths[this.node.path];
    },
    isSelected() {
      return this.selectedPath === this.node.path;
    },
    fileIcon() {
      if (this.node.type === 'folder') {
        return this.isExpanded ? '&#128193;' : '&#128193;';
      }
      // 按代码资产类型给图标: C# 脚本 / SQL 模板 / SFC 文件
      if (this.node.fileKind === 'csharp') return '<span style="color:#9b59b6;">C#</span>';
      if (this.node.fileKind === 'sql') return '<span style="color:#16a085;">⌗</span>';
      var ext = this.node.name.split('.').pop().toLowerCase();
      if (ext === 'vue') return '<span style="color:#42b983;">V</span>';
      if (ext === 'js') return '<span style="color:#f7df1e;">J</span>';
      if (ext === 'json') return '<span style="color:#f5a623;">{}</span>';
      return '<span style="color:#888;">&#9679;</span>';
    },
  },
  methods: {
    handleClick() {
      if (this.node.type === 'folder') {
        this.$emit('toggle', this.node);
      } else {
        this.$emit('select', this.node);
      }
    },
    onDeleteClick() {
      this.$emit('delete-file', this.node);
    },
  },
};
</script>
<style lang="less" scoped>
.sfc-tree-node {
  user-select: none;
}
.sfc-tree-node-label {
  display: flex;
  align-items: center;
  padding: 3px 8px 3px 0;
  cursor: pointer;
  white-space: nowrap;
  &:hover {
    background: #2a2d2e;
  }
  &.is-selected {
    background: #094771;
    color: #fff;
  }
}
.sfc-tree-arrow {
  display: inline-block;
  width: 14px;
  font-size: 9px;
  color: #888;
  text-align: center;
  transition: transform 0.15s;
  &.is-expanded {
    transform: rotate(90deg);
  }
}
.sfc-tree-arrow-placeholder {
  width: 14px;
}
.sfc-tree-icon {
  display: inline-block;
  width: 18px;
  text-align: center;
  font-size: 11px;
  font-weight: bold;
  margin-right: 2px;
}
.sfc-tree-name {
  font-size: 13px;
}
.sfc-tree-node-actions {
  margin-left: auto;
  padding: 0 4px;
  color: #888;
  font-size: 12px;
  cursor: pointer;
  visibility: hidden;
  &:hover {
    color: #ed4014;
  }
}
.sfc-tree-node-label:hover .sfc-tree-node-actions {
  visibility: visible;
}
</style>

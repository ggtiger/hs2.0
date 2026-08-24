<template>
  <div class="sfc-file-tree">
    <div class="sfc-file-tree-header">
      <span class="sfc-file-tree-title">文件</span>
      <div class="sfc-file-tree-actions">
        <i class="h-icon-refresh" title="刷新" @click="loadTree"></i>
        <i class="h-icon-plus" title="新建文件" @click="$emit('new-file')"></i>
      </div>
    </div>
    <div class="sfc-file-tree-search">
      <input
        v-model="searchText"
        placeholder="搜索文件..."
        class="sfc-tree-search-input"
        @input="onSearch"
      />
    </div>
    <div class="sfc-file-tree-body">
      <div v-if="loading" class="sfc-tree-loading">
        <span class="sfc-tree-spinner"></span> 加载中...
      </div>
      <div v-else-if="filteredTree.length === 0" class="sfc-tree-empty">
        <span v-if="searchText">无匹配文件</span>
        <span v-else>暂无文件，点击 + 新建</span>
      </div>
      <tree-node
        v-else
        v-for="node in filteredTree"
        :key="node.path"
        :node="node"
        :depth="0"
        :selectedPath="selectedPath"
        :expandedPaths="expandedPaths"
        @select="onSelect"
        @toggle="onToggle"
        @delete-file="onDeleteFile"
      ></tree-node>
    </div>
  </div>
</template>
<script>
import treeNode from './tree-node.vue';
import { Constants } from '../store';
import { loadModuleCodes, deriveAssetDir } from '../code-asset';

export default {
  name: 'sfc-file-tree',
  components: { treeNode },
  props: {
    selectedPath: {
      type: String,
      default: '',
    },
  },
  data() {
    return {
      treeData: [],
      filteredTree: [],
      loading: false,
      searchText: '',
      expandedPaths: {},
    };
  },
  mounted() {
    this.loadTree();
    this.$root.$on('sfc-tree-refresh', this.loadTree);
  },
  beforeDestroy() {
    this.$root.$off('sfc-tree-refresh', this.loadTree);
  },
  methods: {
    async loadTree() {
      this.loading = true;
      try {
        // 统一一个接口取数: RS_M17/A01(F01 参数化, 返回全部类型)
        // 前端按 ASSETTYPE 分三组: VUE/JS 脚本(同组) / API 脚本(C#) / SQL 模板
        var [assetRet, moduleCodes] = await Promise.all([
          this.$callAction({
            action: `${Constants.STORE_NAME}/listAssets`,
            param: {},
            isBusy: false,
          }),
          loadModuleCodes(),
        ]);
        var items = (assetRet && assetRet.Items) || [];
        this.treeData = this.buildUnifiedTree(items, moduleCodes);
        this.filteredTree = this.treeData;
        this.expandDefault();
      } catch (e) {
        console.error('[FileTree] 加载失败:', e);
      } finally {
        this.loading = false;
      }
    },
    // 统一文件树: 三个固定根分组(VUE/JS 脚本 / API 脚本 / SQL 模板)
    // 数据来自一个接口(VSS_CODE_ASSET 全量), 按 ASSETTYPE 客户端分组(vue+js 同组)
    buildUnifiedTree(items, moduleCodes) {
      var tree = [];
      var byType = { vue: [], js: [], csharp: [], sql: [] };
      items.forEach(function(t) {
        var at = (t.ASSETTYPE || '').toLowerCase();
        if (at === 'vue') byType.vue.push(t);
        else if (at === 'js') byType.js.push(t);
        else if (at === 'csharp') byType.csharp.push(t);
        else if (at === 'sql') byType.sql.push(t);
      });
      // VUE/JS 脚本组(vue+js 同一组, 按 MODULEPATH 的树)
      tree.push({
        name: 'VUE/JS 脚本',
        path: '_sfc',
        type: 'folder',
        fileKind: 'sfc',
        children: this.buildTree(byType.vue.concat(byType.js)),
      });
      // API 脚本(C#)组: 按模块目录, 文件显示 .cs 扩展名
      tree.push({
        name: 'API 脚本 (C#)',
        path: '_csharp',
        type: 'folder',
        fileKind: 'csharp',
        children: this.buildAssetDirs(byType.csharp, 'csharp', moduleCodes),
      });
      // SQL 模板组: 按模块目录, 文件显示 .sql 扩展名
      tree.push({
        name: 'SQL 模板',
        path: '_sql',
        type: 'folder',
        fileKind: 'sql',
        children: this.buildAssetDirs(byType.sql, 'sql', moduleCodes),
      });
      return tree;
    },
    /**
     * 将扁平的模板列表构建为树形结构
     * @param {Array} items - 模板记录数组
     */
    buildTree(items) {
      var root = { children: [] };
      var pathMap = {};

      items.forEach(function(item) {
        var path = item.MODULEPATH || '';
        if (!path) return;

        // 标准化路径: 去掉可能的查询参数
        path = path.split('?')[0];

        // 拆分路径段
        var parts = path.split('/');
        var currentPath = '';
        var currentNode = root;

        for (var i = 0; i < parts.length; i++) {
          var part = parts[i];
          if (!part) continue;
          currentPath = currentPath ? currentPath + '/' + part : part;

          if (!pathMap[currentPath]) {
            var isFile = (i === parts.length - 1);
            var node = {
              name: part,
              path: currentPath,
              type: isFile ? 'file' : 'folder',
              children: [],
            };
            if (isFile) {
              node.templateId = item.ID;
              node.templateName = item.NAME;
              node.fileType = item.FILETYPE;
            }
            pathMap[currentPath] = node;
            currentNode.children.push(node);
          }
          currentNode = pathMap[currentPath];
        }
      });

      // 排序: 文件夹在前，文件在后，各自按名称排序
      this.sortTree(root);
      return root.children;
    },
    sortTree(node) {
      var self = this;
      node.children.sort(function(a, b) {
        if (a.type !== b.type) {
          return a.type === 'folder' ? -1 : 1;
        }
        return a.name.localeCompare(b.name);
      });
      node.children.forEach(function(child) {
        if (child.children && child.children.length > 0) {
          self.sortTree(child);
        }
      });
    },
    // 资产按模块目录分组（deriveAssetDir 推导；当前排序: 模块目录字母序, 公共最后）
    // 统一视图 VSS_CODE_ASSET 字段: ID/CODE/NAME
    buildAssetDirs(items, kind, moduleCodes) {
      var ext = kind === 'csharp' ? '.cs' : '.sql';
      var dirMap = {};
      items.forEach(function(item) {
        var code = item.CODE;
        var dir = deriveAssetDir(code, kind, moduleCodes);
        if (!dirMap[dir]) {
          dirMap[dir] = {
            name: dir,
            path: '_' + kind + '/' + dir,
            type: 'folder',
            fileKind: kind,
            children: [],
          };
        }
        dirMap[dir].children.push({
          name: code + ext,
          path: '_' + kind + '/' + code,
          type: 'file',
          fileKind: kind,
          fileType: kind === 'csharp' ? 'CSHARP' : 'SQL',
          templateId: item.ID,
          scriptId: item.ID,
          sqlId: item.ID,
          templateName: item.NAME,
        });
      });
      var dirs = Object.keys(dirMap).map(function(k) { return dirMap[k] });
      dirs.sort(function(a, b) {
        if (a.name === '公共') return 1;
        if (b.name === '公共') return -1;
        return a.name.localeCompare(b.name);
      });
      dirs.forEach(function(d) {
        d.children.sort(function(a, b) { return a.name.localeCompare(b.name) });
      });
      return dirs;
    },
    // 默认展开第一层（根分组）
    expandDefault() {
      var expanded = {};
      this.treeData.forEach(function(node) {
        if (node.children && node.children.length > 0) {
          expanded[node.path] = true;
        }
      });
      this.expandedPaths = expanded;
    },
    onSearch() {
      if (!this.searchText.trim()) {
        this.filteredTree = this.treeData;
        this.expandDefault();
        return;
      }
      var keyword = this.searchText.toLowerCase();
      this.filteredTree = this.filterTree(this.treeData, keyword);
      // 搜索时展开所有匹配目录（否则匹配文件藏在折叠目录里看不到）
      var expanded = {};
      var walk = function(nodes) {
        nodes.forEach(function(n) {
          if (n.type === 'folder') {
            expanded[n.path] = true;
            walk(n.children || []);
          }
        });
      };
      walk(this.filteredTree);
      this.expandedPaths = expanded;
    },
    filterTree(nodes, keyword) {
      var result = [];
      var self = this;
      nodes.forEach(function(node) {
        if (node.type === 'file') {
          // 匹配: 文件名 / 中文名称(templateName) / 路径
          if (node.name.toLowerCase().indexOf(keyword) >= 0 ||
              (node.templateName || '').toLowerCase().indexOf(keyword) >= 0 ||
              (node.path && node.path.toLowerCase().indexOf(keyword) >= 0)) {
            result.push(node);
          }
        } else if (node.children && node.children.length > 0) {
          var filteredChildren = self.filterTree(node.children, keyword);
          if (filteredChildren.length > 0) {
            var cloned = Object.assign({}, node, { children: filteredChildren });
            result.push(cloned);
          }
        }
      });
      return result;
    },
    onSelect(node) {
      if (node.type === 'file') {
        this.$emit('select', node);
      }
    },
    onToggle(node) {
      if (node.type === 'folder') {
        this.$set(this.expandedPaths, node.path, !this.expandedPaths[node.path]);
      }
    },
    onDeleteFile(node) {
      this.$emit('delete-file', node);
    },
  },
};
</script>
<style lang="less" scoped>
.sfc-file-tree {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: #2d2d2d;
  color: #ccc;
  font-size: 13px;
}
.sfc-file-tree-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 12px;
  background: #252526;
  border-bottom: 1px solid #3c3c3c;
  flex-shrink: 0;
}
.sfc-file-tree-title {
  font-weight: bold;
  text-transform: uppercase;
  font-size: 11px;
  color: #888;
  letter-spacing: 0.5px;
}
.sfc-file-tree-actions {
  display: flex;
  gap: 8px;
  i {
    cursor: pointer;
    color: #888;
    font-size: 14px;
    &:hover {
      color: #fff;
    }
  }
}
.sfc-file-tree-search {
  padding: 6px 8px;
  flex-shrink: 0;
}
.sfc-tree-search-input {
  width: 100%;
  background: #3c3c3c;
  border: 1px solid #3c3c3c;
  border-radius: 3px;
  padding: 4px 8px;
  color: #ccc;
  font-size: 12px;
  outline: none;
  box-sizing: border-box;
  &:focus {
    border-color: #0a84ff;
  }
}
.sfc-file-tree-body {
  flex: 1;
  overflow: auto;
  padding: 4px 0;
}
.sfc-tree-loading, .sfc-tree-empty {
  text-align: center;
  padding: 20px 0;
  color: #666;
  font-size: 12px;
}
.sfc-tree-spinner {
  display: inline-block;
  width: 12px;
  height: 12px;
  border: 2px solid #555;
  border-top-color: #0a84ff;
  border-radius: 50%;
  animation: sfc-spin 0.6s linear infinite;
  vertical-align: middle;
}
@keyframes sfc-spin {
  to { transform: rotate(360deg); }
}
</style>

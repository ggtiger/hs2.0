<template>
  <rs-modal ref="modal" v-model="visible" :width="820" @close="onClose">
    <view-dialog :title="title" class="d-width">
      <template slot="body">
        <div class="cfg-sel">
          <!-- 搜索栏 -->
          <div class="cfg-sel-search">
            <AutoComplete
              v-if="mode === 'field'"
              :option="resSearchOption"
              v-model="resObj"
              @change="onResChange"
              type="object"
              placeholder="搜索资源名称（如 VBS_CUST、TBS_EMP）"
              style="flex:1;"
            >
              <template slot="item" slot-scope="{item}">
                <div class="cfg-sel-res-item">
                  <span class="cfg-sel-res-name">{{ item.value.RESOURCENAME }}</span>
                  <span class="cfg-sel-res-type">{{ item.value.RESOURCETYPE }}</span>
                  <span class="cfg-sel-res-table">{{ item.value.TABLENAME }}</span>
                </div>
              </template>
            </AutoComplete>
            <Select
              v-else
              v-model="moduleCode"
              :datas="moduleOptions"
              keyName="MODULECODE"
              titleName="MODULENAME"
              placeholder="搜索模块名称"
              :filterable="true"
              style="flex:1;"
              @change="onModuleChange"
            ></Select>
          </div>
          <!-- 提示 -->
          <div class="cfg-sel-tip">
            <i class="h-icon-info"></i>
            <span>{{ tip }}</span>
          </div>
          <!-- 列表 -->
          <div class="cfg-sel-table-wrap">
            <Table border :datas="filteredList" :height="380" checkbox @select="onTableSelect">
              <!-- 字段模式列 -->
              <template v-if="mode === 'field'">
                <TableItem title="字段名" prop="FIELDNAME" :width="130"></TableItem>
                <TableItem title="标签" prop="LABELNAME" :width="120"></TableItem>
                <TableItem title="编辑类型" :width="90">
                  <template slot-scope="{data}">{{ editTypeLabel(data.EDITTYPE) }}</template>
                </TableItem>
                <TableItem title="SELECTDATA" :width="180">
                  <template slot-scope="{data}">
                    <span :title="data.SELECTDATA" class="cfg-sel-cell">{{ data.SELECTDATA || '-' }}</span>
                  </template>
                </TableItem>
                <TableItem title="更新字段" :width="120">
                  <template slot-scope="{data}">
                    <span :title="data.UPDATEFIELDS" class="cfg-sel-cell">{{ data.UPDATEFIELDS || '-' }}</span>
                  </template>
                </TableItem>
              </template>
              <!-- 按钮模式列 -->
              <template v-else>
                <TableItem title="页面" :width="120">
                  <template slot-scope="{data}">{{ pageNameOf(data.PAGEID) }}</template>
                </TableItem>
                <TableItem title="区域" prop="BTNAREA" :width="80"></TableItem>
                <TableItem title="排序" prop="SORTNO" :width="60" align="center"></TableItem>
                <TableItem title="按钮名称" prop="BTNNAME" :width="110"></TableItem>
                <TableItem title="编码" prop="BTNCODE" :width="90"></TableItem>
                <TableItem title="接口" prop="APICODE" :width="65"></TableItem>
                <TableItem title="动作类型" prop="ACTIONTYPE" :width="85"></TableItem>
                <TableItem title="显隐条件" :width="130">
                  <template slot-scope="{data}">
                    <span :title="data.SHOWCOND" class="cfg-sel-cell">{{ data.SHOWCOND || '-' }}</span>
                  </template>
                </TableItem>
              </template>
            </Table>
            <div v-if="dataList.length === 0 && hasLoaded" class="cfg-sel-empty">暂无可选数据</div>
            <div v-if="!hasLoaded" class="cfg-sel-empty">{{ mode === 'field' ? '请先搜索并选择资源' : '请先选择模块' }}</div>
          </div>
        </div>
      </template>
      <template slot="footer">
        <span v-if="mode === 'button' && selectedList.length > 0" style="float:left;line-height:32px;color:#666;font-size:12px;">
          已选 {{ selectedList.length }} 个按钮
        </span>
        <Button @click="visible = false">取消</Button>
        <Button color="primary" :disabled="!hasSelection" @click="onConfirm">确认</Button>
      </template>
    </view-dialog>
  </rs-modal>
</template>
<script>
import { searchResources, queryUisetFields, queryModules, queryModuleButtonsAndPages } from '@/utils/selRegistry';

export default {
  name: 'config-sel-popup',
  data() {
    return {
      visible: false,
      mode: 'field', // 'field' | 'button'
      resObj: null,
      moduleCode: '',
      moduleOptions: [],
      dataList: [],
      pageList: [],
      sortedList: [],
      // 字段模式：单选
      selected: null,
      // 按钮模式：多选
      selectedList: [],
      hasLoaded: false,
      resSearchOption: {
        loadData: this.searchRes,
        keyName: 'ID',
        titleName: 'RESOURCENAME',
      },
    };
  },
  computed: {
    title() {
      return this.mode === 'field' ? '从其他字段复制配置' : '从其他模块复制按钮';
    },
    tip() {
      if (this.mode === 'field') {
        return '选择字段后点击"确认"，将编辑类型、SELECTDATA、UPDATEFIELDS、QUERYTYPE、QUERYMODE 等配置复制到当前字段';
      }
      return '勾选按钮后点击"确认"，将按钮配置复制到当前页面（按区域、排序排列）';
    },
    hasSelection() {
      if (this.mode === 'field') return !!this.selected;
      return this.selectedList.length > 0;
    },
    filteredList() {
      return this.mode === 'button' ? this.sortedList : this.dataList;
    },
  },
  methods: {
    // 字段模式：打开
    openField() {
      this.mode = 'field';
      this.reset();
      this.visible = true;
    },
    // 按钮模式：打开
    async openButton() {
      this.mode = 'button';
      this.reset();
      this.moduleOptions = await queryModules();
      this.visible = true;
    },
    reset() {
      this.resObj = null;
      this.moduleCode = '';
      this.dataList = [];
      this.pageList = [];
      this.sortedList = [];
      this.selected = null;
      this.selectedList = [];
      this.hasLoaded = false;
    },
    onClose() {
      this.reset();
    },
    // 资源搜索（字段模式）
    async searchRes(INPUT, callback) {
      const items = await searchResources(INPUT);
      callback(items);
    },
    async onResChange({ value }) {
      if (!value || !value.ID) {
        this.dataList = [];
        this.hasLoaded = false;
        return;
      }
      this.selected = null;
      this.dataList = await queryUisetFields(value.ID);
      this.hasLoaded = true;
    },
    // 模块选择（按钮模式）
    async onModuleChange(val) {
      var code = val;
      if (val && typeof val === 'object') {
        code = val.MODULECODE;
      }
      this.moduleCode = code || '';
      if (!code) {
        this.dataList = [];
        this.pageList = [];
        this.hasLoaded = false;
        return;
      }
      this.selectedList = [];
      var result = await queryModuleButtonsAndPages(code);
      this.dataList = result.buttons;
      this.pageList = result.pages;
      // 排序：先页面ID、再区域、再 SORTNO
      var pages = result.pages || [];
      var pageOrder = function(pid) {
        var idx = pages.findIndex(function(x) { return x.ID === pid; });
        return idx >= 0 ? idx : 999999;
      };
      this.sortedList = (result.buttons || []).slice().sort(function(a, b) {
        var pa = pageOrder(a.PAGEID);
        var pb = pageOrder(b.PAGEID);
        if (pa !== pb) return pa - pb;
        var aa = a.BTNAREA || '';
        var ab = b.BTNAREA || '';
        if (aa !== ab) return aa < ab ? -1 : 1;
        return (+a.SORTNO || 0) - (+b.SORTNO || 0);
      });
      this.hasLoaded = true;
    },
    // 根据 PAGEID 查页面名称
    pageNameOf(pageId) {
      if (!pageId) return '-';
      var p = this.pageList.find(function(x) { return x.ID === pageId; });
      return p ? (p.PAGENAME || p.PAGECODE || '-') : '-';
    },
    // 表格多选
    onTableSelect(rows) {
      this.selectedList = rows || [];
    },
    // 编辑类型中文标签
    editTypeLabel(type) {
      var map = {
        input: '文本',
        textarea: '文本域',
        number: '数字',
        select: '下拉',
        checkbox: '勾选',
        date: '日期',
        datetime: '日期时间',
        daterange: '日期范围',
        autocomplete: '自动完成',
        multiautocomplete: '多选自动完成',
        treepicker: '树形选择',
        fileupload: '文件上传',
        imageupload: '图片上传',
        code: '代码',
        action: '动作',
        tableblock: '表格区块',
        label: '标签',
      };
      return map[type] || type || '未设置';
    },
    onConfirm() {
      if (this.mode === 'field') {
        if (!this.selected) return;
        this.$emit('confirm', {
          mode: 'field',
          data: this.selected,
        });
      } else {
        if (this.selectedList.length === 0) return;
        this.$emit('confirm', {
          mode: 'button',
          data: this.selectedList,
        });
      }
      this.visible = false;
    },
  },
};
</script>
<style scoped lang="less">
.cfg-sel {
  padding: 12px 16px;
}
.cfg-sel-search {
  display: flex;
  gap: 10px;
  margin-bottom: 10px;
}
.cfg-sel-tip {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 10px;
  padding: 6px 10px;
  background: #f0f5ff;
  border-radius: 4px;
  font-size: 12px;
  color: #2f54eb;
  i {
    font-size: 14px;
  }
}
.cfg-sel-table-wrap {
  max-height: 400px;
  overflow-y: auto;
  border: 1px solid #e8e8e8;
  border-radius: 4px;
  /deep/ .h-table-row {
    cursor: pointer;
    &:hover {
      background: #f0f5ff;
    }
  }
}
.cfg-sel-cell {
  display: inline-block;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 12px;
  color: #666;
}
.cfg-sel-res-item {
  display: flex;
  align-items: center;
  gap: 8px;
}
.cfg-sel-res-name {
  font-weight: 500;
}
.cfg-sel-res-type {
  font-size: 10px;
  padding: 1px 5px;
  border-radius: 2px;
  background: #e8e8e8;
  color: #666;
}
.cfg-sel-res-table {
  font-size: 11px;
  color: #999;
}
.cfg-sel-empty {
  text-align: center;
  color: #999;
  padding: 30px 0;
  font-size: 13px;
}
</style>

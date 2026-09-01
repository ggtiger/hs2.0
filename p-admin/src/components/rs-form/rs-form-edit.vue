
<template>
  <div class="rs-form-edit-wrapper">
    <!-- 主表单区域 -->
    <Form ref="form" v-bind="$props" v-on="$listeners" :model="model" :rules="rules" class="rs-form-main">
      <template v-for="(field, index) in enrichedFields">
        <template v-if="isFieldVisible(field)">
          <ToolBar
            v-if="field.props.type === 'toolbar'"
            :label="field.props.formItemProps && field.props.formItemProps.label"
            :size="16"
            :key="'tb' + index"
          ></ToolBar>
          <div v-else-if="field.props.type === 'tableblock'" :key="'tblk' + index" class="rs-form-tableblock" :data-subtable="tableBlockSubtable(field)"
            :class="designer ? designerCellClass(field) : null"
            :draggable="designer"
            @click="designer && $emit('designer-cell-click', field)"
            @dragstart="designer && onDesignerDragStart(field, $event)"
            @dragover="designer && onDesignerDragOver(field, $event)"
            @drop="designer && onDesignerDragDrop(field, $event)"
            @dragleave="designer && onDesignerDragLeave(field)"
            @dragend="designer && onDesignerDragEnd()"
          >
            <ToolBar
              :label="field.props.formItemProps && field.props.formItemProps.label"
              :size="16"
            >
              <div slot="right">
                <template v-for="(btn, bi) in tableBlockButtons(field)">
                  <Poptip
                    v-if="btn.show && btn.poptip"
                    :key="'p' + bi"
                    :content="btn.poptip"
                    @confirm="onTableBlockBtn(field, btn.code)"
                  >
                    <Button
                      :color="btn.color || 'primary'"
                      size="s"
                      :icon="btn.icon"
                      v-per="btn.per"
                    >{{btn.label}}</Button>
                  </Poptip>
                  <Button
                    v-else-if="btn.show"
                    :key="'b' + bi"
                    :color="btn.color || 'primary'"
                    size="s"
                    :icon="btn.icon"
                    v-per="btn.per"
                    @click="onTableBlockBtn(field, btn.code)"
                  >{{btn.label}}</Button>
                </template>
              </div>
            </ToolBar>
            <rs-table-edit
              v-if="tableBlockPath(field)"
              border
              :ref="'tb_' + tableBlockSubtable(field)"
              :path="tableBlockPath(field)"
              :datas="tableBlockDatas(field)"
            ></rs-table-edit>
            <div v-else class="rs-form-tableblock-tip">
              表格区块（子表：{{ tableBlockSubtable(field) || '未配置' }}）
            </div>
          </div>
          <rs-form-cell
            v-else
            :value="getValue(field.props.key)"
            @input="setValue($event,field.props.key)"
            @update-fields="onUpdateFields(field, $event)"
            :row-data="model"
            v-bind="field.props"
            v-on="field.on"
            :key="index"
            :class="designer ? designerCellClass(field) : null"
            :draggable="designer"
            @click.native="designer && $emit('designer-cell-click', field)"
            @dragstart.native="designer && onDesignerDragStart(field, $event)"
            @dragover.native="designer && onDesignerDragOver(field, $event)"
            @drop.native="designer && onDesignerDragDrop(field, $event)"
            @dragleave.native="designer && onDesignerDragLeave(field)"
            @dragend.native="designer && onDesignerDragEnd()"
          >
            <slot :name="field.props.key"></slot>
            <template v-if="designer" #designer-tools>
              <slot name="designer-tools" :field="field"></slot>
            </template>
          </rs-form-cell>
        </template>
      </template>
    </Form>
  </div>
</template>
<script>
// store.dispatch('app/initScms', [this.path.scm]);
import RsFormCell from './rs-form-cell';
import RsTableEdit from '@/components/rs-table/rs-table-edit';
import Gen from '@/utils/gen';
import { buildAutoCompleteOption, buildTreePickerOption } from '@/utils/selRegistry';
import { evalVisibility } from '@/utils/visibility';
import heyui from 'heyui';
const edit = {
  name: 'rs-form-edit',
  components: { RsFormCell, RsTableEdit },
  inject: {
    aiFormModuleCode: { default: null },
    aiFormStoreName: { default: null },
    visibilityHost: { default: null },
    subTableButtonsMap: { default: () => ({}) }
  },
  props: {
    top: {
      type: Number,
    },
    topOffset: {
      type: Number,
      default: 0,
    },
    mode: {
      type: String,
      default: 'single', // inline,single,twocolumn
    },
    labelWidth: {
      type: Number,
      default: 80,
    },
    labelPosition: {
      type: String,
      default: 'right', // left,right
    },
    disabled: {
      type: Boolean,
      default: false,
    },
    showErrorTip: {
      type: Boolean,
      default: false,
    },
    validOnChange: {
      type: Boolean,
      default: true,
    },
    path: {
      type: Object,
    },
    // 可选：直接传入字段配置（优先于 path.scm 读取），用于预览等场景
    fields: {
      type: Array,
    },
    // 可选：表单默认值 { 字段名: 值 }，由 applyDefaultValues() 应用
    // 普通字段：仅当当前为空时写入主表；multiautocomplete 子表模式：仅当子表为空时重建子表
    defaultValues: {
      type: Object,
      default: () => ({}),
    },
    // === 设计器模式 ===
    // 开启后每个 cell 可拖拽、显示工具条 slot，并 emit designer-* 事件给父组件
    designer: { type: Boolean, default: false },
    designerActiveKey: { type: String, default: '' },
    designerDragKey: { type: String, default: '' },
    designerDragOver: { type: Object, default: () => ({ key: '', pos: '' }) },
    // 字段级覆盖: { CUSTNAME: { readonly: true, label: '客户名称' } }
    // 合并到 fields 中对应字段的 props，不整体替换 fields
    overrides: { type: Object, default: () => ({}) },
  },
  data() {
    return {
      innerFields: [],
      rules: [],
      showAiPanel: false,
    };
  },
  computed: {
    aiModuleCode() {
      return this.aiFormModuleCode;
    },
    model() {
      if (this.path.data.length === 0) {
        this.path.add({});
      }
      return this.path.data[0];
    },
    // 收集所有表单数据（主表 + 子表）
    allFormData() {
      const result = {};
      // 主表数据
      if (this.model) {
        Object.assign(result, this.model);
      }
      // 从 MODPATHREF 递归获取所有子表数据
      const moduleConfig = this.aiFormModuleCode && this.$store.state.app && this.$store.state.app.modules ?
        this.$store.state.app.modules[this.aiFormModuleCode] : null;
      if (moduleConfig && moduleConfig.MODPATHREF && this.aiFormStoreName && this.$store.state[this.aiFormStoreName]) {
        const mod = this.$store.state[this.aiFormStoreName];
        if (mod && mod.dt) {
          // 递归获取子表
          const visited = new Set();
          const getSubTables = (parentPath) => {
            moduleConfig.MODPATHREF.forEach(ref => {
              if (ref.PATHNAMEA === parentPath && !visited.has(ref.PATHNAMEB)) {
                visited.add(ref.PATHNAMEB);
                const path = ref.PATHNAMEB;
                const dt = mod.dt[path];
                if (dt && dt.data && dt.data.length > 0) {
                  result['__subtable_' + path] = dt.data.map(row => {
                    const cleanRow = {};
                    Object.keys(row).forEach(k => {
                      if (!k.startsWith('_')) cleanRow[k] = row[k];
                    });
                    return cleanRow;
                  });
                }
                // 递归查找子表的子表
                getSubTables(path);
              }
            });
          };
          // 从主表开始递归
          getSubTables(this.path._path_ || 'MAIN');
        }
      }
      return result;
    },
    // 实际使用的字段：外部传入优先，否则用 scm 读出的内部缓存
    actualFields() {
      return this.fields && this.fields.length ? this.fields : this.innerFields;
    },
    // 为 autocomplete/treepicker/multiautocomplete 字段注入真正的 option（含 loadData 函数）
    // selType/keyName/titleName 等由 gen.js 从 SELECTDATA 解析后放入 cellProps
    enrichedFields() {
      return (this.actualFields || []).map(f => {
        const t = f.props && f.props.type;
        const cp = f.props.cellProps || {};
        // --- sel 类字段：autocomplete/treepicker/multiautocomplete ---
        if (t === 'autocomplete' || t === 'treepicker' || t === 'multiautocomplete') {
          const selConfig = cp.selConfig;
          if (!selConfig) return f;
          let option;
          if (t === 'treepicker') {
            option = buildTreePickerOption(selConfig);
          } else {
            option = buildAutoCompleteOption(selConfig);
          }
          // autocomplete/multiautocomplete：按 paramMappings 注入表单字段值
          if ((t === 'autocomplete' || t === 'multiautocomplete') && option.paramMappings) {
            const self = this;
            const origLoad = option.loadData;
            const mappings = option.paramMappings.split(';').map(s => s.trim()).filter(Boolean).map(seg => {
              const [local, remote] = seg.split(',').map(x => (x || '').trim());
              return { local, remote };
            });
            option.loadData = function(INPUT, callback) {
              const extra = {};
              const model = self.model || {};
              mappings.forEach(m => {
                if (m.local && m.remote && model[m.local] !== undefined && model[m.local] !== null && model[m.local] !== '') {
                  extra[m.remote] = model[m.local];
                }
              });
              origLoad(INPUT, callback, extra);
            };
          }
          // 不修改原对象，避免污染 scm 缓存
          const newCellProps = Object.assign({}, cp, { option });
          // multiautocomplete 子表模式：注入子表读写访问器（cell 据此显示/同步子表行）
          if (t === 'multiautocomplete' && cp.multSelConfig && cp.multSelConfig.mode === 'subtable') {
            newCellProps.subtableAccessor = this._buildSubtableAccessor(cp.multSelConfig);
          }
          const newProps = Object.assign({}, f.props, { cellProps: newCellProps });
          return Object.assign({}, f, { props: newProps });
        }
        // --- 上传子表模式：fileupload/imageupload mode=subtable ---
        if ((t === 'fileupload' || t === 'imageupload') && cp.uploadSubtableConfig) {
          const newCellProps = Object.assign({}, cp, {
            subtableAccessor: this._buildSubtableAccessor(cp.uploadSubtableConfig),
          });
          const newProps = Object.assign({}, f.props, { cellProps: newCellProps });
          return Object.assign({}, f, { props: newProps });
        }
        return f;
      });
    },
  },
  methods: {
    // === 设计器模式：cell class 与拖拽事件转发 ===
    // 父组件通过 designerActiveKey/designerDragKey/designerDragOver 控制视觉状态
    designerCellClass(field) {
      const key = field.props && field.props.key;
      return {
        'uis-form-designer-cell': true,
        active: key === this.designerActiveKey,
        'form-dragging': key === this.designerDragKey,
        'form-drag-over-before': this.designerDragOver.key === key && this.designerDragOver.pos === 'before',
        'form-drag-over-after': this.designerDragOver.key === key && this.designerDragOver.pos === 'after',
      };
    },
    onDesignerDragStart(field, e) {
      try { e.dataTransfer.effectAllowed = 'move'; } catch (err) {}
      this.$emit('designer-cell-dragstart', { field, event: e });
    },
    onDesignerDragOver(field, e) {
      e.preventDefault();
      // 左右判定：鼠标在组件左半=before(前面)，右半=after(后面)
      const rect = e.currentTarget.getBoundingClientRect();
      const midX = rect.left + rect.width / 2;
      const pos = e.clientX < midX ? 'before' : 'after';
      this.$emit('designer-cell-dragover', { field, pos, event: e });
    },
    onDesignerDragDrop(field, e) {
      e.preventDefault();
      this.$emit('designer-cell-drop', { field, event: e });
    },
    onDesignerDragLeave(field) {
      this.$emit('designer-cell-dragleave', { field });
    },
    onDesignerDragEnd() {
      this.$emit('designer-cell-dragend');
    },
    // 构建 multiautocomplete 子表模式的读写访问器
    // cfg: { subtable, subMappings } —— subMappings 形如 "ACCEPTID,ID;ACCEPTCODE,BILLCODE"（同 UPDATEFIELDS）
    _buildSubtableAccessor(cfg) {
      const self = this;
      const subtablePath = cfg.subtable;
      const mappings = (cfg.subMappings || '').split(';').map(s => s.trim()).filter(Boolean).map(seg => {
        const [sub, remote] = seg.split(',').map(x => (x || '').trim());
        return { sub, remote };
      });
      const getTable = () => {
        const ns = self.aiFormStoreName;
        const st = ns && self.$store.state[ns];
        return st && st.dt ? st.dt[subtablePath] : null;
      };
      return {
        mappings,
        getData() {
          const tb = getTable();
          return tb && tb.data ? tb.data.slice() : [];
        },
        // 由选中的远程对象数组重建子表（clear + add），保证子表=当前选中集合
        rebuild(items) {
          const tb = getTable();
          if (!tb) return;
          tb.clear();
          (items || []).forEach(it => {
            const row = {};
            mappings.forEach(m => { if (it && it[m.remote] !== undefined) row[m.sub] = it[m.remote]; });
            tb.add(row);
          });
        },
      };
    },
    // 应用 defaultValues：通用表单默认值机制
    // 普通字段仅当为空时写入；multiautocomplete 子表模式仅当子表为空时重建
    applyDefaultValues() {
      const dv = this.defaultValues;
      if (!dv || !Object.keys(dv).length) return;
      const fields = this.actualFields || [];
      const model = this.model;
      if (!model) return;
      Object.keys(dv).forEach(key => {
        const f = fields.find(ff => ff.props && ff.props.key === key);
        const t = f && f.props.type;
        const cp = (f && f.props.cellProps) || {};
        const val = dv[key];
        if (t === 'multiautocomplete' && cp.multSelConfig && cp.multSelConfig.mode === 'subtable') {
          const acc = this._buildSubtableAccessor(cp.multSelConfig);
          if (acc.getData().length) return; // 已有数据不覆盖
          acc.rebuild(val || []);
          return;
        }
        if (model[key] === undefined || model[key] === null || model[key] === '') {
          this.$set(model, key, val);
          this.path.setValue(key, val);
        }
      });
    },
    // 把 AI 按钮 portal 到对话框标题栏(.h-panel-bar)右侧。
    // 结构：.h-panel > .h-panel-bar(标题) + .h-panel-body(表单在此) + footer
    // .h-panel-bar 不是表单的祖先，而是 .h-panel 的子元素，所以先找 .h-panel 祖先，
    // 再在其内部 querySelector 找 .h-panel-bar。标题栏 flex space-between，按钮推到最右。
    _portalAiBtn() {
      const tryPortal = (retries) => {
        const btn = this.$refs.aiBtn;
        if (!btn) {
          if (retries > 0) setTimeout(() => tryPortal(retries - 1), 100);
          return;
        }
        // 向上找 .h-panel 祖先
        let panelEl = this.$el;
        while (panelEl && panelEl.parentElement) {
          panelEl = panelEl.parentElement;
          if (panelEl.classList && panelEl.classList.contains('h-panel')) break;
        }
        if (!panelEl || !panelEl.classList.contains('h-panel')) {
          if (retries > 0) setTimeout(() => tryPortal(retries - 1), 100);
          return;
        }
        // 在 .h-panel 内部找标题栏
        const barEl = panelEl.querySelector('.h-panel-bar');
        if (!barEl) {
          if (retries > 0) setTimeout(() => tryPortal(retries - 1), 100);
          return;
        }
        // 清理 barEl 内残留的旧 AI 按钮（Vue 重渲染可能重建并重复 portal）
        const stale = barEl.querySelectorAll('.rs-ai-fill-btn');
        stale.forEach(function(el) { if (el !== btn) el.remove(); });
        if (btn.parentNode !== barEl) {
          barEl.appendChild(btn);
        }
      };
      this.$nextTick(() => tryPortal(10));
    },
    getValue(field) {
      return this.model[field];
    },
    setValue(v, field) {
      this.path.setValue(field, v);
    },
    // === tableblock 支持：从当前表单 store 取子表 dt / data ===
    _tableBlockDt(field) {
      const cp = field.props && field.props.cellProps;
      const cfg = cp && cp.tableBlockConfig;
      if (!cfg) return null;
      const ns = this.aiFormStoreName;
      const st = ns && this.$store.state[ns];
      return st && st.dt ? st.dt[cfg.subtable] : null;
    },
    tableBlockSubtable(field) {
      const cp = field.props && field.props.cellProps;
      return cp && cp.tableBlockConfig ? cp.tableBlockConfig.subtable : '';
    },
    tableBlockPath(field) {
      const dt = this._tableBlockDt(field);
      return dt || null;
    },
    tableBlockDatas(field) {
      const dt = this._tableBlockDt(field);
      return dt ? dt.data : [];
    },
    // 工具栏按钮：默认增删移4个 + 配置的自定义按钮
    tableBlockButtons(field) {
      const cp = field.props && field.props.cellProps;
      const cfg = cp && cp.tableBlockConfig;
      if (!cfg) return [];
      const sub = cfg.subtable;
      // 优先用 tss_module_button 配置的子表按钮(BTNAREA=子表路径), 无配置则默认增删移兜底
      const configured = (this.subTableButtonsMap && this.subTableButtonsMap[sub]) || [];
      // 配置按钮 BTNCODE → 内置 code 映射: subAdd/subRemove/subUp/subDown → add/remove/up/down
      // 命中映射的按钮复用 onTableBlockBtn 默认处理(新增行/删除行/上下移), 无需走 custom 分支
      const SUB_CODE_MAP = { subAdd: 'add', subRemove: 'remove', subUp: 'up', subDown: 'down' };
      if (configured.length > 0) {
        return configured.map(b => ({
          code: SUB_CODE_MAP[b.BTNCODE] || b.BTNCODE || 'custom',
          label: b.BTNNAME || '按钮',
          icon: b.ICON || '',
          color: b.COLOR || '',
          poptip: b.INTERACTTYPE === 'poptip' ? (b.POPTIPTEXT || '确定执行？') : '',
          show: true,
          per: b.PERMCODE || '',
          actionCode: b.ACTIONCODE || '',
          btnType: b.BTNTYPE || 'custom'
        }));
      }
      return [
        { code: 'add', label: '新增', icon: 'h-icon-plus', show: true, per: '' },
        { code: 'remove', label: '移除', icon: 'h-icon-minus', show: true, per: '' },
        { code: 'up', label: '上移', icon: 'h-icon-top', show: true, per: '' },
        { code: 'down', label: '下移', icon: 'h-icon-down', show: true, per: '' },
      ];
    },
    // 显隐判断：未配 visibleIf 时默认 ISSHOW+字段名；method 传入 { row: model, key, path }
    isFieldVisible(field) {
      if (!this.visibilityHost) return true;
      let visIf = field.props && field.props.visibleIf;
      const key = (field.props && field.props.key) || '';
      if (!visIf) visIf = 'ISSHOW' + key;
      return evalVisibility(this.visibilityHost, visIf, {
        row: this.model,
        key,
        path: this.path && (this.path._path_ || this.path.path),
      });
    },
    // ACTIONCODE 标准规则：解析"标签:功能点编码,模块编码/功能点编码"，跳转功能点页面
    _handleActionCode(actionCode) {
      if (!actionCode) return false;
      // actionCode 可能是单条或"标签:code,per"格式；走与列表 action 一致的 listAction 派发
      // 这里向上 emit action-click，由业务页/list-t01 处理（复用现有路由跳转逻辑）
      const ad = actionCode.split(':');
      const codePart = ad[1] || ad[0];
      const code = codePart.split(',')[0];
      this.$emit('action-click', code);
      return true;
    },
    onTableBlockBtn(field, code) {
      const ns = this.aiFormStoreName;
      const ref = this.$refs['tb_' + this.tableBlockSubtable(field)];
      const table = Array.isArray(ref) ? ref[0] : ref;
      const sub = this.tableBlockSubtable(field);
      // 从 subTableButtonsMap 查原始按钮对象(含 EXTPARAM), 用于触发 beforeAction/afterAction 钩子
      const list = (this.subTableButtonsMap && this.subTableButtonsMap[sub]) || [];
      // SUB_CODE_MAP 反向查找: 内置 code(add/remove/up/down) → 配置的 BTNCODE(subAdd/subRemove/...)
      const REV_SUB_CODE = { add: 'subAdd', remove: 'subRemove', up: 'subUp', down: 'subDown' };
      const btnCode = REV_SUB_CODE[code] || code;
      const btn = list.find(b => (b.BTNCODE || 'custom') === btnCode) || list.find(b => (b.BTNCODE || 'custom') === code);
      const context = { field: field, subtable: sub, code: code, table: table, btn: btn };
      // beforeAction 钩子(通过 visibilityHost 即 generic-form 实例调用, 那里加载了扩展 JS)
      if (btn && this.visibilityHost && typeof this.visibilityHost.callBtnHook === 'function') {
        if (this.visibilityHost.callBtnHook('beforeAction', btn, context) === false) return;
      }
      if (code === 'add') {
        this.$store.commit(`${ns}/ADD`, { path: sub });
      } else if (code === 'remove') {
        if (table && table.currentRow && table.currentRow !== -1) {
          this.$store.commit(`${ns}/DEL`, { path: sub, item: table.currentRow });
        }
      } else if (code === 'up' || code === 'down') {
        const dt = this._tableBlockDt(field);
        if (!dt || !table || !table.currentRow) return;
        const fn = code === 'up' ? dt.upItem : dt.downItem;
        fn.call(dt, { item: table.currentRow });
        this.$nextTick(() => {
          const idx = dt.data.indexOf(table.currentRow);
          if (idx >= 0 && table.clickCurrentRow) table.clickCurrentRow(idx);
        });
      } else {
        // 自定义按钮: 检查 EXTPARAM.action 是否为 openSelector/openForm
        var ext = {};
        if (btn && btn.EXTPARAM) {
          try { ext = JSON.parse(btn.EXTPARAM) } catch (e) { ext = {} }
        }
        var isSelector = ext.action === 'openSelector' || (!ext.action && (ext.selectPageCode || ext.selectModule));
        if (isSelector && this.visibilityHost && typeof this.visibilityHost.openSelector === 'function') {
          var selOpts = {
            moduleCode: ext.selectModule || '',
            pageCode: ext.selectPageCode || '',
            mode: ext.selectMode || 'single',
            target: ext.selectTarget || sub,
            fieldMap: ext.fieldMap || '',
            width: ext.selectWidth || 900,
            title: btn.BTNNAME || '选择数据'
          };
          this.visibilityHost.openSelector(selOpts);
        } else if (btn && btn.ACTIONCODE && this._handleActionCode(btn.ACTIONCODE)) {
          // actionCode 已处理
        } else {
          this.$emit('tableblock-action', { subtable: sub, code, table });
        }
      }
      // afterAction 钩子
      if (btn && this.visibilityHost && typeof this.visibilityHost.callBtnHook === 'function') {
        this.visibilityHost.callBtnHook('afterAction', btn, context);
      }
    },
    // 打开全局抽屉并切到 AI 填报智能体（合并入口后，AI 填报走全局抽屉，不再用内嵌面板）
    toggleAiPanel() {
      // eslint-disable-next-line no-restricted-syntax
      this.$store.dispatch('assistant/openWithAgent', 'form');
    },
    // 关闭 AI 面板（对话框关闭时调用）
    closeAiPanel() {
      this.showAiPanel = false;
    },
    // AI 填报：把 {字段名:值} 批量写入当前表单主表。
    // 关键：模型 data[0] 初始字段不全，新增属性须用 $set 触发 Vue 响应式（否则 cell 不刷新）；
    // 同时调 setValue 记录变更（保证保存 XML 包含）。
    // 支持字段类型转换：checkbox→1/0, number→parseFloat, select→查字典, autocomplete→触发updateFields联动
    applyFill(fields) {
      console.log('[rs-form-edit] applyFill called with fields:', fields);
      if (!fields || !this.path) {
        console.log('[rs-form-edit] applyFill early return: fields=', fields, 'path=', this.path);
        return;
      }
      const model = this.path.data && this.path.data[0];
      if (!model) {
        console.log('[rs-form-edit] applyFill early return: model is null');
        return;
      }
      const converted = this._convertFields(fields, this.innerFields);
      console.log('[rs-form-edit] converted fields:', converted);
      Object.keys(converted).forEach(key => {
        const v = converted[key];
        this.$set(model, key, v); // 响应式：让 cell 的 :value 刷新
        this.path.setValue(key, v); // 记录变更：保证保存
      });
      // 第二轮：处理 autocomplete/treepicker 的 updateFields 联动
      // LLM 可能同时给了 ID 字段和显示名字段（如 CUSTID='xxx', CUSTNAME='ABC'）
      // 需要按 updateFields 映射，把远程字段值写回本地字段
      this._applyAutocompleteLinkage(fields);
      // 强制刷新视图，确保数据变化被 Vue 检测到
      this.$forceUpdate();
    },
    // 子表行字段转换：给定字段值对象和字段定义，返回转换后的字段值对象
    // 供外部（add.vue）调用来处理子表行的字段转换
    convertSubTableRow(fields, fieldDefs) {
      return this._convertFields(fields, fieldDefs);
    },
    // 内部：根据字段类型做值转换（主表/子表共用）
    // fields: {字段名:原始值}
    // fieldDefs: rs-form-edit 的 innerFields 格式数组
    // 返回: {字段名:转换后的值}
    _convertFields(fields, fieldDefs) {
      const result = {};
      if (!fields) return result;
      Object.keys(fields).forEach(k => {
        const key = (k || '').toUpperCase();
        let v = fields[k];
        // 根据字段类型做值转换
        const fieldDef = (fieldDefs || []).find(f => (f.props && f.props.key) === key);
        if (fieldDef) {
          const type = fieldDef.props && fieldDef.props.type;
          // select 的 dict/datas 在 cellProps 里（gen.js 设置），autocomplete 的 selConfig 也在 cellProps 里
          const cellProps = (fieldDef.props && fieldDef.props.cellProps) || {};
          if (type === 'checkbox') {
            v = v === true || v === 'true' || v === 1 || v === '1' ? 1 : 0;
          } else if (type === 'number') {
            const n = parseFloat(v);
            v = isNaN(n) ? v : n;
          } else if (type === 'select') {
            // select：尝试用 datas（JSON数组）或 dict（字典名）解析
            const datas = cellProps.datas;
            const dict = cellProps.dict;
            if (datas) {
              v = this._resolveSelectValue(v, datas);
            } else if (dict) {
              v = this._resolveSelectValue(v, dict);
            }
          }
        }
        result[key] = v;
      });
      return result;
    },
    // select 字典值解析：如果值是字符串，尝试匹配字典 title→key
    _resolveSelectValue(v, dictOrDatas) {
      if (v == null || v === '') return v;
      const strV = String(v);
      // dictOrDatas 可能是：1) JSON数组字符串 2) 字典名字符串 3) 已解析的数组
      let dictData = dictOrDatas;
      if (typeof dictOrDatas === 'string') {
        try {
          dictData = JSON.parse(dictOrDatas);
        } catch (e) {
          // 是字典名，从 heyui 取
          dictData = null;
          try {
            dictData = heyui.getDict(dictOrDatas);
          } catch (e2) {}
        }
      }
      if (!dictData) return v;
      // dictData 可能是数组 [{key,title}] 或对象 {key:title}
      if (Array.isArray(dictData)) {
        const found = dictData.find(d => d.title === strV || d.key === strV);
        if (found) return found.key;
      } else if (typeof dictData === 'object') {
        for (const dk in dictData) {
          if (dictData[dk] === strV) return dk;
        }
        if (dictData[strV] !== undefined) return strV;
      }
      return v;
    },
    // autocomplete/treepicker 联动：LLM 给了 {CUSTID:'xxx', CUSTNAME:'ABC'}
    // 需要按 updateFields 映射，构造 payload 触发 onUpdateFields
    _applyAutocompleteLinkage(fields) {
      if (!fields || !this.path) return;
      const model = this.path.data && this.path.data[0];
      if (!model) return;
      (this.innerFields || []).forEach(fieldDef => {
        const type = fieldDef.props && fieldDef.props.type;
        const updateFields = fieldDef.props && fieldDef.props.updateFields;
        if ((type !== 'autocomplete' && type !== 'treepicker') || !updateFields) return;
        const mappings = (updateFields || '').split(';')
          .filter(seg => seg && seg.indexOf(',') >= 0)
          .map(seg => {
            const [local, remote] = seg.split(',');
            return { local: (local || '').trim(), remote: (remote || '').trim() };
          });
        if (!mappings.length) return;
        // 检查 LLM 是否给了这个字段组的所有值
        const payload = {};
        let hasAny = false;
        mappings.forEach(m => {
          const localKey = m.local.toUpperCase();
          const remoteKey = m.remote.toUpperCase();
          if (fields[localKey] !== undefined) {
            payload[m.local] = fields[localKey];
            hasAny = true;
          } else if (fields[remoteKey] !== undefined) {
            payload[m.local] = fields[remoteKey];
            hasAny = true;
          }
        });
        if (hasAny) {
          // 直接写 model 和 path
          Object.keys(payload).forEach(k => {
            const uk = k.toUpperCase();
            this.$set(model, uk, payload[k]);
            this.path.setValue(uk, payload[k]);
          });
        }
      });
    },
    // autocomplete/treepicker/fileupload/imageupload 选中后联动写多个字段
    // payload: { 本地字段1: 值1, 本地字段2: 值2 }
    onUpdateFields(field, payload) {
      if (!payload) return;
      Object.keys(payload).forEach(k => {
        this.path.setValue(k, payload[k]);
      });
    },
    // 子表填报：接收 {path, rows}，通过 Vuex store 找到子表 DataTable 并添加行
    onSubTable({ path, rows }) {
      if (!path || !rows || !rows.length) return;
      // 通过 Vuex store 找到包含子表 DataTable 的模块
      // 策略：遍历所有 store module，找到 dt[path] 存在的模块
      const store = this.$store;
      const state = store.state;
      let subTable = null;
      // 遍历所有 store module
      Object.keys(state).forEach(moduleName => {
        const mod = state[moduleName];
        if (mod && mod.dt && mod.dt[path]) {
          subTable = mod.dt[path];
        }
      });
      // 如果找不到，尝试从当前 path 的 store module 找
      if (!subTable && this.path && this.path._path_) {
        // this.path 是主表 DataTable，它的 _path_ 是 'MAIN'
        // 但 store module 名不知道
      }
      if (!subTable) {
        //  fallback：通过 $emit 让父组件处理
        this.$emit('subtable', { path, rows });
        return;
      }
      // 添加行并填充：直接构造完整行对象传给add，push时属性被Vue响应式化，表格才会显示
      rows.forEach(rowData => {
        const newRowData = {};
        Object.keys(rowData).forEach(k => {
          const key = (k || '').toUpperCase();
          newRowData[key] = rowData[k];
        });
        subTable.add(newRowData);
      });
    },
    // disabled=true 时强制全部只读；disabled=false 时保留 gen.js 的 readonly 设置
    _applyDisabled() {
      if (!this.innerFields) return;
      if (this.disabled) {
        this.innerFields.forEach(f => (f.props.cellProps.disabled = true));
      }
      // disabled=false 时不动 cellProps.disabled，让 gen.js 的 readonly 生效
    },
    // 应用 overrides（字段级属性覆盖）
    // override 属性: label/readonly/required/type/dict/placeholder/single/visibleIf/updateFields
    //                + 选择器快捷属性(selType/apiCode/module/keyName/titleName/paramMappings/defaultParams)
    //                + dict+items (字典筛选项)
    //                + cellProps/formItemProps (任意子属性)
    _applyOverrides(fields) {
      if (!this.overrides || !Object.keys(this.overrides).length) return fields;
      var self = this;
      return fields.map(function(f) {
        var key = f.props && f.props.key;
        var ov = self.overrides[key];
        if (!ov) return f;
        var merged = JSON.parse(JSON.stringify(f));
        var props = merged.props;
        var formItemProps = props.formItemProps || {};
        var cellProps = props.cellProps || {};
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
          if (ov.items) {
            var dictMap = (self.$store.state.app.dicts && self.$store.state.app.dicts[ov.dict]) || {};
            var itemArr = Array.isArray(ov.items) ? ov.items : [ov.items];
            cellProps.datas = itemArr.map(function(k) {
              return { key: k, title: dictMap[k] != null ? dictMap[k] : k };
            });
          } else {
            cellProps.dict = ov.dict;
          }
        }
        if (ov.updateFields) props.updateFields = ov.updateFields;
        // 选择器快捷属性
        var selKeys = ['selType', 'apiCode', 'module', 'keyName', 'titleName', 'parentName', 'paramMappings', 'defaultParams'];
        var hasSel = selKeys.some(function(k) { return ov[k] !== undefined });
        if (hasSel) {
          var selCfg = {};
          if (cellProps.selConfig) {
            try { selCfg = JSON.parse(cellProps.selConfig) } catch (e) {}
          }
          selKeys.forEach(function(k) {
            if (ov[k] !== undefined) selCfg[k] = ov[k];
          });
          cellProps.selConfig = JSON.stringify(selCfg);
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
      });
    },
    valid() {
      return this.$refs.form.valid();
    },
  },
  beforeCreate() {
    try {
    } catch (error) {}
  },
  created() {
    // 优先用外部传入的 fields，否则从 scm 读取
    let fields = this.fields || Gen.getFormFields(this.$store.state.app.scms[this.path && this.path.scm]) || [];
    // 应用 overrides（字段级属性覆盖）
    fields = this._applyOverrides(fields);
    // 预初始化所有字段为响应式属性（Vue 2 无法检测新增属性的变化）
    // 新增记录时 model 是空对象 {}，用户输入字段值时新属性不会被 Vue 追踪
    if (this.model && this.path) {
      var row = this.model;
      var self = this;
      fields.forEach(function(f) {
        if (f.props && f.props.key && row[f.props.key] === undefined) {
          self.$set(row, f.props.key, '');
        }
      });
    }
    // 系统审计字段由后端自动填充(无需用户填写)，不参与必填校验
    // 否则这些只读展示字段永远为空, valid() 静默失败, 保存无任何反应
    const AUDIT_FIELDS = ['CREATEID', 'CREATER', 'CREATETIME', 'MODIFYID', 'MODIFER', 'MODIFYTIME'];
    this.rules = {
      required: fields.filter(i => i.props.nullable !== 1 && i.props.type !== 'toolbar' && AUDIT_FIELDS.indexOf(i.props.key) === -1).map(item => item.props.key),
    };
    this.innerFields = fields;
    this._applyDisabled();
    this.$forceUpdate();
  },
  watch: {
    disabled: {
      handler() {
        this._applyDisabled();
      },
      immediate: true,
    },
    // model 变化时对新行做 $set 预初始化字段，保证字段为响应式属性。
    // 背景：add action 的 INIT 会 initData([]) 重建 dt.data，新行字段不全，
    // Vue 2 无法追踪后增属性；$set 预置后，render 期间 isFieldVisible 读取
    // row.FIELD 会自动建立依赖，值变化时 Vue 自动重渲染，无需 $forceUpdate
    model: {
      handler(newRow) {
        if (newRow && this.path) {
          var fields = this.fields || Gen.getFormFields(this.$store.state.app.scms[this.path && this.path.scm]) || [];
          var self = this;
          fields.forEach(function(f) {
            if (f.props && f.props.key && newRow[f.props.key] === undefined) {
              self.$set(newRow, f.props.key, '');
            }
          });
        }
      },
      deep: true
    },
    // fields prop 变化时重新生成 innerFields
    fields: {
      handler() {
        let fields = this.fields || Gen.getFormFields(this.$store.state.app.scms[this.path && this.path.scm]) || [];
        this.innerFields = this._applyOverrides(fields);
        this.$forceUpdate();
      }
    },
    // overrides 变化时重新应用覆盖
    overrides: {
      handler() {
        this.innerFields = this._applyOverrides(this.innerFields);
        this.$forceUpdate();
      },
      deep: true
    }
  },
  mounted() {
    // 注册当前表单上下文到全局 formContext，并自动激活 AI 填报智能体
    if (this.aiModuleCode) {
      this.$store.commit('formContext/SET', {
        rsFormEdit: this,
        moduleCode: this.aiModuleCode,
        storeName: this.aiFormStoreName,
        active: true
      });
      // eslint-disable-next-line no-restricted-syntax
      this.$store.dispatch('assistant/setAgent', 'form');
    }
  },
  beforeDestroy() {
    if (this.aiModuleCode) {
      this.$store.commit('formContext/CLEAR');
    }
  },
  updated() {
  },
};
export default edit;
</script>
<style scoped>
/* 表单包装器 */
.rs-form-edit-wrapper {
  position: relative;
  width: 100%;
}
/* tableblock 预览占位 */
.rs-form-tableblock-tip {
  padding: 12px;
  text-align: center;
  color: #999;
  background: #fafafa;
  border: 1px dashed #d9d9d9;
  border-radius: 3px;
  font-size: 13px;
}
/* AI 填报按钮（portal 到标题栏右侧，贴合主题：品蓝描边/浅蓝底，激活时实心） */
.rs-ai-fill-btn {
  display: inline-flex;
  align-items: center;
  background: #F0F5FF;
  color: #2F54EB;
  border: 1px solid #ADC6FF;
  padding: 0px 12px;
  border-radius: 6px;
  cursor: pointer;
  font-size: 12px;
  font-weight: 500;
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
  white-space: nowrap;
  user-select: none;
  margin-right: 8px;
}

.rs-ai-fill-btn span {
  white-space: nowrap;
}
.rs-ai-fill-btn:hover {
  background: #2F54EB;
  color: #fff;
  border-color: #2F54EB;
}
/* 激活态（面板展开）：实心品蓝 */
.rs-ai-fill-btn.rs-ai-fill-btn-active {
  background: #2F54EB;
  color: #fff;
  border-color: #2F54EB;
}
</style>

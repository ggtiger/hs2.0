<template>
  <view-dialog :title="title" class="d-width">
    <template slot="body">
      <!-- form-top slot 扩展 -->
      <component
        v-if="slotComponents['form-top']"
        :is="slotComponents['form-top']"
        :host="self"
      ></component>
      <!-- 有表单分组(EDITGROUP)时按分组 tab, 每个分组一个 rs-form-edit(含 tableblock 子表) -->
      <template v-if="hasGroups">
        <Tabs :datas="tabDatas" class-name="h-tabs-card" v-model="activeTab"></Tabs>
        <div v-for="g in formGroups"  :key="g.name" v-show="activeTab === g.name">
          <rs-form-edit
            ref="form"
            :key="'form_' + g.name + '_' + mixinVersion"
            class="rs-flex-col"
            :label-width="80"
            :mode="formMode"
            :path="$MAIN"
            :fields="g.fields"
          >
            <template v-for="(comp, fieldName) in getGroupFieldSlots(g)" :slot="fieldName">
              <component :is="comp" :key="fieldName" :host="self"
                :value="getFieldSlotValue(fieldName)"
                @input="setFieldSlotValue(fieldName, $event)" />
            </template>
          </rs-form-edit>
        </div>
      </template>
      <template v-else>
        <ToolBar label="基本信息" :size="16"></ToolBar>
        <rs-form-edit
          ref="form"
          :key="'form_' + mixinVersion"
          class="rs-flex-col"
          :label-width="80"
          :mode="formMode"
          :path="$MAIN"
        >
          <template v-for="(comp, fieldName) in fieldSlotComponents" :slot="fieldName">
            <component :is="comp" :key="fieldName" :host="self"
              :value="getFieldSlotValue(fieldName)"
              @input="setFieldSlotValue(fieldName, $event)" />
          </template>
        </rs-form-edit>
      </template>
      <!-- form-bottom slot 扩展 -->
      <component
        v-if="slotComponents['form-bottom']"
        :is="slotComponents['form-bottom']"
        :host="self"
      ></component>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <template v-for="btn in visibleButtons">
        <Poptip
          v-if="btn.INTERACTTYPE==='poptip'"
          :key="btn.ID"
          :content="btn.POPTIPTEXT || '确定？'"
          @confirm="handleBtn(btn)"
        >
          <Button class="ml5" v-per="btn.PERMCODE" :icon="btn.ICON" :color="btn.COLOR">{{btn.BTNNAME}}</Button>
        </Poptip>
        <Button
          v-else
          :key="btn.ID"
          class="ml5"
          v-per="btn.PERMCODE"
          :icon="btn.ICON"
          :color="btn.COLOR"
          @click="handleBtn(btn)"
        >{{btn.BTNNAME}}</Button>
      </template>
    </template>
  </view-dialog>
</template>
<script>
import Gen from '@/utils/gen';
import Add01 from '@/mixins/add01';
import Sel01 from '@/mixins/sel01';
import { getGenericStore, applyStoreExtend } from './generic-store';
import { loadCompiledSFC } from '@/sfc-loader';

export default {
  name: 'GenericForm',
  mixins: [Add01, Sel01],
  // 覆盖 generic-module 的 visibilityHost，使 rs-form-edit 的 ISSHOWxxx 方法
  // 在本组件（加载了 SFC 扩展）上查找，而非 generic-module
  provide() {
    return { visibilityHost: this, subTableButtonsMap: this.subTableButtonsMap };
  },
  props: {
    storeName: { type: String, required: true },
    pageConfig: { type: Object, default: null },
    buttons: { type: Array, default: () => [] },
    mainPath: { type: String, default: 'MAIN' },
    moduleCode: { type: String, default: '' },
    extraParams: { type: Object, default: () => ({}) }
  },
  data() {
    return {
      activeTab: '',
      mixinVersion: 0,
      fieldsMapped: false,
      // SFC slot 扩展组件: { 'form-top': comp, 'form-bottom': comp }
      slotComponents: {},
      // 字段 slot 扩展: { 'CUSTNAME': comp } (key 去掉 field: 前缀)
      fieldSlotComponents: {}
    };
  },
  computed: {
    // MAIN 资源的原始 scm 字段(用于按 EDITGROUP 分组)
    mainScm() {
      var modData = this.moduleData;
      if (!modData || !modData.MODPATH) return [];
      var mpItem = modData.MODPATH.find(p => p.PATHNAME === (this.mainPath || 'MAIN'));
      if (!mpItem || !mpItem.RESOURCENAME) return [];
      var scms = (this.$store.state.app && this.$store.state.app.scms) || {};
      return scms[mpItem.RESOURCENAME] || [];
    },
    // 按 EDITGROUP 分组字段(每组 Gen.getFormFields 处理), 空 EDITGROUP 归"基本信息"
    formGroups() {
      var scm = this.mainScm;
      if (!scm || !scm.length) return [];
      var valid = scm.filter(f => (f.RESFIELDNAME || f.FIELDNAME) && f.EDITSORT && +f.EDITSORT > 0);
      // 按照 EDITSORT排序
      valid = valid.sort((a, b) => +b.EDITSORT + (-a.EDITSORT));
      var groupMap = {};
      var groups = [];
      valid.forEach(f => {
        var g = f.EDITGROUP || '基本信息';
        if (!groupMap[g]) { groupMap[g] = { name: g, items: [] }; groups.push(groupMap[g]) }
        groupMap[g].items.push(f);
      });
      groups.forEach(g => { g.fields = Gen.getFormFields(g.items) });
      return groups;
    },
    // 只有1个分组时不分 tab(单 rs-form-edit)
    hasGroups() {
      return this.formGroups.length > 1;
    },
    // 表单列模式: PAGECONFIG.FORMLAYOUT(onecolumn/twocolumn/threecolumn), 默认两列(与历史行为一致)
    formMode() {
      var layout = '';
      if (this.pageConfig && this.pageConfig.PAGECONFIG) {
        try { layout = JSON.parse(this.pageConfig.PAGECONFIG).FORMLAYOUT || '' } catch (e) {}
      }
      if (layout === 'onecolumn' || layout === 'single') return 'single';
      if (layout === 'threecolumn') return 'threecolumn';
      return 'twocolumn';
    },
    // 子表按钮: BTNAREA 直接为子表路径(如 DTSA/DTSB), 按 BTNAREA 分组
    // 兼容老数据: BTNAREA='subtable' + EXTPARAM.subtable 也归入对应子表
    subTableButtonsMap() {
      var map = {};
      (this.buttons || []).forEach(btn => {
        var area = btn.BTNAREA;
        if (area === 'header' || area === 'footer' || area === 'row' || !area) return;
        if (area === 'subtable') {
          // 老数据迁移
          var extLegacy = this.parseExtparam(btn) || {};
          area = extLegacy.subtable;
        }
        if (!area) return;
        if (!map[area]) map[area] = [];
        map[area].push(btn);
      });
      return map;
    },
    tabDatas() {
      var tabs = {};
      this.formGroups.forEach(function(g) { tabs[g.name] = g.name });
      return tabs;
    },
    moduleData() {
      if (!this.moduleCode) return null;
      var appState = this.$store.state.app;
      if (appState && appState.modules) {
        return appState.modules[this.moduleCode];
      }
      return null;
    },
    visibleButtons() {
      // fieldsMapped: mapDataTableFields 完成后触发重新计算
      // 未完成时 this.ID 等 getter 未定义，跳过 SHOWCOND 评估（全部显示）
      var mapped = this.fieldsMapped;
      return this.buttons.filter(btn => {
        // 子表按钮(BTNAREA 不在 header/footer/row) 不在主表单区域渲染
        var area = btn.BTNAREA;
        if (area && area !== 'header' && area !== 'footer' && area !== 'row') return false;
        if (!btn.SHOWCOND) return true;
        if (!mapped) return true;
        try {
          // 1) 如果 SHOWCOND 直接是 this 上的属性/方法名 (如 ISSHOWMYBTN)，直接评估
          var target = this[btn.SHOWCOND];
          if (target !== undefined) {
            if (typeof target === 'function') return !!target.call(this);
            return !!target;
          }
          // 2) 表达式求值
          var expr = btn.SHOWCOND;
          var state = this.STATE;
          // STATE 变量替换
          expr = expr.replace(/STATE===?(\d+)/g, function(m, v) { return state + '===' + v });
          // 系统变量替换
          var userInfo = this.$store.state.user && this.$store.state.user.userInfo;
          expr = expr.replace(/_USERID_/g, userInfo ? '\'' + userInfo.ID + '\'' : '\'\'');
          expr = expr.replace(/_EMPID_/g, userInfo ? '\'' + userInfo.EMPID + '\'' : '\'\'');
          expr = expr.replace(/_DEPTID_/g, userInfo ? '\'' + userInfo.DEPTID + '\'' : '\'\'');
          // ID 判断
          expr = expr.replace(/ID!=null/g, this.ID ? 'true' : 'false');
          // 用 with(this) 让表达式可直接访问 this 上的属性 (STATE/ID/ISSHOWxxx 等)
          // eslint-disable-next-line no-new-func, no-with
          var fn = new Function('with(this) { return ' + expr + ' }');
          return fn.call(this);
        } catch (e) {
          return true;
        }
      });
    },
    // 供模板中 :host="self" 传递当前组件实例给 SFC slot 组件
    self() {
      return this;
    }
  },
  beforeCreate() {
    var sn = this.$options.propsData && this.$options.propsData.storeName;
    var mp = (this.$options.propsData && this.$options.propsData.mainPath) || 'MAIN';
    if (sn) {
      var storeResult = getGenericStore(sn);
      // 映射主表
      var dtComputed = storeResult.mapDateTable(mp, []);
      // Vue 2 的 $options.computed 是组件定义级别的共享对象引用,
      // 多个实例会互相污染 (第一个实例设置的 $MAIN 会被后续实例继承)
      // 创建实例级副本, 确保每个实例的 computed 独立
      this.$options.computed = Object.assign({}, this.$options.computed);
      Object.keys(dtComputed).forEach(key => {
        this.$options.computed[key] = dtComputed[key];
      });
      // 固定名称代理，让模板中 $MAIN 不受 mainPath 变化影响
      if (mp !== 'MAIN') {
        this.$options.computed['$MAIN'] = function() { return this['$' + mp] };
        this.$options.computed['MAIN'] = function() { return this[mp] };
      }
    }
  },
  created() {
    this.mapDataTableFields();
    this.loadExtendMixin();
    // 加载 SFC slot 扩展组件
    this.loadSlotComponents();
    // 异步加载模块级 SFC store 扩展
    if (this.moduleCode) {
      applyStoreExtend(this.moduleCode);
    }
    // 独立表单页(PAGETYPE='form'): 父级不是 rs-modal，
    // Add01 mixin 的 $parent.isOpened watch 不会触发，需在首次渲染前调用 onShow
    // 必须在 created 中同步执行 INIT+ADD，否则 rs-form-edit 首次渲染会读到
    // store 中上一个模块的残留数据（DataTable 引用不变，内部变化不触发重渲染）
    var p = this.$parent;
    var inModal = false;
    while (p) {
      if (p.$options && p.$options.name === 'rs-modal') { inModal = true; break }
      p = p.$parent;
    }
    if (!inModal) {
      this.onShow();
    }
  },
  activated() {
    // keep-alive 恢复时重新加载扩展JS
    // 保存时已清了 moduleCache，此处 loadExtendMixin 会从数据库拉最新代码
    // 若缓存未被清（代码没改），则直接用缓存，不会重复请求
    this.loadExtendMixin();
    if (this.moduleCode) {
      applyStoreExtend(this.moduleCode);
    }
  },
  watch: {
    // scm 异步加载后 formGroups 才有值, 设默认选中首个分组
    formGroups: {
      handler(groups) {
        if (groups && groups.length) {
          this.activeTab = groups[0].name;
        }
      },
      immediate: true,
    },
  },
  methods: {
    // 把主表字段映射到 this 上，使扩展 JS 可直接用 this.ID / this.STATE 等
    // 子表的 $DTSA / DTSA 已在 beforeCreate 通过 mapDateTable 映射
    async mapDataTableFields() {
      if (!this.moduleCode) return;
      var modData = this.$store.state.app.modules[this.moduleCode];
      if (!modData || !modData.MODPATH) return;
      var mp = this.mainPath || 'MAIN';
      // 找主表的 RESOURCENAME
      var mpItem = modData.MODPATH.find(function(p) { return p.PATHNAME === mp });
      if (!mpItem || !mpItem.RESOURCENAME) return;
      // 确保 SCM 已加载
      try {
        // eslint-disable-next-line no-restricted-syntax
        await this.$store.dispatch('app/initScms', [mpItem.RESOURCENAME]);
      } catch (e) {
        return;
      }
      var scm = this.$store.state.app.scms[mpItem.RESOURCENAME];
      if (!scm || !Array.isArray(scm)) return;
      var dt = this['$' + mp];
      if (!dt) return;
      var self = this;
      scm.forEach(function(item) {
        var fn = item.FIELDNAME || item.RESFIELDNAME;
        if (!fn) return;
        // 跳过已存在的 methods（避免覆盖组件内置方法），但 props/data 允许覆盖
        // 这样 this.ID / this.STATE 等会读 DataTable 而非 prop，能同步更新
        if (typeof self[fn] === 'function') return;
        var f = fn.replace(/_/g, '.');
        Object.defineProperty(self, fn, {
          get: function() { return dt.getValue(f, 0) },
          set: function(v) { dt.setValue(f, v, 0) },
          enumerable: true,
          configurable: true
        });
      });
      // 字段映射完成：设置标志位触发 visibleButtons 重新计算
      self.fieldsMapped = true;
      // scm 异步加载完成后，强制 rs-form-edit 重建以读取正确的字段定义
      // rs-form-edit 的 created() 在 scm 加载前执行会拿到空/旧的字段列表，且不会自动更新
      self.mixinVersion++;
    },
    async onShow() {
      this.loading = true;
      try {
        // 用 propsData.ID 读 prop 原始值（this.ID 已被 mapDataTableFields 覆盖为读 DataTable）
        var propId = this.$options.propsData && this.$options.propsData.ID;
        if (propId) {
          var openParams = { ID: propId };
          if (this.extraParams && Object.keys(this.extraParams).length > 0) {
            openParams.extraFilterParams = this.extraParams;
          }
          // eslint-disable-next-line no-restricted-syntax
          await this.$store.dispatch(this.storeName + '/open', openParams);
        } else {
          // eslint-disable-next-line no-restricted-syntax
          await this.$store.dispatch(this.storeName + '/add', {});
        }
      } finally {
        this.loading = false;
      }
    },
    save() {
      // 多 tabs 时 $refs.form 是数组，取当前活动 tab 的 form 实例
      var formRef = this.$refs.form;
      var formComp = Array.isArray(formRef) ? formRef.find(f => f.$el && f.$el.parentElement && !f.$el.parentElement.hidden && f.$el.offsetParent !== null) || formRef[0] : formRef;
      if (!formComp) return;
      var validResult = formComp.valid();
      if (!validResult.result) {
        return;
      }
      var self = this;
      this.$callAction({
        action: this.storeName + '/save',
        successText: '操作成功',
        isSuccessBack: true,
        successCall: function() {
          if (formComp && formComp.closeAiPanel) {
            formComp.closeAiPanel();
          }
          self.$emit('saved');
        }
      });
    },
    // 解析按钮的 EXTPARAM
    parseExtparam(btn) {
      if (!btn.EXTPARAM) return {};
      if (typeof btn.EXTPARAM === 'object') return btn.EXTPARAM;
      try { return JSON.parse(btn.EXTPARAM) } catch (e) { return {} }
    },
    // 调用按钮钩子 (beforeAction/afterAction)，支持异步
    async callBtnHook(hookName, btn, context) {
      var ext = this.parseExtparam(btn);
      var hookFn = ext[hookName];
      console.log('[Form callBtnHook]', hookName, '→ method:', hookFn,
        'btn.BTNNAME:', btn && btn.BTNNAME, 'btn.BTNAREA:', btn && btn.BTNAREA,
        'btn.EXTPARAM:', btn && btn.EXTPARAM, 'parsed ext:', ext);
      if (!hookFn) {
        console.warn('[Form callBtnHook] EXTPARAM.' + hookName + ' 未配置 (btn.EXTPARAM=' + (btn && btn.EXTPARAM) + ')，跳过');
        return true;
      }
      if (typeof this[hookFn] !== 'function') {
        console.warn('[Form callBtnHook] this[' + hookFn + '] 不是函数，typeof=', typeof this[hookFn],
          '→ 扩展JS可能未加载或方法名不匹配。请检查: 1) 扩展JS文件是否存在 2) 方法是否定义在 methods 里 3) 方法名是否一致');
        return true;
      }
      var ret = await this[hookFn](btn, context);
      console.log('[Form callBtnHook]', hookFn, '返回:', ret);
      return ret;
    },
    async handleBtn(btn) {
      var ext = this.parseExtparam(btn);
      var context = { row: this.model, ext: ext, btn: btn };
      // beforeAction 钩子（支持异步，返回 false 阻止动作）
      if ((await this.callBtnHook('beforeAction', btn, context)) === false) return;

      // 优先按 BTNCODE 分发
      var code = btn.BTNCODE;
      if (code === 'save') {
        this.save();
      } else if (code === 'delete') {
        this.del();
      } else if (code === 'submit') {
        this.submit(this.ID);
      } else if (code === 'reSubmit') {
        this.reSubmit(this.ID);
      } else if (code === 'check') {
        this.check(this.ID);
      } else if (code === 'reCheck') {
        this.reCheck(this.ID);
      } else if (code === 'verify') {
        this.verify(this.ID);
      } else if (code === 'reVerify') {
        this.reVerify(this.ID);
      } else if (code && code !== 'custom') {
        // export/其他: 走通用 call（APICODE 作为参数，不再当 action type）
        this.$callAction({
          action: this.storeName + '/call',
          param: {
            APICODE: btn.APICODE,
            params: this.mergeExtparam(ext, btn)
          },
          successText: '操作成功',
          successCall: () => { this.$emit('saved') }
        });
      } else {
        // 兼容旧数据(无 BTNCODE): 按 BTNTYPE 分发
        switch (btn.BTNTYPE) {
          case 'crud':
            this.handleCrudBtn(btn, ext);
            break;
          case 'flow':
            this.handleFlowBtn(btn, ext);
            break;
          default:
            this.handleCustomBtn(btn, ext);
            break;
        }
      }
      // afterAction 钩子（支持异步）
      await this.callBtnHook('afterAction', btn, context);
    },
    handleCrudBtn(btn, ext) {
      switch (btn.APICODE) {
        case 'A04':
          this.save();
          break;
        case 'A07':
          this.del();
          break;
        default:
          this.$callAction({
            action: this.storeName + '/call',
            param: {
              APICODE: btn.APICODE,
              params: this.mergeExtparam(ext, btn)
            },
            successText: '操作成功',
            successCall: () => { this.$emit('saved') }
          });
          break;
      }
    },
    // 合并 extraParams 到 action param
    // paramsFn 指向扩展 JS 方法名时，调用该方法获取动态参数并合并
    mergeExtparam(ext, btn) {
      var self = this;
      var param = {};
      if (ext.extraParams) {
        Object.keys(ext.extraParams).forEach(function(k) {
          param[k] = ext.extraParams[k];
        });
      }
      if (ext.paramsFn && typeof self[ext.paramsFn] === 'function') {
        var context = {
          row: self.model,
          ext: ext,
          btn: btn
        };
        var dyn = self[ext.paramsFn](btn, context);
        if (dyn && typeof dyn === 'object') {
          Object.keys(dyn).forEach(function(k) {
            param[k] = dyn[k];
          });
        }
      }
      return param;
    },
    handleFlowBtn(btn, ext) {
      var flowActions = {
        'A17': 'submit',
        'A13': 'reSubmit',
        'A12': 'check',
        'A14': 'verify',
        'A16': 'reCheck',
        'A15': 'reVerify'
      };
      var action = flowActions[btn.APICODE];
      if (action && this[action]) {
        this[action](this.ID);
      } else {
        this.$callAction({
          action: this.storeName + '/call',
          param: {
            APICODE: btn.APICODE,
            params: this.mergeExtparam(ext, btn)
          },
          successText: '操作成功',
          successCall: () => { this.$emit('saved') }
        });
      }
    },
    handleCustomBtn(btn, ext) {
      this.$callAction({
        action: this.storeName + '/call',
        param: {
          APICODE: btn.APICODE,
          params: this.mergeExtparam(ext)
        },
        successText: '操作成功',
        successCall: () => { this.$emit('saved') }
      });
    },
    // 标准接口：打开页面（委托给父级 generic-module）
    openPage(options) {
      var parent = this.$parent;
      while (parent) {
        if (typeof parent.openPage === 'function') return parent.openPage(options);
        parent = parent.$parent;
      }
    },
    // 标准接口：打开选入（委托给父级 generic-module）
    openSelector(options) {
      var parent = this.$parent;
      while (parent) {
        if (typeof parent.openSelector === 'function') return parent.openSelector(options);
        parent = parent.$parent;
      }
    },
    /**
     * 标准接口：关闭当前表单弹窗
     */
    closePage() {
      var rsModal = this.$parent;
      while (rsModal && rsModal.$options.name !== 'rs-modal') {
        rsModal = rsModal.$parent;
      }
      if (rsModal && typeof rsModal.hide === 'function') {
        rsModal.hide();
      }
    },
    // 加载扩展 JS mixin
    async loadExtendMixin() {
      var json = {};
      if (this.pageConfig && this.pageConfig.PAGECONFIG) {
        try { json = JSON.parse(this.pageConfig.PAGECONFIG) } catch (e) {}
      }
      // 优先使用 PAGECONFIG.EXTENDJS，否则用约定路径
      var jsPath = json.EXTENDJS;
      if (!jsPath && this.moduleCode) {
        var pc = this.pageConfig;
        jsPath = '@/modules/' + this.moduleCode + '/' + (pc && pc.PAGECODE ? pc.PAGECODE : 'form') + '.js';
      }
      try {
        var mod = await loadCompiledSFC(jsPath);
        var mixinObj = mod && mod.default ? mod.default : mod;
        if (mixinObj && typeof mixinObj === 'object') {
          var self = this;
          // 记录扩展JS注入的key，热更新时允许覆盖这些key
          if (!this._extendKeys) this._extendKeys = {};
          if (mixinObj.methods) {
            Object.keys(mixinObj.methods).forEach(function(key) {
              if (self._extendKeys[key] || typeof self[key] !== 'function') {
                self[key] = mixinObj.methods[key];
                self._extendKeys[key] = true;
              }
            });
          }
          if (mixinObj.computed) {
            Object.keys(mixinObj.computed).forEach(function(key) {
              if (self._extendKeys[key] || typeof self[key] === 'undefined') {
                var getter = typeof mixinObj.computed[key] === 'function' ?
                  mixinObj.computed[key] :
                  mixinObj.computed[key].get;
                if (getter) {
                  Object.defineProperty(self, key, {
                    get: function() { return getter.call(self) },
                    enumerable: true,
                    configurable: true
                  });
                  self._extendKeys[key] = true;
                }
              }
            });
          }
          if (mixinObj.data && typeof mixinObj.data === 'object') {
            Object.keys(mixinObj.data).forEach(function(key) {
              if (self._extendKeys[key] || typeof self[key] === 'undefined') {
                self[key] = mixinObj.data[key];
                self._extendKeys[key] = true;
              }
            });
          }
          if (typeof mixinObj.init === 'function') {
            Promise.resolve(mixinObj.init.call(this)).catch(function(e) {
              console.error('[GenericForm] 扩展JS init 钩子异常:', e);
            });
          }
          if (typeof mixinObj.mounted === 'function') {
            Promise.resolve(mixinObj.mounted.call(this)).catch(function(e) {
              console.error('[GenericForm] 扩展JS mounted 钩子异常:', e);
            });
          }
          // mixin 注入完成后递增 mixinVersion，强制 rs-form-edit 重建
          // （Object.defineProperty 添加的 computed 在首次渲染时不存在，
          //  需重建子组件让 isFieldVisible 重新评估 ISSHOWxxx）
          this.mixinVersion++;
        }
      } catch (e) {
        // 约定路径不存在是正常的，只在手动配置 EXTENDJS 时报错
        if (json.EXTENDJS) {
          console.error('[GenericForm] 扩展JS加载失败:', jsPath, e);
        }
      }
    },
    // 加载 SFC slot 扩展组件
    async loadSlotComponents() {
      var json = {};
      if (this.pageConfig && this.pageConfig.PAGECONFIG) {
        try { json = JSON.parse(this.pageConfig.PAGECONFIG) } catch (e) {}
      }
      var slots = json.SLOTS;
      if (!slots || typeof slots !== 'object') return;
      var self = this;
      var promises = Object.keys(slots).map(function(slotName) {
        var path = slots[slotName];
        if (!path) return Promise.resolve();
        return loadCompiledSFC(path).then(function(options) {
          if (options && (options.render || options.template || options.component)) {
            if (slotName.indexOf('field:') === 0) {
              var fieldName = slotName.substring(6);
              self.$set(self.fieldSlotComponents, fieldName, options);
            } else {
              self.$set(self.slotComponents, slotName, options);
            }
          }
        }).catch(function(e) {
          console.error('[GenericForm] Slot SFC 加载失败:', slotName, path, e);
        });
      });
      await Promise.all(promises);
      // 字段 slot 加载后递增 mixinVersion 强制 rs-form-edit 重建
      if (Object.keys(this.fieldSlotComponents).length > 0) {
        this.mixinVersion++;
      }
    },
    // 读取主表字段值（供 field slot SFC 的 value prop）
    getFieldSlotValue(fieldName) {
      var mp = this.mainPath || 'MAIN';
      var dt = this['$' + mp];
      if (!dt) return '';
      return dt.getValue(fieldName, 0);
    },
    // 写入主表字段值（供 field slot SFC 的 @input 事件）
    setFieldSlotValue(fieldName, value) {
      var mp = this.mainPath || 'MAIN';
      var dt = this['$' + mp];
      if (dt) dt.setValue(fieldName, value, 0);
    },
    // 分组模式下过滤当前分组包含的字段 slot
    getGroupFieldSlots(group) {
      if (!group || !group.fields) return {};
      var result = {};
      var self = this;
      group.fields.forEach(function(f) {
        var key = f.props && f.props.key;
        if (key && self.fieldSlotComponents[key]) {
          result[key] = self.fieldSlotComponents[key];
        }
      });
      return result;
    }
  }
};
</script>

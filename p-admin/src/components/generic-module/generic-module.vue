<template>
  <div class="generic-module-root">
  <!-- SFC 在线模块页: 优先判断，避免 PAGETYPE 仍为 list/form 时被前面的分支拦截 -->
  <div v-if="isSfcPage && storeReady" class="generic-form-page">
    <component v-if="sfcComponent" :is="sfcComponent"></component>
    <div v-else-if="sfcLoading" style="padding:40px;text-align:center;color:#999;">
      <p>正在加载在线模块...</p>
    </div>
    <div v-else-if="sfcError" style="padding:30px;text-align:center;">
      <p style="color:#ed4014;">模块加载失败</p>
      <pre style="background:#f5f5f5;padding:12px;color:#ed4014;font-size:13px;max-height:300px;overflow:auto;">{{ sfcError }}</pre>
      <Button color="primary" @click="loadSfcModule">重新加载</Button>
    </div>
  </div>

  <!-- 列表页: 直接渲染 list-t01 (不能包 div，否则高度计算会错) -->
  <list-t01
    v-else-if="pageConfig && storeReady && pageConfig.PAGETYPE==='list'"
    :title="title"
    :bcDatas="bcDatas"
    :store="storeObj"
    @list-click-row="clickRow"
    @list-action="listAction"
    @list-select="onListSelect"
    :showQuery="showQuery"
    :dynamicQuery="true"
    :addper="addPerm"
    :expper="expPerm"
    :checkbox="hasBatchButtons"
    :qryPath="listQryPath"
    :qqryPath="listQqryPath"
    :advQueryAPICODE="advQueryAPICODE"
    ref="list"
  >
    <rs-modal ref="madd">
      <generic-form
        :storeName="storeName"
        :moduleCode="moduleCode"
        :title="formTitle"
        :ID="currentId"
        :pageConfig="currentFormPageConfig"
        :buttons="formButtons"
        :mainPath="formMainPath"
        :key="'form_' + (currentFormPageConfig && currentFormPageConfig.PAGECODE || 'default')"
      ></generic-form>
    </rs-modal>
    <template slot="simple-query">
      <component
        v-if="slotComponents['simple-query']"
        :is="slotComponents['simple-query']"
        :host="self"
      ></component>
    </template>
    <template slot="body-query">
      <component
        v-if="slotComponents['body-query']"
        :is="slotComponents['body-query']"
        :host="self"
      ></component>
    </template>
    <template slot="header-action">
      <component
        v-if="slotComponents['header-action']"
        :is="slotComponents['header-action']"
        :host="self"
        :buttons="headerButtons"
      ></component>
      <template v-else>
        <template v-for="btn in headerButtons">
          <Poptip
            v-if="btn.INTERACTTYPE==='poptip'"
            :key="btn.ID || btn._idx_"
            :content="btn.POPTIPTEXT || '确定执行？'"
            @confirm="handleBtnAction(btn)"
          >
            <Button
              class="ml5"
              v-per="btn.PERMCODE"
              :icon="btn.ICON"
              :color="btn.COLOR"
            >{{btn.BTNNAME}}</Button>
          </Poptip>
          <Button
            v-else
            :key="btn.ID || btn._idx_"
            class="ml5"
            v-per="btn.PERMCODE"
            :icon="btn.ICON"
            :color="btn.COLOR"
            @click="handleBtnAction(btn)"
          >{{btn.BTNNAME}}</Button>
        </template>
      </template>
    </template>
    <!-- 列表底部按钮区 -->
    <template slot="footer-action">
      <component
        v-if="slotComponents['footer-action']"
        :is="slotComponents['footer-action']"
        :host="self"
        :buttons="footerButtons"
      ></component>
      <template v-else>
        <template v-for="btn in footerButtons">
          <Poptip
            v-if="btn.INTERACTTYPE==='poptip'"
            :key="btn.ID || btn._idx_"
            :content="btn.POPTIPTEXT || '确定执行？'"
            @confirm="handleBtnAction(btn)"
          >
            <Button class="ml5" v-per="btn.PERMCODE" :color="btn.COLOR">{{btn.BTNNAME}}</Button>
          </Poptip>
          <Button
            v-else
            :key="btn.ID || btn._idx_"
            class="ml5"
            v-per="btn.PERMCODE"
            :color="btn.COLOR"
            @click="handleBtnAction(btn)"
          >{{btn.BTNNAME}}</Button>
        </template>
      </template>
    </template>
    <!-- 行操作列 -->
    <TableItem title="操作" :width="rowBtnWidth" align="center" fixed="right" slot="table-action" v-if="rowButtons.length > 0 || slotComponents['table-action']">
      <template slot-scope="{data}">
        <component
          v-if="slotComponents['table-action']"
          :is="slotComponents['table-action']"
          :host="self"
          :buttons="rowButtons"
          :row="data"
        ></component>
        <template v-else>
          <template v-for="btn in rowButtons">
            <Poptip
              v-if="btn.INTERACTTYPE==='poptip'"
              :key="btn.ID || btn._idx_"
              :content="btn.POPTIPTEXT || '确定执行？'"
              @confirm="handleBtnAction(btn, data)"
            >
              <Button size="s" v-per="btn.PERMCODE" :color="btn.COLOR">{{btn.BTNNAME}}</Button>
            </Poptip>
            <Button
              v-else
              :key="btn.ID || btn._idx_"
              size="s"
              v-per="btn.PERMCODE"
              :color="btn.COLOR"
              @click="handleBtnAction(btn, data)"
            >{{btn.BTNNAME}}</Button>
          </template>
        </template>
      </template>
    </TableItem>
  </list-t01>

  <!-- 表单页: 直接渲染表单内容 -->
  <div v-else-if="pageConfig && storeReady && pageConfig.PAGETYPE==='form'" class="generic-form-page">
    <generic-form
      :key="'gf_' + moduleCode + '_' + pageConfig.PAGECODE"
      :storeName="storeName"
      :moduleCode="moduleCode"
      :title="title"
      :ID="formId"
      :pageConfig="pageConfig"
      :buttons="pageButtons"
      :mainPath="formMainPath"
    ></generic-form>
  </div>

  <!-- 选择页: 弹窗选择器风格，无新增/编辑/弹窗表单 -->
  <div v-else-if="pageConfig && storeReady && pageConfig.PAGETYPE==='select'" class="generic-selector-wrap">
    <list-t01
      :title="title"
      :store="storeObj"
      @list-click-row="clickRow"
      @list-action="listAction"
      @list-select="onListSelect"
      :showQuery="showQuery"
      :dynamicQuery="true"
      :checkbox="isSelectMultiple || hasBatchButtons"
      :addper="false"
      :expper="false"
      :qryPath="listQryPath"
      :qqryPath="listQqryPath"
      :advQueryAPICODE="advQueryAPICODE"
      ref="list"
    >
      <template slot="simple-query">
         <span class="generic-selector-title">{{ title || '选择数据' }}</span>
        <component
          v-if="slotComponents['simple-query']"
          :is="slotComponents['simple-query']"
          :host="self"
        ></component>
      </template>
      <template slot="body-query">
        <component
          v-if="slotComponents['body-query']"
          :is="slotComponents['body-query']"
          :host="self"
        ></component>
      </template>
      <!-- 覆盖默认新增按钮：select 页面不需要新增 -->
      <template slot="header-action">
        <component
          v-if="slotComponents['header-action']"
          :is="slotComponents['header-action']"
          :host="self"
          :buttons="headerButtons"
        ></component>
        <template v-else>
          <template v-for="btn in headerButtons">
            <Poptip
              v-if="btn.INTERACTTYPE==='poptip'"
              :key="btn.ID || btn._idx_"
              :content="btn.POPTIPTEXT || '确定执行？'"
              @confirm="handleBtnAction(btn)"
            >
              <Button
                class="ml5"
                v-per="btn.PERMCODE"
                :icon="btn.ICON"
                :color="btn.COLOR"
              >{{btn.BTNNAME}}</Button>
            </Poptip>
            <Button
              v-else
              :key="btn.ID || btn._idx_"
              class="ml5"
              v-per="btn.PERMCODE"
              :icon="btn.ICON"
              :color="btn.COLOR"
              @click="handleBtnAction(btn)"
            >{{btn.BTNNAME}}</Button>
          </template>
          <span v-if="headerButtons.length === 0 && !slotComponents['header-action']"></span>
          <label style="width: 10px;">&nbsp;&nbsp;&nbsp;&nbsp;</label>
        </template>
      </template>
      <!-- 底部：确认/取消按钮放在分页栏同一行 -->
      <template slot="footer-action">
        <Button @click="$emit('selector-cancel')">取消</Button>
        <Button color="primary" @click="handleSelectorConfirm">确定</Button>
      </template>
    </list-t01>
  </div>

  <!-- 审核页: 与列表页相同布局 -->
  <list-t01
    v-else-if="pageConfig && storeReady && pageConfig.PAGETYPE==='review'"
    :title="title"
    :bcDatas="bcDatas"
    :store="storeObj"
    @list-click-row="clickRow"
    @list-action="listAction"
    @list-select="onListSelect"
    :showQuery="showQuery"
    :dynamicQuery="true"
    :addper="addPerm"
    :expper="expPerm"
    :checkbox="hasBatchButtons"
    :qryPath="listQryPath"
    :qqryPath="listQqryPath"
    ref="list"
  >
    <rs-modal ref="madd">
      <generic-form
        :storeName="storeName"
        :moduleCode="moduleCode"
        :title="formTitle"
        :ID="currentId"
        :pageConfig="currentFormPageConfig"
        :buttons="formButtons"
        :mainPath="formMainPath"
        :key="'form_' + (currentFormPageConfig && currentFormPageConfig.PAGECODE || 'default')"
      ></generic-form>
    </rs-modal>
    <template slot="simple-query">
      <component
        v-if="slotComponents['simple-query']"
        :is="slotComponents['simple-query']"
        :host="self"
      ></component>
    </template>
    <template slot="body-query">
      <component
        v-if="slotComponents['body-query']"
        :is="slotComponents['body-query']"
        :host="self"
      ></component>
    </template>
    <template slot="header-action">
      <component
        v-if="slotComponents['header-action']"
        :is="slotComponents['header-action']"
        :host="self"
        :buttons="headerButtons"
      ></component>
      <template v-else>
        <template v-for="btn in headerButtons">
          <Poptip
            v-if="btn.INTERACTTYPE==='poptip'"
            :key="btn.ID || btn._idx_"
            :content="btn.POPTIPTEXT || '确定执行？'"
            @confirm="handleBtnAction(btn)"
          >
            <Button
              class="ml5"
              v-per="btn.PERMCODE"
              :icon="btn.ICON"
              :color="btn.COLOR"
            >{{btn.BTNNAME}}</Button>
          </Poptip>
          <Button
            v-else
            :key="btn.ID || btn._idx_"
            class="ml5"
            v-per="btn.PERMCODE"
            :icon="btn.ICON"
            :color="btn.COLOR"
            @click="handleBtnAction(btn)"
          >{{btn.BTNNAME}}</Button>
        </template>
      </template>
    </template>
    <template slot="footer-action">
      <component
        v-if="slotComponents['footer-action']"
        :is="slotComponents['footer-action']"
        :host="self"
        :buttons="footerButtons"
      ></component>
      <template v-else>
        <template v-for="btn in footerButtons">
          <Poptip
            v-if="btn.INTERACTTYPE==='poptip'"
            :key="btn.ID || btn._idx_"
            :content="btn.POPTIPTEXT || '确定执行？'"
            @confirm="handleBtnAction(btn)"
          >
            <Button class="ml5" v-per="btn.PERMCODE" :color="btn.COLOR">{{btn.BTNNAME}}</Button>
          </Poptip>
          <Button
            v-else
            :key="btn.ID || btn._idx_"
            class="ml5"
            v-per="btn.PERMCODE"
            :color="btn.COLOR"
            @click="handleBtnAction(btn)"
          >{{btn.BTNNAME}}</Button>
        </template>
      </template>
    </template>
    <TableItem title="操作" :width="rowBtnWidth" align="center" fixed="right" slot="table-action" v-if="rowButtons.length > 0 || slotComponents['table-action']">
      <template slot-scope="{data}">
        <component
          v-if="slotComponents['table-action']"
          :is="slotComponents['table-action']"
          :host="self"
          :buttons="rowButtons"
          :row="data"
        ></component>
        <template v-else>
          <template v-for="btn in rowButtons">
            <Poptip
              v-if="btn.INTERACTTYPE==='poptip'"
              :key="btn.ID || btn._idx_"
              :content="btn.POPTIPTEXT || '确定执行？'"
              @confirm="handleBtnAction(btn, data)"
            >
              <Button size="s" v-per="btn.PERMCODE" :color="btn.COLOR">{{btn.BTNNAME}}</Button>
            </Poptip>
            <Button
              v-else
              :key="btn.ID || btn._idx_"
              size="s"
              v-per="btn.PERMCODE"
              :color="btn.COLOR"
              @click="handleBtnAction(btn, data)"
            >{{btn.BTNNAME}}</Button>
          </template>
        </template>
      </template>
    </TableItem>
  </list-t01>

  <!-- 报表页: PAGECONFIG.REPORT 驱动的 report-t01（表格 + ECharts） -->
  <div v-else-if="pageConfig && storeReady && pageConfig.PAGETYPE==='report'" class="generic-report-page">
    <report-t01
      :bcDatas="bcDatas"
      :datas="reportRows"
      :columns="reportColumns"
      :options="reportChartOptions"
      :initOption="reportChartInitOption"
      @query="loadReportData"
    >
      <template slot="query">
        <component v-if="slotComponents['simple-query']" :is="slotComponents['simple-query']" :host="self" />
        <Button size="s" color="primary" icon="ios-search" @click="loadReportData">查询</Button>
      </template>
    </report-t01>
  </div>

  <!-- 未匹配任何页面类型时的兜底提示 -->
  <div v-else-if="storeReady && !pageConfig" class="generic-module-empty">
    <p>未找到页面配置 ({{ moduleCode }}/{{ pageCode || 'main' }})</p>
    <p style="font-size:12px;color:#999;">请检查模块是否已加载，或页面编码是否正确</p>
  </div>

  <!-- 选入弹窗 -->
  <rs-modal ref="msel" :title="selTitle" :width="selWidth || 900">
    <div v-if="selConfig" style="height:70vh;display:flex;flex-direction:column;overflow:hidden;">
      <generic-module
        :moduleCode="selConfig.moduleCode || moduleCode"
        :pageCode="selConfig.selectPageCode || ''"
        :selectMode="selConfig.selectMode || 'single'"
        :filterParams="selConfig.filterParams || null"
        @list-select="onListSelect"
        @list-click-row="onSelRowClick"
        @selector-selected="onSelSelectorSelected"
        @selector-cancel="closeSelector"
      ></generic-module>
    </div>
  </rs-modal>
  </div>
</template>
<script>
import genericForm from './generic-form.vue';
import genericSelector from './generic-selector.vue';
import reportT01 from '@/components/rs-template/report-t01.vue';
import { getGenericStore, applyStoreExtend } from './generic-store';
import { loadCompiledSFC } from '@/sfc-loader';
import { getUrl } from '@/api/urls';

export default {
  name: 'GenericModule',
  components: { genericForm, genericSelector, reportT01 },
  provide() {
    return { visibilityHost: this };
  },
  props: {
    moduleCode: { type: String, required: true },
    pageCode: { type: String, default: 'main' },
    filterParams: { type: Object, default: null },
    selectMode: { type: String, default: '' } // 'single' | 'multiple'，覆盖 PAGECONFIG.SELECTMODE
  },
  data() {
    return {
      currentId: '',
      formId: '',
      showQuery: false,
      citem: {},
      storeReady: false,
      storeObj: null,
      selectedRows: [],
      sfcComponent: null,
      sfcLoading: false,
      sfcError: '',
      // SFC slot 扩展组件: { 'header-action': componentOptions, ... }
      slotComponents: {},
      // 选入弹窗
      selConfig: null,
      selTitle: '选择数据',
      selWidth: 900,
      selContext: null, // 保存当前选入按钮的上下文 {btn, row}
      // 当前打开的表单页面编码（由按钮的 formPageCode 指定，空=用默认 formPageConfig）
      currentFormPageCode: '',
      // 报表页数据行（PAGETYPE=report，loadReportData 填充）
      reportRows: []
    };
  },
  computed: {
    moduleData() {
      var appState = this.$store.state.app;
      if (appState && appState.modules) {
        return appState.modules[this.moduleCode];
      }
      return null;
    },
    pageConfig() {
      if (!this.moduleData || !this.moduleData.MODPAGE) {
        console.warn('[GenericModule pageConfig] moduleData 为空或无 MODPAGE, moduleCode:', this.moduleCode);
        return null;
      }
      var found = this.moduleData.MODPAGE.find(p => p.PAGECODE === this.pageCode && (p.ISDELETED || 0) === 0);
      if (!found) {
        console.warn('[GenericModule pageConfig] 未找到页面, moduleCode:', this.moduleCode, 'pageCode:', this.pageCode,
          '可用 PAGECODE:', this.moduleData.MODPAGE.filter(p => (p.ISDELETED || 0) === 0).map(p => p.PAGECODE + '(' + p.PAGETYPE + ')'));
      }
      return found;
    },
    // 表单页面配置（优先级：按钮指定 > 页面配置默认 > 第一个子 form > 任意 form）
    formPageConfig() {
      if (!this.pageConfig || this.pageConfig.PAGETYPE !== 'list') return null;
      if (!this.moduleData || !this.moduleData.MODPAGE) return null;
      var pid = this.pageConfig.ID;
      var modPage = this.moduleData.MODPAGE;
      // 1) 按钮指定的 pageCode（handleBtnAction 设置 currentFormPageCode）
      if (this.currentFormPageCode) {
        var specified = modPage.find(p =>
          p.PAGECODE === this.currentFormPageCode && p.PAGETYPE === 'form' && (p.ISDELETED || 0) === 0
        );
        if (specified) return specified;
      }
      // 2) 页面配置指定的默认 form（PAGECONFIG.defaultFormPageCode）
      var defaultCode = this.pageConfigJson && this.pageConfigJson.defaultFormPageCode;
      if (defaultCode) {
        var configured = modPage.find(p =>
          p.PAGECODE === defaultCode && p.PAGETYPE === 'form' && (p.ISDELETED || 0) === 0
        );
        if (configured) return configured;
      }
      // 3) 列表页关联的第一个 form 子页面
      var formPage = modPage.find(p =>
        p.PAGETYPE === 'form' && p.PARENTID === pid && (p.ISDELETED || 0) === 0
      );
      // 4) 回退：任意 form
      if (!formPage) {
        formPage = modPage.find(p => p.PAGETYPE === 'form' && (p.ISDELETED || 0) === 0);
      }
      return formPage;
    },
    // 当前表单页面配置（根据按钮指定的 formPageCode 动态切换，空=默认 formPageConfig）
    currentFormPageConfig() {
      if (!this.currentFormPageCode || !this.moduleData || !this.moduleData.MODPAGE) {
        return this.formPageConfig;
      }
      var found = this.moduleData.MODPAGE.find(p =>
        p.PAGECODE === this.currentFormPageCode && p.PAGETYPE === 'form' && (p.ISDELETED || 0) === 0
      );
      return found || this.formPageConfig;
    },
    title() {
      return this.pageConfig ? (this.pageConfig.PAGENAME || this.$route.meta.title || '') : '';
    },
    // 选择页是否多选模式（prop 优先，否则读 PAGECONFIG.SELECTMODE）
    isSelectMultiple() {
      if (!this.pageConfig || this.pageConfig.PAGETYPE !== 'select') return false;
      if (this.selectMode) return this.selectMode === 'multiple';
      var json = this.pageConfigJson;
      return json && json.SELECTMODE === 'multiple';
    },
    // 弹出表单的标题：取当前弹出 form 页面的 PAGENAME，缺省回退到列表页标题
    formTitle() {
      var fpc = this.currentFormPageConfig;
      return (fpc && fpc.PAGENAME) || this.title;
    },
    bcDatas() {
      // 面包屑: 从当前模块所属菜单向上追溯父级分类, 不再写死"基础数据"
      var omenus = (this.$store.state.app && this.$store.state.app.omenus) || [];
      var menu = omenus.find(t => t.FUNCCODE === this.moduleCode);
      var chain = [];
      var upId = menu ? menu.UPFUNCID : null;
      while (upId) {
        var parent = omenus.find(t => t.ID === upId);
        if (!parent) break;
        chain.unshift({ title: parent.FUNCNAME || '' });
        upId = parent.UPFUNCID;
      }
      chain.push({ title: this.title });
      return chain;
    },
    storeName() {
      return this.moduleCode.replace(/\//g, '_');
    },
    pageButtons() {
      if (!this.pageConfig || !this.moduleData.MODBUTTON) return [];
      return this.moduleData.MODBUTTON
        .filter(b => b.PAGEID === this.pageConfig.ID && (b.ISDELETED || 0) === 0)
        .sort((a, b) => (a.SORTNO || 0) - (b.SORTNO || 0));
    },
    headerButtons() {
      return this.pageButtons.filter(b => b.BTNAREA === 'header' && b.BTNTYPE !== 'crud' && this.evalShowCond(b.SHOWCOND));
    },
    footerButtons() {
      return this.pageButtons.filter(b => b.BTNAREA === 'footer' && this.evalShowCond(b.SHOWCOND));
    },
    rowButtons() {
      return this.pageButtons.filter(b => b.BTNAREA === 'row' && this.evalShowCond(b.SHOWCOND));
    },
    rowBtnWidth() {
      var len = this.rowButtons.length;
      if (len <= 1) return 80;
      if (len <= 2) return 130;
      if (len <= 3) return 180;
      return 60 * len;
    },
    formButtons() {
      var fpc = this.currentFormPageConfig;
      if (!fpc || !this.moduleData.MODBUTTON) return [];
      return this.moduleData.MODBUTTON
        .filter(b => b.PAGEID === fpc.ID && (b.ISDELETED || 0) === 0)
        .sort((a, b) => (a.SORTNO || 0) - (b.SORTNO || 0));
    },
    hasBatchButtons() {
      return this.pageButtons.some(b => b.BTNAREA === 'header' && b.BTNTYPE === 'batch');
    },
    addPerm() {
      // 优先按 BTNCODE=add 找按钮权限码，回退按 APICODE=A04 兼容旧配置
      var addBtn = this.pageButtons.find(b => b.BTNCODE === 'add') ||
                   this.pageButtons.find(b => b.BTNTYPE === 'crud' && b.APICODE === 'A04');
      return addBtn ? addBtn.PERMCODE : '';
    },
    expPerm() {
      // 优先按 BTNCODE=export 找按钮权限码，回退按 APICODE=A09 兼容旧配置
      var expBtn = this.pageButtons.find(b => b.BTNCODE === 'export') ||
                   this.pageButtons.find(b => b.BTNTYPE === 'crud' && b.APICODE === 'A09');
      return expBtn ? expBtn.PERMCODE : '';
    },
    pageConfigJson() {
      if (!this.pageConfig || !this.pageConfig.PAGECONFIG) return {};
      try {
        return JSON.parse(this.pageConfig.PAGECONFIG);
      } catch (e) {
        return {};
      }
    },
    listQryPath() {
      return this.pageConfigJson.QRYPATH || 'QRY';
    },
    listQqryPath() {
      return this.pageConfigJson.QQRYSPATH || 'QQRY';
    },
    // ===== 报表页（PAGETYPE=report，PAGECONFIG.REPORT 驱动）=====
    // REPORT 配置: { APICODE, PAGEMAX, CHART: { type: bar|line|pie, xField, yFields: [], initOption: {} } }
    reportConfig() {
      return this.pageConfigJson.REPORT || {};
    },
    // 报表数据源资源名（QRY 路径对应的 RESOURCENAME）
    reportResourceName() {
      if (!this.storeObj || !this.storeObj.storeHelper || !this.storeObj.storeHelper.moudle) return '';
      var paths = this.storeObj.storeHelper.moudle.getPaths() || {};
      return paths[this.listQryPath] || paths.QRY || '';
    },
    // 报表列：从 scm 的 LISTSORT 生成
    reportColumns() {
      var resName = this.reportResourceName;
      var scm = resName ? (this.$store.state.app.scms[resName] || []) : [];
      return scm
        .filter(f => +(f.LISTSORT || 0) > 0)
        .sort((a, b) => (a.LISTSORT || 0) - (b.LISTSORT || 0))
        .map(f => ({ title: f.LABELNAME, key: f.FIELDNAME }));
    },
    reportChartInitOption() {
      var c = this.reportConfig.CHART || {};
      return c.initOption || {};
    },
    // 图表 options：按 CHART 配置把行数据映射为 echarts option
    reportChartOptions() {
      var c = this.reportConfig.CHART;
      if (!c || !c.type || !c.xField || !this.reportRows || this.reportRows.length === 0) return null;
      var yFields = c.yFields || [];
      if (c.type === 'pie') {
        return {
          tooltip: { trigger: 'item' },
          series: [{
            type: 'pie',
            radius: '60%',
            data: this.reportRows.map(r => ({ name: r[c.xField], value: r[yFields[0]] }))
          }]
        };
      }
      return {
        tooltip: { trigger: 'axis' },
        legend: yFields.length > 1 ? { data: yFields } : undefined,
        xAxis: { type: 'category', data: this.reportRows.map(r => r[c.xField]) },
        yAxis: { type: 'value' },
        series: yFields.map(yf => ({
          name: yf,
          type: c.type,
          data: this.reportRows.map(r => r[yf])
        }))
      };
    },
    // 高级查询 APICODE: 页面配置了 ADVQUERYAPICODE 则传给 list-t01，
    // list-t01 dispatch advQuery 时附带此参数，让 Store03 按 APICODE 查找 API 行
    advQueryAPICODE() {
      return (this.pageConfig && this.pageConfig.ADVQUERYAPICODE) || '';
    },
    formMainPath() {
      var fpc = this.currentFormPageConfig;
      if (!fpc || !fpc.PAGECONFIG) return 'MAIN';
      try {
        var json = JSON.parse(fpc.PAGECONFIG);
        return json.MAINPATH || 'MAIN';
      } catch (e) {
        return 'MAIN';
      }
    },
    subPages() {
      var config = this.pageConfigJson;
      if (!config.SUBPAGES || !Array.isArray(config.SUBPAGES)) return [];
      return config.SUBPAGES.filter(function(sp) { return sp.PAGEID || sp.REFMODULECODE });
    },
    isSfcPage() {
      return this.pageConfig && this.pageConfig.COMPONENTTYPE === 'sfc' && this.pageConfig.SFCMODULEPATH;
    },
    // 供模板中 :host="self" 传递当前组件实例给 SFC slot 组件
    self() {
      return this;
    }
  },
  methods: {
    // 把查询字段(QQRY)映射到 this 上，使扩展 JS 可直接用 this.xxx
    // 列表数据(QRY)不映射单字段（多行语义不明），通过 this.QRY 数组访问
    async mapDataTableFields() {
      if (!this.moduleData || !this.moduleData.MODPATH) return;
      // 只映射 QQRY（查询条件），不映射 QRY（列表数据多行）
      var paths = [this.listQqryPath];
      // 收集 resourceName
      var resNames = [];
      paths.forEach(function(pn) {
        var item = this.moduleData.MODPATH.find(function(p) { return p.PATHNAME === pn });
        if (item && item.RESOURCENAME && resNames.indexOf(item.RESOURCENAME) === -1) {
          resNames.push(item.RESOURCENAME);
        }
      }.bind(this));
      if (resNames.length === 0) return;
      // 确保 SCM 已加载
      try {
        // eslint-disable-next-line no-restricted-syntax
        await this.$store.dispatch('app/initScms', resNames);
      } catch (e) {
        return;
      }
      var self = this;
      paths.forEach(function(pn) {
        var item = self.moduleData.MODPATH.find(function(p) { return p.PATHNAME === pn });
        if (!item || !item.RESOURCENAME) return;
        var scm = self.$store.state.app.scms[item.RESOURCENAME];
        if (!scm || !Array.isArray(scm)) return;
        var dt = self['$' + pn];
        if (!dt) return;
        scm.forEach(function(s) {
          var fn = s.FIELDNAME || s.RESFIELDNAME;
          if (!fn) return;
          if (typeof self[fn] === 'function') return;
          var f = fn.replace(/_/g, '.');
          Object.defineProperty(self, fn, {
            get: function() { return dt.getValue(f, 0) },
            set: function(v) { dt.setValue(f, v, 0) },
            enumerable: true,
            configurable: true
          });
        });
      });
      // 字段映射完成后强制刷新，建立 Vue 依赖追踪
      this.$forceUpdate();
    },
    // 加载 SFC 在线模块
    async loadSfcModule() {
      if (!this.pageConfig || !this.pageConfig.SFCMODULEPATH) return;
      this.sfcLoading = true;
      this.sfcError = '';
      this.sfcComponent = null;
      try {
        var options = await loadCompiledSFC(this.pageConfig.SFCMODULEPATH);
        if (options && (options.render || options.template || options.component)) {
          this.sfcComponent = options;
        } else {
          this.sfcError = '模块 ' + this.pageConfig.SFCMODULEPATH + ' 不是有效的 Vue 组件';
        }
      } catch (e) {
        this.sfcError = e.message || String(e);
        console.error('[GenericModule] SFC 加载失败:', e);
      } finally {
        this.sfcLoading = false;
      }
    },
    // 加载扩展 JS mixin，注入到当前组件
    async loadExtendMixin() {
      // 优先使用 PAGECONFIG.EXTENDJS，否则用约定路径 @/modules/{moduleCode}/{pageCode}.js
      var jsPath = this.pageConfigJson.EXTENDJS;
      if (!jsPath && this.pageConfig) {
        jsPath = '@/modules/' + this.moduleCode + '/' + (this.pageConfig.PAGECODE || this.pageCode) + '.js';
      }
      try {
        var mod = await loadCompiledSFC(jsPath);
        var mixinObj = mod && mod.default ? mod.default : mod;
        if (mixinObj && typeof mixinObj === 'object') {
          var self = this;
          // 记录扩展JS注入的key，热更新时允许覆盖这些key
          if (!this._extendKeys) this._extendKeys = {};
          // 注入 methods（扩展JS注入的允许覆盖，组件内置的跳过）
          if (mixinObj.methods) {
            Object.keys(mixinObj.methods).forEach(function(key) {
              if (self._extendKeys[key] || typeof self[key] !== 'function') {
                self[key] = mixinObj.methods[key];
                self._extendKeys[key] = true;
              }
            });
          }
          // 注入 computed（Vue 2 在 mounted 后添加 computed 不会自动建立 watcher，
          // 需用 Object.defineProperty 创建 getter，每次访问时重新求值）
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
          // 注入 data（浅复制到实例）
          if (mixinObj.data && typeof mixinObj.data === 'object') {
            Object.keys(mixinObj.data).forEach(function(key) {
              if (self._extendKeys[key] || typeof self[key] === 'undefined') {
                self[key] = mixinObj.data[key];
                self._extendKeys[key] = true;
              }
            });
          }
          // 调用扩展的 init 钩子（支持异步）
          if (typeof mixinObj.init === 'function') {
            Promise.resolve(mixinObj.init.call(this)).catch(function(e) {
              console.error('[GenericModule] 扩展JS init 钩子异常:', e);
            });
          }
          // 调用扩展的 mounted 钩子（支持异步）
          if (typeof mixinObj.mounted === 'function') {
            Promise.resolve(mixinObj.mounted.call(this)).catch(function(e) {
              console.error('[GenericModule] 扩展JS mounted 钩子异常:', e);
            });
          }
          // mixin 注入完成后强制重新渲染
          this.$forceUpdate();
        }
      } catch (e) {
        // 约定路径不存在是正常的，只在手动配置 EXTENDJS 时报错
        if (this.pageConfigJson.EXTENDJS) {
          console.error('[GenericModule] 扩展JS加载失败:', jsPath, e);
        }
      }
    },
    // 加载 SFC slot 扩展组件
    async loadSlotComponents() {
      var slots = this.pageConfigJson.SLOTS;
      if (!slots || typeof slots !== 'object') return;
      var keys = Object.keys(slots);
      if (keys.length === 0) return;
      var self = this;
      keys.forEach(function(slotName) {
        var path = slots[slotName];
        if (!path) return;
        loadCompiledSFC(path).then(function(options) {
          if (options && (options.render || options.template || options.component)) {
            self.$set(self.slotComponents, slotName, options);
          }
        }).catch(function(e) {
          console.error('[GenericModule] Slot SFC 加载失败:', slotName, path, e);
        });
      });
    },
    // 根据 FLOWCODE 自动生成审批流按钮
    // ===== 报表页数据加载（PAGETYPE=report）=====
    // 接口优先级: PAGECONFIG.REPORT.APICODE > 页面 QUERYAPICODE；数据进 QRY 路径 DataTable
    async loadReportData() {
      if (!this.pageConfig || this.pageConfig.PAGETYPE !== 'report') return;
      var cfg = this.reportConfig;
      var apiCode = cfg.APICODE || this.pageConfig.QUERYAPICODE || '';
      if (!apiCode) {
        this.$Message.warning('报表页面未配置查询接口（PAGECONFIG.REPORT.APICODE 或页面 QUERYAPICODE）');
        return;
      }
      // 列配置（scm）懒加载
      var resName = this.reportResourceName;
      if (resName) {
        // eslint-disable-next-line no-restricted-syntax
        await this.$store.dispatch('app/initScms', [resName]);
      }
      // 报表默认拉全量（可用 PAGEMAX 调整）
      var qqry = this.storeObj.storeHelper.getTable(this.listQqryPath);
      if (qqry) qqry.setValue('PageSize', cfg.PAGEMAX || 500);
      // eslint-disable-next-line no-restricted-syntax
      await this.$store.dispatch(this.storeName + '/advQuery', { APICODE: apiCode, qryPath: this.listQryPath });
      var dt = this.storeObj.storeHelper.getTable(this.listQryPath);
      this.reportRows = dt && dt.data ? dt.data : [];
    },
    // SHOWCOND 表达式解析（列表页上下文）
    // 支持: 1) ISSHOWxxx 约定名 -> 调用扩展 JS 的 computed/method
    //       2) 表达式 (如 STATE===1, this.ISSHOWMYBTN(), _checks_.every(...))
    evalShowCond(showCond) {
      if (!showCond) return true;
      try {
        // 1) 如果 SHOWCOND 直接是 this 上的属性/方法名 (如 ISSHOWMYBTN)，用 evalVisibility 评估
        var target = this[showCond];
        if (target !== undefined) {
          if (typeof target === 'function') return !!target.call(this);
          return !!target;
        }
        // 2) 表达式求值
        var expr = showCond;
        // 系统变量替换
        var userInfo = this.$store.state.user && this.$store.state.user.userInfo;
        expr = expr.replace(/_USERID_/g, userInfo ? '\'' + userInfo.ID + '\'' : '\'\'');
        expr = expr.replace(/_EMPID_/g, userInfo ? '\'' + userInfo.EMPID + '\'' : '\'\'');
        expr = expr.replace(/_DEPTID_/g, userInfo ? '\'' + userInfo.DEPTID + '\'' : '\'\'');
        // 选中行变量
        expr = expr.replace(/_checks_/g, 'this.selectedRows');
        // 用 with(this) 让表达式可直接访问 this 上的属性 (STATE/ID/ISSHOWxxx 等)
        // eslint-disable-next-line no-new-func, no-with
        var fn = new Function('with(this) { return ' + expr + ' }');
        return fn.call(this);
      } catch (e) {
        return true;
      }
    },
    onListSelect(checks) {
      this.selectedRows = checks || [];
      // 选择页：emit 选中行给父组件（如 generic-selector）
      if (this.pageConfig && this.pageConfig.PAGETYPE === 'select') {
        this.$emit('list-select', checks);
      }
    },
    add() {
      // 当前表单页面是 select 类型时，打开选入弹窗
      var fpc = this.currentFormPageConfig || this.formPageConfig;
      if (fpc && fpc.PAGETYPE === 'select') {
        var selExt = {
          selectPageCode: this.currentFormPageCode || '',
          selectMode: this.isSelectMultiple ? 'multiple' : 'single',
          selectTarget: '',
          fieldMap: ''
        };
        this.doOpenSelector(
          { BTNNAME: fpc.PAGENAME || '选择数据' },
          selExt,
          null,
          { row: null, ext: selExt, btn: { BTNNAME: fpc.PAGENAME || '选择数据' } }
        );
        return;
      }
      this.currentId = '';
      this.$refs.madd && this.$refs.madd.show();
    },
    clickRow(row) {
      this.currentId = row.ID;
      this.citem = row;
      // 选择页：emit 选中行给父组件（如 generic-selector）
      if (this.pageConfig && this.pageConfig.PAGETYPE === 'select') {
        this.$emit('list-click-row', row);
        return;
      }
      // 双击行：重置按钮指定的 formPageCode，走页面配置的默认 form（defaultFormPageCode）
      this.currentFormPageCode = '';
      if (this.formPageConfig) {
        this.$refs.madd && this.$refs.madd.show();
      }
    },
    listAction(action, param) {
      if (action === 'add') this.add(param);
    },
    // 解析按钮的 EXTPARAM (JSON 字符串 -> 对象)
    parseExtparam(btn) {
      if (!btn.EXTPARAM) return {};
      if (typeof btn.EXTPARAM === 'object') return btn.EXTPARAM;
      try { return JSON.parse(btn.EXTPARAM) } catch (e) { return {} }
    },
    // 调用按钮钩子 (beforeAction/afterAction)，定义在扩展 JS 的 methods 中
    // 返回 false 表示中止
    async callBtnHook(hookName, btn, context) {
      var ext = this.parseExtparam(btn);
      var hookFn = ext[hookName];
      console.log('[callBtnHook]', hookName, '→ method:', hookFn, 'ext:', ext, 'btn:', btn);
      if (!hookFn) {
        console.warn('[callBtnHook] EXTPARAM.' + hookName + ' 未配置，跳过');
        return true;
      }
      if (typeof this[hookFn] !== 'function') {
        console.warn('[callBtnHook] this[' + hookFn + '] 不是函数，typeof=', typeof this[hookFn],
          '→ 扩展JS可能未加载或方法名不匹配。请检查: 1) 扩展JS文件是否存在 2) 方法是否定义在 methods 里 3) 方法名是否一致');
        return true;
      }
      var ret = await this[hookFn](btn, context);
      console.log('[callBtnHook]', hookFn, '返回:', ret);
      return ret;
    },
    async handleBtnAction(btn, row) {
      console.log('handleBtnAction', btn);
      if (row) {
        this.citem = row;
      }
      var ext = this.parseExtparam(btn);
      console.log('[handleBtnAction] ext:', ext, 'BTNCODE:', btn.BTNCODE, 'action:', ext.action);
      var context = { row: row || this.citem, ext: ext, btn: btn };
      // beforeAction 钩子（支持异步，返回 false 阻止动作）
      var beforeRet = await this.callBtnHook('beforeAction', btn, context);
      if (beforeRet === false) return;

      // 优先按 ext.action 分发（用户在配置中选择的动作类型），BTNCODE 预设作为快捷补充
      // 兼容旧数据: 无 action 但有 selectPageCode/selectModule → 也走 openSelector
      var code = btn.BTNCODE;
      var action = ext.action;
      var isSelector = action === 'openSelector' || (!action && (ext.selectPageCode || ext.selectModule));
      var isForm = action === 'openForm' || (!action && (ext.formPageCode || ext.openMode));
      if (isSelector) {
        console.log('[handleBtnAction] → doOpenSelector (action=%s)', action);
        this.doOpenSelector(btn, ext, row, context);
        return;
      } else if (isForm) {
        console.log('[handleBtnAction] → doOpenForm (action=%s)', action);
        this.doOpenForm(btn, ext, row);
      } else if (code === 'add') {
        this.currentFormPageCode = ext.formPageCode || '';
        this.add();
      } else if (code === 'edit') {
        this.currentFormPageCode = ext.formPageCode || '';
        // edit 指向 select 页面时也走选入逻辑
        var editFpc = this.currentFormPageConfig || this.formPageConfig;
        if (editFpc && editFpc.PAGETYPE === 'select') {
          this.add();
          return;
        }
        if (!row && !this.citem) { this.$Message('请先选择记录'); return }
        this.clickRow(row || this.citem);
      } else if (code === 'select') {
        this.doOpenSelector(btn, ext, row, context);
        return; // 选入走异步回调，afterAction 在 onSelectorSelected 中调
      } else if (code === 'delete') {
        this.doDelete(btn, ext, row);
      } else if (code) {
        // save/export/submit/reSubmit/check/reCheck/verify/reVerify/custom
        this.doCallApi(btn, ext, row, code);
      } else {
        // 兼容旧数据(无 BTNCODE 且无 action): 按 BTNTYPE 分发
        this.doCallApi(btn, ext, row);
      }
      // afterAction 钩子（支持异步）
      await this.callBtnHook('afterAction', btn, context);
    },
    // 删除：把待删行写入主表后走标准 delete action
    // delete action 内部 clear() 会将主表所有行标记为删除并生成 <d> 删除XML，后端 doDelete 解析执行
    // （不再把 APICODE 当 action type 拼接，避免 unknown action type）
    doDelete(btn, ext, row) {
      var item = row || this.citem;
      if (!item || !item.ID) { this.$Message('请先选择记录'); return }
      var self = this;
      this.$Confirm(btn.POPTIPTEXT || '确定删除？', '提示').then(function() {
        var mainPath = self.formMainPath || 'MAIN';
        self.$store.commit(self.storeName + '/INIT', { paths: [mainPath] });
        self.$store.commit(self.storeName + '/ADD', { path: mainPath, item: Object.assign({}, item) });
        self.$callAction({
          action: self.storeName + '/delete',
          successText: '删除成功',
          successCall: function() { if (self.$refs.list) self.$refs.list.query(1); }
        });
      });
    },
    // 打开表单弹窗
    doOpenForm(btn, ext, row) {
      // 根据按钮指定的 formPageCode 切换表单页面
      var formPageCode = (ext && ext.formPageCode) || '';
      // 判断目标表单页面的 PAGETYPE，如果是 select 则走选入逻辑
      var fpc = this.getFormPageConfig(formPageCode);
      if (fpc && fpc.PAGETYPE === 'select') {
        // select 页面：打开选入弹窗
        var selExt = {
          selectPageCode: formPageCode,
          selectMode: (ext && ext.selectMode) || 'single',
          selectTarget: (ext && ext.selectTarget) || '',
          fieldMap: (ext && ext.fieldMap) || ''
        };
        this.doOpenSelector(btn, selExt, row, { row: row, ext: selExt, btn: btn });
        return;
      }
      this.currentFormPageCode = formPageCode;
      if (ext && ext.openMode === 'edit') {
        if (!row && !this.citem) { this.$Message('请先选择记录'); return }
        this.clickRow(row || this.citem);
      } else {
        this.add();
      }
    },
    // 获取指定 pageCode 的页面配置（考虑子页面中的引用页面）
    getFormPageConfig(formPageCode) {
      if (!formPageCode) return this.formPageConfig;
      if (!this.moduleData || !this.moduleData.MODPAGE) return null;
      // 先从当前模块的 MODPAGE 查找
      var found = this.moduleData.MODPAGE.find(function(p) {
        return p.PAGECODE === formPageCode && (p.ISDELETED || 0) === 0;
      });
      if (found) return found;
      // 再从 SUBPAGES 中查找引用页面
      var subPages = this.subPages;
      for (var i = 0; i < subPages.length; i++) {
        var sp = subPages[i];
        if (sp.REFMODULECODE) {
          var refData = this.$store.state.app.modules[sp.REFMODULECODE];
          if (refData && refData.MODPAGE) {
            var refPage = refData.MODPAGE.find(function(p) {
              return p.PAGECODE === formPageCode && (p.ISDELETED || 0) === 0;
            });
            if (refPage) return refPage;
          }
        }
      }
      return null;
    },
    /**
     * 标准接口：打开页面（供扩展 JS 调用）
     * @param {Object} options
     *   - pageCode: 要打开的表单页面编码（空=默认表单）
     *   - mode: 'add'(新增空表单) | 'edit'(编辑，需传 id)
     *   - id: 编辑模式的记录 ID
     *   - row: 当前行数据（设置 citem）
     *   - title: 弹窗标题
     *   - extraParams: 额外参数（传入表单的 extraParams）
     */
    openPage(options) {
      options = options || {};
      this.currentFormPageCode = options.pageCode || '';
      if (options.row) this.citem = options.row;
      if (options.mode === 'edit' || options.id) {
        this.currentId = options.id || (options.row && options.row.ID) || '';
        this.$refs.madd && this.$refs.madd.show();
      } else {
        this.currentId = '';
        this.$refs.madd && this.$refs.madd.show();
      }
    },
    /**
     * 标准接口：打开选入弹窗（供扩展 JS 调用）
     * @param {Object} options
     *   - moduleCode: 选入模块（空=当前模块）
     *   - pageCode: 选入页面编码（空=自动查找 select 页面）
     *   - mode: 'single' | 'multiple'
     *   - target: 选入数据写入的子表路径
     *   - fieldMap: 字段映射 "源=目标,源=目标"
     *   - title: 弹窗标题
     *   - width: 弹窗宽度
     *   - filterParams: 列表过滤参数
     *   - onSelected: 选中回调 (rows) => {}
     */
    openSelector(options) {
      options = options || {};
      var pageCode = options.pageCode || '';
      var targetModCode = options.moduleCode || this.moduleCode;
      // 未指定 pageCode 时，自动查找目标模块中 PAGETYPE='select' 的页面
      if (!pageCode) {
        var targetModData = this.$store.state.app.modules[targetModCode];
        if (targetModData && targetModData.MODPAGE) {
          var selPage = targetModData.MODPAGE.find(function(p) {
            return p.PAGETYPE === 'select' && (p.ISDELETED || 0) === 0;
          });
          if (selPage) pageCode = selPage.PAGECODE;
        }
      }
      this.selConfig = {
        moduleCode: targetModCode,
        selectPageCode: pageCode,
        selectMode: options.mode || 'single',
        filterParams: options.filterParams || null
      };
      this.selTitle = options.title || '选择数据';
      this.selWidth = options.width || 900;
      this.selContext = {
        ext: { selectTarget: options.target, fieldMap: options.fieldMap },
        btn: { BTNNAME: options.title },
        onSelected: options.onSelected
      };
      var self = this;
      this.$nextTick(function() {
        setTimeout(function() {
          if (self.$refs.msel) self.$refs.msel.show();
        }, 50);
      });
    },
    async doOpenSelector(btn, ext, row, context) {
      console.log('[doOpenSelector] ext:', ext, 'btn:', btn);
      var pageCode = ext.selectPageCode || '';
      // 确定目标模块：优先用 ext.selectModule，其次从 SUBPAGES 引用中查找，最后用当前模块
      var targetModCode = ext.selectModule || '';
      if (!targetModCode) {
        var subPages = this.subPages;
        for (var i = 0; i < subPages.length; i++) {
          if (subPages[i].REFMODULECODE && subPages[i].REFPAGECODE === pageCode) {
            targetModCode = subPages[i].REFMODULECODE;
            break;
          }
        }
      }
      if (!targetModCode) targetModCode = this.moduleCode;
      // 确保目标模块已加载（必须 await，否则 moduleData 为空导致 pageConfig 为 null）
      if (!this.$store.state.app.modules[targetModCode]) {
        // eslint-disable-next-line no-restricted-syntax
        await this.$store.dispatch('app/initModule', targetModCode);
      }
      // 未指定 selectPageCode 时，自动查找目标模块中 PAGETYPE='select' 的页面
      if (!pageCode) {
        var targetModData = this.$store.state.app.modules[targetModCode];
        if (targetModData && targetModData.MODPAGE) {
          var selPage = targetModData.MODPAGE.find(function(p) {
            return p.PAGETYPE === 'select' && (p.ISDELETED || 0) === 0;
          });
          if (selPage) pageCode = selPage.PAGECODE;
        }
      }
      this.selConfig = {
        moduleCode: targetModCode,
        selectPageCode: pageCode,
        selectMode: ext.selectMode || 'single',
        filterParams: ext.selectFilter || null
      };
      this.selTitle = btn.BTNNAME || '选择数据';
      this.selWidth = ext.selectWidth || 900;
      this.selContext = context;
      var self = this;
      this.$nextTick(function() {
        setTimeout(function() {
          if (self.$refs.msel) {
            self.$refs.msel.show();
          } else {
            console.warn('[doOpenSelector] $refs.msel 不存在');
          }
        }, 50);
      });
    },
    // 关闭选入弹窗
    // 选择页确认按钮（多选模式）
    handleSelectorConfirm() {
      if (!this.selectedRows || this.selectedRows.length === 0) {
        this.$Message('请至少选择一条记录');
        return;
      }
      this.$emit('list-select', this.selectedRows);
      this.$emit('selector-selected', { rows: this.selectedRows });
    },
    closeSelector() {
      if (this.$refs.msel) this.$refs.msel.hide();
      this.selConfig = null;
      this.selContext = null;
    },
    // 弹窗中 generic-module select 页面行点击（单选模式直接确认）
    onSelRowClick(row) {
      if (!row) return;
      var mode = (this.selConfig && this.selConfig.selectMode) || 'single';
      if (mode === 'single') {
        this.onSelectorSelected([row]);
      }
    },
    // 弹窗中 generic-module 的 selector-selected 事件（参数是 {rows:[...]}）
    onSelSelectorSelected(data) {
      var rows = (data && data.rows) || [];
      this.onSelectorSelected(rows);
    },
    // 选入确认回调
    async onSelectorSelected(rows) {
      var context = this.selContext || {};
      var ext = context.ext || {};
      var target = ext.selectTarget;
      // 选入目标：按字段映射写入子表
      if (target && rows && rows.length > 0) {
        var fieldMap = this.parseFieldMap(ext.fieldMap);
        var storeName = this.storeName;
        rows.forEach(function(row) {
          var item = {};
          if (fieldMap) {
            // 按字段映射：源字段 → 目标字段
            Object.keys(fieldMap).forEach(function(srcField) {
              var dstField = fieldMap[srcField];
              item[dstField] = row[srcField];
            });
          } else {
            // 无映射：直接复制所有字段
            item = Object.assign({}, row);
          }
          this.$store.commit(storeName + '/ADD', { path: target, item: item });
        }.bind(this));
      }
      // emit 事件供外部监听
      if (target) {
        this.$emit('selector-selected', { btn: context.btn, rows: rows, target: target });
      }
      // onSelected 回调（供扩展 JS 的 openSelector 使用）
      if (typeof context.onSelected === 'function') {
        await context.onSelected(rows);
      }
      // afterAction 钩子（支持异步）
      context.rows = rows;
      await this.callBtnHook('afterAction', context.btn || {}, context);
      this.closeSelector();
    },
    // 解析字段映射字符串 "CUSTCODE=CUSTCODE,CUSTNAME=CUSTNAME" → { CUSTCODE: 'CUSTCODE', CUSTNAME: 'CUSTNAME' }
    parseFieldMap(fieldMapStr) {
      if (!fieldMapStr || typeof fieldMapStr !== 'string') return null;
      var map = {};
      fieldMapStr.split(',').forEach(function(pair) {
        var parts = pair.trim().split('=');
        if (parts.length === 2 && parts[0].trim() && parts[1].trim()) {
          map[parts[0].trim()] = parts[1].trim();
        }
      });
      return Object.keys(map).length > 0 ? map : null;
    },
    // 合并按钮 EXTPARAM 中的 extraParams 到普通对象
    // paramsFn 指向扩展 JS 方法名时，调用该方法获取动态参数并合并
    mergeExtraParams(ext, btn, context) {
      var self = this;
      var p = {};
      if (ext && ext.extraParams) {
        Object.keys(ext.extraParams).forEach(function(k) { p[k] = ext.extraParams[k] });
      }
      if (ext && ext.paramsFn && typeof self[ext.paramsFn] === 'function') {
        var dyn = self[ext.paramsFn](btn, context || {});
        if (dyn && typeof dyn === 'object') {
          Object.keys(dyn).forEach(function(k) { p[k] = dyn[k] });
        }
      }
      return p;
    },
    // 调用 API：按 BTNCODE 映射到 store 语义 action（不再把 APICODE 当 action type）
    // - save    -> save（从主表 getXML）
    // - export  -> query(isExport)
    // - flow 类 -> 列表页走 batchXxx（批量操作选中行），表单页走单条 submit/check/...
    // - batch   -> batch（自定义 APICODE + 选中行）
    // - custom  -> call（通用，APICODE + ID + extraParams）
    doCallApi(btn, ext, row, code) {
      var self = this;
      var item = row || this.citem;
      code = code || btn.BTNCODE || '';
      var pageType = this.pageConfig ? this.pageConfig.PAGETYPE : '';
      var isListPage = (pageType === 'list' || pageType === 'review' || pageType === 'select');
      var refresh = function() { if (self.$refs.list) self.$refs.list.query(1); };
      var ctx = {
        row: item,
        ext: ext,
        btn: btn
      };

      // 1. save：走标准 save action
      if (code === 'save') {
        return this.$callAction({
          action: this.storeName + '/save',
          successText: '保存成功',
          successCall: refresh
        });
      }

      // 2. export：走 query + isExport，返回文件路径下载
      if (code === 'export') {
        return this.$callAction({
          action: this.storeName + '/query',
          param: { isExport: true, columns: ext && ext.columns, sumFields: ext && ext.sumFields },
          successCall: function(ret) { if (ret) window.open(getUrl('upload') + ret, '_blank'); }
        });
      }

      // 3. flow 类：列表页批量操作选中行，表单页单条操作当前单据
      var flowBatchMap = {
        submit: 'batchSubmit',
        reSubmit: 'batchReSubmit',
        check: 'batchCheck',
        reCheck: 'batchReCheck',
        verify: 'batchVerify',
        reVerify: 'batchReVerify'
      };
      if (flowBatchMap[code]) {
        if (isListPage) {
          if (!this.selectedRows || this.selectedRows.length === 0) {
            this.$Message('请先选择记录');
            return;
          }
          return this.$callAction({
            action: this.storeName + '/' + flowBatchMap[code],
            param: {
              items: this.selectedRows,
              REMARK: '',
              CHECKID: '',
              CHECKER: '',
              VERIFYID: '',
              VERIFYER: ''
            },
            successText: '操作成功',
            successCall: refresh
          });
        } else {
          var id = this.currentId || (this.citem && this.citem.ID);
          if (!id) { this.$Message('请先选择记录'); return }
          return this.$callAction({
            action: this.storeName + '/' + code,
            param: { ID: id },
            successText: '操作成功',
            successCall: refresh
          });
        }
      }

      // 4. batch：显式批量，用按钮配置的 APICODE + 选中行
      if (code === 'batch' || btn.BTNTYPE === 'batch') {
        if (!this.selectedRows || this.selectedRows.length === 0) {
          this.$Message('请先选择记录');
          return;
        }
        return this.$callAction({
          action: this.storeName + '/batch',
          param: {
            APICODE: btn.APICODE,
            items: this.selectedRows,
            updateFields: (ext && ext.updateFields) || [],
            params: this.mergeExtraParams(ext, btn, ctx)
          },
          successText: '操作成功',
          successCall: refresh
        });
      }

      // 5. custom / 其他：通用 call，传 APICODE + ID + extraParams
      var params = this.mergeExtraParams(ext, btn, ctx);
      if (item && item.ID) params.ID = item.ID;
      return this.$callAction({
        action: this.storeName + '/call',
        param: {
          APICODE: btn.APICODE,
          params: params
        },
        successText: '操作成功',
        successCall: refresh
      });
    },
    handleCrudAction(btn) {
      // 统一用按钮配置的 APICODE 调用，不写死 A04/A07
      if (!btn.APICODE) {
        this.$Message('按钮未配置接口编码');
        return;
      }
      if (btn.APICODE === 'A07') {
        // 删除需二次确认
        if (this.citem && this.citem.ID) {
          var self = this;
          this.$Confirm(btn.POPTIPTEXT || '确定删除？', '提示').then(function() {
            self.$callAction({
              action: self.storeName + '/' + btn.APICODE,
              param: { item: self.citem },
              successCall: function() { self.$refs.list && self.$refs.list.query(1) }
            });
          });
        }
      } else {
        this.$callAction({
          action: this.storeName + '/' + btn.APICODE,
          param: { item: this.citem, btn: btn },
          successText: '操作成功'
        });
      }
    },
    handleFlowAction(btn) {
      this.$callAction({
        action: this.storeName + '/' + btn.APICODE,
        param: { item: this.citem, btn: btn },
        successText: '操作成功'
      });
    },
    handleBatchAction(btn) {
      if (this.selectedRows.length === 0) {
        this.$Message('请先选择记录');
        return;
      }
      this.$callAction({
        action: this.storeName + '/' + btn.APICODE,
        param: { item: this.citem, btn: btn, checks: this.selectedRows },
        successText: '操作成功'
      });
    },
    // 将 filterParams prop 注入到 QQRY DataTable，使 list 子页面查询时自动带上过滤条件
    applyFilterParams() {
      if (!this.filterParams || !this.storeObj) return;
      var helper = this.storeObj.storeHelper;
      if (!helper) return;
      var qqryPath = this.listQqryPath;
      var dt = helper.getTable(qqryPath);
      if (!dt) return;
      var self = this;
      Object.keys(this.filterParams).forEach(function(k) {
        dt.setValue(k, self.filterParams[k]);
      });
    }
  },
  async mounted() {
    console.log('[GenericModule mounted] moduleCode:', this.moduleCode, 'pageCode:', this.pageCode);
    // 1. 确保模块配置已加载
    if (!this.moduleData) {
      console.log('[GenericModule mounted] initModule:', this.moduleCode);
      // eslint-disable-next-line no-restricted-syntax
      await this.$store.dispatch('app/initModule', this.moduleCode);
    }
    console.log('[GenericModule mounted] moduleData:', !!this.moduleData, 'MODPAGE:', this.moduleData && this.moduleData.MODPAGE ? this.moduleData.MODPAGE.length : 'N/A');
    // 2. 创建 Vuex 模块 (等价于 store.js 中 createStore.getStore())
    var storeResult = getGenericStore(this.moduleCode);
    this.storeObj = storeResult;
    this.storeReady = true;
    console.log('[GenericModule mounted] storeReady=true, pageConfig:', this.pageConfig ? this.pageConfig.PAGECODE + '(' + this.pageConfig.PAGETYPE + ')' : 'null');
    // 3. 异步加载模块级 SFC store 扩展 (@/modules/{moduleCode}/store.js)
    applyStoreExtend(this.moduleCode);
    // 4. 注入 filterParams 到 QQRY DataTable（list 子页面过滤）
    this.applyFilterParams();
    // 5. 映射查询字段到 this (QQRY/QRY 字段，使扩展 JS 可直接用 this.xxx)
    this.mapDataTableFields();
    // 6. SFC 页面加载在线模块
    if (this.isSfcPage) {
      this.loadSfcModule();
    }
    // 7. 加载页面级扩展 JS mixin
    this.loadExtendMixin();
    // 8. 加载 SFC slot 扩展组件
    this.loadSlotComponents();
  },
  activated() {
    // keep-alive 恢复时重新加载扩展JS
    // 保存时已清了 moduleCache，此处 loadExtendMixin 会从数据库拉最新代码
    // 若缓存未被清（代码没改），则直接用缓存，不会重复请求
    this.loadExtendMixin();
    applyStoreExtend(this.moduleCode);
  },
  watch: {
    filterParams: {
      handler() {
        this.applyFilterParams();
        // 参数变化后重新查询
        if (this.storeReady && this.$refs.list) {
          this.$refs.list.query(1);
        }
      },
      deep: true
    },
    // SFC 路径变化时重新加载
    'pageConfig.SFCMODULEPATH'() {
      if (this.isSfcPage) {
        this.loadSfcModule();
      }
    },
    // PAGECONFIG 变化时重新加载 slot 扩展组件
    'pageConfig.PAGECONFIG'() {
      this.slotComponents = {};
      this.loadSlotComponents();
    }
  }
};
</script>
<style lang="less" scoped>
.generic-module-root {
  height: 100%;
}
.generic-form-page {
  height: 100%;
  overflow: auto;
}
.generic-selector-wrap {
  display: flex;
  flex-direction: column;
  height: 100%;
}
.generic-selector-header {
  display: flex;
  align-items: center;
  padding: 8px 12px;
  border-bottom: 1px solid #e8e8e8;
  flex-shrink: 0;
}
.generic-selector-title {
  font-size: 14px;
  font-weight: bold;
  line-height: 28px;
}
.generic-selector-body {
  flex: 1;
  overflow: auto;
  min-height: 300px;
}
.generic-selector-footer {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  border-top: 1px solid #e8e8e8;
  flex-shrink: 0;
}
</style>

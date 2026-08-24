<template>
  <div class="module-wizard">
    <!-- 步骤条（横跨全宽）-->
    <div class="mw-steps">
      <div v-for="(step, idx) in steps" :key="idx" class="mw-step" :class="{ 'mw-step-active': currentStep === idx, 'mw-step-done': currentStep > idx || stepDone[idx] }" @click="goToStep(idx)">
        <span class="mw-step-num">{{ idx + 1 }}</span>
        <span class="mw-step-label">{{ step }}</span>
      </div>
    </div>

    <!-- 一键生成全部入口 -->
    <div class="mw-allgen">
      <input type="text" v-model="allGenPrompt" class="mw-allgen-input" placeholder="描述需求，如：开发一个LIMS样品管理模块，含样品登记、收样、流转、归还" :disabled="loading" />
      <Button color="green" :loading="loading" @click="aiGenerateAll">🚀 AI 一键生成全部</Button>
      <span class="mw-allgen-tip" v-if="!loading">一次描述，AI 自动按 6 步顺序生成全部配置，可在右侧对话继续精修</span>
    </div>

    <!-- 主体: 左右分栏 -->
    <div class="mw-content">
      <!-- 左: 向导表单 -->
      <div class="mw-left">
        <div class="mw-body">
          <!-- Step 1: 基本信息 -->
          <div v-if="currentStep === 0">
            <Form :label-width="100">
              <FormItem label="模块编码" single>
                <input type="text" v-model="form.moduleCode" placeholder="如 R02_M07 (大写字母+下划线)" />
              </FormItem>
              <FormItem label="模块名称" single>
                <input type="text" v-model="form.moduleName" placeholder="如 物流管理" />
              </FormItem>
              <FormItem label="父菜单" single>
                <Select v-model="form.parentFuncId" :datas="wizardMenuOptions" placeholder="选择父菜单" :filterable="true" style="width:100%" />
              </FormItem>
              <FormItem label="业务分类" single>
                <Select v-model="form.bizCategory" :datas="bizCategoryOptions" placeholder="选择分类" />
              </FormItem>
            </Form>
            <div class="mw-hint">可选：从模板开始 — AI 读取模板结构后基于模板做增量修改生成配置</div>
            <Form :label-width="100">
              <FormItem label="搜索模板" single>
                <div style="display:flex;gap:8px">
                  <input type="text" v-model="templateKeyword" placeholder="输入关键词搜索模板" @keyup.enter="searchTemplates" style="flex:1" />
                  <Button size="s" @click="searchTemplates" :loading="templateSearching">搜索</Button>
                </div>
              </FormItem>
              <FormItem v-if="wizardTemplateOptions.length" label="选择模板" single>
                <Select v-model="selectedTemplate" :datas="wizardTemplateOptions" placeholder="选择参考模板" :filterable="true" style="width:100%" @input="onTemplateSelected" />
              </FormItem>
              <FormItem v-if="selectedTemplateName" label="参考模板" single>
                <span style="color:#2F54EB">{{ selectedTemplateName }}</span>
                <Button size="s" style="margin-left:8px" @click="clearTemplate">清除</Button>
              </FormItem>
            </Form>
          </div>

          <!-- Step 2: 数据模型 -->
          <div v-if="currentStep === 1">
            <Form :label-width="100">
              <FormItem label="创建方式" single>
                <Select v-model="form.tableMode" :datas="tableModeOptions" />
              </FormItem>
              <template v-if="form.tableMode === 'new'">
                <FormItem label="表名" single>
                  <input type="text" v-model="form.tableName" placeholder="如 TBS_LOGISTICS (大写)" />
                </FormItem>
                <FormItem label="表注释" single>
                  <input type="text" v-model="form.tableComment" placeholder="如 物流信息表" />
                </FormItem>
              </template>
              <template v-else>
                <FormItem label="已有资源" single>
                  <Select v-model="form.existingResource" :datas="wizardResourceOptions" placeholder="选择已有资源" :filterable="true" style="width:100%" />
                </FormItem>
              </template>
            </Form>
            <div class="mw-hint">字段列表（AI 生成后自动回填，可手动修改）</div>
            <Form :label-width="100">
              <FormItem label="列表字段" single>
                <input type="text" v-model="form.listFields" placeholder="字段名逗号分隔，AI 生成后回填" />
              </FormItem>
              <FormItem label="编辑字段" single>
                <input type="text" v-model="form.editFields" placeholder="字段名逗号分隔，AI 生成后回填" />
              </FormItem>
            </Form>
          </div>

          <!-- Step 3: 视图与查询 -->
          <div v-if="currentStep === 2">
            <div class="mw-hint">AI 会根据第 2 步的物理表字段生成 DATAVIEW 视图 + 过滤器</div>
            <Form :label-width="100">
              <FormItem label="查询字段" single>
                <input type="text" v-model="form.queryFields" placeholder="AI 生成视图后回填，可手动修改" />
              </FormItem>
            </Form>
          </div>

          <!-- Step 4: 接口配置 -->
          <div v-if="currentStep === 3">
            <Form :label-width="100">
              <FormItem label="审批流" single>
                <Select v-model="form.flowCode" :datas="flowCodeOptions" />
              </FormItem>
            </Form>
            <div class="mw-hint">AI 将生成标准接口：A01(查询) A02(打开) A04(保存) A07(删除)<template v-if="form.flowCode"> + 审批流接口</template>。自定义业务操作可用 SQL 脚本接口(define_sql_api)、C# 脚本接口(define_script_api)或多步编排接口(define_script_flow_api)</div>
          </div>

          <!-- Step 5: UI配置 -->
          <div v-if="currentStep === 4">
            <div class="mw-hint">AI 会为第 2/3 步的字段生成 UI 配置（列表列 + 表单控件）</div>
            <Form :label-width="100">
              <FormItem label="表单布局" single>
                <Select v-model="form.formLayout" :datas="formLayoutOptions" />
              </FormItem>
              <FormItem label="高级查询" single>
                <Select v-model="form.enableAdvQuery" :datas="boolOptions" />
              </FormItem>
            </Form>
          </div>

          <!-- Step 6: 菜单注册 -->
          <div v-if="currentStep === 5">
            <Form :label-width="100">
              <FormItem label="菜单名称" single>
                <input type="text" v-model="form.menuName" placeholder="默认使用模块名称" />
              </FormItem>
              <FormItem label="菜单图标" single>
                <input type="text" v-model="form.menuIcon" placeholder="如 h-icon-setting" />
              </FormItem>
              <FormItem label="路由路径" single>
                <input type="text" v-model="form.routePath" placeholder="自动生成，可修改" />
              </FormItem>
            </Form>
          </div>
        </div>

        <!-- AI 生成本步按钮 -->
        <div class="mw-step-actions">
          <Button color="primary" @click="aiGenerateCurrentStep(false)" :loading="loading">✨ AI 生成本步</Button>
          <Button v-if="pendingExecCount > 0" color="green" @click="confirmAndExecute" :loading="executing">
            ✔ 确认并执行本步 ({{ pendingExecCount }})
          </Button>
          <span class="mw-step-tip">{{ stepToolHint }}</span>
        </div>

        <!-- 底部按钮 -->
        <div class="mw-footer">
          <Button v-if="currentStep > 0" @click="prevStep">上一步</Button>
          <Button v-if="currentStep < steps.length - 1" color="primary" @click="nextStep">下一步</Button>
          <Button v-if="currentStep === steps.length - 1" color="green" :loading="generating" @click="generate">生成模块</Button>
          <Button @click="$emit('close')">取消</Button>
        </div>
      </div>

      <!-- 右: AI 对话 + 变更项 -->
      <div class="mw-right">
        <div class="mw-tabs">
          <span :class="{ active: rightTab === 'chat' }" @click="rightTab = 'chat'">AI 对话</span>
          <span :class="{ active: rightTab === 'items' }" @click="rightTab = 'items'">变更项 ({{ items.length }})</span>
        </div>

        <!-- Tab1: 对话区 -->
        <div class="mw-chat" v-show="rightTab === 'chat'">
          <div class="chat-messages" ref="msgBox">
            <div v-if="messages.length === 0" class="chat-empty">
              点击左下「AI 生成本步」，或在下方描述需求让 AI 生成配置。
              <br />支持多轮对话调整，每条生成结果可在「变更项」里确认。
            </div>
            <AiMessageList v-else :messages="messages" scene="wizard" />
            <div v-if="loading" class="asst-thinking">思考中...</div>
          </div>
          <AiInput
            :loading="loading"
            placeholder="描述本步需求，如：创建物流表，含 CUSTCODE/CUSTNAME/STATE/REMARK"
            @send="sendCustom"
          />
        </div>

        <!-- Tab2: 变更项列表 -->
        <div class="mw-items" v-show="rightTab === 'items'">
          <div v-if="items.length === 0" class="chat-empty">暂无变更项，点击「AI 生成本步」生成。</div>
          <template v-else>
            <div class="batch-bar">
              <span class="batch-info">{{ draftCount }} 待确认 / {{ items.length }} 项</span>
              <Button size="s" color="green" @click="confirmAll" :disabled="draftCount === 0 || loading">全部确认</Button>
              <Button size="s" color="primary" @click="confirmAndExecute" :loading="executing" :disabled="pendingExecCount === 0">确认并执行 ({{ pendingExecCount }})</Button>
            </div>
            <div class="item-groups">
              <div v-for="g in groupedItems" :key="g.category" class="item-group">
                <div class="group-header">
                  <span class="group-label">{{ g.label }}</span>
                  <span class="group-count">{{ g.items.length }}</span>
                </div>
                <div v-for="it in g.items" :key="it.ID" :class="['item-row', 'st-' + (it.ITEMSTATUS || 'DRAFT').toLowerCase()]">
                  <div class="item-head">
                    <span :class="['item-status', 'st-' + (it.ITEMSTATUS || 'DRAFT').toLowerCase()]">{{ statusLabel(it.ITEMSTATUS) }}</span>
                  </div>
                  <div class="item-rationale" v-if="it.RATIONALE">{{ it.RATIONALE }}</div>
                  <!-- 变更项摘要 + SQL + 确认/拒绝（复用 ChangeItemCard，内含 buildItemSummary） -->
                  <ChangeItemCard
                    :item="it"
                    :show-actions="it.ITEMSTATUS === 'DRAFT'"
                    @confirm="confirm"
                    @reject="reject"
                  />
                  <div class="item-ops" v-if="it.ITEMSTATUS === 'CONFIRMED'">
                    <Button size="s" @click="unconfirm(it)">撤销</Button>
                  </div>
                </div>
              </div>
            </div>
          </template>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import aidev from '@/api/aidev';
import AiClient from '@/utils/ai/AiClient';
import AiMessageList from '@/components/ai/AiMessageList.vue';
import AiInput from '@/components/ai/AiInput.vue';
import ChangeItemCard from '@/components/ai/ChangeItemCard.vue';
import { Constants as M18, mapGetters as m18MapGetters } from '../../store';

export default {
  name: 'ModuleWizard',
  components: { AiMessageList, AiInput, ChangeItemCard },
  data() {
    return {
      currentStep: 0,
      generating: false,
      executing: false,
      steps: ['基本信息', '数据模型', '视图与查询', '接口与页面', 'UI配置', '菜单注册'],
      form: {
        moduleCode: '',
        moduleName: '',
        parentFuncId: '',
        bizCategory: '',
        tableMode: 'new',
        tableName: '',
        tableComment: '',
        existingResource: '',
        listFields: '',
        queryFields: '',
        editFields: '',
        flowCode: '',
        formLayout: 'twocolumn',
        enableAdvQuery: '1',
        menuName: '',
        menuIcon: '',
        routePath: ''
      },
      tableModeOptions: [
        { key: 'new', title: '新建物理表' },
        { key: 'existing', title: '选择已有表' }
      ],
      flowCodeOptions: [
        { key: '', title: '无审批流' },
        { key: '1', title: '提交->审核' },
        { key: '2', title: '提交->审核->审批' }
      ],
      formLayoutOptions: [
        { key: 'twocolumn', title: '两列' },
        { key: 'onecolumn', title: '单列' }
      ],
      boolOptions: [
        { key: '1', title: '启用' },
        { key: '0', title: '禁用' }
      ],
      // 菜单/资源/模板下拉由 m18 store getter 派生（wizardMenuOptions/wizardResourceOptions/wizardTemplateOptions）
      // AI 对话 + 变更项状态
      sessionId: '',
      changesetId: '',
      rightTab: 'chat',
      messages: [],
      msgSeq: 0,
      loading: false,
      items: [],
      allGenPrompt: '',
      stepDone: [false, false, false, false, false, false], // 一键生成时标记各步完成
      // AiClient 实例（SSE wizard 场景，created 中初始化）
      aiClient: null,
      // 标记当前是一键生成全部模式（onDone 回调区分单步/全量）
      allGenMode: false,
      // 模板搜索相关
      templateKeyword: '',
      selectedTemplate: '',
      selectedTemplateName: '',
      templateSearching: false
    };
  },
  computed: {
    // 向导下拉（菜单/资源/模板）由 m18 store getter 派生（filter+map 已收口到 store）
    ...m18MapGetters(['wizardMenuOptions', 'wizardResourceOptions', 'wizardTemplateOptions']),
    // 业务分类下拉：数据字典「业务分类」(D0707)
    bizCategoryOptions() {
      var d = (this.$store.state.app && this.$store.state.app.dicts['业务分类']) || {};
      return Object.keys(d).map(k => ({ key: k, title: d[k] }));
    },
    moduleCode() {
      return this.form.moduleCode;
    },
    // 当前步骤允许的工具（前端镜像，用于 UI 提示，后端 stepToolMap 是权威）
    stepTools() {
      var map = {
        0: ['register_module', 'search_module_template', 'read_module_template'],
        1: ['create_physical_table', 'configure_resource_field'],
        2: ['define_dataview', 'configure_resource_field', 'define_filter'],
        3: ['define_api', 'define_sql_api', 'define_script_api', 'define_script_flow_api', 'define_filter', 'define_page', 'define_button'],
        4: ['configure_ui_field', 'create_dict'],
        5: ['create_menu', 'create_funcpoints']
      };
      return map[this.currentStep] || [];
    },
    stepToolHint() {
      var labels = {
        register_module: '注册模块',
        search_module_template: '搜模板',
        read_module_template: '读模板',
        create_physical_table: '建物理表',
        configure_resource_field: '配字段',
        define_dataview: '定义视图',
        define_filter: '定义过滤器',
        define_api: '定义接口',
        define_sql_api: 'SQL接口',
        define_script_api: '脚本接口',
        define_script_flow_api: '编排接口',
        define_page: '配页面',
        define_button: '配按钮',
        configure_ui_field: '配UI',
        create_dict: '建字典',
        create_menu: '建菜单',
        create_funcpoints: '建权限'
      };
      return '本步可生成：' + this.stepTools.map(function(t) { return labels[t] || t }).join(' / ');
    },
    draftCount() {
      return this.items.filter(function(it) { return it.ITEMSTATUS === 'DRAFT' }).length;
    },
    // 待执行数(DRAFT+CONFIRMED): 步骤强制要求进入下一步前必须为 0
    pendingExecCount() {
      return this.items.filter(function(it) {
        return it.ITEMSTATUS === 'DRAFT' || it.ITEMSTATUS === 'CONFIRMED';
      }).length;
    },
    // 按 CATEGORY 分组（固定顺序）
    groupedItems() {
      var order = ['physical_table', 'dataview', 'field', 'ui', 'dict', 'filter', 'module', 'api', 'page', 'button', 'menu', 'permission', 'billflow', 'other'];
      var labels = { physical_table: '物理表', dataview: '数据视图', field: '字段定义', ui: '界面配置', dict: '字典', filter: '过滤器', module: '模块', api: '接口', page: '页面配置', button: '按钮配置', menu: '菜单', permission: '权限', billflow: '审批流', other: '其他' };
      var groups = {};
      this.items.forEach(function(it) {
        var cat = it.CATEGORY || 'other';
        if (!groups[cat]) groups[cat] = [];
        groups[cat].push(it);
      });
      return order
        .map(function(cat) { return { category: cat, label: labels[cat] || cat, items: groups[cat] || [] } })
        .filter(function(g) { return g.items.length > 0 });
    }
  },
  watch: {
    'form.moduleName'(val) {
      if (!this.form.menuName) this.form.menuName = val;
    },
    'form.moduleCode'(val) {
      if (val && this.form.bizCategory) {
        var code = val.toLowerCase().replace(/_/g, '/');
        this.form.routePath = '/' + this.form.bizCategory + '/' + code.split('/').pop() + '/main';
      }
    }
  },
  created() {
    // 创建 AiClient（wizard 场景走 SSE），回调按 type 更新 messages/items/steps
    this.aiClient = new AiClient({
      scene: 'wizard',
      onBlock: (b) => this.onWizardBlock(b),
      onItem: (item) => this.onWizardItem(item),
      onStep: (stepKey, status, toolName, block) => this.onWizardStep(stepKey, status, toolName, block),
      onError: (msg) => this.appendAssistantText('⚠️ ' + (msg || '生成失败')),
      onDone: (b) => this.onWizardDone(b)
    });
  },
  async mounted() {
    await this.loadMenus();
    await this.loadResources();
    await this.initWizardSession();
  },
  methods: {
    // 菜单/资源列表：m18 store action 取数 + commit，getter 自动派生下拉项
    async loadMenus() {
      try {
        await this.$callAction({ action: M18.STORE_NAME + '/loadWizardMenus', isBusy: false });
      } catch (e) {
        // ignore
      }
    },
    async loadResources() {
      try {
        await this.$callAction({ action: M18.STORE_NAME + '/loadWizardResources', isBusy: false });
      } catch (e) {
        // ignore
      }
    },
    // 搜索模板市场（m18 store action → state.wizardTemplates → getter 派生下拉项）
    async searchTemplates() {
      var kw = (this.templateKeyword || '').trim();
      if (!kw) return;
      this.templateSearching = true;
      try {
        var rows = await this.$callAction({
          action: M18.STORE_NAME + '/searchWizardTemplates',
          param: { keyword: kw },
          isBusy: false,
        });
        if (!rows || rows.length === 0) this.$Message('未找到匹配模板');
      } catch (e) {
        // ignore（$callAction 已弹错误提示）
      } finally {
        this.templateSearching = false;
      }
    },
    onTemplateSelected(code) {
      if (!code) { this.selectedTemplateName = ''; return }
      var opt = this.wizardTemplateOptions.find(function(t) { return t.key === code });
      this.selectedTemplateName = opt ? opt.title.split(' (')[0] : code;
    },
    clearTemplate() {
      this.selectedTemplate = '';
      this.selectedTemplateName = '';
    },
    // 创建向导会话（6 步共享 sessionId/changesetId）
    async initWizardSession() {
      try {
        var ret = await aidev.openWizardSession();
        var d = (ret && ret.Data) || ret || {};
        this.sessionId = d.sessionId || '';
        this.changesetId = d.changesetId || '';
      } catch (e) {
        console.warn('[Wizard] 创建会话失败', e);
      }
    },
    prevStep() {
      if (this.currentStep > 0) this.currentStep--;
    },
    nextStep() {
      if (this.currentStep === 0) {
        if (!this.form.moduleCode) { this.$Message('请输入模块编码'); return }
        if (!this.form.moduleName) { this.$Message('请输入模块名称'); return }
      }
      if (this.currentStep === 1) {
        if (this.form.tableMode === 'new' && !this.form.tableName) { this.$Message('请输入表名'); return }
        if (this.form.tableMode === 'existing' && !this.form.existingResource) { this.$Message('请选择已有资源'); return }
      }
      if (this.currentStep < this.steps.length - 1) this.currentStep++;
    },
    goToStep(idx) {
      // 允许点步骤条跳转（已填前置信息的步骤）
      if (idx <= this.currentStep) this.currentStep = idx;
    },
    // 构造当前步的默认 AI 生成消息（基于已填表单）
    buildStepPrompt() {
      var f = this.form;
      switch (this.currentStep) {
        case 0:
          if (this.selectedTemplate) return '参考模板 ' + this.selectedTemplate + '（' + this.selectedTemplateName + '）创建模块 ' + (f.moduleCode || '') + '（' + (f.moduleName || '') + '），请先读取模板 SCRIPT 学习其元数据组织方式，然后基于模板结构做增量修改生成当前模块配置';
          if (f.moduleCode && f.moduleName) return '注册模块 ' + f.moduleCode + '（' + f.moduleName + '）';
          return '我要创建一个新模块，请帮我规划模块编码和名称';
        case 1:
          if (f.tableName) return '创建物理表 ' + f.tableName + (f.tableComment ? '（' + f.tableComment + '）' : '') + '，请设计合理的字段（含 ID 主键和基础字段）';
          if (f.moduleName) return '为模块「' + f.moduleName + '」设计物理表结构，请帮我确定表名和字段';
          return '请帮我设计数据表结构';
        case 2:
          return '为表 ' + (f.tableName || '') + ' 定义 DATAVIEW 视图（VCK），包含列表和查询需要的字段，并定义 F00 单条查询和 F01 列表查询过滤器';
        case 3:
          return '为模块 ' + (f.moduleCode || '') + ' 定义标准接口 A01(查询)/A02(打开)/A04(保存)/A07(删除)' + (f.flowCode ? '，并配审批流接口' : '') + '。如有自定义业务操作（如状态流转、批量更新、计算回写），用 define_sql_api 配 SQL 脚本接口；复杂逻辑用 define_script_api 配 C# 脚本接口。然后定义页面：main 列表页(pageType=list, queryApiCode=A01, pageConfig 配 {"defaultFormPageCode":"form"}) + form 表单页(pageType=form, openApiCode=A02, saveApiCode=A04)。再配按钮：列表页 header 加"添加"(btnCode=add, apiCode=A04, permCode=' + (f.moduleCode || '') + '/A04)，表单页 footer 加"保存"(btnCode=save, apiCode=A04)和"取消"(btnCode=cancel)' + (f.flowCode ? '。审批流按钮无需配置，系统按 FLOWCODE 自动生成' : '');
        case 4:
          return '为前面步骤产出的字段配置 UI（列表列 + 表单控件）';
        case 5:
          return '为模块 ' + (f.moduleCode || '') + ' 创建菜单（父菜单ID=' + (f.parentFuncId || '') + '，菜单名=' + (f.menuName || f.moduleName || '') + '）和功能点权限';
        default:
          return '请生成本步配置';
      }
    },
    // AI 生成本步（用默认 prompt）
    aiGenerateCurrentStep() {
      if (!this.sessionId) {
        this.$Message('会话未就绪，请稍候');
        return;
      }
      if (this.loading) return;
      var msg = this.buildStepPrompt();
      this.sendStep(msg);
    },
    // 用户在 AiInput 输入后发送（自定义描述，text 由 AiInput 传入）
    sendCustom(text) {
      var msg = (text || '').trim();
      if (!msg || this.loading) return;
      this.sendStep(msg);
    },
    // 核心 SSE 调用：调 AiClient.sendWizardStep，处理流式事件
    async sendStep(message) {
      this.allGenMode = false;
      this.messages.push({ id: 'm_' + (this.msgSeq++), role: 'user', blocks: [{ type: 'text', text: message }] });
      this.messages.push({ id: 'm_' + (this.msgSeq++), role: 'assistant', blocks: [] });
      this.loading = true;
      this.rightTab = 'chat';
      this.$nextTick(() => this.scrollToBottom());
      try {
        await this.aiClient.sendWizardStep(this.sessionId, this.currentStep, JSON.stringify(this.form), message);
      } catch (e) {
        this.appendAssistantText('⚠️ 生成失败: ' + (e.message || e));
      } finally {
        this.loading = false;
      }
    },
    // AI 一键生成全部 6 步：用户描述一次需求，后端连续生成
    aiGenerateAll() {
      if (!this.sessionId) {
        this.$Message('会话未就绪，请稍候');
        return;
      }
      if (this.loading) return;
      var msg = this.allGenPrompt.trim();
      if (!msg) {
        // 未填需求时用表单上下文兜底
        var f = this.form;
        msg = (f.moduleName ? '开发' + f.moduleName + '模块' : '开发一个新业务模块') + '，请按向导6步顺序生成全部配置（基本信息/数据模型/视图查询/接口/UI/菜单）';
      }
      this.sendAll(msg);
    },
    // 一键生成全部的 SSE 调用：事件带 step 字段，自动推进步骤条
    async sendAll(message) {
      this.allGenMode = true;
      this.stepDone = [false, false, false, false, false, false];
      this.messages = [];
      this.items = [];
      this.messages.push({ id: 'm_' + (this.msgSeq++), role: 'user', blocks: [{ type: 'text', text: message }] });
      this.messages.push({ id: 'm_' + (this.msgSeq++), role: 'assistant', blocks: [] });
      this.loading = true;
      this.rightTab = 'chat';
      this.currentStep = 0;
      this.$nextTick(() => this.scrollToBottom());
      try {
        await this.aiClient.sendWizardAll(this.sessionId, JSON.stringify(this.form), message);
      } catch (e) {
        this.appendAssistantText('⚠️ 生成失败: ' + (e.message || e));
      } finally {
        this.loading = false;
      }
    },
    // ============ AiClient 回调（SSE 事件分发）============
    // onBlock: 处理 text/tool_call/tool_result 等 block 事件（AiClient.handleBlock 透传）
    onWizardBlock(b) {
      if (!b || !b.type) return;
      switch (b.type) {
        case 'text':
          this.appendAssistantText(b.text || '');
          break;
        case 'tool_call':
          this.pushToolCall({ tool: b.tool, args: b.args ? (typeof b.args === 'string' ? b.args : JSON.stringify(b.args)) : '' });
          break;
        case 'tool_result':
          this.updateToolResult({ tool: b.tool, summary: b.summary || '' });
          break;
        default:
          break;
      }
    },
    // onItem: 新变更项前置 + 回写表单
    onWizardItem(it) {
      if (!it) return;
      this.items.unshift({
        ID: it.id,
        CHANGESETID: it.changesetId,
        ITEMSEQ: it.seq,
        CATEGORY: it.category,
        ACTION: it.action,
        TOOL: it.tool,
        TARGET: it.target,
        SQLCONTENT: it.sqlContent || '',
        METADATA: it.metadata || '',
        RATIONALE: it.rationale || '',
        WARNINGS: it.warnings || '',
        ITEMSTATUS: it.status || 'DRAFT',
        ISDELETED: 0
      });
      this.applyItemToForm(it);
    },
    // onStep: step_start 事件推进步骤条 + 追加分步提示文本
    onWizardStep(stepKey, status, toolName, block) {
      if (status === 'start' && block && block.type === 'step_start') {
        this.currentStep = stepKey;
        if (stepKey > 0) this.$set(this.stepDone, stepKey - 1, true);
        this.appendAssistantText('\n\n- 开始第 ' + (stepKey + 1) + ' 步：' + (block.label || '') + ' -\n');
      }
    },
    // onDone: 处理 changeSetId/warnings，一键生成全部模式额外推进步骤条 + 完成提示
    onWizardDone(b) {
      if (b && b.changeSetId) this.changesetId = b.changeSetId;
      if (b && b.warnings && b.warnings.length) {
        this.$error('部分警告: ' + b.warnings.join('; '));
      }
      if (this.allGenMode) {
        // 全部完成，标记最后一步done，步骤条停在末步
        this.$set(this.stepDone, 5, true);
        this.currentStep = 5;
        this.appendAssistantText('\n\n✅ 6 步全部生成完成，共 ' + (b.newItemCount || 0) + ' 个变更项。请在「变更项」Tab 逐条确认，确认后点「生成模块」落库。');
      }
    },
    // 解析变更项 metadata 回写左侧表单
    applyItemToForm(item) {
      if (!item || !item.metadata) return;
      var meta;
      try { meta = typeof item.metadata === 'string' ? JSON.parse(item.metadata) : item.metadata } catch (e) { return }
      if (!meta) return;
      var cat = item.category;
      if (cat === 'physical_table') {
        var res = meta.resource || {};
        if (res.TABLENAME) this.form.tableName = res.TABLENAME;
        var fields = (meta.resfields || []).map(function(f) { return f.FIELDNAME }).filter(Boolean);
        if (fields.length) {
          this.form.listFields = fields.join(',');
          this.form.editFields = fields.join(',');
        }
      } else if (cat === 'dataview') {
        var dfields = (meta.resfields || []).map(function(f) { return f.FIELDNAME }).filter(Boolean);
        if (dfields.length) {
          // 补充到查询字段（不覆盖已有）
          var exist = this.form.queryFields ? this.form.queryFields.split(',').filter(Boolean) : [];
          dfields.forEach(function(fn) { if (exist.indexOf(fn) < 0) exist.push(fn); });
          this.form.queryFields = exist.join(',');
        }
      } else if (cat === 'module') {
        var m = meta.module || {};
        if (m.MODULECODE) this.form.moduleCode = m.MODULECODE;
        if (m.MODULENAME) this.form.moduleName = m.MODULENAME;
      } else if (cat === 'menu') {
        var f2 = meta.func || meta.menu || {};
        if (f2.FUNCNAME) this.form.menuName = f2.FUNCNAME;
      }
    },
    // ============ 变更项确认操作（复用 workspace.vue 模式）============
    async confirm(it) {
      try {
        await aidev.confirmItem(it.ID);
        it.ITEMSTATUS = 'CONFIRMED';
      } catch (e) {
        this.$error('确认失败: ' + (e.message || e));
      }
    },
    async reject(it) {
      try {
        await aidev.rejectItem(it.ID);
        it.ITEMSTATUS = 'REJECTED';
      } catch (e) {
        this.$error('拒绝失败: ' + (e.message || e));
      }
    },
    async unconfirm(it) {
      try {
        await aidev.unconfirmItem(it.ID);
        it.ITEMSTATUS = 'DRAFT';
      } catch (e) {
        this.$error('撤销失败: ' + (e.message || e));
      }
    },
    async confirmAll() {
      var drafts = this.items.filter(function(it) { return it.ITEMSTATUS === 'DRAFT' });
      if (drafts.length === 0) return;
      try {
        for (var i = 0; i < drafts.length; i++) {
          await aidev.confirmItem(drafts[i].ID);
          drafts[i].ITEMSTATUS = 'CONFIRMED';
        }
        this.$alert('已确认 ' + drafts.length + ' 项');
      } catch (e) {
        this.$error('批量确认失败: ' + (e.message || e));
      }
    },
    // 确认并执行本步: confirmAll + executeConfirmed, 执行成功后本地状态同步为 EXECUTED
    // 步骤强制(后端 enforcePreviousExecuted): 下一步生成前必须无 DRAFT/CONFIRMED 项
    async confirmAndExecute() {
      this.executing = true;
      try {
        await this.confirmAll();
        var confirmedCount = this.items.filter(function(it) { return it.ITEMSTATUS === 'CONFIRMED' }).length;
        if (confirmedCount === 0) { this.$Message('没有待执行的变更项'); return; }
        var ret = await aidev.executeConfirmed(this.sessionId);
        if (ret && ret.success) {
          // 同步本地状态: CONFIRMED → EXECUTED(后端已同事务更新)
          this.items.forEach(function(it) {
            if (it.ITEMSTATUS === 'CONFIRMED') it.ITEMSTATUS = 'EXECUTED';
          });
          this.$alert('执行成功！共执行 ' + (ret.itemCount || confirmedCount) + ' 个变更项，可以进入下一步。');
        } else {
          this.$error('执行失败：' + ((ret && ret.errorMsg) || '未知错误'));
        }
      } catch (e) {
        this.$error('执行失败: ' + (e.message || e));
      } finally {
        this.executing = false;
      }
    },
    // ============ 生成模块（末步按钮）============
    async generate() {
      this.generating = true;
      try {
        // 先确认全部 DRAFT 变更项
        await this.confirmAll();
        var confirmedCount = this.items.filter(function(it) { return it.ITEMSTATUS === 'CONFIRMED' }).length;
        if (confirmedCount > 0) {
          // AI 路径：执行已确认变更项统一落库
          var ret = await aidev.executeConfirmed(this.sessionId);
          if (ret && ret.success) {
            this.$alert('模块创建成功！共执行 ' + (ret.itemCount || confirmedCount) + ' 个变更项。可通过菜单访问或进入模块配置详细调整。');
            this.$emit('done', { moduleCode: this.form.moduleCode, moduleName: this.form.moduleName });
          } else {
            this.$error('执行失败：' + (ret && ret.errorMsg));
          }
        } else {
          // 手动填表兜底路径（未用 AI 时走原 XML 落库）
          await this.generateManual();
        }
      } catch (e) {
        this.$error('创建失败: ' + (e.message || e));
      } finally {
        this.generating = false;
      }
    },
    // 手动填表兜底：未用 AI 时的落库逻辑（严禁手拼 XML；字段对象交给 m18 store 的 DataTable 生成 XML）
    async generateManual() {
      var mc = this.form.moduleCode;
      var mn = this.form.moduleName;
      // 1. 创建模块（MAIN=VSS_MOUDLE DataTable）
      await this.$callAction({
        action: M18.STORE_NAME + '/createModuleBare',
        param: { moduleCode: mc, moduleName: mn },
        isBusy: false,
      });
      // 2. 刷新模块缓存
      // eslint-disable-next-line no-restricted-syntax
      await this.$store.dispatch('app/initModule', mc);
      // 3. 创建页面配置 + 按钮（MODPAGE/MODBUTTON DataTable）
      var pageId = this._genId();
      var formPageId = this._genId();
      await this.$callAction({
        action: M18.STORE_NAME + '/saveModulePagesBare',
        param: {
          pages: [
            { ID: pageId, MODULECODE: mc, PAGECODE: 'main', PAGENAME: mn + '列表', PAGETYPE: 'list', COMPONENTTYPE: 'standard', PAGECONFIG: '{"QRYPATH":"QRY","QQRYSPATH":"QQRY"}', ISDELETED: '0', SORTNO: 1 },
            { ID: formPageId, MODULECODE: mc, PAGECODE: 'form', PAGENAME: mn + '编辑', PAGETYPE: 'form', COMPONENTTYPE: 'standard', PAGECONFIG: '{"MAINPATH":"MAIN"}', ISDELETED: '0', SORTNO: 2 },
          ],
          buttons: [
            { ID: this._genId(), PAGEID: pageId, MODULECODE: mc, BTNNAME: '添加', BTNTYPE: 'crud', BTNAREA: 'header', APICODE: 'A04', ICON: 'h-icon-plus', COLOR: 'primary', ISDELETED: '0', SORTNO: 1 },
            { ID: this._genId(), PAGEID: formPageId, MODULECODE: mc, BTNNAME: '保存', BTNTYPE: 'crud', BTNAREA: 'header', APICODE: 'A04', ICON: '', COLOR: 'primary', ISDELETED: '0', SORTNO: 1 },
            { ID: this._genId(), PAGEID: formPageId, MODULECODE: mc, BTNNAME: '删除', BTNTYPE: 'crud', BTNAREA: 'header', APICODE: 'A07', ICON: '', COLOR: 'red', ISDELETED: '0', SORTNO: 2 },
          ],
        },
        isBusy: false,
      });
      // 4. 创建菜单（FUNC=VSS_FUNC DataTable）
      if (this.form.parentFuncId) {
        await this.$callAction({
          action: M18.STORE_NAME + '/createMenuBare',
          param: {
            funcCode: mc,
            funcName: this.form.menuName || mn,
            parentFuncId: this.form.parentFuncId,
            moduleCode: mc,
          },
          isBusy: false,
        });
      }
      this.$alert('模块创建成功！可通过菜单访问或进入模块配置详细调整。');
      this.$emit('done', { moduleCode: mc, moduleName: mn });
    },
    _genId() {
      return 'xxxxxxxxxxxx4xxxyxxxxxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        var r = Math.random() * 16 | 0;
        var v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
      });
    },
    // ============ 对话 blocks 消息操作（AiClient 不维护 messages，由调用方更新）============
    appendAssistantText(text) {
      var last = this.messages[this.messages.length - 1];
      if (last && last.role === 'assistant') {
        var blocks = last.blocks;
        if (blocks.length && blocks[blocks.length - 1].type === 'text') {
          blocks[blocks.length - 1].text += text;
        } else {
          blocks.push({ type: 'text', text: text });
        }
      }
      this.$nextTick(() => this.scrollToBottom());
    },
    pushToolCall(b) {
      var last = this.messages[this.messages.length - 1];
      if (!last || last.role !== 'assistant') return;
      last.blocks.push({ type: 'tool_call', tool: b.tool, args: b.args || '', summary: '执行中…' });
      this.$nextTick(() => this.scrollToBottom());
    },
    updateToolResult(b) {
      var last = this.messages[this.messages.length - 1];
      if (!last || last.role !== 'assistant') return;
      for (var i = last.blocks.length - 1; i >= 0; i--) {
        if (last.blocks[i].type === 'tool_call' && last.blocks[i].tool === b.tool) {
          last.blocks[i].summary = b.summary;
          break;
        }
      }
      this.$nextTick(() => this.scrollToBottom());
    },
    scrollToBottom() {
      var box = this.$refs.msgBox;
      if (box) box.scrollTop = box.scrollHeight;
    },
    statusLabel(s) {
      var map = { DRAFT: '待确认', CONFIRMED: '已确认', REJECTED: '已拒绝', MERGED: '已合并', EXECUTED: '已执行' };
      return map[s] || s;
    }
  }
};
</script>

<style lang="less" scoped>
.module-wizard {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 16px;
}
.mw-steps {
  display: flex;
  justify-content: center;
  gap: 24px;
  padding: 12px 0;
  border-bottom: 1px solid #f0f0f0;
  margin-bottom: 12px;
  flex-shrink: 0;
}
.mw-step {
  display: flex;
  align-items: center;
  gap: 8px;
  color: #BFBFBF;
  cursor: default;
}
.mw-step-active {
  color: #2F54EB;
  .mw-step-num { background: #2F54EB; color: #fff; }
}
.mw-step-done {
  color: #52C41A;
  cursor: pointer;
  .mw-step-num { background: #52C41A; color: #fff; }
}
.mw-step-num {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background: #F0F0F0;
  color: #8C8C8C;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  flex-shrink: 0;
}
.mw-step-label { font-size: 14px; }
/* 一键生成入口 */
.mw-allgen {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 12px;
  background: linear-gradient(90deg, #f6ffed 0%, #f0f5ff 100%);
  border: 1px solid #b7eb8f;
  border-radius: 6px;
  margin-bottom: 12px;
  flex-shrink: 0;
}
.mw-allgen-input {
  flex: 1;
  height: 32px;
  border: 1px solid #d9d9d9;
  border-radius: 4px;
  padding: 0 10px;
  font-size: 13px;
}
.mw-allgen-tip { font-size: 12px; color: #52c41a; }
.mw-content {
  flex: 1;
  display: flex;
  gap: 12px;
  overflow: hidden;
}
/* 左侧向导表单 */
.mw-left {
  width: 58%;
  display: flex;
  flex-direction: column;
  border: 1px solid #e8e8e8;
  border-radius: 6px;
  background: #fff;
  overflow: hidden;
}
.mw-body {
  flex: 1;
  overflow-y: auto;
  padding: 12px 24px;
}
.mw-hint {
  padding: 8px 12px;
  background: #F0F5FF;
  border-radius: 4px;
  color: #2F54EB;
  font-size: 13px;
  margin-bottom: 12px;
}
.mw-step-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 16px;
  border-top: 1px solid #f0f0f0;
  background: #fafafa;
  flex-shrink: 0;
}
.mw-step-tip { font-size: 12px; color: #999; }
.mw-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding: 10px 16px;
  border-top: 1px solid #f0f0f0;
  flex-shrink: 0;
}
/* 右侧 AI 面板 */
.mw-right {
  flex: 1;
  display: flex;
  flex-direction: column;
  border: 1px solid #e8e8e8;
  border-radius: 6px;
  background: #fff;
  overflow: hidden;
}
.mw-tabs {
  display: flex;
  border-bottom: 1px solid #e8e8e8;
  flex-shrink: 0;
}
.mw-tabs span {
  padding: 10px 16px;
  cursor: pointer;
  font-size: 13px;
  border-right: 1px solid #e8e8e8;
}
.mw-tabs span.active {
  background: #2F54EB;
  color: #fff;
}
.mw-chat {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.chat-messages {
  flex: 1;
  overflow-y: auto;
  padding: 0;
}
/* AiMessageList 根自带 flex:1/overflow/padding，外层 chat-messages 已负责滚动，覆盖内部避免双层滚动 */
.chat-messages /deep/ .ai-msg-list {
  flex: none;
  overflow: visible;
  padding: 12px;
}
.chat-empty {
  text-align: center;
  color: #999;
  padding: 40px 20px;
  font-size: 13px;
  line-height: 1.8;
}
.asst-thinking { color: #999; font-style: italic; padding: 4px 12px 8px; }
/* 变更项列表 */
.mw-items {
  flex: 1;
  overflow-y: auto;
  padding: 10px;
}
.batch-bar {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 10px;
  background: #fafafa;
  border-bottom: 1px solid #f0f0f0;
  margin-bottom: 8px;
}
.batch-info { flex: 1; font-size: 12px; color: #666; }
.item-groups { padding: 0; }
.item-group { margin-bottom: 8px; }
.group-header {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 8px;
  background: #F5F5F5;
  border-left: 3px solid #2F54EB;
  border-radius: 4px;
}
.group-label { font-weight: bold; font-size: 13px; color: #333; }
.group-count { background: #e8e8e8; color: #666; border-radius: 10px; padding: 0 8px; font-size: 11px; }
.item-row {
  border: 1px solid #E8E8E8;
  border-radius: 6px;
  padding: 8px;
  margin: 6px 0 6px 12px;
  background: #fff;
}
.item-row.st-confirmed { border-color: #52C41A; background: #f6ffed; }
.item-row.st-executed { border-color: #bfbfbf; background: #fafafa; opacity: 0.75; }
.item-row.st-rejected { opacity: 0.5; }
.item-head { display: flex; align-items: center; gap: 8px; font-size: 12px; margin-bottom: 4px; flex-wrap: wrap; }
.item-status { margin-left: auto; padding: 1px 6px; border-radius: 3px; }
.item-status.st-draft { background: #fff7e6; color: #fa8c16; }
.item-status.st-confirmed { background: #f6ffed; color: #52c41a; }
.item-status.st-executed { background: #e6f7ff; color: #1890ff; }
.item-status.st-rejected { background: #fff1f0; color: #f5223d; }
.item-rationale { font-size: 12px; color: #666; margin-bottom: 4px; }
.item-ops { display: flex; gap: 6px; margin-top: 6px; }
</style>

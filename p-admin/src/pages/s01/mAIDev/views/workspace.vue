<template>
  <div>
  <rs-modal ref="modal" :fullScreen="true" style="z-index:9999">
    <div class="aidev-ws">
    <!-- 顶部工具栏 -->
    <div class="ws-header">
      <div class="ws-title">
        <span class="back-btn" @click="back">&lt; 返回</span>
        <h3>{{ session.SESSIONNAME || '开发工作区' }}</h3>
        <span class="ws-status" :class="'st-' + (session.STATUS || 'DRAFT').toLowerCase()">{{ statusLabel(session.STATUS) }}</span>
      </div>
      <div class="ws-actions">
        <button class="h-btn h-btn-s" @click="runValidate" :disabled="loading">校验</button>
        <button class="h-btn h-btn-s h-btn-primary" @click="exportPack" :disabled="loading || confirmedCount === 0">导出升级包</button>
        <button class="h-btn h-btn-s h-btn-blue" @click="openSaveTpl" :disabled="loading || confirmedCount === 0">存为模板</button>
      </div>
    </div>

    <!-- 存为模板弹窗 -->
    <rs-modal ref="saveTplModal" :width="480">
      <div class="save-tpl-form">
        <div class="save-tpl-title">存为业务模板</div>
        <Form :label-width="90">
          <FormItem label="模板编码" required>
            <Input v-model="saveTplForm.templateCode" placeholder="如 TPL_LOGISTICS" />
          </FormItem>
          <FormItem label="模板名称" required>
            <Input v-model="saveTplForm.templateName" placeholder="如 物流管理模板" />
          </FormItem>
          <FormItem label="业务分类">
            <Select v-model="saveTplForm.category" :datas="bizCatOptions"></Select>
          </FormItem>
          <FormItem label="描述">
            <Input v-model="saveTplForm.description" placeholder="可选" />
          </FormItem>
        </Form>
        <div class="save-tpl-tip">取本会话<strong>已确认</strong>的变更项（{{ confirmedCount }} 条）生成模板脚本，目标模块编码替换为安装变量。</div>
        <div class="save-tpl-footer">
          <Button @click="$refs.saveTplModal.hide()">取消</Button>
          <Button color="primary" :loading="saveTplLoading" @click="doSaveTpl">保存</Button>
        </div>
      </div>
    </rs-modal>

    <!-- 开发流程步骤条 -->
    <StepBar :steps="steps" />

    <!-- 主体: 左右分栏 -->
    <div class="ws-body">
      <!-- 左: 对话区 -->
      <div class="ws-chat">
        <div class="chat-messages" ref="msgBox">
          <AiMessageList :messages="messages" scene="aidev" />
          <div v-if="loading" class="asst-thinking">思考中...</div>
        </div>
        <AiInput
          :loading="loading"
          placeholder="描述你要开发的功能，如：新增一个设备校准记录模块，含设备名、校准日期、校准人、结果"
          @send="send"
        />
      </div>

      <!-- 右: 变更项 + 脚本区 (Tab 切换) -->
      <div class="ws-side">
        <div class="side-tabs">
          <span :class="{ active: tab === 'items' }" @click="tab = 'items'">变更项 ({{ items.length }})</span>
          <span :class="{ active: tab === 'script' }" @click="tab = 'script'">已确认脚本 ({{ confirmedCount }})</span>
        </div>

        <!-- Tab1: 变更项（按类别分组 + 批量确认） -->
        <div v-show="tab === 'items'" class="side-items">
          <div v-if="items.length === 0" class="empty-tip">暂无变更项，在左侧描述需求让 AI 生成</div>
          <template v-else>
            <!-- 批量操作栏 -->
            <div class="batch-bar">
              <span class="batch-info">{{ draftCount }} 待确认 / {{ items.length }} 项</span>
              <button class="h-btn h-btn-s h-btn-green" @click="confirmAll" :disabled="draftCount === 0 || exported">全部确认</button>
              <button class="h-btn h-btn-s h-btn-red" @click="rejectAll" :disabled="draftCount === 0 || exported">全部拒绝</button>
              <button class="h-btn h-btn-s" @click="dedupItems" :disabled="items.length === 0">去重清理</button>
              <button class="h-btn h-btn-s h-btn-blue" @click="mergeAll" :disabled="draftCount === 0 || exported">合并确认</button>
            </div>
            <!-- 按类别分组 -->
            <div class="item-groups">
              <div v-for="g in groupedItems" :key="g.category" class="item-group">
                <div class="group-header" @click="toggleGroup(g.category)">
                  <span class="group-toggle">{{ openGroups[g.category] === false ? '▸' : '▾' }}</span>
                  <span class="group-label">{{ g.label }}</span>
                  <span class="group-count">{{ g.items.length }}</span>
                  <span class="group-draft" v-if="groupDraftCount(g) > 0">（{{ groupDraftCount(g) }} 待确认）</span>
                  <button v-if="groupDraftCount(g) > 0 && !exported" class="h-btn h-btn-s h-btn-green group-confirm-btn" @click.stop="confirmGroup(g)">确认本组</button>
                </div>
                <div class="group-body" v-show="openGroups[g.category] !== false">
                  <div v-for="it in g.items" :key="it.ID" :class="['item-row', 'st-' + (it.ITEMSTATUS || 'DRAFT').toLowerCase()]">
                    <div class="item-head">
                      <span class="item-seq">#{{ it.ITEMSEQ }}</span>
                      <span :class="['item-status', 'st-' + (it.ITEMSTATUS || 'DRAFT').toLowerCase()]">{{ statusLabel2(it.ITEMSTATUS) }}</span>
                    </div>
                    <div class="item-rationale" v-if="it.RATIONALE">{{ it.RATIONALE }}</div>
                    <div class="item-warnings" v-if="it.WARNINGS">⚠ {{ it.WARNINGS }}</div>
                    <!-- 变更项摘要 + SQL + 确认/拒绝（复用 ChangeItemCard，内含 buildItemSummary） -->
                    <ChangeItemCard
                      :item="it"
                      :show-actions="it.ITEMSTATUS === 'DRAFT' && !exported"
                      @confirm="confirm"
                      @reject="reject"
                    />
                    <div class="item-ops" v-if="it.ITEMSTATUS === 'CONFIRMED' && !exported">
                      <button class="h-btn h-btn-s" @click="unconfirm(it)">撤销确认</button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </template>
        </div>

        <!-- Tab2: 已确认脚本区 -->
        <div v-show="tab === 'script'" class="side-script">
          <div v-if="confirmedCount === 0" class="empty-tip">暂无已确认变更项</div>
          <div v-else>
            <div class="script-toolbar">
              <button class="h-btn h-btn-s" @click="copyScript">复制</button>
              <button class="h-btn h-btn-s" @click="refreshScript">刷新</button>
              <button class="h-btn h-btn-s h-btn-green" @click="executeConfirmed" :disabled="confirmedCount === 0 || executing">{{ executing ? '执行中...' : '执行已确认脚本' }}</button>
            </div>
            <pre class="script-content">{{ confirmedScript }}</pre>
          </div>
        </div>
      </div>
    </div>
    </div>
  </rs-modal>

  <!-- 校验报告弹窗（与主 modal 平级，对齐 Word 编辑器模式） -->
  <rs-modal ref="validateModal" :width="600" style="z-index:10000">
    <div class="validate-report">
      <h4>校验报告 - {{ validationReport.Passed ? '通过' : '未通过' }}</h4>
      <div v-for="(c, i) in (validationReport.Checks || [])" :key="i" :class="['check-row', c.Status]">
        <span class="check-status">{{ c.Status === 'pass' ? '✓' : c.Status === 'fail' ? '✗' : '!' }}</span>
        <span class="check-rule">{{ c.Rule }}</span>
        <span class="check-msg">{{ c.Message }}</span>
      </div>
    </div>
  </rs-modal>
  </div>
</template>

<script>
import aidev from '@/api/aidev';
import { Constants } from '../store';
import AiClient from '@/utils/ai/AiClient';
import AiMessageList from '@/components/ai/AiMessageList.vue';
import AiInput from '@/components/ai/AiInput.vue';
import ChangeItemCard from '@/components/ai/ChangeItemCard.vue';
import StepBar from '@/components/ai/StepBar.vue';

export default {
  name: 's01-mAIDev-workspace',
  components: { AiMessageList, AiInput, ChangeItemCard, StepBar },
  data() {
    return {
      sessionId: '',
      session: {},
      // blocks 模型：[{ id, role: 'user'|'assistant', blocks: [{ type, text?, tool?, args?, summary? }] }]
      messages: [],
      // 消息 id 递增计数器（流式 push 时作为 :key，避免 index 复用错误 DOM）
      msgSeq: 0,
      loading: false,
      executing: false,
      items: [],
      tab: 'items',
      confirmedScript: '',
      validationReport: {},
      exported: false,
      // 开发流程步骤条 (后端推 steps 事件初始化, step 事件更新)
      steps: [],
      // 分组折叠状态: { category: true/false }，默认展开（值为 false 时折叠）
      openGroups: {},
      // AiClient 实例（SSE aidev 场景，created 中初始化）
      aiClient: null,
      // 存为模板弹窗
      saveTplForm: { templateCode: '', templateName: '', category: '', description: '' },
      saveTplLoading: false
    };
  },
  computed: {
    // 业务分类下拉：数据字典「业务分类」(D0707)
    bizCatOptions() {
      const d = (this.$store.state.app && this.$store.state.app.dicts['业务分类']) || {};
      return Object.keys(d).map(k => ({ key: k, title: d[k] }));
    },
    confirmedCount() {
      return this.items.filter(it => it.ITEMSTATUS === 'CONFIRMED').length;
    },
    // 按 CATEGORY 分组，固定顺序
    groupedItems() {
      const order = ['physical_table', 'dataview', 'field', 'ui', 'dict', 'filter', 'module', 'api', 'page', 'button', 'menu', 'permission', 'billflow', 'other'];
      const groups = {};
      this.items.forEach(it => {
        const cat = it.CATEGORY || 'other';
        if (!groups[cat]) groups[cat] = [];
        groups[cat].push(it);
      });
      const labels = { physical_table: '物理表', dataview: '数据视图', field: '字段定义', ui: '界面配置', dict: '字典', filter: '过滤器', module: '模块', api: '接口', page: '页面配置', button: '按钮配置', menu: '菜单', permission: '权限', billflow: '审批流', other: '其他' };
      return order
        .map(cat => ({ category: cat, label: labels[cat] || cat, items: groups[cat] || [] }))
        .filter(g => g.items.length > 0);
    },
    // 全局待确认数
    draftCount() {
      return this.items.filter(it => it.ITEMSTATUS === 'DRAFT').length;
    }
  },
  created() {
    // 创建 AiClient（aidev 场景走 SSE），回调按 type 更新 messages/items/steps/validationReport
    this.aiClient = new AiClient({
      scene: 'aidev',
      onBlock: (b) => this.onAidevBlock(b),
      onItem: (item) => this.onAidevItem(item),
      onValidate: (report) => { this.validationReport = report || {} },
      onStep: (stepKey, status) => this.onAidevStep(stepKey, status),
      onError: (msg) => this.appendAssistantText('⚠️ ' + (msg || '生成失败')),
      onDone: (b) => this.onAidevDone(b)
    });
  },
  methods: {
    // 由父组件调用: this.$refs.workspace.open(sessionId)
    // 弹出全屏 modal 并加载会话数据
    async open(sessionId) {
      this.sessionId = sessionId;
      this.session = {};
      this.messages = [];
      this.items = [];
      this.steps = [];
      this.confirmedScript = '';
      this.validationReport = {};
      this.exported = false;
      this.$refs.modal.show();
      await this.loadSession();
      await this.loadItems();
      await this.loadConversation();
    },
    // 加载历史对话（重新打开工作区显示之前的 user/assistant 对话）
    async loadConversation() {
      if (!this.sessionId) return;
      try {
        const ret = await aidev.getConversation(this.sessionId);
        const json = (ret && ret.conversation) || '';
        if (!json) return;
        let history = [];
        try { history = typeof json === 'string' ? JSON.parse(json) : json } catch (e) { return }
        if (!Array.isArray(history)) return;
        // 转成 messages blocks 结构：每条历史 -> {id, role, blocks:[{type:'text', text}]}
        this.messages = history.map(h => ({
          id: 'm_' + (this.msgSeq++),
          role: h.role,
          blocks: [{ type: 'text', text: h.content || '' }]
        }));
        this.$nextTick(() => this.scrollToBottom());
      } catch (e) {
        console.warn('[AIDev] loadConversation 失败', e);
      }
    },
    statusLabel(s) {
      const map = { DRAFT: '草稿', GENERATING: '生成中', REVIEWING: '审核中', EXPORTED: '已导出', ARCHIVED: '已归档' };
      return map[s] || s;
    },
    statusLabel2(s) {
      const map = { DRAFT: '待确认', CONFIRMED: '已确认', REJECTED: '已拒绝' };
      return map[s] || s;
    },
    // 切换分组折叠
    toggleGroup(cat) {
      this.$set(this.openGroups, cat, !this.openGroups[cat]);
    },
    back() {
      this.$refs.modal.hide();
    },
    async loadSession() {
      try {
        const session = await this.$callAction({
          action: Constants.STORE_NAME + '/loadSessionDetail',
          param: { id: this.sessionId },
          isBusy: false,
        });
        console.log('[AIDev] loadSession session=', session);
        this.session = session || {};
        this.exported = this.session.STATUS === 'EXPORTED';
      } catch (e) {
        console.error('[AIDev] loadSession 失败', e);
        // $callAction 失败时已弹错误提示
      }
    },
    async loadItems() {
      // 用 A16 自定义接口按 changesetId 查 changeitem（不走标准 A01，VSS_AIDEV_CHANGEITEM 未挂模块路径）
      const csid = this.session.CHANGESETID;
      if (!csid) {
        console.warn('[AIDev] loadItems 跳过：session.CHANGESETID 为空', this.session);
        return;
      }
      try {
        const ret = await aidev.listChangeItems(csid);
        console.log('[AIDev] loadItems 返回', { csid, ret });
        // A16 返回 {Items:[...], TotalCount}, 字段大写
        this.items = (ret && ret.Items) || [];
        console.log('[AIDev] loadItems 设置 items=', this.items.length, '条');
      } catch (e) {
        console.error('[AIDev] loadItems 失败', e);
        this.$error('加载变更项失败: ' + (e.message || e));
      }
    },
    // ============ AiClient 回调（SSE 事件分发）============
    // onBlock: 处理 text/tool_call/tool_result/steps 等 block 事件（AiClient.handleBlock 透传）
    onAidevBlock(b) {
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
        case 'steps':
          // 初始化流程步骤模板
          this.steps = (b.steps || []).map(s => ({ key: s.key, label: s.label, status: s.status || 'pending' }));
          break;
        default:
          break;
      }
    },
    // onItem: 新变更项前置（字段名转成大写与 DB 返回一致）
    onAidevItem(it) {
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
    },
    // onStep: 更新对应步骤状态(start/done/skipped)
    onAidevStep(stepKey, status) {
      const step = this.steps.find(s => s.key === stepKey);
      if (step) step.status = status;
    },
    // onDone: 处理 changeSetId 和 warnings
    onAidevDone(b) {
      if (b && b.changeSetId) {
        this.session.CHANGESETID = b.changeSetId;
      }
      if (b && b.warnings && b.warnings.length) {
        this.$error('部分警告: ' + b.warnings.join('; '));
      }
    },
    // ============ 发送（AiInput @send 回调，text 由 AiInput 传入）============
    async send(text) {
      const t = (text || '').trim();
      if (!t || this.loading) return;
      // 用户消息：blocks 模型（id 用计数器保证唯一，作为 v-for :key）
      this.messages.push({ id: 'm_' + (this.msgSeq++), role: 'user', blocks: [{ type: 'text', text: t }] });
      // AI 回复起始：空 blocks，后续追加
      this.messages.push({ id: 'm_' + (this.msgSeq++), role: 'assistant', blocks: [] });
      this.loading = true;
      this.$nextTick(() => {
        this.scrollToBottom();
      });
      try {
        await this.aiClient.sendDev(this.sessionId, t);
        // 流式过程中已通过 item 事件 unshift 了所有新变更项，不再调 loadItems 覆盖
        // (避免 A01 查询失败或字段不一致导致已展示的变更项消失)
      } catch (e) {
        // 兜底：流式失败时补一条错误 text block
        this.appendAssistantText('⚠️ 生成失败: ' + (e.message || e));
      } finally {
        this.loading = false;
      }
    },
    // 追加流式 AI 文本到当前 assistant 消息的 blocks（参考 assistant.js ADD_BLOCK）
    appendAssistantText(text) {
      const last = this.messages[this.messages.length - 1];
      if (last && last.role === 'assistant') {
        const blocks = last.blocks;
        if (blocks.length && blocks[blocks.length - 1].type === 'text') {
          // 末尾是 text block，拼接（打字效果）
          blocks[blocks.length - 1].text += text;
        } else {
          // 否则新建 text block
          blocks.push({ type: 'text', text });
        }
      }
      this.$nextTick(() => this.scrollToBottom());
    },
    // push 一个 tool_call block 到当前 assistant 消息
    pushToolCall(b) {
      const last = this.messages[this.messages.length - 1];
      if (!last || last.role !== 'assistant') return;
      last.blocks.push({ type: 'tool_call', tool: b.tool, args: b.args || '', summary: '执行中…' });
      this.$nextTick(() => this.scrollToBottom());
    },
    // 更新最后一个同名 tool_call block 的 summary
    updateToolResult(b) {
      const last = this.messages[this.messages.length - 1];
      if (!last || last.role !== 'assistant') return;
      for (let i = last.blocks.length - 1; i >= 0; i--) {
        if (last.blocks[i].type === 'tool_call' && last.blocks[i].tool === b.tool) {
          last.blocks[i].summary = b.summary;
          break;
        }
      }
      this.$nextTick(() => this.scrollToBottom());
    },
    scrollToBottom() {
      const box = this.$refs.msgBox;
      if (box) box.scrollTop = box.scrollHeight;
    },
    // 分组内待确认数
    groupDraftCount(g) {
      return g.items.filter(it => it.ITEMSTATUS === 'DRAFT').length;
    },
    // 批量确认所有待确认项（串行：后端 CONFIRMORDER 依赖 MAX+1，并发会重复）
    async confirmAll() {
      const drafts = this.items.filter(it => it.ITEMSTATUS === 'DRAFT');
      if (drafts.length === 0) return;
      try {
        for (const it of drafts) {
          await aidev.confirmItem(it.ID);
          it.ITEMSTATUS = 'CONFIRMED';
        }
        // 全局 draftCount===0 才标记 confirm 步骤完成（避免任一批确认就标 done）
        if (this.draftCount === 0) {
          const step = this.steps.find(s => s.key === 'confirm');
          if (step) step.status = 'done';
        }
        await this.refreshScript();
        this.$alert('已确认 ' + drafts.length + ' 项');
      } catch (e) {
        this.$error('批量确认失败: ' + (e.message || e));
      }
    },
    // 批量拒绝所有待确认项（串行，与 confirmAll 一致）
    async rejectAll() {
      const drafts = this.items.filter(it => it.ITEMSTATUS === 'DRAFT');
      if (drafts.length === 0) return;
      try {
        for (const it of drafts) {
          await aidev.rejectItem(it.ID);
          it.ITEMSTATUS = 'REJECTED';
        }
        this.$alert('已拒绝 ' + drafts.length + ' 项');
      } catch (e) {
        this.$error('批量拒绝失败: ' + (e.message || e));
      }
    },
    // 确认一个分组内的所有待确认项（串行）
    async confirmGroup(g) {
      const drafts = g.items.filter(it => it.ITEMSTATUS === 'DRAFT');
      if (drafts.length === 0) return;
      try {
        for (const it of drafts) {
          await aidev.confirmItem(it.ID);
          it.ITEMSTATUS = 'CONFIRMED';
        }
        // 全局 draftCount===0 才标记 confirm 步骤完成
        if (this.draftCount === 0) {
          const step = this.steps.find(s => s.key === 'confirm');
          if (step) step.status = 'done';
        }
        await this.refreshScript();
        this.$alert('已确认本组 ' + drafts.length + ' 项');
      } catch (e) {
        this.$error('本组确认失败: ' + (e.message || e));
      }
    },
    async dedupItems() {
      if (!this.sessionId) return;
      try {
        await this.$confirm('清理同 changeset 内完全相同的重复项（CATEGORY+ACTION+TARGET+SQL 一致），每组保留最早一条？');
      } catch (e) { return }
      try {
        const ret = await aidev.dedupItems(this.sessionId);
        const deleted = (ret && ret.deleted) || 0;
        this.$alert(deleted > 0 ? ('已清理 ' + deleted + ' 个重复项') : '无重复项');
        await this.loadItems();
      } catch (e) {
        this.$error('去重失败: ' + (e.message || e));
      }
    },
    async executeConfirmed() {
      if (!this.sessionId || this.confirmedCount === 0) return;
      try {
        await this.$confirm('确认执行已确认的 ' + this.confirmedCount + ' 个变更项？脚本将直接在当前数据库落库（开发环境调试用，单事务，失败自动回滚）。');
      } catch (e) { return }
      this.executing = true;
      try {
        const ret = await aidev.executeConfirmed(this.sessionId);
        if (ret && ret.success) {
          this.$alert('执行成功：' + ret.itemCount + ' 个变更项，' + (ret.totalStatements || 0) + ' 条 SQL 已落库，会话即将关闭');
          // 执行成功后关闭会话（脚本已落库，会话完成使命）
          setTimeout(() => {
            this.$refs.modal.hide();
          }, 1500);
        } else {
          this.$error('执行失败：' + (ret && ret.errorMsg));
        }
      } catch (e) {
        this.$error('执行失败: ' + (e.message || e));
      } finally {
        this.executing = false;
      }
    },
    // 合并确认：把所有 DRAFT 项合并成一条统一变更项（解决 DATAVIEW 依赖 TABLE 字段 ID 的执行时序问题）
    // 合并后原 DRAFT 项标记为 MERGED，导出/执行只取合并后的 CONFIRMED 项（含整段脚本）
    async mergeAll() {
      if (!this.sessionId) return;
      try {
        await this.$confirm('将所有待确认项合并为一条统一变更项？\n\n合并后：\n• 所有 DRAFT 项的 SQL 按依赖顺序拼接成整段脚本\n• 合并项状态为已确认，可直接导出\n• 原 DRAFT 项标记为"已合并"（保留追溯）\n\n合并执行能解决 DATAVIEW 字段依赖 TABLE 字段 ID 的问题（同事务一起执行）。');
      } catch (e) { return }
      try {
        const ret = await aidev.mergeItems(this.sessionId);
        const mergedId = (ret && ret.mergedId) || '';
        this.$alert(mergedId ? ('已合并为一条统一变更项（' + mergedId.substring(0, 8) + '...）') : '已合并为一条统一变更项');
        await this.loadItems();
        await this.refreshScript();
        const step = this.steps.find(s => s.key === 'confirm');
        if (step) step.status = 'done';
      } catch (e) {
        this.$error('合并失败: ' + (e.message || e));
      }
    },
    async confirm(it) {
      try {
        await aidev.confirmItem(it.ID);
        it.ITEMSTATUS = 'CONFIRMED';
        // 标记 confirm 步骤完成
        const step = this.steps.find(s => s.key === 'confirm');
        if (step) step.status = 'done';
        await this.refreshScript();
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
        await this.refreshScript();
      } catch (e) {
        this.$error('撤销失败: ' + (e.message || e));
      }
    },
    async refreshScript() {
      const csid = this.session.CHANGESETID;
      if (!csid) return;
      try {
        const ret = await aidev.getConfirmedScript(csid);
        this.confirmedScript = (ret && ret.script) || (typeof ret === 'string' ? ret : '');
      } catch (e) {
        // 忽略
      }
    },
    async runValidate() {
      const csid = this.session.CHANGESETID;
      if (!csid) {
        this.$Notice('暂无变更包');
        return;
      }
      try {
        const ret = await aidev.validateChangeSet(csid);
        this.validationReport = ret || {};
        this.$refs.validateModal.show();
      } catch (e) {
        this.$error('校验失败: ' + (e.message || e));
      }
    },
    async exportPack() {
      if (this.confirmedCount === 0) {
        this.$Notice('请先确认变更项');
        return;
      }
      try {
        const ret = await aidev.exportScript(this.sessionId);
        const script = (ret && ret.script) || (typeof ret === 'string' ? ret : '');
        // 触发下载
        const blob = new Blob([script], { type: 'text/sql' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${this.session.SESSIONCODE || this.sessionId}_升级包.aidev.sql`;
        a.click();
        URL.revokeObjectURL(url);
        this.exported = true;
        this.session.STATUS = 'EXPORTED';
        // 标记 export 步骤完成
        const step = this.steps.find(s => s.key === 'export');
        if (step) step.status = 'done';
        this.$alert('导出成功，会话已冻结');
      } catch (e) {
        this.$error('导出失败: ' + (e.message || e));
      }
    },
    copyScript() {
      if (navigator.clipboard) {
        navigator.clipboard.writeText(this.confirmedScript);
        this.$alert('已复制');
      }
    },
    // ====== 存为业务模板（模板来源 A：AI 会话导出）======
    openSaveTpl() {
      if (this.confirmedCount === 0) {
        this.$Notice('请先确认变更项');
        return;
      }
      const modCode = this.session.TARGETMODULE || 'MODULE';
      this.saveTplForm.templateCode = 'TPL_' + modCode;
      this.saveTplForm.templateName = (this.session.SESSIONNAME || modCode) + '模板';
      this.saveTplForm.category = '';
      this.saveTplForm.description = '';
      this.$refs.saveTplModal.show();
    },
    async doSaveTpl() {
      if (!this.saveTplForm.templateCode || !this.saveTplForm.templateName) {
        this.$error('模板编码和名称必填');
        return;
      }
      this.saveTplLoading = true;
      try {
        const ret = await this.$callAction({
          action: Constants.STORE_NAME + '/saveSessionAsTemplate',
          param: {
            sessionId: this.sessionId,
            templateCode: this.saveTplForm.templateCode,
            templateName: this.saveTplForm.templateName,
            category: this.saveTplForm.category,
            description: this.saveTplForm.description,
          },
          isBusy: false,
        });
        this.$alert((ret && ret.message) || '已存为模板');
        this.$refs.saveTplModal.hide();
      } catch (e) {
        // $callAction 失败时已弹错误提示
      } finally {
        this.saveTplLoading = false;
      }
    }
  }
};
</script>

<style lang="less" scoped>
.save-tpl-form {
  padding: 4px 6px;
  .save-tpl-title {
    font-size: 15px;
    font-weight: 600;
    color: #17233d;
    padding-bottom: 12px;
  }
  .save-tpl-tip {
    color: #9ea7b4;
    font-size: 12px;
    padding: 4px 0 12px;
  }
  .save-tpl-footer {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
  }
}
.aidev-ws {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 2px);
  background: #F0F2F5;
}
.ws-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 16px;
  border-bottom: 1px solid #e8e8e8;
  background: #fff;
  position: relative;
  z-index: 20;
  flex-shrink: 0;
}

.ws-title { display: flex; align-items: center; gap: 12px; }
.back-btn { cursor: pointer; color: #2F54EB; }
.back-btn:hover { text-decoration: underline; }
.ws-title h3 { margin: 0; font-size: 16px; }
.ws-status { padding: 2px 8px; border-radius: 10px; font-size: 12px; background: #F0F5FF; color: #2F54EB; }
.ws-status.st-exported { background: #f0f0f0; color: #999; }
.ws-actions { display: flex; gap: 8px; }
.ws-body { flex: 1; display: flex; overflow: hidden; }
.ws-chat { width: 45%; display: flex; flex-direction: column; border-right: 1px solid #e8e8e8; background: #fff; }
.chat-messages { flex: 1; overflow-y: auto; padding: 0; }
/* AiMessageList 根自带 flex:1/overflow/padding，外层 chat-messages 已负责滚动，覆盖内部避免双层滚动 */
.chat-messages /deep/ .ai-msg-list {
  flex: none;
  overflow: visible;
  padding: 12px;
}
.asst-thinking { color: #999; font-style: italic; padding: 4px 12px 8px; }
.ws-side { flex: 1; display: flex; flex-direction: column; background: #fff; }
.side-tabs { display: flex; border-bottom: 1px solid #e8e8e8; }
.side-tabs span { padding: 10px 16px; cursor: pointer; font-size: 13px; border-right: 1px solid #e8e8e8; }
.side-tabs span.active { background: #2F54EB; color: #fff; }
.side-items { flex: 1; overflow-y: auto; padding: 12px; }
.empty-tip { text-align: center; color: #999; padding: 40px; }
.batch-bar { display: flex; align-items: center; gap: 8px; padding: 8px 10px; background: #fafafa; border-bottom: 1px solid #f0f0f0; }
.batch-info { flex: 1; font-size: 12px; color: #666; }
.item-groups { padding: 4px 0; }
.item-group { margin-bottom: 2px; }
.group-header { display: flex; align-items: center; gap: 6px; padding: 8px 10px; background: #F5F5F5; cursor: pointer; user-select: none; border-left: 3px solid #2F54EB; border-radius: 4px; margin-bottom: 4px; }
.group-header:hover { background: #F0F5FF; }
.group-toggle { width: 14px; color: #999; }
.group-label { font-weight: bold; font-size: 13px; color: #333; }
.group-count { background: #e8e8e8; color: #666; border-radius: 10px; padding: 0 8px; font-size: 11px; }
.group-draft { color: #fa8c16; font-size: 11px; }
.group-confirm-btn { margin-left: auto; }
.group-body { padding: 0; }
.group-body .item-row { padding-left: 16px; }
.item-row { border: 1px solid #E8E8E8; border-radius: 6px; padding: 10px; margin-bottom: 10px; background: #fff; box-shadow: 0 1px 2px rgba(0,0,0,0.04); transition: box-shadow 0.2s; }
.item-row:hover { box-shadow: 0 2px 8px rgba(0,0,0,0.06); }
.item-row.st-confirmed { border-color: #52C41A; background: #f6ffed; }
.item-row.st-rejected { opacity: 0.5; }
.item-head { display: flex; align-items: center; gap: 8px; font-size: 12px; margin-bottom: 6px; flex-wrap: wrap; }
.item-seq { font-weight: bold; color: #2F54EB; }
.item-status { margin-left: auto; padding: 1px 6px; border-radius: 3px; }
.item-status.st-draft { background: #fff7e6; color: #fa8c16; }
.item-status.st-confirmed { background: #f6ffed; color: #52c41a; }
.item-status.st-rejected { background: #fff1f0; color: #f5223d; }
.item-rationale { font-size: 12px; color: #666; margin-bottom: 4px; }
.item-warnings { font-size: 12px; color: #fa8c16; margin-bottom: 4px; }
.item-ops { display: flex; gap: 6px; margin-top: 6px; }
.side-script { flex: 1; overflow-y: auto; padding: 12px; display: flex; flex-direction: column; }
.script-toolbar { margin-bottom: 8px; display: flex; gap: 6px; }
.script-content { flex: 1; background: #1e1e1e; color: #d4d4d4; padding: 12px; border-radius: 4px; font-size: 12px; overflow: auto; margin: 0; white-space: pre-wrap; word-break: break-all; word-wrap: break-word; min-width: 0; }
.validate-report { padding: 16px; }
.check-row { display: flex; align-items: center; gap: 8px; padding: 6px 0; border-bottom: 1px solid #f0f0f0; font-size: 13px; }
.check-row.pass .check-status { color: #52c41a; }
.check-row.fail .check-status { color: #f5223d; }
.check-row.warn .check-status { color: #fa8c16; }
.check-rule { font-weight: bold; min-width: 200px; }
.check-msg { color: #666; }
</style>

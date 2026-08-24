<template>
  <rs-modal ref="modal" v-model="visible" :width="1100" class="script-flow-editor-modal">
    <view-dialog title="编排接口编辑器" class="d-width">
      <template slot="body">
        <div class="sfe-body">
          <!-- 左侧：接口列表 -->
          <div class="sfe-left">
            <div class="sfe-left-header">
              <span class="sfe-left-title">编排接口</span>
              <Button size="xs" icon="h-icon-plus" color="primary" @click="showAddForm">新增</Button>
            </div>
            <div class="sfe-list" v-if="scriptApis.length > 0">
              <div
                v-for="api in scriptApis"
                :key="api.ID"
                :class="['sfe-list-item', activeApiId === api.ID ? 'active' : '']"
                @click="selectApi(api)"
              >
                <div class="sfe-list-item-main">
                  <span class="sfe-api-code">{{ api.APICODE }}</span>
                  <span class="sfe-api-name">{{ api.APINAME || '(未命名)' }}</span>
                </div>
                <div class="sfe-list-item-sub">
                  <span class="sfe-step-count">{{ getStepCount(api) }} 步</span>
                  <span class="sfe-list-del" @click.stop="removeApi(api)" title="移除关联">x</span>
                </div>
              </div>
            </div>
            <div v-else class="sfe-list-empty">暂无编排接口</div>
            <!-- 新增表单 -->
            <div v-if="addFormVisible" class="sfe-add-form">
              <div class="sfe-add-row">
                <label>编码</label>
                <input v-model="addForm.apiCode" placeholder="如 A52" />
              </div>
              <div class="sfe-add-row">
                <label>名称</label>
                <input v-model="addForm.apiName" placeholder="如 审核并回写" />
              </div>
              <div class="sfe-add-row">
                <label>动作</label>
                <input v-model="addForm.actionCode" placeholder="如 checkAndWriteback" />
              </div>
              <div class="sfe-add-actions">
                <Button size="xs" color="primary" :loading="addLoading" @click="doAddApi">确定</Button>
                <Button size="xs" @click="addFormVisible = false">取消</Button>
              </div>
            </div>
          </div>

          <!-- 右侧：步骤编辑器 + AI 对话 -->
          <div class="sfe-right" v-if="activeApi">
            <!-- 基本信息 -->
            <div class="sfe-api-info">
              <div class="sfe-info-row">
                <label>接口编码</label>
                <span>{{ activeApi.APICODE }}</span>
              </div>
              <div class="sfe-info-row">
                <label>接口名称</label>
                <input v-model="APINAME" class="sfe-info-input" />
              </div>
              <div class="sfe-info-row">
                <label>动作编码</label>
                <input v-model="ACTIONCODE" class="sfe-info-input" />
              </div>
            </div>

            <!-- 步骤编辑器 -->
            <div class="sfe-steps">
              <div class="sfe-steps-header">
                <span>步骤配置</span>
                <DropdownMenu @click="addStep" :datas="stepTypes" class="sfe-add-step-dropdown">
                  <Button size="xs" icon="h-icon-plus" noborder>添加步骤</Button>
                </DropdownMenu>
              </div>
              <div class="sfe-step-list" v-if="steps.length > 0">
                <div v-for="(step, idx) in steps" :key="idx" class="sfe-step-card">
                  <div class="sfe-step-header">
                    <span class="sfe-step-idx">{{ idx }}</span>
                    <Select v-model="step.type" :datas="stepTypeOptions" :no-border="true" style="width:100px;" @change="onStepTypeChange(step)"></Select>
                    <span class="sfe-step-actions">
                      <span class="sfe-step-btn" @click="moveStep(idx, -1)" title="上移" v-if="idx > 0">&uarr;</span>
                      <span class="sfe-step-btn" @click="moveStep(idx, 1)" title="下移" v-if="idx < steps.length - 1">&darr;</span>
                      <span class="sfe-step-btn sfe-step-del" @click="removeStep(idx)" title="删除">x</span>
                    </span>
                  </div>
                  <div class="sfe-step-body">
                    <!-- sql / update -->
                    <template v-if="step.type === 'sql' || step.type === 'update'">
                      <div class="sfe-field">
                        <label>SQLCODE</label>
                        <Select v-model="step.sqlCode" :datas="sqlTemplateOptions" filterable style="width:100%;" placeholder="选择SQL模板"></Select>
                      </div>
                    </template>
                    <!-- query -->
                    <template v-if="step.type === 'query'">
                      <div class="sfe-field">
                        <label>APICODE</label>
                        <Select v-model="step.apiCode" :datas="queryApiOptions" filterable style="width:100%;" placeholder="选择查询接口"></Select>
                      </div>
                    </template>
                    <!-- if -->
                    <template v-if="step.type === 'if'">
                      <div class="sfe-field">
                        <label>条件</label>
                        <input v-model="step.cond" placeholder="如 r1.affected>0" class="sfe-step-input" />
                      </div>
                      <div class="sfe-field">
                        <label>跳转</label>
                        <input type="number" v-model.number="step.goto" class="sfe-step-input sfe-step-goto" min="0" />
                      </div>
                    </template>
                    <!-- return -->
                    <template v-if="step.type === 'return'">
                      <div class="sfe-field">
                        <label>返回数据</label>
                        <input v-model="step.data" placeholder="变量名，如 r1" class="sfe-step-input" />
                      </div>
                    </template>
                    <!-- output (sql/update/query) -->
                    <template v-if="step.type === 'sql' || step.type === 'update' || step.type === 'query'">
                      <div class="sfe-field">
                        <label>输出变量</label>
                        <input v-model="step.output" placeholder="如 r1（后续步骤用）" class="sfe-step-input" />
                      </div>
                    </template>
                  </div>
                </div>
              </div>
              <div v-else class="sfe-steps-empty">暂无步骤，点击上方"添加步骤"或使用 AI 生成</div>
            </div>

            <!-- 保存按钮 -->
            <div class="sfe-save-bar">
              <Button color="primary" :loading="saveLoading" @click="doSave">保存</Button>
            </div>

            <!-- AI 对话面板 -->
            <div class="sfe-ai-section">
              <div class="sfe-ai-header" @click="aiPanelOpen = !aiPanelOpen">
                <span>AI 助手</span>
                <span class="sfe-ai-toggle">{{ aiPanelOpen ? '收起' : '展开' }}</span>
              </div>
              <div class="sfe-ai-body" v-if="aiPanelOpen">
                <div class="sfe-ai-messages" ref="aiMsgList">
                  <div v-if="aiMessages.length === 0" class="sfe-ai-empty">
                    描述你的需求，AI 帮你生成或调整步骤<br/>
                    例如：先查 A01 获取列表，如果 count>0 则执行 SS_XXX 更新
                  </div>
                  <div v-for="(msg, mi) in aiMessages" :key="mi" :class="['sfe-ai-msg', 'sfe-ai-msg-' + msg.role]">
                    <div class="sfe-ai-msg-text" v-if="msg.text">{{ msg.text }}</div>
                    <div v-if="msg.steps" class="sfe-ai-msg-steps">
                      <div class="sfe-ai-steps-label">AI 建议 {{ msg.steps.length }} 个步骤：</div>
                      <div v-for="(s, si) in msg.steps" :key="si" class="sfe-ai-step-item">
                        {{ si }}: [{{ s.type }}] <template v-if="s.sqlCode">sqlCode={{ s.sqlCode }}</template><template v-if="s.apiCode">apiCode={{ s.apiCode }}</template><template v-if="s.cond">cond={{ s.cond }}, goto={{ s.goto }}</template><template v-if="s.data">data={{ s.data }}</template><template v-if="s.output">output={{ s.output }}</template>
                      </div>
                      <Button size="xs" color="primary" @click="applyAiSteps(msg.steps)" style="margin-top:6px;">应用这些步骤</Button>
                    </div>
                  </div>
                </div>
                <div class="sfe-ai-input-bar">
                  <input
                    v-model="aiInput"
                    @keydown.enter.prevent="sendAiMessage"
                    :disabled="aiLoading"
                    placeholder="描述需求..."
                    class="sfe-ai-input"
                  />
                  <Button size="xs" color="primary" :loading="aiLoading" @click="sendAiMessage">发送</Button>
                </div>
              </div>
            </div>
          </div>

          <!-- 右侧空态 -->
          <div class="sfe-right-empty" v-else>
            <div class="sfe-right-empty-text">请从左侧选择一个编排接口<br/>或点击"新增"创建</div>
          </div>
        </div>
      </template>
    </view-dialog>
  </rs-modal>
</template>

<script>
import AiClient from '@/utils/ai/AiClient';
import aidev from '@/api/aidev';
import { Constants as SFE, mapState as sfeMapState, mapGetters as sfeMapGetters, mapDateTable as sfeMapDateTable } from './script-flow-store';

var STEP_TYPES = [
  { title: 'SQL 执行', key: 'sql' },
  { title: '查询接口', key: 'query' },
  { title: '条件跳转', key: 'if' },
  { title: '更新执行', key: 'update' },
  { title: '返回数据', key: 'return' }
];

var STEP_TYPE_OPTIONS = STEP_TYPES.map(function(t) { return { key: t.key, title: t.title } });

function newStep(type) {
  var s = { type: type || 'sql' };
  if (type === 'sql' || type === 'update') s.sqlCode = '';
  if (type === 'query') s.apiCode = '';
  if (type === 'if') { s.cond = ''; s.goto = 0 }
  if (type === 'return') s.data = '';
  if (type === 'sql' || type === 'update' || type === 'query') s.output = '';
  return s;
}

export default {
  name: 'script-flow-editor',
  data() {
    return {
      visible: false,
      moduleCode: '',
      // 接口列表(scriptApis)/查询接口下拉(queryApiOptions) 由 store getter 派生
      // 当前编辑行(activeApi) 在 store state；APINAME/ACTIONCODE 绑 MAIN DataTable
      steps: [], // 当前编辑的步骤数组（JSON 文档型字段，组件编辑缓冲，保存时序列化到 APIPARAM）
      addFormVisible: false,
      addForm: { apiCode: '', apiName: '', actionCode: '' },
      addLoading: false,
      saveLoading: false,
      stepTypes: STEP_TYPES,
      stepTypeOptions: STEP_TYPE_OPTIONS,
      // AI 对话
      aiPanelOpen: false,
      aiMessages: [],
      aiInput: '',
      aiLoading: false,
      aiClient: null,
      aiCurrentMsg: null,
      sessionId: '',
      changesetId: ''
    };
  },
  created() {
    this.aiClient = new AiClient({
      scene: 'aidev',
      onBlock: this.onAiBlock.bind(this),
      onError: this.onAiError.bind(this),
      onDone: this.onAiDone.bind(this)
    });
  },
  computed: {
    // 当前编辑行（store state）+ 接口列表/下拉（store getter 派生，filter+map 不在 .vue 里）
    ...sfeMapState(['activeApi']),
    ...sfeMapGetters(['sqlTemplateOptions', 'scriptApis', 'queryApiOptions']),
    // 编辑字段绑 MAIN DataTable 当前接口行（selectApi initData 后自动同步）
    ...sfeMapDateTable('MAIN', ['APINAME', 'ACTIONCODE']),
    activeApiId() { return this.activeApi ? this.activeApi.ID : '' },
  },
  beforeDestroy() {
    if (this.aiClient) this.aiClient.disconnect();
  },
  methods: {
    show(moduleCode) {
      this.moduleCode = moduleCode;
      this.visible = true;
      this.steps = [];
      this.aiMessages = [];
      this.aiPanelOpen = false;
      this.sessionId = '';
      this.changesetId = '';
      // store 上下文：模块编码 + 清空当前编辑行
      this.$callAction({ action: SFE.STORE_NAME + '/setModuleCode', param: moduleCode, isBusy: false });
      this.$callAction({ action: SFE.STORE_NAME + '/selectApi', param: null, isBusy: false });
      this.loadApis();
      this.loadSelectOptions();
      this.initAiSession();
    },
    async loadApis() {
      // 接口列表由 store getter scriptApis 从 app store MODAPI 派生；
      // 这里只确保模块配置已加载（加载后 getter 自动更新）
      try {
        var modData = this.$store.state.app && this.$store.state.app.modules && this.$store.state.app.modules[this.moduleCode];
        if (!modData || !modData.MODAPI) {
          // eslint-disable-next-line no-restricted-syntax
          await this.$store.dispatch('app/initModule', this.moduleCode);
        }
      } catch (e) { /* ignore */ }
    },
    async loadSelectOptions() {
      // SQL 模板：store action 取数 + commit，getter 自动派生 sqlTemplateOptions
      // 查询接口下拉 queryApiOptions 同为 store getter 派生，无需在此处理
      try {
        await this.$callAction({
          action: SFE.STORE_NAME + '/loadModuleAssets',
          param: { moduleCode: this.moduleCode },
          isBusy: false,
        });
      } catch (e) { /* ignore */ }
    },
    getStepCount(api) {
      return api._stepCount || 0;
    },
    async selectApi(api) {
      // state.activeApi + MAIN DataTable 加载该行（APINAME/ACTIONCODE 经 mapDateTable 自动同步）
      await this.$callAction({ action: SFE.STORE_NAME + '/selectApi', param: api, isBusy: false });
      // 解析步骤（JSON 文档型字段，组件编辑缓冲）
      try {
        this.steps = JSON.parse(api.APIPARAM || '[]');
        if (!Array.isArray(this.steps)) this.steps = [];
      } catch (e) {
        this.steps = [];
      }
    },
    showAddForm() {
      this.addForm = { apiCode: '', apiName: '', actionCode: '' };
      this.addFormVisible = true;
    },
    async doAddApi() {
      if (!this.addForm.apiCode) { this.$Message.warn('请输入接口编码'); return }
      if (!this.addForm.apiName) { this.$Message.warn('请输入接口名称'); return }
      if (!this.addForm.actionCode) { this.$Message.warn('请输入动作编码'); return }
      this.addLoading = true;
      try {
        var ret = await this.$callAction({
          action: SFE.STORE_NAME + '/createApi',
          param: {
            moduleCode: this.moduleCode,
            apiCode: this.addForm.apiCode,
            apiName: this.addForm.apiName,
            actionCode: this.addForm.actionCode,
          },
          isBusy: false,
        });
        this.$Message.success('创建成功');
        this.addFormVisible = false;
        // 刷新模块配置 + 接口列表
        // eslint-disable-next-line no-restricted-syntax
        await this.$store.dispatch('app/initModule', this.moduleCode);
        await this.loadApis();
        await this.loadSelectOptions();
        // 选中新建的接口
        var newApi = this.scriptApis.find(function(a) { return a.APICODE === ret.apiCode });
        if (newApi) this.selectApi(newApi);
      } catch (e) {
        this.$Message.error('创建失败: ' + (e.message || e));
      } finally {
        this.addLoading = false;
      }
    },
    async removeApi(api) {
      try {
        await this.$confirm('确认移除编排接口 ' + api.APICODE + ' 的关联？');
      } catch (e) { return }
      try {
        await this.$callAction({
          action: SFE.STORE_NAME + '/removeApi',
          param: { moduleCode: this.moduleCode, apiCode: api.APICODE },
          isBusy: false,
        });
        this.$Message.success('已移除');
        if (this.activeApiId === api.ID) {
          await this.$callAction({ action: SFE.STORE_NAME + '/selectApi', param: null, isBusy: false });
          this.steps = [];
        }
        // eslint-disable-next-line no-restricted-syntax
        await this.$store.dispatch('app/initModule', this.moduleCode);
        await this.loadApis();
      } catch (e) {
        this.$Message.error('移除失败: ' + (e.message || e));
      }
    },
    addStep(type) {
      this.steps.push(newStep(type));
    },
    onStepTypeChange(step) {
      // 重置类型相关字段
      delete step.sqlCode;
      delete step.apiCode;
      delete step.cond;
      delete step.goto;
      delete step.data;
      delete step.output;
      var s = newStep(step.type);
      Object.keys(s).forEach(function(k) { if (k !== 'type') step[k] = s[k]; });
    },
    moveStep(idx, dir) {
      var target = idx + dir;
      if (target < 0 || target >= this.steps.length) return;
      var arr = this.steps;
      var tmp = arr[idx];
      this.$set(arr, idx, arr[target]);
      this.$set(arr, target, tmp);
    },
    removeStep(idx) {
      this.steps.splice(idx, 1);
    },
    async doSave() {
      if (!this.activeApi) return;
      this.saveLoading = true;
      try {
        await this.$callAction({
          action: SFE.STORE_NAME + '/saveSteps',
          param: {
            apiId: this.activeApi.ID,
            stepsJson: JSON.stringify(this.steps),
            apiName: this.APINAME,
            actionCode: this.ACTIONCODE,
            stepCount: this.steps.length,
          },
          isBusy: false,
        });
        this.$Message.success('保存成功');
        // store mutation APPLY_STEPS 已同步 activeApi 行（APIPARAM/APINAME/ACTIONCODE/_stepCount）
        this.$emit('saved');
      } catch (e) {
        this.$Message.error('保存失败: ' + (e.message || e));
      } finally {
        this.saveLoading = false;
      }
    },

    // ====== AI 对话 ======
    async initAiSession() {
      try {
        var ret = await aidev.openWizardSession();
        var d = (ret && ret.Data) || ret || {};
        this.sessionId = d.sessionId || '';
        this.changesetId = d.changesetId || '';
      } catch (e) {
        console.warn('[ScriptFlowEditor] 创建AI会话失败', e);
      }
    },
    async sendAiMessage() {
      var text = this.aiInput.trim();
      if (!text || this.aiLoading) return;
      if (!this.sessionId) {
        this.$Message.warn('AI 会话未就绪，请稍候');
        return;
      }
      this.aiInput = '';

      // 构建上下文消息
      var contextParts = [];
      contextParts.push('[当前编排接口: ' + (this.activeApi ? this.activeApi.APICODE : '') + ' - ' + (this.APINAME || '') + ']');
      contextParts.push('[当前步骤配置]: ' + JSON.stringify(this.steps));
      if (this.sqlTemplateOptions.length > 0) {
        contextParts.push('[模块可用SQL模板]: ' + this.sqlTemplateOptions.map(function(o) { return o.key }).join(', '));
      }
      if (this.queryApiOptions.length > 0) {
        contextParts.push('[模块可用查询接口]: ' + this.queryApiOptions.map(function(o) { return o.key }).join(', '));
      }
      contextParts.push('[模块编码]: ' + this.moduleCode);
      var contextMsg = contextParts.join('\n') + '\n\n' + text;

      this.aiMessages.push({ role: 'user', text: text });
      this.aiLoading = true;

      // AI 消息占位
      var aiMsg = { role: 'assistant', text: '', steps: null, streaming: true };
      this.aiMessages.push(aiMsg);
      this.aiCurrentMsg = aiMsg;

      try {
        await this.aiClient.sendDev(this.sessionId, contextMsg);
      } catch (e) {
        this.appendAiText('错误: ' + (e.message || String(e)));
      } finally {
        this.finalizeAiStream();
      }
    },
    onAiBlock(b) {
      if (!b || !b.type) return;
      var msg = this.aiCurrentMsg;
      if (!msg) return;
      if (b.type === 'text') {
        this.appendAiText(b.text || '');
      } else if (b.type === 'tool_call') {
        var toolName = b.tool || '';
        if (toolName === 'define_script_flow_api' || toolName === 'update_script_flow_api') {
          // 提取步骤
          try {
            var args = typeof b.args === 'string' ? JSON.parse(b.args) : b.args;
            if (args && args.steps) {
              var parsedSteps = typeof args.steps === 'string' ? JSON.parse(args.steps) : args.steps;
              if (Array.isArray(parsedSteps) && parsedSteps.length > 0) {
                msg.steps = parsedSteps;
              }
            }
          } catch (e) { /* ignore parse errors */ }
        }
      } else if (b.type === 'tool_result') {
        // 尝试从结果中提取步骤
        if (b.result) {
          try {
            var result = typeof b.result === 'string' ? JSON.parse(b.result) : b.result;
            // define_script_flow_api / update_script_flow_api 的结果中有 metadata.moudleapi.APIPARAM
            if (result.metadata && result.metadata.moudleapi && result.metadata.moudleapi.APIPARAM) {
              var parsedSteps2 = JSON.parse(result.metadata.moudleapi.APIPARAM);
              if (Array.isArray(parsedSteps2) && parsedSteps2.length > 0 && !msg.steps) {
                msg.steps = parsedSteps2;
              }
            }
          } catch (e) { /* ignore */ }
        }
      }
    },
    onAiError(msg) {
      this.appendAiText('\n错误: ' + msg);
    },
    onAiDone() {
      var msg = this.aiCurrentMsg;
      if (msg) msg.streaming = false;
    },
    finalizeAiStream() {
      var msg = this.aiCurrentMsg;
      if (msg && msg.streaming) msg.streaming = false;
      this.aiCurrentMsg = null;
      this.aiLoading = false;
      this.$nextTick(function() {
        var el = this.$refs.aiMsgList;
        if (el) el.scrollTop = el.scrollHeight;
      });
    },
    appendAiText(text) {
      var msg = this.aiCurrentMsg;
      if (!msg) return;
      msg.text = (msg.text || '') + text;
    },
    applyAiSteps(steps) {
      this.steps = steps.map(function(s) {
        var step = { type: s.type || 'sql' };
        if (s.sqlCode !== undefined) step.sqlCode = s.sqlCode;
        if (s.apiCode !== undefined) step.apiCode = s.apiCode;
        if (s.cond !== undefined) step.cond = s.cond;
        if (s.goto !== undefined) step.goto = s.goto;
        if (s.data !== undefined) step.data = s.data;
        if (s.output !== undefined) step.output = s.output;
        return step;
      });
      this.$Message.success('已应用 ' + steps.length + ' 个步骤');
    }
  }
};
</script>

<style lang="less" scoped>
.sfe-body {
  display: flex;
  height: 560px;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  overflow: hidden;
}
.sfe-left {
  width: 240px;
  border-right: 1px solid #e0e0e0;
  display: flex;
  flex-direction: column;
  background: #fafafa;
}
.sfe-left-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 10px;
  border-bottom: 1px solid #e0e0e0;
}
.sfe-left-title {
  font-weight: 600;
  font-size: 13px;
}
.sfe-list {
  flex: 1;
  overflow-y: auto;
}
.sfe-list-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 10px;
  cursor: pointer;
  border-bottom: 1px solid #f0f0f0;
  &:hover { background: #f0f7ff; }
  &.active { background: #e6f4ff; border-left: 3px solid #1890ff; }
}
.sfe-list-item-main {
  display: flex;
  flex-direction: column;
}
.sfe-api-code {
  font-size: 13px;
  font-weight: 500;
  color: #1890ff;
}
.sfe-api-name {
  font-size: 12px;
  color: #888;
  margin-top: 2px;
}
.sfe-list-item-sub {
  display: flex;
  align-items: center;
  gap: 6px;
}
.sfe-step-count {
  font-size: 11px;
  color: #999;
  background: #f0f0f0;
  border-radius: 8px;
  padding: 1px 6px;
}
.sfe-list-del {
  color: #ccc;
  cursor: pointer;
  font-size: 12px;
  &:hover { color: #f5222d; }
}
.sfe-list-empty, .sfe-right-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  color: #bbb;
  font-size: 13px;
  flex: 1;
  padding: 20px;
  text-align: center;
}
.sfe-add-form {
  padding: 8px 10px;
  border-top: 1px solid #e0e0e0;
  background: #fff;
}
.sfe-add-row {
  display: flex;
  align-items: center;
  margin-bottom: 6px;
  label {
    width: 40px;
    font-size: 12px;
    color: #666;
    flex-shrink: 0;
  }
  input {
    flex: 1;
    border: 1px solid #d9d9d9;
    border-radius: 3px;
    padding: 3px 6px;
    font-size: 12px;
  }
}
.sfe-add-actions {
  display: flex;
  gap: 6px;
  justify-content: flex-end;
  margin-top: 6px;
}
.sfe-right {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.sfe-api-info {
  display: flex;
  gap: 12px;
  padding: 8px 12px;
  border-bottom: 1px solid #e0e0e0;
  background: #fafafa;
  align-items: center;
}
.sfe-info-row {
  display: flex;
  align-items: center;
  gap: 4px;
  label {
    font-size: 12px;
    color: #888;
    white-space: nowrap;
  }
  span {
    font-size: 13px;
    color: #333;
  }
}
.sfe-info-input {
  border: 1px solid #d9d9d9;
  border-radius: 3px;
  padding: 2px 6px;
  font-size: 12px;
  width: 120px;
}
.sfe-steps {
  flex: 1;
  overflow-y: auto;
  padding: 8px 12px;
}
.sfe-steps-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
  font-weight: 600;
  font-size: 13px;
}
.sfe-step-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.sfe-step-card {
  border: 1px solid #e8e8e8;
  border-radius: 4px;
  padding: 6px 8px;
  background: #fff;
}
.sfe-step-header {
  display: flex;
  align-items: center;
  gap: 6px;
}
.sfe-step-idx {
  width: 20px;
  height: 20px;
  border-radius: 10px;
  background: #1890ff;
  color: #fff;
  font-size: 11px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.sfe-step-actions {
  margin-left: auto;
  display: flex;
  gap: 4px;
}
.sfe-step-btn {
  cursor: pointer;
  font-size: 13px;
  color: #999;
  &:hover { color: #1890ff; }
}
.sfe-step-del:hover { color: #f5222d; }
.sfe-step-body {
  margin-top: 4px;
  padding-left: 26px;
}
.sfe-field {
  display: flex;
  align-items: center;
  gap: 4px;
  margin-bottom: 4px;
  label {
    font-size: 11px;
    color: #888;
    width: 56px;
    flex-shrink: 0;
  }
}
.sfe-step-input {
  border: 1px solid #d9d9d9;
  border-radius: 3px;
  padding: 2px 6px;
  font-size: 12px;
  flex: 1;
}
.sfe-step-goto { width: 60px; flex: none; }
.sfe-steps-empty {
  color: #bbb;
  font-size: 13px;
  text-align: center;
  padding: 20px;
}
.sfe-save-bar {
  padding: 6px 12px;
  border-top: 1px solid #e0e0e0;
  text-align: right;
}
// AI 对话面板
.sfe-ai-section {
  border-top: 1px solid #e0e0e0;
  background: #fafafa;
}
.sfe-ai-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 6px 12px;
  cursor: pointer;
  font-size: 13px;
  font-weight: 500;
  &:hover { background: #f0f0f0; }
}
.sfe-ai-toggle {
  font-size: 12px;
  color: #1890ff;
}
.sfe-ai-body {
  height: 200px;
  display: flex;
  flex-direction: column;
  border-top: 1px solid #e0e0e0;
}
.sfe-ai-messages {
  flex: 1;
  overflow-y: auto;
  padding: 8px 12px;
  font-size: 12px;
}
.sfe-ai-empty {
  color: #bbb;
  text-align: center;
  padding: 16px;
  line-height: 1.6;
}
.sfe-ai-msg {
  margin-bottom: 8px;
  &-user { color: #1890ff; }
  &-assistant { color: #333; }
}
.sfe-ai-msg-text { white-space: pre-wrap; word-break: break-word; }
.sfe-ai-msg-steps {
  margin-top: 6px;
  background: #f6ffed;
  border: 1px solid #b7eb8f;
  border-radius: 4px;
  padding: 6px 8px;
}
.sfe-ai-steps-label { font-weight: 500; margin-bottom: 4px; }
.sfe-ai-step-item { padding: 2px 0; color: #555; }
.sfe-ai-input-bar {
  display: flex;
  gap: 6px;
  padding: 6px 12px;
  border-top: 1px solid #e0e0e0;
}
.sfe-ai-input {
  flex: 1;
  border: 1px solid #d9d9d9;
  border-radius: 3px;
  padding: 4px 8px;
  font-size: 12px;
}
</style>

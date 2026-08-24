<template>
  <div class="review-wrapper">
    <!-- 顶部信息条 -->
    <div class="review-header">
      <div class="review-header-left">
        <span class="review-header-label">当前委托单号：</span>
        <span class="review-header-value">{{ wtItem ? wtItem.WTCODE : '' }}</span>
        <span class="review-header-divider">|</span>
        <span class="review-header-label">委托时间：</span>
        <span class="review-header-value" :class="{ 'review-overdue': isOverdue }">
          {{ wtItem ? wtItem.BILLDATE : '' }}
          <span v-if="isOverdue" class="review-overdue-tag">超期</span>
        </span>
        <span class="review-header-divider">|</span>
        <span class="review-header-label">审核进度：</span>
        <span class="review-header-value">{{ reviewedCount }} / {{ dtlItems.length }}</span>
      </div>
      <div class="review-header-right">

      </div>
    </div>

    <!-- 三栏分屏布局 -->
    <div class="review-container">
      <!-- 左栏：记录列表（可折叠） -->
      <div class="review-left" :class="{ 'review-left--collapsed': leftCollapsed }">
        <div class="review-left-title">
          <span v-if="!leftCollapsed">记录列表</span>
          <span v-else>列表</span>
          <i
            :class="leftCollapsed ? 'h-icon-right' : 'h-icon-left'"
            class="review-left-toggle"
            @click="leftCollapsed = !leftCollapsed"
          ></i>
        </div>
        <div class="review-left-list" v-show="!leftCollapsed">
          <div
            v-for="(item, index) in dtlItems"
            :key="item.ID || index"
            class="review-left-item"
            :class="{
              'review-left-item--active': activeItem && activeItem.ID === item.ID,
              'review-left-item--done': item.STATE === 6 || item.STATE === 20,
              'review-left-item--rejected': item.STATE === 12
            }"
            @click="selectItem(item)"
          >
            <div class="review-left-item-index">{{ index + 1 }}</div>
            <div class="review-left-item-info">
              <div class="review-left-item-name" :title="item.MNAME">{{ item.MNAME }}</div>
              <div class="review-left-item-code" :title="item.OPCODE">{{ item.OPCODE }}</div>
            </div>
            <div class="review-left-item-status">
              <span v-if="item.STATE === 6 || item.STATE === 20" class="status-done">已审批</span>
              <span v-else-if="item.STATE === 12" class="status-rejected">已驳回</span>
              <span v-else-if="item.STATE === 2" class="status-pending">待审核</span>
              <span v-else-if="item.STATE === 5 || item.STATE === 19" class="status-pending">待审批</span>
              <span v-else class="status-default">--</span>
            </div>
          </div>
        </div>
      </div>

      <!-- 中栏：证书 Word 预览（OnlyOffice） -->
      <div class="review-center" :style="{ width: centerWidth + 'px', flex: 'none' }">
        <div class="review-panel-title">
          <span>证书预览</span>
          <span class="review-panel-name" v-if="activeItem">{{ activeItem.CERTCODE }}</span>
        </div>
        <div class="review-preview-area">
          <div v-if="!certFileId" class="review-preview-placeholder">
            <i class="h-icon-info" style="font-size: 32px; color: #c5c5c5;"></i>
            <p>请从左侧选择一条记录进行预览</p>
          </div>
          <rs-onlyoffice-preview
            v-else
            :file-id="certFileId"
            :title="certFileName"
            :file-type="certFileType"
          ></rs-onlyoffice-preview>
        </div>
      </div>

      <!-- 可拖动分隔条（中栏与右栏之间） -->
      <div
        class="review-resizer"
        :class="{ 'review-resizer--active': isResizing }"
        @mousedown="startResize"
      >
        <div class="review-resizer-line"></div>
      </div>

      <!-- 右栏：原始记录（使用 rs-edit-item 组件，与现有原始记录查看一致） -->
      <div class="review-right">
        <div class="review-panel-title">
          <span>原始记录</span>
          <span class="review-panel-name" v-if="activeItem">{{ activeItem.MNAME }}</span>
        </div>
        <div class="review-form-area">
          <div v-if="!activeItem" class="review-preview-placeholder">
            <i class="h-icon-info" style="font-size: 32px; color: #c5c5c5;"></i>
            <p>请从左侧选择一条记录查看原始记录</p>
          </div>
          <div v-else-if="recordLoading" class="review-preview-placeholder">
            <i class="h-icon-loading" style="font-size: 32px;"></i>
            <p>加载原始记录...</p>
          </div>
          <div v-else-if="refPmData && refPmData.length > 0" class="review-form-content">
            <rs-edit-item
              ref="editItem"
              :layouts="refPmData"
              :select="{}"
              :parent="-1"
              :inLayout="false"
            ></rs-edit-item>
          </div>
          <div v-else class="review-preview-placeholder">
            <i class="h-icon-info" style="font-size: 32px; color: #c5c5c5;"></i>
            <p>该记录无原始记录模板</p>
          </div>
        </div>
      </div>
    </div>

    <!-- 审核检查清单（高度可拖拽） -->
    <div class="review-checklist" :style="{ height: checklistHeight + 'px' }">
      <!-- 拖拽条：增大热区，添加手柄图标 -->
      <div
        class="review-checklist-resizer"
        :class="{ 'review-checklist-resizer--active': isChecklistResizing }"
        @mousedown="startChecklistResize"
      >
        <div class="review-checklist-resizer-handle">
          <span></span><span></span><span></span>
        </div>
      </div>
      <div class="review-checklist-title">
        审核检查清单
        <Button size="s" @click="autoCheckAll" :disabled="!activeItem" style="margin-left: 10px;">自动检查</Button>
        <Button size="s" @click="showHelpDoc" style="margin-left: 6px;">审核人帮助文档</Button>
      </div>
      <div class="review-checklist-body">
        <!-- 按分组显示检查清单 -->
        <div class="review-checklist-groups">
          <div
            v-for="(group, gIdx) in groupedCheckList"
            :key="gIdx"
            class="review-checklist-group"
          >
            <div class="review-checklist-group-title">{{ group.label }}</div>
            <div class="review-checklist-group-items">
              <div
                v-for="(item, i) in group.items"
                :key="i"
                class="review-checklist-item"
                :class="{ 'review-checklist-item--has-reason': item.failReason }"
              >
                <div class="review-checklist-item-main">
                  <checkbox v-model="item.checked" :disabled="isReviewing">{{ item.label }}</checkbox>
                  <span v-if="item.autoResult === 'pass'" class="check-auto check-auto--pass" title="自动检查通过">&#10003;</span>
                  <span v-else-if="item.autoResult === 'fail'" class="check-auto check-auto--fail" title="自动检查未通过">&#10007;</span>
                  <span v-else-if="item.autoResult === 'warn'" class="check-auto check-auto--warn" title="需人工确认">&#9888;</span>
                </div>
                <div v-if="item.failReason" class="review-checklist-item-reason">
                  <i class="h-icon-warn" style="color: #ff9900; font-size: 11px;"></i>
                  <span>{{ item.failReason }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
        <!-- AI 检查结果详情 -->
        <div v-if="warnings.length > 0" class="review-checklist-warnings">
          <div class="review-checklist-warnings-title">
            <i class="h-icon-warn" style="color: #ff9900;"></i>
            <span>AI 异常检测结果</span>
          </div>
          <div class="review-checklist-warnings-list">
            <div v-for="(w, i) in warnings" :key="i" class="review-checklist-warnings-item">
              <span class="review-warning-type" :class="'warning-type-' + w.type">{{ w.typeLabel }}</span>
              <span class="review-warning-desc">{{ w.desc }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 底部操作栏 -->
    <div class="review-footer">
      <div class="review-footer-left">
        <!-- 待审核/待审批：显示通过+驳回按钮 -->
        <template v-if="canReview">
          <Tooltip
            theme="white"
            trigger="click"
            editable
            ref="reviewTip"
          >
            <Button
              color="primary"
              icon="h-icon-check"
              :loading="isReviewing"
              :disabled="!canApprove"
            >{{ approveBtnText }}</Button>
            <div slot="content">
              <div v-padding="10">
                <textarea
                  dict="simple"
                  :placeholder="'请输入' + approveBtnText + '说明'"
                  v-model="reviewRemark"
                  style="width: 200px;"
                ></textarea>
                <!-- 审核模式(STATE=2)需要选择下一审批人 -->
                <template v-if="mode === 'check'">
                  <div style="margin-top: 8px;">下一审批人</div>
                  <AutoComplete
                    placeholder="请选择审批人"
                    :option="empParam1"
                    v-model="VERIFYID"
                    @change="onVerifyChange"
                    style="margin-top: 4px;"
                  ></AutoComplete>
                </template>
              </div>
              <div v-padding="10" class="text-center">
                <Button color="primary" @click.native="handleApprove">{{ passBtnText }}</Button>
                <Button color="red" @click.native="handleReject">{{ rejectBtnText }}</Button>
              </div>
            </div>
          </Tooltip>
        </template>
        <!-- 已审核(STATE=3)：撤销审核 -->
        <template v-if="canReCheck">
          <Poptip content="确定撤销审核？" @confirm="handleReCheck">
            <Button color="red" icon="h-icon-close" :loading="isReviewing">撤销审核</Button>
          </Poptip>
        </template>
        <!-- 已审批(STATE=6/20)：撤销审批 -->
        <template v-if="canReVerify">
          <Poptip content="确定撤销审批？" @confirm="handleReVerify">
            <Button color="red" icon="h-icon-close" :loading="isReviewing">撤销审批</Button>
          </Poptip>
        </template>
        <!-- 已驳回(STATE=12)：无操作（驳回不可撤销） -->
        <template v-if="isRejected">
          <span style="color:#ed4014;font-size:13px;">已驳回</span>
        </template>
        <!-- 已审核(STATE=3)在审批模式或已审批(STATE=6/20)在审核模式：仅显示状态 -->
        <template v-if="activeItem && !canReview && !canReCheck && !canReVerify && !isRejected">
          <span style="color:#19be6b;font-size:13px;">{{ stateLabel }}</span>
        </template>
        <!-- 未选中记录 -->
        <template v-if="!activeItem">
          <span style="color:#999;font-size:13px;">请选择一条记录</span>
        </template>
      </div>
      <div class="review-footer-right">
        <Button icon="h-icon-list" @click="showChangeLog">查看变更记录</Button>
        <Button icon="h-icon-download" @click="handleExport">导出</Button>
      </div>
    </div>

    <!-- 帮助文档弹窗 -->
    <rs-modal ref="helpDocModal" v-if="helpDocVisible">
      <div class="review-helpdoc-container">
        <div class="review-helpdoc-title">审核人帮助文档</div>
        <div class="review-helpdoc-content">
          <div class="review-helpdoc-section" v-for="(section, idx) in helpDocContent" :key="idx">
            <h4>{{ section.title }}</h4>
            <ul>
              <li v-for="(item, i) in section.items" :key="i">{{ item }}</li>
            </ul>
          </div>
        </div>
      </div>
    </rs-modal>

    <!-- 变更记录弹窗 -->
    <rs-modal ref="logModal" v-if="showLogModal">
      <div class="review-log-container">
        <div class="review-log-title">变更记录</div>
        <div class="review-log-list">
          <div v-for="(log, i) in changeLogs" :key="i" class="review-log-item">
            <div class="review-log-time">{{ log.CREATEDATE }}</div>
            <div class="review-log-content">
              <span class="review-log-user">{{ log.CREATER }}</span>
              <span class="review-log-action">{{ log.ACTION }}</span>
              <span class="review-log-detail" v-if="log.REMARK">{{ log.REMARK }}</span>
            </div>
          </div>
          <div v-if="changeLogs.length === 0" class="review-log-empty">暂无变更记录</div>
        </div>
      </div>
    </rs-modal>
  </div>
</template>
<script>
// eslint-disable-next-line no-restricted-imports
import db from '@/api/db';
import store from '@/store';
import { mapState, mapGetters, mapDateTable, Constants, getStore as getStore025 } from '../store';
import { getStore as getStore026 } from '../../m026/store';
import { BILL_STATE_MAP } from '@/constants';

export default {
  name: 'r01-m025-review',
  components: {
    'rs-edit-item': function() { return import('@/components/edit/rs-edit-item.vue') },
  },
  props: {
    wtItem: {
      type: Object,
      default: function() { return {} },
    },
    dtlItems: {
      type: Array,
      default: function() { return [] },
    },
    // 'check' = 委托审核(STATE=2)，'verify' = 委托审批(STATE=5/19)
    mode: {
      type: String,
      default: 'check',
    },
    // store 命名空间，默认 r01/m025，m026 传入 r01/m026
    storeName: {
      type: String,
      default: 'r01/m025',
    },
  },
  computed: {
    // 实际使用的 store namespace，供 dispatch 调用
    currentStoreName: function() {
      return this.storeName || Constants.STORE_NAME;
    },
    ...mapDateTable('EMPUSER', []),
    ...mapDateTable('MAIN', ['REFTPMDATA', 'PTEMPLATEID']),
    isOverdue: function() {
      if (!this.wtItem || !this.wtItem.BILLDATE) return false;
      var billDate = new Date(this.wtItem.BILLDATE);
      var now = new Date();
      var diffDays = Math.floor((now - billDate) / (1000 * 60 * 60 * 24));
      return diffDays > 30;
    },
    reviewedCount: function() {
      return this.dtlItems.filter(function(item) {
        return item.STATE === 6 || item.STATE === 20 || item.STATE === 12;
      }).length;
    },
    // 按 category 分组后的检查清单（计算属性）
    groupedCheckList: function() {
      var groups = [];
      var map = {};
      this.checkList.forEach(function(item) {
        var cat = item.category || 'other';
        if (!map[cat]) {
          map[cat] = {
            label: this.categoryLabels[cat] || '其他',
            items: []
          };
          groups.push(map[cat]);
        }
        map[cat].items.push(item);
      }.bind(this));
      return groups;
    },
    canReview: function() {
      if (!this.activeItem) return false;
      if (this.mode === 'check') return this.activeItem.STATE === 2;
      if (this.mode === 'verify') return this.activeItem.STATE === 5 || this.activeItem.STATE === 19;
      return false;
    },
    canReCheck: function() {
      if (!this.activeItem) return false;
      return this.mode === 'check' && this.activeItem.STATE === 3;
    },
    canReVerify: function() {
      if (!this.activeItem) return false;
      return this.mode === 'verify' && (this.activeItem.STATE === 6 || this.activeItem.STATE === 20);
    },
    isRejected: function() {
      if (!this.activeItem) return false;
      return this.activeItem.STATE === 12;
    },
    canApprove: function() {
      if (!this.canReview) return false;
      // 检查清单全部勾选才能通过
      return this.checkList.every(function(c) { return c.checked });
    },
    stateLabel: function() {
      if (!this.activeItem) return '';
      return BILL_STATE_MAP[this.activeItem.STATE] || '未知状态';
    },
    approveBtnText: function() {
      return this.mode === 'verify' ? '审批' : '审核';
    },
    rejectBtnText: function() {
      return this.mode === 'verify' ? '审批驳回' : '复核驳回';
    },
    passBtnText: function() {
      return this.mode === 'verify' ? '审批通过' : '复核通过';
    },
  },
  data: function() {
    return {
      store: { mapState: mapState, mapGetters: mapGetters, mapDateTable: mapDateTable, Constants: Constants },
      activeItem: null,
      // 左栏折叠状态
      leftCollapsed: false,
      // 面板宽度（可拖动调整）
      centerWidth: 700,
      isResizing: false,
      isChecklistResizing: false,
      checklistHeight: 120,
      // 证书预览（OnlyOffice）
      certFileId: '',
      certFileName: '',
      certFileType: 'docx',
      // 原始记录数据
      recordData: null,
      recordLoading: false,
      refPmData: [],
      fieldsConfig: [],
      inputObj: {},
      tableObj: {},
      editorObj: [],
      standardList: [],
      isReviewing: false,
      reviewRemark: '',
      // 异常检测结果
      warnings: [],
      // 审核检查清单（使用 key 标识，每项包含检查结果和错误原因）
      checkList: [
        // 基础信息完整性
        { key: 'basic_info', label: '设备名称、送校单位、委托方地址等基础信息完整', checked: false, autoResult: null, category: 'basic', failReason: '' },
        { key: 'env_condition', label: '环境条件（温湿度/气压）已填写且在合理范围', checked: false, autoResult: null, category: 'basic', failReason: '' },
        { key: 'standard_ref', label: '依据标准/规程已填写', checked: false, autoResult: null, category: 'basic', failReason: '' },
        // 数据/日期/环境条件复核
        { key: 'date_valid', label: '检校日期合理，无录入错误', checked: false, autoResult: null, category: 'data', failReason: '' },
        { key: 'data_no_error', label: '测量数据无超差项', checked: false, autoResult: null, category: 'data', failReason: '' },
        { key: 'uncertainty_range', label: '不确定度/测量结果在规定范围内', checked: false, autoResult: null, category: 'data', failReason: '' },
        // 格式规范
        { key: 'format_complete', label: '报告模板、编号、页码、栏目无缺失', checked: false, autoResult: null, category: 'format', failReason: '' },
        // 方法合规性
        { key: 'method_valid', label: '所用检定规程/校准规范/产品标准现行有效', checked: false, autoResult: null, category: 'compliance', failReason: '' },
        { key: 'deviation_approved', label: '方法偏离有审批和说明', checked: false, autoResult: null, category: 'compliance', failReason: '' },
        // 标准器核查
        { key: 'standard_expiry', label: '标准器在有效期内', checked: false, autoResult: null, category: 'standard', failReason: '' },
        { key: 'no_conflict', label: '标准器/人员无时间地域冲突', checked: false, autoResult: null, category: 'standard', failReason: '' },
        // 数据真实性（AI预留）
        { key: 'data_authenticity', label: '数据真实性与照片示值一致（AI）', checked: false, autoResult: null, category: 'ai', failReason: '' },
        // 原始记录完整性
        { key: 'record_complete', label: '原始记录完整', checked: false, autoResult: null, category: 'record', failReason: '' },
        { key: 'conclusion_correct', label: '结论正确', checked: false, autoResult: null, category: 'record', failReason: '' },
      ],
      // 分类标签
      categoryLabels: {
        basic: '基础信息完整性',
        data: '数据/日期/环境条件复核',
        format: '格式规范检查',
        compliance: '方法合规性',
        standard: '标准器/人员核查',
        ai: '数据真实性（AI）',
        record: '原始记录完整性',
      },
      // 帮助文档
      helpDocVisible: false,
      helpDocContent: [
        {
          title: '一、基础信息完整性审查',
          items: [
            '设备名称、型号规格、出厂编号是否填写齐全',
            '送校单位、委托方地址、联系人信息是否完整',
            '环境条件（温度、湿度、气压）是否记录',
            '依据标准/规程名称及编号是否正确',
          ],
        },
        {
          title: '二、数据/日期/环境条件复核',
          items: [
            '检校日期是否在合理范围内，无录入错误',
            '测量数据是否存在明显异常或非法符号',
            '必填项是否全部填写，无漏项',
            '超差项是否有明确标识和处理记录',
            '环境条件是否在标准要求的范围内',
            '不确定度评定是否合理，测量结果是否在范围内',
          ],
        },
        {
          title: '三、格式规范检查',
          items: [
            '报告模板是否完整，无缺失栏目',
            '证书编号、页码是否连续正确',
            '签字栏、盖章栏是否齐全',
          ],
        },
        {
          title: '四、方法合规性',
          items: [
            '所用检定规程/校准规范/产品标准是否为现行有效版本',
            '方法偏离是否有审批手续和书面说明',
            '非标方法是否经过确认',
          ],
        },
        {
          title: '五、标准器/人员核查',
          items: [
            '标准器是否在有效期内',
            '标准器是否同一天多地使用冲突',
            '检校人员是否同一天多地使用冲突',
            '地域冲突是否已筛查',
          ],
        },
        {
          title: '六、数据真实性（AI）',
          items: [
            '测量数据与原始照片示值是否一致（AI自动比对）',
            '数据是否存在篡改痕迹',
          ],
        },
        {
          title: '七、原始记录完整性',
          items: [
            '原始记录是否包含所有必要信息',
            '结论是否明确、正确',
            '签字是否齐全',
          ],
        },
      ],
      // 变更记录
      showLogModal: false,
      changeLogs: [],
      VERIFYID: '',
      VERIFYER: '',
      empParam1: {
        loadData: this.empSel1,
        keyName: 'ID',
        titleName: 'EMPNAME',
      },
    };
  },
  created: async function() {
    // 确保 LI_M02 模块已初始化，防止直接刷新页面时模块未加载
    await store.dispatch('app/initModule', 'LI_M02');
    this.getCurrentStore();
  },
  watch: {
    dtlItems: {
      handler: function(val) {
        if (val && val.length > 0 && !this.activeItem) {
          this.selectItem(val[0]);
        }
        this.detectWarnings();
      },
      immediate: true,
    },
  },
  methods: {
    // 根据 currentStoreName 获取对应的 store 实例
    getCurrentStore: function() {
      if (this.currentStoreName === 'r01/m026') {
        return getStore026();
      }
      return getStore025();
    },
    // 选择记录
    selectItem: function(item) {
      var self = this;
      self.activeItem = item;
      self.recordData = item;
      self.refPmData = [];
      self.loadCertFile(item);
      // 加载原始记录模板数据（rs-edit-item 渲染用）
      self.loadRecordTemplate(item);
      // 加载标准器列表（自动检查用）
      self.loadStandardList(item.ID);
      // 重置检查清单自动检查结果和错误原因
      self.checkList.forEach(function(c) { c.autoResult = null; c.failReason = '' })
    },
    // 加载原始记录模板数据（复用现有 openPTEMP 逻辑）
    loadRecordTemplate: async function(item) {
      var self = this;
      self.recordLoading = true;
      try {
        // 1. 调用 store 的 open 加载 orecord 数据到 MAIN/DTSA/DTSB 表
        await self.$callAction({
          action: self.currentStoreName + '/open',
          param: { ID: item.ID },
          isBusy: false,
        });
        var ptemplateId = item.PTEMPLATEID;
        if (ptemplateId) {
          // 2. 调用 openPTEMP 加载模板配置
          // ISEDIT=true 时：
          //   - 解析 TPMDATA/REFTPMDATA JSON 到 MAIN.data[0].REFTPMDATA
          //   - dealTreeData 从 MAIN 表填充各 field 节点的 value
          //   - 不调用 SETTPMDATA（不重置初始值）和 ardSel（不加载 ARD）
          await self.$callAction({
            action: self.currentStoreName + '/openPTEMP',
            param: { ID: ptemplateId, ISEDIT: true, item: item },
            isBusy: false,
          });
          // 3. 从 MAIN DataTable 获取 REFTPMDATA（dealTreeData 已填充了 field 节点的 value）
          var storeResult = self.getCurrentStore();
          var MAIN = storeResult.storeHelper.getTable('MAIN');
          if (MAIN && MAIN.data.length > 0) {
            var tpmData = MAIN.data[0].REFTPMDATA;
            if (tpmData && Array.isArray(tpmData) && tpmData.length > 0) {
              // 4. 构建 inputObj/tableObj/editorObj 索引
              self.tableObj = {};
              self.inputObj = {};
              self.editorObj = [];
              self.dealConfigSelect(tpmData, self);
              // 5. 补充 SETSHOWTPMDATA 中对非 MAIN 字段、editorObj、tableObj 的数据填充
              //    dealTreeData 只处理了 field 节点，还需要处理 DTSB 和 DTSA 数据
              //    注意：不调用原 SETSHOWTPMDATA mutation，因为 ARD DataTable 不存在会导致崩溃
              var DTSA = storeResult.storeHelper.getTable('DTSA');
              var DTSB = storeResult.storeHelper.getTable('DTSB');
              var fields = MAIN.getFields();
              Object.keys(self.inputObj).forEach(function(k) {
                if (fields.indexOf(k) === -1 && DTSB && DTSB.data) {
                  var tt = DTSB.data.find(function(d) { return d.FIELDNAME === k });
                  if (tt) {
                    self.inputObj[k].value = tt.FIELDVALUE;
                    self.inputObj[k].name = tt.FIELDREMARK;
                    self.inputObj[k].field = tt.FIELDNAME;
                  }
                }
              });
              self.editorObj.forEach(function(p) {
                (p.fields || []).forEach(function(f) {
                  if (DTSB && DTSB.data) {
                    var tt = DTSB.data.find(function(d) { return d.FIELDNAME === f.field });
                    if (tt) {
                      f.value = tt.FIELDVALUE;
                      f.name = tt.FIELDREMARK;
                      f.field = tt.FIELDNAME;
                    }
                  }
                });
              });
              if (DTSA && DTSA.data && DTSA.data.length > 0) {
                Object.values(self.tableObj).forEach(function(t) {
                  DTSA.data.forEach(function(ditem) {
                    (t.value || []).push({
                      ID: ditem.ARDID,
                      ARDNAME: ditem.ARDNAME,
                      SIZETYPE: ditem.SIZETYPE,
                      OMCODE: ditem.OMCODE,
                      DEGREE: ditem.DEGREE,
                      CERTCODE: ditem.CERTCODE,
                      EXPDATE: ditem.EXPDATE,
                      CORGNAME: ditem.CORGNAME
                    });
                  });
                });
              }
              // 6. 用 $set 确保 Vue 检测到数据变化
              self.$set(self, 'refPmData', tpmData);
            } else {
              self.$set(self, 'refPmData', []);
            }
          } else {
            self.$set(self, 'refPmData', []);
          }
        } else {
          self.$set(self, 'refPmData', []);
        }
      } catch (e) {
        console.error('加载原始记录模板失败', e);
        self.$set(self, 'refPmData', []);
      } finally {
        self.recordLoading = false;
      }
    },
    // 递归遍历 REFTPMDATA 配置，构建索引对象
    dealConfigSelect: function(nodes, that) {
      nodes.map(function(n) {
        if (n.path) { n.fieldProps = n.fieldProps || {} }
        if (n.field) { that.inputObj[n.field] = n }
        if (n.sourceName) { that.tableObj[n.sourceName] = n; n.value = [] }
        if (n.type === 'itemEditor') { that.editorObj.push(n) }
        if (n.children && n.children.length > 0) { that.dealConfigSelect(n.children, that) }
      });
    },
    // 加载证书文件ID（用于 OnlyOffice 预览）
    loadCertFile: async function(item) {
      var self = this;
      self.certFileId = '';
      self.certFileName = '';
      try {
        // 调用 A49 获取证书文件ID
        var ret = await this.$callAction({
          action: Constants.STORE_NAME + '/loadCertFileId',
          param: { id: item.ID },
          isBusy: false,
        });
        if (ret) {
          self.certFileId = ret;
          self.certFileName = (item.CERTCODE || '证书') + '.docx';
          self.certFileType = 'docx';
        }
      } catch (e) {
        console.error('加载证书文件ID失败', e);
      }
    },
    // 加载标准器列表（从 store 的 DTSA 表中获取）
    loadStandardList: function() {
      var self = this;
      try {
        var storeResult = self.getCurrentStore();
        if (storeResult && storeResult.storeHelper) {
          var DTSA = storeResult.storeHelper.getTable('DTSA');
          if (DTSA && DTSA.data && DTSA.data.length > 0) {
            self.standardList = DTSA.data.map(function(ditem) {
              return {
                ID: ditem.ARDID,
                ARDNAME: ditem.ARDNAME,
                SIZETYPE: ditem.SIZETYPE,
                OMCODE: ditem.OMCODE,
                DEGREE: ditem.DEGREE,
                CERTCODE: ditem.CERTCODE,
                EXPDATE: ditem.EXPDATE,
                CORGNAME: ditem.CORGNAME
              };
            });
          } else {
            self.standardList = [];
          }
        }
      } catch (e) {
        console.error('加载标准器列表失败', e);
        self.standardList = [];
      }
    },
    // 面板拖动调整宽度
    startResize: function(e) {
      var self = this;
      e.preventDefault();
      self.isResizing = true;
      var startX = e.clientX;
      var startCenterWidth = self.centerWidth;
      // 创建遮罩层，防止 iframe 捕获鼠标事件
      var mask = document.createElement('div');
      mask.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;z-index:9999;cursor:col-resize;';
      document.body.appendChild(mask);
      function onMouseMove(ev) {
        var diff = ev.clientX - startX;
        var newCenter = startCenterWidth + diff;
        // 限制中栏最小 300px
        if (newCenter < 300) newCenter = 300;
        self.centerWidth = newCenter;
      }
      function onMouseUp() {
        self.isResizing = false;
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
        document.body.removeChild(mask);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
      }
      document.body.style.cursor = 'col-resize';
      document.body.style.userSelect = 'none';
      document.addEventListener('mousemove', onMouseMove);
      document.addEventListener('mouseup', onMouseUp);
    },
    // 审核清单区域拖拽调整高度
    startChecklistResize: function(e) {
      var self = this;
      e.preventDefault();
      self.isChecklistResizing = true;
      var startY = e.clientY;
      var startHeight = self.checklistHeight;
      // 创建遮罩层，防止拖动时被 iframe 或其他元素捕获鼠标事件
      var mask = document.createElement('div');
      mask.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;z-index:9999;cursor:row-resize;';
      document.body.appendChild(mask);
      function onMouseMove(ev) {
        var diff = ev.clientY - startY;
        var newHeight = startHeight - diff;
        if (newHeight < 60) newHeight = 60;
        if (newHeight > 400) newHeight = 400;
        self.checklistHeight = newHeight;
      }
      function onMouseUp() {
        self.isChecklistResizing = false;
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
        document.body.removeChild(mask);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
      }
      document.body.style.cursor = 'row-resize';
      document.body.style.userSelect = 'none';
      document.addEventListener('mousemove', onMouseMove);
      document.addEventListener('mouseup', onMouseUp);
    },
    // 自动检查所有清单项
    autoCheckAll: function() {
      var self = this;
      if (!self.activeItem) return;
      var item = self.activeItem;

      // 辅助：设置检查结果（带原因）
      function setResult(key, result, checked, reason) {
        var idx = self.checkList.findIndex(function(c) { return c.key === key });
        if (idx === -1) { return }
        self.$set(self.checkList[idx], 'autoResult', result);
        self.checkList[idx].checked = checked;
        self.checkList[idx].failReason = reason || '';
      }

      // 辅助：获取字段值（同时支持 inputObj 和 item，优先 inputObj）
      function getFieldValue(fieldName) {
        // 1. 优先从 inputObj 取（原始记录模板自定义字段）
        if (self.inputObj && self.inputObj[fieldName]) {
          var v = self.inputObj[fieldName].value;
          if (v !== null && v !== undefined) return v;
        }
        // 2. 从 item（VCK_ORECORD 列表行数据）取
        if (item[fieldName] !== null && item[fieldName] !== undefined) {
          return item[fieldName];
        }
        return undefined;
      }

      // 辅助：判断字段是否有值（不为 null/undefined/空字符串）
      function hasValue(fieldName) {
        var v = getFieldValue(fieldName);
        return v !== null && v !== undefined && String(v).trim() !== '';
      }

      // 辅助：检查环境条件数值是否在合理范围
      function checkEnvCondition(value, min, max) {
        if (!value && value !== 0) return null;
        var num = parseFloat(value);
        if (isNaN(num)) return null;
        if (num >= min && num <= max) return 'pass';
        return 'warn';
      }

      // 辅助：递归遍历 refPmData，收集所有 isnotnull 的字段
      function collectRequiredFields(nodes, result) {
        result = result || [];
        if (!nodes || !Array.isArray(nodes)) return result;
        nodes.forEach(function(n) {
          if (n.type === 'itemField' && n.isnotnull && n.field) {
            result.push({
              field: n.field,
              label: n.label || n.field
            });
          }
          if (n.children && n.children.length > 0) {
            collectRequiredFields(n.children, result);
          }
        });
        return result;
      }

      // ============================================
      // VCK_ORECORD 字段对照：
      //   MNAME=设备名称  SIZETYPE=型号规格  OPCODE=出厂编号
      //   CUSTNAME=送校单位  ADDR=委托方地址
      //   CTEMPERATURE=检校温度  CHUMIDITY=检校湿度  ATMOS=检校大气压
      //   TSTANDARDNAME=检校依据  REGUITEMNAME=依据规程
      //   BILLDATE=检校日期  CERTCODE=证书编号  PTEMPLATEID=原始记录模版
      //   STTDEGREE=不确定度/准确度等级/最大允许误差
      //   AFTERUSE=使用后状态  BEFOREUSE=使用前状态
      //   OTHER=其他条件  OTHERCONTENT=不确定评论
      //   MANUFACTURER=生产厂家  CADDR=校准地点  ISONSITE=是否现场
      // ============================================

      // === 基础信息完整性 ===

      // basic_info: 设备名称、送校单位、委托方地址等基础信息完整
      var basicFields = [
        { field: 'MNAME', label: '设备名称' },
        { field: 'SIZETYPE', label: '型号规格' },
        { field: 'OPCODE', label: '出厂编号' },
        { field: 'CUSTNAME', label: '送校单位' }
      ];
      var basicMissing = basicFields.filter(function(f) { return !hasValue(f.field) });
      var addrMissing = !hasValue('ADDR');
      if (basicMissing.length === 0 && !addrMissing) {
        setResult('basic_info', 'pass', true);
      } else {
        var reason = '';
        if (basicMissing.length > 0) {
          reason = '缺少：' + basicMissing.map(function(f) { return f.label }).join('、');
        }
        if (addrMissing) {
          reason += (reason ? '；' : '缺少：') + '委托方地址';
        }
        setResult('basic_info', basicMissing.length === 0 ? 'warn' : 'fail', false, reason);
      }

      // env_condition: 环境条件（温湿度/气压）已填写且在合理范围
      var temp = getFieldValue('CTEMPERATURE');
      var humidity = getFieldValue('CHUMIDITY');
      var pressure = getFieldValue('ATMOS');
      var hasTemp = hasValue('CTEMPERATURE');
      var hasHum = hasValue('CHUMIDITY');
      var hasPressure = hasValue('ATMOS');
      if (hasTemp || hasHum || hasPressure) {
        var tempCheck = checkEnvCondition(temp, 15, 35);
        var humCheck = checkEnvCondition(humidity, 20, 80);
        var pressureCheck = hasPressure ? 'pass' : null;
        if (tempCheck === 'pass' && humCheck === 'pass' && pressureCheck !== 'warn') {
          setResult('env_condition', 'pass', true);
        } else {
          var envReason = '';
          if (tempCheck === 'warn') { envReason += '温度 ' + temp + '℃ 超出合理范围(15-35℃)' }
          if (humCheck === 'warn') { envReason += (envReason ? '；' : '') + '湿度 ' + humidity + '% 超出合理范围(20-80%)' }
          setResult('env_condition', 'warn', false, envReason);
        }
      } else {
        setResult('env_condition', 'fail', false, '未填写环境条件（温度/湿度/气压）');
      }

      // standard_ref: 依据标准/规程已填写
      if (hasValue('TSTANDARDNAME') || hasValue('REGUITEMNAME')) {
        setResult('standard_ref', 'pass', true);
      } else {
        setResult('standard_ref', 'fail', false, '未填写检校依据标准/规程');
      }

      // === 数据/日期/环境条件复核 ===

      // date_valid: 检校日期合理，无录入错误
      if (hasValue('BILLDATE')) {
        var billDate = new Date(getFieldValue('BILLDATE'));
        var now = new Date();
        var diffDays = Math.floor((now - billDate) / (1000 * 60 * 60 * 24));
        if (diffDays >= -1 && diffDays <= 365) {
          setResult('date_valid', 'pass', true);
        } else if (diffDays > 365) {
          setResult('date_valid', 'warn', false, '检校日期距今天数超过365天(' + diffDays + '天)');
        } else {
          setResult('date_valid', 'fail', false, '检校日期为未来日期(' + (-diffDays) + '天后)');
        }
      } else {
        setResult('date_valid', 'fail', false, '未填写检校日期');
      }

      // data_no_error: 测量数据无超差项
      var hasDataWarning = self.warnings.some(function(w) { return w.type === 'std' || w.type === 'ard_conflict' });
      if (hasDataWarning) {
        setResult('data_no_error', 'warn', false, '检测到标准器冲突或数据异常');
      } else if (hasValue('TSTANDARDNAME') || hasValue('REGUITEMNAME')) {
        setResult('data_no_error', 'pass', true);
      } else {
        setResult('data_no_error', 'warn', false, '无法确认测量数据状态');
      }

      // uncertainty_range: 不确定度/测量结果在规定范围内
      if (hasValue('STTDEGREE')) {
        setResult('uncertainty_range', 'pass', true);
      } else {
        setResult('uncertainty_range', 'warn', false, '未填写不确定度/准确度等级/最大允许误差');
      }

      // === 格式规范 ===

      // format_complete: 报告模板、编号、页码、栏目无缺失
      if (hasValue('CERTCODE') && hasValue('PTEMPLATEID')) {
        setResult('format_complete', 'pass', true);
      } else if (hasValue('CERTCODE')) {
        setResult('format_complete', 'warn', false, '缺少原始记录模板关联');
      } else {
        setResult('format_complete', 'fail', false, '缺少证书编号');
      }

      // === 方法合规性 ===

      // method_valid: 所用检定规程/校准规范/产品标准现行有效
      if (hasValue('TSTANDARDNAME') || hasValue('REGUITEMNAME')) {
        setResult('method_valid', 'pass', true);
      } else {
        setResult('method_valid', 'warn', false, '未填写检校依据，无法确认规程有效性');
      }

      // deviation_approved: 方法偏离有审批和说明
      // VCK_ORECORD 中 OTHER=其他条件, OTHERCONTENT=不确定评论
      // 方法偏离信息在原始记录模板自定义字段中，不在 VCK_ORECORD 标准字段
      var deviation = getFieldValue('OTHER');
      if (!deviation) {
        setResult('deviation_approved', 'pass', true);
      } else {
        var deviationApproved = getFieldValue('OTHERCONTENT');
        if (deviationApproved) {
          setResult('deviation_approved', 'pass', true);
        } else {
          setResult('deviation_approved', 'warn', false, '存在其他条件但未填写不确定评论');
        }
      }

      // === 标准器核查 ===

      // standard_expiry: 标准器在有效期内
      if (self.standardList.length === 0) {
        setResult('standard_expiry', 'warn', false, '未关联标准器信息');
      } else {
        var expiredList = [];
        var nowDate = new Date();
        self.standardList.forEach(function(std) {
          if (std.EXPDATE) {
            var expDate = new Date(std.EXPDATE);
            if (expDate < nowDate) {
              expiredList.push(std.ARDNAME || std.ARDID);
            }
          }
        });
        if (expiredList.length > 0) {
          setResult('standard_expiry', 'fail', false, '以下标准器已过期：' + expiredList.join('、'));
        } else {
          setResult('standard_expiry', 'pass', true);
        }
      }

      // no_conflict: 标准器/人员无时间地域冲突
      var conflictWarnings = self.warnings.filter(function(w) {
        return w.type === 'ard_conflict' || w.type === 'emp_conflict';
      });
      if (conflictWarnings.length > 0) {
        var conflictReason = conflictWarnings.map(function(w) { return w.desc }).join('；');
        setResult('no_conflict', 'fail', false, conflictReason);
      } else {
        setResult('no_conflict', 'pass', true);
      }

      // === 数据真实性 AI ===

      // data_authenticity: 数据真实性与照片示值一致（AI）- 预留，默认需人工确认
      setResult('data_authenticity', 'warn', false, 'AI自动检测功能尚未启用，需人工核对照片示值');

      // === 原始记录完整性 ===

      // record_complete: 原始记录完整（基于模板 isnotnull 字段 + 标准必填字段）
      var requiredFromTemplate = collectRequiredFields(self.refPmData);
      var standardRequired = [
        { field: 'MNAME', label: '设备名称' },
        { field: 'SIZETYPE', label: '型号规格' },
        { field: 'OPCODE', label: '出厂编号' },
        { field: 'TSTANDARDNAME', label: '检校依据' }
      ];

      // 合并模板必填和标准必填，去重
      var allRequired = standardRequired.slice();
      requiredFromTemplate.forEach(function(tmplField) {
        var exists = allRequired.some(function(r) { return r.field === tmplField.field });
        if (!exists) {
          allRequired.push(tmplField);
        }
      });

      var missingFields = allRequired.filter(function(f) { return !hasValue(f.field) });
      if (missingFields.length === 0) {
        setResult('record_complete', 'pass', true);
      } else {
        var recordReason = '原始记录缺少必填字段：' + missingFields.map(function(f) { return f.label }).join('、');
        setResult('record_complete', 'fail', false, recordReason);
      }

      // conclusion_correct: 结论正确
      // VCK_ORECORD 中 AFTERUSE=使用后状态, BEFOREUSE=使用前状态
      // 结论信息可能在原始记录模板自定义字段中
      if (hasValue('AFTERUSE')) {
        setResult('conclusion_correct', 'pass', true);
      } else {
        setResult('conclusion_correct', 'warn', false, '未填写使用后状态，需人工确认结论');
      }
    },
    // 显示帮助文档
    showHelpDoc: function() {
      this.helpDocVisible = true;
      var self = this;
      self.$nextTick(function() {
        if (self.$refs.helpDocModal) {
          self.$refs.helpDocModal.show();
        }
      });
    },

    // AI异常检测（调用后端doCheckAnomaly接口，降级为前端逻辑）
    detectWarnings: async function() {
      var self = this;
      self.warnings = [];
      if (!self.dtlItems || self.dtlItems.length === 0) return;

      // 尝试调用后端A57异常检测接口
      try {
        var ids = self.dtlItems.map(function(item) { return item.ID }).filter(Boolean);
        if (ids.length > 0) {
          var ret = await this.$callAction({
            action: Constants.STORE_NAME + '/detectAnomalies',
            param: { id: ids[0] },
            isBusy: false,
          });
          if (ret && ret.Code === 200 && ret.Data) {
            var anomalies = ret.Data;
            if (Array.isArray(anomalies)) {
              anomalies.forEach(function(a) {
                self.warnings.push({
                  type: a.type || 'std',
                  typeLabel: a.type === 'person' ? '人员冲突' : (a.type === 'timeout' ? '委托超期' : '标准器冲突'),
                  desc: a.message || a.desc || '',
                });
              });
            }
            if (self.warnings.length > 0) return; // 后端检测成功则返回
          }
        }
      } catch (e) {
        // 后端接口调用失败，降级为前端逻辑
      }

      // 前端降级：检测标准器冲突
      var stdMap = {};
      self.dtlItems.forEach(function(item) {
        var stdKey = item.TSTANDARDID || item.TSTANDARDNAME;
        if (stdKey) {
          if (!stdMap[stdKey]) {
            stdMap[stdKey] = [];
          }
          stdMap[stdKey].push(item);
        }
      });
      Object.keys(stdMap).forEach(function(key) {
        var items = stdMap[key];
        var dates = items.map(function(i) { return i.BILLDATE }).filter(function(d) { return d });
        if (dates.length > 1) {
          var uniqueDates = [];
          dates.forEach(function(d) {
            if (uniqueDates.indexOf(d) === -1) uniqueDates.push(d);
          });
          if (uniqueDates.length > 1) {
            self.warnings.push({
              type: 'std',
              typeLabel: '标准器冲突',
              desc: '标准器 "' + key + '" 在多个日期被使用: ' + uniqueDates.join(', '),
            });
          }
        }
      });
      // 前端降级：检测人员冲突
      var checkerMap = {};
      self.dtlItems.forEach(function(item) {
        var checker = item.CHECKER || item.CREATER;
        if (checker) {
          if (!checkerMap[checker]) {
            checkerMap[checker] = [];
          }
          checkerMap[checker].push(item);
        }
      });
      Object.keys(checkerMap).forEach(function(checker) {
        var items = checkerMap[checker];
        var sameDateItems = items.filter(function(i) { return i.BILLDATE === items[0].BILLDATE });
        if (sameDateItems.length > 5) {
          self.warnings.push({
            type: 'person',
            typeLabel: '人员冲突',
            desc: '"' + checker + '" 在 ' + items[0].BILLDATE + ' 有 ' + sameDateItems.length + ' 条记录，可能存在超负荷',
          });
        }
      });
    },
    // 关闭审核 Tooltip 并清理残留的 popper DOM
    closeReviewTip: function() {
      var tip = this.$refs.reviewTip;
      if (tip) {
        tip.hide();
        // HeyUI Tooltip hide 有 300ms 延迟，手动立即清理 body 上的 popper 节点
        var popperEl = tip.$el && tip.$el.querySelector('.h-tooltip');
        if (popperEl) {
          popperEl.style.display = 'none';
        }
      }
      // 兜底：移除 body 上所有残留的 .h-tooltip-popper
      var poppers = document.querySelectorAll('.h-tooltip-popper');
      poppers.forEach(function(el) { el.style.display = 'none' });
    },
    // 通过（根据 mode 区分复核/审批）
    handleApprove: async function() {
      var self = this;
      if (!self.activeItem) {
        self.$error('请先选择一条记录！');
        return;
      }
      // 检查清单验证
      var unchecked = self.checkList.filter(function(item) { return !item.checked });
      if (unchecked.length > 0) {
        self.$error('请先完成所有检查项！');
        return;
      }
      self.isReviewing = true;
      try {
        var actionName = '';
        var params = { REMARK: self.reviewRemark || '', ID: self.activeItem.ID, item: self.activeItem };

        if (self.mode === 'check') {
          // 委托审核模式 → 复核通过(A12)，需要选择下一审批人
          if (!self.VERIFYID) {
            self.$error('请选择审批人！');
            self.isReviewing = false;
            return;
          }
          actionName = 'check';
          params.VERIFYID = self.VERIFYID;
          params.VERIFYER = self.VERIFYER;
        } else if (self.mode === 'verify') {
          // 委托审批模式 → 审批通过(A14)
          actionName = 'verify';
        } else {
          self.$error('未知的审核模式！');
          self.isReviewing = false;
          return;
        }

        // dispatch 前先关闭 Tooltip，避免 STATE 变化导致 v-if 移除 Tooltip DOM 后 popper 残留
        self.closeReviewTip();
        await self.$callAction({
          action: self.currentStoreName + '/' + actionName,
          param: params,
          isBusy: false,
        });
        self.$alert(self.approveBtnText + '成功');
        self.reviewRemark = '';
        self.VERIFYID = '';
        self.VERIFYER = '';
        self.checkList.forEach(function(item) { item.checked = false; item.autoResult = null });
        self.autoSelectNext();
      } catch (e) {
        self.$error('操作失败：' + (e.message || '未知错误'));
      } finally {
        self.isReviewing = false;
      }
    },
    // 驳回
    handleReject: async function() {
      var self = this;
      if (!self.activeItem) {
        self.$error('请先选择一条记录！');
        return;
      }
      self.isReviewing = true;
      try {
        // dispatch 前先关闭 Tooltip
        self.closeReviewTip();
        await self.$callAction({
          action: self.currentStoreName + '/reject',
          param: {
            REMARK: self.reviewRemark || '',
            ID: self.activeItem.ID,
            item: self.activeItem,
          },
          isBusy: false,
        });
        self.$alert(self.rejectBtnText + '成功');
        self.reviewRemark = '';
        self.VERIFYID = '';
        self.VERIFYER = '';
        self.autoSelectNext();
      } catch (e) {
        self.$error('操作失败：' + (e.message || '未知错误'));
      } finally {
        self.isReviewing = false;
      }
    },
    // 撤销审核
    handleReCheck: function() {
      var self = this;
      if (!self.activeItem) return;
      self.isReviewing = true;
      self.$callAction({
        action: self.currentStoreName + '/reCheck',
        param: { ID: self.activeItem.ID, item: self.activeItem },
        isBusy: false,
      }).then(function() {
        self.$alert('撤销审核成功');
      }).catch(function() {
        // $callAction 失败时已弹错误提示
      }).finally(function() {
        self.isReviewing = false;
      });
    },
    // 撤销审批
    handleReVerify: function() {
      var self = this;
      if (!self.activeItem) return;
      self.isReviewing = true;
      self.$callAction({
        action: self.currentStoreName + '/reVerify',
        param: { ID: self.activeItem.ID, item: self.activeItem },
        isBusy: false,
      }).then(function() {
        self.$alert('撤销审批成功');
      }).catch(function() {
        // $callAction 失败时已弹错误提示
      }).finally(function() {
        self.isReviewing = false;
      });
    },
    // 自动选择下一条待操作的记录（根据 mode 查找对应状态）
    autoSelectNext: function() {
      var self = this;
      var currentIndex = self.dtlItems.findIndex(function(item) {
        return self.activeItem && item.ID === self.activeItem.ID;
      });
      var nextItem = null;
      // 根据 mode 确定目标状态
      var targetStates = self.mode === 'verify' ? [5, 19] : [2];
      for (var i = currentIndex + 1; i < self.dtlItems.length; i++) {
        if (targetStates.indexOf(self.dtlItems[i].STATE) !== -1) {
          nextItem = self.dtlItems[i];
          break;
        }
      }
      if (nextItem) {
        self.selectItem(nextItem);
      }
    },
    // 返回列表
    handleBack: function() {
      this.$emit('close');
    },
    // 查看变更记录
    showChangeLog: async function() {
      var self = this;
      if (!self.activeItem) {
        self.$error('请先选择一条记录！');
        return;
      }
      try {
        var ret = await this.$callAction({
          action: Constants.STORE_NAME + '/loadChangeLogs',
          param: { id: self.activeItem.ID },
          isBusy: false,
        });
        self.changeLogs = (ret && ret.Items) || [];
      } catch (e) {
        self.changeLogs = [];
        console.error('加载变更记录失败', e);
      }
      self.showLogModal = true;
      self.$nextTick(function() {
        self.$refs.logModal.show();
      });
    },
    // 导出
    handleExport: function() {
      var self = this;
      if (!self.activeItem) {
        self.$error('请先选择一条记录！');
        return;
      }
      self.$callAction({
        action: self.currentStoreName + '/download',
        param: { items: [self.activeItem] },
        successCall: function(ret) {
          window.open(db.getUrl('upload') + ret.ID, '_blank');
        },
      });
    },
    // 审批人选择回调
    onVerifyChange: function(v) {
      this.VERIFYER = v.value.EMPNAME;
    },
    empSel1: async function(INPUT, callback) {
      var self = this;
      if (self.TEMP1 === INPUT) {
        INPUT = '';
      }
      await self.$callAction({
        action: self.currentStoreName + '/empSel1',
        param: {
          INPUT: INPUT,
          FUNCID: '3be11623d4114bc68a8e63551e861ced',
          DEPTID: self.activeItem ? self.activeItem.ADEPTID : '',
        },
        isBusy: false,
      });
      callback(self.EMPUSER);
    },
  },
};
</script>
<style lang="less" scoped>
.review-wrapper {
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
  background: #f5f5f5;
}
.review-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 20px;
  background: #fff;
  border-bottom: 1px solid #e8e8e8;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
  flex-shrink: 0;
}
.review-header-label {
  color: #999;
  font-size: 13px;
}
.review-header-value {
  font-size: 14px;
  font-weight: bold;
  color: #333;
}
.review-header-divider {
  margin: 0 15px;
  color: #e8e8e8;
}
.review-overdue {
  color: #ed4014 !important;
}
.review-overdue-tag {
  display: inline-block;
  background: #ed4014;
  color: #fff;
  font-size: 11px;
  padding: 1px 6px;
  border-radius: 3px;
  margin-left: 5px;
  font-weight: normal;
}

/* 三栏布局 — flex:1 自适应，随 checklist 拖动自动伸缩 */
.review-container {
  display: flex;
  flex: 1;
  min-height: 200px;
}
.review-left {
  width: 220px;
  overflow-y: auto;
  border-right: 1px solid #e8e8e8;
  background: #fff;
  flex-shrink: 0;
  &--collapsed {
    width: 40px;
    overflow: hidden;
  }
}
.review-center {
  overflow: hidden;
  background: #fff;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
}
.review-right {
  flex: 1;
  overflow: hidden;
  border-left: 1px solid #e8e8e8;
  background: #fff;
  display: flex;
  flex-direction: column;
  min-width: 250px;
}

/* 左栏折叠按钮 */
.review-left-toggle {
  cursor: pointer;
  color: #999;
  font-size: 12px;
  float: right;
  &:hover {
    color: #1890ff;
  }
}

/* 左栏记录列表 */
.review-left-title {
  padding: 10px 12px;
  font-weight: bold;
  font-size: 13px;
  border-bottom: 1px solid #e8e8e8;
  background: #fafafa;
}
.review-left-list {
  overflow-y: auto;
}
.review-left-item {
  display: flex;
  align-items: center;
  padding: 8px 12px;
  cursor: pointer;
  border-bottom: 1px solid #f0f0f0;
  transition: background 0.2s;
  &:hover {
    background: #e6f7ff;
  }
  &--active {
    background: #bae7ff;
    border-left: 3px solid #1890ff;
  }
  &--done {
    .review-left-item-status .status-done {
      color: #19be6b;
    }
  }
  &--rejected {
    .review-left-item-status .status-rejected {
      color: #ed4014;
    }
  }
}
.review-left-item-index {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background: #f0f0f0;
  text-align: center;
  line-height: 24px;
  font-size: 12px;
  color: #666;
  margin-right: 8px;
  flex-shrink: 0;
}
.review-left-item--active .review-left-item-index {
  background: #1890ff;
  color: #fff;
}
.review-left-item-info {
  flex: 1;
  overflow: hidden;
}
.review-left-item-name {
  font-size: 13px;
  color: #333;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.review-left-item-code {
  font-size: 11px;
  color: #999;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  margin-top: 2px;
}
.review-left-item-status {
  flex-shrink: 0;
  font-size: 11px;
  margin-left: 5px;
}
.status-done { color: #19be6b; }
.status-rejected { color: #ed4014; }
.status-pending { color: #ff9900; }
.status-default { color: #999; }

/* 预览区标题 */
.review-panel-title {
  padding: 8px 15px;
  font-weight: bold;
  font-size: 13px;
  border-bottom: 1px solid #e8e8e8;
  background: #fafafa;
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.review-panel-name {
  font-weight: normal;
  color: #999;
  font-size: 12px;
}

/* 中栏预览区 */
.review-preview-area {
  flex: 1;
  overflow: hidden;
  position: relative;
}
.review-preview-placeholder {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  height: 100%;
  color: #c5c5c5;
  font-size: 14px;
  p {
    margin-top: 10px;
  }
}

/* 右栏表单区 */
.review-form-area {
  flex: 1;
  overflow-y: auto;
  padding: 20px 15px;
  background: #eee;
}
.review-form-content {
  background: #fff;
  border: 1px solid #eee;
  padding: 20px 30px;
  min-height: 100%;
  // 只读模式：禁止交互
  pointer-events: none;
  user-select: none;
  /deep/ input, /deep/ textarea, /deep/ select {
    pointer-events: none;
    background: transparent;
  }
}

/* 异常检测区 */
.review-warning {
  background: #fffbe6;
  border: 1px solid #ffe58f;
  border-radius: 4px;
  margin: 10px 15px;
  padding: 10px 15px;
}
.review-warning-title {
  font-weight: bold;
  font-size: 13px;
  margin-bottom: 8px;
  display: flex;
  align-items: center;
  gap: 5px;
}
.review-warning-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
.review-warning-item {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 12px;
}
.review-warning-type {
  display: inline-block;
  padding: 1px 6px;
  border-radius: 3px;
  font-size: 11px;
  color: #fff;
}
.warning-type-std { background: #ed4014; }
.warning-type-person { background: #ff9900; }
.review-warning-desc {
  color: #666;
}

/* 审核清单（可拖拽高度） */
.review-checklist {
  position: relative;
  background: #fff;
  border-top: 1px solid #e8e8e8;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.review-checklist-resizer {
  position: absolute;
  top: -6px;
  left: 0;
  right: 0;
  height: 12px;
  cursor: row-resize;
  z-index: 10;
  display: flex;
  align-items: center;
  justify-content: center;
  &:hover, &--active {
    background: rgba(45, 140, 240, 0.15);
  }
}
.review-checklist-resizer-handle {
  display: flex;
  gap: 3px;
  align-items: center;
  justify-content: center;
  padding: 2px 8px;
  border-radius: 3px;
  background: #e0e0e0;
  transition: background 0.15s;
  span {
    display: block;
    width: 3px;
    height: 3px;
    border-radius: 50%;
    background: #999;
  }
  .review-checklist-resizer:hover &,
  .review-checklist-resizer--active & {
    background: rgba(45, 140, 240, 0.3);
    span { background: #1890ff; }
  }
}
.review-checklist-title {
  display: flex;
  align-items: center;
  padding: 6px 16px;
  font-weight: 600;
  font-size: 13px;
  background: #fafafa;
  border-bottom: 1px solid #f0f0f0;
  flex-shrink: 0;
}
.review-checklist-body {
  flex: 1;
  overflow-y: auto;
  padding: 8px 16px;
}
/* 检查清单分组样式 */
.review-checklist-groups {
  display: flex;
  flex-wrap: wrap;
  gap: 12px 20px;
}
.review-checklist-group {
  flex: 1;
  min-width: 280px;
  max-width: 400px;
  background: #fafafa;
  border: 1px solid #f0f0f0;
  border-radius: 4px;
  padding: 8px 12px;
}
.review-checklist-group-title {
  font-size: 12px;
  font-weight: 600;
  color: #1890ff;
  padding: 4px 0 6px;
  border-bottom: 1px solid #e8e8e8;
  margin-bottom: 6px;
}
.review-checklist-group-items {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.review-checklist-group-items .review-checklist-item {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 2px;
  font-size: 12px;
}
.review-checklist-item-main {
  display: flex;
  align-items: center;
  gap: 4px;
}
.review-checklist-item-reason {
  display: flex;
  align-items: flex-start;
  gap: 4px;
  margin-left: 20px;
  padding: 2px 6px;
  background: #fffbe6;
  border: 1px solid #ffe58f;
  border-radius: 3px;
  font-size: 11px;
  color: #d46b08;
  line-height: 1.4;
  max-width: 100%;
}
.review-checklist-item-reason i {
  flex-shrink: 0;
  margin-top: 1px;
}
/* 旧的检查清单样式（兼容保留） */
.review-checklist-items {
  display: flex;
  flex-wrap: wrap;
  gap: 6px 20px;
}
.review-checklist-item {
  display: flex;
  align-items: center;
  gap: 4px;
}
.check-auto {
  font-size: 12px;
  &--pass { color: #19be6b; }
  &--fail { color: #ed4014; }
  &--warn { color: #ff9900; }
}
/* AI 检查结果（嵌套在清单区域内） */
.review-checklist-warnings {
  margin-top: 10px;
  padding-top: 8px;
  border-top: 1px dashed #e8e8e8;
}
.review-checklist-warnings-title {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  font-weight: 500;
  margin-bottom: 6px;
}
.review-checklist-warnings-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.review-checklist-warnings-item {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  font-size: 12px;
  line-height: 1.5;
}
.review-warning-type {
  display: inline-block;
  padding: 1px 6px;
  border-radius: 3px;
  font-size: 11px;
  white-space: nowrap;
  flex-shrink: 0;
}
.warning-type-std { background: #ed4014; color: #fff; }
.warning-type-person { background: #ff9900; color: #fff; }
.review-warning-desc { color: #515a6e; }

/* 拖动分隔条 */
.review-resizer {
  width: 6px;
  cursor: col-resize;
  background: #f0f0f0;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  z-index: 10;
  transition: background 0.15s;
  &:hover, &--active {
    background: #d0d0d0;
  }
  &--active {
    .review-resizer-line {
      background: #1890ff;
    }
  }
}
.review-resizer-line {
  width: 2px;
  height: 30px;
  background: #c0c0c0;
  border-radius: 1px;
}

/* 底部操作栏 */
.review-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 20px;
  background: #fff;
  border-top: 1px solid #e8e8e8;
  box-shadow: 0 -1px 4px rgba(0, 0, 0, 0.08);
  flex-shrink: 0;
}
.review-footer-left,
.review-footer-right {
  display: flex;
  gap: 10px;
}

/* 变更记录弹窗 */
.review-log-container {
  padding: 15px;
}
.review-log-title {
  font-weight: bold;
  font-size: 14px;
  margin-bottom: 15px;
  padding-bottom: 10px;
  border-bottom: 1px solid #e8e8e8;
}
.review-log-list {
  max-height: 400px;
  overflow-y: auto;
}
.review-log-item {
  display: flex;
  padding: 8px 0;
  border-bottom: 1px solid #f5f5f5;
}
.review-log-time {
  width: 150px;
  color: #999;
  font-size: 12px;
  flex-shrink: 0;
}
.review-log-content {
  flex: 1;
  font-size: 13px;
}
.review-log-user {
  color: #1890ff;
  margin-right: 5px;
}
.review-log-action {
  margin-right: 5px;
}
.review-log-detail {
  color: #999;
  font-size: 12px;
}
.review-log-empty {
  text-align: center;
  color: #999;
  padding: 30px 0;
}

/* 帮助文档弹窗 */
.review-helpdoc-container {
  padding: 15px;
  max-height: 600px;
  overflow-y: auto;
}
.review-helpdoc-title {
  font-weight: bold;
  font-size: 16px;
  margin-bottom: 15px;
  padding-bottom: 10px;
  border-bottom: 1px solid #e8e8e8;
}
.review-helpdoc-section {
  margin-bottom: 15px;
}
.review-helpdoc-section h4 {
  font-size: 14px;
  color: #333;
  margin-bottom: 8px;
  font-weight: 600;
}
.review-helpdoc-section ul {
  margin: 0;
  padding-left: 20px;
}
.review-helpdoc-section li {
  font-size: 13px;
  color: #515a6e;
  line-height: 1.8;
}

/* 检查清单分类（旧样式，保留兼容） */
.review-checklist-category {
  width: 100%;
  font-size: 12px;
  font-weight: 600;
  color: #1890ff;
  padding: 4px 0;
  margin-top: 4px;
  border-bottom: 1px solid #e8e8e8;
}
.review-checklist-item-row {
  display: flex;
  align-items: center;
  gap: 4px;
}
</style>

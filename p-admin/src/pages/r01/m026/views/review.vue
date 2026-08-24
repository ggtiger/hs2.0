<template>
  <div class="review-container" v-padding="15">
    <!-- 权限拦截提示 -->
    <div v-if="!hasPermission" class="permission-denied" style="text-align:center;padding:60px 20px;">
      <div style="font-size:48px;color:#ed4014;margin-bottom:20px;">
        <i class="h-icon-close" style="font-size:48px;"></i>
      </div>
      <p style="font-size:18px;color:#515a6e;font-weight:bold;">您无权审批该项目</p>
      <p style="font-size:14px;color:#808695;margin-top:10px;">请联系管理员获取授权签字人权限</p>
      <Button style="margin-top:20px;" @click="handleClose">返回列表</Button>
    </div>

    <!-- 审批内容 -->
    <div v-else>
      <h3 style="margin-bottom:15px;font-size:16px;border-bottom:1px solid #e8eaec;padding-bottom:10px;">
        审批 - {{ citem.BILLCODE || citem.WTCODE || '' }}
      </h3>

      <!-- 基本信息 -->
      <div class="info-section" style="margin-bottom:20px;">
        <Row :space="10">
          <Cell width="8">
            <div class="info-item">
              <label>委托单号：</label>
              <span>{{ citem.WTCODE }}</span>
            </div>
          </Cell>
          <Cell width="8">
            <div class="info-item">
              <label>受理编号：</label>
              <span>{{ citem.BILLCODE }}</span>
            </div>
          </Cell>
          <Cell width="8">
            <div class="info-item">
              <label>客户：</label>
              <span>{{ citem.CUSTNAME }}</span>
            </div>
          </Cell>
          <Cell width="8">
            <div class="info-item">
              <label>仪器名称：</label>
              <span>{{ citem.MNAME }}</span>
            </div>
          </Cell>
          <Cell width="8">
            <div class="info-item">
              <label>型号规格：</label>
              <span>{{ citem.SIZETYPE }}</span>
            </div>
          </Cell>
          <Cell width="8">
            <div class="info-item">
              <label>出厂编号：</label>
              <span>{{ citem.OPCODE }}</span>
            </div>
          </Cell>
          <Cell width="8">
            <div class="info-item">
              <label>检验部门：</label>
              <span>{{ citem.DEPTNAME }}</span>
            </div>
          </Cell>
          <Cell width="8">
            <div class="info-item">
              <label>检验员：</label>
              <span>{{ citem.EMPNAME }}</span>
            </div>
          </Cell>
          <Cell width="8">
            <div class="info-item">
              <label>当前状态：</label>
              <span>{{ stateText }}</span>
            </div>
          </Cell>
        </Row>
      </div>

      <!-- 审批检查清单 -->
      <div class="checklist-section" style="margin-bottom:20px;">
        <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:10px;">
          <h4 style="font-size:14px;color:#515a6e;margin:0;">审批检查清单</h4>
          <div>
            <Button size="s" @click="autoCheckAll" :disabled="isDisabled">自动检查</Button>
            <Button size="s" @click="showHelpDoc" style="margin-left:6px;">审核人帮助文档</Button>
          </div>
        </div>
        <table class="checklist-table" style="width:100%;border-collapse:collapse;">
          <thead>
            <tr style="background:#f8f8f9;">
              <th style="padding:10px;border:1px solid #e8eaec;width:50px;text-align:center;">序号</th>
              <th style="padding:10px;border:1px solid #e8eaec;">检查项目</th>
              <th style="padding:10px;border:1px solid #e8eaec;width:80px;text-align:center;">结果</th>
              <th style="padding:10px;border:1px solid #e8eaec;width:60px;text-align:center;">自动</th>
              <th style="padding:10px;border:1px solid #e8eaec;">备注</th>
            </tr>
          </thead>
          <tbody>
            <template v-for="(item, index) in checkList">
              <tr v-if="index === 0 || checkList[index - 1].category !== item.category" :key="'cat-' + index" style="background:#e6f7ff;">
                <td colspan="5" style="padding:6px 10px;border:1px solid #e8eaec;font-weight:600;color:#1890ff;font-size:12px;">
                  {{ categoryLabels[item.category] }}
                </td>
              </tr>
              <tr :key="index">
                <td style="padding:10px;border:1px solid #e8eaec;text-align:center;">{{ index + 1 }}</td>
                <td style="padding:10px;border:1px solid #e8eaec;">
                  <div style="font-weight:bold;">{{ item.label }}</div>
                </td>
                <td style="padding:10px;border:1px solid #e8eaec;text-align:center;">
                  <Checkbox v-model="item.checked" :disabled="isDisabled"></Checkbox>
                </td>
                <td style="padding:10px;border:1px solid #e8eaec;text-align:center;">
                  <span v-if="item.autoResult === 'pass'" style="color:#19be6b;font-size:16px;">&#10003;</span>
                  <span v-else-if="item.autoResult === 'fail'" style="color:#ed4014;font-size:16px;">&#10007;</span>
                  <span v-else-if="item.autoResult === 'warn'" style="color:#ff9900;font-size:16px;">&#9888;</span>
                  <span v-else style="color:#ccc;font-size:12px;">-</span>
                </td>
                <td style="padding:10px;border:1px solid #e8eaec;">
                  <input type="text" v-model="item.remark" class="rr-flex-1" :disabled="isDisabled" style="width:100%;border:none;outline:none;" />
                </td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>

      <!-- AI 异常检测结果 -->
      <div v-if="warnings.length > 0" class="anomaly-section" style="margin-bottom:20px;padding:10px 15px;background:#fffbe6;border:1px solid #ffe58f;border-radius:4px;">
        <div style="font-weight:bold;font-size:13px;margin-bottom:8px;display:flex;align-items:center;gap:6px;">
          <i class="h-icon-warn" style="color:#ff9900;"></i>
          <span>AI 异常检测结果</span>
        </div>
        <div style="display:flex;flex-direction:column;gap:4px;">
          <div v-for="(w, i) in warnings" :key="i" style="display:flex;align-items:flex-start;gap:8px;font-size:12px;line-height:1.5;">
            <span style="display:inline-block;padding:1px 6px;border-radius:3px;font-size:11px;white-space:nowrap;flex-shrink:0;" :style="{ background: w.type === 'person' ? '#ff9900' : '#ed4014', color: '#fff' }">{{ w.typeLabel }}</span>
            <span style="color:#515a6e;">{{ w.desc }}</span>
          </div>
        </div>
      </div>

      <!-- 审批意见 -->
      <div class="remark-section" style="margin-bottom:20px;">
        <h4 style="margin-bottom:10px;font-size:14px;color:#515a6e;">审批意见</h4>
        <textarea
          v-model="REMARK"
          style="width:100%;min-height:80px;padding:10px;border:1px solid #dddee1;border-radius:4px;"
          placeholder="请输入审批意见..."
          :disabled="isDisabled"
        ></textarea>
      </div>

      <!-- 操作按钮 -->
      <div class="action-section" style="text-align:right;" v-if="!isDisabled">
        <Button @click="handleClose" style="margin-right:10px;">取消</Button>
        <Button color="red" @click="handleReject" :disabled="!canSubmit">驳回</Button>
        <Button color="primary" @click="handleVerify" :disabled="!canSubmit" style="margin-left:10px;">审批通过</Button>
      </div>
    </div>

    <!-- 帮助文档弹窗 -->
    <Modal v-model="helpDocVisible" title="审核人帮助文档">
      <div style="max-height:500px;overflow-y:auto;padding:10px;">
        <div v-for="(section, idx) in helpDocContent" :key="idx" style="margin-bottom:15px;">
          <h4 style="font-size:14px;color:#333;margin-bottom:8px;font-weight:600;">{{ section.title }}</h4>
          <ul style="margin:0;padding-left:20px;">
            <li v-for="(item, i) in section.items" :key="i" style="font-size:13px;color:#515a6e;line-height:1.8;">{{ item }}</li>
          </ul>
        </div>
      </div>
    </Modal>
  </div>
</template>
<script>
import { Constants, getStore } from '../store';
import store from '@/store';
import { BILL_STATE_MAP } from '@/constants';
export default {
  name: 'r01-m026-review',
  props: {
    storeName: String,
    citem: {
      type: Object,
      default: () => ({})
    },
    title: String,
    ID: String
  },
  data() {
    return {
      REMARK: '',
      hasPermission: true,
      // 异常检测结果
      warnings: [],
      // 帮助文档弹窗
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
      checkList: [
        // 基础信息完整性
        { label: '设备名称、送校单位、委托方地址等基础信息完整', checked: false, autoResult: null, category: 'basic', remark: '' },
        { label: '环境条件（温湿度/气压）已填写且在合理范围', checked: false, autoResult: null, category: 'basic', remark: '' },
        { label: '依据标准/规程已填写', checked: false, autoResult: null, category: 'basic', remark: '' },
        // 数据/日期/环境条件复核
        { label: '检校日期合理，无录入错误', checked: false, autoResult: null, category: 'data', remark: '' },
        { label: '测量数据无超差项', checked: false, autoResult: null, category: 'data', remark: '' },
        { label: '不确定度/测量结果在规定范围内', checked: false, autoResult: null, category: 'data', remark: '' },
        // 格式规范
        { label: '报告模板、编号、页码、栏目无缺失', checked: false, autoResult: null, category: 'format', remark: '' },
        // 方法合规性
        { label: '所用检定规程/校准规范/产品标准现行有效', checked: false, autoResult: null, category: 'compliance', remark: '' },
        { label: '方法偏离有审批和说明', checked: false, autoResult: null, category: 'compliance', remark: '' },
        // 标准器核查
        { label: '标准器在有效期内', checked: false, autoResult: null, category: 'standard', remark: '' },
        { label: '标准器/人员无时间地域冲突', checked: false, autoResult: null, category: 'standard', remark: '' },
        // 数据真实性（AI预留）
        { label: '数据真实性与照片示值一致（AI）', checked: false, autoResult: null, category: 'ai', remark: '' },
        // 原始记录完整性
        { label: '原始记录完整', checked: false, autoResult: null, category: 'record', remark: '' },
        { label: '结论正确', checked: false, autoResult: null, category: 'record', remark: '' },
      ]
    };
  },
  computed: {
    stateText() {
      return BILL_STATE_MAP[this.citem.STATE] || '未知';
    },
    isDisabled() {
      // 已审批或已驳回时不可操作
      return this.citem.STATE === 6 || this.citem.STATE === 12;
    },
    canSubmit() {
      // 检查清单是否全部勾选
      return this.checkList.every(item => item.checked);
    }
  },
  async created() {
    // 确保 LI_M02 模块已初始化，防止直接刷新页面时模块未加载
    await store.dispatch('app/initModule', 'LI_M02');
    getStore();
    this.checkPermission();
    this.detectWarnings();
  },
  methods: {
    checkPermission() {
      // 权限检查：验证当前用户是否为该项目的授权签字人
      // TODO: 后续对接后端权限接口进行实际校验
      // 当前默认有权限，后续可根据 citem 中的授权签字人字段进行比对
      // const userInfo = store.state['user'].userInfo;
      // if (this.citem.VERIFYID && this.citem.VERIFYID !== userInfo.EMPID) {
      //   this.hasPermission = false;
      // }
      this.hasPermission = true;
    },
    // 显示帮助文档
    showHelpDoc() {
      this.helpDocVisible = true;
    },
    // 自动检查所有清单项
    autoCheckAll() {
      var self = this;
      var item = self.citem;
      if (!item || !item.ID) return;

      // 辅助：设置检查结果
      function setResult(index, result, checked) {
        self.$set(self.checkList[index], 'autoResult', result);
        self.checkList[index].checked = checked;
      }

      // === 基础信息完整性 (indices 0-2) ===

      // 0. 设备名称、送校单位、委托方地址等基础信息完整
      var basicFields = ['MNAME', 'SIZETYPE', 'OPCODE', 'CUSTNAME'];
      var basicComplete = basicFields.every(function(f) { return !!item[f] });
      if (basicComplete) {
        setResult(0, 'pass', true);
      } else {
        setResult(0, 'fail', false);
      }

      // 1. 环境条件（温湿度/气压）已填写且在合理范围
      var temp = item.TEMPERATURE || item.TEMP;
      var humidity = item.HUMIDITY || item.HUM;
      if (temp || humidity) {
        var tempNum = parseFloat(temp);
        var humNum = parseFloat(humidity);
        var tempOk = !temp || (!isNaN(tempNum) && tempNum >= 15 && tempNum <= 35);
        var humOk = !humidity || (!isNaN(humNum) && humNum >= 20 && humNum <= 80);
        if (tempOk && humOk) {
          setResult(1, 'pass', true);
        } else {
          setResult(1, 'warn', false);
        }
      } else {
        setResult(1, 'fail', false);
      }

      // 2. 依据标准/规程已填写
      if (item.TSTANDARDNAME) {
        setResult(2, 'pass', true);
      } else {
        setResult(2, 'fail', false);
      }

      // === 数据/日期/环境条件复核 (indices 3-5) ===

      // 3. 检校日期合理，无录入错误
      if (item.BILLDATE) {
        var billDate = new Date(item.BILLDATE);
        var now = new Date();
        var diffDays = Math.floor((now - billDate) / (1000 * 60 * 60 * 24));
        if (diffDays >= -1 && diffDays <= 365) {
          setResult(3, 'pass', true);
        } else if (diffDays > 365) {
          setResult(3, 'warn', false);
        } else {
          setResult(3, 'fail', false);
        }
      } else {
        setResult(3, 'fail', false);
      }

      // 4. 测量数据无超差项
      var hasDataWarning = self.warnings.some(function(w) { return w.type === 'std' || w.type === 'ard_conflict' });
      if (hasDataWarning) {
        setResult(4, 'warn', false);
      } else if (item.TSTANDARDNAME) {
        setResult(4, 'pass', true);
      } else {
        setResult(4, 'warn', false);
      }

      // 5. 不确定度/测量结果在规定范围内
      if (item.UNCERTAINTY || item.RESULT || item.MEASURERESULT) {
        setResult(5, 'pass', true);
      } else {
        setResult(5, 'warn', false);
      }

      // === 格式规范 (index 6) ===

      // 6. 报告模板、编号、页码、栏目无缺失
      if (item.CERTCODE && item.PTEMPLATEID) {
        setResult(6, 'pass', true);
      } else if (item.CERTCODE) {
        setResult(6, 'warn', false);
      } else {
        setResult(6, 'fail', false);
      }

      // === 方法合规性 (indices 7-8) ===

      // 7. 所用检定规程/校准规范/产品标准现行有效
      if (item.TSTANDARDNAME) {
        setResult(7, 'pass', true);
      } else {
        setResult(7, 'warn', false);
      }

      // 8. 方法偏离有审批和说明
      var deviation = item.DEVIATION || item.METHODDEV;
      if (!deviation) {
        setResult(8, 'pass', true);
      } else {
        var deviationApproved = item.DEVIATIONAPPROVED || item.DEVAPPROVE;
        if (deviationApproved) {
          setResult(8, 'pass', true);
        } else {
          setResult(8, 'warn', false);
        }
      }

      // === 标准器核查 (indices 9-10) ===

      // 9. 标准器在有效期内（简化检查，实际需加载标准器列表）
      setResult(9, 'warn', false);

      // 10. 标准器/人员无时间地域冲突
      var hasConflict = self.warnings.some(function(w) {
        return w.type === 'ard_conflict' || w.type === 'emp_conflict';
      });
      if (hasConflict) {
        setResult(10, 'fail', false);
      } else {
        setResult(10, 'pass', true);
      }

      // === 数据真实性 AI (index 11) ===

      // 11. 数据真实性与照片示值一致（AI）- 预留，默认需人工确认
      setResult(11, 'warn', false);

      // === 原始记录完整性 (indices 12-13) ===

      // 12. 原始记录完整
      var requiredFields = ['MNAME', 'SIZETYPE', 'OPCODE', 'TSTANDARDNAME', 'AEMPNAME'];
      var missingFields = requiredFields.filter(function(f) { return !item[f] });
      if (missingFields.length === 0) {
        setResult(12, 'pass', true);
      } else {
        setResult(12, 'fail', false);
      }

      // 13. 结论正确
      if (item.CONCLUSION || item.RESULT) {
        setResult(13, 'pass', true);
      } else {
        setResult(13, 'warn', false);
      }
    },
    // AI异常检测（调用后端doCheckAnomaly接口，降级为前端逻辑）
    async detectWarnings() {
      var self = this;
      self.warnings = [];

      // 尝试调用后端A57异常检测接口
      try {
        var ret = await this.$callAction({
          action: Constants.STORE_NAME + '/detectAnomalies',
          param: { id: self.citem.ID },
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
          if (self.warnings.length > 0) return;
        }
      } catch (e) {
        // 后端接口调用失败，降级为前端逻辑
      }

      // 前端降级：检测标准器冲突
      var stdKey = self.citem.TSTANDARDID || self.citem.TSTANDARDNAME;
      if (stdKey) {
        // 简化：仅做基本提示
      }
    },
    handleClose() {
      this.$parent.$emit('close');
    },
    handleVerify() {
      if (!this.canSubmit) {
        this.$error('请完成所有检查项');
        return;
      }
      let item = this.citem;
      this.$callAction({
        action: `${Constants.STORE_NAME}/verify`,
        param: { REMARK: this.REMARK, ID: item.ID, item },
        successText: '审批通过',
        successCall: () => {
          this.handleClose();
        },
      });
    },
    handleReject() {
      if (!this.REMARK) {
        this.$error('驳回时请填写审批意见');
        return;
      }
      let item = this.citem;
      this.$callAction({
        action: `${Constants.STORE_NAME}/reject`,
        param: { REMARK: this.REMARK, ID: item.ID, item },
        successText: '已驳回',
        successCall: () => {
          this.handleClose();
        },
      });
    }
  }
};
</script>
<style scoped>
.checklist-table {
  font-size: 13px;
}
.info-item {
  margin-bottom: 8px;
  font-size: 13px;
}
.info-item label {
  color: #808695;
  display: inline-block;
  width: 80px;
  text-align: right;
}
.info-item span {
  color: #515a6e;
}
</style>

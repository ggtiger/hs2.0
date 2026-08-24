<template>
  <div class="ecert-page">
    <div class="ecert-header">
      <h1>华溯计量 - 电子证书验证</h1>
      <p class="ecert-subtitle">验证证书真伪，查看电子证书</p>
    </div>

    <!-- 初始状态：输入证书编号 -->
    <div class="ecert-search" v-if="step === 'search'">
      <div class="search-box">
        <input
          type="text"
          v-model="certNo"
          placeholder="请输入证书编号"
          class="search-input"
          @keyup.enter="doCheckPwd"
        />
        <button class="search-btn" @click="doCheckPwd">验证</button>
      </div>
    </div>

    <!-- 需要密码 -->
    <div class="ecert-pwd" v-if="step === 'pwd'">
      <div class="pwd-box">
        <h3>该证书已设置查看密码</h3>
        <p class="pwd-subtitle">证书编号：{{ certData.CERTCODE }}</p>
        <div class="pwd-input-row">
          <input
            type="password"
            v-model="pwd"
            placeholder="请输入查看密码"
            class="search-input"
            @keyup.enter="doViewWithPwd"
          />
          <button class="search-btn" @click="doViewWithPwd">确认</button>
        </div>
        <p class="pwd-error" v-if="pwdError">{{ pwdError }}</p>
      </div>
    </div>

    <!-- 加载中 -->
    <div class="ecert-loading" v-if="step === 'loading'">
      <div class="loading-spinner"></div>
      <p>正在验证证书信息...</p>
    </div>

    <!-- 验证成功：显示证书信息 -->
    <div class="ecert-result" v-if="step === 'result'">
      <div class="cert-badge">
        <div class="cert-icon">&#10003;</div>
        <div class="cert-status">验证通过</div>
      </div>
      <div class="cert-info">
        <div class="cert-row">
          <span class="cert-label">证书编号</span>
          <span class="cert-value">{{ certData.CERTCODE }}</span>
        </div>
        <div class="cert-row">
          <span class="cert-label">委托单位</span>
          <span class="cert-value">{{ certData.CUSTNAME }}</span>
        </div>
        <div class="cert-row">
          <span class="cert-label">设备名称</span>
          <span class="cert-value">{{ certData.MNAME }}</span>
        </div>
        <div class="cert-row">
          <span class="cert-label">规格型号</span>
          <span class="cert-value">{{ certData.SIZETYPE || '-' }}</span>
        </div>
        <div class="cert-row">
          <span class="cert-label">出厂编号</span>
          <span class="cert-value">{{ certData.OPCODE || '-' }}</span>
        </div>
        <div class="cert-row">
          <span class="cert-label">生产厂家</span>
          <span class="cert-value">{{ certData.MANUFACTURER || '-' }}</span>
        </div>
        <div class="cert-row">
          <span class="cert-label">签发日期</span>
          <span class="cert-value">{{ certData.SIGNDATE }}</span>
        </div>
        <div class="cert-row">
          <span class="cert-label">有效期至</span>
          <span class="cert-value">{{ certData.EXPDATE || '-' }}</span>
        </div>
      </div>
      <!-- PDF预览区 -->
      <div class="cert-pdf" v-if="pdfUrl">
        <h4>电子证书</h4>
        <iframe :src="pdfUrl" class="pdf-iframe"></iframe>
      </div>
      <div class="cert-actions">
        <button class="download-btn" @click="downloadCert">下载电子证书</button>
        <button class="back-btn" @click="resetSearch">重新查询</button>
      </div>
    </div>

    <!-- 错误 -->
    <div class="ecert-error" v-if="step === 'error'">
      <div class="error-badge">
        <div class="error-icon">&#10007;</div>
        <div class="error-status">验证失败</div>
      </div>
      <p class="error-msg">{{ errorMsg }}</p>
      <button class="back-btn" @click="resetSearch">重新查询</button>
    </div>

    <div class="ecert-footer">
      <p>华溯计量管理系统 &copy; 版权所有</p>
      <p>如有疑问请联系客服</p>
    </div>
  </div>
</template>

<script>
import { queryCert, viewCert, getUrl } from './store';

export default {
  name: 'out-ecert',
  data() {
    return {
      step: 'search', // search | pwd | loading | result | error
      certNo: '',
      certId: '',
      pwd: '',
      pwdError: '',
      certData: {},
      pdfUrl: '',
      errorMsg: '',
    };
  },
  mounted() {
    let query = this.$route.query;
    let id = query.ID || query.id;
    let certNo = query.certNo || query.certno || query.CERTNO;
    if (id) {
      this.certId = id;
      this.doCheckPwd();
    } else if (certNo) {
      this.certNo = certNo;
      this.doCheckPwd();
    }
  },
  methods: {
    async doCheckPwd() {
      if (!this.certId && !this.certNo.trim()) return;
      this.step = 'loading';
      this.errorMsg = '';
      try {
        let data = await queryCert({ id: this.certId, certNo: this.certNo });
        this.certId = data.ID;
        this.certData = { CERTCODE: data.CERTCODE };
        if (data.NEED_PWD === 1) {
          this.step = 'pwd';
        } else {
          this.doViewWithPwd();
        }
      } catch (e) {
        this.errorMsg = e.message || '未找到该证书信息，请确认证书编号是否正确。';
        this.step = 'error';
      }
    },
    async doViewWithPwd() {
      this.step = 'loading';
      this.pwdError = '';
      try {
        let data = await viewCert({ id: this.certId, pwd: this.pwd });
        this.certData = data;
        let baseUrl = getUrl('url');
        let fileUrl = baseUrl + '/api/file/pdfsy/' + data.FILEID;
        if (data.ACCESS_TOKEN) {
          fileUrl += '?token=' + encodeURIComponent(data.ACCESS_TOKEN);
        }
        this.pdfUrl = fileUrl;
        this.step = 'result';
      } catch (e) {
        let msg = e.message || '查看失败';
        if (msg.indexOf('密码错误') >= 0) {
          this.pwdError = '密码错误，请重新输入';
          this.step = 'pwd';
        } else {
          this.errorMsg = msg;
          this.step = 'error';
        }
      }
    },
    downloadCert() {
      if (this.pdfUrl) {
        window.open(this.pdfUrl, '_blank');
      }
    },
    resetSearch() {
      this.step = 'search';
      this.certNo = '';
      this.certId = '';
      this.pwd = '';
      this.pwdError = '';
      this.certData = {};
      this.pdfUrl = '';
      this.errorMsg = '';
    },
  },
};
</script>

<style scoped>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}
.ecert-page {
  max-width: 600px;
  margin: 0 auto;
  padding: 40px 20px;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
  color: #333;
}
.ecert-header {
  text-align: center;
  margin-bottom: 36px;
}
.ecert-header h1 {
  font-size: 24px;
  color: #1890ff;
  margin-bottom: 8px;
}
.ecert-subtitle {
  color: #999;
  font-size: 14px;
}
.ecert-search {
  display: flex;
  justify-content: center;
  margin-bottom: 30px;
}
.search-box {
  display: flex;
}
.search-input {
  width: 320px;
  height: 42px;
  border: 1px solid #d9d9d9;
  border-radius: 4px 0 0 4px;
  padding: 0 12px;
  font-size: 15px;
  outline: none;
}
.search-input:focus {
  border-color: #1890ff;
}
.search-btn {
  height: 42px;
  padding: 0 28px;
  background: #1890ff;
  color: #fff;
  border: none;
  border-radius: 0 4px 4px 0;
  font-size: 15px;
  cursor: pointer;
}
.search-btn:hover {
  background: #40a9ff;
}
/* 密码输入 */
.ecert-pwd {
  display: flex;
  justify-content: center;
  margin-bottom: 30px;
}
.pwd-box {
  text-align: center;
  background: #f5f7fa;
  border-radius: 8px;
  padding: 30px;
  width: 100%;
}
.pwd-box h3 {
  margin-bottom: 8px;
  color: #333;
}
.pwd-subtitle {
  color: #999;
  font-size: 13px;
  margin-bottom: 20px;
}
.pwd-input-row {
  display: flex;
  justify-content: center;
}
.pwd-input-row .search-input {
  border-radius: 4px 0 0 4px;
  width: 240px;
}
.pwd-input-row .search-btn {
  border-radius: 0 4px 4px 0;
}
.pwd-error {
  color: #ff4d4f;
  font-size: 13px;
  margin-top: 10px;
}
/* 加载中 */
.ecert-loading {
  text-align: center;
  padding: 60px 0;
  color: #999;
}
.loading-spinner {
  display: inline-block;
  width: 36px;
  height: 36px;
  border: 3px solid #e8e8e8;
  border-top: 3px solid #1890ff;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin-bottom: 12px;
}
@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}
/* 验证结果 */
.cert-badge {
  text-align: center;
  margin-bottom: 24px;
}
.cert-icon {
  display: inline-block;
  width: 60px;
  height: 60px;
  line-height: 60px;
  border-radius: 50%;
  background: #52c41a;
  color: #fff;
  font-size: 32px;
  margin-bottom: 8px;
}
.cert-status {
  font-size: 18px;
  font-weight: 600;
  color: #52c41a;
}
.cert-info {
  background: #f5f7fa;
  border-radius: 6px;
  padding: 20px 24px;
  margin-bottom: 24px;
}
.cert-row {
  display: flex;
  align-items: center;
  padding: 8px 0;
  border-bottom: 1px solid #e8e8e8;
}
.cert-row:last-child {
  border-bottom: none;
}
.cert-label {
  color: #999;
  width: 90px;
  flex-shrink: 0;
  font-size: 14px;
}
.cert-value {
  color: #333;
  font-size: 14px;
  font-weight: 500;
}
/* PDF预览 */
.cert-pdf {
  margin-bottom: 24px;
}
.cert-pdf h4 {
  margin-bottom: 12px;
  color: #333;
}
.pdf-iframe {
  width: 100%;
  height: 500px;
  border: 1px solid #e8e8e8;
  border-radius: 4px;
}
.cert-actions {
  text-align: center;
  margin-bottom: 30px;
}
.download-btn {
  display: inline-block;
  padding: 10px 36px;
  background: #1890ff;
  color: #fff;
  border: none;
  border-radius: 4px;
  font-size: 15px;
  cursor: pointer;
  margin-right: 12px;
}
.download-btn:hover {
  background: #40a9ff;
}
.back-btn {
  display: inline-block;
  padding: 10px 36px;
  background: #fff;
  color: #666;
  border: 1px solid #d9d9d9;
  border-radius: 4px;
  font-size: 15px;
  cursor: pointer;
}
.back-btn:hover {
  border-color: #1890ff;
  color: #1890ff;
}
/* 错误 */
.ecert-error {
  text-align: center;
  padding: 40px 0;
}
.error-badge {
  margin-bottom: 16px;
}
.error-icon {
  display: inline-block;
  width: 60px;
  height: 60px;
  line-height: 60px;
  border-radius: 50%;
  background: #ff4d4f;
  color: #fff;
  font-size: 32px;
  margin-bottom: 8px;
}
.error-status {
  font-size: 18px;
  font-weight: 600;
  color: #ff4d4f;
  margin-bottom: 12px;
}
.error-msg {
  color: #999;
  font-size: 14px;
  margin-bottom: 20px;
}
.ecert-footer {
  text-align: center;
  margin-top: 40px;
  color: #ccc;
  font-size: 12px;
}
.ecert-footer p {
  margin-bottom: 4px;
}
</style>

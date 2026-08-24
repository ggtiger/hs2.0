<template>
  <div class="ctp">
    <!-- 头部: 模式选择 + 运行 -->
    <div class="ctp-head">
      <span class="ctp-title">接口测试</span>
      <label class="ctp-mode">
        <input type="radio" value="source" v-model="mode" /> 源码试运行
      </label>
      <label class="ctp-mode" :class="{ disabled: apiLinks.length === 0 }" :title="apiLinks.length === 0 ? '未关联模块接口（保存后自动关联）' : ''">
        <input type="radio" value="api" v-model="mode" :disabled="apiLinks.length === 0" /> 接口执行
      </label>
      <span class="ctp-flex"></span>
      <Button size="s" color="primary" :loading="running" @click="run">运行</Button>
    </div>

    <!-- 参数区（自动从源码识别；接口下拉与参数同 label 宽度/控件宽度） -->
    <div class="ctp-params">
      <div class="ctp-param" v-if="mode === 'api'">
        <label class="ctp-label">接口</label>
        <select v-model="activeApi" class="ctp-control">
          <option v-for="l in apiLinks" :key="l.moduleCode + l.apiCode" :value="l">{{ l.moduleCode }} / {{ l.apiCode }}（{{ l.apiName || l.apiType }}）</option>
        </select>
      </div>
      <div class="ctp-param" v-for="p in params" :key="p">
        <label class="ctp-label">{{ p }}</label>
        <input v-model="values[p]" :placeholder="'@' + p" class="ctp-control" />
      </div>
      <div class="ctp-noparam" v-if="params.length === 0 && mode !== 'api'">无参数（直接运行）</div>
    </div>

    <!-- 结果区 -->
    <div class="ctp-result" v-if="result">
      <div class="ctp-result-head">
        <span :class="result.ok ? 'ctp-ok' : 'ctp-err'">{{ result.ok ? '成功' : '失败' }}</span>
        <span class="ctp-meta" v-if="result.count != null">{{ result.count }} 行</span>
        <span class="ctp-meta" v-if="result.message">{{ result.message }}</span>
      </div>
      <!-- 表格（SQL 查询结果 / 数组数据） -->
      <div v-if="result.columns && result.columns.length > 0" class="ctp-table-wrap">
        <table class="ctp-table">
          <thead>
            <tr><th v-for="c in result.columns" :key="c">{{ c }}</th></tr>
          </thead>
          <tbody>
            <tr v-for="(r, i) in result.rows" :key="i">
              <td v-for="c in result.columns" :key="c">{{ r[c] }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <!-- JSON 树（脚本 Response / 接口返回，可折叠） -->
      <json-view v-else-if="result.jsonObj" :data="result.jsonObj" class="ctp-jsonview" />
      <!-- 执行的 SQL（源码试运行 SQL 时显示） -->
      <pre v-if="result.sql" class="ctp-sql">{{ result.sql }}</pre>
    </div>
  </div>
</template>
<script>
import jsonView from './json-view.vue';
import { Constants } from './code-test-store';

// 接口测试面板（csharp/sql 共用）：
// 自动从源码识别参数 → 输入参数 → 运行 → 表格/JSON树 展示结果
// 双模式: 源码试运行(RCodeAsset A07/A08, 可测未保存内容) / 接口执行(已关联模块接口, 走真实通道)
// 接口调用全部经 store actions (见 code-test-store.js)，.vue 不再 import db
var SYS_PARAMS = ['_USERID_', '_EMPID_', '_DEPTID_'];
var CTP = Constants.STORE_NAME;

export default {
  name: 'code-test-panel',
  components: { jsonView },
  props: {
    kind: { type: String, required: true }, // csharp | sql
    source: { type: String, default: '' },
    code: { type: String, default: '' },
  },
  data() {
    return {
      mode: 'source',
      apiLinks: [],
      activeApi: null,
      values: {},
      running: false,
      result: null,
    };
  },
  computed: {
    // 从源码自动识别参数（去重、排除系统变量）
    params() {
      var src = this.source || '';
      var found = [];
      var push = function(name) {
        if (name && SYS_PARAMS.indexOf(name) < 0 && found.indexOf(name) < 0) found.push(name);
      };
      if (this.kind === 'csharp') {
        var reCs = /P\(\s*"([^"]+)"\s*\)/g;
        var m1;
        while ((m1 = reCs.exec(src)) !== null) push(m1[1]);
      } else {
        var reAt = /@([A-Za-z_][A-Za-z0-9_]*)/g;
        var m2;
        while ((m2 = reAt.exec(src)) !== null) push(m2[1]);
        var reVel = /\$!\{([A-Za-z_][A-Za-z0-9_]*)\}/g;
        while ((m2 = reVel.exec(src)) !== null) push(m2[1]);
      }
      return found;
    },
  },
  watch: {
    params: {
      immediate: true,
      handler(list) {
        var v = {};
        list.forEach((p) => { v[p] = this.values[p] || '' });
        this.values = v;
      },
    },
    code: {
      immediate: true,
      handler() {
        this.loadApiLinks();
      },
    },
  },
  methods: {
    // 查资产已关联的模块接口（接口执行模式下拉）
    loadApiLinks() {
      this.apiLinks = [];
      this.activeApi = null;
      if (!this.code || this.kind === 'js') return;
      this.$callAction({
        action: CTP + '/loadApiLinks',
        param: { code: this.code, kind: this.kind },
        isBusy: false,
        successCall: ({ items }) => {
          this.apiLinks = items || [];
          if (this.apiLinks.length > 0) {
            this.activeApi = this.apiLinks[0];
            this.mode = 'api';
          } else {
            this.activeApi = null;
          }
        },
        errorCall: () => {
          // 无关联时静默（接口执行不可用）
        },
      });
    },
    run() {
      this.running = true;
      this.result = null;
      var useApi = this.mode === 'api' && this.activeApi;
      var actionName;
      var param;
      if (useApi) {
        actionName = CTP + '/runViaApi';
        param = {
          moduleCode: this.activeApi.moduleCode,
          apiCode: this.activeApi.apiCode,
          values: Object.assign({}, this.values),
        };
      } else {
        actionName = CTP + '/runSource';
        param = {
          kind: this.kind,
          code: this.code,
          source: this.source,
          values: Object.assign({}, this.values),
        };
      }
      this.$callAction({
        action: actionName,
        param: param,
        isBusy: false,
        successCall: (ret) => {
          this.result = ret;
          this.running = false;
        },
        errorCall: () => {
          this.result = { ok: false, message: '执行失败' };
          this.running = false;
        },
      });
    },
  },
};
</script>
<style lang="less" scoped>
.ctp {
  border-top: 1px solid #e8eaec;
  padding: 8px 10px;
  background: #fafafa;
  font-size: 12px;
  flex-shrink: 0;
  overflow: auto;
}
.ctp-head {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px 10px;
}
.ctp-title {
  font-weight: 600;
  color: #17233d;
  white-space: nowrap;
}
.ctp-mode {
  display: flex;
  align-items: center;
  gap: 3px;
  color: #515a6e;
  cursor: pointer;
  white-space: nowrap;
  &.disabled {
    color: #c0c4cc;
    cursor: not-allowed;
  }
}
.ctp-flex {
  flex: 1;
}
.ctp-params {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 8px 0 4px;
}
.ctp-param {
  display: flex;
  align-items: center;
  gap: 8px;
}
/* label 统一宽度右对齐，控件统一宽度 */
.ctp-label {
  width: 72px;
  flex-shrink: 0;
  text-align: right;
  color: #515a6e;
  font-family: Consolas, Monaco, monospace;
  font-size: 12px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.ctp-control {
  width: 200px;
  border: 1px solid #dcdee2;
  border-radius: 3px;
  padding: 3px 8px;
  font-size: 12px;
  outline: none;
  background: #fff;
  box-sizing: border-box;
  &:focus {
    border-color: #2d8cf0;
  }
}
.ctp-noparam {
  color: #9ea7b4;
}
.ctp-result {
  margin-top: 8px;
  border-top: 1px dashed #e8eaec;
  padding-top: 6px;
}
.ctp-result-head {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 4px;
}
.ctp-ok {
  color: #52c41a;
  font-weight: 600;
}
.ctp-err {
  color: #ed4014;
  font-weight: 600;
}
.ctp-meta {
  color: #9ea7b4;
}
.ctp-table-wrap {
  max-height: 180px;
  overflow: auto;
  border: 1px solid #e8eaec;
  border-radius: 4px;
}
.ctp-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 12px;
  th, td {
    border-bottom: 1px solid #f0f0f0;
    padding: 4px 8px;
    text-align: left;
    white-space: nowrap;
  }
  th {
    background: #f8f8f9;
    position: sticky;
    top: 0;
  }
}
.ctp-jsonview {
  background: #fff;
  border: 1px solid #e8eaec;
  border-radius: 4px;
  padding: 6px 10px;
  max-height: 300px;
  overflow: auto;
  margin-top: 4px;
}
.ctp-sql {
  background: #f6f8fa;
  border: 1px solid #e8eaec;
  border-radius: 4px;
  padding: 6px 10px;
  font-family: Consolas, Monaco, monospace;
  font-size: 11px;
  max-height: 120px;
  overflow: auto;
  white-space: pre-wrap;
  word-break: break-all;
  margin: 4px 0 0;
  color: #9ea7b4;
}
</style>

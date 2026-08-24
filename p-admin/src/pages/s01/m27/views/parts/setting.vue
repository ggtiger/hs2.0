<template>
  <div class="setting-part">
    <div class="sp-list">
      <div class="sp-list-head">
        <span>LLM 配置</span>
        <Button size="s" color="primary" @click="addNew">+ 新增</Button>
      </div>
      <div
        v-for="it in list"
        :key="it.ID"
        :class="['sp-item', { active: currentId === it.ID }]"
        @click="editRow(it)"
      >
        <div class="sp-item-head">
          <span class="sp-provider">{{ it.PROVIDER }}</span>
          <span :class="['sp-enabled', it.ENABLED === 1 ? 'on' : 'off']">{{ it.ENABLED === 1 ? '启用' : '停用' }}</span>
          <span class="sp-vision" v-if="it.ISVISION === 1">视觉</span>
          <span class="sp-fallback" v-if="it.FALLBACKID">降级</span>
        </div>
        <div class="sp-model">{{ it.MODELNAME }}</div>
        <div class="sp-url">{{ it.BASEURL }}</div>
      </div>
      <div v-if="list.length === 0 && !loading" class="sp-empty">还没有配置，点上方「新增」</div>
    </div>

    <div class="sp-form" v-if="editing">
      <div class="sp-form-title">{{ isNew ? '新增 LLM 配置' : '编辑 LLM 配置' }}</div>
      <div class="sp-grid">
        <div class="sp-field">
          <label>服务商</label>
          <Select v-model="form.PROVIDER" :datas="providerOptions" />
        </div>
        <div class="sp-field">
          <label>模型名</label>
          <input v-model="form.MODELNAME" placeholder="如 deepseek-chat / deepseek-v3" />
        </div>
        <div class="sp-field full">
          <label>BaseURL</label>
          <input v-model="form.BASEURL" placeholder="如 https://api.deepseek.com/v1" />
        </div>
        <div class="sp-field full">
          <label>API Key</label>
          <input v-model="form.APIKEY" type="password" placeholder="保存时自动 AES 加密存储" />
        </div>
        <div class="sp-field">
          <label>输入价格(元/百万token)</label>
          <NumberInput v-model="form.PRICEINPUT" :step="0.1" />
        </div>
        <div class="sp-field">
          <label>输出价格(元/百万token)</label>
          <NumberInput v-model="form.PRICEOUTPUT" :step="0.1" />
        </div>
        <div class="sp-field">
          <label>启用</label>
          <h-switch :value="form.ENABLED===1" @input="form.ENABLED=$event?1:0" />
        </div>
        <div class="sp-field">
          <label>视觉模型</label>
          <h-switch :value="form.ISVISION===1" @input="form.ISVISION=$event?1:0" />
        </div>
        <div class="sp-field full">
          <label>扩展参数(JSON)</label>
          <textarea v-model="form.PARAMS" rows="3" placeholder='{"temperature":0.2,"max_tokens":4096}'></textarea>
        </div>
        <div class="sp-field">
          <label>降级模型</label>
          <Select v-model="form.FALLBACKID" :datas="fallbackOptions" placeholder="无降级" />
        </div>
        <div class="sp-field">
          <label></label>
          <span class="sp-hint">本模型不可用(禁用/删除)时，自动回落到降级模型</span>
        </div>
      </div>
      <div class="sp-actions">
        <Button color="primary" @click="save" :loading="saving">保存</Button>
        <Button color="green" @click="testConn" :loading="testing">测试连接</Button>
        <Poptip content="确定删除该配置？" @confirm="del" v-if="!isNew">
          <Button color="red">删除</Button>
        </Poptip>
        <Button @click="editing = false">取消</Button>
      </div>
      <div v-if="testResult" :class="['sp-test-result', testResult.ok ? 'ok' : 'fail']">
        {{ testResult.text }}
      </div>
    </div>

    <div class="sp-form sp-placeholder" v-else>
      <div>
        <p>← 选择左侧配置进行编辑，或新增</p>
        <p class="sp-tip">AI 设置是所有 AI 功能的基础：先在这里配置 LLM 服务商和 Key，其他分区才能工作</p>
        <p class="sp-tip">「测试连接」会用启用的配置发一条 ping 验证可用性</p>
      </div>
    </div>
  </div>
</template>

<script>
import { getGenericStore } from '@/components/generic-module/generic-store';
import aidev from '@/api/aidev';

const MC = 'RS_M14';

export default {
  name: 'SettingPart',
  data() {
    return {
      storeName: MC,
      loading: false,
      saving: false,
      testing: false,
      list: [],
      editing: false,
      isNew: false,
      currentId: '',
      form: this.emptyForm(),
      testResult: null
    };
  },
  computed: {
    providerOptions() {
      return [
        { key: 'deepseek', title: 'DeepSeek' },
        { key: 'openai', title: 'OpenAI' },
        { key: 'qwen', title: '通义千问' },
        { key: 'zhipu', title: '智谱 GLM' },
        { key: 'other', title: '其他(兼容 OpenAI 协议)' }
      ];
    },
    fallbackOptions() {
      var opts = [{ key: '', title: '(无降级)' }];
      this.list.forEach(m => {
        if (m.ID === this.currentId) return;  // 不能降级到自身
        if (m.ISDELETED === 1) return;
        var label = (m.PROVIDER || '') + '/' + (m.MODELNAME || '') + (m.ENABLED === 1 ? '' : ' [停用]');
        opts.push({ key: m.ID, title: label });
      });
      return opts;
    }
  },
  created() {
    this.storeObj = getGenericStore(MC);
  },
  mounted() {
    this.loadList();
  },
  methods: {
    emptyForm() {
      return {
        ID: '',
        PROVIDER: 'deepseek',
        MODELNAME: '',
        BASEURL: '',
        APIKEY: '',
        PRICEINPUT: 0,
        PRICEOUTPUT: 0,
        ENABLED: 0,
        ISVISION: 0,
        PARAMS: '',
        FALLBACKID: ''
      };
    },
    async loadList() {
      this.loading = true;
      try {
        var QQRY = this.storeObj.storeHelper.getTable('QQRY');
        if (QQRY) { QQRY.setValue('PageSize', 50); QQRY.setValue('PageIndex', 1) }
        await this.$callAction({ action: MC + '/query' });
        var st = this.$store.state[MC];
        this.list = (st && st.dt && st.dt.QRY && st.dt.QRY.data) || [];
        this.$emit('count', { key: 'setting', n: this.list.length });
      } finally {
        this.loading = false;
      }
    },
    async editRow(it) {
      this.currentId = it.ID;
      this.isNew = false;
      this.editing = true;
      this.testResult = null;
      this.form = Object.assign(this.emptyForm(), {
        ID: it.ID,
        PROVIDER: it.PROVIDER,
        MODELNAME: it.MODELNAME,
        BASEURL: it.BASEURL,
        APIKEY: it.APIKEY,
        PRICEINPUT: it.PRICEINPUT,
        PRICEOUTPUT: it.PRICEOUTPUT,
        ENABLED: it.ENABLED,
        ISVISION: it.ISVISION,
        PARAMS: it.PARAMS,
        FALLBACKID: it.FALLBACKID || ''
      });
      await this.$callAction({ action: MC + '/open', param: { ID: it.ID } });
    },
    addNew() {
      this.currentId = '';
      this.isNew = true;
      this.editing = true;
      this.testResult = null;
      this.form = this.emptyForm();
      this.$store.commit(MC + '/INIT', { paths: ['MAIN'] });
      this.$store.commit(MC + '/ADD', { path: 'MAIN', item: {} });
    },
    async save() {
      if (!this.form.MODELNAME) { this.$Message('模型名不能为空'); return }
      if (!this.form.BASEURL) { this.$Message('BaseURL 不能为空'); return }
      this.saving = true;
      try {
        var MAIN = this.storeObj.storeHelper.getTable('MAIN');
        var keys = ['PROVIDER', 'MODELNAME', 'BASEURL', 'APIKEY', 'PRICEINPUT', 'PRICEOUTPUT', 'ENABLED', 'ISVISION', 'PARAMS', 'FALLBACKID'];
        keys.forEach(k => MAIN.setValue(k, this.form[k]));
        await this.$callAction({
          action: MC + '/save',
          successText: '保存成功',
          successCall: () => { this.editing = false; this.loadList() }
        });
      } finally {
        this.saving = false;
      }
    },
    async testConn() {
      this.testing = true;
      this.testResult = null;
      try {
        var ret = await aidev.testLlm();
        if (ret && ret.Code === 200) {
          var d = ret.Data || {};
          this.testResult = { ok: true, text: '连接成功(' + d.ms + 'ms) 模型: ' + d.model + ' 回复: ' + d.reply };
        } else {
          this.testResult = { ok: false, text: (ret && ret.Message) || '连接失败' };
        }
      } catch (e) {
        this.testResult = { ok: false, text: '连接失败: ' + (e.message || e) };
      } finally {
        this.testing = false;
      }
    },
    async del() {
      this.$store.commit(MC + '/INIT', { paths: ['MAIN'] });
      this.$store.commit(MC + '/ADD', { path: 'MAIN', item: { ID: this.currentId } });
      await this.$callAction({
        action: MC + '/delete',
        successText: '删除成功',
        successCall: () => { this.editing = false; this.loadList() }
      });
    }
  }
};
</script>

<style lang="less" scoped>
.setting-part { flex: 1; display: flex; min-height: 0; }
.sp-list {
  width: 260px;
  border-right: 1px solid #e8e8e8;
  background: #fff;
  padding: 10px;
  overflow-y: auto;
  flex-shrink: 0;
}
.sp-list-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 13px;
  font-weight: 600;
  margin-bottom: 8px;
}
.sp-item {
  border: 1px solid #e8e8e8;
  border-radius: 6px;
  padding: 10px;
  margin-bottom: 8px;
  cursor: pointer;
  &:hover { border-color: #91d5ff; }
  &.active { border-color: #2F54EB; background: #e6f7ff; }
}
.sp-item-head { display: flex; gap: 6px; align-items: center; }
.sp-provider { font-size: 13px; font-weight: 600; }
.sp-enabled {
  font-size: 11px; padding: 0 6px; border-radius: 3px;
  &.on { background: #f6ffed; color: #52c41a; }
  &.off { background: #f5f5f5; color: #999; }
}
.sp-vision { font-size: 11px; background: #fff7e6; color: #fa8c16; padding: 0 6px; border-radius: 3px; }
.sp-fallback { font-size: 11px; background: #f0f5ff; color: #2F54EB; padding: 0 6px; border-radius: 3px; }
.sp-model { font-size: 12px; color: #2F54EB; font-family: Consolas, monospace; margin-top: 4px; }
.sp-url { font-size: 11px; color: #999; margin-top: 2px; word-break: break-all; }
.sp-empty { text-align: center; color: #bbb; padding: 30px 0; font-size: 12px; }
.sp-form { flex: 1; min-width: 0; padding: 16px 20px; overflow-y: auto; background: #fff; }
.sp-form-title { font-size: 14px; font-weight: 600; margin-bottom: 14px; }
.sp-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px 20px;
  max-width: 760px;
}
.sp-field {
  display: flex;
  align-items: center;
  gap: 8px;
  &.full { grid-column: span 2; }
  label { font-size: 12px; color: #666; width: 130px; flex-shrink: 0; text-align: right; }
  input, textarea {
    flex: 1;
    border: 1px solid #d9d9d9;
    border-radius: 4px;
    padding: 6px 8px;
    font-size: 13px;
    outline: none;
    min-width: 0;
    &:focus { border-color: #2F54EB; }
  }
  textarea { font-family: Consolas, monospace; font-size: 12px; resize: vertical; }
}
.sp-actions { display: flex; gap: 8px; margin-top: 16px; }
.sp-test-result {
  margin-top: 12px;
  padding: 8px 12px;
  border-radius: 4px;
  font-size: 12px;
  max-width: 760px;
  word-break: break-all;
  &.ok { background: #f6ffed; color: #52c41a; border: 1px solid #b7eb8f; }
  &.fail { background: #fff1f0; color: #f5222d; border: 1px solid #ffa39e; }
}
.sp-placeholder { display: flex; align-items: center; justify-content: center; text-align: center; color: #999; }
.sp-tip { font-size: 12px; color: #bbb; margin-top: 8px; }
.sp-hint { font-size: 11px; color: #999; }
</style>

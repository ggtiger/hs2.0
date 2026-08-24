<template>
  <view-dialog title="添加物流" @on-show="onShow">
    <template slot="body">
      <ToolBar label="关联受理单" :size="16"></ToolBar>
      <div style="padding: 0 20px 10px;">
        <span v-for="(item, idx) in acceptItems" :key="idx" class="h-tag h-tag-blue" style="margin-right:6px;">
          {{ item.ACCEPTCODE }}
        </span>
      </div>
      <ToolBar label="物流信息" :size="16"></ToolBar>
      <rs-form-edit
        ref="form"
        class="rs-flex-col"
        :label-width="100"
        mode="twocolumn"
        :path="$MAIN"
      >
        <FormItem label="类型" prop="REFTYPE">
          <Select v-model="REFTYPE" :datas="typeList" placeholder="请选择类型"></Select>
        </FormItem>
        <FormItem label="快递公司" prop="EXPCOMPANY">
          <input type="text" v-model="EXPCOMPANY" placeholder="请输入快递公司" />
        </FormItem>
        <FormItem label="物流单号" prop="LOGISTICSNO">
          <input type="text" v-model="LOGISTICSNO" placeholder="请输入物流单号" />
        </FormItem>
        <FormItem label="寄出日期" prop="SENDDATE">
          <DatePicker v-model="SENDDATE" placeholder="请选择寄出日期"></DatePicker>
        </FormItem>
        <FormItem label="收件人" prop="RECEIVENAME">
          <input type="text" v-model="RECEIVENAME" placeholder="请输入收件人" />
        </FormItem>
        <FormItem label="电话" prop="RECEIVEPHONE">
          <input type="text" v-model="RECEIVEPHONE" placeholder="请输入联系电话" />
        </FormItem>
        <FormItem label="地址" prop="RECEIVEADDR" :single="true">
          <textarea v-model="RECEIVEADDR" placeholder="请输入收件地址" rows="2" style="width:100%"></textarea>
        </FormItem>
        <FormItem label="备注" prop="REMARK" :single="true">
          <textarea v-model="REMARK" placeholder="请输入备注" rows="2" style="width:100%"></textarea>
        </FormItem>
      </rs-form-edit>
      <ToolBar label="图片上传" :size="16"></ToolBar>
      <div style="padding: 0 20px;">
        <RsUploader
          type="images"
          data-type="file"
          :options="uploadOptions"
          v-model="fileList"
        ></RsUploader>
      </div>
    </template>
    <template slot="footer">
      <Button color="primary" @click="save" :loading="saving">保存</Button>
      <Button @click="close">取消</Button>
    </template>
  </view-dialog>
</template>

<script>
import createStore from '@/store/createStore';
import Store from '@/store';
import RsUploader from '@/components/rs-uploader';

const STORE_NAME = 'r02/m07';
const MODULE_CODE = 'R02_M07';

// 延迟获取 store，确保 R02_M07 模块已通过 initModule 初始化后再调用
let _storeResult = null;

function getLogisticsStore() {
  if (_storeResult) return _storeResult;

  // 如果模块已注册（用户访问过物流管理页面），先注销再重新注册
  // 因为 createStore.getStore 会 registerModule，已存在时会报错
  if (Store.state[STORE_NAME]) {
    Store.unregisterModule(STORE_NAME);
  }

  _storeResult = createStore.getStore({
    config: { moduleCode: MODULE_CODE },
    storeName: STORE_NAME,
    mutations: {},
    actions: {
      add({ commit }) {
        commit('INIT', { paths: ["MAIN", "DTSA", "DTS"] });
        commit('ADD', { path: 'MAIN', item: {} });
      },
    }
  });
  return _storeResult;
}

const mapState = function() {
  return getLogisticsStore().mapState.apply(this, arguments);
};
const mapGetters = function() {
  return getLogisticsStore().mapGetters.apply(this, arguments);
};
const mapDateTable = function(path, aFields, itemProp) {
  return getLogisticsStore().mapDateTable(path, aFields, itemProp);
};

const Constants = {
  STORE_NAME: 'r02/m07',
};

export default {
  name: 'logistics-add',
  props: {
    acceptItems: {
      type: Array,
      default: () => [],
    },
  },
  data() {
    return {
      uploadOptions: {
        max_file_size: '10mb',
      },
      typeList: [
        { title: '样品', key: '1' },
        { title: '证书', key: '2' },
      ],
      saving: false,
    };
  },
  computed: {
    ...mapDateTable('MAIN', [
      'REFTYPE', 'EXPCOMPANY', 'LOGISTICSNO',
      'SENDDATE', 'RECEIVENAME', 'RECEIVEPHONE', 'RECEIVEADDR', 'REMARK', 'FILES'
    ]),
    fileList: {
      get() {
        if (!this.FILES) return [];
        return this.FILES.split(',').filter(id => id).map(id => ({ id, name: '' }));
      },
      set(files) {
        if (!files || !files.length) {
          this.FILES = '';
        } else {
          this.FILES = files.map(f => f.id || f).filter(id => id).join(',');
        }
      },
    },
  },
  components: { RsUploader },
  created() {
    // 初始化 store 数据表（不依赖 view-dialog 的 on-show，因为 rs-modal 没有 isOpened）
    this.$callAction({ action: `${Constants.STORE_NAME}/add` });
  },
  methods: {
    onShow() {
      this.saving = false;
    },
    // 构建 DTSA 子表数据并写入 store
    buildDTSA() {
      let storeHelper = this.$store.state[Constants.STORE_NAME];
      let DTSA = storeHelper.getTable('DTSA');
      if (!DTSA) return;
      DTSA.clear();
      this.acceptItems.forEach(item => {
        DTSA.add({ ACCEPTID: item.ACCEPTID, ACCEPTCODE: item.ACCEPTCODE });
      });
    },
    async save() {
      let validResult = this.$refs.form.valid();
      if (!validResult.result) {
        return;
      }
      if (!this.acceptItems || this.acceptItems.length === 0) {
        this.$alert('未选择受理单');
        return;
      }

      this.saving = true;
      try {
        this.buildDTSA();

        await this.$callAction({
          action: `${Constants.STORE_NAME}/save`,
          successText: '保存成功',
          isSuccessBack: true,
        });

        this.$emit('saved');
        this.close();
      } catch (e) {
        this.$error(e.message || '保存失败');
      } finally {
        this.saving = false;
      }
    },
    close() {
      this.$parent.$emit('close');
      this.$parent.setvalue(false);
    },
  },
};
</script>

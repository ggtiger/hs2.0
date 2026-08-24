<template>
  <view-dialog :title="getTitle()" @on-show="onShow">
    <template slot="body">
      <rs-form-edit
        ref="form"
        class="rs-flex-col"
        :label-width="100"
        mode="twocolumn"
        :path="$MAIN"
        :default-values="defaultValues"
      ></rs-form-edit>
    </template>
    <template slot="footer">
      <Button color="primary" @click="save">保存</Button>
      <Button @click="close">取消</Button>
    </template>
  </view-dialog>
</template>

<script>
import { mapDateTable, Constants } from '../store';
import Add01 from '@/mixins/add01';

export default {
  name: 'r02-m07-add',
  props: {
    storeName: { Type: String },
    title: { Type: String, default: '物流信息' },
    ID: { Type: String },
    // 通用表单默认值：{ 字段名: 值 }。multiautocomplete 子表字段可传选中对象数组
    defaultValues: {
      type: Object,
      default: () => ({}),
    },
  },
  data() {
    return {};
  },
  mixins: [Add01],
  computed: {
    ...mapDateTable('MAIN', []),
  },
  methods: {
    // 取 store 中的 DTSA 子表（关联受理单）
    getDTSA() {
      let s = this.$store.state[Constants.STORE_NAME];
      return s && s.dt && s.dt.DTSA;
    },
    // 重写 save：multiautocomplete 字段已实时同步 DTSA，这里只做校验+提交
    save() {
      let validResult = this.$refs.form.valid();
      if (!validResult.result) {
        return;
      }
      let DTSA = this.getDTSA();
      if (!DTSA || !DTSA.data || DTSA.data.length === 0) {
        this.$alert('请至少添加一个关联受理单');
        return;
      }
      this.$callAction({
        action: `${Constants.STORE_NAME}/save`,
        successText: '操作成功',
        isSuccessBack: true,
        successCall: () => {
          this.$callAction({ action: `${Constants.STORE_NAME}/query`, timeOut: 0 });
        },
      });
    },
    close() {
      this.$parent.$emit('close');
      this.$parent.setvalue(false);
    },
    async onShow() {
      // 防止同一次打开内重复调用（Add01 mixin watch 和 view-dialog @on-show 可能同时触发）
      if (this._onShowPending) return;
      this._onShowPending = true;
      if (this.ID) {
        await this.$callAction({
          action: `${Constants.STORE_NAME}/open`,
          param: { ID: this.ID },
          isBusy: false,
        });
        this.loadAcceptRefs();
      } else {
        await this.$callAction({
          action: `${Constants.STORE_NAME}/add`,
          param: {},
          isBusy: false,
        });
      }
      // 应用默认值（普通空字段 + multiautocomplete 子表）
      if (this.$refs.form && this.$refs.form.applyDefaultValues) {
        this.$refs.form.applyDefaultValues();
      }
      this._onShowPending = false;
    },
    // 编辑回显：标准 open 不返回 DTSA，单独拉取后直写 store 的 DTSA，
    // multiautocomplete 字段会据此显示已关联受理单
    async loadAcceptRefs() {
      try {
        let ret = await this.$callAction({
          action: Constants.STORE_NAME + '/loadAcceptRefs',
          param: { id: this.ID },
          isBusy: false,
        });
        let DTSA = this.getDTSA();
        if (!DTSA) return;
        DTSA.clear();
        ((ret && ret.DTSA) || []).forEach(r => {
          DTSA.add({ ACCEPTID: r.ACCEPTID, ACCEPTCODE: r.ACCEPTCODE });
        });
      } catch (e) {
        // ignore
      }
    },
  },
};
</script>

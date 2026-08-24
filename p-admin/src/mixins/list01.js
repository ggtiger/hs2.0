export default {
  props: {
  },
  provide() {
    // 把列表页组件自身提供给子组件（rs-table-list），用于读取 ISxxx 显隐 computed
    return { visibilityHost: this };
  },
  data() {
    return {
      CHECKID: '',
      CHECKER: '',
      VERIFYID: '',
      VERIFYER: ''
    };
  },
  computed: {
    ISSHOWSUBMIT(state, getters, rootState, rootGetters) {
      // 提交
      let fchecks = this.checks.filter(item => {
        return item.STATE === 1;
      });
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWRESUBMIT(state, getters, rootState, rootGetters) {
      let fchecks = this.checks.filter(item => {
        return item.STATE === 2;
      });
      // 撤销提交
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWCHECK(state, getters, rootState, rootGetters) {
      // 提交
      let fchecks = this.checks.filter(item => {
        return item.STATE === 2;
      });
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWRECHECK(state, getters, rootState, rootGetters) {
      let fchecks = this.checks.filter(item => {
        return item.STATE === 5 || item.STATE === 19;
      });
      // 撤销提交
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWVERIFY(state, getters, rootState, rootGetters) {
      let fchecks = this.checks.filter(item => {
        return item.STATE === 5 || item.STATE === 19;
      });
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWREVERIFY(state, getters, rootState, rootGetters) {
      debugger;
      let fchecks = this.checks.filter(item => {
        return item.STATE === 6 || item.STATE === 20;
      });
      // 撤销受理
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
  },
  mounted() {
  },
  methods: {
    batchCheck(noVerify) {
      if (!this.VERIFYID && !noVerify) {
        this.$error('请选择审批人！');
        return;
      }
      this.$callAction({
        action: `${this.store.Constants.STORE_NAME}/batchCheck`,
        param: { items: this.checks, REMARK: this.REMARK, VERIFYID: this.VERIFYID, VERIFYER: this.VERIFYER },
        successText: '操作成功',
        successCall: () => {
        },
      });
    },
    batchReCheck() {
      this.$callAction({
        action: `${this.store.Constants.STORE_NAME}/batchReCheck`,
        param: { items: this.checks, REMARK: this.REMARK },
        successText: '操作成功',
      });
    },
    batchComCheck() {
      this.$callAction({
        action: `${this.store.Constants.STORE_NAME}/batchComCheck`,
        param: { items: this.checks, REMARK: this.REMARK, VERIFYID: this.VERIFYID, VERIFYER: this.VERIFYER },
        successText: '操作成功',
        successCall: () => {
        },
      });
    },
    batchComReCheck() {
      this.$callAction({
        action: `${this.store.Constants.STORE_NAME}/batchComReCheck`,
        param: { items: this.checks, REMARK: this.REMARK },
        successText: '操作成功',
      });
    },
    batchComVerify() {
      this.$callAction({
        action: `${this.store.Constants.STORE_NAME}/batchComVerify`,
        param: { items: this.checks, REMARK: this.REMARK, VERIFYID: this.VERIFYID, VERIFYER: this.VERIFYER },
        successText: '操作成功',
        successCall: () => {
        },
      });
    },
    batchComReVerify() {
      this.$callAction({
        action: `${this.store.Constants.STORE_NAME}/batchComReVerify`,
        param: { items: this.checks, REMARK: this.REMARK },
        successText: '操作成功',
      });
    },
    batchVerify() {
      this.$callAction({
        action: `${this.store.Constants.STORE_NAME}/batchVerify`,
        param: { items: this.checks, REMARK: this.REMARK },
        successText: '操作成功',
        successCall: () => {
        },
      });
    },
    batchReVerify() {
      this.$callAction({
        action: `${this.store.Constants.STORE_NAME}/batchReVerify`,
        param: { items: this.checks, REMARK: this.REMARK },
        successText: '操作成功',
      });
    },
    batchCheckReject() {
      this.$callAction({
        action: `${this.store.Constants.STORE_NAME}/batchCheckReject`,
        param: { items: this.checks, REMARK: this.REMARK },
        successText: '操作成功',
      });
    },
    batchVerifyReject() {
      this.$callAction({
        action: `${this.store.Constants.STORE_NAME}/batchVerifyReject`,
        param: { items: this.checks, REMARK: this.REMARK },
        successText: '操作成功',
      });
    },
    batchSubmit(noVerify) {
      if (!this.CHECKID && !noVerify) {
        this.$error('请选择审核人！');
        return;
      }
      if (this.$refs.submitTip) { this.$refs.submitTip.hide() }
      this.$callAction({
        action: `${this.store.Constants.STORE_NAME}/batchSubmit`,
        param: { items: this.checks, CHECKID: this.CHECKID, CHECKER: this.CHECKER },
        successText: '操作成功',
        successCall: () => {
        },
      });
    },
    batchReSubmit() {
      this.$callAction({
        action: `${this.store.Constants.STORE_NAME}/batchReSubmit`,
        param: { items: this.checks },
        successText: '操作成功',
        successCall: () => {
        },
      });
    },
    listAction(action, param) {
      this.$callAction({
        action: `${this.store.Constants.STORE_NAME}/${action}`,
        param: param,
        successText: '操作成功',
        successCall: () => {
        },
      });
    },
  },
  watch: {
  },
};

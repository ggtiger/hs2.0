export default {
  props: {
    ID: {
      Type: String,
    },
    title: {
      Type: String,
    },
    storeName: {
      Type: String,
    },
    showQuery: {
      Type: Boolean,
      default: false
    },
    citem: {
      Type: Object,
      default: () => {
        return {};
      }
    }
  },
  provide() {
    // 把当前表单的 moduleCode 提供给子组件（如 rs-form-edit 的 AI 填报按钮）
    const ns = this.storeName;
    let mc = null;
    try {
      mc = ns && this.$store.state[ns] ? this.$store.state[ns].MODULECODE : null;
    } catch (e) {
      mc = null;
    }
    return { aiFormModuleCode: mc, aiFormStoreName: ns, visibilityHost: this };
  },
  data() {
    return {
      loading: false,
      isAutoRefresh: false,
      isModifyBySelf: false,
    };
  },
  computed: {
    // 待提交 = "1";待审核 = "2";已审核 = "3";已作废 = "4";待审批 = "5";已审批 = "6";
    ISSHOWDELETE(state, getters, rootState, rootGetters) {
      // 状态空、待提交
      return this.ID && (!this.STATE || this.STATE === 1) && (this.isModifyBySelf === false || this.CREATEID == '' || (this.isModifyBySelf && this.CREATEID == this.$store.state.user.userInfo.ID));
    },
    ISSHOWSAVE(state, getters, rootState, rootGetters) {
      // 主键、状态空、待提交
      return (!this.STATE || this.STATE === 1) && (this.isModifyBySelf === false || this.CREATEID == '' || (this.isModifyBySelf && this.CREATEID == this.$store.state.user.userInfo.ID));
    },
    ISSHOWSUBMIT(state, getters, rootState, rootGetters) {
      // 主键、待提交
      return (!this.STATE || this.STATE === 1) && (this.isModifyBySelf === false || this.CREATEID == '' || (this.isModifyBySelf && this.CREATEID == this.$store.state.user.userInfo.ID));
    },
    ISSHOWRESUBMIT(state, getters, rootState, rootGetters) {
      // 主键、待审核
      return this.ID && this.STATE === 2 && (this.isModifyBySelf === false || this.CREATEID == '' || (this.isModifyBySelf && this.CREATEID == this.$store.state.user.userInfo.ID));
    },
    ISSHOWCHECK(state, getters, rootState, rootGetters) {
      return this.ID && this.STATE === 2;
    },
    ISSHOWRECHECK(state, getters, rootState, rootGetters) {
      // 主键、已审核、待审批
      return this.ID && (this.STATE === 3 || this.STATE === 5 || this.STATE === 19);
    },
    ISSHOWVERIFY(state, getters, rootState, rootGetters) {
      // 主键、已审核、待审批
      return this.ID && (this.STATE === 3 || this.STATE === 5 || this.STATE === 19);
    },
    ISSHOWREVERIFY(state, getters, rootState, rootGetters) {
      // 主键、已审批
      return this.ID && (this.STATE === 6 || this.STATE === 20);
    },
    ISSHOWINVALID(state, getters, rootState, rootGetters) {
      // 主键 已审批
      return this.ID && this.STATE === 6;
    },
  },
  mounted() {
  },
  methods: {
    getTitle() {
      return (!this.ID ? '新增' : '编辑') + this.title;
    },
    close() {
      this.$parent.setvalue(false);
    },
    closeW() {
      // 关闭 AI 面板（如果打开的话）
      if (this.$refs.form && this.$refs.form.closeAiPanel) {
        this.$refs.form.closeAiPanel();
      }
      this.$parent.setvalue(false);
    },
    save() {
      let validResult = this.$refs.form.valid();
      if (!validResult.result) {
        return;
      }
      console.log("save start");
      this.$callAction({
        action: `${this.storeName}/save`,
        successText: '操作成功',
        isSuccessBack: true,
        successCall: () => {
          // 保存成功后关闭 AI 面板
          if (this.$refs.form && this.$refs.form.closeAiPanel) {
            this.$refs.form.closeAiPanel();
          }
          if (this.showQuery) {
            this.$callAction({ action: `${this.storeName}/advQuery`, timeOut: 0 });
          } else {
            this.$callAction({ action: `${this.storeName}/query`, timeOut: 0 });
          }
        },
      });
    },
    submit(ID) {
      this.$callAction({
        action: `${this.storeName}/submit`,
        successText: '操作成功',
        isSuccessBack: true,
        successCall: () => {
          if (this.showQuery) {
            this.$callAction({ action: `${this.storeName}/advQuery`, timeOut: 0 });
          } else {
            this.$callAction({ action: `${this.storeName}/query`, timeOut: 0 });
          }
        },
        param: { ID },
      });
    },
    reSubmit(ID) {
      // await this.$confirm('确认撤销提交？');
      this.$callAction({
        action: `${this.storeName}/reSubmit`,
        successText: '操作成功',
        isSuccessBack: false,
        successCall: () => {
          if (this.showQuery) {
            this.$callAction({ action: `${this.storeName}/advQuery`, timeOut: 0 });
          } else {
            this.$callAction({ action: `${this.storeName}/query`, timeOut: 0 });
          }
        },
        param: { ID },
      });
    },
    check(ID) {
      this.$callAction({
        action: `${this.storeName}/check`,
        successText: '操作成功',
        isSuccessBack: true,
        successCall: () => {
          if (this.isAutoRefresh) {
            if (this.showQuery) {
              this.$callAction({ action: `${this.storeName}/advQuery`, timeOut: 0 });
            } else {
              this.$callAction({ action: `${this.storeName}/query`, timeOut: 0 });
            }
          }
        },
        param: { ID, item: this.citem },
      });
    },
    reCheck(ID) {
      // await this.$confirm('确认撤销审核？');
      this.$callAction({
        action: `${this.storeName}/reCheck`,
        successText: '操作成功',
        isSuccessBack: false,
        successCall: () => {
        },
        param: { ID, item: this.citem },
      });
    },
    verify(ID) {
      this.$callAction({
        action: `${this.storeName}/verify`,
        successText: '操作成功',
        isSuccessBack: true,
        successCall: () => {
          if (this.isAutoRefresh) {
            if (this.showQuery) {
              this.$callAction({ action: `${this.storeName}/advQuery`, timeOut: 0 });
            } else {
              this.$callAction({ action: `${this.storeName}/query`, timeOut: 0 });
            }
          }
        },
        param: { ID, item: this.citem },
      });
    },
    reVerify(ID) {
      // await this.$confirm('确认撤销审批？');
      this.$callAction({
        action: `${this.storeName}/reVerify`,
        successText: '操作成功',
        isSuccessBack: false,
        successCall: () => {
        },
        param: { ID, item: this.citem },
      });
    },
    invalid(ID) {
      // await this.$confirm('确认作废？');
      this.$callAction({
        action: `${this.storeName}/invalid`,
        successText: '操作成功',
        isSuccessBack: false,
        param: { ID },
      });
    },
    del() {
      this.$callAction({
        action: `${this.storeName}/delete`,
        successText: '删除成功',
        isSuccessBack: true,
        successCall: () => {
          if (this.showQuery) {
            this.$callAction({ action: `${this.storeName}/advQuery`, timeOut: 0 });
          } else {
            this.$callAction({ action: `${this.storeName}/query`, timeOut: 0 });
          }
        },
      });
    },
    addDts(path) {
      this.$store.commit(`${this.storeName}/ADD`, { path });
    },
    moveUp(path, table) {
      this[`$${path}`].upItem({ item: table.currentRow });
      this.$nextTick(() => {
        table.clickCurrentRow(this[path].indexOf(table.currentRow));
      });
    },
    moveDown(path, table) {
      this[`$${path}`].downItem({ item: table.currentRow });
      this.$nextTick(() => {
        table.clickCurrentRow(this[path].indexOf(table.currentRow));
      });
    },
    removeDts(path, table) {
      if (table.currentRow === -1) {
        return;
      }
      this.$store.commit(`${this.storeName}/DEL`, { path, item: table.currentRow });
    },
    async onShow() {
      this.loading = true;
      debugger;
      try {
        if (this.ID) {
          await this.$store.dispatch(`${this.storeName}/open`, { ID: this.ID });
        } else {
          await this.$store.dispatch(`${this.storeName}/add`, {});
        }
      } finally {
        this.loading = false;
      }
    },
  },
  watch: {
    '$parent.isOpened': {
      async handler(v) {
        if (v) {
          await this.onShow();
        }
      },
    },
  },
};

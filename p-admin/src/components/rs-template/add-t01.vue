<template>
  <view-dialog :title="getTitle()">
    <template slot="body">
      <rs-form-edit ref="form" :label-width="80" mode="twocolumn" :path="path"></rs-form-edit>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="close">取消</Button>
      <Poptip content="确认删除?" @confirm="del">
        <Button class="ml5" v-if="!!ID" color="red">删除</Button>
      </Poptip>
      <Button class="ml5" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
export default {
  name: 'add-t01',
  props: {
    ID: {
      Type: String,
    },
    title: {
      Type: String,
    },
    path: {
      Type: Object,
    },
    storeName: {
      Type: String,
    },
  },
  data() {
    return {};
  },
  computed: {},
  mounted() {
    debugger;
  },
  methods: {
    getTitle() {
      return (!this.ID ? '新增' : '编辑') + this.title;
    },
    close() {
      this.$parent.setvalue(false);
    },
    save() {
      let validResult = this.$refs.form.valid();
      if (!validResult.result) {
        return;
      }
      this.$callAction({
        action: `${this.storeName}/save`,
        successText: '保存成功',
        isSuccessBack: true,
        successCall: () => {
          this.$callAction({ action: `${this.store.Constants.STORE_NAME}/query`, timeOut: 0 });
        },
      });
    },
    async del() {
      await this.$confirm('确认删除？');
      this.$callAction({
        action: `${this.storeName}/delete`,
        successText: '删除成功',
        isSuccessBack: true,
        successCall: () => {
          this.$callAction({ action: `${this.store.Constants.STORE_NAME}/query`, timeOut: 0 });
        },
      });
    },
    onShow() {
      if (this.ID) {
        this.$callAction({
          action: `${this.storeName}/open`,
          param: { ID: this.ID },
          isBusy: false,
        });
      } else {
        this.$callAction({
          action: `${this.storeName}/add`,
          param: {},
          isBusy: false,
        });
      }
    },
  },
  watch: {
    '$parent.isOpened': {
      async handler(v) {
        if (v) {
          this.onShow();
        }
      },
    },
  },
};
</script>
<style lang="less" scoped>
@import '~@/theme/modern.less';
/deep/ .h-modal-content {
  border-radius: @card-border-radius;
}
/deep/ .h-form-item {
  margin-bottom: 16px;
}
</style>

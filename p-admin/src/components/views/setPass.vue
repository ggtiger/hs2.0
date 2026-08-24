<template>
  <view-dialog title="修改密码">
    <template #body>
      <Form :model="formItem" :labelWidth="40" ref="form" :rules="rules">
        <FormItem icon="h-icon-lock" prop="OPASSWORD" :required="true">
          <input type="password" v-model="formItem.OPASSWORD" placeholder="请输入原密码..." />
        </FormItem>
        <FormItem icon="h-icon-lock" prop="PASSWORD" :required="true">
          <input type="password" v-model="formItem.PASSWORD" placeholder="请输入新密码..." />
        </FormItem>
        <FormItem icon="h-icon-lock" prop="RPASSWORD" :required="true">
          <input type="password" v-model="formItem.RPASSWORD" placeholder="请重复输入新密码..." />
        </FormItem>
        <div class="rr-text-center">
          <Button style="width:100%" :block="true" color="primary" @click="handleSubmit()">修改密码</Button>
        </div>
      </Form>
    </template>
  </view-dialog>
</template>
<script>
export default {
  name: 'setPass',
  props: {
    showType: {
      type: [String, Number],
    },
    item: { Type: Object },
  },
  data() {
    return {
      formItem: {
        OPASSWORD: '',
        PASSWORD: '',
        RPASSWORD: '',
        USERNAME: '',
      },
      rules: [],
    };
  },
  computed: {},
  methods: {
    async handleSubmit() {
      this.formItem.USERNAME = this.$store.state.user.userInfo.USERNAME;
      let validResult = this.$refs.form.valid();
      if (this.formItem.PASSWORD !== this.formItem.RPASSWORD) {
        this.$error('两次密码输入不正确！');
        return;
      }
      if (validResult.result) {
        await this.$callAsync({
          method: this.$store.dispatch,
          params: ['user/resetPass', { params: this.formItem }],
        });
        this.$parent.setvalue(false);
      }
    },
  },
  mounted() {},
};
</script>

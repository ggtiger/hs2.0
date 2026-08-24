<template>
  <div class="rr-login" :style="mianBanBk">
    <div class="login-panel">
      <div class="login-logo">
        <img src="@/assets/logo.png" height="60px" />
      </div>
      <div class="login-form">
        <Form :model="formItem" :labelWidth="40" ref="form" :rules="rules" :showErrorTip="true">
          <FormItem icon="h-icon-user" prop="USERNAME" :required="true">
            <input type="text" ref="uname" @keyup.enter="handleSubmit" v-model="formItem.USERNAME" placeholder="请输入用户名" />
          </FormItem>
          <FormItem icon="h-icon-lock" prop="PASSWORD" :required="true">
            <input type="password" @keyup.enter="handleSubmit" v-model="formItem.PASSWORD" placeholder="请输入密码" />
          </FormItem>
          <div class="login-options">
            <Checkbox class="rr-check" v-model="formItem.REMEMBERWORD">记住密码</Checkbox>
          </div>
          <div class="login-btn-wrap">
            <Button color="primary" style="width:100%;height:44px;font-size:16px;border-radius:8px;" ref="btn" @keyup.enter="handleSubmit" @click="handleSubmit()">登 录</Button>
          </div>
        </Form>
      </div>
    </div>
  </div>
</template>
<script>
const url = require('@/assets/img/bk-login.jpg');
export default {
  data() {
    return {
      formItem: {
        USERNAME: localStorage.getItem('USERNAME'),
        PASSWORD: localStorage.getItem('PASSWORD'),
        REMEMBERWORD: localStorage.getItem('REMEMBERWORD') == 'true' ? true : false,
      },
      rules: {
        USERNAME: ['USERNAME'],
        PASSWORD: ['PASSWORD'],
        required: ['USERNAME', 'PASSWORD'],
        combineRules: [{}],
      },
      mianBanBk: {
        backgroundImage: 'url(' + url + ')',
      },
    };
  },
  methods: {
    async handleSubmit() {
      let validResult = this.$refs.form.valid();
      //let ret = await this.$store.dispatch('user/login', this.formItem);
      let ret = await this.$callAsync({ method: this.$store.dispatch, params: ['user/login', this.formItem] });
      if (!ret) {
        this.$error(this.$store.state['user'].ERRMESSAGE);
        return;
      }
      if (this.formItem.REMEMBERWORD) {
        localStorage.setItem('USERNAME', this.formItem.USERNAME);
        localStorage.setItem('PASSWORD', this.formItem.PASSWORD);
        localStorage.setItem('REMEMBERWORD', this.formItem.REMEMBERWORD);
      } else {
        localStorage.setItem('USERNAME', '');
        localStorage.setItem('PASSWORD', '');
        localStorage.setItem('REMEMBERWORD', '');
      }

      this.$Message('登录成功');
      this.$router.push({ name: 'wodezhuye' });
    },
    handleReset(name) {
      this.$refs[name].resetFields();
    },
  },
  mounted() {
    this.formItem = {
      USERNAME: localStorage.getItem('USERNAME'),
      PASSWORD: localStorage.getItem('PASSWORD'),
      REMEMBERWORD: localStorage.getItem('REMEMBERWORD') == 'true' ? true : false,
    };
    this.$refs.uname.focus();
  },
};
</script>
<style lang="less" scoped>
@import '~@/theme/index.less';
.rr-login {
  height: 100vh;
  position: relative;
  background-position: center;
  background-repeat: no-repeat;
  background-size: cover;
  display: flex;
  align-items: center;
  justify-content: center;
  &::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.3);
  }
}
.login-panel {
  width: 400px;
  position: relative;
  z-index: 1;
  padding: 40px 36px;
  background: #fff;
  border-radius: 16px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.12), 0 2px 8px rgba(0, 0, 0, 0.06);
}
.login-logo {
  text-align: center;
  margin-bottom: 16px;
  img {
    display: inline-block;
  }
}
.login-title {
  text-align: center;
  font-size: 20px;
  font-weight: 600;
  color: @dark-color;
  margin-bottom: 32px;
}
.login-form {
  .h-form-item {
    margin-bottom: 20px;
  }
  input {
    height: 44px;
    border-radius: 8px;
    font-size: 14px;
  }
}
.login-options {
  margin-bottom: 20px;
}
.login-btn-wrap {
  margin-top: 8px;
}
.rr-check {
  margin-top: 0;
  color: @dark3-color;
}
</style>

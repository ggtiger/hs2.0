<template>
  <div class="rr-login" :style="mianBanBk">
    <div class="h-panel">
      <div class="h-panel-body">
        <div class="rr-left">
          <img src="@/assets/tu_login.jpg" class="rr-left-img" width="310px" />
        </div>
        <div class="rr-right">
          <img src="@/assets/logo4.png" class="rr-login-logo" height="60px" />
          <Form :model="formItem" :labelWidth="40" ref="form" :rules="rules" :showErrorTip="true">
            <FormItem icon="h-icon-user" prop="USERNAME" :required="true">
              <input type="text" v-model="formItem.USERNAME" placeholder="请输入用户名..." />
            </FormItem>
            <FormItem icon="h-icon-lock" prop="PASSWORD" :required="true">
              <input type="password" v-model="formItem.PASSWORD" placeholder="请输入密码..." />
            </FormItem>
            <div class="rr-text-center">
              <Button color="primary" @click="handleSubmit()">登录</Button>
              <Checkbox class="rr-check" v-model="formItem.REMEMBERWORD">记住密码</Checkbox>
            </div>
          </Form>
        </div>
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
        USERNAME: 'admin',
        PASSWORD: '',
        REMEMBERWORD: true,
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
      if (ret.Data !== false) {
        this.$Message('登录成功');
        this.$router.push({ name: 'wodezhuye' });
      } else {
        this.$Error('用户名或密码错误');
      }
    },
    handleReset(name) {
      this.$refs[name].resetFields();
    },
  },
};
</script>
<style lang="css" scoped>
.rr-login {
  height: 100vh;
  position: relative;
  background-position: center;
  background-repeat: no-repeat;
  background-size: 100% 100%;
}
.h-panel {
  width: 760px;
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  -webkit-transform: translate(-50%, -50%);
  background: #fff;
}
.rr-left {
  float: left;
  width: 330px;
}
.rr-left-img {
  display: block;
  margin: 10px 0 30px 10px;
}
.rr-right {
  float: left;
  width: 320px;
  text-align: center;
  margin-left: 60px;
}
.rr-login-logo {
  margin: 60px auto 80px;
  display: block;
}
.rr-check {
  float: right;
  margin-top: 5px;
}
</style>

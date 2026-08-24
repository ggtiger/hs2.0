<template>
  <view-dialog :title="ISADD?'新增角色':'编辑角色'">
    <template slot="body">
      <rs-form-edit
        ref="form"
        class="maxModalH rs-flex-col"
        :label-width="80"
        mode="twocolumn"
        :path="$MAIN"
      ></rs-form-edit>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'RS_M04/A04'" v-if="!ISADD" @confirm="del"><Button class="ml5" color="red">删除</Button></Poptip>
      <Button class="ml5" v-per="'RS_M04/A03'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>

<script>
import { mapDateTable, Constants } from '../store';
export default {
  name: 'zyadd',
  data() {
    return {
      param: {
        keyName: 'ID',
        parentName: 'UPFUNCID',
        titleName: 'FUNCNAME',
        dataMode: 'list',
        getTotalDatas: this.remoteMethod2,
      },
    };
  },
  computed: {
    ...mapDateTable('MAIN', ['ID']),
    ISADD() {
      return !this.ID;
    },
  },
  methods: {
    closeW() {
      this.$parent.setvalue(false);
    },
    save() {
      let validResult = this.$refs.form.valid();
      if (!validResult.result) {
        return;
      }
      this.$callAction({
        action: `${Constants.STORE_NAME}/save`,
        successText: '保存成功',
        isSuccessBack: true,
      });
    },
    async del() {
      await this.$confirm('确认删除？');
      this.$callAction({
        action: `${Constants.STORE_NAME}/delete`,
        successText: '删除成功',
        isSuccessBack: true,
      });
    },
  },
  watch: {
    ID: function() {},
    ISADD: {
      handler(v) {
        if (v + '' === '1') {
          this.$callAction({ action: `${Constants.STORE_NAME}/add`, param: {}, isBusy: false });
        }
      },
      immediate: true,
    },
  },
};
</script>

<style scoped>
.maxModalH {
  overflow: auto;
}
</style>

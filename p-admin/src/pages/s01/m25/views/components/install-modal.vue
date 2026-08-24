<template>
  <rs-modal ref="modal" :width="560">
    <div class="tpl-install" v-if="tpl">
      <div class="tpl-install-title">
        <span>安装模板 · {{ tpl.TEMPLATENAME }}</span>
        <a class="tpl-close" @click="close"><Icon type="md-close" /></a>
      </div>
      <div class="tpl-install-tip">
        模板脚本中的变量将替换为你填写的值。安装注册后跳转到升级详情页预览并执行（单事务，可回滚）。
      </div>
      <Form :label-width="110">
        <FormItem v-for="v in varDefs" :key="v.name" :label="v.label || v.name" :required="!!v.required">
          <Input v-model="varValues[v.name]" :placeholder="v.default ? '默认: ' + v.default : '请输入'" />
        </FormItem>
      </Form>
      <div class="tpl-install-footer">
        <Button @click="close">取消</Button>
        <Button color="primary" :loading="installing" @click="doInstall">注册安装</Button>
      </div>
    </div>
  </rs-modal>
</template>

<script>
import { Constants } from '../../store';

export default {
  name: 'TplInstallModal',
  data() {
    return {
      tpl: null,
      varDefs: [],
      varValues: {},
      installing: false
    };
  },
  methods: {
    open(row) {
      this.tpl = row;
      // 解析 VARIABLES JSON → 变量表单
      let defs = [];
      try {
        defs = row.VARIABLES ? JSON.parse(row.VARIABLES) : [];
      } catch (e) {
        defs = [];
      }
      // 兜底：老模板无 VARIABLES 时给标准三变量
      if (!defs.length) {
        defs = [
          { name: 'MODULECODE', label: '模块编码', required: true },
          { name: 'MODULENAME', label: '模块名称', required: true },
          { name: 'PARENTFUNCID', label: '父菜单ID', required: true }
        ];
      }
      this.varDefs = defs;
      let values = {};
      defs.forEach(v => { values[v.name] = v.default || '' });
      this.varValues = values;
      this.$refs.modal.show();
    },
    async doInstall() {
      // 必填校验
      for (let v of this.varDefs) {
        if (v.required && !this.varValues[v.name]) {
          this.$Message.warning('请填写 ' + (v.label || v.name));
          return;
        }
      }
      this.installing = true;
      try {
        let ret = await this.$callAction({
          action: Constants.STORE_NAME + '/installTemplate',
          param: { templateId: this.tpl.ID, variables: this.varValues },
          isBusy: false,
        });
        this.$Message.success((ret && ret.message) || '已注册升级');
        this.close();
        // 跳转升级详情页：预览/执行/回滚（复用 mAIDevUPG 完整管道）
        if (ret && ret.upgradeId) {
          this.$router.push('/s01/mAIDevUPG/detail/' + ret.upgradeId);
        }
      } catch (e) {
        // $callAction 失败时已弹错误提示
      } finally {
        this.installing = false;
      }
    },
    close() {
      this.$refs.modal.hide();
    }
  }
};
</script>

<style lang="less" scoped>
.tpl-install {
  padding: 4px 2px;
}
.tpl-install-title {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 15px;
  font-weight: 600;
  color: #17233d;
  padding-bottom: 10px;
  border-bottom: 1px solid #e8eaec;
  .tpl-close { color: #9ea7b4; cursor: pointer; }
}
.tpl-install-tip {
  color: #9ea7b4;
  font-size: 12px;
  padding: 10px 0;
}
.tpl-install-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding-top: 6px;
}
</style>

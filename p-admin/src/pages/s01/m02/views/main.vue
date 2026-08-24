<template>
  <div class="s01-m02-main">
  <list-t01
    title="模块"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    addper="RS_M02/A04"
    ref="list"
    style="height:100%"
  >
    <rs-modal title="新增模块" ref="madd">
      <rsAdd :DID="CDID"></rsAdd>
    </rs-modal>
    <template slot="header-action">
      <Button class="ml5" color="green" icon="h-icon-plus" @click="openWizard">向导创建</Button>
    </template>
    <TableItem title="操作" :width="120" align="center" fixed="right" slot="table-action">
      <template slot-scope="{data}">
        <button class="h-btn h-btn-s h-btn-blue" @click.stop="openConfig(data)">模块配置</button>
      </template>
    </TableItem>
  </list-t01>
  <Modal v-model="configVisible" :title="configTitle" fullScreen hasCloseIcon>
    <mod-config
      ref="modConfig"
      v-if="configVisible"
      :moduleCodeProp="configModuleCode"
      @close="configVisible = false"
      @saved="onConfigSaved"
      @save-error="configSaving = false"
    ></mod-config>
    <div slot="footer">
      <Button @click="configVisible = false">关闭</Button>
      <Button color="primary" class="ml5" :loading="configSaving" @click="saveConfig">保存</Button>
    </div>
  </Modal>
  <Modal v-model="wizardVisible" title="新建业务模块向导" fullScreen hasCloseIcon>
    <module-wizard
      v-if="wizardVisible"
      @close="wizardVisible = false"
      @done="onWizardDone"
    ></module-wizard>
  </Modal>
  </div>
</template>

<script>
import rsAdd from './add.vue';
import modConfig from '@/pages/s01/m18/views/config.vue';
import moduleWizard from '@/pages/s01/m18/views/components/module-wizard.vue';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 's01-m02-main',
  components: {
    rsAdd,
    modConfig,
    moduleWizard,
  },
  computed: {
    configTitle() {
      return this.configModuleCode ? '模块配置 - ' + this.configModuleCode : '模块配置';
    }
  },
  data() {
    return {
      CDID: '',
      citem: {},
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [
        {
          title: '系统管理',
        },
        {
          title: '模块管理',
        },
      ],
      configVisible: false,
      configModuleCode: '',
      configSaving: false,
      wizardVisible: false,
    };
  },

  methods: {
    add() {
      this.CDID = '';
      this.$callAction({ action: `${Constants.STORE_NAME}/open`, param: { DID: '-9' }, isBusy: false });
      this.$refs.madd.show();
    },
    clickRow(row) {
      this.CDID = row.ID;
      this.$callAction({ action: `${Constants.STORE_NAME}/open`, param: { DID: row.ID }, isBusy: false });
      this.$refs.madd.show();
    },
    listAction(action, param) {
      switch (action) {
        case 'add':
          this.add(param);
          break;
        default:
          break;
      }
    },
    openConfig(row) {
      this.configModuleCode = row.MODULECODE;
      this.configSaving = false;
      this.configVisible = true;
    },
    saveConfig() {
      if (this.$refs.modConfig) {
        this.configSaving = true;
        this.$refs.modConfig.handleSave();
      }
    },
    onConfigSaved() {
      this.configSaving = false;
      this.configVisible = false;
    },
    openWizard() {
      this.wizardVisible = true;
    },
    onWizardDone() {
      this.wizardVisible = false;
      // 刷新列表
      this.$refs.list && this.$refs.list.query(1);
    },
  },
};
</script>
<style scoped>
.s01-m02-main {
  height: 100%;
  display: flex;
  flex-direction: column;
}
</style>

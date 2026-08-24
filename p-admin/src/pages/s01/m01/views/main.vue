<template>
  <list-t01
    title="资源"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    addper="RS_M01/A03"
  >
    <template slot="header-action">
      <Button v-per="'RS_M01/A03'" class="ml5 rr-flex-1" @click="listAction('add','')"  color="primary">新增资源</Button>
      <Button v-per="'RS_M01/A03'" class="ml5" @click="syncResource">同步资源</Button>
    </template>
    <rs-modal title="新增资源" ref="madd">
      <rsAdd :DID="CDID"></rsAdd>
    </rs-modal>
    <rs-modal title="界面设置(旧)" ref="uisetOld" :width="1100">
      <ui-set :item="citem"></ui-set>
    </rs-modal>
    <Modal v-model="uiSetFullShow" :title="uiSetFullTitle" fullScreen hasCloseIcon>
      <ui-set-full
        v-if="uiSetFullShow"
        ref="uiSetFull"
        :resourceId="uiSetResourceId"
        :resourceName="uiSetResourceName"
        @close="uiSetFullShow = false"
        @saved="onUiSetSaved"
        @saving-change="uiSetFullSaving = $event"
      ></ui-set-full>
      <div slot="footer">
        <Button @click="uiSetFullShow = false">取消</Button>
        <Button color="primary" class="ml5" :loading="uiSetFullSaving" @click="onUiSetSave">保存</Button>
      </div>
    </Modal>
    <Modal v-model="syncShow" title="同步资源" :width="600" hasCloseIcon>
      <div v-if="syncLoading" style="text-align:center;padding:20px;">
        <Loading text="加载中..."></Loading>
      </div>
      <div v-else-if="unregisteredTables.length===0" style="text-align:center;padding:20px;">
        所有物理表均已注册为资源
      </div>
      <div v-else>
        <p style="margin-bottom:10px;color:#999;">以下物理表尚未注册为TABLE类型资源，勾选后点击"生成资源"：</p>
        <Table :datas="unregisteredTables" border checkbox @select="syncSelect">
          <TableItem title="表名" prop="TABLENAME" :width="200"></TableItem>
          <TableItem title="说明" prop="COMMENTS"></TableItem>
          <TableItem title="字段数" prop="COLUMN_COUNT" :width="80"></TableItem>
        </Table>
      </div>
      <div slot="footer">
        <Button @click="syncShow=false">关闭</Button>
        <Button color="primary" :loading="batchLoading" :disabled="selectedTables.length===0" @click="batchCreate">生成资源</Button>
      </div>
    </Modal>
  </list-t01>
</template>

<script>
import rsAdd from './add.vue';
import refSet from './refSet.vue';
import uiSet from './uiSet.vue';
import uiSetFull from './uiSetFull.vue';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 's01-m01-main',
  components: {
    rsAdd,
    refSet,
    uiSet,
    uiSetFull,
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
          title: '资源管理',
        },
      ],
      syncShow: false,
      syncLoading: false,
      batchLoading: false,
      unregisteredTables: [],
      selectedTables: [],
      // 页面配置弹窗（uiSetFull）
      uiSetFullShow: false,
      uiSetResourceId: '',
      uiSetResourceName: '',
      uiSetFullSaving: false,
    };
  },
  computed: {
    uiSetFullTitle() {
      return this.uiSetResourceName ?
        `页面配置 - ${this.uiSetResourceName}` :
        '页面配置';
    },
  },

  methods: {
    add() {
      this.CDID = '';
      this.$refs.madd.show();
    },
    clickRow(row) {
      this.CDID = row.ID;
      this.$refs.madd.show();
    },
    clickUiSet(row) {
      this.citem = row;
      this.uiSetResourceId = row.ID;
      this.uiSetResourceName = row.RESOURCENAME;
      this.uiSetFullSaving = false;
      this.uiSetFullShow = true;
    },
    onUiSetSaved() {
      // 保存后刷新资源列表（resourceName 等可能变化）
      this.$callAction({
        action: `${Constants.STORE_NAME}/query`,
        timeOut: 0,
      });
    },
    onUiSetSave() {
      // 通过 ref 调用 uiSetFull 的保存方法
      if (this.$refs.uiSetFull) {
        this.$refs.uiSetFull.onSave();
      }
    },
    clickUiSetOld(row) {
      this.citem = row;
      this.$refs.uisetOld.show();
    },
    listAction(action, param) {
      switch (action) {
        case 'add':
          this.add(param);
          break;
        case 'uiset':
          this.clickUiSet(param);
          break;
        case 'uisetOld':
          this.clickUiSetOld(param);
          break;
        default:
          break;
      }
    },
    async syncResource() {
      this.syncShow = true;
      this.syncLoading = true;
      this.selectedTables = [];
      try {
        let ret = await this.$callAction({ action: `${Constants.STORE_NAME}/queryUnregistered`, param: { INPUT: '' }, isBusy: false });
        this.unregisteredTables = ret || [];
      } catch (e) {
        this.$Message.error(e.message || '查询失败');
      } finally {
        this.syncLoading = false;
      }
    },
    syncSelect(selections) {
      this.selectedTables = selections.map(s => s.TABLENAME);
    },
    async batchCreate() {
      if (this.selectedTables.length === 0) return;
      this.batchLoading = true;
      try {
        let ret = await this.$callAction({ action: `${Constants.STORE_NAME}/batchCreateResources`,
          param: {
            TABLES: this.selectedTables,
          },
          isBusy: false });
        this.$Message.success(`成功生成${ret.createdCount}个资源`);
        this.syncShow = false;
        // 刷新列表
        this.$callAction({
          action: `${Constants.STORE_NAME}/query`,
          timeOut: 0,
        });
      } catch (e) {
        this.$Message.error(e.message || '生成失败');
      } finally {
        this.batchLoading = false;
      }
    },
  },
};
</script>

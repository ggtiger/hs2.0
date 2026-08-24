<template>
  <list-t01
    ref="list"
    title="模板市场"
    :bcDatas="datas"
    :store="store"
    :showQuery="true"
  >
    <template slot="table-action">
      <TableItem title="操作" :width="170" fixed="right">
        <template slot-scope="{ data }">
          <a class="tpl-action" @click="openPreview(data)">预览</a>
          <a v-per="'RS_M25/A06'" class="tpl-action primary" @click="openInstall(data)">安装</a>
          <Poptip :content="'确定删除模板 ' + data.TEMPLATENAME + '？'" transfer @confirm="doDelete(data)">
            <a v-per="'RS_M25/A07'" class="tpl-action danger">删除</a>
          </Poptip>
        </template>
      </TableItem>
    </template>

    <!-- 预览弹窗 -->
    <rs-modal ref="previewModal" :width="1000">
      <div class="tpl-preview" v-if="previewRow">
        <div class="tpl-preview-title">
          <span>{{ previewRow.TEMPLATENAME }}（{{ previewRow.TEMPLATECODE }}）</span>
          <a class="tpl-close" @click="$refs.previewModal.hide()"><Icon type="md-close" /></a>
        </div>
        <div class="tpl-preview-meta">
          来源：{{ previewRow.SOURCEINFO || '-' }} · 版本：{{ previewRow.VERSION || '-' }} · 创建：{{ previewRow.CREATER || '-' }} {{ previewRow.CREATETIME || '' }}
        </div>
        <div class="tpl-preview-desc" v-if="previewRow.DESCRIPTION">{{ previewRow.DESCRIPTION }}</div>
        <pre class="tpl-script">{{ previewScript || '（加载中…）' }}</pre>
        <div class="tpl-preview-footer">
          <Button @click="$refs.previewModal.hide()">关闭</Button>
        </div>
      </div>
    </rs-modal>

    <!-- 安装弹窗（变量表单） -->
    <tpl-install-modal ref="installModal" />
  </list-t01>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import TplInstallModal from './components/install-modal.vue';

export default {
  name: 's01-m25-main',
  components: { TplInstallModal },
  data() {
    return {
      datas: [{ title: '系统管理' }, { title: '模板市场' }],
      store: { mapState, mapGetters, mapDateTable, Constants },
      previewRow: null,
      previewScript: ''
    };
  },
  methods: {
    async openPreview(row) {
      this.previewRow = row;
      this.previewScript = '';
      this.$refs.previewModal.show();
      try {
        this.previewScript = await this.$callAction({
          action: Constants.STORE_NAME + '/loadPreviewScript',
          param: { id: row.ID },
          isBusy: false,
        });
      } catch (e) {
        // $callAction 失败时已弹错误提示
        this.previewScript = '（脚本加载失败）';
      }
    },
    openInstall(row) {
      this.$refs.installModal.open(row);
    },
    async doDelete(row) {
      try {
        // 删除模板: 物理删除（<d> 行只带键值; 删除前有版本快照, 可在版本中心找回）
        // 注意: 不能用 INIT+ADD+save 改 ISDELETED——ADD 行会被当新增 INSERT, 撞 NOT NULL
        await this.$callAction({
          action: Constants.STORE_NAME + '/deleteTemplate',
          param: { id: row.ID },
          isBusy: false,
        });
        this.$Message.success('已删除');
        this.$refs.list.query();
      } catch (e) {
        // $callAction 失败时已弹错误提示
      }
    }
  }
};
</script>

<style lang="less" scoped>
.tpl-action {
  margin-right: 10px;
  cursor: pointer;
  &.primary { color: #2d8cf0; }
  &.danger { color: #ed4014; }
}
.tpl-preview {
  padding: 4px 2px;
}
.tpl-preview-title {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 15px;
  font-weight: 600;
  color: #17233d;
  padding-bottom: 8px;
  border-bottom: 1px solid #e8eaec;
  .tpl-close { color: #9ea7b4; cursor: pointer; }
}
.tpl-preview-meta {
  color: #9ea7b4;
  font-size: 12px;
  padding: 8px 0 4px;
}
.tpl-preview-desc {
  color: #515a6e;
  font-size: 13px;
  padding-bottom: 8px;
}
.tpl-script {
  height: 420px;
  overflow: auto;
  background: #f8f8f9;
  border: 1px solid #e8eaec;
  border-radius: 4px;
  padding: 10px;
  font-family: Consolas, Monaco, monospace;
  font-size: 12px;
  line-height: 1.6;
  white-space: pre-wrap;
  word-break: break-all;
}
.tpl-preview-footer {
  display: flex;
  justify-content: flex-end;
  padding-top: 10px;
}
</style>

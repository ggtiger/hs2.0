<template>
  <div class="ver-center">
    <!-- Tab 切换 -->
    <div class="ver-tabs">
      <span :class="{ active: activeTab === 'version' }" @click="activeTab = 'version'">版本记录</span>
      <span :class="{ active: activeTab === 'release' }" @click="activeTab = 'release'">发布管理</span>
    </div>

    <!-- Tab1: 版本记录（原有功能） -->
    <div v-show="activeTab === 'version'" class="ver-tab-content">
      <div class="ver-batch-bar">
        <Button size="s" color="primary" @click="openBatchMark">批量打标</Button>
      </div>
      <list-t01
        ref="list"
        title="版本中心"
        :bcDatas="datas"
        :store="store"
        :showQuery="false"
      >
        <template slot="table-action">
          <TableItem title="操作" :width="200" fixed="right">
            <template slot-scope="{ data }">
              <a class="ver-action" @click="openDiff(data)">对比</a>
              <a class="ver-action" @click="openHistory(data)">历史</a>
              <Poptip v-if="data.OPTYPE !== 'rollback'" :content="'确定回滚到该版本（v' + data.VERSION + '）？回滚会生成一个新版本'" transfer @confirm="doRollback(data)">
                <a v-per="'RS_M22/A05'" class="ver-action ver-danger">回滚</a>
              </Poptip>
            </template>
          </TableItem>
        </template>
      </list-t01>
    </div>

    <!-- Tab2: 发布管理 -->
    <div v-show="activeTab === 'release'" class="ver-tab-content">
      <div class="ver-release-bar">
        <Button color="primary" @click="openCreateRelease">创建发布包</Button>
      </div>
      <div v-if="releases.length === 0" class="ver-empty">暂无发布包，请先给版本打 TAG 再创建发布包</div>
      <Table v-else :datas="releases" :height="400">
        <TableItem title="发布编码" prop="RELEASECODE" :width="180" />
        <TableItem title="发布名称" prop="RELEASENAME" :width="200" />
        <TableItem title="TAG" prop="TAG" :width="120" />
        <TableItem title="对象数" prop="OBJCOUNT" :width="80" />
        <TableItem title="状态" prop="STATUS" :width="100">
          <template slot-scope="{ data }">
            <span :class="'rel-st-' + data.STATUS">{{ statusLabel(data.STATUS) }}</span>
          </template>
        </TableItem>
        <TableItem title="创建人" prop="CREATER" :width="100" />
        <TableItem title="创建时间" prop="CREATETIME" :width="160" />
        <TableItem title="操作" :width="160" fixed="right">
          <template slot-scope="{ data }">
            <a class="ver-action" @click="previewScript(data)">预览脚本</a>
            <Poptip v-if="data.STATUS === 'draft'" content="确定部署此发布包？将导入到升级中心" transfer @confirm="doDeploy(data)">
              <a class="ver-action ver-danger">部署</a>
            </Poptip>
          </template>
        </TableItem>
      </Table>
    </div>

    <version-diff-modal ref="diffModal" @rollback="doRollback" />
    <version-history-popup ref="verHistory" @rollback="onSearch" />

    <!-- 批量打标弹窗 -->
    <Modal v-model="batchMarkOpen" title="批量打标">
      <Form :label-width="100">
        <FormItem label="对象类型" single>
          <Select v-model="batchMarkForm.objType" :datas="objTypeOptions" placeholder="选择对象类型" filterable />
        </FormItem>
        <FormItem label="对象编码前缀" single>
          <input type="text" v-model="batchMarkForm.objCode" placeholder="如 LIB_M01（模糊匹配前缀）" />
        </FormItem>
        <FormItem label="TAG" single>
          <input type="text" v-model="batchMarkForm.tag" placeholder="如 v1.0" />
        </FormItem>
        <FormItem label="置顶" single>
          <Select v-model="batchMarkForm.pinned" :datas="[{key:'1',title:'是'},{key:'0',title:'否'}]" />
        </FormItem>
      </Form>
      <div slot="footer">
        <Button @click="batchMarkOpen = false">取消</Button>
        <Button color="primary" :loading="batchMarkLoading" @click="doBatchMark">确定</Button>
      </div>
    </Modal>

    <!-- 创建发布包弹窗 -->
    <Modal v-model="createReleaseOpen" title="创建发布包">
      <Form :label-width="100">
        <FormItem label="TAG" single>
          <Select v-model="releaseForm.tag" :datas="tagOptions" placeholder="选择已有 TAG" filterable />
        </FormItem>
        <FormItem label="发布编码" single>
          <input type="text" v-model="releaseForm.releaseCode" placeholder="如 REL_20260719_01" />
        </FormItem>
        <FormItem label="发布名称" single>
          <input type="text" v-model="releaseForm.releaseName" placeholder="如 LIB_M01 v1.0 发布" />
        </FormItem>
        <FormItem label="备注" single>
          <input type="text" v-model="releaseForm.remark" placeholder="可选" />
        </FormItem>
      </Form>
      <div slot="footer">
        <Button @click="createReleaseOpen = false">取消</Button>
        <Button color="primary" :loading="createReleaseLoading" @click="doCreateRelease">创建</Button>
      </div>
    </Modal>

    <!-- 预览脚本弹窗 -->
    <Modal v-model="previewOpen" title="发布脚本预览" :width="800">
      <pre class="ver-script-preview">{{ previewContent }}</pre>
      <div slot="footer">
        <Button @click="previewOpen = false">关闭</Button>
      </div>
    </Modal>
  </div>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import VersionDiffModal from './components/version-diff-modal.vue';
import versionHistoryPopup from '@/components/generic-module/version-history-popup.vue';

export default {
  name: 's01-m22-main',
  components: { VersionDiffModal, versionHistoryPopup },
  data() {
    return {
      datas: [{ title: '系统管理' }, { title: '版本中心' }],
      store: { mapState, mapGetters, mapDateTable, Constants },
      activeTab: 'version',
      // 发布包列表(releases) 在 store state，经 mapState 派生
      // 批量打标
      batchMarkOpen: false,
      batchMarkLoading: false,
      batchMarkForm: { objType: '', objCode: '', tag: '', pinned: '0' },
      // 创建发布包
      createReleaseOpen: false,
      createReleaseLoading: false,
      releaseForm: { tag: '', releaseCode: '', releaseName: '', remark: '' },
      tagOptions: [],
      // 预览脚本
      previewOpen: false,
      previewContent: ''
    };
  },
  computed: {
    // 发布包列表（store state，loadReleases fetch+commit）
    ...mapState(['releases']),
    objTypeOptions() {
      var d = (this.$store.state.app && this.$store.state.app.dicts['版本对象类型']) || {};
      return Object.keys(d).map(k => ({ key: k, title: d[k] }));
    },
    ...mapDateTable('QQRY', ['OBJTYPE', 'OBJCODE', 'CREATER']),
    filterObjType: {
      get() { return this.OBJTYPE },
      set(v) { this.OBJTYPE = v || '' }
    },
    filterObjCode: {
      get() { return this.OBJCODE },
      set(v) { this.OBJCODE = v || '' }
    },
    filterCreateBy: {
      get() { return this.CREATER },
      set(v) { this.CREATER = v || '' }
    }
  },
  watch: {
    activeTab(val) {
      if (val === 'release') this.loadReleases();
    }
  },
  mounted() {
    var q = this.$route.query || {};
    if (q.objType) this.OBJTYPE = q.objType;
    if (q.objCode) this.OBJCODE = q.objCode;
    if (q.objType || q.objCode) {
      this.$nextTick(() => this.onSearch());
      setTimeout(() => {
        var dirty = false;
        if (q.objType && this.OBJTYPE !== q.objType) { this.OBJTYPE = q.objType; dirty = true }
        if (q.objCode && this.OBJCODE !== q.objCode) { this.OBJCODE = q.objCode; dirty = true }
        if (dirty) this.onSearch();
      }, 500);
    }
  },
  methods: {
    onSearch() {
      this.$refs.list.query();
    },
    openDiff(row) {
      this.$refs.diffModal.open(row);
    },
    openHistory(row) {
      this.$refs.verHistory.show({
        objType: row.OBJTYPE,
        objId: row.OBJID,
        objCode: row.OBJCODE
      });
    },
    async doRollback(row) {
      try {
        var ret = await this.$callAction({
          action: Constants.STORE_NAME + '/rollback',
          param: { id: row.ID },
          isBusy: false,
        });
        this.$Message.success((ret && ret.message) || '回滚成功');
        this.onSearch();
      } catch (e) {
        // $callAction 失败时已弹错误提示
      }
    },
    // 发布管理：加载列表（store fetch+commit → state.releases）
    async loadReleases() {
      try {
        await this.$callAction({
          action: Constants.STORE_NAME + '/loadReleases',
          isBusy: false,
        });
      } catch (e) {
        // $callAction 失败时已弹错误提示
      }
    },
    statusLabel(s) {
      var map = { draft: '草稿', published: '已发布', deployed: '已部署' };
      return map[s] || s;
    },
    // 批量打标
    openBatchMark() {
      this.batchMarkForm = { objType: '', objCode: '', tag: '', pinned: '0' };
      this.batchMarkOpen = true;
    },
    async doBatchMark() {
      var f = this.batchMarkForm;
      if (!f.tag && f.pinned === '0') { this.$Message('TAG 或置顶至少填一项'); return }
      this.batchMarkLoading = true;
      try {
        var ret = await this.$callAction({
          action: Constants.STORE_NAME + '/batchMark',
          param: { objType: f.objType, objCode: f.objCode, tag: f.tag, pinned: f.pinned },
          isBusy: false,
        });
        this.$Message.success('已标记 ' + ((ret && ret.Data && ret.Data.affected) || 0) + ' 条版本记录');
        this.batchMarkOpen = false;
        this.onSearch();
      } catch (e) {
        // $callAction 失败时已弹错误提示
      } finally {
        this.batchMarkLoading = false;
      }
    },
    // 创建发布包
    async openCreateRelease() {
      // 简单方案：手动输入 TAG
      this.releaseForm = { tag: '', releaseCode: 'REL_' + new Date().toISOString().slice(0, 10).replace(/-/g, '') + '_01', releaseName: '', remark: '' };
      this.createReleaseOpen = true;
    },
    async doCreateRelease() {
      var f = this.releaseForm;
      if (!f.tag) { this.$Message('TAG 不能为空'); return }
      if (!f.releaseCode) { this.$Message('发布编码不能为空'); return }
      if (!f.releaseName) { this.$Message('发布名称不能为空'); return }
      this.createReleaseLoading = true;
      try {
        var ret = await this.$callAction({
          action: Constants.STORE_NAME + '/createRelease',
          param: { tag: f.tag, releaseCode: f.releaseCode, releaseName: f.releaseName, remark: f.remark },
          isBusy: false,
        });
        this.$Message.success('发布包创建成功，包含 ' + ((ret && ret.Data && ret.Data.objCount) || 0) + ' 个对象');
        this.createReleaseOpen = false;
        this.loadReleases();
      } catch (e) {
        // $callAction 失败时已弹错误提示
      } finally {
        this.createReleaseLoading = false;
      }
    },
    // 预览脚本（一次性展示数据，store action 直接 return 不进 state）
    async previewScript(row) {
      try {
        this.previewContent = await this.$callAction({
          action: Constants.STORE_NAME + '/loadReleaseScript',
          param: { id: row.ID },
          isBusy: false,
        });
        this.previewOpen = true;
      } catch (e) {
        // $callAction 失败时已弹错误提示
      }
    },
    // 部署
    async doDeploy(row) {
      try {
        await this.$callAction({
          action: Constants.STORE_NAME + '/deployRelease',
          param: { releaseId: row.ID },
          isBusy: false,
        });
        this.$Message.success('导入成功，请到升级中心执行部署');
        this.loadReleases();
      } catch (e) {
        // $callAction 失败时已弹错误提示
      }
    }
  }
};
</script>

<style lang="less" scoped>
.ver-center {
  height: 100%;
  display: flex;
  flex-direction: column;
}
.ver-tabs {
  display: flex;
  border-bottom: 1px solid #e8eaec;
  flex-shrink: 0;
}
.ver-tabs span {
  padding: 10px 20px;
  cursor: pointer;
  font-size: 14px;
  border-right: 1px solid #e8eaec;
}
.ver-tabs span.active {
  color: #2F54EB;
  border-bottom: 2px solid #2F54EB;
  font-weight: bold;
}
.ver-tab-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.ver-batch-bar {
  padding: 8px 12px;
  background: #fff;
  border-bottom: 1px solid #e8eaec;
}
.ver-release-bar {
  padding: 8px 12px;
  background: #fff;
  border-bottom: 1px solid #e8eaec;
}
.ver-empty {
  text-align: center;
  color: #999;
  padding: 60px 20px;
  font-size: 13px;
}
.ver-action {
  margin-right: 10px;
  cursor: pointer;
}
.ver-danger {
  color: #ed4014;
}
.ver-script-preview {
  max-height: 500px;
  overflow: auto;
  background: #f5f5f5;
  padding: 12px;
  font-size: 12px;
  white-space: pre-wrap;
  word-break: break-all;
}
.rel-st-draft { color: #fa8c16; }
.rel-st-published { color: #52c41a; }
.rel-st-deployed { color: #1890ff; }
</style>

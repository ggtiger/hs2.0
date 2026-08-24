<template>
  <rs-modal ref="modal" :width="1150">
    <view-dialog :title="title">

    <template slot="header" v-if="current">
      <!-- 头部 toolbar: 版本信息 + 对比模式 + 回滚 -->
          <div class="vhp-head">
            <span class="vhp-flex"></span>
            <!-- 对比模式: 该版本变化(当时) / 与现在对比 -->
            <Button size="s" :color="mode === 'change' ? 'primary' : null" @click="setMode('change')">该版本变化</Button>
            <Button size="s" :color="mode === 'current' ? 'primary' : null" :loading="loadingCurrent" @click="setMode('current')">与现在对比</Button>
            <Poptip
              v-if="current.OPTYPE !== 'rollback'"
              :content="'确定回滚到 v' + current.VERSION + '？回滚会生成一个新版本'"
              transfer
              @confirm="doRollback"
            >
              <Button size="s" color="error" v-per="'RS_M22/A05'">回滚到此版本</Button>
            </Poptip>
          </div>
    </template>
    <div class="vhp" slot="body">
      <div class="vhp-body">
        <!-- 左: 版本列表 -->
        <div class="vhp-list">
          <div v-if="loading" class="vhp-empty">加载中...</div>
          <div v-else-if="versions.length === 0" class="vhp-empty">暂无版本记录</div>
          <div
            v-for="v in versions"
            :key="v.ID"
            :class="['vhp-item', { active: current && current.ID === v.ID }]"
            @click="selectVersion(v)"
          >
            <div class="vhp-item-line1">
              <span class="vhp-ver">v{{ v.VERSION }}</span>
              <span class="vhp-op" :class="'op-' + v.OPTYPE">{{ opLabel(v.OPTYPE) }}</span>
              <span v-if="v.PINNED === 1 || v.PINNED === '1'" class="vhp-tag pin">置顶</span>
              <span v-if="v.TAG" class="vhp-tag">{{ v.TAG }}</span>
            </div>
            <div class="vhp-item-line2">{{ v.CREATER || '-' }} · {{ v.CREATETIME || '-' }}</div>
            <div class="vhp-item-note" v-if="v.CHANGENOTE">{{ v.CHANGENOTE }}</div>
          </div>
        </div>

        <!-- 右: 对比视图 + 操作 -->
        <div class="vhp-main" v-if="current">

          <!-- 对比区: 占满剩余空间, 内容内部滚动 -->
          <version-diff-view
            v-if="!loadingDetail"
            class="vhp-diff"
            fill
            :beforeContent="diffBefore"
            :afterContent="diffAfter"
          />
          <div v-else class="vhp-empty">加载版本内容...</div>
          <!-- 底部: 对比提示 + 标记条（TAG 发布标记 + PINNED 置顶, 有的版本永不过期清理） -->
          <div class="vhp-foot">
            <div class="vhp-diff-tip" v-if="mode === 'current'">
              {{ currentExists ? '左=该版本快照(v' + current.VERSION + ' 保存后)，右=当前实时状态' : '对象当前已不存在（可能被删除）' }}
            </div>
            <div class="vhp-mark">
              <Input v-model="markTag" size="small" placeholder="标签(如 v1.0 发布点)" style="width:180px" />
              <label class="vhp-pin-label">
                <input type="checkbox" v-model="markPinned" /> 置顶保留
              </label>
              <Button size="s" :loading="marking" @click="doMark">保存标记</Button>
              <span class="vhp-note" v-if="current.CHANGENOTE">说明：{{ current.CHANGENOTE }}</span>
            </div>
          </div>
        </div>
        <div class="vhp-main vhp-empty" v-else>从左侧选择版本查看对比</div>
      </div>
    </div>
    </view-dialog>
  </rs-modal>
</template>
<script>
import versionDiffView from './version-diff-view.vue';
import { Constants as VHP, mapState as vhpMapState, mapDateTable as vhpMapDateTable } from './version-history-store';

// 通用版本历史弹窗：任意对象( objType+objId )的变更查询/当时对比/与现在对比/回滚/标记
// 用法: this.$refs.verHistory.show({ objType: 'code', objId: id, objCode: code })
export default {
  name: 'version-history-popup',
  components: { versionDiffView },
  data() {
    return {
      loading: false,
      loadingDetail: false,
      loadingCurrent: false,
      // UI 交互态：对比模式 radio，属纯组件 data
      mode: 'change',
      marking: false,
    };
  },
  computed: {
    // 标记表单(TAG/PINNED)绑 MAIN DataTable 当前版本行（loadDetail open 后自动同步）
    ...vhpMapDateTable('MAIN', ['TAG', 'PINNED']),
    // 语义化双向别名（PINNED 1/'1' ↔ checkbox boolean）
    markTag: {
      get() { return this.TAG || '' },
      set(v) { this.TAG = v },
    },
    markPinned: {
      get() { return this.PINNED === 1 || this.PINNED === '1' },
      set(v) { this.PINNED = v ? '1' : '0' },
    },
    ...vhpMapState([
      'objType', 'objId', 'objCode',
      'versions', 'currentDetail', 'currentContent', 'currentExists',
    ]),
    // 语义化别名（与原 .vue 内 current 字段对齐，模板不动）
    current() { return this.currentDetail },
    title() {
      return `版本历史 · ${this.objCode || this.objId}`;
    },
    // change 模式: BEFORE→AFTER(当时变化); current 模式: 该版本AFTER→当前实时
    diffBefore() {
      if (!this.current) return null;
      return this.mode === 'change' ? this.current.BEFORECONTENT : this.current.AFTERCONTENT;
    },
    diffAfter() {
      if (!this.current) return null;
      return this.mode === 'change' ? this.current.AFTERCONTENT : this.currentContent;
    },
  },
  methods: {
    opLabel(op) {
      var d = (this.$store.state.app && this.$store.state.app.dicts['版本操作类型']) || {};
      return d[op] || op;
    },
    async show(opts) {
      await this.$callAction({
        action: VHP.STORE_NAME + '/setContext',
        param: opts || {},
        isBusy: false,
      });
      this.mode = 'change';
      this.$refs.modal.show();
      await this.loadVersions();
    },
    close() {
      this.$refs.modal.hide();
    },
    async loadVersions() {
      this.loading = true;
      try {
        await this.$callAction({
          action: VHP.STORE_NAME + '/loadVersions',
          param: { objType: this.objType, objId: this.objId },
          isBusy: false,
        });
        if (this.versions.length > 0) {
          await this.selectVersion(this.versions[0]);
        }
      } catch (e) {
        this.$Message.error('加载版本历史失败: ' + (e.message || e));
      } finally {
        this.loading = false;
      }
    },
    async selectVersion(v) {
      this.mode = 'change';
      // 切换版本：清当前态缓存（下次 setMode('current') 重新加载）
      await this.$callAction({
        action: VHP.STORE_NAME + '/loadCurrentState',
        param: { id: '' },
        isBusy: false,
      }).catch(function() {});
      // A02 open 加载完整行进 MAIN DataTable（含 BEFORE/AFTER 大字段 + TAG/PINNED 标记表单自动同步）；
      // 接口异常时降级用列表行
      this.loadingDetail = true;
      try {
        await this.$callAction({
          action: VHP.STORE_NAME + '/loadDetail',
          param: { id: v.ID, fallback: v },
          isBusy: false,
        });
      } catch (e) {
        await this.$callAction({
          action: VHP.STORE_NAME + '/loadDetail',
          param: { id: '', fallback: v },
          isBusy: false,
        });
      } finally {
        this.loadingDetail = false;
      }
    },
    async setMode(m) {
      this.mode = m;
      // 仅在第一次切到 current 模式时拉取（currentContent === '' 表示未加载过）
      if (m === 'current' && !this.currentContent && !this.currentExists) {
        this.loadingCurrent = true;
        try {
          await this.$callAction({
            action: VHP.STORE_NAME + '/loadCurrentState',
            param: { id: this.current.ID },
            isBusy: false,
          });
        } catch (e) {
          this.$Message.error('读取当前状态失败: ' + (e.message || e));
          this.mode = 'change';
        } finally {
          this.loadingCurrent = false;
        }
      }
    },
    async doRollback() {
      try {
        var ret = await this.$callAction({
          action: VHP.STORE_NAME + '/rollback',
          param: { id: this.current.ID },
          isBusy: false,
        });
        this.$Message.success((ret && ret.message) || '回滚成功');
        // 回滚后当前态失效，清缓存（loadVersions → selectVersion 会再次重置）
        await this.$callAction({
          action: VHP.STORE_NAME + '/loadCurrentState',
          param: { id: '' },
          isBusy: false,
        }).catch(function() {});
        await this.loadVersions();
        this.$emit('rollback', this.current);
      } catch (e) {
        this.$Message.error('回滚失败: ' + (e.message || e));
      }
    },
    async doMark() {
      this.marking = true;
      try {
        await this.$callAction({
          action: VHP.STORE_NAME + '/markVersion',
          param: { id: this.current.ID, tag: this.TAG || '', pinned: this.markPinned },
          isBusy: false,
        });
        this.$Message.success('已标记');
        // store mutation APPLY_MARK 已同步 currentDetail.TAG/PINNED 和 versions 列表
      } catch (e) {
        this.$Message.error('标记失败: ' + (e.message || e));
      } finally {
        this.marking = false;
      }
    },
  },
};
</script>
<style lang="less" scoped>
.vhp {
  display: flex;
  flex-direction: column;
  height: 640px;
  padding: 0;
}
.vhp-title {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 15px;
  font-weight: 600;
  color: #17233d;
  padding-bottom: 10px;
  border-bottom: 1px solid #e8eaec;
  flex-shrink: 0;
  .vhp-close {
    color: #9ea7b4;
    font-size: 16px;
    cursor: pointer;
  }
}
.vhp-body {
  flex: 1;
  display: flex;
  min-height: 0;
  gap: 6px;
}
.vhp-list {
  width: 280px;
  flex-shrink: 0;
  overflow: auto;
  border: 1px solid #e8eaec;
  border-radius: 4px;
}
.vhp-item {
  padding: 8px 10px;
  border-bottom: 1px solid #f0f0f0;
  cursor: pointer;
  &:hover {
    background: #f8f8f9;
  }
  &.active {
    background: #e6f7ff;
  }
}
.vhp-item-line1 {
  display: flex;
  align-items: center;
  gap: 6px;
}
.vhp-ver {
  font-weight: 700;
  color: #17233d;
}
.vhp-op {
  padding: 0 6px;
  border-radius: 3px;
  font-size: 12px;
  &.op-insert { background: #f6ffed; color: #52c41a; }
  &.op-update { background: #fff7e6; color: #fa8c16; }
  &.op-delete { background: #fff1f0; color: #f5222d; }
  &.op-rollback { background: #f9f0ff; color: #722ed1; }
  &.lg {
    padding: 1px 8px;
  }
}
.vhp-tag {
  background: #fff7e6;
  color: #fa8c16;
  font-size: 11px;
  padding: 0 5px;
  border-radius: 3px;
  &.pin {
    background: #f9f0ff;
    color: #722ed1;
  }
}
.vhp-item-line2 {
  color: #9ea7b4;
  font-size: 12px;
  margin-top: 3px;
}
.vhp-item-note {
  color: #515a6e;
  font-size: 12px;
  margin-top: 2px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.vhp-main {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.vhp-diff {
  flex: 1;
  min-height: 0;
}
.vhp-foot {
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  gap: 6px;
  border-top: 1px solid #f0f0f0;
  padding-top: 8px;
}
.vhp-head {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}
.vhp-meta {
  color: #9ea7b4;
  font-size: 12px;
}
.vhp-flex {
  flex: 1;
}
.vhp-mark {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}
.vhp-pin-label {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: #515a6e;
  cursor: pointer;
}
.vhp-note {
  color: #9ea7b4;
  font-size: 12px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.vhp-diff-tip {
  color: #9ea7b4;
  font-size: 12px;
  flex-shrink: 0;
}
.vhp-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  color: #9ea7b4;
  font-size: 13px;
  padding: 40px 0;
  flex: 1;
}
</style>

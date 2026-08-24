<template>
  <rs-modal ref="modal" :width="1100">
    <view-dialog :title="title">
      <template slot="header">
      <Button size="s" :color="mode === 'change' ? 'primary' : null" @click="setMode('change')">该版本变化</Button>
        <Button size="s" :color="mode === 'current' ? 'primary' : null" :loading="loadingCurrent" @click="setMode('current')">与现在对比</Button>
      </template>
       <template slot="body">
    <div class="vdiff" v-if="ver">
      <!-- 版本信息头 -->
      <div class="vdiff-head">
        <span class="vdiff-tag type">{{ typeLabel }}</span>
        <span class="vdiff-code">{{ ver.OBJCODE || ver.OBJID }}</span>
        <span class="vdiff-tag" :class="'op-' + ver.OPTYPE">{{ opLabel }}</span>
        <span class="vdiff-meta">v{{ ver.VERSION }} · {{ ver.CREATER || '-' }} · {{ ver.CREATETIME || '-' }}</span>
        <span class="vdiff-note" v-if="ver.CHANGENOTE">备注：{{ ver.CHANGENOTE }}</span>
        <span class="vdiff-flex"></span>

      </div>
      <div class="vdiff-tip" v-if="mode === 'current'">
        {{ currentExists ? '左=该版本快照(v' + ver.VERSION + ' 保存后)，右=当前实时状态' : '对象当前已不存在（可能被删除）' }}
      </div>

      <!-- 对比内容（共用 version-diff-view） -->
      <version-diff-view
        :beforeContent="diffBefore"
        :afterContent="diffAfter"
        :height="450"
      />

    </div>
    </template>
     <!-- 底部按钮 -->
    <template slot="footer">
        <Button v-if="ver && ver.OPTYPE !== 'rollback'" v-per="'RS_M22/A05'" color="error" @click="onRollback">回滚到此版本</Button>
        <Button @click="close">关闭</Button>
      </template>
    </view-dialog>
  </rs-modal>
</template>

<script>
import versionDiffView from '@/components/generic-module/version-diff-view.vue';
import { Constants as VHP, mapState as vhpMapState } from '@/components/generic-module/version-history-store';

export default {
  name: 'VersionDiffModal',
  components: { versionDiffView },
  data() {
    return {
      // 版本详情(ver)/当前态(currentContent/currentExists) 在 vhp store state，经 mapState 派生
      mode: 'change',
      loadingCurrent: false,
    };
  },
  computed: {
    ...vhpMapState(['currentDetail', 'currentContent', 'currentExists']),
    // 语义化别名（模板沿用 ver 字段名）
    ver() { return this.currentDetail },
    title() {
      return this.ver ? '版本对比 · ' + (this.ver.OBJCODE || '') + ' v' + this.ver.VERSION : '版本对比';
    },
    // 类型/操作标签走数据字典（D0701 版本对象类型 / D0702 版本操作类型），与筛选下拉同源
    typeLabel() {
      if (!this.ver) return '';
      var d = (this.$store.state.app && this.$store.state.app.dicts['版本对象类型']) || {};
      return d[this.ver.OBJTYPE] || this.ver.OBJTYPE;
    },
    opLabel() {
      if (!this.ver) return '';
      var d = (this.$store.state.app && this.$store.state.app.dicts['版本操作类型']) || {};
      return d[this.ver.OPTYPE] || this.ver.OPTYPE;
    },
    // change 模式: BEFORE→AFTER(当时变化); current 模式: 该版本AFTER→当前实时
    diffBefore() {
      if (!this.ver) return null;
      return this.mode === 'change' ? this.ver.BEFORECONTENT : this.ver.AFTERCONTENT;
    },
    diffAfter() {
      if (!this.ver) return null;
      return this.mode === 'change' ? this.ver.AFTERCONTENT : this.currentContent;
    }
  },
  methods: {
    async open(row) {
      this.mode = 'change';
      this.$refs.modal.show();
      try {
        // 重置当前态缓存（清上次弹窗残留），再 A02 open 详情 → vhp MAIN DataTable + currentDetail
        await this.$callAction({
          action: VHP.STORE_NAME + '/loadCurrentState',
          param: { id: '' },
          isBusy: false,
        }).catch(function() {});
        await this.$callAction({
          action: VHP.STORE_NAME + '/loadDetail',
          param: { id: row.ID, fallback: null },
          isBusy: false,
        });
        if (!this.ver) {
          this.$Message.error('版本详情读取失败');
          this.close();
        }
      } catch (e) {
        // $callAction 失败时已弹错误提示
        this.close();
      }
    },
    async setMode(m) {
      this.mode = m;
      // 仅在第一次切到 current 模式时拉取（currentContent/currentExists 复位后表示未加载过）
      if (m === 'current' && !this.currentContent && !this.currentExists) {
        this.loadingCurrent = true;
        try {
          await this.$callAction({
            action: VHP.STORE_NAME + '/loadCurrentState',
            param: { id: this.ver.ID },
            isBusy: false,
          });
        } catch (e) {
          // $callAction 失败时已弹错误提示
          this.mode = 'change';
        } finally {
          this.loadingCurrent = false;
        }
      }
    },
    onRollback() {
      this.$emit('rollback', this.ver);
      this.close();
    },
    close() {
      this.$refs.modal.hide();
    }
  }
};
</script>

<style lang="less" scoped>
.vdiff {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 4px 2px;
}
.vdiff-title {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 15px;
  font-weight: 600;
  color: #17233d;
  padding-bottom: 10px;
  border-bottom: 1px solid #e8eaec;
  .vdiff-close {
    color: #9ea7b4;
    font-size: 16px;
    cursor: pointer;
  }
}
.vdiff-head {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 0;
  padding-top: 0;
  flex-wrap: wrap;
  .vdiff-code {
    font-weight: 600;
    color: #17233d;
  }
  .vdiff-meta {
    color: #9ea7b4;
    font-size: 12px;
  }
  .vdiff-note {
    color: #515a6e;
    font-size: 12px;
  }
  .vdiff-flex {
    flex: 1;
  }
}
.vdiff-tip {
  color: #9ea7b4;
  font-size: 12px;
  padding-bottom: 6px;
}
.vdiff-tag {
  padding: 1px 8px;
  border-radius: 3px;
  font-size: 12px;
  background: #e8eaec;
  color: #515a6e;
  &.type {
    background: #e6f7ff;
    color: #1890ff;
  }
  &.op-insert { background: #f6ffed; color: #52c41a; }
  &.op-update { background: #fff7e6; color: #fa8c16; }
  &.op-delete { background: #fff1f0; color: #f5222d; }
  &.op-rollback { background: #f9f0ff; color: #722ed1; }
}
.vdiff-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding-top: 12px;
}
</style>

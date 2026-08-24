<template>
  <!-- 保存双按钮:
       保存 — 快速保存, 不留版本, 无需说明 (SKIPVERSION=1)
       提交 — 锚定浮层填变更说明, 保存并生成版本记录 (CHANGENOTE)
       用法: <save-actions :loading="saving" @save="onQuickSave" @commit="onCommitSave" /> -->
  <span class="sav">
    <Button size="s" :loading="loading" @click="$emit('save')">保存</Button>
    <DropdownCustom ref="dd" trigger="click" placement="bottom-end" :toggleIcon="false">
      <Button size="s" color="primary" :loading="loading">提交</Button>
      <div slot="content" class="sav-tip">
        <textarea
          v-model="note"
          rows="3"
          class="sav-input"
          placeholder="变更说明：本次修改了什么（提交会保存版本记录，可对比/回滚）"
        ></textarea>
        <div class="sav-foot">
          <a class="sav-btn" @click="commit">提交</a>
          <a class="sav-btn sav-cancel" @click="cancel">取消</a>
        </div>
      </div>
    </DropdownCustom>
  </span>
</template>
<script>
export default {
  name: 'save-actions',
  props: {
    loading: { type: Boolean, default: false },
  },
  data() {
    return {
      note: '',
    };
  },
  methods: {
    commit() {
      this.$refs.dd.hide();
      this.$emit('commit', this.note.trim());
      this.note = '';
    },
    cancel() {
      this.$refs.dd.hide();
      this.note = '';
    },
  },
};
</script>
<style lang="less" scoped>
.sav {
  display: inline-flex;
  gap: 6px;
}
.sav-tip {
  width: 260px;
  padding: 8px;
}
.sav-input {
  width: 100%;
  border: 1px solid #dcdee2;
  border-radius: 4px;
  padding: 6px 8px;
  font-size: 12px;
  resize: none;
  outline: none;
  box-sizing: border-box;
  &:focus {
    border-color: #2d8cf0;
  }
}
.sav-foot {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding-top: 6px;
}
.sav-btn {
  color: #2d8cf0;
  font-size: 12px;
  cursor: pointer;
  &.sav-cancel {
    color: #9ea7b4;
  }
}
</style>

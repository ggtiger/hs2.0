<template>
  <view-dialog :title="title">
    <template slot="body">
      <ToolBar label="提示词配置" :size="16">
        <div slot="right">
          <Button color="primary" icon="h-icon-star" size="s" @click="optimize" :loading="optimizing">✨ AI优化</Button>
        </div>
      </ToolBar>
      <rs-form-edit
        ref="form"
        class="maxModalH rs-flex-col"
        :label-width="100"
        mode="single"
        :path="$MAIN"
      ></rs-form-edit>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'RS_M16/A07'" v-if="ID" @confirm="del"><Button class="ml5" color="red">删除</Button></Poptip>
      <Button class="ml5" v-per="'RS_M16/A04'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Add01 from '@/mixins/add01';
export default {
  name: 's01-m11-add',
  mixins: [Add01],
  computed: {
    ...mapDateTable('MAIN', []),
  },
  data() {
    return { optimizing: false };
  },
  methods: {
    // AI优化提示词：调 SignalR OptimizePrompt，结果填回 CONTENT
    async optimize() {
      const model = this.$MAIN && this.$MAIN.data && this.$MAIN.data[0];
      if (!model) {
        this.$Message.warn('请先填写或选择一条提示词');
        return;
      }
      const content = model.CONTENT || '';
      if (!content || !content.trim()) {
        this.$Message.warn('提示词内容为空，无法优化');
        return;
      }
      this.optimizing = true;
      try {
        const aiClient = this.$aiAgent.createClient({ scene: 'optimize' });
        const result = await aiClient.optimizePrompt(content);
        if (result && typeof result === 'string' && result.indexOf('⚠️') !== 0) {
          // 优化成功，填回 CONTENT
          this.$MAIN.setValue('CONTENT', result);
          if (this.$refs.form && this.$refs.form.path) {
            this.$set(this.$refs.form.path.data[0], 'CONTENT', result);
          }
          this.$Message.success('AI优化完成，请确认后保存');
        } else {
          this.$Message.error(result || '优化失败');
        }
      } catch (e) {
        this.$Message.error('优化失败：' + (e.message || e));
      } finally {
        this.optimizing = false;
      }
    },
  },
};
</script>

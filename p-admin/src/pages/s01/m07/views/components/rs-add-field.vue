<template>
  <!-- 右键弹出框添加控件 -->
  <view-dialog :title="title">
    <template slot="body">
      <Form ref="form" :labelWidth="75">
        <FormItem label="控件类型">
          <Select v-model="type" :datas="selectType">
            <template
              slot-scope="{item}"
              slot="item"
              v-if="!(itemType!='itemLayout'&&item.key==='itemLayout')"
            >{{item.title}}</template>
          </Select>
        </FormItem>
        <FormItem label="插入位置">
          <Select v-if="itemType==='itemLayout'" v-model="position" :datas="selectPosition2"></Select>
          <Select v-else v-model="position" :datas="selectPosition1"></Select>
        </FormItem>
      </Form>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="close">取消</Button>
      <Button class="ml5" color="primary" @click.native="success">确定</Button>
    </template>
  </view-dialog>
</template>

<script>
export default {
  name: 'rs-add-field',
  components: {},
  directives: {},
  filters: {},
  mixins: [],
  props: {
    title: String,
    itemType: {
      type: String,
      default: 'itemLayout',
    },
  },
  data() {
    return {
      selectType: [
        { title: '布局', key: 'itemLayout' },
        { title: '标签', key: 'itemLabel' },
        { title: '输入框', key: 'itemField' },
        { title: '勾选框', key: 'itemCheckBox' },
        { title: '表格', key: 'itemTable' },
        { title: '富文本框', key: 'itemEditor' },
      ],
      selectPosition1: [{ title: '前', key: 'top' }, { title: '后', key: 'bottom' }],
      selectPosition2: [{ title: '前', key: 'top' }, { title: '内', key: 'inset' }, { title: '后', key: 'bottom' }],
      type: 'itemLayout',
      position: 'top',
    };
  },
  computed: {},
  watch: {},
  created() {},
  mounted() {
    this.$nextTick(function() {});
  },
  methods: {
    success() {
      var values = { type: this.type, position: this.position };
      this.$emit('success', values);
      this.close();
    },
    close() {
      this.$emit('close');
    },
  },
};
</script>

<style lang="less" scoped>
@import '~@/theme/index.less';
</style>

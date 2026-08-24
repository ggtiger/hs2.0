<template>
  <view-dialog title="常用菜单设置">
    <template slot="body">
      <Transfer v-model="source" :datas="sourceDatas" :option="option" keyName="text"></Transfer>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="close">取消</Button>
      <Button class="ml5" color="primary" @click.native="ok">确定</Button>
    </template>
  </view-dialog>
</template>


<script>
export default {
  name: 'setMenu',
  props: {
    menus: Array,
  },
  data() {
    return {
      source: this.menus,
      sourceDatas: [
        { icon: 'h-icon-home', text: '受理送检1', route: { name: 'lsthSearch' } },
        { icon: 'h-icon-link', text: '委托单位1', route: { name: 'lsthAdd' } },
        { icon: 'h-icon-task', text: '原始记录1', route: { name: 'cgddSearch' } },
        { icon: 'h-icon-star', text: '费用管理1', route: { name: 'cgddAdd' } },
      ],
      option: {
        ltHeadText: '未选',
        rtHeadText: '已选',
        rtIcon: 'h-icon-plus',
        ltIcon: 'h-icon-minus',
      },
    };
  },
  computed: {},
  watch: {},
  methods: {
    close() {
      this.$emit('close');
    },
    ok() {
      this.$emit('ok', this.source);
      this.close();
    },
  },
};
</script>

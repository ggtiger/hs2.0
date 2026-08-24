
<template>
  <ul class="treeItem">
    <li v-for="(item, index) in listArr" :key="index">
      <div>
        <Checkbox
          v-model="item.ISCHECK"
          :style="{marginLeft:(item.level*20)+'px'}"
          @change="change(item)"
        >{{item.FUNCNAME}}</Checkbox>
      </div>
      <func-item :listArr="item.children"></func-item>
      <ul class="funcpiont" :style="{marginLeft:((item.level+2)*20)+'px'}">
        <Checkbox
          v-for="tt in item.point"
          @change="changeUpItem(tt)"
          v-model="tt.ISCHECK"
          :key="tt.ID"
        >{{tt.FUNCPOINTNAME}}</Checkbox>
      </ul>
    </li>
  </ul>
</template>

<script>
import bus from '../eventbus';
export default {
  name: 'func-item',
  props: {
    listArr: {
      default: function() {
        return [];
      },
    },
    level: 1,
  },
  data() {
    return {};
  },
  computed: {},
  created() {},
  methods: {
    change(item) {
      bus.$emit('change', item, !item.ISCHECK);
    },
    changeUpItem(item) {
      if (!item.ISCHECK) bus.$emit('change-up-item', item, !item.ISCHECK);
    },
  },
};
</script>

<style scoped lang="less">
.treeItem {
  font-size: 16px;
}
.treeItem li {
  margin: 5px;
}
.funcpiont {
  margin: 5px;
}
</style>

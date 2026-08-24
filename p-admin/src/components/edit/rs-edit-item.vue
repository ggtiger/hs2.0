<template>
  <div>
    <template v-for="(item,i) in layouts">
      <component
        :is="item.type"
        v-bind="item"
        :key="i"
        v-model="item.value"
        :class="select.id===item.id?'rr-active':''"
        @click.stop.native="selectItem(item)"
        :inLayout="inLayout"
        @clickAtion="clickAtion"
      >
        <div v-for="(it,idx) in item.cell" :slot="`cell${idx}`" :key="idx">
          <rs-edit-item
            v-if="item.cell==1&&item.children&&item.children.length>0"
            :layouts="item.children||[]"
            :parent="item"
            :select="select"
            @selectItem="selectItem"
            @clickAtion="clickAtion"
            :inLayout="inLayout"
          ></rs-edit-item>
          <rs-edit-item
            v-else-if="item.cell>1&&item.children&&item.children.length>0&&item.children[idx]"
            :layouts="[item.children[idx]]"
            :parent="item"
            :select="select"
            @selectItem="selectItem"
            @clickAtion="clickAtion"
            :inLayout="inLayout"
            :key="idx"
          ></rs-edit-item>
        </div>
      </component>
    </template>
  </div>
</template>

<script>
import itemLabel from './label/index.vue';
import itemLayout from './layout/index.vue';
import itemField from './field/index.vue';
import itemCheckBox from './checkbox/index.vue';
import itemTable from './table/index.vue';
import itemEditor from './ueditor/index2.vue';
export default {
  name: 'rs-edit-item',
  components: {
    itemLabel,
    itemLayout,
    itemField,
    itemCheckBox,
    itemTable,
    itemEditor,
  },
  directives: {},
  filters: {},
  mixins: [],
  props: {
    layouts: [],
    parent: {},
    select: {},
    inLayout: true,
  },
  data() {
    return {};
  },
  computed: {},
  watch: {},
  created() {},
  mounted() {
    this.$nextTick(function() {});
  },
  methods: {
    selectItem(item) {
      this.$emit('selectItem', item);
    },
    clickAtion(action) {
      this.$emit('clickAtion', action);
    },
  },
};
</script>

<style lang="less" scoped>
@import '~@/theme/index.less';
.rr-active {
  border: 1px solid @primary-color;
}
/deep/.rr-active.h-row {
  border: none;
}
/deep/ .rr-active > .h-col {
  border: 1px solid @primary-color;
}
.rr-active-layout {
  border: 1px solid @primary-color;
}
/deep/ .h-dropdowncustom-show-content {
  width: 100%;
}
</style>

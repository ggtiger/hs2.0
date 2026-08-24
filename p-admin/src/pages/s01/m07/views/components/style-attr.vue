<template>
  <!-- 属性 -->
  <div>
    <div class="list-item">
      <span>字号</span>
      <div class="list-right">
        <Select v-model="attr.size" class="rr-text-center" :datas="fontSize"></Select>
      </div>
    </div>
    <div class="list-item">
      <span>宽度</span>
      <div class="list-right">
        <input type="text" class="rr-text-center" v-model="attr.width" style="width:100%" />
      </div>
    </div>
    <div class="list-item">
      <span>高度</span>
      <div class="list-right">
        <input type="text" class="rr-text-center" v-model="attr.height" style="width:100%" />
      </div>
    </div>
    <!-- <div class="list-item">
      <span>行高</span>
      <div class="list-right">
        <input type="text" class="rr-text-center" v-model="lineheight" style="width:100%" />
      </div>
    </div>-->
    <div class="list-item">
      <span>样式</span>
      <div class="list-right">
        <SwitchList v-model="attr.align" :datas="param1"></SwitchList>
        <div class="rr-switch-only">
          <Button :color="attr.weight?'primary':''" size="s" icon="rr-font rr-font-bold"></Button>
          <h-switch v-model="attr.weight"></h-switch>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: 'setAttr',
  directives: {},
  filters: {},
  mixins: [],
  props: {
    value: {},
  },
  data() {
    return {
      fontSize: [12, 13, 14, 16, 18, 20, 21, 24, 29, 32, 48, 64, 96],
      attr: this.value,
      param1: [
        {
          key: 'left',
          icon: 'rr-font rr-font-align-left',
        },
        {
          key: 'center',
          icon: 'rr-font rr-font-align-center',
        },
        {
          key: 'right',
          icon: 'rr-font rr-font-align-right',
        },
      ],
    };
  },
  computed: {},
  watch: {
    value: {
      handler(val) {
        this.attr = val;
      },
      immediate: true,
      deep: true,
    },
    attr: {
      handler(val) {
        this.$emit('input', val);
      },
      immediate: true,
      deep: true,
    },
  },
  created() {},
  mounted() {
    this.$nextTick(function() {});
  },
  methods: {},
};
</script>

<style lang="less" scoped>
@import '~@/theme/index.less';
.title {
  font-size: 14px;
  color: @primary-color;
}
.padding-t10 {
  padding-top: 10px;
}
.padding-b10 {
  padding-bottom: 10px;
}
.list-item {
  margin-bottom: 5px;
  line-height: 33px;
  overflow: auto;
  & > span {
    float: left;
  }
  .list-right {
    margin-left: 4em;
  }
}
.edit {
  background: #f2f2f2;
  .edit-body {
    background: #fff;
  }
}
/deep/ .h-switchlist {
  & > span {
    padding: 5px 10px;
    border-radius: 4px;
    margin-right: 5px;
    border: 1px solid #d3d3d3;
    box-shadow: 0 1px 1px 0 #eeeeee;
    &:last-child,
    & > i {
      margin-right: 0;
    }
  }
}
.rr-switch-only {
  position: relative;
  display: inline-block;
  vertical-align: middle;
  line-height: 1;
  /deep/ .h-switch {
    width: 35px;
    overflow: hidden;
    height: 28px;
    position: absolute;
    left: 0;
    top: 0;
    opacity: 0;
  }
}
</style>

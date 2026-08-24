<template>
  <div style="height:100%;overflow-y:auto;overflow-x:hidden;">
    <router-view v-if="routerName==='wodezhuye'"></router-view>
    <div v-else class="h-panel h-panel-no-border rr-flex-col">
      <div class="h-panel-bar">
        <span class="h-panel-title">
          <Breadcrumb :datas="routeDatas"></Breadcrumb>
        </span>
        <div class="h-panel-right" v-if="routerMeta.isAdd">
          <Button v-if="pageStatus===1" color="primary" @click="editPage">编辑</Button>
          <Button v-if="pageStatus===1||pageStatus===2" color="primary" @click="addPage">新增</Button>
          <Button v-if="pageStatus===2||pageStatus===3" color="primary" @click="baocunPage">保存</Button>
          <Button v-if="pageStatus===1" color="red" @click="delPage">删除</Button>
          <Button color="primary" @click="searchPage">查询</Button>
        </div>
      </div>
      <div class="h-panel-body rr-flex-1">
        <router-view :pageStatus="pageStatus"></router-view>
      </div>
    </div>
  </div>
</template>
<script>
export default {
  name: 'home-body',
  props: {
    routerName: {
      type: String,
      default: 'wodezhuye',
    },
    routerMeta: {
      type: Object,
    },
    routeDatas: {
      type: Array,
    },
  },
  data() {
    return {
      pageStatus: 1, // 页面状态1：查看，2：编辑，3：新增
    };
  },
  computed: {},
  mounted() {},
  methods: {
    searchPage() {
      this.$router.push({ name: 'lsthSearch' });
    },
    editPage() {
      this.pageStatus = 2;
    },
    baocunPage() {
      this.pageStatus = 1;
    },
    addPage() {
      this.pageStatus = 3;
    },
    delPage() {
      this.$router.go(-1);
    },
  },
};
</script>

<style lang="less" scoped>
@import '~heyui/themes/index.less';
.app-header {
  .h-autocomplete {
    line-height: 1.5;
    float: left;
    margin-top: 15px;
    margin-right: 20px;
    width: 120px;
    &-show,
    &-show:hover,
    &-show.focusing {
      outline: none;
      box-shadow: none;
      border-color: transparent;
      border-radius: 0;
    }
    &-show.focusing {
      border-bottom: 1px solid #eee;
    }
  }
  &-info &-icon-item {
    cursor: pointer;
    float: left;
    padding: 0 15px;
    height: @layout-header-height;
    line-height: @layout-header-height;
    margin-right: 10px;
    &:hover {
      background: @hover-background-color;
    }
    i {
      font-size: 18px;
    }
    a {
      color: inherit;
    }
    .h-badge {
      margin: 20px 0;
      display: block;
    }
  }
  .h-dropdownmenu {
    float: left;
  }
  &-dropdown {
    float: right;
    margin-left: 10px;
    padding: 0 20px 0 15px;
    .h-icon-down {
      right: 20px;
    }
    cursor: pointer;
    &:hover,
    &.h-pop-trigger {
      background: @hover-background-color;
    }
    &-dropdown {
      padding: 5px 0;
      .h-dropdownmenu-item {
        padding: 8px 20px;
      }
    }
  }
  &-menus {
    display: inline-block;
    vertical-align: top;
    > div {
      display: inline-block;
      font-size: 15px;
      padding: 0 25px;
      color: @dark-color;
      &:hover {
        color: @primary-color;
      }
      + div {
        margin-left: 5px;
      }
      &.h-tab-selected {
        color: @white-color;
        background-color: @primary-color;
      }
    }
  }
}
</style>

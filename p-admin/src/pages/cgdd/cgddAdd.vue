<template>
  <div class="h-panel h-panel-no-border rr-flex-col">
    <div class="h-panel-bar">
      <span class="h-panel-title">
        <Breadcrumb :datas="routeDatas"></Breadcrumb>
      </span>
      <div class="h-panel-right">
        <Button v-if="pageStatus===1" color="primary" @click="editPage">编辑</Button>
        <Button v-if="pageStatus===1||pageStatus===2" color="primary" @click="addPage">新增</Button>
        <Button v-if="pageStatus===2||pageStatus===3" color="primary" @click="baocunPage">保存</Button>
        <Button v-if="pageStatus===2" color="red" @click="delPage">删除</Button>
        <Button color="primary" @click="searchPage">查询</Button>
      </div>
    </div>
    <div class="h-panel-body rr-flex-1">
      <div class="rr-flex-col">
        <add-page-title title="采购订单" :data="danju"></add-page-title>
        <Tabs :datas="tabs" v-model="selected" @change="change" style="margin-bottom:20px"></Tabs>
        <component
          :is="currentComponent"
          class="rr-flex-1 rr-scroll-bar"
          style="padding:0 5px;"
          :pageStatus="pageStatus"
        ></component>
      </div>
    </div>
  </div>
</template>

<script>
import feiyong from './components/feiyong';
import neirong from './components/neirong';

export default {
  components: { feiyong, neirong },
  data() {
    return {
      routeDatas: [
        {
          icon: 'h-icon-home',
          route: { name: 'wodezhuye' },
        },
        {
          title: '采购订单新增',
        },
      ],
      danju: {},
      pageStatus: 1, // 页面状态1：查看，2：编辑，3：新增
      data: {
        intData: null,
        guke: '',
        danjuhao: 0,
        danjuriqi: {},
        danjuzhuangtai: '',
        tuihuozhuangtai: '',
      },
      status: {
        0: '制单',
        1: '审核',
        2: '关闭',
      },
      status1: {
        0: '已确认',
        1: '未确认',
      },
      modeParam: {
        single: '一个区块一行',
        twocolumn: '两列一行',
        threecolumn: '三列一行',
        block: '标题独立一行',
      },
      isInputAsyncError: false,
      // 选项卡
      tabs: {
        module1: '内容',
        module2: '费用',
        module3: '部门预算',
      },
      selected: 'module1',
    };
  },
  computed: {
    currentComponent() {
      const comList = {
        module1: 'neirong', // 内容
        module2: 'feiyong', // 费用
        module3: 'bumenyusuan', // 部门预算
      };
      return comList[this.selected];
    },
  },
  watch: {},
  mounted() {
    this.$nextTick(function() {});
  },
  methods: {
    open() {
      this.$Modal({
        title: '处理',
        content: '我要去做特殊的处理',
      });
    },
    change(data) {
      this.selected = data.key;
    },
    searchPage() {
      this.$router.push({ name: 'cgddSearch' });
    },
    editPage() {
      this.pageStatus = 2;
    },
    baocunPage() {
      this.pageStatus = 1;
      this.danju = {
        danjuhao: 'DJ20190805001',
        tijiaoren: '张三',
        tijiaoshijian: '2019-08-05 14:41:32',
      };
    },
    addPage() {
      this.pageStatus = 3;
    },
    delPage() {
      console.log('delPage');
    },
  },
};
</script>
<style lang="less" scoped>
</style>

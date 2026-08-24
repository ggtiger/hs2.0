<template>
  <div class="rr-flex-col">
    <Form :label-width="110" mode="threecolumn" :model="data" ref="form" :top="0.2" showErrorTip>
      <FormItem label="顾客" prop="inputData">
        <input type="text" v-model="data.guke" placeholder="请输入顾客名称" :disabled="pageStatus===1" />
      </FormItem>
      <FormItem label="单据号" prop="numberData">
        <NumberInput
          type="text"
          placeholder="请输入单据号"
          v-model="data.danjuhao"
          :disabled="pageStatus===1"
        />
      </FormItem>
      <FormItem label="单据日期" ref="datapicker" prop="dateData">
        <DateRangePicker v-model="data.danjuriqi" :disabled="pageStatus===1"></DateRangePicker>
      </FormItem>
      <FormItem label="单据状态" prop="select2Data">
        <Select v-model="data.danjuzhuangtai" :datas="status" :disabled="pageStatus===1"></Select>
      </FormItem>
      <FormItem label="退货确认状态" prop="select2Data">
        <Select v-model="data.tuihuozhuangtai" :datas="status1" :disabled="pageStatus===1"></Select>
      </FormItem>
    </Form>
    <Tabs :datas="tabs" v-model="selected" @change="change" style="margin-bottom:20px"></Tabs>
    <component :is="currentComponent" class="rr-flex-1" :pageStatus="pageStatus"></component>
  </div>
</template>

<script>
import feiyong from './components/feiyong';
import neirong from './components/neirong';
export default {
  components: { feiyong, neirong },
  props: {
    pageStatus: {
      type: Number,
      default: 1,
    },
  },
  data() {
    return {
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
  },
};
</script>
<style lang="less" scoped>
</style>

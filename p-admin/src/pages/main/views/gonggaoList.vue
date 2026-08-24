<template>
  <view-dialog title="公告" style="width:1000px;">
    <template slot="body">
      <ul class="rr-gonggao">
        <li class="rr-flex-row" v-for="(item,index) in QRY11" :key="index" @click="getDetail(item)">
          <span class="rr-gonggao-xuhao">{{index+1}}</span>
          <div class="rr-flex-1 rr-gonggao-title">
            <a>{{item.NOTITLE}}</a>
          </div>
          <span>{{item.BILLDATE}}</span>
        </li>
      </ul>
      <Pagination v-model="pagination" align="left" @change="change"></Pagination>
    </template>
    <template slot="footer">
      <Button class="ml5" color="primary" @click.native="close">确定</Button>
    </template>
  </view-dialog>
</template>


<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 'gonggaoList',
  data() {
    return {
    };
  },
  computed: {
    ...mapDateTable('QRY11', []),
    ...mapDateTable('QQRY1', ['TotalCount', 'PageSize', 'PageIndex']),
    pagination: {
      get() {
        return {
          page: this.PageIndex,
          size: this.PageSize,
          total: this.TotalCount,
          pagerSize: 10,
        };
      },
      set(v) {
        this.PageIndex = v.page;
        this.PageSize = v.size;
      },
    },
  },
  created() {
      this.refreshNotice();
    },
  methods: {
    refreshNotice() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/query11`,
        param: {},
      });
    },

    getDetail(item) {
      this.$emit('getDetail', item);
    },
    close() {
      this.$emit('close');
    },
     change(pageInfo) {
      this.PageIndex = pageInfo.page;
      this.PageSize = pageInfo.size;
        this.refreshNotice();
    },
  },
};
</script>
<style lang="less" scoped>
@import '~@/theme/index.less';
.rr-gonggao {
  min-height: 300px;
  li {
    margin-bottom: 5px;
    .rr-gonggao-title {
      overflow: hidden;
      height: 24px;
      a {
        color: #666;
      }
    }
    &:hover {
      color: @primary-color;
    }
    .rr-gonggao-xuhao {
      border-radius: 3px;
      width: 20px;
      height: 20px;
      line-height: 20px;
      text-align: center;
      color: #fff;
      background: @primary-color;
      margin-right: 10px;
    }
  }
}
</style>

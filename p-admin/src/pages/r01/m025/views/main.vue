<template>
  <div class="h-panel h-panel-no-border rr-flex-col">
    <!-- 顶部面包屑 + 搜索栏 -->
    <div class="h-panel-bar rr-flex-row">
      <span class="h-panel-title" width="400px">
        <Breadcrumb :datas="datas"></Breadcrumb>
      </span>
      <Row :space="9" class="rr-flex-1">
        <Cell style="text-align: right;">
          <label style="margin-right:5px">关键字</label>
          <input type="text" style="width:150px" v-model="searchInput" placeholder="委托单号/客户/设备名等" @keyup.enter="doSearch" />
          <label style="margin:0 5px 0 10px">检校日期</label>
          <DateRangePicker style="width:220px;display:inline-block" v-model="searchBillDate"></DateRangePicker>
          <Button color="primary" icon="h-icon-search" class="ml5" @click="doSearch">查询</Button>
          <Button class="ml5" @click="resetSearch">重置</Button>
        </Cell>
      </Row>
    </div>

    <div class="h-panel-body rr-flex-1">
      <div class="rr-flex-col">
        <!-- 上部分：委托单列表 -->
        <div class="m025-top">
          <div class="m025-section-title">
            <span>委托单列表</span>
          </div>
          <div class="m025-table-wrap">
            <Table
              ref="wtTable"
              :datas="wtDatas"
              :loading="wtLoading"
              @select="onWtSelect"
              @click-row="onWtClickRow"
              border
              stripe
              checkbox
            >
              <TableItem title="委托单号" prop="WTCODE" width="180"></TableItem>
              <TableItem title="委托单位" prop="CUSTNAME"></TableItem>
              <TableItem title="器具数量" prop="DEVICECOUNT" width="80" align="center"></TableItem>
              <TableItem title="委托时间" prop="BILLDATE" width="130"></TableItem>
              <TableItem title="提交时间" prop="SUMBMITTIME" width="130" sort></TableItem>
              <TableItem title="提交人" prop="CREATER" width="80"></TableItem>
            </Table>
          </div>
          <table-tool-bar v-model="wtPageInfo" @change="onPageChange">
            <span class="m025-count" v-if="totalRows > 0">共 {{ totalRows }} 条</span>
          </table-tool-bar>
        </div>

        <!-- 下部分：委托明细列表 -->
        <div class="m025-bottom">
          <div class="m025-section-title">
            <span>委托明细</span>
            <span class="m025-count" v-if="selectedWt">当前委托单: {{ selectedWt.WTCODE }}</span>
          </div>
          <div class="m025-table-wrap">
            <Table
              ref="dtlTable"
              :datas="dtlDatas"
              :loading="dtlLoading"
              @select="onDtlSelect"
              checkbox
              border
              stripe
            >
              <TableItem title="序号" width="60" align="center">
                <template slot-scope="{ row, index }">
                  <span>{{ index + 1 }}</span>
                </template>
              </TableItem>
              <TableItem title="证书单位" prop="ORGNAME" width="150"></TableItem>
              <TableItem title="设备名称" prop="MNAME" width="150"></TableItem>
              <TableItem title="设备型号" prop="SIZETYPE" width="120"></TableItem>
              <TableItem title="设备编号" prop="OPCODE" width="120"></TableItem>
              <TableItem title="审核人" prop="CHECKER" width="80"></TableItem>
              <TableItem title="审批状态" width="80">
                <template slot-scope="{ row }">
                  <span v-if="row" :style="{ color: stateColor(row.STATE) }">{{ stateText(row.STATE) }}</span>
                </template>
              </TableItem>
            </Table>
          </div>
        </div>

        <!-- 底部操作栏 -->
        <table-tool-bar>
          <Button
            color="primary"
            icon="h-icon-check"
            v-per="'LI_M02/A12'"
            @click="startReview('check')"
            :disabled="!canStartReview"
          >委托审核</Button>
          <Button
            color="blue"
            icon="h-icon-refresh"
            v-per="'LI_M02/A14'"
            @click="startReview('verify')"
            :disabled="!canStartReReview"
          >委托审批</Button>
        </table-tool-bar>
      </div>
    </div>

    <!-- 审核弹窗 -->
    <rs-modal ref="reviewModal" :fullScreen="true" v-if="showReviewModal">
      <review-page
        :wtItem="selectedWt"
        :dtlItems="selectedDtlItems"
        :mode="reviewMode"
        @close="onReviewClose"
      ></review-page>
    </rs-modal>
  </div>
</template>
<script>
import List01 from '@/mixins/list01';
import store from '@/store';
import { mapState, mapGetters, mapDateTable, Constants, getStore } from '../store';
import { BILL_STATE_MAP, BILL_STATE_COLOR } from '@/constants';
export default {
  name: 'r01-m025-main',
  components: {
    reviewPage: () => import('./review.vue'),
  },
  computed: {
    ...mapDateTable('QQRY', [
      'WTCODE',
      'CUSTNAME',
      'SUBMITDATE',
      'STATE',
    ]),
    ...mapDateTable('EMPUSER', []),
    canStartReview() {
      let fchecks = this.selectedDtlItems.filter(item => {
        return item.STATE === 2;
      });
      return this.selectedDtlItems.length > 0 && fchecks.length === this.selectedDtlItems.length;
    },
    canStartReReview() {
      let fchecks = this.selectedDtlItems.filter(item => {
        return item.STATE === 5 || item.STATE === 19;
      });
      return this.selectedDtlItems.length > 0 && fchecks.length === this.selectedDtlItems.length;
    },
    wtPageInfo: {
      get() {
        return {
          page: this.currentPage,
          size: this.pageSize,
          total: this.totalRows,
          pagerSize: 1,
        };
      },
      set(v) {
        this.currentPage = v.page;
        this.pageSize = v.size;
      },
    },
  },
  mixins: [List01],
  data() {
    return {
      store: { mapState, mapGetters, mapDateTable, Constants },
      showQuery: false,
      checks: [],
      REMARK: '',
      datas: [
        { title: '检验管理' },
        { title: '委托审核' },
      ],
      // 搜索条件
      searchInput: '',
      searchBillDate: null,
      // 委托单列表
      wtLoading: false,
      wtDatas: [],
      selectedWt: null,
      totalRows: 0,
      currentPage: 1,
      pageSize: 20,
      // 明细列表
      dtlLoading: false,
      dtlDatas: [],
      selectedDtlItems: [],
      showReviewModal: false,
      reviewMode: 'check',
      empParam1: {
        loadData: this.empSel1,
        keyName: 'ID',
        titleName: 'EMPNAME',
      },
      VERIFYID: '',
      VERIFYER: '',
    };
  },
  async created() {
    // 确保 LI_M02 模块已初始化，防止直接刷新页面时模块未加载
    await store.dispatch('app/initModule', 'LI_M02');
    getStore();
  },
  mounted() {
    this.loadWtList();
  },
  methods: {
    stateText(state) {
      if (state == null) return '';
      return BILL_STATE_MAP[state] || '未知';
    },
    stateColor(state) {
      return BILL_STATE_COLOR(state);
    },
    // 搜索相关
    doSearch() {
      this.currentPage = 1;
      this.loadWtList();
    },
    resetSearch() {
      this.searchInput = '';
      this.searchBillDate = null;
      this.currentPage = 1;
      this.loadWtList();
    },
    // 加载委托单列表
    async loadWtList() {
      this.wtLoading = true;
      try {
        let ret = await this.$callAction({
          action: Constants.STORE_NAME + '/loadWtList',
          param: { input: this.searchInput || '', billDate: this.searchBillDate },
          isBusy: false,
        });
        // A53 返回 orecord 粒度数据，按 REFBILLID 分组为委托单级别
        let items = ret.Items || [];
        // 缓存所有 orecord 行，供下表明细使用
        this._allOrecords = items;
        let groupMap = {};
        items.forEach(function(item) {
          let key = item.REFBILLID;
          if (!groupMap[key]) {
            groupMap[key] = {
              REFBILLID: item.REFBILLID,
              WTCODE: item.WTCODE,
              CUSTNAME: item.CUSTNAME,
              BILLDATE: item.BILLDATE,
              SUMBMITTIME: item.SUMBMITTIME,
              CREATER: item.CREATER,
              DEVICECOUNT: 0,
            };
          }
          groupMap[key].DEVICECOUNT++;
        });
        let groups = Object.values(groupMap);
        // 前端分页
        this.totalRows = groups.length;
        let start = (this.currentPage - 1) * this.pageSize;
        this.wtDatas = groups.slice(start, start + this.pageSize);
      } catch (e) {
        console.error('加载委托单列表失败', e);
      } finally {
        this.wtLoading = false;
      }
    },
    onPageChange(pageInfo) {
      this.currentPage = pageInfo.page;
      this.pageSize = pageInfo.size;
      this.loadWtList();
    },
    // 点击委托单行
    onWtClickRow(row) {
      // 手动实现单选：清除其他选中，只选中当前行
      if (this.selectedWt && this.selectedWt !== row) {
        this.$refs.wtTable.setCheck(this.selectedWt, false);
      }
      this.$refs.wtTable.setCheck(row, true);
      this.selectedWt = row;
      this.selectedDtlItems = [];
      if (this._allOrecords) {
        this.dtlDatas = this._allOrecords.filter(function(item) {
          return item.REFBILLID === row.REFBILLID;
        });
      } else {
        this.dtlDatas = [];
      }
    },
    // checkbox 选中变化（配合单选逻辑）
    onWtSelect(checks) {
      if (checks.length > 1) {
        // 只保留最后选中的
        var last = checks[checks.length - 1];
        this.wtDatas.forEach(function(item) {
          if (item !== last) {
            this.$refs.wtTable.setCheck(item, false);
          }
        }.bind(this));
        this.selectedWt = last;
      } else if (checks.length === 1) {
        this.selectedWt = checks[0];
      } else {
        this.selectedWt = null;
      }
      this.selectedDtlItems = [];
      if (this._allOrecords && this.selectedWt) {
        this.dtlDatas = this._allOrecords.filter(function(item) {
          return item.REFBILLID === this.selectedWt.REFBILLID;
        }.bind(this));
      } else {
        this.dtlDatas = [];
      }
    },
    onDtlSelect(checks) {
      this.selectedDtlItems = checks || [];
      this.checks = this.selectedDtlItems;
    },
    // 开始审核/审批（统一入口）
    startReview(mode) {
      if (!this.selectedWt) {
        this.$error('请先选择一条委托单！');
        return;
      }
      if (this.selectedDtlItems.length === 0) {
        this.$error(mode === 'verify' ? '请选择需要审批的委托明细！' : '请选择需要审核的委托明细！');
        return;
      }
      this.reviewMode = mode;
      this.showReviewModal = true;
      this.$nextTick(() => {
        this.$refs.reviewModal.show();
      });
    },
    onReviewClose() {
      this.showReviewModal = false;
      if (this.$refs.reviewModal) {
        this.$refs.reviewModal.close();
      }
      // 重新加载数据
      this.loadWtList();
    },
    selectRow(checks) {
      this.checks = checks || [];
    },
    advQuery() {
      this.doSearch();
    },
    async empSel1(INPUT, callback) {
      if (this.TEMP1 === INPUT) {
        INPUT = '';
      }
      await this.$callAction({
        action: `${Constants.STORE_NAME}/empSel1`,
        param: {
          INPUT,
          FUNCID: '3be11623d4114bc68a8e63551e861ced',
          DEPTID: this.selectedWt ? this.selectedWt.ADEPTID : '',
        },
        isBusy: false,
      });
      callback(this.EMPUSER);
    },
  },
};
</script>
<style lang="less" scoped>
@import '~@/theme/modern.less';
/deep/ .h-panel-bar {
  background: #fff;
  border-bottom: 1px solid #f0f0f0;
  padding: 10px 20px;
  display: flex;
  align-items: center;
}
/deep/ .h-panel-body {
  padding: 10px 20px;
}
/deep/ .h-breadcrumb a {
  color: @primary-color;
}
/deep/ .h-btn-primary {
  background-color: @primary-color;
}
.m025-top {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.m025-bottom {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.m025-table-wrap {
  flex: 1;
  overflow: hidden;
}
.m025-section-title {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 0;
  font-size: 13px;
  font-weight: bold;
  border-bottom: 1px solid #e8e8e8;
  margin-bottom: 5px;
}
.m025-count {
  font-size: 12px;
  color: #999;
  font-weight: normal;
}
</style>

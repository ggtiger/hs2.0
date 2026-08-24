<template>
  <list-t01
    title="文件管理"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    :showQuery="showQuery"
    @list-select="selectRow"
    :checkbox="true"
    ref="list"
  >
    <rs-modal ref="madd" :fullScreen="true">
      <rsAdd
        :storeName="store.Constants.STORE_NAME"
        :citem="citem"
        :showQuery="showQuery"
        title="文件管理"
        :ID="CDID"
      ></rsAdd>
    </rs-modal>
    <template slot="body-query">
      <Row :space="9">
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">发布日期</label>
            <DateRangePicker class="rr-flex-1" v-model="BILLDATE"></DateRangePicker>
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">文件名称</label>
            <input type="text" class="rr-flex-1" v-model="DOCNAME" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">文件代码</label>
            <input type="text" class="rr-flex-1" v-model="DOCCODE" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">文件序号</label>
            <input type="text" class="rr-flex-1" v-model="DOCSORT" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">所属分类</label>
            <Select class="rr-flex-1" v-model="DOCTYPEID" dict="文件类别"></Select>
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">所属部门</label>
            <input type="text" class="rr-flex-1" v-model="DEPTNAME" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">状态</label>
            <Select class="rr-flex-1" v-model="STATE" :datas="param"></Select>
          </div>
        </Cell>
        <Cell width="6">
          <div style="width:100%;text-align:right;padding-right:10px">
            <Button class="ml5" @click="advQuery">查询</Button>
            <Button class="ml5">重置</Button>
          </div>
        </Cell>
      </Row>
    </template>
    <template slot="header-action">
      <Button class="ml5" @click="showQuery=!showQuery">高级查询</Button>
    </template>

    <template slot="footer-action">
      <!--
      <Button color="primary" icon="h-icon-plus" @click="add" v-per="'RS_M10/A04'">添加</Button>
      <Tooltip
        theme="white"
        trigger="click"
        editable
        v-per="'RS_M10/A17'"
        v-if="ISSHOWCHECK"
        ref="checkTip"
      >
        <Button class="ml5" icon="h-icon-check" color="primary">审核</Button>
        <div slot="content">
          <div v-padding="10">
            <textarea dict="simple" v-model="REMARK" style="width: 200px;"></textarea>
          </div>
          <div v-padding="10" class="text-center">
            <Button color="primary" @click.native="batchCheck(ID);$refs.checkTip.hide();">通过</Button>
            <Button
              class="ml5"
              color="red"
              @click.native="batchCheckReject(ID);$refs.checkTip.hide();"
            >驳回</Button>
          </div>
        </div>
      </Tooltip>
      <Poptip content="确定撤销审核？" v-per="'RS_M10/A19'" v-if="ISSHOWRECHECK" @confirm="batchReCheck">
        <Button class="ml5" color="red" icon="h-icon-close">撤销审核</Button>
      </Poptip>
      -->
      <Tooltip
        theme="white"
        trigger="click"
        v-per="'RS_M10/A20'"
        editable
        v-if="ISSHOWRECHECK"
        ref="verifyTip"
      >
        <Button class="ml5" icon="h-icon-check" color="primary">审批</Button>
        <div slot="content">
          <div v-padding="10">
            <textarea dict="simple" v-model="REMARK" style="width: 200px;"></textarea>
          </div>
          <div v-padding="10" class="text-center">
            <Button color="primary" @click.native="batchVerify(ID);$refs.verifyTip.hide();">通过</Button>
            <Button
              class="ml5"
              color="red"
              @click.native="batchVerifyReject(ID);$refs.verifyTip.hide();"
            >驳回</Button>
          </div>
        </div>
      </Tooltip>
      <Poptip content="确定撤销审批？" v-per="'RS_M10/A22'" v-if="ISSHOWREVERIFY" @confirm="batchReVerify">
        <Button class="ml5" color="red" icon="h-icon-close">撤销审批</Button>
      </Poptip>
    </template>
  </list-t01>
</template>
<script>
import rsAdd from './add2.vue';
import List01 from '@/mixins/list01';
import { mapState, mapGetters, mapDateTable, Constants } from '../store2';
export default {
  name: 's01-m102-main',
  components: {
    rsAdd,
  },
  computed: {
    ...mapDateTable('QQRY', ['BILLDATE', 'DOCNAME', 'DOCCODE', 'DOCSORT', 'DOCTYPEID', 'DEPTNAME', 'STATE']),
  },
  mixins: [List01],
  data() {
    return {
      CDID: '',
      citem: {},
      showQuery: false,
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [
        {
          title: '公文管理',
        },
        {
          title: this.$route.meta.title,
        },
      ],
      param: [
        { title: '待审批', key: 5 },
        { title: '已审批', key: 6 }
      ],
      checks: [],
      REMARK: '',
    };
  },
  methods: {
    add() {
      this.CDID = '';

      this.$refs.madd.show();
    },
    clickRow(row) {
      this.CDID = row.ID;
      this.citem = row;
      this.$refs.madd.show();
    },
    selectRow(checks) {
      this.checks = checks;
    },
    advQuery(param) {
      this.$refs.list.advQuery();
    },
  },
};
</script>

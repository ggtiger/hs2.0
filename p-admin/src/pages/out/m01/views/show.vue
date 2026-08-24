<template>
  <div class="show" style="overflow-x: hidden;">
      <div class="h-panel-bar">
      <span class="h-panel-title">
       证书校验
      </span>
    </div>
      <Row :space="1" style="padding:0px 10px">
        <Cell width="24">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width: 60px">客户名称	</label>
            <input type="text" readonly class="rr-flex-1" v-model="CUSTNAME" />
          </div>
        </Cell>
        <Cell width="12">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width: 60px">设备名称</label>
            <input type="text" readonly class="rr-flex-1" v-model="MNAME" />
          </div>
        </Cell>
        <Cell width="12">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width: 60px">型号规格</label>
            <input type="text" readonly class="rr-flex-1" v-model="SIZETYPE" />
          </div>
        </Cell>
        <Cell width="12">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width: 60px">出厂编号	</label>
            <input type="text" readonly class="rr-flex-1" v-model="OPCODE" />
          </div>
        </Cell>
        <Cell width="12">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width: 60px">生产厂家	</label>
            <input type="text" readonly class="rr-flex-1" v-model="MANUFACTURER" />
          </div>
        </Cell>
         <Cell width="12">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width: 60px">校准日期</label>
            <input type="text" readonly class="rr-flex-1" v-model="BILLDATE" />
          </div>
        </Cell>
        <Cell width="12">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width: 60px">签发日期</label>
            <input type="text" readonly class="rr-flex-1" v-model="SIGNDATE" />
          </div>
        </Cell>
      </Row>
       <div class="info"><label>{{info}}</label></div>
      <pdf
            v-for="i in pageNum"
            :key="i"
            :src="pdfSrc"
            :page="i">
            </pdf>


  </div>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
// eslint-disable-next-line no-restricted-imports
import db from '@/api/db';
import pdf from 'vue-pdf';
export default {
   components: { pdf },
  computed: {
    ...mapDateTable('MAIN', ['ID','MNAME', 'SIZETYPE','OPCODE','MANUFACTURER','BILLDATE','SIGNDATE','CERTID','CUSTNAME']),
  },
  data() {
    return {
      CDID: '',
      showQuery: false,
      citem: {},
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [
        {
          title: '证书验证',
        },
      ],
      pdfSrc: '',
      pageNum:0,
      info:'加载中....'
    };
  },

  methods: {
    show() {
      if(this.CERTID){
        this.info="证书加载中..."
      }
      this.pdfSrc = db.getUrl('pdfsy') + this.CERTID;
      this.pdfSrc = pdf.createLoadingTask(this.pdfSrc);
      this.pdfSrc.promise.then(pdf => {
        this.pageNum = pdf.numPages
        this.info="验证通过";
      });
    }
  },
  async mounted(){
    await this.$callAction({ action: `${Constants.STORE_NAME}/open`, param: { ID: this.$route.query.ID }, isBusy: false });
    this.show();
  }
};
</script>
<style lang="css" scoped>
 .show {
   overflow-x: hidden;
 }
.info {
    text-align: center;
    font-size: 20px;
    color: red;
    position: absolute;
    top: 200px;
    z-index: 999;
    width: 100%;
}
.info label {

}
</style>

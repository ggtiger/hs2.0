<template>
  <view-dialog title="预览" style="max-width:1000px; position:relative">
    <template slot="body" style>
      <div class="pdf">
        <div class="pdf-header" v-if="false">
          <Button @click.native.stop="prePage()" :text="true">
            <i class="h-icon-top"></i>上一页
          </Button>
          <span class="pdf-header-fy">{{pageNum}} / {{pageTotalNum}}</span>
          <Button @click.native.stop="nextPage()" :text="true">
            下一页
            <i class="h-icon-down"></i>
          </Button>
        </div>
        <div class="rr-text-center">
           <!-- <pdf
            v-if="this.type!='preview'"
            ref="pdf"
            :src="src"
            :page="pageNum"
            @num-pages="pageTotalNum=$event"
            @page-loaded="pageLoaded($event)"
            @link-clicked="page = $event"
            :style="{width:+ scale+'%'}"
            @progress="loadedRatio = $event"
          ></pdf>-->
            <pdf
            v-for="i in pageNum"
            :key="i"
            :src="src"
            :style="{width:'100%'}"
            :page="i">
            </pdf>
         <iframe
            v-if="this.type!='preview'"
            :src="src"
            frameborder="0"
            style="width: 100%; height: 80vh"
            class="rr-scroll-bar"
          ></iframe>
        </div>
        <div class="pdf-right" v-if="false">
          <div style="margin-bottom:10px">
            <Button
              @click.native="scaleBig()"
              color="primary"
              icon="h-icon-plus"
              :icon-circle="true"
            ></Button>
          </div>
          <div>
            <Button
              @click.native="scaleSmall()"
              color="primary"
              icon="h-icon-minus"
              :icon-circle="true"
            ></Button>
          </div>
        </div>
      </div>
    </template>
    <template slot="footer" v-if="false">
      <Button class="ml5" color="primary" icon="rr-font rr-font-dayin" @click.native="print()">打印</Button>
    </template>
  </view-dialog>
</template>
<script>
import pdf from 'vue-pdf';
export default {
  metaInfo: {
    meta: [
      { charset: 'utf-8' },
      { name: 'viewport', content: 'width=device-width,initial-scale=1,minimum-scale=1,maximum-scale=2,user-scalable=yes' }
    ]
  },
  components: { pdf },
  name: 'rs-print-pdf',
  props: {
    src: {
      type: String,
      default: 'http://101.132.42.6:5001/api/file/195ec6c3bf7348a3a590e72560a28efd',
    },
    type: {
      type: String,
      default: 'print'
    }
  },
  data() {
    return {
      pageNum: 0,
      pageTotalNum: 1,
      pageRotate: 0, // 加载进度
      loadedRatio: 0,
      curPageNum: 0,
      scale: 100, // 放大倍数，增长10倍
    };
  },
  mounted() {
    // 有时PDF文件地址会出现跨域的情况,这里最好处理一下
    if (this.type == 'preview') {
      this.src = pdf.createLoadingTask(this.src);
      this.src.promise.then(pdf => {
        this.pageNum = pdf.numPages;
      });
    }
  },
  methods: {
    // 放大
    scaleBig() {
      if (this.scale === 100) {
        return;
      }
      this.scale += 5;
    },
    // 缩小
    scaleSmall() {
      this.scale = this.scale - 5;
    },
    prePage() {
      var p = this.pageNum;
      p = p > 1 ? p - 1 : this.pageTotalNum;
      this.pageNum = p;
    },
    nextPage() {
      var p = this.pageNum;
      p = p < this.pageTotalNum ? p + 1 : 1;
      this.pageNum = p;
    },
    pageLoaded(e) {
      this.curPageNum = e;
    },
    print() {
      this.$refs.print.print();
    },
  },
};
</script>
<style lang="postcss" scoped>
.pdf-header {
  position: absolute;
  top: 15px;
  left: 50%;
  margin-left: -90px;
  .pdf-header-fy {
    padding: 0 10px;
  }
}
.pdf-right {
  position: absolute;
  right: 40px;
  bottom: 100px;
}
</style>

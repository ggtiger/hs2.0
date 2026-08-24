<template>
  <view-dialog title="选入 Word 模版" @on-show="onShow" :loading="loading">
    <div slot="body" style="height: calc(100vh - 167px);">
      <Form :label-width="80" class="maxModalH rs-flex-col" style="overflow: hidden;">
        <Row>
          <Col span="8">
            <Search placeholder="请输入模版名称" v-model="searchKey" style="width:100%;" @search="doSearch" />
          </Col>
          <Col span="8" offset="1">
            <Select v-model="filterType" :datas="typeOptions" placeholder="模版类型" style="width:100%;" @change="doSearch" size="s"></Select>
          </Col>
        </Row>
        <div class="rs-flex-1 rr-overflow-hidden maxModalH">
          <Table
            ref="table"
            :datas="templateList"
            border
            radio
            selectRow
            @rowSelect="onRowSelect"
          >
            <TableItem title="模版名称" prop="TEMPLATENAME" :width="200"></TableItem>
            <TableItem title="模版类型" :width="120">
              <template slot-scope="{data}">
                {{ typeLabel(data.TEMPLATETYPE) }}
              </template>
            </TableItem>
            <TableItem title="文件名" prop="FILENAME"></TableItem>
          </Table>
        </div>
      </Form>
    </div>
    <template slot="footer">
      <Button class="ml5" @click.native="close">取消</Button>
      <Button class="ml5" color="primary" @click.native="confirm">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import db from '@/api/db';
import { httpGet } from '@/components/rs-onlyoffice-shared';

// 模版类型编码 → 中文名称映射（与字典 D0028 WORD模版类型一致）
var TYPE_MAP = {
  'ZS': '证书模版',
  'DY': '打印模版',
  'YSJL': '原始记录模版',
  'WJDY': '文件打印模版',
};

export default {
  name: 'template-selector',
  props: {
    templateType: {
      type: String,
      default: '',
    },
    moduleCode: {
      type: String,
      default: '',
    },
  },
  data() {
    return {
      searchKey: '',
      filterType: this.templateType || '',
      templateList: [],
      selectedRow: null,
      loading: false,
      typeOptions: [
        { key: '', title: '全部类型' },
        { key: 'ZS', title: '证书模版' },
        { key: 'YSJL', title: '原始记录模版' },
        { key: 'DY', title: '打印模版' },
        { key: 'WJDY', title: '文件打印模版' },
      ],
    };
  },
  methods: {
    typeLabel(code) {
      return TYPE_MAP[code] || code || '';
    },
    onShow() {
      this.filterType = this.templateType || '';
      this.selectedRow = null;
      this.doSearch();
    },
    doSearch() {
      var self = this;
      var apiUrl = db.getUrl('url');
      var token = self.$store.state['user'].access_token;

      var params = [];
      if (self.searchKey) params.push('keyword=' + encodeURIComponent(self.searchKey));
      if (self.filterType) params.push('templateType=' + encodeURIComponent(self.filterType));
      if (self.moduleCode) params.push('moduleCode=' + encodeURIComponent(self.moduleCode));

      var url = apiUrl + '/api/word-template/list' + (params.length ? '?' + params.join('&') : '');
      self.loading = true;

      httpGet(url, token).then(function(result) {
        self.loading = false;
        if (result.success) {
          self.templateList = result.data || [];
        } else {
          self.templateList = [];
        }
      }).catch(function() {
        self.loading = false;
        self.templateList = [];
      });
    },
    onRowSelect(data) {
      this.selectedRow = data;
    },
    close() {
      this.$parent.setvalue(false);
    },
    confirm() {
      if (!this.selectedRow) {
        this.$Notice('请选择一个模版');
        return;
      }
      this.$emit('on-select', this.selectedRow);
      this.$parent.setvalue(false);
    },
  },
};
</script>
<style scoped>
.maxModalH {
  height: calc(100vh - 185px);
  overflow: auto;
}
</style>

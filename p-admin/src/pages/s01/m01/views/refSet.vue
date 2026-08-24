<template>
  <view-dialog title="引用来源表">
    <template slot="body">
      <Form ref="formValidate" mode="twocolumn" :label-width="80" class="rs-flex-col">
        <Row>
          <FormItem label="资源名称" prop="RESOURCENAME" :single="showType=='MAIN'">
            <AutoComplete
              :option="param"
              v-model="REFRESOURCE"
              type="object"
              :disabled="showType=='MAIN'"
              @change="selectMethod"
            >
              <template slot="item" slot-scope="{item}">
                <div>{{item.value.RESOURCENAME}}</div>
              </template>
            </AutoComplete>
          </FormItem>
          <FormItem label="资源别名" v-if="showType!=='MAIN'">
            <input type="text" v-model="REFRESOURCEANAME" placeholder="请输入资源别名" />
          </FormItem>
          <FormItem label="来源表名" v-if="showType!=='MAIN'" single>
            <input type="text" v-model="REFRELATION" placeholder="请输入引用关系" />
          </FormItem>
        </Row>
        <div class="rs-flex-1 rr-overflow-hidden">
          <Table border ref="selection" :datas="SELFDTS" checkbox>
            <TableItem title="#" prop="$serial" :width="60"></TableItem>
            <TableItem title="名称" prop="COMMENTS" :width="150"></TableItem>
            <TableItem title="字段名" prop="FIELDNAME" :width="150"></TableItem>
            <TableItem title="允许空">
              <template slot-scope="{data}">{{data.NULLABLE?'√':''}}</template>
            </TableItem>
            <TableItem title="主键否">
              <template slot-scope="{data}">{{data.ISKEY?'√':''}}</template>
            </TableItem>
            <TableItem title="字段类型" prop="FIELDTYPE"></TableItem>
          </Table>
        </div>
      </Form>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="close">取消</Button>
      <Button class="ml5" color="primary" @click.native="ok">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapDateTable, Constants } from '../store';
export default {
  name: 'zyrefSet',
  props: {
    showType: {
      type: [String, Number],
    },
  },
  data() {
    return {
      param: {
        loadData: this.remoteMethod2,
        keyName: 'ID',
        titleName: 'RESOURCENAME',
      },
    };
  },
  methods: {
    clickCheck(row) {
      debugger;
    },
    async remoteMethod2(INPUT, callback) {
      await this.$callAction({ action: `${Constants.STORE_NAME}/querySelT`, param: { INPUT }, isBusy: false });
      callback(this.SEL);
    },
    selectMethod() {
      this.$callAction({ action: `${Constants.STORE_NAME}/querySelsDts`,
        param: {
          RESOURCEID: this.REFRESOURCEID,
        },
        isBusy: false });
    },
    close() {
      this.$parent.setvalue(false);
    },
    ok() {
      this.$emit('on-ok', this.$refs.selection.getSelection());
      this.$parent.setvalue(false);
    },
  },
  computed: {
    ...mapDateTable('SELF', ['REFRESOURCENAME', 'REFRESOURCEANAME', 'REFRELATION', 'REFRESOURCEID', 'TYPE']),
    ...mapDateTable('SELFDTS', []),
    ...mapDateTable('SEL', []),
    REFRESOURCE: {
      get() {
        return { ID: this.REFRESOURCEID, RESOURCENAME: this.REFRESOURCENAME };
      },
      set(v) {
        v = v || {};
        this.REFRESOURCEID = v.ID;
        this.REFRESOURCENAME = v.RESOURCENAME;
      },
    },
  },
  watch: {
    SELFDTS: {
      handler(v) {
        this.$nextTick(function() {
          this.$refs.selection.setSelection(this.SELFDTS.filter((item, index) => item.ISREF == 1));
        });
      },
    },
  },
};
</script>
<style scoped>

</style>

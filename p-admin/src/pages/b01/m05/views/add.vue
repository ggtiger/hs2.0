<template>
  <view-dialog :title="title"  class="d-width">
    <template slot="body">
      <ToolBar label="基本信息" :size="16"></ToolBar>
      <rs-form-edit
        ref="form"
        class="maxModalH rs-flex-col"
        :label-width="80"
        mode="twocolumn"
        :path="$MAIN"
      >
        <template slot="UPDEPTNAME">
          <TreePicker :option="param" ref="UPDEPTID" v-model="UPDEPTID"></TreePicker>
        </template>
      </rs-form-edit>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'LIB_M05/A07'" v-if="ID" @confirm="del">
        <Button class="ml5" color="red">删除</Button>
      </Poptip>
      <Button class="ml5" v-per="'LIB_M05/A04'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Add01 from '@/mixins/add01';
export default {
  name: 'b01-m05-add',
  mixins: [Add01],
  data() {
    return {
      param: {
        keyName: 'ID',
        parentName: 'UPDEPTID',
        titleName: 'DEPTNAME',
        dataMode: 'list',
        getTotalDatas: this.updeptSel,
      },
    };
  },
  computed: {
    ...mapDateTable('MAIN', ['UPDEPTID']),
    ...mapDateTable('UPDEPT', []),
  },
  methods: {
    async updeptSel(callback) {
      await this.$callAction({ action: `${Constants.STORE_NAME}/updeptSel`, param: { ID: this.ID }, isBusy: false });
      callback(this.UPDEPT);
    },
  },
};
</script>

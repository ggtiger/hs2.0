<template>
  <view-dialog :title="title"  class="d-width">
    <template slot="body">
      <ToolBar label="基本信息" :size="16"></ToolBar>
      <rs-form-edit ref="form" class="maxModalH rs-flex-col" :label-width="80" mode="twocolumn" :path="$MAIN">
      </rs-form-edit>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'LIB_M01/A07'" v-if="ID" @confirm="del">
        <Button class="ml5" color="red">删除</Button>
      </Poptip>
      <Button class="ml5" v-per="'LIB_M01/A04'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapDateTable, Constants } from '../store';
import Add01 from '@/mixins/add01';
import Sel01 from '@/mixins/sel01';
export default {
  name: 'b01-m05-add',
  mixins: [Add01, Sel01],
  data() {
    return {};
  },
  computed: {
    ...mapDateTable('MAIN', []),
  },
  methods: {},
  mounted() {
  },
};
</script>

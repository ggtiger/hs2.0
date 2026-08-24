<template>
  <view-dialog :title="title">
    <template slot="body">
      <ToolBar label="基本信息" :size="16"></ToolBar>
      <rs-form-edit
        ref="form"
        class="maxModalH rs-flex-col"
        :label-width="80"
        mode="twocolumn"
        :path="$MAIN"
      ></rs-form-edit>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Button class="ml5" v-per="'RS_MAIDEV/A03'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapDateTable } from '../store';
import Add01 from '@/mixins/add01';
export default {
  name: 's01-mAIDev-add',
  mixins: [Add01],
  data() {
    return {};
  },
  computed: {
    ...mapDateTable('MAIN', []),
  },
  methods: {},
};
</script>

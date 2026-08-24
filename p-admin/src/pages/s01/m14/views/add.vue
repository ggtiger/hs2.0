<template>
  <view-dialog :title="title">
    <template slot="body">
      <ToolBar label="基本配置" :size="16"></ToolBar>
      <rs-form-edit
        ref="form"
        class="maxModalH rs-flex-col"
        :label-width="90"
        mode="twocolumn"
        :path="$MAIN"
      ></rs-form-edit>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'RS_M14/A05'" v-if="ID" @confirm="del">
        <Button class="ml5" color="red">删除</Button>
      </Poptip>
      <Button class="ml5" v-per="'RS_M14/A04'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Add01 from '@/mixins/add01';

export default {
  name: 's01-m14-add',
  mixins: [Add01],
  computed: {
    ...mapDateTable('MAIN', [])
  }
};
</script>

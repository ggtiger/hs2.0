<template>
  <view-dialog :title="title" class="d-width">
    <template slot="body">
      <ToolBar label="基本信息" :size="16" />
      <rs-form-edit ref="form" class="maxModalH rs-flex-col" :label-width="100" mode="twocolumn" :path="$MAIN">
      </rs-form-edit>
    </template>
    <template slot="footer">
      <Button @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'LIB_M012/A05'" v-if="ID" @confirm="del">
        <Button color="red">删除</Button>
      </Poptip>
      <Button v-per="'LIB_M012/A04'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Add01 from '@/mixins/add01';

export default {
  name: 'm012-add',
  mixins: [Add01],
  computed: {
    ...mapDateTable('MAIN', [])
  }
};
</script>

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
      <ToolBar label="字典值" :size="16">
        <div slot="right">
          <Button color="primary" icon="h-icon-plus" size="s" @click="addDts('DTSA')">新增</Button>
          <Button color="primary" icon="h-icon-minus" size="s" @click="removeDts('DTSA',$refs.DTSA)">移除</Button>
          <Button color="primary" icon="h-icon-top" size="s" @click="moveUp('DTSA',$refs.DTSA)">上移</Button>
          <Button color="primary" icon="h-icon-down" size="s" @click="moveDown('DTSA',$refs.DTSA)">下移</Button>
        </div>
      </ToolBar>
      <rs-table-edit border ref="DTSA" :path="$DTSA" :datas="DTSA" :height="300"></rs-table-edit>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'RS_M06/A04'" v-if="ID" @confirm="del"><Button class="ml5" color="red">删除</Button></Poptip>
      <Button class="ml5" v-per="'RS_M06/A03'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Add01 from '@/mixins/add01';
export default {
  name: 's01-m05-add',
  mixins: [Add01],
  computed: {
    ...mapDateTable('MAIN', []),
    ...mapDateTable('DTSA', []),
  },
};
</script>

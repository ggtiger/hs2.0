<template>
  <view-dialog :title="title" class="d-width">
    <template slot="body">
      <ToolBar label="基本信息" :size="16" />
      <rs-form-edit ref="form" class="maxModalH rs-flex-col" :label-width="100" mode="twocolumn" :path="$MAIN">
      </rs-form-edit>
      <ToolBar label="培训记录" :size="16">
        <div slot="right">
          <Button color="primary" icon="h-icon-plus" size="s" @click="addDts('DTSA')">新增</Button>
          <Button color="primary" icon="h-icon-minus" size="s" @click="removeDts('DTSA', $refs.DTSA)">删除</Button>
          <Button color="primary" icon="h-icon-top" size="s" @click="moveUp('DTSA', $refs.DTSA)">上移</Button>
          <Button color="primary" icon="h-icon-down" size="s" @click="moveDown('DTSA', $refs.DTSA)">下移</Button>
        </div>
      </ToolBar>
      <rs-table-edit border ref="DTSA" :path="$DTSA" :datas="DTSA" style="min-height:200px;"></rs-table-edit>
    </template>
    <template slot="footer">
      <Button @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'LIB_M09/A05'" v-if="ID" @confirm="del">
        <Button color="red">删除</Button>
      </Poptip>
      <Button v-per="'LIB_M09/A04'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Add01 from '@/mixins/add01';

export default {
  name: 'm09-add',
  mixins: [Add01],
  computed: {
    ...mapDateTable('MAIN', []),
    ...mapDateTable('DTSA', [])
  },
  methods: {
    addDts(path) {
        this.$store.commit(`${Constants.STORE_NAME}/ADD`, {path});
      },
     removeDts(path, table) {
        if (table.currentRow === -1) {
          return;
        }
        this.$store.commit(`${Constants.STORE_NAME}/DEL`, { path, item: table.currentRow });
      },
  }
};
</script>

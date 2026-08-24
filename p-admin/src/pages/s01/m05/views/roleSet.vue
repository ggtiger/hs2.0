<template>
  <view-dialog title="设置角色" @on-show="onShow">
    <template slot="body">
      <Transfer v-model="DTSADATA" :datas="ROLE" keyName="ID">
        <template slot="sourceHeader">
          <div class="h-transfer-header">角色</div>
        </template>
        <template slot="targetHeader">
          <div class="h-transfer-header">已设置</div>
        </template>
        <template slot-scope="{option}" slot="item">{{option.ROLENAME}}</template>
      </Transfer>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Button class="ml5" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Gen from '@/utils/gen';
import bus from '../eventbus';
export default {
  name: 'role-set',
  props: {
    params: { Type: Object },
  },
  components: {},
  data() {
    return {
      DTSADATA: [],
    };
  },
  computed: {
    ...mapDateTable('ROLE', []),
    ...mapDateTable('DTSA', []),
  },
  mounted() {},
  methods: {
    closeW() {
      this.$parent.setvalue(false);
    },
    save() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/saveRole`,
        param: {},
        successText: '保存成功',
        isSuccessBack: true,
      });
    },
    async del() {
      await this.$confirm('确认删除？');
      this.$callAction({
        action: `${Constants.STORE_NAME}/delete`,
        successText: '删除成功',
        isSuccessBack: true,
      });
    },
    async onShow() {
      this.$callAction({ action: `${Constants.STORE_NAME}/openSel`, param: { ID: this.params.ID }, isBusy: false });
      await this.$callAction({ action: `${Constants.STORE_NAME}/openDts`, param: { ID: this.params.ID }, isBusy: false });
      this.DTSADATA = this.DTSA.map(item => item.ROLEID);
    },
  },
  async mounted() {
    bus.$on('change', (item, ISCHECK) => {
      this.change(item, ISCHECK);
    });
    bus.$on('change-up-item', item => {
      this.changeUpItem(item);
    });
  },
  watch: {
    DTSADATA: {
      handler(v) {
        this.$store.commit(`${Constants.STORE_NAME}/SET_DTSA`, { data: v, USERID: this.params.ID });
      },
    },
  },
};
</script>

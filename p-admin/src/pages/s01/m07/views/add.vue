<template>
  <view-dialog :title="title"   :loading="loading">
    <template slot="body">
      <ToolBar label="基本信息" :size="16"></ToolBar>
      <rs-form-edit
        ref="form"
        class="maxModalH rs-flex-col"
        :label-width="80"
        mode="twocolumn"
        :path="$MAIN"
      >
        <template slot="EXPTEMPFILENAME">
          <RsUploader :options="options" type="file" data-type="file" v-model="EXPTEMPFILE"></RsUploader>
        </template>
      </rs-form-edit>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Button class="ml5" v-per="'RS_M07/A03'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import RsUploader from '@/components/rs-uploader';
import Add01 from '@/mixins/add01';
export default {
  name: 's01-m05-add',
  data() {
    return {
      options: {
        max_file_size: '1mb',
      },
    };
  },
  props: {
    item: {
      Type: Object,
      default: {},
    },
  },
  mixins: [Add01],
  computed: {
    ...mapDateTable('MAIN', ['EXPTEMP', 'EXPTEMPFILENAME']),
    EXPTEMPFILE: {
      get() {
        if (this.EXPTEMP) return { id: this.EXPTEMP, name: this.EXPTEMPFILENAME };
        else return null;
      },
      set({ id, name }) {
        this.EXPTEMP = id;
        this.EXPTEMPFILENAME = name;
      },
    },
  },
  components: { RsUploader },
  methods: {
    closeW() {
      debugger;
      this.$parent.setvalue(false);
    },
    async onShow() {
      this.loading = true;
      try {
        if (this.ID) {
          await this.$callAction({ action: `${this.storeName}/open`, param: { ID: this.ID }, isBusy: false });
        } else {
          await this.$callAction({ action: `${this.storeName}/add`, param: { item: this.item }, isBusy: false });
        }
      } finally {
        this.loading = false;
      }
    },
  },
};
</script>

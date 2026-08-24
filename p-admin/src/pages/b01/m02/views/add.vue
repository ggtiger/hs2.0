<template>
  <view-dialog :title="title"  class="d-width">
    <template slot="body">
      <ToolBar label="基本信息" :size="16"></ToolBar>
      <rs-form-edit ref="form" class="maxModalH rs-flex-col" :label-width="80" mode="twocolumn" :path="$MAIN">
        <template slot="DEPTNAME">
          <AutoComplete :option="param2" v-model="TDEPT" type="object">
            <template slot="item" slot-scope="{ item }">
              <div>{{ item.value.DEPTNAME }}</div>
            </template>
          </AutoComplete>
        </template>
        <template slot="FILES">
          <RsUploader :options="options" type="files" data-type="file" v-model="FILES"></RsUploader>
        </template>
      </rs-form-edit>

    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'LIB_M02/A07'" v-if="ID" @confirm="del">
        <Button class="ml5" color="red">删除</Button>
      </Poptip>
      <Button class="ml5" v-per="'LIB_M02/A04'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Add01 from '@/mixins/add01';
import RsUploader from '@/components/rs-uploader';
export default {
  name: 'b01-m05-add',
  mixins: [Add01],
  components: { RsUploader },
  data() {
    return {
      param2: {
        loadData: this.deptSel,
        keyName: 'ID',
        titleName: 'DEPTNAME',
      },
      options: {},
    };
  },
  computed: {
    ...mapDateTable('MAIN', ['DEPTID', 'DEPTNAME', 'FILES']),
    ...mapDateTable('DEPT', []),
    ...mapDateTable('DTS', []),
    TDEPT: {
      get() {
        if (!this.DEPTID) {
          return null;
        }
        return { ID: this.DEPTID, DEPTNAME: this.DEPTNAME };
      },
      set(v) {
        v = v || {};
        this.DEPTID = v.ID;
        this.DEPTNAME = v.DEPTNAME;
      },
    },
    FILES: {
      get() {
        let dts = [...this.DTS];
        dts.map((d) => {
          d.id = d.FILEID;
          d.name = d.FILENAME;
        });
        return dts;
      },
      set(files) {
        files = files || [];
        this.$store.commit(`${Constants.STORE_NAME}/SETFILEDATA`, { files });
      },
    },
  },
  methods: {
    async deptSel(INPUT, callback) {
      if (this.DEPTNAME === INPUT) {
        INPUT = '';
      }
      await this.$callAction({ action: `${Constants.STORE_NAME}/deptSel`,
        param: {
          INPUT,
        },
        isBusy: false });
      callback(this.DEPT);
    },
  },
};
</script>

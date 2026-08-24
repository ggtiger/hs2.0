<template>
  <view-dialog :title="title"  class="d-width">
    <template slot="body">
      <ToolBar label="基本信息" :size="16"></ToolBar>
      <rs-form-edit ref="form" class="maxModalH rs-flex-col" :label-width="80" mode="twocolumn" :path="$MAIN">
        <template slot="PROVINCENAME">
          <AutoComplete :option="provinceParam" v-model="TREG1" type="object">
            <template slot="item" slot-scope="{ item }">
              <div>{{ item.value.REGION_NAME }}</div>
            </template>
          </AutoComplete>
        </template>
        <template slot="CITYNAME">
          <AutoComplete :option="cityParam" v-model="TREG2" type="object">
            <template slot="item" slot-scope="{ item }">
              <div>{{ item.value.REGION_NAME }}</div>
            </template>
          </AutoComplete>
        </template>
        <template slot="COUNTYNAME">
          <AutoComplete :option="countyParam" v-model="TREG3" type="object">
            <template slot="item" slot-scope="{ item }">
              <div>{{ item.value.REGION_NAME }}</div>
            </template>
          </AutoComplete>
        </template>
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
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Add01 from '@/mixins/add01';
import Sel01 from '@/mixins/sel01';
import RsUploader from '@/components/rs-uploader';
export default {
  name: 'b01-m05-add',
  mixins: [Add01, Sel01],
  components: { RsUploader },
  data() {
    return {};
  },
  computed: {
    ...mapDateTable('MAIN', ['PROVINCEID', 'CITYID', 'COUNTYID', 'PROVINCENAME', 'CITYNAME', 'COUNTYNAME']),
    ...mapDateTable('REG', []),
    TREG1: {
      get() {
        if (!this.PROVINCEID) {
          return null;
        }
        return { REGION_CODE: this.PROVINCEID, REGION_NAME: this.PROVINCENAME };
      },
      set(v) {
        v = v || {};
        this.PROVINCEID = v.REGION_CODE;
        this.PROVINCENAME = v.REGION_NAME;
        this.CITYID = '';
        this.CITYNAME = '';
        this.COUNTYID = '';
        this.COUNTYNAME = '';
      },
    },
    TREG2: {
      get() {
        if (!this.CITYID) {
          return null;
        }
        return { REGION_CODE: this.CITYID, REGION_NAME: this.CITYNAME };
      },
      set(v) {
        v = v || {};
        this.CITYID = v.REGION_CODE;
        this.CITYNAME = v.REGION_NAME;
        this.COUNTYID = '';
        this.COUNTYNAME = '';
      },
    },
    TREG3: {
      get() {
        if (!this.COUNTYID) {
          return null;
        }
        return { REGION_CODE: this.COUNTYID, REGION_NAME: this.COUNTYNAME };
      },
      set(v) {
        v = v || {};
        this.COUNTYID = v.REGION_CODE;
        this.COUNTYNAME = v.REGION_NAME;
      },
      addDts() {
        this.$store.commit(`${Constants.STORE_NAME}/ADD`, {});
      },
      removeDts(path, table) {
        if (table.currentRow == -1) {
          return;
        }
        this.$store.commit(`${Constants.STORE_NAME}/DEL`, { path, item: table.currentRow });
      },
    },
  },
  methods: {},
};
</script>

<template>
  <view-dialog :title="title">
    <template slot="body">
      <rs-form-edit ref="form" :label-width="80" mode="twocolumn" :path="$MAIN">
        <template slot="EMPNAME">
          <AutoComplete :option="empParam" v-model="TEMP" type="object">
            <template slot="item" slot-scope="{item}">
              <div>{{item.value.EMPNAME}}</div>
            </template>
          </AutoComplete>
        </template>
      </rs-form-edit>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="close">取消</Button>
      <Poptip v-per="'RS_M05/A04'" content="确认删除?" @confirm="del">
        <Button class="ml5" v-if="!!ID" color="red">删除</Button>
      </Poptip>
      <Button v-per="'RS_M05/A03'" class="ml5" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Sel01 from '@/mixins/sel01';
import Add01 from '@/mixins/add01';
export default {
  name: 's01-m05-add',
  props: {},
  mixins: [Add01, Sel01],
  data() {
    return {};
  },
  computed: {
    ...mapDateTable('MAIN', ['EMPID', 'EMPNAME']),
    TEMP: {
      get() {
        if (!this.EMPID) {
          return null;
        }
        return { ID: this.EMPID, EMPNAME: this.EMPNAME };
      },
      set(v) {
        v = v || {};
        this.EMPID = v.ID;
        this.EMPNAME = v.EMPNAME;
      },
    },
  },
  mounted() {},
  methods: {},
};
</script>

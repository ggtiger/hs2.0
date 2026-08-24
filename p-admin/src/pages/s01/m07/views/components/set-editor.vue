<template>
  <view-dialog title="更多设置">
    <template slot="body">
      <div class="list-item">
        <span>类型</span>
        <div class="list-right">
          <div class="rr-flex-row">
            <div class="rr-flex-1">
              <Select v-model="attr.fieldType" :datas="fieldType"></Select>
            </div>
          </div>
        </div>
      </div>
    <div class="list-item" v-if="attr.fieldType==='text'">
      <span>行数</span>
      <div class="list-right">
        <Checkbox v-model="attr.textMore">是否多行输入框</Checkbox>
      </div>
    </div>
      <div class="list-item">
        <span>编辑</span>
        <div class="list-right">
          <Checkbox v-model="attr.readonly">禁止编辑</Checkbox>
          <Checkbox v-model="attr.isnotnull">禁止空</Checkbox>
        </div>
      </div>
    <div class="list-item">
       <span>数值范围</span>
       <div class="list-right">
           <input type="text" v-model="attr.minv" style="width:30%" />
           ——
           <input type="text" v-model="attr.maxv" style="width:30%" />
       </div>
    </div>

      <div class="list-item" v-if="attr.fieldType==='checkbox'">
        <span>数据</span>
        <div class="list-right">
          <input type="text" v-model="attr.data" style="width:100%" />
        </div>
      </div>
      <div class="list-item" v-if="false">
        <span>空显示</span>
        <div class="list-right">
          <input type="text" v-model="attr.placeholder" style="width:100%" />
        </div>
      </div>
      <style-attr v-model="attr"></style-attr>
      <div class="list-item">
        <span>字段名</span>
        <div class="list-right">
          <input type="text" disabled v-model="attr.field" style="width:100%" />
        </div>
      </div>
      <div class="list-item">
        <span>默认值</span>
        <div class="list-right">
          <input type="text" v-model="attr.dvalue" style="width:100%" />
        </div>
      </div>
      <div class="list-item">
        <span>量值说明</span>
        <div class="list-right">
          <textarea v-model="attr.name" style="width:100%"></textarea>
        </div>
      </div>
      <div class="list-item">
        <span>帮助说明</span>
        <div class="list-right">
          <textarea v-model="attr.helpInfo" style="width:100%"></textarea>
        </div>
      </div>
      <div class="list-item">
        <span>计算公式</span>
        <div class="list-right">
          <textarea v-model="attr.formula" style="width:100%"></textarea>
        </div>
      </div>
    </template>
    <template slot="footer">
      <Button class="ml5" color="primary" @click.native="ok">确定</Button>
    </template>
  </view-dialog>
</template>

<script>
import styleAttr from './style-attr.vue';
export default {
  name: 'setEditor',
  props: {
    value: {},
  },
  components: {
    styleAttr,
  },
  data() {
    return {
      attr: this.value,
      fieldType: [
        { title: '输入框', key: 'text' },
        { title: '选择', key: 'select' },
        { title: '日期', key: 'date' },
        { title: '复选框', key: 'checkbox' },
      ],
    };
  },
  watch: {},
  computed: {},
  methods: {
    ok() {
      this.$emit('input', this.attr);
      this.$emit('close');
    },
  },
};
</script>
<style lang="less" scoped>
.list-item {
  margin-bottom: 5px;
  line-height: 33px;
  overflow: auto;
  & > span {
    float: left;
  }
  .list-right {
    margin-left: 4em;
    input,
    select {
      text-align: center !important;
    }
  }
}
.rs-checks {
  position: relative;
  padding: 5px 5px 5px 0;
  display: inline-block;
  .rs-delCheck {
    background: red;
    color: #fff;
    padding: 2px;
    border-radius: 100%;
    position: absolute;
    top: 0;
    right: 0;
    cursor: pointer;
  }
}
</style>

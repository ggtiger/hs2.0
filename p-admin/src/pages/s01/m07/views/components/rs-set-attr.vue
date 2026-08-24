<template>
  <!-- 属性 -->
  <div class="h-panel-body">
    <div class="list-item">
      <span>name</span>
      <div class="list-right">
        <input type="text" v-model="attr.refname" style="width:100%" />
      </div>
    </div>
    <div class="component-tip" v-if="attr.type==='itemLayout'">列数占比之和不能超过24</div>
    <div class="list-item" v-if="attr.type==='itemLayout'">
      <span>列数</span>
      <div class="list-right">
        <input type="number" v-model="attr.cell" style="width:100%" />
      </div>
    </div>
    <div class="list-item" v-if="attr.type==='itemLayout'">
      <span>占比</span>
      <div class="list-right">
        <Row>
          <Cell v-for="(n,index) in Object.keys(attr.cols||{})" :key="n" :width="24/attr.cell">
            <div>
              <input
                type="number"
                v-model="attr.cols[index]"
                @input="setUpdateCol()"
                style="width:100%"
              />
            </div>
          </Cell>
        </Row>
      </div>
    </div>
    <div class="list-item" v-if="attr.type==='itemTable'">
      <span>数据源</span>
      <div class="list-right">
        <Select v-model="attr.sourceName" class="rr-text-center" dict="模版表格数据源"></Select>
      </div>
    </div>
    <div class="list-item" v-if="attr.type==='itemEditor'">
      <div style="margin-bottom:5px">
        <Button color="primary" size="s" @click.native="openExcelEditor">Excel编辑</Button>
      </div>
      <textarea v-model="SFIELDS" style="width:100%"></textarea>
      <table style="width:100%">
        <tr>
          <td style="width:50px">字段名</td>
          <td>说明</td>
          <td style="width:40px">操作</td>
        </tr>
        <tr v-for=" (f,index) in attr.fields" :key="index">
          <td>
            <input type="text" :title="f.field" style="width:100%" readonly v-model="f.field" />
          </td>
          <td>
            <input type="text" :title="f.name" style="width:30%" v-model="f.name" />
            <input
              type="text"
              :title="f.width"
              style="width:30%"
              placeholder="宽度"
              v-model="f.width"
            />
            <input
              type="text"
              :title="f.formula"
              style="width:30%"
              placeholder="公式"
              v-model="f.formula"
            />
          </td>
          <td>
            <a style="margin-top:-5px" color="primary" @click="setEditor(f)">设置</a>
          </td>
        </tr>
      </table>
      <rs-modal ref="showEditor">
        <setEditor
          class="rr-flex-1 rr-scroll-bar"
          style="padding:0 5px;"
          v-model="editorFields"
          autoWidth
          @close="closeEditor"
        ></setEditor>
      </rs-modal>
      <excel-editor ref="excelEditor" @save="onExcelSave" />
    </div>

    <div class="list-item" v-if="attr.type==='itemField'">
      <span>总宽度</span>
      <div class="list-right">
        <input type="text" v-model="attr.width" style="width:100%" />
      </div>
    </div>
    <div class="list-item" v-if="attr.type==='itemField'">
      <span>总高度</span>
      <div class="list-right">
        <input type="text" v-model="attr.height" style="width:100%" />
      </div>
    </div>
    <div v-if="attr.type==='itemField'" class="padding-b10 title">
      <span class="h-icon-menu"></span> 标签样式
    </div>
    <div class="list-item" v-if="attr.type==='itemLabel'">
      <span>标签</span>
      <div class="list-right">
        <textarea v-model="attr.label" v-autosize rows="1" style="width:100%"></textarea>
      </div>
    </div>
    <div class="list-item" v-if="attr.type==='itemField'">
      <span>标签</span>
      <div class="list-right">
        <textarea v-model="attr.labelProps.label" v-autosize rows="1" style="width:100%"></textarea>
      </div>
    </div>
    <div class="list-item" v-if="attr.type==='itemCheckBox'">
      <span>类型</span>
      <div class="list-right">
        <Checkbox v-model="attr.fieldType" trueValue="checkBox" falseValue="Radio">是否多选</Checkbox>
      </div>
    </div>
    <div class="list-item" v-if="attr.type==='itemCheckBox'">
      <span>数据</span>
      <div class="list-right">
        <div v-for="(item,index) in attr.datas" :key="index">
          <div class="rs-checks" v-width="142">
            <input type="text" v-model="item.title" style="width:133px" />
            <span color="red" class="rs-delCheck h-icon-minus" @click.prevent="delChecks(index)"></span>
          </div>
          <Button
            v-if="index+1===attr.datas.length"
            color="primary"
            icon="h-icon-plus"
            size="s"
            @click.native="addCheck()"
          ></Button>
        </div>
        <Button
          v-if="attr.datas.length===0"
          color="primary"
          icon="h-icon-plus"
          size="s"
          @click.native="addCheck()"
        ></Button>
      </div>
    </div>
    <style-attr
      v-model="attr"
      v-if="attr.type==='itemLabel'||attr.type==='itemLayout'||attr.type==='itemCheckBox'"
    ></style-attr>
    <style-attr v-model="attr.labelProps" v-if="attr.type==='itemField'"></style-attr>
    <div v-if="attr.type==='itemField'" class="padding-t10 padding-b10 title">
      <span class="h-icon-menu"></span> 输入框样式
    </div>
    <div class="list-item" v-if="attr.type==='itemField'">
      <span>类型</span>
      <div class="list-right">
        <div class="rr-flex-row">
          <div class="rr-flex-1">
            <Select v-model="attr.fieldType" :datas="fieldType"></Select>
          </div>
          <!-- <h-switch v-model="attr.textMore" v-if="attr.fieldType==='text'">
            <label slot="open">单行</label>
            <label slot="close">多行</label>
          </h-switch>-->
          <!-- <Checkbox v-model="attr.textMore">多行输入框</Checkbox> -->
        </div>
      </div>
    </div>
    <div class="list-item" v-if="attr.type==='itemField'&&attr.fieldType==='text'">
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
    <div class="list-item" v-if="attr.type==='itemField'">
      <span>数据</span>
      <div class="list-right">
        <input type="text" v-model="attr.path" style="width:100%" />
      </div>
    </div>
    <div class="list-item" v-if="attr.type==='itemField'">
      <span>空显示</span>
      <div class="list-right">
        <input type="text" v-model="attr.placeholder" style="width:100%" />
      </div>
    </div>
    <div class="list-item" v-if="attr.fieldType==='date'">
      <span>格式</span>
      <div class="list-right">
        <input type="text" v-model="attr.format" style="width:100%" />
      </div>
    </div>
    <style-attr v-model="attr.fieldProps" v-if="attr.type==='itemField'"></style-attr>
    <div
      class="list-item"
      v-if="attr.type==='itemLabel'||attr.type==='itemField'||attr.type==='itemCheckBox'"
    >
      <span>字段名</span>
      <div class="list-right">
        <input type="text" v-model="attr.field" style="width:100%" />
      </div>
    </div>
    <div class="list-item" v-if="attr.type==='itemLabel'||attr.type==='itemField'">
      <span>公共字段</span>
      <div class="list-right">
        <Select v-model="attr.field" class="rr-text-center" :dict="cfields"></Select>
      </div>
    </div>
    <div class="list-item">
        <span>默认值</span>
        <div class="list-right">
          <input type="text" v-model="attr.dvalue" style="width:100%" />
        </div>
      </div>
    <div
      class="list-item"
      v-if="attr.type==='itemLabel'||attr.type==='itemField'||attr.type==='itemCheckBox'"
    >
      <span>说明</span>
      <div class="list-right">
        <textarea v-model="attr.content" style="width:100%"></textarea>
      </div>
    </div>
  </div>
</template>

<script>
import styleAttr from './style-attr.vue';
import setEditor from './set-editor.vue';
import excelEditor from './excel-editor.vue';
export default {
  name: 'setAttr',
  components: {
    styleAttr,
    setEditor,
    excelEditor,
  },
  directives: {},
  filters: {
    setCols(value, cell) {
      var datas = [];
      var cellWidth = parseInt(24 / cell);
      for (var i = 0; i < cell; i++) {
        datas.push(cellWidth);
      }
      this.attr.cols = datas;
      return datas;
    },
  },
  mixins: [],
  props: {
    value: {},
    tpmType: {
      type: String,
      default: 'LIMS',
    },
  },
  data() {
    return {
      fieldType: [
        { title: '输入框', key: 'text' },
        { title: '查询选择', key: 'autocomplete' },
        { title: '选择', key: 'select' },
        { title: '日期', key: 'date' },
        { title: '富文本', key: 'editor' },
      ],
      attr: this.value,
      Attr: {},
      editorFields: {},
    };
  },
  computed: {
    cfields() {
      if (this.tpmType === 'LIMS') {
        return 'LIMS模板通用字段';
      }
      if (this.tpmType === 'D001') {
        return '日常模版通用字段';
      }
    },
    SFIELDS: {
      get() {
        return JSON.stringify(this.attr.fields);
      },
      set(v) {
        this.attr.fields = JSON.parse(v);
      },
    },
  },
  watch: {
    value: {
      handler(val) {
        if (val) {
          this.attr = val;
        }
      },
      immediate: true,
      deep: true,
    },
    attr: {
      handler(val) {
        if (val) {
          if (val.type === 'itemLayout') {
            if (val.cell) {
              val.cell = parseInt(val.cell);
            }
            let cols = val.cols || {};
            if (Object.keys(cols).length !== val.cell) {
              let v = parseInt(24 / val.cell, 10);
              let tc = {};
              for (let i = 0; i < val.cell; i++) {
                tc[i] = v;
              }
              this.attr.cols = tc;
            }
          }
          if (val.type === 'itemEditor') {
            let fields = val.fields || [];
            let d = val.value;
            let patt = /\$\{[^\}]+\}/g;
            let arr = d.match(patt);
            let tfields = [];
            if (arr) {
              arr.map(a => {
                let ta = a.substring(2, a.length - 1);
                let ttf = fields.find(f => f.field == ta) || { field: ta, name: '', value: '', width: '100%' };
                ttf.value = '';
                tfields.push(ttf);
              });
            }
            if (JSON.stringify(fields) !== JSON.stringify(tfields)) {
              val.fields = tfields;
            }
          }
        }
        this.$emit('input', val);
      },
      immediate: true,
      deep: true,
    },
  },
  created() {},
  mounted() {
    this.$nextTick(function() {});
  },
  methods: {
    delChecks(index) {
      this.attr.datas.splice(index, 1);
    },
    addCheck() {
      var i = this.attr.datas.length;
      this.attr.datas.push({ title: '选择', key: i });
    },
    setUpdateCol() {
      if (this.attr.type === 'itemLayout') {
        let size = this.attr.size;
        this.attr.size = '12px';
        this.attr.size = size;
      }
    },
    setEditor(f) {
      this.editorFields = f;
      this.$refs.showEditor.show();
    },
    closeEditor() {
      this.$refs.showEditor.hide();
    },
    openExcelEditor() {
      this.$refs.excelEditor.open({
        value: this.attr.value,
        fields: this.attr.fields,
      });
    },
    onExcelSave(result) {
      this.attr.value = result.value;
      this.attr.fields = result.fields;
    },
  },
};
</script>

<style lang="less" scoped>
@import '~@/theme/index.less';
.title {
  font-size: 14px;
  color: @primary-color;
}
.padding-t10 {
  padding-top: 10px;
}
.padding-b10 {
  padding-bottom: 10px;
}
.component-tip {
  border-left: none;
  border-radius: 5px;
  padding: 5px 20px 5px 28px;
  background-color: #f8f8f8;
  position: relative;
  font-size: 12px;
  margin-bottom: 10px;
  &:before {
    content: '!';
    background-color: #f66;
    position: absolute;
    top: 4px;
    left: 4px;
    color: #fff;
    width: 20px;
    height: 20px;
    border-radius: 100%;
    text-align: center;
    line-height: 20px;
    font-weight: 700;
  }
}
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

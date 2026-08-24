<style lang="less">
</style>
<template>
  <div class="ueditor" v-if="inLayout">
    <div class="toolbar" ref="toolbar" v-show="false"></div>
    <div class="text" ref="text"></div>
  </div>
  <div class="ueditor" v-else v-html="editValue"></div>
</template>
<script>
import WangEditor from 'wangeditor';
// import filterWord from '@/utils/html';
export default {
  name: 'itemEditor',
  props: {
    value: {
      type: String,
      default: '',
    },
    type: {
      type: String,
      default: 'html', // html, text
    },
    cache: {
      type: Boolean,
      default: false, // 是否开启本地存储
    },
    inLayout: {
      type: Boolean,
      default: true,
    },
    fields: {
      type: Array,
    },
  },
  data() {
    return {
      stashValue: this.value,
      editValue: '',
    };
  },
  methods: {
    setHtml(val) {
      this.editor.txt.html(val);
    },
    initEditor() {
      this.editor = new WangEditor(this.$refs.toolbar, this.$refs.text);
      // 开启图片复制
      this.editor.customConfig.uploadImgShowBase64 = true;
      this.editor.customConfig.menus = [];
      this.editor.customConfig.onchange = html => {
        let UE = window.UE;
        html = UE.htmlparser(UE.filterWord(html), true).toHtml();
        let text = this.editor.txt.text();
        if (this.cache) localStorage.editorCache = html;
        let value = (this.stashValue = this.type === 'html' ? html : html);
        this.$emit('input', value);
        this.$emit('change', html, text);

        document.querySelectorAll('.ueditor .text table').forEach(n => {
          n.style.width = '100%';
        });
        document.querySelectorAll('.ueditor .text table td').forEach(n => {
          n.style.wordWrap = 'break-word';
          n.style.wordBreak = 'break-all';
        });
        document.querySelectorAll('.ueditor .text table td img').forEach(n => {
          n.style.width = '120px';
        });
      };
      this.editor.create();
      let html = this.value || localStorage.editorCache;
      if (html) {
        this.editor.txt.html(html);
      } else {
        // this.editor.txt.html('<div style="padding: 5px 0; color: #ccc">请粘贴word表格</div>');
      }
    },
  },
  watch: {
    value: {
      handler() {
        if (this.inLayout) {
          if (this.editor && this.value !== this.stashValue) {
            if (this.value == null) {
              // this.editor.txt.html('<div style="padding: 5px 0; color: #ccc">请粘贴word表格</div>');
            } else {
              this.editor.txt.html(this.value);
            }
          }
        } else {
          let tvalue = this.value;
          if (this.fields) {
            this.fields.forEach(p => {
              tvalue = tvalue.replace(
                '${' + p.field + '}',
                '<input type="text"  style="width:' +
                  (p.width ? p.width : '100%') +
                  ';height:100%;" name="' +
                  p.field +
                  '"/>'
              );
            });
          }
          this.editValue = tvalue;
          this.$nextTick(() => {
            document.querySelectorAll('.ueditor table td').forEach(n => {
              n.style.paddingLeft = '0';
              n.style.paddingRight = '0';
            });
            document.querySelectorAll('.ueditor table td p').forEach(n => {
              n.style.marginLeft = '0';
              n.style.marginRight = '0';
            });
            document.querySelectorAll('.ueditor table td input').forEach(n => {
              n.blur = () => {
                alert(n.name);
              };
            });
          });
        }
      },
      immediate: true,
    },
  },
  mounted() {
    if (this.inLayout) this.initEditor();
  },
};
</script>
<style lang="less" scoped>
/deep/ table {
  border-spacing: 0;
}
.toolbar {
  border: 1px solid #ccc;
}
.text {
  border: 0px solid #ccc;
  height: auto;
}

.ueditor {
  margin-top: 10px;
  margin-bottom: 10px;
}

/deep/.w-e-text {
  overflow-y: auto;
  overflow-x: hidden;
  padding: 0;
}
</style>

<style lang="less">
</style>
<template>
  <div class="ueditor" ref="editor" v-if="inLayout">
    <vue-ueditor-wrap v-if="isShowUEditor" v-model="editValue" :config="myConfig" @ready="ready"></vue-ueditor-wrap>
    <div class="toolbar" ref="toolbar" v-show="true"></div>
    <div class="text" @dblclick="click" v-show="!isShowUEditor" ref="text"></div>
  </div>
  <div class="ueditor" ref="editor" v-html="editValue" v-else></div>
</template>
<script>
import WangEditor from 'wangeditor';
// import filterWord from '@/utils/html';
import VueUeditorWrap from 'vue-ueditor-wrap'; // ES6 Module
import db from '@/api/db';
export default {
  name: 'itemEditor',
  components: { VueUeditorWrap },
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
  computed: {
    isShowUEditor() {
      if (this.editValue && this.editValue.indexOf('</table>') !== -1) {
        return false;
      } else {
        return true;
      }
    },
  },
  data() {
    return {
      stashValue: this.value,
      editValue: this.value,
      myConfig: {
        // 编辑器不自动被内容撑高
        autoHeightEnabled: true,
        // 初始容器高度
        initialFrameHeight: '100%',
        // 初始容器宽度
        initialFrameWidth: '100%',
        // 上传文件接口（这个地址是我为了方便各位体验文件上传功能搭建的临时接口，请勿在生产环境使用！！！）
        // serverUrl: 'http://35.201.165.105:8000/controller.php',
        // UEditor 资源文件的存放路径，如果你使用的是 vue-cli 生成的项目，通常不需要设置该选项，vue-ueditor-wrap 会自动处理常见的情况，如果需要特殊配置，参考下方的常见问题2
        UEDITOR_HOME_URL: '/static/UEditor/',
        toolbars: [
          ['fullscreen', 'source', 'undo', 'redo', 'bold']
        ],

        enableAutoSave: false,
        elementPathEnabled: false,
        wordCount: false,
      },
      tfields: [],
      fmFields: {},
    };
  },
  methods: {
    async loadFormulas() {
      try {
        let ret = await db.postData({
          api: '/api/data/call/RS_M11/A01/',
          params: {
            FilterParams: { INPUT: '' },
            PageSize: 1,
            PageIndex: 1
          }
        });
        console.log('loadFormulas', ret);
        if (ret && ret.Items && ret.Items.length > 0) {
          ret.Items.forEach(item => {
            if (item.FORMULACODE && item.FORMULATEXT) {
              let vars = [];
              try {
                vars = JSON.parse(item.FORMULAVARS || '[]');
              } catch (e) {
                let patt = /\$\{[^\}]+\}/g;
                let matches = item.FORMULATEXT.match(patt);
                if (matches) {
                  vars = [...new Set(matches.map(m => m.substring(2, m.length - 1)))];
                }
              }
              let body = item.FORMULATEXT;
              vars.forEach(v => {
                body = body.replaceAll('\\${' + v + '}', '${' + v + '}');
              });
              let params = vars.join(', ');
              try {
                // eslint-disable-next-line
                eval(`window.${item.FORMULACODE} = (${params}) => { return eval(\`${body}\`) }`);
              } catch (e) {
                console.warn('公式注册失败: ' + item.FORMULACODE, e);
              }
            }
          });
        }
      } catch (e) {
        console.warn('加载公式列表失败', e);
      }
    },
    setHtml(val) {
      this.editor.txt.html(val);
    },

    ready(editor) {
      editor.addListener('contentchange', (type, v) => {
        let html = editor.getContent();
        this.$emit('input', html);
        this.$emit('change', html);
        this.$el.querySelectorAll('.ueditor .text table').forEach(n => {
          n.style.width = '100%';
        });
        this.$el.querySelectorAll('.ueditor .text table td').forEach(n => {
          n.style.wordWrap = 'break-word';
          n.style.wordBreak = 'break-all';
        });
        this.$el.querySelectorAll('.ueditor .text table td img').forEach(n => {
          n.style.width = '120px';
        });
      });
    },
    initEditor() {
      this.editor = new WangEditor(this.$refs.toolbar, this.$refs.text);

      // 开启图片复制
      this.editor.customConfig.uploadImgShowBase64 = true;
      this.editor.customConfig.mode = 'default';
      this.editor.customConfig.onchange = html => {
        let text = this.editor.txt.text();
        if (this.cache) localStorage.editorCache = html;
        let value = (this.stashValue = this.type === 'html' ? html : html);
        this.$emit('input', value);
        this.$emit('change', html, text);
        this.$el.querySelectorAll('.ueditor .text table').forEach(n => {
          n.style.width = '100%';
        });
        this.$el.querySelectorAll('.ueditor table sup span').forEach(n => {
          n.style.fontSize = '8px';
        });
        this.$el.querySelectorAll('.ueditor table sub span').forEach(n => {
          n.style.fontSize = '8px';
        });
        this.$el.querySelectorAll('.ueditor .text table td').forEach(n => {
          n.style.wordWrap = 'break-word';
          n.style.wordBreak = 'break-all';
        });
        this.$el.querySelectorAll('.ueditor .text table td img').forEach(n => {
          if (!n.style.width) {
            n.style.width = n.width / 1.5 + 'px';
          }
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
    contentchange(v) {
      alert(v);
    },
    click(e) {
      let field = e.target.innerText.trim();
      let patt = /\$\{[^\}]+\}/g;
      let ff = field.match(patt);
      if (ff.length === 1) {
        let tff = ff[0].substring(2, ff[0].length - 1);
        let node = this.$parent.$parent.$parent.$parent.$el.querySelectorAll('input[title="' + tff + '"]');
        this.$ScrollIntoView(node[0], {
          time: 500,
          align: {
            top: 0,
            topOffset: 0,
          },
        });
      }
    },
    openExcelEditor() {
      this.$emit('excel-edit');
    },
  },
  watch: {
    value: {
      handler() {
        if (this.inLayout) {
          this.editValue = this.value;
          if (this.editor && this.value !== this.stashValue) {
            if (this.value == null) {
              // this.editor.txt.html('<div style="padding: 5px 0; color: #ccc">请粘贴word表格</div>');
            } else {
              this.editor.txt.html(this.value);
            }
          }
        } else {
          let tvalue = this.value;
          let fmFields = JSON.parse(sessionStorage.getItem('hlims_ueditor_fmFields') || '{}');
          if (this.fields) {
            this.fields.forEach(p => {
              if (p.formula) {
                let patt = /\$\{[^\}]+\}/g;
                let ff = p.formula.match(patt);
                if (ff) {
                  ff.map(a => {
                    let ta = a.substring(2, a.length - 1);
                    let ppp = fmFields[ta] || [];
                    if (
                      !ppp.find(fff => {
                        return fff.field === p.field;
                      })
                    ) {
                      ppp.push(p);
                    }

                    fmFields[ta] = ppp;
                  });
                }
              }
              let v = p.value;
              if (!v) {
                p.value = p.dvalue || '';
                v = p.value;
              }
              if (p.fieldType === 'checkbox') {
                let tdata = p.data;
                let dstr =
                  '<div  style="width:' +
                  (p.width ? p.width : '100%') +
                  ';height:100%;border: 0px;text-align:center;">';
                tdata.split('|').map(d => {
                  dstr += `<label><input type='checkbox' ${v === d ? 'checked' : ''} name='${p.field}'/><label class="v">${d}</label></label>`;
                });
                dstr += '</div>';
                tvalue = tvalue.replace('${' + p.field + '}', dstr);
              } else {
                var _getColor = (p, v) => {
                  if (p.isnotnull && !v) {
                    return 'red';
                  } else if (v) {
                    // console.log('abc', p);
                    if (p.minv && v * 1 < p.minv * 1) {
                      return 'red';
                    }
                    if (p.maxv && v * 1 > p.maxv * 1) {
                      return 'red';
                    }
                  }
                  return '';
                };
                if (p.textMore) {
                  debugger;
                }
                tvalue = tvalue.replace(
                  '${' + p.field + '}',
                  '<' + (p.textMore ? 'textarea' : 'input') + ' type="text"' +
                  (p.readonly ? ' disabled ' : '') +
                    'title="' +
                    p.name +
                    '" value="' +
                    v +
                    '"  style="width:' +
                    (p.width ? p.width : '100%') +
                    ';height:' + (p.height ? p.height : '100%') + ';background-color:' + _getColor(p, v) + ';" name="' +
                    p.field +
                    (p.textMore ? '">' + v + '</textarea>' : '"/>')
                );
              }
            });
            sessionStorage.setItem('hlims_ueditor_fmFields', JSON.stringify(fmFields));
          }
          this.editValue = tvalue;
          this.$nextTick(() => {
            this.$el.querySelectorAll('.ueditor table td').forEach(n => {
              n.style.paddingLeft = '0';
              n.style.paddingRight = '0';
            });
            this.$el.querySelectorAll('.ueditor table td p').forEach(n => {
              n.style.marginLeft = '0';
              n.style.marginRight = '0';
            });

            this.$el.querySelectorAll('.ueditor table td input,.ueditor table td textarea').forEach(n => {
              n.parentNode.ondblclick = () => {
                let f1 = this.fields.find(f => {
                  return f.field === n.name;
                });
                if (f1) {
                  console.log('this.fields', this.fields);
                  this.$Notice({ type: 'info', timeout: 5000, title: f1.field + ':' + f1.name, content: f1.helpInfo || '无帮助说明' });
                }
              };
              if (n.type === 'checkbox') {
                n.onclick = () => {
                  if (n.checked) {
                    let f1 = this.fields.find(f => {
                      return f.field === n.name;
                    });
                    if (f1) {
                      f1.value = n.parentNode.querySelectorAll('.v')[0].innerHTML;
                    }
                    n.parentNode.parentNode.querySelectorAll('input,textarea').forEach(n1 => {
                      if (n1 !== n) {
                        n1.checked = false;
                      }
                    });
                  }
                };
              }
              var _this = this;

              let setColor = (p, v) => {
                var _color = '';
                if (p.isnotnull && !v) {
                  _color = 'red';
                } else if (v) {
                  if (p.minv && v * 1 < p.minv * 1) {
                    _color = 'red';
                  }
                  if (p.maxv && v * 1 > p.maxv * 1) {
                    _color = 'red';
                  }
                }
                _this.$el.parentNode.querySelector('.ueditor table td input[name="' + p.field + '"],.ueditor table td textarea[name="' + p.field + '"]').style.backgroundColor = _color;
              };

              let setFormlaValue = f1 => {
                if (f1) {
                  this.fmFields = JSON.parse(sessionStorage.getItem('hlims_ueditor_fmFields') || '{}');
                  let fmField = this.fmFields[f1.field];
                  if (!fmField) {
                    return;
                  }
                  fmField.map(f => {
                    let tf = this.fields.find(f111 => {
                      return f111.field === f.field;
                    });
                    if (!tf) {
                      this.$parent.$children.find(cc => {
                        if (!cc.fields) return false;
                        tf = cc.fields.find(tf1 => tf1.field === f.field);
                        return cc.fields && tf;
                      });
                    }
                    f = tf || f;
                    let fm = f.formula;
                    if (fm) {
                      let patt = /\$\{[^\}]+\}/g;
                      let ff = fm.match(patt);
                      if (ff) {
                        ff.map(a => {
                          let ta = a.substring(2, a.length - 1);
                          let f11 = this.fields.find(f => {
                            return f.field === ta;
                          });
                          if (!f11) {
                            this.$parent.$children.find(cc => {
                              if (!cc.fields) return false;
                              f11 = cc.fields.find(tf1 => tf1.field === ta);
                              return cc.fields && f11;
                            });
                          }
                          if (f11) {
                            fm = fm.replaceAll('\\${' + ta + '}', (f11.value || '0') * 1);
                          } else {
                            fm = fm.replaceAll('\\${' + ta + '}', '0');
                          }
                        });
                      }
                      console.log('执行公式：' + fm);
                      try {
                        // eslint-disable-next-line
                        f.value = eval(fm);
                      } catch (e) {
                        this.$Notice('字段：' + f.field + ' 的公式：' + f.formula + '错误！');
                      }
                      if (this.fmFields[f.field]) {
                        console.log('执行问题：', this.fmFields[f.field]);
                        if (f1.field !== f.field) {
                          setFormlaValue(f);
                        }
                      }
                      this.$el.parentNode.querySelector('.ueditor table td input[name="' + f.field + '"],.ueditor table td textarea[name="' + f.field + '"]').value =
                        f.value;
                      setColor(f, f.value);
                    }
                  });
                }
              };

              n.onblur = () => {
                debugger;
                if (n.type === 'checkbox') {
                  return;
                }
                let f1 = this.fields.find(f => {
                  return f.field === n.name;
                });
                if (f1) {
                  f1.value = n.value;
                  setFormlaValue(f1);
                  setColor(f1, f1.value);
                }
                console.log('老虎在测试', f1);
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
  created() {
    this.loadFormulas();
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
  margin-top: 0px;
  margin-bottom: 0px;
  /deep/ table {
    border-top: 1px solid #333;
    border-left: 1px solid #333;
  }
  /deep/ table td,
  table th {
    border-bottom: 1px solid #333;
    border-right: 1px solid #333;
  }
  /deep/ p {
    margin: 2px 0;
  }
}

/deep/.w-e-text {
  overflow-y: auto;
  overflow-x: hidden;
  padding: 0;
}
/deep/.w-e-text p {
  margin: 2px 0;
}
/deep/ .w-e-text table {
  border-top: 1px solid #333;
  border-left: 1px solid #333;
}
.excel-edit-btn {
  display: inline-block;
  padding: 4px 10px;
  margin-bottom: 2px;
  background: #4b9efd;
  color: #fff;
  border-radius: 3px;
  cursor: pointer;
  font-size: 12px;
  vertical-align: middle;
}
.excel-edit-btn:hover {
  background: #3a8eec;
}
</style>

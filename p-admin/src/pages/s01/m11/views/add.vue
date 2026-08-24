<template>
  <view-dialog :title="title" >
    <template slot="body">
      <ToolBar label="基本信息" :size="16"></ToolBar>
      <rs-form-edit
        ref="form"
        class="maxModalH rs-flex-col"
        :label-width="80"
        mode="twocolumn"
        :path="$MAIN"
      ></rs-form-edit>
      <ToolBar label="函数预览" :size="16"></ToolBar>
      <div class="formulaTestArea">
        <div class="funcPreview" v-if="funcPreview">
          <pre class="funcCode">{{ funcPreview }}</pre>
        </div>
        <div class="formulaVarsEmpty" v-else>
          <span class="emptyTip">请填写"公式编码"和"公式内容"，系统将自动生成函数</span>
        </div>
        <div class="callExample" v-if="funcPreview">
          <span class="callLabel">调用方式：</span>
          <code class="callCode">{{ callExample }}</code>
        </div>
      </div>
      <ToolBar label="公式试算" :size="16"></ToolBar>
      <div class="formulaTestArea">
        <div class="formulaVars" v-if="formulaVars.length > 0">
          <Row>
            <Cell v-for="(v, index) in formulaVars" :key="index" :width="24">
              <div class="varItem">
                <span class="varLabel">{{ v }}</span>
                <input
                  type="number"
                  v-model.number="varValues[v]"
                  class="varInput"
                  placeholder="输入数值"
                />
              </div>
            </Cell>
          </Row>
        </div>
        <div class="formulaVarsEmpty" v-else>
          <span class="emptyTip">请在"公式内容"中输入公式，系统将自动提取变量（格式：${变量名}）</span>
        </div>
        <div class="formulaActions">
          <Button color="primary" @click.native="testFormula" v-per="'RS_M11/A06'">试算</Button>
        </div>
        <div class="formulaResult" v-if="testResult !== null">
          <div class="resultLabel">计算结果：</div>
          <div :class="['resultValue', testError ? 'error' : 'success']">
            {{ testError ? testResult : testResult }}
          </div>
        </div>
      </div>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="'RS_M11/A04'" v-if="ID" @confirm="del"><Button class="ml5" color="red">删除</Button></Poptip>
      <Button class="ml5" v-per="'RS_M11/A03'" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapDateTable } from '../store';
import Add01 from '@/mixins/add01';
export default {
  name: 's01-m11-add',
  mixins: [Add01],
  data() {
    return {
      formulaVars: [],
      varValues: {},
      testResult: null,
      testError: false
    };
  },
  computed: {
    ...mapDateTable('MAIN', ['FORMULATEXT', 'ISUSE', 'FORMULAVARS', 'FORMULACODE']),
    funcPreview() {
      const code = this.FORMULACODE;
      const text = this.FORMULATEXT;
      if (!code || !text) return '';
      const vars = this.formulaVars;
      const params = vars.join(', ');
      // 将 ${变量} 转为模板字符串中的 ${参数名}
      let body = text;
      vars.forEach(v => {
        body = body.replaceAll('\\${' + v + '}', '${' + v + '}');
      });
      return `window.${code} = (${params}) => {\n  return eval(\`${body}\`)\n}`;
    },
    callExample() {
      const code = this.FORMULACODE;
      if (!code) return '';
      const vars = this.formulaVars;
      const args = vars.map(v => '${' + v + '}').join(', ');
      return `${code}(${args})`;
    }
  },
  watch: {
    FORMULATEXT: {
      handler(newVal, oldVal) {
        this.extractVars(newVal);
      },
      immediate: true
    }
  },
  methods: {
    extractVars(formulaText) {
      if (!formulaText) {
        this.formulaVars = [];
        return;
      }
      const patt = /\$\{[^\}]+\}/g;
      const matches = formulaText.match(patt);
      if (matches) {
        const vars = [...new Set(matches.map(m => m.substring(2, m.length - 1)))];
        this.formulaVars = vars;
        if (this.MAIN) {
          this.FORMULAVARS = JSON.stringify(vars);
        }
        vars.forEach(v => {
          if (this.varValues[v] === undefined) {
            this.$set(this.varValues, v, 0);
          }
        });
      } else {
        this.formulaVars = [];
        if (this.MAIN) {
          this.FORMULAVARS = '[]';
        }
      }
    },
    testFormula() {
      const code = this.FORMULACODE;
      const formula = this.FORMULATEXT;
      if (!code) {
        this.$Notice('请输入公式编码');
        return;
      }
      if (!formula) {
        this.$Notice('请输入公式内容');
        return;
      }
      try {
        // 构建函数: window.公式编码 = (参数) => { return eval(`公式内容`) }
        let body = formula;
        this.formulaVars.forEach(v => {
          body = body.replaceAll('\\${' + v + '}', '${' + v + '}');
        });
        const params = this.formulaVars.join(', ');
        // eslint-disable-next-line
        eval(`window.${code} = (${params}) => { return eval(\`${body}\`) }`);
        // 调用函数，传入变量值
        const args = this.formulaVars.map(v => this.varValues[v] || 0);
        // eslint-disable-next-line
        const result = window[code](...args);
        this.testResult = result;
        this.testError = false;
      } catch (e) {
        this.testResult = '公式错误: ' + e.message;
        this.testError = true;
      }
    }
  }
};
</script>
<style lang="less" scoped>
.formulaTestArea {
  padding: 10px;
  background: #f8f8f8;
  border-radius: 4px;
  margin: 0 10px 10px 10px;
}
.funcPreview {
  background: #1e1e1e;
  border-radius: 4px;
  padding: 10px;
  margin-bottom: 10px;
}
.funcCode {
  color: #d4d4d4;
  font-family: Consolas, Monaco, monospace;
  font-size: 13px;
  margin: 0;
  white-space: pre-wrap;
  word-break: break-all;
}
.callExample {
  padding: 8px 10px;
  background: #fff;
  border-radius: 4px;
  font-size: 13px;
}
.callLabel {
  color: #666;
}
.callCode {
  color: #3c8dbc;
  font-family: Consolas, Monaco, monospace;
}
.formulaVars {
  margin-bottom: 10px;
}
.varItem {
  display: flex;
  align-items: center;
  margin-bottom: 5px;
  padding: 5px 10px;
  background: #fff;
  border-radius: 4px;
}
.varLabel {
  width: 100px;
  font-weight: bold;
  color: #333;
}
.varInput {
  flex: 1;
  height: 28px;
  border: 1px solid #ddd;
  border-radius: 4px;
  padding: 0 8px;
  text-align: center;
}
.formulaVarsEmpty {
  padding: 20px;
  text-align: center;
}
.emptyTip {
  color: #999;
  font-size: 12px;
}
.formulaActions {
  margin-bottom: 10px;
}
.formulaResult {
  padding: 10px;
  background: #fff;
  border-radius: 4px;
}
.resultLabel {
  font-weight: bold;
  margin-bottom: 5px;
}
.resultValue {
  font-size: 18px;
  font-weight: bold;
  &.success {
    color: #3c8dbc;
  }
  &.error {
    color: #dd4b39;
  }
}
</style>

<template>
  <div class="asst-rich" :class="{ dark: theme === 'dark' }">
    <template v-for="(seg, i) in segments">
      <div
        v-if="seg.type === 'text'"
        class="asst-text"
        v-html="renderMd(seg.content)"
        :key="'t' + i"
      ></div>
      <div v-else-if="seg.type === 'echarts-error'" class="asst-chart-err" :key="'e' + i">
        ⚠️ 图表 JSON 解析失败：{{ seg.error }}
      </div>
      <chart-block v-else-if="seg.type === 'echarts'" :option="seg.option" :key="'c' + i"></chart-block>
      <html-block v-else-if="seg.type === 'html'" :html="seg.content" :key="'h' + i"></html-block>
      <code-block v-else-if="seg.type === 'code'" :code="seg.code" :language="seg.lang" :applyable="applyable" :key="'cb' + i" @apply="$emit('apply-code', $event)"></code-block>
      <metadata-sql-block
        v-else-if="seg.type === 'metadata-sql'"
        :code="seg.code"
        :key="'ms' + i"
        ref="msqlBlocks"
        @confirm="onSqlConfirm"
      ></metadata-sql-block>
    </template>
  </div>
</template>

<script>
import marked from 'marked';
import DOMPurify from 'dompurify';
import ChartBlock from './ChartBlock.vue';
import HtmlBlock from './HtmlBlock.vue';
import CodeBlock from './CodeBlock.vue';
import MetadataSqlBlock from './MetadataSqlBlock.vue';

// 代码语言别名归一化
function normalizeLang(lang) {
  var l = (lang || '').toLowerCase();
  if (l === 'javascript') return 'js';
  return l;
}

// 把文本切成段：
//   ```echarts ... ``` -> echarts 段（JSON option）
//   ```html ... ```     -> html 段
//   ```vue/js/javascript/css/less ... ``` -> code 段
//   ```metadata-sql ... ``` -> metadata-sql 段
// 其余为 markdown 文本段
function parseSegments(text) {
  var segments = [];
  if (!text) return segments;
  var re = /```(echarts|html|vue|js|javascript|css|less|csharp|sql|metadata-sql)[ \t]*\n([\s\S]*?)```/g;
  var last = 0;
  var m;
  while ((m = re.exec(text)) !== null) {
    if (m.index > last) segments.push({ type: 'text', content: text.slice(last, m.index) });
    var lang = m[1];
    var code = m[2];
    if (lang === 'echarts') {
      try {
        segments.push({ type: 'echarts', option: JSON.parse(code.trim()) });
      } catch (e) {
        segments.push({ type: 'echarts-error', error: e.message });
      }
    } else if (lang === 'html') {
      segments.push({ type: 'html', content: code });
    } else if (lang === 'metadata-sql') {
      segments.push({ type: 'metadata-sql', code: code });
    } else {
      segments.push({ type: 'code', lang: normalizeLang(lang), code: code });
    }
    last = re.lastIndex;
  }
  if (last < text.length) {
    var rest = text.slice(last);
    // 流式中未闭合的代码块（```lang 后还无闭合 ```）：当代码段，避免整块当 text 显示成大字
    var openM = rest.match(/```(echarts|html|vue|js|javascript|css|less|csharp|sql|metadata-sql)[ \t]*\n([\s\S]*)$/);
    if (openM) {
      var olang = openM[1];
      var ocode = openM[2];
      if (olang === 'echarts') {
        try { segments.push({ type: 'echarts', option: JSON.parse(ocode.trim()) }) } catch (e) { segments.push({ type: 'code', lang: normalizeLang(olang), code: ocode }) }
      } else if (olang === 'html') {
        segments.push({ type: 'html', content: ocode });
      } else if (olang === 'metadata-sql') {
        segments.push({ type: 'metadata-sql', code: ocode });
      } else {
        segments.push({ type: 'code', lang: normalizeLang(olang), code: ocode });
      }
    } else {
      segments.push({ type: 'text', content: rest });
    }
  }
  return segments;
}

export default {
  name: 'RichTextBlock',
  components: { ChartBlock, HtmlBlock, CodeBlock, MetadataSqlBlock },
  props: {
    text: { type: String, default: '' },
    applyable: { type: Boolean, default: false },
    theme: { type: String, default: 'light' }
  },
  computed: {
    segments() {
      return parseSegments(this.text);
    }
  },
  methods: {
    renderMd(content) {
      return DOMPurify.sanitize(marked.parse(content, { breaks: true }));
    },
    onSqlConfirm(code) {
      // 透传给父组件执行 SQL 后回传结果
      this.$emit('confirm-sql', code);
    },
    // 供父组件按 code 定位 MetadataSqlBlock 实例并更新执行状态
    // 返回 true 表示找到匹配并已更新
    setSqlStatus(code, status, result) {
      var arr = this.$refs.msqlBlocks;
      if (!arr) return false;
      if (!Array.isArray(arr)) arr = [arr];
      for (var i = 0; i < arr.length; i++) {
        if (arr[i] && arr[i].code === code) {
          arr[i].setStatus(status, result);
          return true;
        }
      }
      return false;
    }
  }
};
</script>

<style scoped>
/* 深色主题（SFC 代码生成面板）：文字浅色，行内代码/pre/表格适配 */
.asst-rich.dark .asst-text {
  color: #d4d4d4;
}
.asst-rich.dark .asst-text >>> code {
  background: #2d2d2d;
  color: #ce9178;
  padding: 1px 5px;
  border-radius: 3px;
  font-size: 0.9em;
}
.asst-rich.dark .asst-text >>> pre {
  background: #1e1e1e;
  border: 1px solid #3c3c3c;
  border-radius: 4px;
  padding: 8px 12px;
}
.asst-rich.dark .asst-text >>> pre code {
  background: transparent;
  color: #d4d4d4;
  padding: 0;
}
.asst-rich.dark .asst-text >>> a {
  color: #5dade2;
}
.asst-rich.dark .asst-text >>> th,
.asst-rich.dark .asst-text >>> td {
  border-color: #3c3c3c;
}
.asst-text {
  font-size: 14px;
  line-height: 1.6;
  word-break: break-word;
}
.asst-text >>> h1 { font-size: 18px; margin: 8px 0 4px; }
.asst-text >>> h2 { font-size: 16px; margin: 6px 0 3px; }
.asst-text >>> h3 { font-size: 15px; margin: 6px 0 3px; }
.asst-text >>> p { margin: 4px 0; }
.asst-text >>> ul, .asst-text >>> ol { margin: 4px 0; padding-left: 20px; }
.asst-text >>> li { margin: 2px 0; }
.asst-text >>> code {
  background: #f0f0f0;
  color: #c7254e;
  padding: 1px 5px;
  border-radius: 3px;
  font-size: 13px;
  font-family: 'Courier New', Consolas, Monaco, monospace;
}
.asst-text >>> pre {
  background: #f6f8fa;
  padding: 8px 12px;
  border-radius: 4px;
  overflow-x: auto;
  margin: 6px 0;
}
.asst-text >>> pre code {
  background: transparent;
  color: #333;
  padding: 0;
}
.asst-text >>> table {
  border-collapse: collapse;
  margin: 6px 0;
}
.asst-text >>> th,
.asst-text >>> td {
  border: 1px solid #ddd;
  padding: 4px 8px;
}
.asst-chart-err {
  color: #ed4014;
  font-size: 12px;
  background: #fef0f0;
  padding: 4px 8px;
  border-radius: 4px;
  margin: 4px 0;
}
</style>

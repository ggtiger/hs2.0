/**
 * SFC 编译器 — 保存时编译 + 运行时编译核心
 *
 * 编译流程:
 * 1. parseComponent — 解析 SFC 为 template/script/styles 块
 * 2. compileTemplate — template 编译为 render 函数
 * 3. compileScript — Babel 将 ES6 import/export 转 CJS (require → __sfc_require__)
 * 4. compileStyles — Less 编译 + scoped 标记
 * 5. extractDeps — 静态分析 import 路径
 * 6. 组装 COMPILEDCODE — 可执行函数体字符串
 */

import Vue from 'vue';
import * as compiler from 'vue-template-compiler';

/**
 * 生成 scopeId (用于 scoped style)
 */
function genScopeId(modulePath) {
  var hash = 0;
  for (var i = 0; i < modulePath.length; i++) {
    hash = ((hash << 5) - hash) + modulePath.charCodeAt(i);
    hash = hash & hash;
  }
  return 'd-' + Math.abs(hash).toString(36);
}

/**
 * 解析 SFC 源码为 { template, script, styles } 块
 */
export function parseSFC(source) {
  var descriptor = compiler.parseComponent(source, { pad: 'line' });
  return {
    template: descriptor.template,
    script: descriptor.script,
    styles: descriptor.styles || [],
  };
}

/**
 * 提取所有 import 路径 (用于 DEPS)
 */
export function extractDeps(scriptContent) {
  if (!scriptContent) return [];
  var deps = [];
  // 匹配: import xxx from 'path' / import { xxx } from 'path' / import * as ns from 'path' / import 'path'
  var importRegex = /import\s+(?:[\w*$\s{},]+\s+from\s+)?['"]([^'"]+)['"]/g;
  var match;
  while ((match = importRegex.exec(scriptContent)) !== null) {
    deps.push(match[1]);
  }
  return deps;
}

/**
 * 编译 template 块为 render + staticRenderFns
 * @returns { render: String, staticRenderFns: [String] }
 */
export function compileTemplate(templateContent) {
  if (!templateContent) {
    return { render: null, staticRenderFns: [] };
  }
  var result = compiler.compile(templateContent);
  if (result.errors && result.errors.length > 0) {
    throw new Error('模板编译错误: ' + result.errors.join('; '));
  }
  return {
    render: result.render,
    staticRenderFns: result.staticRenderFns || [],
  };
}

/**
 * 编译 script 块: ES6 import/export → CJS (require)
 * 使用 @babel/standalone
 * @param {String} scriptContent - script 块内容
 * @param {String} modulePath - 模块路径 (用于错误提示)
 * @returns {String} 编译后的 CJS 代码
 */
export function compileScript(scriptContent, modulePath) {
  if (!scriptContent) return '';

  // 动态引入 @babel/standalone (体积大, 仅在编译时加载)
  var Babel = require('@babel/standalone');

  var transformed = Babel.transform(scriptContent, {
    presets: [
      ['env', {
        targets: { browsers: ['> 1%', 'last 2 versions', 'not ie <= 8'] },
        modules: 'commonjs',
      }],
    ],
    sourceType: 'module',
    filename: modulePath || 'sfc-module.js',
  });

  // 去除 "use strict" 指令: vue-template-compiler 生成的 render 函数使用 with(this) 语法,
  // 在 strict mode 下会报 SyntaxError, 需要移除 strict 声明
  var code = transformed.code.replace(/^["']use strict["'];?\s*\n?/m, '');
  return code;
}

/**
 * 编译 style 块 (Less → CSS + scoped 标记)
 * @param {Array} styles - parseComponent 返回的 styles 数组
 * @param {String} scopeId - scoped 标记 ID
 * @returns {Promise<Array<{css, scopeId, scoped}>>}
 */
export function compileStyles(styles, scopeId) {
  if (!styles || styles.length === 0) {
    return Promise.resolve([]);
  }

  return Promise.all(styles.map(function(styleBlock) {
    var content = styleBlock.content || '';
    var scoped = styleBlock.scoped || false;
    var lang = styleBlock.lang || 'css';

    var compilePromise;
    if (lang === 'less' || lang === 'scss') {
      // 动态引入 less
      var less = require('less');
      compilePromise = new Promise(function(resolve, reject) {
        less.render(content, function(err, output) {
          if (err) {
            reject(new Error('样式编译错误: ' + err.message));
          } else {
            resolve(output.css);
          }
        });
      });
    } else {
      compilePromise = Promise.resolve(content);
    }

    return compilePromise.then(function(css) {
      if (scoped) {
        css = addScopedAttr(css, scopeId);
      }
      return { css: css, scopeId: scopeId, scoped: scoped };
    });
  }));
}

/**
 * 为 CSS 选择器添加 [data-v-xxx] 属性
 */
function addScopedAttr(css, scopeId) {
  var attr = '[' + scopeId + ']';
  // 简单实现: 为每个选择器末尾添加属性选择器
  // 跳过 @media / @keyframes 等规则
  return css.replace(/([^{}]+)\{/g, function(match, selectors) {
    // 跳过 @ 规则
    if (selectors.trim().startsWith('@')) {
      return match;
    }
    var parts = selectors.split(',').map(function(sel) {
      var trimmed = sel.trim();
      // 在最后一个元素前插入属性
      if (trimmed.indexOf(':') > -1) {
        // 伪类: .foo:hover → .foo[data-v-xxx]:hover
        return trimmed.replace(/(:[^:]+)$/, attr + '$1');
      }
      return trimmed + attr;
    });
    return parts.join(', ') + ' {';
  });
}

/**
 * 完整编译 (保存时和预览时使用)
 * @param {String} sourceCode - SFC 原始源码
 * @param {String} modulePath - 模块路径
 * @param {String} fileType - 文件类型 'VUE' | 'JS' (默认自动检测)
 * @returns {Promise<{compiledCode, deps, render, staticRenderFns, styles}>}
 */
export async function compileSFC(sourceCode, modulePath, fileType) {
  var scopeId = genScopeId(modulePath || 'anonymous');

  // 自动检测文件类型: 如果源码不含 <template> 或 <script> 标签, 当作纯 JS 处理
  var isSFC = fileType !== 'JS' && /<\/?template>|<\/?script>|<\/?style/.test(sourceCode);

  if (!isSFC) {
    // 纯 JS 文件: 只编译 script, 不编译 template/style
    var scriptCompiled = compileScript(sourceCode, modulePath);
    var deps = extractDeps(sourceCode);
    var compiledCode = assembleCompiledCode(scriptCompiled, { render: null, staticRenderFns: [] }, [], scopeId);
    return {
      compiledCode: compiledCode,
      deps: deps,
      render: null,
      staticRenderFns: [],
      styles: [],
    };
  }

  // SFC 文件: 完整编译
  var parsed = parseSFC(sourceCode);

  // 1. 编译 template
  var templateResult = compileTemplate(parsed.template ? parsed.template.content : '');

  // 2. 编译 script (ES6 → CJS)
  var scriptCompiledSFC = '';
  if (parsed.script) {
    scriptCompiledSFC = compileScript(parsed.script.content, modulePath);
  }

  // 3. 提取依赖
  var depsSFC = extractDeps(parsed.script ? parsed.script.content : '');

  // 4. 编译 styles
  var styles = await compileStyles(parsed.styles, scopeId);

  // 5. 组装 COMPILEDCODE
  var compiledCodeSFC = assembleCompiledCode(scriptCompiledSFC, templateResult, styles, scopeId);

  return {
    compiledCode: compiledCodeSFC,
    deps: depsSFC,
    render: templateResult.render,
    staticRenderFns: templateResult.staticRenderFns,
    styles: styles,
  };
}

/**
 * 组装编译后的代码字符串
 *
 * 生成格式:
 * (function(module, exports, require, Vue) {
 *   // Babel 编译后的 script (import 已转为 require)
 *   ...
 *   // render 函数注入
 *   module.exports.render = function() { ... };
 *   module.exports.staticRenderFns = [ ... ];
 *   // scoped style 注入
 *   module.exports._sfcStyles = [ ... ];
 * })
 */
function assembleCompiledCode(scriptCompiled, templateResult, styles, scopeId) {
  var parts = [];

  // script 编译后的代码
  if (scriptCompiled) {
    parts.push(scriptCompiled);
  }

  // 如果没有 export default, 创建空对象
  parts.push(
    'if (!module.exports || typeof module.exports !== "object") { module.exports = {}; }'
  );

  // Babel 将 export default {} 转为 module.exports.default = {}
  // 后续 render/beforeMount 注入需要直接挂在组件 options 上, 故提升 default 为 module.exports
  parts.push(
    'if (module.exports.default && typeof module.exports.default === "object") { module.exports = module.exports.default; }'
  );

  // render 函数注入: 存储为字符串, 运行时用间接 eval 转换
  // (不能内联为代码, 因为 with(this) 在 strict mode 下会报 SyntaxError)
  if (templateResult.render) {
    parts.push('module.exports._renderStr = ' + JSON.stringify(templateResult.render) + ';');
  }
  if (templateResult.staticRenderFns && templateResult.staticRenderFns.length > 0) {
    parts.push('module.exports._staticRenderFnsStr = ' + JSON.stringify(templateResult.staticRenderFns) + ';');
  }

  // scoped style 注入
  if (styles && styles.length > 0) {
    var stylesJson = JSON.stringify(styles);
    parts.push('module.exports._sfcStyles = ' + stylesJson + ';');
  }

  // beforeMount + mounted 钩子: 注入 style
  // beforeMount: 注入 CSS 到 DOM (不依赖 this.$el)
  // mounted: 设置 scoped 属性到组件元素 (需要 this.$el)
  if (styles && styles.length > 0) {
    parts.push(
      'var _origBeforeMount = module.exports.beforeMount;' +
      'module.exports.beforeMount = function() {' +
      '  if (_origBeforeMount) _origBeforeMount.call(this);' +
      '  var _styles = module.exports._sfcStyles || [];' +
      '  _styles.forEach(function(s) {' +
      '    if (!s._styleEl) {' +
      '      s._styleEl = document.createElement("style");' +
      '      s._styleEl.textContent = s.css;' +
      '      document.head.appendChild(s._styleEl);' +
      '    }' +
      '  });' +
      '};' +
      'var _origMounted = module.exports.mounted;' +
      'module.exports.mounted = function() {' +
      '  if (_origMounted) _origMounted.call(this);' +
      '  var _self = this;' +
      '  var _styles = module.exports._sfcStyles || [];' +
      '  _styles.forEach(function(s) {' +
      '    if (s.scoped) {' +
      '      var _el = _self.$el;' +
      '      if (_el && _el.setAttribute) { _el.setAttribute("' + scopeId + '", ""); }' +
      '      if (_el && _el.querySelectorAll) {' +
      '        _el.querySelectorAll("*").forEach(function(child) { child.setAttribute("' + scopeId + '", ""); });' +
      '      }' +
      '    }' +
      '  });' +
      '};'
    );
  }

  // 包裹为自执行函数
  return '(function(module, exports, require, Vue) {\n' + parts.join('\n') + '\n})';
}

/**
 * 执行编译后的代码, 返回 Vue 组件 options
 * @param {String} compiledCode - 编译后的函数体字符串
 * @param {Function} requireFn - 自定义 require 函数 (__sfc_require__)
 * @returns {Object} Vue 组件 options (module.exports)
 */
export function executeCompiled(compiledCode, requireFn) {
  var moduleObj = { exports: {} };

  // 使用间接 eval (0, eval)() 在全局非严格作用域执行 compiledCode
  // webpack ES 模块默认严格模式 + new Function 会继承严格性, 导致 with(this) 报 SyntaxError
  // 间接 eval 在全局作用域执行, 不继承调用方严格模式, 可正确解析 with
  // eslint-disable-next-line no-eval
  var factory = (0, eval)('(' + compiledCode + ')');
  factory(moduleObj, moduleObj.exports, requireFn, Vue);

  var exports = moduleObj.exports;

  // Babel 将 export default {} 转为 exports.default = {}
  // Vue 需要的是 default 属性指向的组件 options 对象
  if (exports && typeof exports === 'object' && exports.default && typeof exports.default === 'object') {
    exports = exports.default;
  }

  // 将 render 函数字符串转换为真实函数 (间接 eval 解析 with(this) 语法)
  // _renderStr 是函数体 "with(this){...}", 需包成 function(){...} 才能用 eval 转换
  if (exports._renderStr) {
    // eslint-disable-next-line no-eval
    exports.render = (0, eval)('(function(){' + exports._renderStr + '})');
    delete exports._renderStr;
  }
  if (exports._staticRenderFnsStr && exports._staticRenderFnsStr.length > 0) {
    exports.staticRenderFns = exports._staticRenderFnsStr.map(function(s) {
      // eslint-disable-next-line no-eval
      return (0, eval)('(function(){' + s + '})');
    });
    delete exports._staticRenderFnsStr;
  }

  return exports;
}

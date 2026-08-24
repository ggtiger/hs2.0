// https://eslint.org/docs/user-guide/configuring

module.exports = {
  root: true,
  parserOptions: {
    parser: 'babel-eslint',
  },
  env: {
    browser: true,
  },
  extends: [
    // https://github.com/vuejs/eslint-plugin-vue#priority-a-essential-error-prevention
    // consider switching to `plugin:vue/strongly-recommended` or `plugin:vue/recommended` for stricter rules.
    'plugin:vue/essential',
    // https://github.com/standard/standard/blob/master/docs/RULES-en.md
    'standard',
  ],
  // required to lint *.vue files
  plugins: ['vue'],
  // add your custom rules here
  rules: {
    'no-console': process.env.NODE_ENV === 'production' ? 'off' : 'off',
    // allow debugger during development
    'no-debugger': process.env.NODE_ENV === 'production' ? 'error' : 'off',
    // allow paren-less arrow functions
    'arrow-parens': 0,
    // allow async-await
    'generator-star-spacing': 0,
    // allow extend-native
    'no-extend-native': 0,
    // 要求在语句末尾使用分号
    semi: [1, 'always', { omitLastInOneLineBlock: true }],
    // 要求或禁止函数圆括号之前有一个空格
    'space-before-function-paren': [1, 'never'],
    // 要求或禁止块内填充
    'padded-blocks': [0, { blocks: 'always' }],
    // 要求或禁止使用拖尾逗号
    'comma-dangle': [0, 'always-multiline'],
    // 强制使用一致的缩进
    indent: [1, 2, { SwitchCase: 1 }],
    // 禁止使用 空格 和 tab 混合缩进
    'no-mixed-spaces-and-tabs': 2,
    // 强制操作符使用一致的换行符风格（操作符后面换行）
    'operator-linebreak': [2, 'after'],
    // 强制js字符串使用单引号
    quotes: [2, 'single'],
    // 允许在正则表达式中出现控制字符
    'no-control-regex': 0,
    'no-useless-escape': 0,

    // === 前端 Store 数据流强制规范（详见 docs/frontend-store-convention.md）===
    // 先全量 warn，跑 `npm run lint` 评估影响面，后续逐批迁移到合规后再升为 error。
    // 注意：ESLint 4.19 的 no-restricted-imports 不支持 message 字段，详细文案见规范文档。
    'no-restricted-imports': ['warn', {
      paths: ['@/api/db'],
      patterns: ['@/api/db*'],
    }],
    'no-restricted-syntax': ['warn', {
      // 命中 this.$store.dispatch(...)（this/vm/任意前缀 都捕获）
      selector: "CallExpression[callee.property.name='dispatch'][callee.object.property.name='$store']",
      message: '禁止 this.$store.dispatch；请用 this.$callAction({action, param, ...})',
    }],
  },
  overrides: [
    {
      // 白名单：Store 框架 / 网络层 / $callAction 实现等允许直接 import db 与 $store.dispatch
      files: [
        'src/store/**/*.js',
        'src/api/**/*.js',
        'src/utils/extends.js',
        'src/sfc-loader/**/*.js',
        'src/pages/**/store.js',
        'src/pages/**/store-*.js',
        'src/pages/**/baseStore.js',
        'src/pages/**/code-asset.js',
        'src/components/generic-module/generic-store.js',
        'src/components/generic-module/code-test-store.js',
        'src/components/**/*-store.js',
        'src/modules/**/*.js',
      ],
      rules: {
        'no-restricted-imports': 'off',
        // 框架内部允许 $store.dispatch（Store03 BaseStore createStore）
        // 这里不放开 no-restricted-syntax，因为业务 .vue 仍要禁止
      },
    },
    {
      // 专用 endpoint 组件（文件上传 / OnlyOffice / UEditor / Word 模板等）
      files: [
        'src/components/rs-uploader/**/*.vue',
        'src/components/rs-uploader-template/**/*.vue',
        'src/components/rs-onlyoffice-preview/**/*.vue',
        'src/components/edit/ueditor/**/*.vue',
        'src/components/rs-word-template-editor/**/*.vue',
      ],
      rules: {
        'no-restricted-imports': 'off',
      },
    },
  ],
};

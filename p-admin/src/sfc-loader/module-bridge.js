/**
 * 桥梁模块 — webpack 打包模块暴露到 window.__SFC_MODULES__
 *
 * 此文件由 webpack 正常打包, 在应用启动时执行,
 * 将核心基础设施暴露到 window.__SFC_MODULES__, 供在线 SFC 的 __sfc_require__ 调用
 */

import Vue from 'vue';
import HeyUI from 'heyui';
import Vuex from 'vuex';
import axios from 'axios';
import db from '@/api/db';
import Store from '@/store';
import createStore from '@/store/createStore';
import Store03 from '@/store/Store03';
import BaseStore from '@/store/BaseStore';
import Add01 from '@/mixins/add01';
import { getGenericStore } from '@/components/generic-module/generic-store';
import rsVcoreDate, { dateToString } from 'rs-vcore/utils/Date';

// 按需补充更多导出...
// 注意: 每个条目必须带 __esModule: true 标记
// Babel 的 _interopRequireDefault 会检查此标记, 否则会把对象再包一层 { default: obj }
// 导致 .default.default 嵌套, 取不到真实导出
window.__SFC_MODULES__ = Object.assign(window.__SFC_MODULES__ || {}, {
  // 类型C: 全局库
  'vue': { __esModule: true, default: Vue, Vue: Vue },
  'heyui': { __esModule: true, default: HeyUI },
  'vuex': { __esModule: true, default: Vuex },
  'axios': { __esModule: true, default: axios },

  // 类型A: 项目内部模块 (@/ 开头)
  '@/api/db': { __esModule: true, default: db },
  '@/store': { __esModule: true, default: Store },
  '@/store/createStore': { __esModule: true, default: createStore },
  '@/store/Store03': { __esModule: true, default: Store03 },
  '@/store/BaseStore': { __esModule: true, default: BaseStore },
  '@/mixins/add01': { __esModule: true, default: Add01 },
  '@/components/generic-module/generic-store': { __esModule: true, default: { getGenericStore: getGenericStore }, getGenericStore: getGenericStore },

  // rs-vcore 工具
  'rs-vcore/utils/Date': { __esModule: true, default: rsVcoreDate, dateToString: dateToString },
});

console.log('[SFC-Loader] 桥梁模块已初始化, 可用模块:', Object.keys(window.__SFC_MODULES__));

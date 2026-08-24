/**
 * AI Agent Proxy - 前端AI能力代理层
 *
 * 设计目标：
 * 1. 统一管理AI可调用的前端能力
 * 2. 支持动态注册和发现工具
 * 3. 维护执行上下文（路由、store、表单组件等）
 * 4. 标准化工具定义（供LLM function calling使用）
 * 5. 完整的错误处理和重试机制
 *
 * 工具分两类：
 * - 前端工具（executeLocally=true）：在前端执行，操作UI/store/router
 * - 后端工具（executeLocally=false）：由后端执行，前端只透传定义
 */

import store from '@/store';
import router from '@/router';
import { getSceneSync } from '@/utils/ai/sceneConfig';

// ==================== 核心类型定义 ====================

/**
 * 工具接口 - 所有AI可调用能力必须实现此接口
 */
export class ITool {
  constructor() {
    if (new.target === ITool) {
      throw new Error('ITool 是抽象类，不能直接实例化');
    }
  }

  /**
   * 获取工具定义（用于LLM function calling）
   * @returns {Object} 工具定义
   */
  getDefinition() {
    throw new Error('子类必须实现 getDefinition 方法');
  }

  /**
   * 执行工具（仅前端工具需要实现）
   * @param {Object} args - 工具参数
   * @param {ToolExecutionContext} context - 执行上下文
   * @returns {Promise<Object>} 执行结果
   */
  async execute(args, context) {
    throw new Error('子类必须实现 execute 方法');
  }

  /**
   * 是否在前端执行
   * true=前端执行（操作UI），false=后端执行（查数据库）
   */
  isFrontend() {
    return true;
  }
}

/**
 * 工具执行上下文 - 封装执行时的环境信息
 */
export class ToolExecutionContext {
  constructor(options = {}) {
    this.store = options.store || store;
    this.router = options.router || router;
    this.route = options.route || null;
    this.userInfo = options.userInfo || null;
    this.moduleCode = options.moduleCode || null;
    this.storeName = options.storeName || null;
    this.connectionId = options.connectionId || null;
    this.extra = options.extra || {};
  }

  /**
   * 获取store模块
   */
  getStoreModule() {
    if (!this.storeName) return null;
    return this.store.state[this.storeName] || null;
  }

  /**
   * 获取主表数据
   */
  getMainData() {
    const mod = this.getStoreModule();
    if (!mod || !mod.dt || !mod.dt.MAIN) return null;
    return mod.dt.MAIN.data[0] || null;
  }

  /**
   * 获取子表数据
   */
  getSubTableData(path) {
    const mod = this.getStoreModule();
    if (!mod || !mod.dt || !mod.dt[path]) return null;
    return mod.dt[path].data || [];
  }

  /**
   * 获取当前路由
   */
  getCurrentRoute() {
    return this.route || (this.router ? this.router.currentRoute : null);
  }

  /**
   * 获取当前表单组件
   */
  getFormEdit() {
    return this.extra.formEdit || null;
  }
}

// ==================== 工具注册表 ====================

/**
 * 工具注册表 - 单例模式，线程安全（JS单线程，无需锁）
 */
class ToolRegistry {
  constructor() {
    if (ToolRegistry.instance) {
      return ToolRegistry.instance;
    }
    this.tools = new Map();
    ToolRegistry.instance = this;
  }

  /**
   * 注册工具
   */
  register(name, tool) {
    if (!(tool instanceof ITool)) {
      throw new Error('工具必须继承 ITool 接口');
    }
    this.tools.set(name, tool);
    console.log(`[ToolRegistry] 工具已注册: ${name} (${tool.isFrontend() ? '前端' : '后端'})`);
  }

  /**
   * 获取工具
   */
  get(name) {
    return this.tools.get(name) || null;
  }

  /**
   * 获取所有工具定义（用于LLM）
   */
  getAllDefinitions() {
    const definitions = [];
    for (const [name, tool] of this.tools) {
      try {
        definitions.push(tool.getDefinition());
      } catch (e) {
        console.error(`[ToolRegistry] 获取工具 ${name} 定义失败:`, e);
      }
    }
    return definitions;
  }

  /**
   * 获取前端工具定义
   */
  getFrontendDefinitions() {
    const definitions = [];
    for (const [name, tool] of this.tools) {
      if (!tool.isFrontend()) continue;
      try {
        definitions.push(tool.getDefinition());
      } catch (e) {
        console.error(`[ToolRegistry] 获取工具 ${name} 定义失败:`, e);
      }
    }
    return definitions;
  }

  /**
   * 执行工具
   */
  async execute(name, args, context) {
    const tool = this.get(name);
    if (!tool) {
      throw new Error(`工具 ${name} 未注册`);
    }
    if (!tool.isFrontend()) {
      throw new Error(`工具 ${name} 是后端工具，不能在前端执行`);
    }
    return await tool.execute(args || {}, context);
  }

  /**
   * 清空注册表
   */
  clear() {
    this.tools.clear();
  }
}

// 导出单例
export const toolRegistry = new ToolRegistry();

// ==================== 前端工具实现 ====================

/**
 * 导航工具 - 跳转到指定模块页面
 */
export class NavigateTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'navigate',
        description: '跳转到指定模块页面。用于让用户在真实页面操作（新增/编辑/删除/审批等）',
        parameters: {
          type: 'object',
          properties: {
            path: {
              type: 'string',
              description: '模块路由路径，如 /r01/m05、/b01/m02'
            },
            query: {
              type: 'object',
              description: '查询参数（可选），如 { id: "xxx" }'
            }
          },
          required: ['path']
        }
      }
    };
  }

  async execute(args, context) {
    const { path, query } = args;
    if (!path) {
      throw new Error('导航路径不能为空');
    }
    try {
      await router.push({ path, query: query || {} });
      return { success: true, message: `已导航到 ${path}` };
    } catch (e) {
      throw new Error(`导航失败: ${e.message || e}`);
    }
  }
}

/**
 * 填表工具 - 填充当前表单字段值
 */
export class FillFormTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'fill_form',
        description: '填充当前表单的字段值。fields的key必须是大写字段名',
        parameters: {
          type: 'object',
          properties: {
            fields: {
              type: 'object',
              description: '字段值对象，如 { CUSTNAME: "ABC公司", CUSTID: "xxx" }'
            }
          },
          required: ['fields']
        }
      }
    };
  }

  async execute(args, context) {
    const { fields } = args;
    if (!fields || typeof fields !== 'object') {
      throw new Error('fields 必须是对象');
    }
    const formEdit = context.getFormEdit();
    if (!formEdit) {
      throw new Error('当前不在表单页面，无法填充');
    }
    if (typeof formEdit.applyFill !== 'function') {
      throw new Error('表单组件缺少 applyFill 方法');
    }
    formEdit.applyFill(fields);
    return {
      success: true,
      message: `已填充 ${Object.keys(fields).length} 个字段`,
      fields: Object.keys(fields)
    };
  }
}

/**
 * 子表填值工具 - 向子表添加行
 */
export class FillSubTableTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'fill_subtable',
        description: '向当前表单的子表添加行数据',
        parameters: {
          type: 'object',
          properties: {
            path: {
              type: 'string',
              description: '子表路径名，如 DTSA、DTS'
            },
            rows: {
              type: 'array',
              description: '行数据数组',
              items: { type: 'object' }
            }
          },
          required: ['path', 'rows']
        }
      }
    };
  }

  async execute(args, context) {
    const { path, rows } = args;
    if (!path) {
      throw new Error('子表路径 path 不能为空');
    }
    if (!Array.isArray(rows) || rows.length === 0) {
      throw new Error('rows 必须是非空数组');
    }
    const formEdit = context.getFormEdit();
    if (!formEdit) {
      throw new Error('当前不在表单页面，无法填充子表');
    }
    if (typeof formEdit.onSubTable !== 'function') {
      throw new Error('表单组件缺少 onSubTable 方法');
    }
    formEdit.onSubTable({ path, rows });
    return {
      success: true,
      message: `已向子表 ${path} 添加 ${rows.length} 行`
    };
  }
}

/**
 * 获取当前页面信息工具
 */
export class GetCurrentPageTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'get_current_page',
        description: '获取当前页面信息（路由路径、参数、模块代码等）',
        parameters: {
          type: 'object',
          properties: {}
        }
      }
    };
  }

  async execute(args, context) {
    const route = context.getCurrentRoute();
    if (!route) {
      return { success: true, data: { path: '/', message: '无法获取当前路由' } };
    }
    return {
      success: true,
      data: {
        path: route.path,
        name: route.name,
        params: route.params,
        query: route.query,
        moduleCode: context.moduleCode,
        storeName: context.storeName
      }
    };
  }
}

/**
 * 获取当前表单数据工具
 */
export class GetFormDataTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'get_form_data',
        description: '获取当前表单的所有数据（主表+子表），用于了解用户已填内容',
        parameters: {
          type: 'object',
          properties: {}
        }
      }
    };
  }

  async execute(args, context) {
    const formEdit = context.getFormEdit();
    if (!formEdit) {
      return { success: true, data: null, message: '当前不在表单页面' };
    }
    const store = context.store;
    const storeName = context.storeName;
    const moduleCode = context.moduleCode;
    if (!storeName || !store) {
      // 降级：直接从path获取主表数据
      const mainData = context.getMainData();
      return { success: true, data: mainData };
    }
    try {
      const result = {};
      const mod = store.state[storeName];
      if (!mod || !mod.dt) {
        return { success: true, data: context.getMainData() };
      }
      // 主表：直接取 data[0]（不走 computed，避免响应式副作用）
      const mainPath = formEdit.path && formEdit.path._path_ ? formEdit.path._path_ : 'MAIN';
      const mainDt = mod.dt[mainPath] || mod.dt.MAIN;
      if (mainDt && mainDt.data && mainDt.data[0]) {
        // 过滤内部字段（_idx_/_type_/_path_ 等）
        const mainRow = mainDt.data[0];
        Object.keys(mainRow).forEach(k => {
          if (!k.startsWith('_')) result[k] = mainRow[k];
        });
      }
      // 子表：从 MODPATHREF 递归获取所有子表完整数据
      const moduleConfig = store.state.app && store.state.app.modules ?
        store.state.app.modules[moduleCode] : null;
      if (moduleConfig && moduleConfig.MODPATHREF) {
        const visited = new Set();
        const collect = (parentPath) => {
          moduleConfig.MODPATHREF.forEach(ref => {
            if (ref.PATHNAMEA === parentPath && !visited.has(ref.PATHNAMEB)) {
              visited.add(ref.PATHNAMEB);
              const p = ref.PATHNAMEB;
              const dt = mod.dt[p];
              if (dt && dt.data && dt.data.length > 0) {
                result['__subtable_' + p] = dt.data.map(row => {
                  const cleanRow = {};
                  Object.keys(row).forEach(k => {
                    if (!k.startsWith('_')) cleanRow[k] = row[k];
                  });
                  return cleanRow;
                });
              }
              collect(p);
            }
          });
        };
        collect(mainPath);
      }
      return { success: true, data: result };
    } catch (e) {
      throw new Error(`获取表单数据失败: ${e.message || e}`);
    }
  }
}

/**
 * 获取当前用户信息工具
 */
export class GetUserInfoTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'get_user_info',
        description: '获取当前登录用户信息',
        parameters: {
          type: 'object',
          properties: {}
        }
      }
    };
  }

  async execute(args, context) {
    try {
      const userInfo = store.state.user.userInfo;
      if (!userInfo) {
        return { success: true, data: null, message: '用户未登录' };
      }
      return {
        success: true,
        data: {
          id: userInfo.ID,
          name: userInfo.NICKNAME,
          deptId: userInfo.DEPTID,
          deptName: userInfo.DEPTNAME
        }
      };
    } catch (e) {
      throw new Error(`获取用户信息失败: ${e.message || e}`);
    }
  }
}

/**
 * 获取菜单树工具
 */
export class GetMenusTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'get_menus',
        description: '获取系统菜单树（当前用户可见的菜单）',
        parameters: {
          type: 'object',
          properties: {
            keyword: {
              type: 'string',
              description: '可选，按关键词过滤'
            }
          }
        }
      }
    };
  }

  async execute(args, context) {
    try {
      let menus = store.state.app.menus || [];
      const keyword = args.keyword;
      if (keyword) {
        menus = menus.filter(m =>
          (m.FUNCNAME && m.FUNCNAME.includes(keyword)) ||
          (m.FUNCCODE && m.FUNCCODE.includes(keyword))
        );
      }
      return {
        success: true,
        data: menus.map(m => ({
          code: m.FUNCCODE,
          name: m.FUNCNAME,
          url: m.OUTERURL,
          parentId: m.UPFUNCID
        }))
      };
    } catch (e) {
      throw new Error(`获取菜单失败: ${e.message || e}`);
    }
  }
}

/**
 * 显示消息提示工具
 */
export class ShowMessageTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'show_message',
        description: '在页面上显示消息提示（success/error/info/warning）',
        parameters: {
          type: 'object',
          properties: {
            text: {
              type: 'string',
              description: '消息内容'
            },
            type: {
              type: 'string',
              enum: ['success', 'error', 'info', 'warning'],
              description: '消息类型'
            }
          },
          required: ['text']
        }
      }
    };
  }

  async execute(args, context) {
    const { text, type } = args;
    if (!text) {
      throw new Error('消息内容不能为空');
    }
    try {
      // 使用HeyUI的消息提示
      const heyui = require('heyui').default;
      const msgType = type || 'info';
      if (msgType === 'success') heyui.$Message.success(text);
      else if (msgType === 'error') heyui.$Message.error(text);
      else if (msgType === 'warning') heyui.$Message.warn(text);
      else heyui.$Message.info(text);
      return { success: true, message: '消息已显示' };
    } catch (e) {
      console.log('[ShowMessageTool]', text);
      return { success: true, message: '消息已显示' };
    }
  }
}

/**
 * 关闭当前对话框工具
 */
export class CloseDialogTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'close_dialog',
        description: '关闭当前打开的对话框/抽屉',
        parameters: {
          type: 'object',
          properties: {}
        }
      }
    };
  }

  async execute(args, context) {
    try {
      // 关闭AI面板
      const formEdit = context.getFormEdit();
      if (formEdit && typeof formEdit.closeAiPanel === 'function') {
        formEdit.closeAiPanel();
        return { success: true, message: 'AI面板已关闭' };
      }
      return { success: true, message: '无可关闭的对话框' };
    } catch (e) {
      throw new Error(`关闭对话框失败: ${e.message || e}`);
    }
  }
}

// ==================== 主表操作工具 ====================

/**
 * 打开表单记录工具 - 按ID加载记录到当前表单
 */
export class OpenFormTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'open_form',
        description: '按ID打开/加载一条记录到当前表单（编辑模式）。需先navigate到表单页面。',
        parameters: {
          type: 'object',
          properties: {
            id: {
              type: 'string',
              description: '记录ID'
            }
          },
          required: ['id']
        }
      }
    };
  }

  async execute(args, context) {
    const { id } = args;
    if (!id) throw new Error('记录ID不能为空');
    const storeName = context.storeName;
    const store = context.store;
    if (!storeName) throw new Error('无法获取当前模块store，需先打开表单页面');
    try {
      await store.dispatch(`${storeName}/open`, { ID: id });
      return { success: true, message: `已加载记录 ${id}` };
    } catch (e) {
      throw new Error(`打开记录失败: ${e.message || e}`);
    }
  }
}

/**
 * 设置主表字段工具 - 设置当前表单主表的某个字段值
 */
export class SetFormFieldTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'set_form_field',
        description: '设置当前表单主表的单个字段值。field必须是大写字段名。',
        parameters: {
          type: 'object',
          properties: {
            field: {
              type: 'string',
              description: '字段名（大写），如 CUSTNAME'
            },
            value: {
              description: '字段值（字符串/数字/布尔）'
            }
          },
          required: ['field', 'value']
        }
      }
    };
  }

  async execute(args, context) {
    const { field, value } = args;
    if (!field) throw new Error('字段名不能为空');
    const formEdit = context.getFormEdit();
    if (!formEdit) throw new Error('当前不在表单页面');
    try {
      const key = String(field).toUpperCase();
      const model = formEdit.path && formEdit.path.data && formEdit.path.data[0];
      if (!model) throw new Error('主表数据不存在');
      formEdit.$set(model, key, value);
      formEdit.path.setValue(key, value);
      return { success: true, message: `已设置 ${key}=${value}` };
    } catch (e) {
      throw new Error(`设置字段失败: ${e.message || e}`);
    }
  }
}

/**
 * 获取主表字段值工具
 */
export class GetFormFieldTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'get_form_field',
        description: '获取当前表单主表某个字段的值',
        parameters: {
          type: 'object',
          properties: {
            field: {
              type: 'string',
              description: '字段名（大写）'
            }
          },
          required: ['field']
        }
      }
    };
  }

  async execute(args, context) {
    const { field } = args;
    if (!field) throw new Error('字段名不能为空');
    const formEdit = context.getFormEdit();
    if (!formEdit) return { success: true, data: null, message: '当前不在表单页面' };
    try {
      const key = String(field).toUpperCase();
      const model = formEdit.path && formEdit.path.data && formEdit.path.data[0];
      const value = model ? model[key] : undefined;
      return { success: true, data: value, field: key };
    } catch (e) {
      throw new Error(`获取字段失败: ${e.message || e}`);
    }
  }
}

/**
 * 保存表单工具 - 触发当前表单的保存操作
 */
export class SaveFormTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'save_form',
        description: '保存当前表单（触发store的save action，含校验）。保存是真实的数据变更操作。',
        parameters: {
          type: 'object',
          properties: {}
        }
      }
    };
  }

  async execute(args, context) {
    const formEdit = context.getFormEdit();
    if (!formEdit) throw new Error('当前不在表单页面');
    const storeName = context.storeName;
    if (!storeName) throw new Error('无法获取当前模块store');
    try {
      // 先校验
      if (formEdit.$refs && formEdit.$refs.form && typeof formEdit.$refs.form.valid === 'function') {
        const validResult = formEdit.$refs.form.valid();
        if (validResult && !validResult.result) {
          return { success: false, error: '表单校验未通过', details: validResult };
        }
      }
      await context.store.dispatch(`${storeName}/save`);
      return { success: true, message: '表单已保存' };
    } catch (e) {
      throw new Error(`保存表单失败: ${e.message || e}`);
    }
  }
}

// ==================== 子表操作工具 ====================

/**
 * 新增子表行工具 - 向子表添加一行数据
 */
export class AddSubTableRowTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'add_subtable_row',
        description: '向子表新增一行数据。path是子表路径名(如DTSA)，row是该行的字段值对象。',
        parameters: {
          type: 'object',
          properties: {
            path: {
              type: 'string',
              description: '子表路径名，如 DTSA/DTSB/DTS'
            },
            row: {
              type: 'object',
              description: '行数据，key为大写字段名，如 {ITEMNAME:"万用表", QTY:1}'
            }
          },
          required: ['path', 'row']
        }
      }
    };
  }

  async execute(args, context) {
    const { path, row } = args;
    if (!path) throw new Error('子表路径path不能为空');
    if (!row || typeof row !== 'object') throw new Error('row必须是对象');
    const dt = _getSubTable(context, path);
    if (!dt) throw new Error(`子表 ${path} 不存在`);
    try {
      // 构造大写key的完整行对象，直接传给add（push时属性会被Vue响应式化，表格才会显示）
      const rowData = {};
      Object.keys(row).forEach(k => {
        rowData[String(k).toUpperCase()] = row[k];
      });
      dt.add(rowData);
      // 走Vuex mutation强制刷新（和addDts走ADD能刷新同理）
      const storeName = context.storeName;
      if (storeName) {
        context.store.commit(`${storeName}/REFRESH_SUBTABLE`, { path });
      } else {
        _refreshSubTable(dt, context.getFormEdit());
      }
      return { success: true, message: `已向子表 ${path} 新增1行`, rowCount: dt.data.length };
    } catch (e) {
      throw new Error(`新增子表行失败: ${e.message || e}`);
    }
  }
}

/**
 * 删除子表行工具 - 按索引删除子表行
 */
export class DeleteSubTableRowTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'delete_subtable_row',
        description: '删除子表的指定行（按索引）。index从0开始。不传index则删除最后一行。',
        parameters: {
          type: 'object',
          properties: {
            path: {
              type: 'string',
              description: '子表路径名'
            },
            index: {
              type: 'number',
              description: '行索引(从0开始)，不传则删除最后一行'
            }
          },
          required: ['path']
        }
      }
    };
  }

  async execute(args, context) {
    const { path, index } = args;
    if (!path) throw new Error('子表路径path不能为空');
    const dt = _getSubTable(context, path);
    if (!dt) throw new Error(`子表 ${path} 不存在`);
    if (!dt.data || dt.data.length === 0) {
      return { success: true, message: `子表 ${path} 无数据`, rowCount: 0 };
    }
    try {
      const idx = (typeof index === 'number') ? index : dt.data.length - 1;
      if (idx < 0 || idx >= dt.data.length) {
        throw new Error(`索引 ${idx} 超出范围(0-${dt.data.length - 1})`);
      }
      dt.del(idx);
      return { success: true, message: `已删除子表 ${path} 第${idx + 1}行`, rowCount: dt.data.length };
    } catch (e) {
      throw new Error(`删除子表行失败: ${e.message || e}`);
    }
  }
}

/**
 * 修改子表行工具 - 修改子表指定行的字段值
 */
export class UpdateSubTableRowTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'update_subtable_row',
        description: '修改子表指定行的字段值。index从0开始。',
        parameters: {
          type: 'object',
          properties: {
            path: {
              type: 'string',
              description: '子表路径名'
            },
            index: {
              type: 'number',
              description: '行索引(从0开始)'
            },
            row: {
              type: 'object',
              description: '要修改的字段值，key为大写字段名'
            }
          },
          required: ['path', 'index', 'row']
        }
      }
    };
  }

  async execute(args, context) {
    const { path, index, row } = args;
    if (!path) throw new Error('子表路径path不能为空');
    if (typeof index !== 'number') throw new Error('index必须是数字');
    if (!row || typeof row !== 'object') throw new Error('row必须是对象');
    const dt = _getSubTable(context, path);
    if (!dt) throw new Error(`子表 ${path} 不存在`);
    if (!dt.data || index < 0 || index >= dt.data.length) {
      throw new Error(`索引 ${index} 超出范围(0-${dt.data ? dt.data.length - 1 : -1})`);
    }
    try {
      // 转大写key
      const item = {};
      Object.keys(row).forEach(k => {
        item[String(k).toUpperCase()] = row[k];
      });
      // 优先用rs-table-edit的onApplyEdit（组件内$set，正确触发响应式）
      const formEdit = context.getFormEdit();
      const tableComp = _findTableComp(formEdit, path);
      console.log('[update_subtable_row] tableComp=', tableComp ? '找到' : '未找到', 'path=', path);
      if (tableComp && typeof tableComp.onApplyEdit === 'function') {
        // tableComp.onApplyEdit();
        tableComp.onApplyEdit.apply(this, [{ item, index }]);
        console.log('[update_subtable_row] 调用onApplyEdit成功');
      } else {
        // 降级：直接dt.setValue + mutation刷新
        const targetRow = dt.data[index];
        Object.keys(item).forEach(key => {
          dt.setValue(key, item[key], targetRow);
        });
        dt.update(targetRow);
        const storeName = context.storeName;
        if (storeName) context.store.commit(`${storeName}/REFRESH_SUBTABLE`, { path });
      }
      return { success: true, message: `已修改子表 ${path} 第${index + 1}行` };
    } catch (e) {
      throw new Error(`修改子表行失败: ${e.message || e}`);
    }
  }
}

/**
 * 清空子表工具 - 删除子表所有行
 */
export class ClearSubTableTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'clear_subtable',
        description: '清空子表的所有行数据',
        parameters: {
          type: 'object',
          properties: {
            path: {
              type: 'string',
              description: '子表路径名'
            }
          },
          required: ['path']
        }
      }
    };
  }

  async execute(args, context) {
    const { path } = args;
    if (!path) throw new Error('子表路径path不能为空');
    const dt = _getSubTable(context, path);
    if (!dt) throw new Error(`子表 ${path} 不存在`);
    try {
      dt.clear();
      return { success: true, message: `已清空子表 ${path}`, rowCount: 0 };
    } catch (e) {
      throw new Error(`清空子表失败: ${e.message || e}`);
    }
  }
}

/**
 * 获取子表数据工具 - 获取子表所有行数据
 */
export class GetSubTableDataTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'get_subtable_data',
        description: '获取子表的所有行数据（过滤内部字段）',
        parameters: {
          type: 'object',
          properties: {
            path: {
              type: 'string',
              description: '子表路径名'
            }
          },
          required: ['path']
        }
      }
    };
  }

  async execute(args, context) {
    const { path } = args;
    if (!path) throw new Error('子表路径path不能为空');
    const dt = _getSubTable(context, path);
    if (!dt) throw new Error(`子表 ${path} 不存在`);
    try {
      const data = (dt.data || []).map(row => {
        const cleanRow = {};
        Object.keys(row).forEach(k => {
          if (!k.startsWith('_')) cleanRow[k] = row[k];
        });
        return cleanRow;
      });
      return { success: true, data, rowCount: data.length };
    } catch (e) {
      throw new Error(`获取子表数据失败: ${e.message || e}`);
    }
  }
}

/**
 * 获取所有子表信息工具 - 列出当前表单的所有子表路径
 */
export class ListSubTablesTool extends ITool {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'list_subtables',
        description: '列出当前表单的所有子表路径名和行数',
        parameters: {
          type: 'object',
          properties: {}
        }
      }
    };
  }

  async execute(args, context) {
    const moduleCode = context.moduleCode;
    const storeName = context.storeName;
    const store = context.store;
    if (!moduleCode || !storeName) {
      return { success: true, data: [], message: '无法获取模块配置' };
    }
    try {
      const moduleConfig = store.state.app && store.state.app.modules ?
        store.state.app.modules[moduleCode] : null;
      const mod = store.state[storeName];
      if (!moduleConfig || !moduleConfig.MODPATHREF || !mod || !mod.dt) {
        return { success: true, data: [] };
      }
      const result = [];
      const visited = new Set();
      const collect = (parentPath) => {
        moduleConfig.MODPATHREF.forEach(ref => {
          if (ref.PATHNAMEA === parentPath && !visited.has(ref.PATHNAMEB)) {
            visited.add(ref.PATHNAMEB);
            const p = ref.PATHNAMEB;
            const dt = mod.dt[p];
            result.push({
              path: p,
              parentPath: parentPath,
              rowCount: dt && dt.data ? dt.data.length : 0
            });
            collect(p);
          }
        });
      };
      collect('MAIN');
      return { success: true, data: result };
    } catch (e) {
      throw new Error(`列出子表失败: ${e.message || e}`);
    }
  }
}

// ==================== 辅助函数 ====================

/**
 * 强制触发子表响应式刷新
 * heyui Table内部缓存datas，仅改数组引用/元素不一定触发重新渲染，
 * 因此除了改数组引用，还forceUpdate包含表格的父组件（rs-form-edit.$parent = add.vue）
 */
function _refreshSubTable(dt, formEdit) {
  if (!dt || !dt.data) return;
  // slice创建新数组（同元素），改变引用，触发Vue响应式
  dt.data = dt.data.slice();
  // forceUpdate父组件（包含rs-table-edit），强制表格重新渲染
  if (formEdit && formEdit.$parent && typeof formEdit.$parent.$forceUpdate === 'function') {
    formEdit.$parent.$forceUpdate();
  }
}

/**
 * 查找rs-table-edit组件（通过$parent链遍历$refs）
 * 优先按path匹配ref名，其次遍历所有ref找name='rs-table-edit'
 */
function _findTableComp(formEdit, path) {
  if (!formEdit) return null;
  let parent = formEdit.$parent;
  let depth = 0;
  while (parent && depth < 6) {
    if (parent.$refs) {
      // 优先按 path ref 名找
      if (path && parent.$refs[path]) {
        const ref = parent.$refs[path];
        const t = Array.isArray(ref) ? ref[0] : ref;
        if (t && t.$options && t.$options.name === 'rs-table-edit') return t;
      }
      // 遍历所有ref找rs-table-edit
      for (const key in parent.$refs) {
        const ref = parent.$refs[key];
        const targets = Array.isArray(ref) ? ref : [ref];
        for (const t of targets) {
          if (t && t.$options && t.$options.name === 'rs-table-edit') return t;
        }
      }
    }
    parent = parent.$parent;
    depth++;
  }
  return null;
}

/**
 * 获取子表DataTable
 * 优先从context.storeName的store模块获取，找不到则遍历所有模块
 */
function _getSubTable(context, path) {
  const store = context.store;
  const storeName = context.storeName;
  // 1. 优先从当前store模块获取
  if (storeName && store.state[storeName]) {
    const mod = store.state[storeName];
    if (mod && mod.dt && mod.dt[path]) return mod.dt[path];
  }
  // 2. 遍历所有store模块查找
  const state = store.state;
  for (const moduleName of Object.keys(state)) {
    const mod = state[moduleName];
    if (mod && mod.dt && mod.dt[path]) return mod.dt[path];
  }
  // 3. fallback：从表单组件的onSubTable逻辑（已弃用，直接返回null）
  return null;
}

// ==================== 代理层主类 ====================

/**
 * AI Agent Proxy - 前端AI能力代理层主类
 */
export class AIAgentProxy {
  constructor(options = {}) {
    this.store = options.store || store;
    this.router = options.router || router;
    this.registry = toolRegistry;
    this.isInitialized = false;
    // 待处理的前端工具调用回调（connectionId → resolve/reject）
    this.pendingCalls = new Map();
  }

  /**
   * 初始化代理层
   */
  init(options = {}) {
    if (this.isInitialized) return;

    this.store = options.store || this.store;
    this.router = options.router || this.router;

    // 注册默认前端工具
    this.registerDefaultTools();

    this.isInitialized = true;
    console.log('[AIAgentProxy] 代理层已初始化，已注册', this.registry.tools.size, '个工具');
  }

  /**
   * 注册默认前端工具
   */
  registerDefaultTools() {
    // 基础前端工具
    this.registry.register('navigate', new NavigateTool());
    this.registry.register('fill_form', new FillFormTool());
    this.registry.register('fill_subtable', new FillSubTableTool());
    this.registry.register('get_current_page', new GetCurrentPageTool());
    this.registry.register('get_form_data', new GetFormDataTool());
    this.registry.register('get_user_info', new GetUserInfoTool());
    this.registry.register('get_menus', new GetMenusTool());
    this.registry.register('show_message', new ShowMessageTool());
    this.registry.register('close_dialog', new CloseDialogTool());
    // 主表操作工具
    this.registry.register('open_form', new OpenFormTool());
    this.registry.register('set_form_field', new SetFormFieldTool());
    this.registry.register('get_form_field', new GetFormFieldTool());
    this.registry.register('save_form', new SaveFormTool());
    // 子表操作工具
    this.registry.register('add_subtable_row', new AddSubTableRowTool());
    this.registry.register('delete_subtable_row', new DeleteSubTableRowTool());
    this.registry.register('update_subtable_row', new UpdateSubTableRowTool());
    this.registry.register('clear_subtable', new ClearSubTableTool());
    this.registry.register('get_subtable_data', new GetSubTableDataTool());
    this.registry.register('list_subtables', new ListSubTablesTool());
  }

  /**
   * 创建执行上下文
   */
  createContext(extra = {}) {
    return new ToolExecutionContext({
      store: this.store,
      router: this.router,
      userInfo: this.store ? this.store.state.user.userInfo : null,
      moduleCode: extra.moduleCode || null,
      storeName: extra.storeName || null,
      extra
    });
  }

  /**
   * 执行前端工具（带超时保护，避免工具Promise pending导致后端干等）
   */
  async execute(name, args, extra = {}) {
    const context = this.createContext(extra);
    const TIMEOUT = 15000; // 15s 超时（小于后端30s，确保能回传）
    try {
      const result = await Promise.race([
        this.registry.execute(name, args, context),
        new Promise((resolve) => setTimeout(
          () => resolve({ success: false, error: `工具 ${name} 执行超时(${TIMEOUT / 1000}s)` }),
          TIMEOUT
        ))
      ]);
      return result;
    } catch (e) {
      console.error(`[AIAgentProxy] 工具 ${name} 执行失败:`, e);
      return { success: false, error: e.message || '执行失败' };
    }
  }

  /**
   * 获取所有工具定义（用于LLM）
   */
  getToolDefinitions() {
    return this.registry.getAllDefinitions();
  }

  /**
   * 获取前端工具定义
   */
  getFrontendToolDefinitions() {
    return this.registry.getFrontendDefinitions();
  }

  /**
   * 注册自定义工具
   */
  registerTool(name, tool) {
    this.registry.register(name, tool);
  }

  /**
   * 判断工具是否为前端工具
   */
  isFrontendTool(name) {
    const tool = this.registry.get(name);
    return tool ? tool.isFrontend() : false;
  }

  /**
   * 按 scene 返回该场景的前端工具定义子集（供 AiClient 注册给后端）
   * 优先读 tss_ai_scene.FRONTENDTOOLS（all/none/逗号名单）；配置未加载时回落历史硬编码：
   * - assistant: 全部 19 个前端工具
   * - form: 表单/子表相关工具子集
   * - optimize/aidev/wizard/sfc: 空数组（无前端工具）
   * @param {string} scene - 'assistant'|'form'|'optimize'|'aidev'|'wizard'|'sfc'|自定义场景
   * @returns {Array} 工具定义数组
   */
  registerForScene(scene) {
    const allDefs = this.registry.getFrontendDefinitions();
    const filterByNames = (names) => allDefs.filter(d => {
      const name = d && d.function && d.function.name;
      return name ? names.indexOf(name) !== -1 : false;
    });
    // 场景配置（tss_ai_scene）优先
    const cfg = getSceneSync(scene);
    if (cfg && cfg.FRONTENDTOOLS) {
      if (cfg.FRONTENDTOOLS === 'all') return allDefs;
      if (cfg.FRONTENDTOOLS === 'none') return [];
      return filterByNames(cfg.FRONTENDTOOLS.split(',').map(s => s.trim()).filter(Boolean));
    }
    // 配置未加载：回落历史硬编码
    if (scene === 'assistant') {
      return allDefs;
    }
    if (scene === 'form') {
      return filterByNames([
        'fill_form', 'fill_subtable', 'get_form_data', 'get_form_field',
        'set_form_field', 'save_form', 'add_subtable_row', 'delete_subtable_row',
        'update_subtable_row', 'clear_subtable', 'get_subtable_data', 'list_subtables'
      ]);
    }
    // optimize/aidev/wizard/sfc 无前端工具
    return [];
  }
}

// 导出单例
export const aiAgentProxy = new AIAgentProxy();

export default aiAgentProxy;

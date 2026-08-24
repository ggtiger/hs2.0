# AI Agent Proxy - 前端AI能力代理层

## 概述

AI Agent Proxy 是一个前端AI能力代理层，用于统一管理AI可调用的前端能力。它提供了一套完整的工具注册、执行和管理机制，让AI能够通过标准化的方式操作前端应用。

## 核心特性

- **工具注册表**：支持动态注册和发现工具
- **上下文管理**：维护执行上下文，包括路由、状态、用户信息等
- **标准化接口**：统一的工具定义和执行接口
- **SignalR集成**：与后端SignalR无缝集成
- **Vue插件**：提供Vue插件，方便在组件中使用

## 架构

```
┌─────────────────────────────────────────┐
│              AI Assistant               │
│         (DeepSeek LLM + ReAct)          │
└─────────────────┬───────────────────────┘
                  │ SignalR
                  ▼
┌─────────────────────────────────────────┐
│         AI Agent Proxy (前端)            │
│  ┌─────────────┐  ┌─────────────────┐ │
│  │ ToolRegistry │  │ ContextManager  │ │
│  │  工具注册表   │  │   上下文管理     │ │
│  └──────┬──────┘  └─────────────────┘ │
│         │                               │
│  ┌──────┴──────┐  ┌─────────────────┐  │
│  │  NavigateTool │  │  QueryDataTool   │  │
│  │  FillFormTool │  │  SearchMenuTool  │  │
│  │  OpenRecordTool│  │  ...            │  │
│  └───────────────┘  └─────────────────┘  │
└─────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│           前端应用 (Vue + Vuex)           │
│  ┌─────────┐ ┌─────────┐ ┌──────────┐  │
│  │  Router │ │  Store  │ │  Components│ │
│  └─────────┘ └─────────┘ └──────────┘  │
└─────────────────────────────────────────┘
```

## 安装

在 `main.js` 中安装插件：

```javascript
import AIAgentPlugin from '@/utils/aiAgentPlugin';

Vue.use(AIAgentPlugin, { store, router });
```

## 使用方式

### 1. 在组件中执行工具

```javascript
// 导航到指定模块
const result = await this.$aiAgent.execute('navigate', {
  path: '/r01/m05',
  query: { id: '123' }
});

// 查询数据
const result = await this.$aiAgent.execute('query_data', {
  moduleCode: 'LI_M02',
  filter: { STATE: 1 }
});

// 填充表单
const result = await this.$aiAgent.execute('fill_form', {
  fields: { CUSTNAME: 'ABC公司' }
}, {
  formEdit: this.$refs.form
});
```

### 2. 注册自定义工具

```javascript
this.$aiAgent.registerTool('my_tool', {
  getDefinition() {
    return {
      type: 'function',
      function: {
        name: 'my_tool',
        description: '我的自定义工具',
        parameters: {
          type: 'object',
          properties: {
            param1: { type: 'string' }
          },
          required: ['param1']
        }
      }
    };
  },
  async execute(args, context) {
    const { param1 } = args;
    return { success: true, data: `结果: ${param1}` };
  }
});
```

### 3. 使用SignalR客户端

```javascript
// 创建客户端
const client = this.$aiAgent.createClient({
  moduleCode: 'LI_M02',
  userInfo: this.$store.state.user.userInfo,
  onBlock: (b) => console.log('收到:', b),
  onError: (err) => console.error('错误:', err),
  onDone: () => console.log('完成')
});

// 初始化连接
await client.init();

// 发送消息
await client.send('查询本月订单');

// 断开连接
await client.disconnect();
```

## 内置工具

| 工具名称 | 描述 | 参数 |
|---------|------|------|
| `navigate` | 导航到指定模块 | `path`, `query` |
| `query_data` | 查询模块数据 | `moduleCode`, `filter`, `pageSize`, `pageIndex` |
| `get_module_schema` | 获取模块Schema | `moduleCode` |
| `fill_form` | 填充表单字段 | `fields` |
| `search_menu` | 搜索菜单 | `keyword` |
| `open_record` | 打开记录 | `moduleCode`, `id` |

## API参考

### ITool 接口

所有工具必须实现此接口：

```javascript
class ITool {
  getDefinition() // 获取工具定义（用于LLM）
  execute(args, context) // 执行工具
}
```

### ToolExecutionContext 上下文

```javascript
class ToolExecutionContext {
  store      // Vuex store实例
  route      // 当前路由
  userInfo   // 用户信息
  moduleCode // 当前模块代码
  storeName  // 当前store名称
  extra      // 额外数据

  getStoreModule()   // 获取store模块
  getMainData()      // 获取主表数据
  getSubTableData()  // 获取子表数据
}
```

### AIAgentProxy 代理层

```javascript
class AIAgentProxy {
  init(options)              // 初始化
  execute(name, args, extra) // 执行工具
  getToolDefinitions()       // 获取所有工具定义
  registerTool(name, tool)   // 注册工具
}
```

## 最佳实践

1. **工具命名**：使用下划线命名法，如 `query_data`
2. **错误处理**：工具执行失败时返回 `{ success: false, error: '错误信息' }`
3. **上下文传递**：通过 `extra` 参数传递额外上下文
4. **异步操作**：工具执行方法使用 `async/await`
5. **资源清理**：使用 `beforeDestroy` 清理SignalR连接

## 示例

参见 `aiAgentExamples.js` 文件，包含完整的使用示例。

/**
 * SFC Loader — 对外暴露 API
 *
 * 用法:
 *   运行时加载:  import { loadCompiledSFC } from '@/sfc-loader'
 *   保存时编译:  import { compileSFC } from '@/sfc-loader'
 */

// eslint-disable-next-line camelcase
export { loadCompiledSFC, __sfc_require__, clearModuleCache, invalidateCacheByPrefix, resolvePath, preloadDeps } from './module-resolver';
export {
  compileSFC,
  parseSFC,
  extractDeps,
  compileTemplate,
  compileScript,
  compileStyles,
  executeCompiled,
} from './sfc-compiler';

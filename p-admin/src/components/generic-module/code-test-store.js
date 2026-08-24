/**
 * code-test-panel 的 store
 *
 * 把原先 .vue 直接 db.postData 的 3 处调用收口到 actions:
 *   - loadApiLinks: 加载资产已关联的模块接口（接口执行下拉）
 *   - runSource:   源码试运行 (RCodeAsset A07=sql / A08=csharp)
 *   - runViaApi:   接口执行（走真实模块接口通道 /api/data/call/{MC}/{AC}/）
 *
 * 所有 action 内部 catch 异常并返回统一结果信封 { ok, message, ... }，
 * 保证 $callAction 的 successCall 总能触发（避免全局 $error 弹窗打断测试面板的行内错误展示）。
 *
 * 详见 docs/frontend-store-convention.md
 */
import db from '@/api/db';
import createStore from '@/store/createStore';

const STORE_NAME = 'ctp';

const { mapState, mapGetters, Constants } = createStore.getStore({
  config: { moduleCode: 'CTP' },
  storeName: STORE_NAME,
  actions: {
    // 加载资产已关联的模块接口（接口执行模式下拉）
    // 失败静默（无关联时接口执行不可用）
    async loadApiLinks(ctx, { code, kind }) {
      if (!code || kind === 'js') return { items: [] };
      try {
        const ret = await db.postData({
          api: '/api/RCodeAsset/call/RS_M17/A09/',
          params: { CODE: code },
        });
        return { items: (ret && ret.items) || [] };
      } catch (e) {
        return { items: [] };
      }
    },
    // 源码试运行（可测未保存内容）
    async runSource(ctx, { kind, code, source, values }) {
      const apiCode = kind === 'csharp' ? 'A08' : 'A07';
      try {
        const ret = await db.postData({
          api: '/api/RCodeAsset/call/RS_M17/' + apiCode + '/',
          params: { CODE: code, SOURCE: source, VALUES: Object.assign({}, values) },
        });
        if (kind === 'sql') {
          return {
            ok: true,
            count: ret.count,
            columns: ret.columns || [],
            rows: ret.rows || [],
            sql: ret.sql,
          };
        }
        const ok = ret && ret.code === 200;
        return {
          ok: ok,
          message: ret && ret.message,
          jsonObj: (ret && ret.data) !== undefined ? ret.data : ret,
        };
      } catch (e) {
        return { ok: false, message: e.message || String(e) };
      }
    },
    // 接口执行: 走模块真实接口通道
    async runViaApi(ctx, { moduleCode, apiCode, values }) {
      try {
        const ret = await db.postData({
          api: '/api/data/call/' + moduleCode + '/' + apiCode + '/',
          params: Object.assign({}, values),
        });
        // 查询类返回 Items，脚本类返回脚本 SetData 的内容
        if (ret && ret.Items) {
          return {
            ok: true,
            count: ret.Items.length,
            columns: ret.Items.length > 0 ? Object.keys(ret.Items[0]) : [],
            rows: ret.Items,
          };
        }
        return { ok: true, jsonObj: ret };
      } catch (e) {
        return { ok: false, message: e.message || String(e) };
      }
    },
  },
});

export { mapState, mapGetters, Constants };

# r01/m02 原始记录（LI_M02）→ generic-module + 整页 SFC 重写方案

## 迁移思路

系统最核心模块（5484 行），4 个变体页面（m02/m021/m022/m023/m024），流程极其复杂（受理→检验→提交→审核→审批→签发→电子签发→作废），不走标准 list/form 模板，采用**整页 SFC 重写**策略：

1. **数据库配置** - tss_module_page（6 个页面）+ tss_module_button（按角色差异化按钮）+ tss_resuipc
2. **PAGECONFIG** - SFCMODULEPATH 指向整页 SFC，不走 generic-form 标准 render
3. **Store 扩展** - 完整保留 30+ 自定义 actions（审批流/证书生成/电子签发/模板加载）+ 7 个 mutations
4. **main.js 列表扩展** - 16 查询字段 + 复杂按钮显隐 + PDF 预览 + 审核人选入
5. **form.js 表单扩展** - attach-flow-panel 三栏 + rs-edit-item 动态字段 + 审核人选入弹窗
6. **整页 SFC** - 8 个 SFC 资产完全替代原 5 个 main.vue + 5 个 add.vue
7. **变体页面** - 4 个角色差异化（查询过滤器/按钮/状态选项）

---

## 一、数据库配置（m18 可视化配置）

### 1.1 模块配置 (tss_module_page)

| PAGECODE | PAGENAME | PAGETYPE | ROUTEPATH | QUERY_APICODE | OPEN_APICODE | SAVE_APICODE |
|----------|----------|----------|-----------|---------------|--------------|--------------|
| main | 原始记录 | list | /g/LI_M02/main | A01 | - | - |
| add | 原始记录编辑 | form | /g/LI_M02/add | - | A02 | A04 |
| m021 | 记录审核 | list | /g/LI_M02/m021 | A34 | - | - |
| m022 | 记录审批 | list | /g/LI_M02/m022 | A36 | - | - |
| m023 | 记录签发 | list | /g/LI_M02/m023 | A40 | - | - |
| m024 | 记录查询下载 | list | /g/LI_M02/m024 | A41 | - | - |

注意：变体页面 (m021-m024) 的 ADVQUERY_APICODE 分别为 A35/A37/A42/A46。

### 1.2 main 页 PAGECONFIG

```json
{
  "QRYPATH": "QRY",
  "QQRYSPATH": "QQRY",
  "defaultFormPageCode": "add",
  "EXTENDJS": "@/modules/LI_M02/main.js",
  "SLOTS": {
    "simple-query": "@/modules/LI_M02/query-panel.vue",
    "header-action": "@/modules/LI_M02/header-actions.vue",
    "footer-action": "@/modules/LI_M02/footer-actions.vue",
    "table-action": "@/modules/LI_M02/table-actions.vue"
  }
}
```

### 1.3 add 页 PAGECONFIG（整页 SFC，不走 generic-form）

```json
{
  "PAGETYPE": "form",
  "SFCMODULEPATH": "@/modules/LI_M02/add.vue",
  "EXTENDJS": "@/modules/LI_M02/form.js",
  "MAINPATH": "MAIN",
  "DTSA_PATH": "DTSA",
  "DTSB_PATH": "DTSB",
  "DTSC_PATH": "DTSC",
  "DTSD_PATH": "DTSD"
}
```

### 1.4 变体页面 PAGECONFIG

```json
// m021 审核
{
  "QRYPATH": "QRY",
  "QQRYSPATH": "QQRY",
  "QUERY_APICODE": "A34",
  "ADVQUERY_APICODE": "A35",
  "defaultFormPageCode": "add",
  "EXTENDJS": "@/modules/LI_M02/m021.js",
  "SLOTS": {
    "simple-query": "@/modules/LI_M02/query-panel.vue",
    "footer-action": "@/modules/LI_M02/m021-footer.vue"
  }
}

// m022 审批
{
  "QRYPATH": "QRY",
  "QQRYSPATH": "QQRY",
  "QUERY_APICODE": "A36",
  "ADVQUERY_APICODE": "A37",
  "EXTENDJS": "@/modules/LI_M02/m022.js",
  "SLOTS": {
    "simple-query": "@/modules/LI_M02/query-panel.vue",
    "footer-action": "@/modules/LI_M02/m022-footer.vue"
  }
}

// m023 签发
{
  "QRYPATH": "QRY",
  "QQRYSPATH": "QQRY",
  "QUERY_APICODE": "A40",
  "ADVQUERY_APICODE": "A42",
  "EXTENDJS": "@/modules/LI_M02/m023.js",
  "SLOTS": {
    "simple-query": "@/modules/LI_M02/query-panel.vue",
    "footer-action": "@/modules/LI_M02/m023-footer.vue"
  }
}

// m024 查询下载
{
  "QRYPATH": "QRY",
  "QQRYSPATH": "QQRY",
  "QUERY_APICODE": "A41",
  "ADVQUERY_APICODE": "A46",
  "EXTENDJS": "@/modules/LI_M02/m024.js",
  "SLOTS": {
    "simple-query": "@/modules/LI_M02/query-panel.vue",
    "footer-action": "@/modules/LI_M02/m024-footer.vue"
  }
}
```

### 1.5 按钮配置 (tss_module_button)

#### main 页（检验员/提交人）

| BTNNAME | BTNCODE | BTNAREA | APICODE | INTERACTTYPE | SHOWCOND | PERMCODE |
|---------|---------|---------|---------|--------------|----------|----------|
| 添加 | add | footer | - | navigate | - | LI_M02/A04 |
| 查看变更记录 | viewLog | footer | A31 | modal | `ISSHOWLOGLIST` | - |
| 提交（含选审核人） | submit | footer | A17 | tooltip-submit | `ISSHOWSUBMIT` | LI_M02/A17 |
| 撤销提交 | reSubmit | footer | A18 | confirm | `ISSHOWRESUBMIT` | LI_M02/A18 |
| 更新模版 | updateTemplate | footer | A45 | confirm | `ISSHOWUPDATE` | LI_M02/A45 |
| 证书预览 | printPreview | footer | A49 | custom | `ISSHOWPRINTPREVIEW` | LI_M02/A49 |

#### m021 页（审核员）

| BTNNAME | BTNCODE | BTNAREA | APICODE | INTERACTTYPE | SHOWCOND | PERMCODE |
|---------|---------|---------|---------|--------------|----------|----------|
| 查看变更记录 | viewLog | footer | A31 | modal | `ISSHOWLOGLIST` | - |
| 审核（含选下一审批人） | check | footer | A12 | tooltip-check | `ISSHOWCHECK` | LI_M02/A12 |
| 驳回 | checkReject | footer | A16 | confirm | `ISSHOWCHECK` | LI_M02/A12 |
| 撤销审核 | reCheck | footer | A13 | confirm | `ISSHOWRECHECK` | LI_M02/A13 |
| 证书预览 | printPreview | footer | A49 | custom | `ISSHOWPRINTPREVIEW` | LI_M02/A49 |

#### m022 页（审批员）

| BTNNAME | BTNCODE | BTNAREA | APICODE | INTERACTTYPE | SHOWCOND | PERMCODE |
|---------|---------|---------|---------|--------------|----------|----------|
| 查看变更记录 | viewLog | footer | A31 | modal | `ISSHOWLOGLIST` | - |
| 审批 | verify | footer | A14 | tooltip-verify | `ISSHOWVERIFY` | LI_M02/A14 |
| 驳回 | verifyReject | footer | A16 | confirm | `ISSHOWVERIFY` | LI_M02/A14 |
| 撤销审批 | reVerify | footer | A15 | confirm | `ISSHOWREVERIFY` | LI_M02/A15 |
| 证书生成 | genCert | footer | A21 | confirm | `ISSHOWGENCERT` | LI_M02/A21 |
| 证书预览 | printPreview | footer | A49 | custom | `ISSHOWPRINTPREVIEW` | LI_M02/A49 |

#### m023 页（签发员）

| BTNNAME | BTNCODE | BTNAREA | APICODE | INTERACTTYPE | SHOWCOND | PERMCODE |
|---------|---------|---------|---------|--------------|----------|----------|
| 查看变更记录 | viewLog | footer | A31 | modal | `ISSHOWLOGLIST` | - |
| 记录签发 | signCert | footer | A27 | confirm | `ISSHOWGENCERT` | LI_M02/A27 |
| 撤销签发 | reGenCert | footer | A50 | confirm | `ISSHOWREGENCERT` | LI_M02/A50 |
| 电子签发（含设密码） | eCertSign | footer | A55 | modal-pwd | `ISSHOWECERTSIGN` | LI_M02/A55 |
| 重置密码 | resetPwd | footer | A58 | modal-resetpwd | `ISSHOWRESETPWD` | LI_M02/A58 |
| 证书预览 | printPreview | footer | A49 | custom | `ISSHOWPRINTPREVIEW` | LI_M02/A49 |

#### m024 页（查询下载）

| BTNNAME | BTNCODE | BTNAREA | APICODE | INTERACTTYPE | SHOWCOND | PERMCODE |
|---------|---------|---------|---------|--------------|----------|----------|
| 查看变更记录 | viewLog | footer | A31 | modal | `ISSHOWLOGLIST` | - |
| 记录打印 | print | footer | A38 | custom | `ISSHOWPRINT` | LI_M02/A38 |
| 记录下载 | download | footer | A39 | custom | `ISSHOWPDOWNLOAD` | LI_M02/A39 |
| 证书预览 | printPreview | footer | A49 | custom | `ISSHOWPRINTPREVIEW` | LI_M02/A49 |

#### add 页（表单内按钮）

| BTNNAME | BTNCODE | BTNAREA | APICODE | INTERACTTYPE | SHOWCOND | PERMCODE |
|---------|---------|---------|---------|--------------|----------|----------|
| 暂存 | save | footer | A04 | custom | `ISSHOWSAVE` | LI_M02/A04 |
| 删除 | delete | footer | A07 | confirm | `ISSHOWDELETE` | LI_M02/A07 |
| 提交（选审核人） | submit | footer | A17 | tooltip-submit | `ISSHOWSUBMIT` | LI_M02/A17 |
| 提交并继续 | submit2 | footer | A17 | tooltip-submit2 | `ISSHOWSUBMIT` | LI_M02/A17 |
| 撤销提交 | reSubmit | footer | A18 | confirm | `ISSHOWRESUBMIT` | LI_M02/A18 |

---

## 二、Store 扩展（完整代码框架）

路径：`@/modules/LI_M02/store.js`

### 2.1 Actions 清单（30+ 自定义）

```javascript
/**
 * LI_M02 Store 扩展 - 原始记录核心模块
 *
 * 保留 baseStore.js 的全部自定义 actions/mutations。
 * Store03 默认 actions (query/open/add/save/delete/submit/reSubmit/batch/call) 已内置。
 *
 * 关键设计：
 * - apiPath: '/api/rm11/call' （走 RM11Controller，非标准 /api/data/call）
 * - paths: MAIN/DTSA/DTSB/DTSC/DTSD/ARD/ACCEPT/LOG/OPLOGS/PTEMPSEL/EMPUSER/REGUITEM/TSTDD/PTMP
 */
export default {
  actions: {
    // ====== 单据号/证书号 ======

    // 获取证书编号 (A44 TCODE 模板)
    // 用法: const ret = await dispatch('getBillCode', { TCODE: 'WT|%Y%m%d|' });
    async getBillCode({ dispatch }, { TCODE }) {
      return await dispatch('call', {
        APICODE: 'A44',
        params: { TCODE },
      });
    },

    // ====== 模板加载（核心）======

    // 打开原始记录模板 (A08)，解析 REFTPMDATA，填充 MAIN 各字段
    // 参数: ID=模板ID, ISEDIT=true编辑模式, item=ACCEPT 行（受理单）
    // 调用 SETTPMDATA mutation + 触发 ardSel（标准器查询）+ 复制附件
    async openPTEMP({ state, commit, dispatch }, { ID, ISEDIT, item }) {
      sessionStorage.setItem('hlims_ueditor_fmFields', JSON.stringify({}));
      const ret = await dispatch('call', {
        APICODE: 'A08',
        params: { PageSize: 1, PageIndex: 1, FilterParams: { ID } },
        apiPath: '/api/data/call',  // 走标准 data 路由
      });
      commit('M_INITDATA', { path: 'PTEMPSEL', data: ret.Items || [] });

      const MAIN = storeHelper.getTable('MAIN');
      const PTEMPSEL = storeHelper.getTable('PTEMPSEL');
      let REFTPMDATA = MAIN.getValue('TPMDATA');
      if (!REFTPMDATA) {
        MAIN.setValue('TPMDATA', PTEMPSEL.getValue('REFTPMDATA'));
        MAIN.setValue('REFTPMDATA', JSON.parse(PTEMPSEL.getValue('REFTPMDATA')));
      } else {
        MAIN.setValue('REFTPMDATA', JSON.parse(REFTPMDATA));
      }

      if (ISEDIT !== true && ret.Items) {
        // 新建模式：自动选入标准器 + 生成证书编号 + SETTPMDATA + 复制附件
        await dispatch('ardSel', {
          INPUT: '-------------',
          TSTANDARDID: ret.Items[0].TSTANDARDID,
        });
        const bcode = await dispatch('getBillCode', { TCODE: ret.Items[0].CERTCODE });
        if (bcode) ret.Items[0].CERTCODE = bcode;
        commit('SETTPMDATA', { item: ret.Items[0], aitem: item });

        // 复制受理单附件 (A51)
        const files = await dispatch('call', {
          APICODE: 'A51',
          params: { PageSize: 1, PageIndex: 1, FilterParams: { ID: storeHelper.getTable('ACCEPT').getValue('ID', item) } },
          apiPath: '/api/data/call',
        });
        commit('SETFILEDATA', { files: (files.Items || []).map(f => ({ id: f.FILEID, name: f.FILENAME })) });
      }

      // 解析 REFTPMDATA 树（dealTreeData）
      dealTreeData(MAIN.getValue('REFTPMDATA') || [], MAIN);
    },

    // ====== 变更记录 ======

    // 查询变更记录 (A31)
    async queryLog({ state, commit }, { ID }) {
      const ret = await dispatch('call', {
        APICODE: 'A31',
        params: { PageSize: 1, PageIndex: 1, FilterParams: { ID } },
        apiPath: '/api/data/call',
      });
      commit('M_INITDATA', { path: 'LOG', data: ret.Items || [] });
      commit('INIT', { paths: ['OPLOGS'] });
      commit('SETLOGDATA', {});
    },

    // ====== 自定义保存（组装数据）======

    // 自定义保存 (A04)：SETSAVEDATA 组装 → 清审核字段 → save
    async doMySave({ state, commit, dispatch }, { inputObj, editorObj, tableObj }) {
      commit('SETSAVEDATA', { inputObj, editorObj, tableObj });
      const MAIN = storeHelper.getTable('MAIN');
      ['CHECKER', 'CHECKREMARK', 'CHECKTIME', 'VERIFIER', 'VERIFYREMARK', 'VERIFYTIME']
        .forEach(f => MAIN.setValue(f, ''));
      await dispatch('save', {});
      commit('INIT', { paths: ['ARD'] });
    },

    // 自定义提交 (A17)：SETSAVEDATA → 清部分审核字段 → submit
    async doMySubmit({ state, commit, dispatch }, { inputObj, editorObj, tableObj }) {
      commit('SETSAVEDATA', { inputObj, editorObj, tableObj });
      const MAIN = storeHelper.getTable('MAIN');
      ['CHECKREMARK', 'CHECKTIME', 'VERIFIER', 'VERIFYREMARK', 'VERIFYTIME']
        .forEach(f => MAIN.setValue(f, ''));
      await dispatch('submit', {});
      commit('INIT', { paths: ['ARD'] });
    },

    // ====== 单条审批流操作（返回 updateFields）======

    // 通用更新方法：call AXX → 回写指定字段到 item
    async callUpdate({ commit, dispatch }, { APICODE, params, item, updateFields }) {
      const ret = await dispatch('call', { APICODE, params });
      updateFields.forEach(f => { item[f] = ret[0][f]; });
    },

    // 审核 (A12) - 含选下一审批人 VERIFYID/VERIFYER
    async check({ dispatch }, { REMARK, ID, item, VERIFYID, VERIFYER }) {
      return await dispatch('callUpdate', {
        APICODE: 'A12',
        params: { REMARK, ID, NEXTAPRID: VERIFYID, NEXTAPRER: VERIFYER },
        item,
        updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'],
      });
    },

    // 撤销审核 (A13)
    async reCheck({ dispatch }, { REMARK, ID, item }) {
      return await dispatch('callUpdate', {
        APICODE: 'A13', params: { REMARK, ID }, item,
        updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'],
      });
    },

    // 审批 (A14)
    async verify({ dispatch }, { REMARK, ID, item }) {
      return await dispatch('callUpdate', {
        APICODE: 'A14', params: { REMARK, ID }, item,
        updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'],
      });
    },

    // 撤销审批 (A15)
    async reVerify({ dispatch }, { REMARK, ID, item }) {
      return await dispatch('callUpdate', {
        APICODE: 'A15', params: { REMARK, ID }, item,
        updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'],
      });
    },

    // 驳回 (A16) - 审核驳回/审批驳回共用
    async reject({ dispatch }, { REMARK, ID, item }) {
      return await dispatch('callUpdate', {
        APICODE: 'A16', params: { REMARK, ID }, item,
        updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'],
      });
    },

    // ====== 批量审批流操作（走 batch action）======

    // 批量审核 (A23) - 含选下一审批人
    async batchCheck({ dispatch }, { items, REMARK, VERIFYID, VERIFYER }) {
      await dispatch('batch', {
        APICODE: 'A23', items,
        params: { REMARK, NEXTAPRID: VERIFYID, NEXTAPRER: VERIFYER },
        updateFields: ['STATE', 'CHECKER', 'CHECKTIME'],
      });
    },

    // 批量审核驳回 (A28)
    async batchCheckReject({ dispatch }, { items, REMARK }) {
      await dispatch('batch', {
        APICODE: 'A28', items, params: { REMARK },
        updateFields: ['STATE', 'CHECKER', 'CHECKTIME'],
      });
    },

    // 批量撤销审核 (A24)
    async batchReCheck({ dispatch }, { items, REMARK }) {
      await dispatch('batch', {
        APICODE: 'A24', items,
        updateFields: ['STATE', 'CHECKER', 'CHECKTIME'],
      });
    },

    // 批量审批 (A25)
    async batchVerify({ dispatch }, { items, REMARK }) {
      await dispatch('batch', {
        APICODE: 'A25', items, params: { REMARK },
        updateFields: ['STATE', 'VERIFIER', 'VERIFYTIME'],
      });
    },

    // 批量审批驳回 (A29)
    async batchVerifyReject({ dispatch }, { items, REMARK }) {
      await dispatch('batch', {
        APICODE: 'A29', items, params: { REMARK },
        updateFields: ['STATE', 'VERIFIER', 'VERIFYTIME'],
      });
    },

    // 批量撤销审批 (A26)
    async batchReVerify({ dispatch }, { items }) {
      await dispatch('batch', {
        APICODE: 'A26', items,
        updateFields: ['STATE', 'VERIFIER', 'VERIFYTIME'],
      });
    },

    // 批量证书生成/记录签发 (A27)
    async batchGenCert({ dispatch }, { items }) {
      await dispatch('batch', {
        APICODE: 'A27', items,
        updateFields: ['STATE', 'AEMPID', 'AEMPNAME'],
      });
    },

    // 批量撤销签发 (A50)
    async batchReGenCert({ dispatch }, { items }) {
      await dispatch('batch', {
        APICODE: 'A50', items,
        updateFields: ['STATE', 'AEMPID', 'AEMPNAME'],
      });
    },

    // 批量更新模板 (A45)
    async batchUpdateTemplate({ dispatch }, { items, REMARK, CHECKID, CHECKER }) {
      await dispatch('batch', {
        APICODE: 'A45', items,
        updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'],
      });
    },

    // 批量下载 (A39)
    async download({ dispatch }, { items }) {
      return await dispatch('batch', {
        APICODE: 'A39', items,
        updateFields: ['STATE'],
      });
    },

    // ====== 受理/撤销受理 ======

    // 受理 (A11)
    async accept({ dispatch }, { items }) {
      await dispatch('batch', {
        APICODE: 'A11', items,
        updateFields: ['STATE', 'AEMPID', 'AEMPNAME'],
      });
    },

    // 撤销受理 (A30)
    async reAccept({ dispatch }, { items }) {
      await dispatch('batch', {
        APICODE: 'A30', items,
        updateFields: ['STATE', 'AEMPID', 'AEMPNAME'],
      });
    },

    // ====== 电子签发（核心）======

    // 单条证书生成 (A21 TYPE=GENCERT)
    async genCert({ dispatch }, { ID }) {
      await dispatch('call', {
        APICODE: 'A21', params: { TYPE: 'GENCERT', ID },
      });
    },

    // 证书作废 (A22)
    async invalid({ dispatch }, { ID }) {
      return await dispatch('call', { APICODE: 'A22', params: { ID } });
    },

    // 证书预览 (A49) - 返回 PDF url
    async printPreview({ dispatch }, { ID }) {
      return await dispatch('call', { APICODE: 'A49', params: { ID } });
    },

    // 单条电子签发 (A55) - 含 ECERTPWD
    async eCertSign({ dispatch }, { ID, ECERTPWD }) {
      return await dispatch('call', {
        APICODE: 'A55', params: { ID, ECERTPWD },
      });
    },

    // 批量电子签发 (A55 batch)
    async batchECertSign({ dispatch }, { items, ECERTPWD }) {
      await dispatch('batch', {
        APICODE: 'A55', items, params: { ECERTPWD },
        updateFields: ['ECERTSIGN', 'ECERTSIGNDATE', 'ECERTSIGNER', 'ECERTPWD'],
      });
    },

    // 单条更新电子证书密码 (A58)
    async updateECertPwd({ dispatch }, { ID, ECERTPWD }) {
      return await dispatch('call', {
        APICODE: 'A58', params: { ID, ECERTPWD },
      });
    },

    // 批量更新电子证书密码 (A58 batch)
    async batchUpdateECertPwd({ dispatch }, { items, ECERTPWD }) {
      await dispatch('batch', {
        APICODE: 'A58', items, params: { ECERTPWD },
        updateFields: ['ECERTPWD'],
      });
    },

    // ====== SelStore 下拉选择器 actions（mixActions）======
    // 由 oSelStore.mixActions() 注入：
    // - acceptSel(INPUT, STATE) - 待检验/待受理列表查询
    // - ardSel(INPUT, TSTANDARDID) - 标准器选入
    // - ptmpSel(INPUT, DEPTID) - 模板选入
    // - empSel1(INPUT, FUNCID, DEPTID) - 按功能点+部门选员工（审核人/审批人）
    // - tstddSel(INPUT) - 检校依据
    // - reguitemSel(INPUT) - 检定项目
  },
};
```

### 2.2 Mutations 清单（7 个）

```javascript
mutations: {
  // ====== 1. SETTPMDATA: 模板数据写入 MAIN ======
  // 触发: openPTEMP action, 新建模式下
  // 参数: { item: 模板对象, aitem: 受理单 ACCEPT 行 }
  // 作用: 从模板复制 20+ 字段到 MAIN（DOCCODE/CERTCODE/MNAME/CUSTID/REGUITEMID/TSTANDARDID/...）
  // 同时从 ACCEPT 行复制客户/设备/地址等字段，生成 BILLCODE/CREATER
  SETTPMDATA(state, { item, aitem }) {
    const MAIN = storeHelper.getTable('MAIN');
    const ACCEPT = storeHelper.getTable('ACCEPT');
    // 模板字段
    MAIN.setValue('DOCCODE', item.DOCCODE);
    MAIN.setValue('CERTCODE', item.CERTCODE);
    MAIN.setValue('ORGNAME', item.ORGNAME);
    MAIN.setValue('TSTANDARDID', item.TSTANDARDID);
    MAIN.setValue('TSTANDARDNAME', `${item.STDDCODE}《${item.STDDNAME}》`);
    MAIN.setValue('REGUITEMID', item.REGUITEMID);
    MAIN.setValue('REGUITEMNAME', `${item.REGUITEMCODE} ${item.REGUITEMNAME}`);
    MAIN.setValue('PTEMPLATEID', item.ID);
    // 从 ACCEPT 复制
    MAIN.setValue('ADEPTID', ACCEPT.getValue('ADEPTID', aitem));
    MAIN.setValue('MNAME', ACCEPT.getValue('MNAME', aitem));
    MAIN.setValue('ADDR', ACCEPT.getValue('ADDR', aitem));
    MAIN.setValue('CUSTID', ACCEPT.getValue('CUSTID', aitem));
    MAIN.setValue('CUSTNAME', ACCEPT.getValue('CUSTNAME', aitem));
    MAIN.setValue('REFBILLID', ACCEPT.getValue('ID', aitem));
    MAIN.setValue('REFBILLCODE', ACCEPT.getValue('BILLCODE', aitem));
    // 默认值
    MAIN.setValue('BILLDATE', dateToString(new Date()));
    MAIN.setValue('CREATER', store.state['user'].userInfo.NICKNAME);
    // ...（共 30+ 字段赋值，详见原文件）
  },

  // ====== 2. SETSAVEDATA: 保存前数据组装 ======
  // 触发: doMySave / doMySubmit
  // 参数: { inputObj, editorObj, tableObj }（来自 rs-edit-item）
  // 作用:
  //   1. 必填校验（isnotnull）
  //   2. inputObj 写入 MAIN（REGUITEMNAME 特殊处理）
  //   3. 非主表字段写入 DTSB（FIELDNAME/FIELDREMARK/FIELDVALUE）
  //   4. editorObj（UEditor）字段写入 DTSB
  //   5. tableObj（标准器多选）写入 DTSA
  //   6. 日期校验（MANUFACTDATE < BILLDATE < SIGNDATE）
  //   7. 标准器重复校验
  SETSAVEDATA(state, { inputObj, editorObj, tableObj }) {
    // ...必填校验 + 主表/扩展字段/标准器分别组装
  },

  // ====== 3. SETSHOWTPMDATA: 打开已存在记录时回填 rs-edit-item ======
  // 触发: add.vue 的 showByTemplate / accept 方法
  // 参数: { inputObj, editorObj, tableObj }（来自 rs-edit-item 的 dealConfigSelect）
  // 作用:
  //   1. inputObj 从 MAIN 取值回填
  //   2. 非主表字段从 DTSB 取 FIELDVALUE 回填
  //   3. editorObj（UEditor）从 DTSB 回填
  //   4. tableObj 标准器从 DTSA + ARD 合并回填
  SETSHOWTPMDATA(state, { inputObj, editorObj, tableObj }) {
    // ...回填逻辑
  },

  // ====== 4. SETLOGDATA: 变更记录展开 ======
  // 触发: queryLog action
  // 作用: LOG 表的 LOGDATA（JSON 字符串）展开为 OPLOGS 平铺列表
  SETLOGDATA(state) {
    const LOG = storeHelper.getTable('LOG');
    const OPLOGS = storeHelper.getTable('OPLOGS');
    const items = [];
    LOG.data.forEach(p => {
      const d = JSON.parse(p.LOGDATA);
      d.forEach(pp => items.push({ ...pp, 操作人: p.CREATER, 操作时间: p.CREATEDATE }));
    });
    OPLOGS.setData(items);
  },

  // ====== 5. SETFILEDATA: 附件写入 DTSD ======
  // 触发: openPTEMP 复制附件 / 表单上传
  // 参数: { files: [{id, name}] }
  SETFILEDATA(state, { files }) {
    const DTS = storeHelper.getTable('DTSD');
    DTS.clear();
    files.forEach(f => DTS.add({ FILEID: f.id, FILENAME: f.name }));
  },

  // ====== 6. INIT (Store03 内置，需在 paths 中声明) ======
  // 用法: commit('INIT', { paths: ['MAIN', 'DTSA', 'DTSB', 'DTSC', 'ARD', 'DTSD'] });
  // 作用: 清空指定 DataTable 的 data，但保留列结构

  // ====== 7. M_INITDATA (Store03 内置常量) ======
  // 用法: commit(Constants.M_INITDATA, { path: 'LOG', data: ret.Items });
  // 作用: 用查询结果初始化指定 DataTable
},
```

### 2.3 DataTable Paths 清单

| PATH | 用途 | 来源 |
|------|------|------|
| MAIN | 主表（原始记录主字段） | A02 open |
| DTSA | 标准器子表 | A02 open |
| DTSB | 扩展字段子表（模板自定义字段） | A02 open |
| DTSC | 审批日志子表 | A02 open |
| DTSD | 附件子表 | A02 open |
| ARD | 标准器选入暂存表 | ardSel action |
| ACCEPT | 受理单列表（新建时显示） | acceptSel action |
| LOG | 变更记录原始数据 | queryLog action |
| OPLOGS | 变更记录展开后列表 | SETLOGDATA mutation |
| PTEMPSEL | 模板查询结果 | openPTEMP action |
| EMPUSER | 员工选择器数据 | empSel1 action |
| PTMP | 模板选择列表 | ptmpSel action |
| TSTDD | 检校依据选择器 | tstddSel action |
| REGUITEM | 检定项目选择器 | reguitemSel action |

---

## 三、main.js 列表页扩展

路径：`@/modules/LI_M02/main.js`

```javascript
/**
 * 原始记录列表页扩展
 *
 * this 上下文 (generic-module list):
 *   this.moduleCode = 'LI_M02'
 *   this.storeName / this.storeObj
 *   this.$refs.list - RsTableList 引用
 *   this.selectedRows - 选中的行（checks）
 *   this.$callAction / this.$alert / this.$error / this.$confirm / this.$busy / this.$free
 *   this.PAGECONFIG - 当前页面配置
 */
export default {
  computed: {
    // ====== 按钮显隐逻辑（基于 checks 的 STATE 判断）======

    // 提交：选中行全部 STATE=1 或 12，且当前用户是 CREATEID，且 ADEPTID 一致
    ISSHOWSUBMIT() {
      const rows = this.selectedRows || [];
      if (!rows.length) return false;
      const allSubmit = rows.every(r => r.STATE === 1 || r.STATE === 12);
      if (!allSubmit) return false;
      const ADEPTID = rows[0].ADEPTID;
      const userId = this.$store.state.user.userInfo.ID;
      return rows.every(r => r.ADEPTID === ADEPTID && r.CREATEID == userId);
    },

    // 撤销提交：全部 STATE=2
    ISSHOWRESUBMIT() {
      return this._allSelectedState(2);
    },

    // 更新模板：有选中行（提交人或查看变更时可用）
    ISSHOWUPDATE() {
      return (this.selectedRows || []).length > 0;
    },

    // 证书生成：全部 STATE=6（已审批）
    ISSHOWGENCERT() {
      return this._allSelectedState(6);
    },

    // 撤销签发：全部 STATE=10
    ISSHOWREGENCERT() {
      return this._allSelectedState(10);
    },

    // 电子签发：全部 STATE=10 且 ECERTSIGN!=1
    ISSHOWECERTSIGN() {
      const rows = this.selectedRows || [];
      if (!rows.length) return false;
      return rows.every(r => r.STATE === 10 && r.ECERTSIGN !== 1);
    },

    // 重置密码：全部 ECERTSIGN=1
    ISSHOWRESETPWD() {
      const rows = this.selectedRows || [];
      if (!rows.length) return false;
      return rows.every(r => r.ECERTSIGN === 1);
    },

    // 变更记录/证书预览：仅 1 行
    ISSHOWLOGLIST() {
      return (this.selectedRows || []).length === 1;
    },
    ISSHOWPRINTPREVIEW() {
      return (this.selectedRows || []).length === 1;
    },

    // 记录打印：1 行且 STATE=10
    ISSHOWPRINT() {
      const rows = this.selectedRows || [];
      if (rows.length !== 1) return false;
      return rows[0].STATE === 10;
    },

    // 记录下载：全部 STATE=10
    ISSHOWPDOWNLOAD() {
      return this._allSelectedState(10);
    },
  },

  data() {
    return {
      // 审核人选入（提交时）
      CHECKID: '',
      CHECKER: '',
      // 审批人选入（审核时）
      VERIFYID: '',
      VERIFYER: '',
      REMARK: '',
      // PDF 预览
      pdfSrc: '',
      // 电子签发密码
      ecertPwd: '',
      resetPwd: '',
    };
  },

  methods: {
    // ====== 辅助 ======
    _allSelectedState(state) {
      const rows = this.selectedRows || [];
      if (!rows.length) return false;
      return rows.every(r => r.STATE === state);
    },

    // 审核人选入器（提交时）
    async empSel1(INPUT, callback) {
      const INPUT_FINAL = this.CHECKER === INPUT ? '' : INPUT;
      await this.$callAction({
        action: this.moduleCode + '/empSel1',
        param: {
          INPUT: INPUT_FINAL,
          FUNCID: 'a94920a95a6946fca61bcb3421d16ff4', // 审核功能点 ID
          DEPTID: this.selectedRows[0].ADEPTID,
        },
        isBusy: false,
      });
      callback(this.$store.state[this.storeName].EMPUSER);
    },

    // 审批人选入器（审核时）
    async empSel2(INPUT, callback) {
      const INPUT_FINAL = this.VERIFYER === INPUT ? '' : INPUT;
      await this.$callAction({
        action: this.moduleCode + '/empSel1',
        param: {
          INPUT: INPUT_FINAL,
          FUNCID: '3be11623d4114bc68a8e63551e861ced', // 审批功能点 ID
          DEPTID: this.selectedRows[0].ADEPTID,
        },
        isBusy: false,
      });
      callback(this.$store.state[this.storeName].EMPUSER);
    },

    // ====== 批量操作 ======

    // 提交（选审核人）
    async batchSubmit() {
      if (!this.CHECKID) {
        this.$error('请选择审核人！');
        return;
      }
      await this.$callAction({
        action: this.moduleCode + '/batchCheck',
        param: {
          items: this.selectedRows,
          REMARK: '',
          VERIFYID: this.CHECKID,
          VERIFYER: this.CHECKER,
        },
        successText: '提交成功',
      });
      this.$refs.list.query(1);
    },

    // 撤销提交
    async batchReSubmit() {
      await this.$callAction({
        action: this.moduleCode + '/batchReCheck',
        param: { items: this.selectedRows, REMARK: '' },
        successText: '撤销成功',
      });
    },

    // 更新模板
    async batchUpdateTemplate() {
      await this.$callAction({
        action: this.moduleCode + '/batchUpdateTemplate',
        param: { items: this.selectedRows },
        successText: '更新成功',
      });
    },

    // ====== 审核操作（m021）======
    async batchCheck() {
      await this.$callAction({
        action: this.moduleCode + '/batchCheck',
        param: {
          items: this.selectedRows,
          REMARK: this.REMARK,
          VERIFYID: this.VERIFYID,
          VERIFYER: this.VERIFYER,
        },
        successText: '审核成功',
      });
    },
    async batchCheckReject() {
      await this.$callAction({
        action: this.moduleCode + '/batchCheckReject',
        param: { items: this.selectedRows, REMARK: this.REMARK },
        successText: '驳回成功',
      });
    },
    async batchReCheck() {
      await this.$callAction({
        action: this.moduleCode + '/batchReCheck',
        param: { items: this.selectedRows, REMARK: this.REMARK },
        successText: '撤销成功',
      });
    },

    // ====== 审批操作（m022/m023）======
    async batchVerify() {
      await this.$callAction({
        action: this.moduleCode + '/batchVerify',
        param: { items: this.selectedRows, REMARK: this.REMARK },
        successText: '审批成功',
      });
    },
    async batchVerifyReject() {
      await this.$callAction({
        action: this.moduleCode + '/batchVerifyReject',
        param: { items: this.selectedRows, REMARK: this.REMARK },
        successText: '驳回成功',
      });
    },
    async batchReVerify() {
      await this.$callAction({
        action: this.moduleCode + '/batchReVerify',
        param: { items: this.selectedRows },
        successText: '撤销成功',
      });
    },

    // ====== 签发操作（m023）======
    async batchGenCert() {
      await this.$callAction({
        action: this.moduleCode + '/batchGenCert',
        param: { items: this.selectedRows },
        successText: '签发成功',
      });
    },
    async batchReGenCert() {
      await this.$callAction({
        action: this.moduleCode + '/batchReGenCert',
        param: { items: this.selectedRows },
        successText: '撤销成功',
      });
    },

    // 电子签发
    async batchECertSign() {
      await this.$callAction({
        action: this.moduleCode + '/batchECertSign',
        param: { items: this.selectedRows, ECERTPWD: this.ecertPwd },
        successText: '电子签发成功',
      });
    },
    async batchResetPwd() {
      await this.$callAction({
        action: this.moduleCode + '/batchUpdateECertPwd',
        param: { items: this.selectedRows, ECERTPWD: this.resetPwd },
        successText: this.resetPwd ? '密码修改成功' : '密码已清除',
      });
    },

    // ====== 打印/下载/预览 ======

    // 记录打印（直接打开 EXPFILEID）
    print() {
      const item = (this.selectedRows || [])[0];
      if (!item) return this.$error('请选择！');
      if (!item.EXPFILEID) return this.$error('原始记录未生成！');
      this.pdfSrc = db.getUrl('pdf') + item.EXPFILEID;
      this.$refs.mpdf.show();
    },

    // 证书预览（A49 生成临时 PDF）
    async printPreview() {
      const item = (this.selectedRows || [])[0];
      if (!item) return this.$error('请选择！');
      const ret = await this.$callAction({
        action: this.moduleCode + '/printPreview',
        param: { ID: item.ID },
      });
      this.pdfSrc = db.getUrl('pdf') + ret;
      this.$refs.ppdf.show();
    },

    // 记录下载
    async download() {
      const ret = await this.$callAction({
        action: this.moduleCode + '/download',
        param: { items: this.selectedRows },
      });
      window.open(`${db.getUrl('upload')}${ret.ID}`, '_blank');
    },
  },
};
```

### 查询面板配置（16 字段）

路径：`@/modules/LI_M02/query-panel.vue`

```html
<template>
  <div slot="simple-query" v-if="qqryDt">
    <rs-meta-query-panel
      :path="qqryDt"
      module-code="LI_M02"
      :overrides="fieldOverrides"
      :cell-width="6"
      :show-buttons="false"
      @query="onQuery"
    />
    <div style="text-align:right; padding:4px 0;">
      <Button color="primary" @click="doSearch">查询</Button>
      <Button class="ml5" @click="doReset">重置</Button>
    </div>
  </div>
</template>

<script>
export default {
  props: { host: { type: Object, required: true } },
  computed: {
    qqryDt() {
      return this.host?.storeObj?.storeHelper.getTable('QQRY');
    },
    fieldOverrides() {
      return {
        // 16 个查询字段
        BILLDATE: { type: 'daterange' },          // 检校日期
        EXPDATE: { type: 'daterange' },           // 证书有效期
        STATE: {                                   // 状态
          type: 'select',
          datas: [
            { key: '', title: '全部' },
            { key: 1, title: '待提交' },
            { key: 2, title: '待审核' },
            { key: 5, title: '待审批' },
            { key: 12, title: '已驳回' },
            { key: 6, title: '已审批' },
            { key: 10, title: '已签发' },
            { key: 4, title: '已作废' },
          ],
        },
        // 其余字段（WTCODE/REFBILLCODE/CUSTNAME/MNAME/OPCODE/SIZETYPE/MANUFACTURER/
        //  TSTANDARDNAME/DOCTITLE/CERTCODE/CREATER/CHECKER/VERIFIER）
        // 默认 type: 'input'，由 rs-meta-query-panel 自动渲染
      };
    },
  },
  methods: {
    doSearch() { this.host.$refs.list.query(1); },
    doReset() {
      const dt = this.qqryDt;
      if (dt?.data[0]) {
        Object.keys(dt.data[0]).forEach(k => {
          if (!k.startsWith('_')) dt.data[0][k] = '';
        });
      }
      this.doSearch();
    },
  },
};
</script>
```

---

## 四、form.js 表单页扩展

路径：`@/modules/LI_M02/form.js`

```javascript
/**
 * 原始记录表单页扩展（整页 SFC）
 *
 * 关键设计：表单不走 generic-form，由 add.vue 整页 SFC 渲染。
 * 本扩展负责：
 * 1. 数据加载（onShow → open + openPTEMP + acceptSel）
 * 2. attach-flow-panel 三栏面板初始化
 * 3. rs-edit-item 动态字段渲染驱动
 * 4. 提交时审核人选入弹窗
 *
 * this 上下文（整页 SFC）:
 *   this.ID / this.STATE / this.CUSTNAME / this.PTEMPLATEID ...
 *   this.$MAIN / this.$DTSA / this.$DTSB / this.$DTSC / this.$DTSD
 *   this.inputObj / this.editorObj / this.tableObj （rs-edit-item 的字段引用）
 *   this.$refs.edit - rs-edit-item 引用
 *   this.$refs.editNoPanel - 无 PTEMPLATEID 时的 rs-edit-item
 */
export default {
  computed: {
    // ====== 按钮显隐 ======
    ISSHOWSAVE() {
      return ((!this.STATE && this.PTEMPLATEID) ||
        this.STATE === 1 || this.STATE === 12) &&
        (this.CREATEID == '' || this.CREATEID == this.$store.state.user.userInfo.ID);
    },
    ISSHOWSUBMIT() { /* 同 ISSHOWSAVE */ },
    ISSHOWDELETE() {
      return this.ID && (!this.STATE || this.STATE === 1 || this.STATE === 12) &&
        (this.CREATEID == '' || this.CREATEID == this.$store.state.user.userInfo.ID);
    },
    ISSHOWDINVALID() { return this.STATE === 10; },
    ISSHOWECERTSIGN() { return this.STATE === 10 && this.ECERTSIGN !== 1; },
    ISSHOWRESETPWD() { return this.ECERTSIGN === 1; },

    // 附件列表（DTSD → [{id, name}]）
    FILES: {
      get() {
        return (this.DTSD || []).map(d => ({ id: d.FILEID, name: d.FILENAME }));
      },
      set(files) {
        this.$store.commit(this.storeName + '/SETFILEDATA', { files: files || [] });
      },
    },

    // REFTPMDATA（rs-edit-item 的 layouts 数据源）
    REFTPMDATA() {
      return this.$MAIN?.getValue('REFTPMDATA') || [];
    },
  },

  data() {
    return {
      // rs-edit-item 字段引用
      inputObj: {},
      editorObj: [],
      tableObj: {},
      fieldsConfig: [],
      // 受理单列表相关
      selected: '8',  // '7'待受理 / '8'待检验
      INPUT: '',
      currentItem: {},
      // 选审核人/审批人
      CHECKID: '', CHECKER: '',
      VERIFYID: '', VERIFYER: '',
      REMARK: '',
      // 电子签发
      ecertPwd: '', resetPwd: '',
      // 布局
      isWideLayout: false,
    };
  },

  methods: {
    // ====== 1. 初始化 rs-edit-item 的字段配置 ======
    // 遍历 REFTPMDATA 树，收集 inputObj/editorObj/tableObj
    dealConfigSelect(nodes) {
      nodes.forEach(n => {
        if (n.path === 'REGUITEM') {
          n.fieldProps = n.fieldProps || {};
          n.fieldProps.option = this.reguitemParam;
        }
        if (n.field) this.inputObj[n.field] = n;
        if (n.sourceName) {
          this.tableObj[n.sourceName] = n;
          n.value = [];
        }
        if (n.type === 'itemEditor') this.editorObj.push(n);
        if (n.children?.length) this.dealConfigSelect(n.children);
      });
    },

    initTree() {
      this.fieldsConfig = this.REFTPMDATA || [];
      this.tableObj = {};
      this.inputObj = {};
      this.editorObj = [];
      this.dealConfigSelect(this.REFTPMDATA || []);
      this.$forceUpdate();
    },

    // ====== 2. onShow - 表单打开入口 ======
    async onShow() {
      if (this._onShowLoading) return;
      this._onShowLoading = true;
      try {
        if (this.ID) {
          // 编辑模式：打开数据 + 加载模板
          await this.$callAction({
            action: this.storeName + '/open',
            param: { ID: this.ID },
            isBusy: false,
          });
          await this.showByTemplate();
        } else {
          // 新建模式：初始化 + 加载受理单列表
          await this.$callAction({ action: this.storeName + '/add', param: {}, isBusy: false });
          await this.$callAction({
            action: this.storeName + '/acceptSel',
            param: { INPUT: '', STATE: this.selected },
            isBusy: false,
          });
        }
      } finally {
        this._onShowLoading = false;
      }
    },

    // ====== 3. 加载模板并回填 ======
    async showByTemplate() {
      await this.$callAction({
        action: this.storeName + '/openPTEMP',
        param: { ID: this.PTEMPLATEID, ISEDIT: true },
        isBusy: false,
      });
      this.initTree();
      // 回填 rs-edit-item
      this.$store.commit(this.storeName + '/SETSHOWTPMDATA', {
        inputObj: this.inputObj,
        tableObj: this.tableObj,
        editorObj: this.editorObj,
      });
      // 强制 UEditor 刷新
      this.editorObj.forEach(n => { n.value += ' '; });
      this.$forceUpdate();
    },

    // ====== 4. 受理单操作 ======
    async accept(item) {
      await this.$callAction({
        action: this.storeName + '/accept',
        param: { items: [item] },
        successCall: async () => {
          this.currentItem = item;
          item.ADEPTID = this.$store.state.user.userInfo.DEPTID;
          if (item.PTEMPLATEID) {
            await this.$callAction({
              action: this.storeName + '/openPTEMP',
              param: { ID: item.PTEMPLATEID, item },
              isBusy: false,
            });
            this.initTree();
            this.$store.commit(this.storeName + '/SETSHOWTPMDATA', {
              inputObj: this.inputObj,
              tableObj: this.tableObj,
              editorObj: this.editorObj,
            });
          }
        },
      });
    },

    // ====== 5. 保存/提交 ======
    save() {
      this.$callAction({
        action: this.storeName + '/doMySave',
        param: {
          inputObj: this.inputObj,
          tableObj: this.tableObj,
          editorObj: this.editorObj,
        },
        successText: '保存成功',
        isSuccessBack: true,
      });
    },

    submit() {
      if (!this.CHECKID) return this.$error('请选择审核人！');
      this.$callAction({
        action: this.storeName + '/doMySubmit',
        param: {
          inputObj: this.inputObj,
          tableObj: this.tableObj,
          editorObj: this.editorObj,
        },
        successText: '提交成功',
        isSuccessBack: true,
      });
    },

    // 提交并继续：成功后不清表单，重新加载受理单
    async submit2() {
      if (!this.CHECKID) return this.$error('请选择审核人！');
      await this.$callAction({
        action: this.storeName + '/doMySubmit',
        param: {
          inputObj: this.inputObj,
          tableObj: this.tableObj,
          editorObj: this.editorObj,
        },
        successText: '提交成功',
        isSuccessBack: false,
        successCall: async () => {
          await this.$callAction({ action: this.storeName + '/add', param: {} });
          await this.$callAction({
            action: this.storeName + '/acceptSel',
            param: { INPUT: '', STATE: this.selected },
          });
        },
      });
    },

    // ====== 6. 选择器加载方法 ======
    async empSel1(INPUT, callback) { /* FUNCID=审核 */ },
    async empSel2(INPUT, callback) { /* FUNCID=审批 */ },
    async tstddSel(INPUT, callback) { /* 检校依据 */ },
    async reguitemSel(INPUT, callback) { /* 检定项目 */ },

    // ====== 7. 标准器选入回调 ======
    onSelectArd(items) {
      const titems = this.tableObj['VBS_ARD_4TPL'].value || [];
      items.forEach(item => {
        if (!titems.find(i => i.ID == item.ID)) titems.push(item);
      });
      this.tableObj['VBS_ARD_4TPL'].value = titems;
      this.$forceUpdate();
    },

    // ====== 8. 宽屏检测（三栏布局）======
    checkWideLayout() {
      this.isWideLayout = window.innerWidth >= 1280;
    },
  },

  mounted() {
    this.checkWideLayout();
    this._resizeHandler = () => this.checkWideLayout();
    window.addEventListener('resize', this._resizeHandler);
  },
  beforeDestroy() {
    window.removeEventListener('resize', this._resizeHandler);
  },
};
```

### attach-flow-panel 三栏面板说明

| 栏位 | 宽度 | 内容 | 数据源 |
|------|------|------|--------|
| 左：附件 | 200px | 图片网格 + 文件列表 + 上传 | `files` prop（DTSD） |
| 中：表单 | flex | rs-edit-item（动态字段） | `<slot>` 默认插槽 |
| 右：审批 | 200px | 时间轴 + 状态点 + 备注 | `logs` prop（DTSC） |

宽屏 (>=1280px) 三栏；窄屏上下堆叠。

---

## 五、整页 SFC 组件清单

### 5.1 add.vue（完整表单页，最大资产）

路径：`@/modules/LI_M02/add.vue`

完整替代原 `views/add.vue`（732 行）。核心结构：

```html
<template>
  <view-dialog :title="title">
    <div slot="body">
      <div class="rr-flex-row" style="background:#eee;">
        <!-- 左：受理单列表（新建模式，未选模板时） -->
        <div class="left-list" v-if="!ID && !PTEMPLATEID">
          <Search v-model="INPUT" @search="query" />
          <Tabs :datas="param" v-model="selected" @change="query" />
          <rs-table-list :datas="ACCEPT" :path="$ACCEPT" border ref="table">
            <TableItem title="操作" :width="120" fixed="right">
              <template slot-scope="{data}">
                <Poptip v-if="selected=='7'" @confirm="accept(data)">
                  <button class="h-btn h-btn-s h-btn-blue">受理检验</button>
                </Poptip>
                <button v-if="selected=='8'" @click.stop="clickRow(data)">检验</button>
                <Poptip v-if="selected=='8'" @confirm="reAccept(data)">
                  <button class="h-btn h-btn-s h-btn-red">撤销</button>
                </Poptip>
              </template>
            </TableItem>
          </rs-table-list>
        </div>

        <!-- 右：表单编辑区（选了模板后） -->
        <div class="edit rr-scroll-bar" :class="{'rr-wide-mode': isWideLayout}" v-show="ID || PTEMPLATEID">
          <!-- 三栏面板（含附件+表单+审批） -->
          <attach-flow-panel
            :files="FILES"
            :logs="DTSC"
            :wide="isWideLayout"
            :readonly="!ISSHOWSAVE"
            :canUpload="!ID || PTEMPLATEID"
            v-show="PTEMPLATEID"
            @remove="removeFile"
          >
            <rs-edit-item
              ref="edit"
              :layouts="REFTPMDATA"
              :select="{}"
              :parent="-1"
              :inLayout="false"
              @clickAtion="clickAtion"
            />
          </attach-flow-panel>
          <!-- 无模板时独立渲染 -->
          <rs-edit-item
            v-if="!PTEMPLATEID"
            ref="editNoPanel"
            :layouts="REFTPMDATA"
            @clickAtion="clickAtion"
          />
        </div>
      </div>

      <!-- 标准器选入弹窗 -->
      <rs-modal ref="madd"><ard-sel @on-select="onSelectArd" /></rs-modal>
      <!-- 模板选入弹窗 -->
      <rs-modal ref="mtmp"><tmp-sel @on-select="onSelectTmp" :item="currentItem" /></rs-modal>
      <!-- 电子签发密码弹窗 -->
      <rs-modal ref="mECertPwd">
        <input type="password" v-model="ecertPwd" />
        <Button @click="confirmECertSign">确认签发</Button>
      </rs-modal>
    </div>

    <!-- 底部按钮（由 EXTENDJS 的 ISSHOWXXX 控制显隐） -->
    <template slot="footer">
      <Button v-if="ISSHOWSAVE" @click="save">暂存</Button>
      <Poptip v-if="ISSHOWDELETE" @confirm="del"><Button color="red">删除</Button></Poptip>
      <!-- 提交按钮（Tooltip 内嵌审核人 AutoComplete） -->
      <Tooltip v-if="ISSHOWSUBMIT" ref="submitTip" trigger="click" editable>
        <Button color="primary">提交</Button>
        <div slot="content">
          <AutoComplete :option="empParam1" v-model="CHECKID"
            @change="v => CHECKER = v.title" />
          <Button color="primary" @click="submit">确定提交</Button>
        </div>
      </Tooltip>
      <!-- 提交并继续 -->
      <Tooltip v-if="ISSHOWSUBMIT" ref="submitTip2" trigger="click" editable>
        <Button color="primary">提交并继续</Button>
        <div slot="content">
          <AutoComplete v-model="CHECKID" />
          <Button @click="submit2">确定提交</Button>
        </div>
      </Tooltip>
      <Poptip v-if="ISSHOWRESUBMIT" @confirm="reSubmit(ID)">
        <Button color="red">撤销提交</Button>
      </Poptip>
    </template>
  </view-dialog>
</template>
```

### 5.2 query-panel.vue（查询面板）

见上文 16 字段配置，6 个字段一行。

### 5.3 footer-actions.vue（底部按钮区）

```html
<template>
  <div slot="footer-action">
    <!-- main 页: 添加/变更记录/提交(选审核人)/撤销提交/更新模版/证书预览 -->
    <!-- m021 页: 变更记录/审核(选审批人)/驳回/撤销审核/证书预览 -->
    <!-- m022 页: 变更记录/审批/驳回/撤销审批/证书生成/证书预览 -->
    <!-- m023 页: 变更记录/记录签发/撤销签发/电子签发/重置密码/证书预览 -->
    <!-- m024 页: 变更记录/记录打印/记录下载/证书预览 -->
    <Button color="primary" v-per="PERM_ADD" @click="add">添加</Button>
    <Button v-if="host.ISSHOWLOGLIST" @click="showLogList">查看变更记录</Button>
    <Tooltip v-if="host.ISSHOWSUBMIT" trigger="click" editable>
      <Button color="primary">提交</Button>
      <div slot="content">
        <AutoComplete v-model="host.CHECKID" :option="host.empParam1" />
        <Button @click="host.batchSubmit">确定</Button>
      </div>
    </Tooltip>
    <!-- ...其他按钮 -->
  </div>
</template>
```

### 5.4 table-actions.vue（行操作列）

m02/add 页的左侧受理单列表行操作。

### 5.5 ardSel.vue（标准器选入弹窗）

完整迁移原 `views/ardSel.vue`（68 行）。核心：
- `ardSel()` action 查询标准器
- `rs-table-list` 多选 + 确定 `on-select` emit

### 5.6 tmpSel.vue / tmpSel2.vue（模板选入弹窗）

完整迁移原 `views/tmpSel.vue` / `tmpSel2.vue`（~70 行）。

### 5.7 attach-flow-panel.vue（附件+审批流三栏面板）

完整迁移原 `views/attach-flow-panel.vue`（319 行）。核心：
- 宽屏 (>=1280px) 三栏：附件左 / 表单中 / 审批右
- 窄屏上下堆叠
- 图片悬浮大图 / 视频预览弹窗
- 审批时间轴（状态点颜色映射）

### 5.8 logList.vue（变更记录列表）

完整迁移原 `views/logList.vue`（56 行）。核心：
- `queryLog(ID)` action 查询
- OPLOGS DataTable 展示

---

## 六、变体页面差异化配置

| 配置项 | m02 (原始记录) | m021 (审核) | m022 (审批) | m023 (签发) | m024 (查询下载) |
|--------|---------------|-------------|-------------|-------------|-----------------|
| **QUERY_APICODE** | A01 | A34 | A36 | A40 | A41 |
| **ADVQUERY_APICODE** | - | A35 | A37 | A42 | A46 |
| **状态选项** | 全部(1/2/5/12/6/10/4) | 待审核(2)/已审核(3) | 待审批(5)/已审批(6) | 待签发(6)/已签发(10) | 全部 |
| **EXTENDJS** | main.js | m021.js | m022.js | m023.js | m024.js |
| **footer SLOTS** | footer-actions.vue | m021-footer.vue | m022-footer.vue | m023-footer.vue | m024-footer.vue |
| **主要按钮** | 添加/提交/撤销提交/更新模板/证书预览 | 审核/驳回/撤销审核/证书预览 | 审批/驳回/撤销审批/证书生成/证书预览 | 记录签发/撤销签发/电子签发/重置密码/证书预览 | 记录打印/记录下载/证书预览 |
| **Store** | 复用 LI_M02 store.js | 复用 | 复用 | 复用 | 复用 |
| **FUNCID（选员工）** | - | 审核员 a949... | 审批员 3be1... | - | - |

### 变体页面 footer SFC 差异

```javascript
// m021-footer.vue（审核页特有）
// - 审核按钮（Tooltip 内嵌审批人 AutoComplete）
// - 驳回按钮（Poptip 确认）
// - 撤销审核

// m022-footer.vue（审批页特有）
// - 审批按钮（Tooltip 内嵌 Remark textarea）
// - 驳回按钮
// - 撤销审批
// - 证书生成（STATE=6 时显示）

// m023-footer.vue（签发页特有）
// - 记录签发（A27）
// - 撤销签发（A50）
// - 电子签发（A55，含密码弹窗）
// - 重置密码（A58，含密码弹窗）

// m024-footer.vue（查询下载页特有）
// - 记录打印（A38，直接打开 EXPFILEID）
// - 记录下载（A39）
```

---

## 七、迁移对照表

| 原 r01/m02 文件 | 行数 | 迁移后 | 说明 |
|----------------|------|--------|------|
| `router.js` (5 路由) | 123 | 不需要 | generic-module 自动注册路由 |
| `store.js` (主) | 12 | 不需要 | generic-store.js 自动注册 |
| `store1.js` (m021) | 24 | 不需要 | PAGECONFIG.QUERY_APICODE 替代 |
| `store2.js` (m022) | 24 | 不需要 | 同上 |
| `store3.js` (m023) | 24 | 不需要 | 同上 |
| `store4.js` (m024) | 24 | 不需要 | 同上 |
| `baseStore.js` | 521 | `@/modules/LI_M02/store.js` | 保留全部 30+ actions + 7 mutations |
| `index.js` | 3 | 不需要 | - |
| `index1.js`~`index4.js` | 4×3 | 不需要 | - |
| `views/main.vue` | 464 | m18 配置 + `main.js` + `query-panel.vue` + `footer-actions.vue` | 列表模板配置化 |
| `views/main1.vue` | 320 | m021 配置 + `m021.js` + `m021-footer.vue` | 审核变体 |
| `views/main2.vue` | 304 | m022 配置 + `m022.js` + `m022-footer.vue` | 审批变体 |
| `views/main3.vue` | 406 | m023 配置 + `m023.js` + `m023-footer.vue` | 签发变体（含电子签发） |
| `views/main4.vue` | 324 | m024 配置 + `m024.js` + `m024-footer.vue` | 查询下载变体 |
| `views/add.vue` | 732 | `@/modules/LI_M02/add.vue` (整页 SFC) + `form.js` | 表单整页 SFC |
| `views/add1.vue` | 595 | 复用 add.vue + m021 角色扩展 | 审核表单 |
| `views/add2.vue` | 556 | 复用 add.vue + m022 角色扩展 | 审批表单 |
| `views/add3.vue` | 637 | 复用 add.vue + m023 角色扩展（电子签发按钮） | 签发表单 |
| `views/add4.vue` | 562 | 复用 add.vue + m024 角色扩展 | 查询表单 |
| `views/ardSel.vue` | 68 | `@/modules/LI_M02/ardSel.vue` | 标准器选入弹窗 |
| `views/tmpSel.vue` | 68 | `@/modules/LI_M02/tmpSel.vue` | 模板选入弹窗（窄屏） |
| `views/tmpSel2.vue` | 73 | `@/modules/LI_M02/tmpSel2.vue` | 模板选入弹窗（宽屏） |
| `views/attach-flow-panel.vue` | 319 | `@/modules/LI_M02/attach-flow-panel.vue` | 三栏面板 |
| `views/logList.vue` | 56 | `@/modules/LI_M02/logList.vue` | 变更记录弹窗 |
| `mapDateTable('QQRY', [...16])` | - | rs-meta-query-panel 自动处理 | 查询字段配置化 |
| `mapDateTable('MAIN', [...15])` | - | 整页 SFC 内 mapDateTable | 表单字段绑定 |
| `ISSHOWXXX` computed | - | 扩展JS computed | 按钮显隐逻辑 |
| `List01` mixin | - | generic-module 内置 | 列表通用逻辑 |
| `Add01` mixin | - | 整页 SFC 内联 | 表单通用逻辑 |
| **合计** | **5484** | **~2500 行 SFC 资产** | **代码量减半** |

---

## 八、迁移步骤（分阶段）

### 阶段 1：数据库配置（m18 可视化）

1. **创建 6 个 tss_module_page 记录**（main/add/m021/m022/m023/m024）
2. **配置 PAGECONFIG**（6 份 JSON，含 SFCMODULEPATH 指向整页 SFC）
3. **创建 tss_module_button 记录**（6 套按钮配置，按角色差异化）
4. **tss_resuipc 字段配置**（QQRY 16 查询字段 + MAIN 主表字段）
5. **tss_func 菜单 OUTERURL 改为** `/g/LI_M02/main`、`/g/LI_M02/m021` 等

### 阶段 2：Store 扩展

6. **在 m17 创建 `@/modules/LI_M02/store.js`**
7. **迁移 baseStore.js 的 30+ actions**（保留 `/api/rm11/call` apiPath）
8. **迁移 7 个 mutations**（SETTPMDATA/SETSAVEDATA/SETSHOWTPMDATA/SETLOGDATA/SETFILEDATA + INIT/M_INITDATA）
9. **保留 dealTreeData 辅助函数**（REFTPMDATA 树解析）
10. **保留 oSelStore.mixActions()**（acceptSel/ardSel/ptmpSel/empSel1/tstddSel/reguitemSel）
11. **通过 m18 "模块脚本" 关联 store.js 到 LI_M02**

### 阶段 3：整页 SFC 资产

12. **创建 `add.vue`**（整页表单，~700 行，含受理单列表+编辑区+三栏面板+底部按钮）
13. **创建 `form.js`**（表单扩展，~300 行，含 onShow/initTree/save/submit/选择器）
14. **迁移 `ardSel.vue`**（68 行，几乎原样）
15. **迁移 `tmpSel.vue` / `tmpSel2.vue`**（~70 行，几乎原样）
16. **迁移 `attach-flow-panel.vue`**（319 行，几乎原样）
17. **迁移 `logList.vue`**（56 行，几乎原样）

### 阶段 4：列表页 SFC 资产

18. **创建 `main.js`**（列表扩展，~400 行，含全部按钮显隐+批量操作+打印下载）
19. **创建 `query-panel.vue`**（16 查询字段，~100 行）
20. **创建 `footer-actions.vue`**（main 页底部按钮，~150 行）
21. **创建 `table-actions.vue`**（行操作，~50 行）

### 阶段 5：变体页面

22. **创建 `m021.js` + `m021-footer.vue`**（审核页扩展+底部按钮）
23. **创建 `m022.js` + `m022-footer.vue`**（审批页）
24. **创建 `m023.js` + `m023-footer.vue`**（签发页，含电子签发密码弹窗）
25. **创建 `m024.js` + `m024-footer.vue`**（查询下载页）

### 阶段 6：联调验证

26. **逐页面验证**
    - `/g/LI_M02/main`：列表查询/添加/提交/撤销提交/更新模板/证书预览
    - `/g/LI_M02/add`：受理单选入/模板加载/字段编辑/标准器选入/保存/提交
    - `/g/LI_M02/m021`：审核/驳回/撤销审核
    - `/g/LI_M02/m022`：审批/驳回/撤销审批/证书生成
    - `/g/LI_M02/m023`：记录签发/撤销签发/电子签发/重置密码
    - `/g/LI_M02/m024`：记录打印/记录下载/证书预览

27. **验证审批流状态机**
    - 新建 → 提交(1→2) → 审核(2→3→5) → 审批(5→6) → 签发(6→10) → 电子签发
    - 撤销链路：撤销提交/撤销审核/撤销审批/撤销签发

28. **验证跨模块调用**
    - 受理单 ACCEPT 查询
    - 标准器 ARD 查询
    - 模板 PTMP 查询
    - 员工 EMPUSER 查询（按 FUNCID + DEPTID）

### 阶段 7：清理

29. **删除原 `src/pages/r01/m02/` 目录**（5484 行代码下线）
30. **移除 router.js 中的 5 条路由**（自动注册接管）
31. **移除 `src/pages/r01/m02/mixins/` 引用**

---

## 九、关键风险与注意事项

### 9.1 apiPath 差异

**原代码混用两个 apiPath**：
- `/api/rm11/call`（RM11Controller，主接口）：A12-A58 自定义审批流/证书/签发
- `/api/data/call`（DataController，标准 CRUD）：A08 模板/A31 变更记录/A51 附件

**迁移策略**：store.js 中保留 `apiPath: '/api/rm11/call'` 作为默认值，特定 action（openPTEMP/queryLog）显式指定 `apiPath: '/api/data/call'`。

### 9.2 rs-edit-item 动态字段

rs-edit-item 的 layouts 来自 REFTPMDATA（模板配置的 JSON 树），**不是** tss_resuipc 配置。这意味着：
- 字段渲染完全由模板数据驱动
- dealConfigSelect 递归遍历整棵树收集 inputObj/editorObj/tableObj
- SETSAVEDATA 和 SETSHOWTPMDATA 必须保留完整的数据组装逻辑

### 9.3 SelStore 依赖

baseStore.js 顶部有 `new SelStore()`，mixActions/mixPaths 依赖 RS_M00 模块加载完成。
**迁移后**：generic-store.js 的 `applyStoreExtend('LI_M02')` 会在 app store 加载后执行，避免 SelStore 时序问题。

### 9.4 UEditor 字段

editorObj 中的 itemEditor 类型字段使用 UEditor 富文本编辑器。
**迁移后**：UEditor 组件需保留在整页 SFC 中，`sessionStorage.setItem('hlims_ueditor_fmFields', ...)` 机制不变。

### 9.5 电子签发密码弹窗

m023 签发页有特殊的密码输入弹窗（mECertPwd / mResetPwd），**不是标准 Poptip/Tooltip**。
**迁移后**：在 `m023-footer.vue` 中用 rs-modal + 密码输入框实现，`confirmECertSign` 触发 `batchECertSign` action。

### 9.6 受理单列表（左侧栏）

add.vue 新建模式下，左侧显示受理单列表（ACCEPT DataTable），可受理/撤销受理。
**这是 LI_M02 特有的"先选受理单再编辑"流程**，不能删除。整页 SFC 必须保留此双栏布局。

---

## 十、迁移后目录结构

```
src/modules/LI_M02/               # SFC 扩展资产（数据库 tss_code_asset）
  store.js                        # Store 扩展（30+ actions + 7 mutations）
  main.js                         # 列表页扩展（按钮显隐 + 批量操作 + 打印下载）
  form.js                         # 表单页扩展（onShow + initTree + save/submit）
  m021.js                         # 审核页扩展（审核/驳回/撤销审核）
  m022.js                         # 审批页扩展（审批/驳回/撤销审批）
  m023.js                         # 签发页扩展（签发/电子签发/重置密码）
  m024.js                         # 查询下载页扩展（打印/下载/预览）
  add.vue                         # 完整表单页 SFC（受理单列表+编辑区+三栏面板）
  query-panel.vue                 # 16 字段查询面板
  footer-actions.vue              # main 页底部按钮
  m021-footer.vue                 # m021 审核页底部按钮
  m022-footer.vue                 # m022 审批页底部按钮
  m023-footer.vue                 # m023 签发页底部按钮
  m024-footer.vue                 # m024 查询下载页底部按钮
  table-actions.vue               # 行操作列
  ardSel.vue                      # 标准器选入弹窗
  tmpSel.vue                      # 模板选入弹窗（窄屏）
  tmpSel2.vue                     # 模板选入弹窗（宽屏）
  attach-flow-panel.vue           # 附件+审批流三栏面板
  logList.vue                     # 变更记录弹窗
```

原 `src/pages/r01/m02/` 目录（5484 行）可全部删除，路由通过菜单 `tss_func.OUTERURL = /g/LI_M02/main` 等自动注册。

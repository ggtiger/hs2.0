import heyui from 'heyui';
import { getSelType, buildAutoCompleteOption } from '@/utils/selRegistry';
export default {
  getFormFields(items, formProps) {
    let titems = [];
    if (!items || !Array.isArray(items)) return titems;
    items.sort((a, b) => a.EDITSORT - b.EDITSORT).map((item, index) => {
      if (!item['RESFIELDNAME'] && !item["FIELDNAME"])
        return;
      // EDITSORT 为 0 或 NULL 的字段不渲染到表单
      if (!item['EDITSORT'] || +item['EDITSORT'] <= 0)
        return;
      let titem = {};
      let props = {};
      let on = {};
      let formItemProps = {};
      let cellProps = {};
      let cellOn = {};
      props['type'] = item['EDITTYPE'];
      props['key'] = item['RESFIELDNAME'] || item["FIELDNAME"];
      props['nullable'] = +item['NULLABLE'];
      // autocomplete/treepicker/fileupload/imageupload 需要联动写入多个字段
      // 格式同 rs-table-cell：本地字段,远程字段;本地字段,远程字段
      if (item['UPDATEFIELDS']) {
        props['updateFields'] = item['UPDATEFIELDS'];
      }
      formItemProps['prop'] = props['key'];
      formItemProps['label'] = item['LABELNAME'];
      formItemProps['showLabel'] = !!item['LABELNAME'];
      formItemProps['required'] = +item['NULLABLE'] !== 1;
      cellProps['disabled'] = item['EDITABLE'] == 0 ? true : false;
      if (item['PLACEHOLDER']) {
        cellProps['placeholder'] = item['PLACEHOLDER'];
      }
      // 显隐 + 权限：透传给 rs-form-cell（空值不生效）
      if (item['VISIBLEIF']) props['visibleIf'] = item['VISIBLEIF'];
      if (item['PERCODE']) cellProps['perCode'] = item['PERCODE'];
      props['dict'] = item['SELECTDATA'];
      if (!item['SHOWLENGTH']) {
        formItemProps['single'] = true;
      }
      // COLSPAN>=2 → 独占整行（HeyUI Form 列模式下 FormItem single=true）
      if (+item['COLSPAN'] >= 2) {
        formItemProps['single'] = true;
      }
      props.formItemProps = formItemProps;
      if ('select' === props['type']) {
        try {
          const parsed = JSON.parse(item['SELECTDATA']);
          // {dict:..., items:[...]} 格式：从字典过滤
          if (parsed && typeof parsed === 'object' && parsed.dict) {
            const dictData = heyui.getDict(parsed.dict) || {};
            const items = parsed.items || [];
            if (items.length > 0) {
              const keySet = new Set(items);
              cellProps['datas'] = Object.keys(dictData)
                .filter(k => keySet.has(k))
                .map(k => ({ key: k, title: dictData[k] }));
            } else {
              cellProps['dict'] = parsed.dict;
            }
          } else {
            cellProps['datas'] = parsed;
          }
        } catch (e) {
          // 非合法 JSON，尝试 key:title,key2:title2 文本格式
          if (typeof item['SELECTDATA'] === 'string' && item['SELECTDATA'].indexOf(':') > 0) {
            cellProps['datas'] = item['SELECTDATA'].split(',').map(seg => {
              const [k, title] = seg.split(':');
              return { key: (k || '').trim(), title: (title || k || '').trim() };
            });
          } else {
            // 否则当作已注册的字典名
            cellProps['dict'] = item['SELECTDATA'];
          }
        }
      }
      if ('number' === props['type']) {
        cellProps['precision'] = +item['REFFIELDPREC'];
      }
      // autocomplete / treepicker：SELECTDATA 存选择器配置，支持两种格式：
      //   预设：{"selType":"dept"} 或字符串 "dept"
      //   自定义：{"module":"RS_M00","apiCode":"A05","keyName":"ID","titleName":"DEPTNAME"}
      // 真正的 option（含 loadData 函数）由 rs-form-edit 在渲染前通过 selRegistry 注入。
      if ('autocomplete' === props['type'] || 'treepicker' === props['type'] || 'multiautocomplete' === props['type']) {
        cellProps['selConfig'] = item['SELECTDATA'];
        // 解析出 titleName 给 rs-form-cell 的 item slot 用
        try {
          const parsed = JSON.parse(item['SELECTDATA']);
          if (parsed && typeof parsed === 'object') {
            if (parsed.titleName) cellProps['titleName'] = parsed.titleName;
            if (parsed.keyName) cellProps['keyName'] = parsed.keyName;
          }
        } catch (e) {
          // 字符串格式：从预设查 titleName
          const preset = getSelType(item['SELECTDATA'], true);
          if (preset) cellProps['titleName'] = preset.titleName;
        }
      }
      // toolbar：分组标题（纯显示），占整行、不参与校验/取值
      if ('toolbar' === props['type']) {
        formItemProps['single'] = true;
        formItemProps['required'] = false;
        formItemProps['showLabel'] = false;
      }
      // tableblock：分组标题 + 可编辑子表（增删移按钮 + rs-table-edit）
      // SELECTDATA: {subtable, targetModule, showButtons:{add,remove,up,down}, buttons:[{label,code,per}]}
      // subtable 默认=字段名；列配置复用子表 scm（Gen.getTableColumns）
      if ('tableblock' === props['type']) {
        formItemProps['single'] = true;
        formItemProps['required'] = false;
        formItemProps['showLabel'] = false;
        let tbc = {};
        try { tbc = JSON.parse(item['SELECTDATA']) || {}; } catch (e) {}
        // 按钮统一迁移到 tss_module_button(BTNAREA=subtable), 这里只保留子表归属
        cellProps['tableBlockConfig'] = {
          subtable: tbc.subtable || props['key'],
          targetModule: tbc.targetModule || '',
        };
      }
      // multiautocomplete：多选自动完成，SELECTDATA 同 autocomplete，
      // 额外 mode(subtable|field)：subtable=选中项映射成子表行；field=选中项 key 拼逗号id 存单字段
      // subtable 模式 subMappings 形如 "ACCEPTID,ID;ACCEPTCODE,BILLCODE"（子表字段,远程字段；同 UPDATEFIELDS）
      if ('multiautocomplete' === props['type']) {
        formItemProps['single'] = true;
        let mc = {};
        try { mc = JSON.parse(item['SELECTDATA']) || {}; } catch (e) {}
        cellProps['multSelConfig'] = {
          mode: mc.mode || 'subtable',
          subtable: mc.subtable || props['key'],
          field: mc.field || props['key'],
          subMappings: mc.subMappings || '',
        };
      }
      // fileupload / imageupload：解析上传配置
      // SELECTDATA 支持：{multifile:true} 单字段逗号id / {mode:'subtable',subtable:'DTS',subMappings:'FILEID,id;FILENAME,name'} 子表绑定
      if ('fileupload' === props['type'] || 'imageupload' === props['type']) {
        let upCfg = {};
        try {
          upCfg = JSON.parse(item['SELECTDATA']) || {};
        } catch (e) {}
        cellProps['uploaderOptions'] = upCfg;
        // subtable 模式：每文件=子表一行
        if (upCfg.mode === 'subtable') {
          formItemProps['single'] = true;
          cellProps['uploadSubtableConfig'] = {
            subtable: upCfg.subtable || props['key'],
            subMappings: upCfg.subMappings || '',
          };
        }
      }
      // code：SELECTDATA 存语言类型配置（如 {"language":"sql"}）
      if ('code' === props['type']) {
        formItemProps['single'] = true;
        try {
          const parsed = JSON.parse(item['SELECTDATA']);
          if (parsed && parsed.language) cellProps['language'] = parsed.language;
        } catch (e) {}
      }
      // fileuploadtpl：文件上传+模板选择
      // SELECTDATA 存 {"templateType":"YSJL","moduleCode":"R01_M01","maxFileSize":"10mb","showSelect":true}
      if ('fileuploadtpl' === props['type']) {
        formItemProps['single'] = true;
        let tplCfg = {};
        try {
          tplCfg = JSON.parse(item['SELECTDATA']) || {};
        } catch (e) {}
        cellProps['uploaderTplConfig'] = tplCfg;
      }

      props.cellProps = cellProps;
      props.cellOn = cellOn;
      titem.props = props;
      titems.push(titem);
    });
    return titems;
  },
  getTableColumns(items, tableProps) {
    tableProps = tableProps || {};
    let titems = [];
    if (!items || !Array.isArray(items)) return titems;
    // DISPLAYINLIST 过滤（混合模式）：
    // 资源上有任意非 null DISPLAYINLIST 值时走新逻辑（按 DISPLAYINLIST === 1 过滤）；
    // 全部为 null/undefined 时回退到老逻辑（SHOWLENGTH != "0"），保证迁移后行为不变。
    const useDisplayFlag = items.some(it => it['DISPLAYINLIST'] !== null && it['DISPLAYINLIST'] !== undefined);
    // 列表排序：LISTSORT 优先，LISTSORT 为空时按 ENTRYNUM（保留 UI 设置顺序）
    const orderedItems = items.slice().sort((a, b) => {
      const la = +a['LISTSORT'] || 0;
      const lb = +b['LISTSORT'] || 0;
      if (la && lb) return la - lb;
      if (la) return -1;
      if (lb) return 1;
      return (+a['ENTRYNUM'] || 0) - (+b['ENTRYNUM'] || 0);
    });
    orderedItems.map((item, index) => {
      // pageaction：列表页全局按钮，不生成表格列，单独收集到 pageActions
      if (item['EDITTYPE'] === 'pageaction') {
        let acts = [];
        (item['ACTIONCODE'] || '').split('|').map(d => {
          let ad = d.split(':');
          if (ad[1]) {
            acts.push({
              label: ad[0],
              code: ad[1].split(',')[0],
              per: ad[1].split(',')[1],
              visibleIf: item['VISIBLEIF'] || '',
              perCode: item['PERCODE'] || '',
            });
          }
        });
        titems.push({ type: 'pageaction', key: item['RESFIELDNAME'] || ('pa' + index), pageActions: acts });
        return;
      }
      if (useDisplayFlag) {
        if (+item['DISPLAYINLIST'] !== 1) {
          return;
        }
      } else if (item['SHOWLENGTH'] + "" === "0") {
        return;
      }
      let titem = {};
      titem['type'] = item['EDITTYPE'];
      titem['key'] = item['RESFIELDNAME'];
      if ('index' === titem['type']) {
        titem['key'] = '$serial';
      }
      titem['prop'] = titem['key'];
      titem['title'] = item['LABELNAME'];
      titem['updateFields'] = item['UPDATEFIELDS'];
      titem['selectData'] = item['SELECTDATA'];
      // 显隐 + 权限
      if (item['VISIBLEIF']) titem['visibleIf'] = item['VISIBLEIF'];
      if (item['PERCODE']) titem['perCode'] = item['PERCODE'];
      // autocomplete / treepicker 的 SELECTDATA 是选择器配置，不是字典数据，不应设 dict
      if (item['SELECTDATA'] && titem['type'] !== 'autocomplete' && titem['type'] !== 'treepicker') {
        try {
          heyui.addDict('$' + titem['key'], JSON.parse(item['SELECTDATA']));
          titem['dict'] = '$' + titem['key'];
        } catch (e) {
          // 非合法 JSON，尝试 key:title,key2:title2 文本格式 → 注册为局部字典
          if (typeof item['SELECTDATA'] === 'string' && item['SELECTDATA'].indexOf(':') > 0) {
            const datas = item['SELECTDATA'].split(',').map(seg => {
              const [k, title] = seg.split(':');
              return { key: (k || '').trim(), title: (title || k || '').trim() };
            });
            heyui.addDict('$' + titem['key'], datas);
            titem['dict'] = '$' + titem['key'];
          } else {
            // 否则当作已注册的字典名
            titem['dict'] = item['SELECTDATA'];
          }
        }
      }
      if ((item['SHOWLENGTH'] + "").indexOf('>') == 0) {
        titem['minWidth'] = parseInt(item['SHOWLENGTH'].substring(1), 10);
      } else if ((item['SHOWLENGTH'] + "").indexOf('<') == 0) {
        titem['maxWidth'] = parseInt(item['SHOWLENGTH'].substring(1), 10);
      } else if ((item['SHOWLENGTH'] + "") > 0) {
        titem['width'] = parseInt(item['SHOWLENGTH'], 10);
      } else {
        titem['width'] = 200;
      }
      titems.push(titem);
      let getProps = tableProps["getProps"];
      let props = {};
      if (getProps)
        props = getProps(titem['key'])||{};
      // getProps 返回的 type 可以覆盖元数据的 EDITTYPE（如元数据 EDITTYPE 为 null 时）
      if (props.type) {
        titem['type'] = props.type;
      }
      // EDITTYPE 为空且 getProps 没指定 type 的字段，不可编辑也不应显示在编辑表格中
      if (!titem['type'] && !props.type) {
        return;
      }
      if (['text', 'select', 'number', 'autocomplete', 'textarea', 'checkbox', 'action',"file",'fileupload','imageupload','fileuploadtpl','datepicker','image','code'].indexOf(titem['type']) != -1) {
        let options = [];
        if (titem['type'] === 'select') {
          /*
          let { selectData } = titem;
          selectData.split(',').map(d => {
            let ad = d.split(':');
            if (ad.length == 2) {
              options.push({ title: ad[0], key: ad[1] });
            } else {
              options.push({ title: d, key: d });
            }
          });
          */
          if (!props.cellProps) {
            props.cellProps = { dict: item['SELECTDATA'] };
          }
        }
        if (titem['type'] === 'autocomplete') {
          if (!props.cellProps) {
            const option = buildAutoCompleteOption(item['SELECTDATA']);
            props.cellProps = { option, keyName: option.keyName, titleName: option.titleName };
          }
        }
        // fileuploadtpl：文件上传+模板选择，SELECTDATA 存 {"templateType":"","moduleCode":"","showSelect":true}
        if (titem['type'] === 'fileuploadtpl') {
          let tplCfg = {};
          try { tplCfg = JSON.parse(item['SELECTDATA']) || {}; } catch (e) {}
          props.cellProps = props.cellProps || {};
          props.cellProps.uploaderTplConfig = tplCfg;
        }
        props.formItemProps= {required: +item['NULLABLE']!==1,prop: titem['key'],label:item['LABELNAME']};
        if (titem['type'] === 'action') {
          let { ACTIONCODE } = item;
          let actions = [];
          ACTIONCODE.split('|').map(d => {
            let ad = d.split(':');
            if (ad[1])
              actions.push({
                label: ad[0],
                code: ad[1].split(',')[0],
                per: ad[1].split(',')[1],
                perCode: titem['perCode'] || '',
                visibleIf: titem['visibleIf'] || '',
              });
          });
          titem.actions = actions;
        }
        if (titem['type'] === 'checkbox') {
          titem.align = 'center';
          heyui.addDict('$' + titem['key'], { 1: '√', 0: '' });
          titem['dict'] = '$' + titem['key'];
        }
        if(titem['type'] === 'image'){
          titem['render'] = (v)=>{
            return `<img src="${v}" style="width:100px;height:20px"/>`
          }
        }
        titem.props = {
          field: titem['key'],
          updateFields: titem['updateFields'],
          type: titem['type'],
          editInfo: tableProps.editInfo,
          selectInfo: tableProps.selectInfo,
          ...props
        }
        titem.on = {
          'on-cell-search': function(v) {
            if (tableProps['on-cell-search']) {
              tableProps['on-cell-search'].apply(this, [v]);
            }
          },
          'on-cell-click': function(v) {
            if (tableProps['on-cell-click']) {
              tableProps['on-cell-click'].apply(this, [v]);
            }
          },
          'on-apply-edit': function(v) {
            if (tableProps['on-apply-edit']) {
              tableProps['on-apply-edit'].apply(this, [v]);
            }
          }
        }
      }
    });
    return titems;
  }
};

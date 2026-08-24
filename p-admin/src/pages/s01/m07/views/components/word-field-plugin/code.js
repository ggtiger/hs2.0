/**
 * OnlyOffice Word 模版字段插入插件
 *
 * 功能：监听外部 postMessage，在光标位置插入 Content Control
 * 使用方式：
 *   window.postMessage({ type: 'insertField', key: 'CERTCODE', label: '证书编号', fieldType: 'text' })
 *
 * 支持的字段类型：
 *   text  → 纯文本 Content Control
 *   date  → 日期 Content Control（带日期选择器）
 *   image → 图片 Content Control
 *   html  → 富文本 Content Control
 *   table → 表格行标记
 */
(function() {
  // 字段插入队列（等待编辑器就绪后依次插入）
  var pendingFields = [];

  // 当前是否已连接
  var isConnected = false;

  window.Asc.plugin.init = function() {
    isConnected = true;

    // 处理排队的字段
    while (pendingFields.length > 0) {
      var field = pendingFields.shift();
      doInsertField(field);
    }
  };

  window.Asc.plugin.button = function(id) {
    // 插件按钮回调（无按钮，可忽略）
  };

  window.Asc.plugin.onExternalMouseUp = function() {
    // 外部鼠标事件
  };

  /**
   * 插入 Content Control 字段
   * 使用 OnlyOffice Document Builder API
   */
  function doInsertField(field) {
    var key = field.key || '';
    var label = field.label || key;
    var fieldType = field.fieldType || 'text';

    window.Asc.plugin.callCommand(function() {
      var oDocument = Api.GetDocument();

      // 创建 Content Control
      var nType;
      switch (fieldType) {
        case 'image':
          nType = 3; // picture
          break;
        case 'date':
          nType = 1; // rich text (OnlyOffice 没有专门的日期 SDT，用 rich text)
          break;
        default:
          nType = 1; // rich text
          break;
      }

      var oContentControl = oDocument.AddContentControl(nType);

      if (oContentControl) {
        // 设置 Tag（字段标识）
        oContentControl.SetTag(key);

        // 设置 Label（显示标签，显示在 Content Control 左上角）
        oContentControl.SetLabel(label);

        // 设置占位文本
        var oContent = oContentControl.GetContent();
        var oParagraph = oContent.GetElement(0);
        if (!oParagraph) {
          oParagraph = Api.CreateParagraph();
          oContent.Push(oParagraph);
        }
        oParagraph.AddText('[' + label + ']');
      }
    }, function(result) {
      // 插入完成回调
      window.parent.postMessage({
        type: 'fieldInserted',
        key: key,
        success: !!result
      }, '*');
    });
  }

  // 监听外部 postMessage
  window.addEventListener('message', function(event) {
    var data = event.data;

    if (!data || data.type !== 'insertField') return;

    var field = {
      key: data.key || '',
      label: data.label || data.key || '',
      fieldType: data.fieldType || 'text'
    };

    if (!field.key) return;

    if (isConnected) {
      doInsertField(field);
    } else {
      pendingFields.push(field);
    }
  });

  // 通知父页面插件已加载
  window.parent.postMessage({
    type: 'wordFieldPluginReady'
  }, '*');
})();

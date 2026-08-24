(function(window) {
  var pluginGuid = 'asc.{5F3B1A7C-8D2E-4B6F-A1C3-9E7D5F2B8A4E}';
  var htmlReceived = false;

  // 插件初始化 — OnlyOffice 加载完成后自动调用
  window.Asc.plugin.init = function() {
    // 通过 window.top.postMessage 通知外部页面插件已就绪
    try {
      window.top.postMessage(JSON.stringify({
        type: 'externalHtmlPastePluginReady',
        guid: pluginGuid
      }), '*');
    } catch (e) {
      // 跨域可能失败，尝试 window.parent
      try {
        window.parent.postMessage(JSON.stringify({
          type: 'externalHtmlPastePluginReady',
          guid: pluginGuid
        }), '*');
      } catch (e2) {
        // ignore
      }
    }
  };

  // 监听来自外部页面的消息
  // 外部页面通过 postMessage 发送 HTML 内容
  window.addEventListener('message', function(event) {
    var data;
    try {
      data = typeof event.data === 'string' ? JSON.parse(event.data) : event.data;
    } catch (e) {
      return;
    }

    // 处理粘贴 HTML 请求
    if (data.type === 'pasteHtml' && data.html && !htmlReceived) {
      htmlReceived = true;
      try {
        window.Asc.plugin.executeMethod('PasteHtml', [data.html], function(result) {
          // 通知外部粘贴完成
          sendToTop({
            type: 'pasteHtmlComplete',
            success: true,
            result: result
          });
        });
      } catch (e) {
        sendToTop({
          type: 'pasteHtmlComplete',
          success: false,
          error: e.message || 'PasteHtml failed'
        });
      }
    }
  });

  // 向外部页面发送消息
  function sendToTop(msg) {
    var str = JSON.stringify(msg);
    try {
      window.top.postMessage(str, '*');
    } catch (e) {
      try {
        window.parent.postMessage(str, '*');
      } catch (e2) {
        // ignore
      }
    }
  }

  // 插件按钮（非模态插件不需要）
  window.Asc.plugin.button = function(id) {
    this.executeCommand('close', '');
  };
})(window);

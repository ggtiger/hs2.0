(function() {
  var POLL_INTERVAL = 500;
  var SELECTION_CHECK_INTERVAL = 600;
  var INSERT_GAP = 400;
  var INSERT_TIMEOUT = 3000;
  var API_URL = window._WORD_TEMPLATE_API_URL_ || 'http://localhost:5001/api/word-template/field-queue';
  var SEL_URL = window._WORD_TEMPLATE_SEL_URL_ || 'http://localhost:5001/api/word-template/current-selection';
  var docKey = window._WORD_TEMPLATE_DOC_KEY_ || '';
  var pollTimer = null;
  var selectionTimer = null;
  var lastReportedTag = null;
  var insertQueue = [];
  var isInserting = false;

  function stopSelectionCheck() {
    if (selectionTimer) { clearInterval(selectionTimer); selectionTimer = null; }
  }
  function startSelectionCheck() {
    if (!selectionTimer) { selectionTimer = setInterval(checkSelection, SELECTION_CHECK_INTERVAL); }
  }

  function insertField(field, callback) {
    var fKey = field.key || '';
    var fLabel = field.label || fKey;
    var fType = field.type || 'text';
    console.log('[FieldInserter] inserting: key=' + fKey + ', label=' + fLabel + ', type=' + fType);

    var done = false;
    function finish() {
      if (done) return;
      done = true;
      startSelectionCheck();
      if (callback) callback();
    }

    stopSelectionCheck();

    // 第一步：在 callCommand 内部获取光标样式信息
    // 用 return 返回一个简单字符串（颜色 hex 或空），避免复杂对象序列化问题
    window.Asc.plugin.callCommand(function() {
      var result = '';
      try {
        var doc = Api.GetDocument();

        // 检查当前 Run 是否有显式颜色
        var run = doc.GetCurrentRun();
        if (run && typeof run.GetTextPr === 'function') {
          var pr = run.GetTextPr();
          if (pr) {
            try {
              var color = pr.GetColor();
              if (color && typeof color.GetHex === 'function') {
                var hex = color.GetHex();
                if (hex) result = hex;
              }
            } catch(e) {}
          }
        }

        // 如果 Run 没有颜色，检查段落标记
        if (!result) {
          var para = doc.GetCurrentParagraph();
          if (para && typeof para.GetParagraphMarkTextPr === 'function') {
            var mPr = para.GetParagraphMarkTextPr();
            if (mPr) {
              try {
                var mColor = mPr.GetColor();
                if (mColor && typeof mColor.GetHex === 'function') {
                  var mHex = mColor.GetHex();
                  if (mHex) result = mHex;
                }
              } catch(e) {}
            }
          }
        }

        // 检查段落 GetTextPr
        if (!result) {
          var para2 = doc.GetCurrentParagraph();
          if (para2 && typeof para2.GetTextPr === 'function') {
            var pPr = para2.GetTextPr();
            if (pPr) {
              try {
                var pColor = pPr.GetColor();
                if (pColor && typeof pColor.GetHex === 'function') {
                  var pHex = pColor.GetHex();
                  if (pHex) result = pHex;
                }
              } catch(e) {}
            }
          }
        }
      } catch(e) {}
      return result;
    }, false, false, function(colorHex) {
      // return 简单字符串是可靠的
      var hasColor = !!(colorHex && typeof colorHex === 'string' && colorHex.length > 0);
      console.log('[FieldInserter] cursor has explicit color: ' + hasColor + (hasColor ? ' (#' + colorHex + ')' : ''));

      // 第二步：插入 SDT
      var commonPr = {
        Tag: fKey,
        Alias: fLabel,
        PlaceHolderText: '[' + fLabel + ']'
      };

      function afterInsert() {
        console.log('[FieldInserter] SDT created: ' + fKey);

        // 第三步：只在需要时应用颜色
        // SDT 已自动继承字体/字号/加粗等样式
        // 只需要处理颜色：如果光标位置没有显式颜色，SDT 占位文本会显示灰色，需要设为黑色
        if (!hasColor) {
          // 没有显式颜色 = 默认黑色，需要设置避免占位文本灰色
          applyDefaultColor(function() { finish(); });
        } else {
          // 有显式颜色，SDT 已自动继承，不需要额外设置
          console.log('[FieldInserter] color inherited, skip SetTextPr');
          finish();
        }
      }

      if (fType === 'image') {
        window.Asc.plugin.executeMethod("AddContentControlPicture", [commonPr], function() { afterInsert(); });
      } else if (fType === 'table') {
        window.Asc.plugin.executeMethod("AddContentControl", [1, commonPr], function() { afterInsert(); });
      } else if (fType === 'html') {
        window.Asc.plugin.executeMethod("AddContentControl", [1, commonPr], function() { afterInsert(); });
      } else {
        window.Asc.plugin.executeMethod("AddContentControl", [2, commonPr], function() { afterInsert(); });
      }

      setTimeout(finish, INSERT_TIMEOUT);
    });
  }

  /**
   * 设置当前 SDT 的占位文本颜色为黑色
   * 仅在没有显式颜色时调用，避免占位文本显示灰色
   */
  function applyDefaultColor(callback) {
    try {
      window.Asc.plugin.callCommand(function() {
        var doc = Api.GetDocument();
        var cc = doc.GetCurrentContentControl();
        if (cc && typeof cc.SetTextPr === 'function') {
          var textPr = Api.CreateTextPr();
          textPr.SetColor(Api.HexColor('#000000'));
          cc.SetTextPr(textPr);
        }
      }, false, false, function() {
        console.log('[FieldInserter] default black color applied');
        if (callback) callback();
      });
    } catch(e) {
      console.log('[FieldInserter] applyDefaultColor error: ' + e);
      if (callback) callback();
    }
  }

  function processInsertQueue() {
    if (isInserting) return;
    if (insertQueue.length === 0) return;
    isInserting = true;
    var field = insertQueue.shift();
    insertField(field, function() {
      isInserting = false;
      if (insertQueue.length > 0) {
        setTimeout(processInsertQueue, INSERT_GAP);
      }
    });
  }

  function enqueueInsert(fields) {
    for (var i = 0; i < fields.length; i++) { insertQueue.push(fields[i]); }
    processInsertQueue();
  }

  function pollQueue() {
    if (isInserting) return;
    try {
      var xhr = new XMLHttpRequest();
      var url = API_URL;
      if (docKey) { url = API_URL + (API_URL.indexOf('?') > -1 ? '&' : '?') + 'key=' + encodeURIComponent(docKey); }
      xhr.open('GET', url);
      xhr.onreadystatechange = function() {
        if (xhr.readyState === 4 && xhr.status === 200) {
          try {
            var result = JSON.parse(xhr.responseText);
            var fields = result.fields || [];
            if (fields.length > 0) {
              console.log('[FieldInserter] received ' + fields.length + ' fields');
              enqueueInsert(fields);
            }
          } catch (e) { console.log('[FieldInserter] parse error: ' + e); }
        }
      };
      xhr.onerror = function() { console.log('[FieldInserter] XHR network error'); };
      xhr.send();
    } catch (e) { console.log('[FieldInserter] poll error: ' + e); }
  }

  function checkSelection() {
    if (isInserting) return;
    try {
      window.Asc.plugin.callCommand(function() {
        var oDocument = Api.GetDocument();
        var tag = '';
        try {
          if (oDocument && typeof oDocument.GetCurrentContentControl === 'function') {
            var oCC = oDocument.GetCurrentContentControl();
            if (oCC) {
              if (typeof oCC.GetTag === 'function') { tag = oCC.GetTag(); }
              else if (oCC.Tag) { tag = oCC.Tag; }
            }
          }
        } catch(e) {}
        return tag;
      }, false, false, function(tag) {
        try {
          var currentTag = tag || '';
          if (currentTag === lastReportedTag) return;
          lastReportedTag = currentTag;
          console.log('[FieldInserter] CC selection tag: ' + currentTag);
          try { window.top.postMessage({ type: 'onlyoffice-cc-selection', tag: currentTag }, '*'); } catch(e) {}
          try {
            var xhr = new XMLHttpRequest();
            xhr.open('POST', SEL_URL);
            xhr.setRequestHeader('Content-Type', 'application/json');
            xhr.send(JSON.stringify({ key: docKey || 'default', tag: currentTag }));
          } catch(e) {}
        } catch(e) {}
      });
    } catch(e) { console.log('[FieldInserter] checkSelection error: ' + e); }
  }

  window.Asc.plugin.init = function() {
    console.log('[FieldInserter] plugin ready v1.8.0, docKey=' + docKey);
    pollTimer = setInterval(pollQueue, POLL_INTERVAL);
    selectionTimer = setInterval(checkSelection, SELECTION_CHECK_INTERVAL);
    setTimeout(checkSelection, 1000);
  };

  window.Asc.plugin.button = function(id) {
    if (pollTimer) { clearInterval(pollTimer); pollTimer = null; }
    if (selectionTimer) { clearInterval(selectionTimer); selectionTimer = null; }
  };
})();

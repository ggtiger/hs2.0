/**
 * 根据传入html字符串过滤word
 * @module UE
 * @since 1.2.6.1
 * @method filterWord
 * @param { String } html html字符串
 * @return { String } 已过滤后的结果字符串
 * @example
 * ```javascript
 * UE.filterWord(html);
 * ```
 */
let filterWord = function() {
  function transUnitToPx(val) {
    if (!/(pt|cm)/.test(val)) {
      return val
    }
    var unit;
    val.replace(/([\d.]+)(\w+)/, function(str, v, u) {
      val = v;
      unit = u;
    });
    switch (unit) {
      case 'cm':
        val = parseFloat(val) * 25;
        break;
      case 'pt':
        val = Math.round(parseFloat(val) * 96 / 72);
    }
    return val + (val ? 'px' : '');
  }
  //是否是word过来的内容
  function isWordDocument(str) {
    return /(class="?Mso|style="[^"]*\bmso\-|w:WordDocument|<(v|o):|lang=)/ig.test(str);
  }
  //去掉小数
  function transUnit(v) {
    v = v.replace(/[\d.]+\w+/g, function(m) {
      return transUnitToPx(m);
    });
    return v;
  }

  function filterPasteWord(str) {
    return str.replace(/[\t\r\n]+/g, ' ')
      .replace(/<!--[\s\S]*?-->/ig, "")
      //转换图片
      .replace(/<v:shape [^>]*>[\s\S]*?.<\/v:shape>/gi, function(str) {
        //opera能自己解析出image所这里直接返回空
        if (browser.opera) {
          return '';
        }
        try {
          //有可能是bitmap占为图，无用，直接过滤掉，主要体现在粘贴excel表格中
          if (/Bitmap/i.test(str)) {
            return '';
          }
          var width = str.match(/width:([ \d.]*p[tx])/i)[1],
            height = str.match(/height:([ \d.]*p[tx])/i)[1],
            src = str.match(/src=\s*"([^"]*)"/i)[1];
          return '<img width="' + transUnit(width) + '" height="' + transUnit(height) + '" src="' + src + '" />';
        } catch (e) {
          return '';
        }
      })
      //针对wps添加的多余标签处理
      .replace(/<\/?div[^>]*>/g, '')
      //去掉多余的属性
      .replace(/v:\w+=(["']?)[^'"]+\1/g, '')
      .replace(/<(!|script[^>]*>.*?<\/script(?=[>\s])|\/?(\?xml(:\w+)?|xml|meta|link|style|\w+:\w+)(?=[\s\/>]))[^>]*>/gi, "")
      .replace(/<p [^>]*class="?MsoHeading"?[^>]*>(.*?)<\/p>/gi, "<p><strong>$1</strong></p>")
      //去掉多余的属性
      .replace(/\s+(class|lang|align)\s*=\s*(['"]?)([\w-]+)\2/ig, function(str, name, marks, val) {
        //保留list的标示
        return name == 'class' && val == 'MsoListParagraph' ? str : ''
      })
      //清除多余的font/span不能匹配&nbsp;有可能是空格
      .replace(/<(font|span)[^>]*>(\s*)<\/\1>/gi, function(a, b, c) {
        return c.replace(/[\t\r\n ]+/g, ' ')
      })
      //处理style的问题
      .replace(/(<[a-z][^>]*)\sstyle=(["'])([^\2]*?)\2/gi, function(str, tag, tmp, style) {
        var n = [],
          s = style.replace(/^\s+|\s+$/, '')
            .replace(/&#39;/g, '\'')
            .replace(/&quot;/gi, "'")
            .replace(/[\d.]+(cm|pt)/g, function(str) {
              return transUnitToPx(str)
            })
            .split(/;\s*/g);

        for (var i = 0, v; v = s[i]; i++) {

          var name, value,
            parts = v.split(":");

          if (parts.length == 2) {
            name = parts[0].toLowerCase();
            value = parts[1].toLowerCase();
            if (/^(background)\w*/.test(name) && value.replace(/(initial|\s)/g, '').length == 0
              ||
              /^(margin)\w*/.test(name) && /^0\w+$/.test(value)
            ) {
              continue;
            }

            switch (name) {
              case "mso-padding-alt":
              case "mso-padding-top-alt":
              case "mso-padding-right-alt":
              case "mso-padding-bottom-alt":
              case "mso-padding-left-alt":
              case "mso-margin-alt":
              case "mso-margin-top-alt":
              case "mso-margin-right-alt":
              case "mso-margin-bottom-alt":
              case "mso-margin-left-alt":
              //ie下会出现挤到一起的情况
              //case "mso-table-layout-alt":
              case "mso-height":
              case "mso-width":
              case "mso-vertical-align-alt":
                //trace:1819 ff下会解析出padding在table上
                if (!/<table/.test(tag))
                  n[i] = name.replace(/^mso-|-alt$/g, "") + ":" + transUnit(value);
                continue;
              case "horiz-align":
                n[i] = "text-align:" + value;
                continue;

              case "vert-align":
                n[i] = "vertical-align:" + value;
                continue;

              case "font-color":
              case "mso-foreground":
                n[i] = "color:" + value;
                continue;

              case "mso-background":
              case "mso-highlight":
                n[i] = "background:" + value;
                continue;

              case "mso-default-height":
                n[i] = "min-height:" + transUnit(value);
                continue;

              case "mso-default-width":
                n[i] = "min-width:" + transUnit(value);
                continue;

              case "mso-padding-between-alt":
                n[i] = "border-collapse:separate;border-spacing:" + transUnit(value);
                continue;

              case "text-line-through":
                if ((value == "single") || (value == "double")) {
                  n[i] = "text-decoration:line-through";
                }
                continue;
              case "mso-zero-height":
                if (value == "yes") {
                  n[i] = "display:none";
                }
                continue;
              //                                case 'background':
              //                                    break;
              case 'margin':
                if (!/[1-9]/.test(value)) {
                  continue;
                }

            }

            if (/^(mso|column|font-emph|lang|layout|line-break|list-image|nav|panose|punct|row|ruby|sep|size|src|tab-|table-border|text-(?:decor|trans)|top-bar|version|vnd|word-break)/.test(name)
              ||
              /text\-indent|padding|margin/.test(name) && /\-[\d.]+/.test(value)
            ) {
              continue;
            }

            n[i] = name + ":" + parts[1];
          }
        }
        return tag + (n.length ? ' style="' + n.join(';').replace(/;{2,}/g, ';') + '"' : '');
      })


  }

  return function(html) {
    return (isWordDocument(html) ? filterPasteWord(html) : html);
  };
}();
var htmlparser = function(htmlstr, ignoreBlank) {
  //todo 原来的方式  [^"'<>\/] 有\/就不能配对上 <TD vAlign=top background=../AAA.JPG> 这样的标签了
  //先去掉了，加上的原因忘了，这里先记录
  var re_tag = /<(?:(?:\/([^>]+)>)|(?:!--([\S|\s]*?)-->)|(?:([^\s\/<>]+)\s*((?:(?:"[^"]*")|(?:'[^']*')|[^"'<>])*)\/?>))/g,
    re_attr = /([\w\-:.]+)(?:(?:\s*=\s*(?:(?:"([^"]*)")|(?:'([^']*)')|([^\s>]+)))|(?=\s|$))/g;

  //ie下取得的html可能会有\n存在，要去掉，在处理replace(/[\t\r\n]*/g,'');代码高量的\n不能去除
  var allowEmptyTags = {
    b: 1, code: 1, i: 1, u: 1, strike: 1, s: 1, tt: 1, strong: 1, q: 1, samp: 1, em: 1, span: 1,
    sub: 1, img: 1, sup: 1, font: 1, big: 1, small: 1, iframe: 1, a: 1, br: 1, pre: 1
  };
  htmlstr = htmlstr.replace(new RegExp(domUtils.fillChar, 'g'), '');
  if (!ignoreBlank) {
    htmlstr = htmlstr.replace(new RegExp('[\\r\\t\\n' + (ignoreBlank ? '' : ' ') + ']*<\/?(\\w+)\\s*(?:[^>]*)>[\\r\\t\\n' + (ignoreBlank ? '' : ' ') + ']*', 'g'), function(a, b) {
      //br暂时单独处理
      if (b && allowEmptyTags[b.toLowerCase()]) {
        return a.replace(/(^[\n\r]+)|([\n\r]+$)/g, '');
      }
      return a.replace(new RegExp('^[\\r\\n' + (ignoreBlank ? '' : ' ') + ']+'), '').replace(new RegExp('[\\r\\n' + (ignoreBlank ? '' : ' ') + ']+$'), '');
    });
  }

  var notTransAttrs = {
    'href': 1,
    'src': 1
  };

  var uNode = UE.uNode,
    needParentNode = {
      'td': 'tr',
      'tr': ['tbody', 'thead', 'tfoot'],
      'tbody': 'table',
      'th': 'tr',
      'thead': 'table',
      'tfoot': 'table',
      'caption': 'table',
      'li': ['ul', 'ol'],
      'dt': 'dl',
      'dd': 'dl',
      'option': 'select'
    },
    needChild = {
      'ol': 'li',
      'ul': 'li'
    };

  function text(parent, data) {

    if (needChild[parent.tagName]) {
      var tmpNode = uNode.createElement(needChild[parent.tagName]);
      parent.appendChild(tmpNode);
      tmpNode.appendChild(uNode.createText(data));
      parent = tmpNode;
    } else {

      parent.appendChild(uNode.createText(data));
    }
  }

  function element(parent, tagName, htmlattr) {
    var needParentTag;
    if (needParentTag = needParentNode[tagName]) {
      var tmpParent = parent, hasParent;
      while (tmpParent.type != 'root') {
        if (utils.isArray(needParentTag) ? utils.indexOf(needParentTag, tmpParent.tagName) != -1 : needParentTag == tmpParent.tagName) {
          parent = tmpParent;
          hasParent = true;
          break;
        }
        tmpParent = tmpParent.parentNode;
      }
      if (!hasParent) {
        parent = element(parent, utils.isArray(needParentTag) ? needParentTag[0] : needParentTag)
      }
    }
    //按dtd处理嵌套
    //        if(parent.type != 'root' && !dtd[parent.tagName][tagName])
    //            parent = parent.parentNode;
    var elm = new uNode({
      parentNode: parent,
      type: 'element',
      tagName: tagName.toLowerCase(),
      //是自闭合的处理一下
      children: dtd.$empty[tagName] ? null : []
    });
    //如果属性存在，处理属性
    if (htmlattr) {
      var attrs = {}, match;
      while (match = re_attr.exec(htmlattr)) {
        attrs[match[1].toLowerCase()] = notTransAttrs[match[1].toLowerCase()] ? (match[2] || match[3] || match[4]) : utils.unhtml(match[2] || match[3] || match[4])
      }
      elm.attrs = attrs;
    }
    //trace:3970
    //        //如果parent下不能放elm
    //        if(dtd.$inline[parent.tagName] && dtd.$block[elm.tagName] && !dtd[parent.tagName][elm.tagName]){
    //            parent = parent.parentNode;
    //            elm.parentNode = parent;
    //        }
    parent.children.push(elm);
    //如果是自闭合节点返回父亲节点
    return dtd.$empty[tagName] ? parent : elm
  }

  function comment(parent, data) {
    parent.children.push(new uNode({
      type: 'comment',
      data: data,
      parentNode: parent
    }));
  }

  var match, currentIndex = 0, nextIndex = 0;
  //设置根节点
  var root = new uNode({
    type: 'root',
    children: []
  });
  var currentParent = root;

  while (match = re_tag.exec(htmlstr)) {
    currentIndex = match.index;
    try {
      if (currentIndex > nextIndex) {
        //text node
        text(currentParent, htmlstr.slice(nextIndex, currentIndex));
      }
      if (match[3]) {

        if (dtd.$cdata[currentParent.tagName]) {
          text(currentParent, match[0]);
        } else {
          //start tag
          currentParent = element(currentParent, match[3].toLowerCase(), match[4]);
        }


      } else if (match[1]) {
        if (currentParent.type != 'root') {
          if (dtd.$cdata[currentParent.tagName] && !dtd.$cdata[match[1]]) {
            text(currentParent, match[0]);
          } else {
            var tmpParent = currentParent;
            while (currentParent.type == 'element' && currentParent.tagName != match[1].toLowerCase()) {
              currentParent = currentParent.parentNode;
              if (currentParent.type == 'root') {
                currentParent = tmpParent;
                throw 'break'
              }
            }
            //end tag
            currentParent = currentParent.parentNode;
          }

        }

      } else if (match[2]) {
        //comment
        comment(currentParent, match[2])
      }
    } catch (e) { }

    nextIndex = re_tag.lastIndex;

  }
  //如果结束是文本，就有可能丢掉，所以这里手动判断一下
  //例如 <li>sdfsdfsdf<li>sdfsdfsdfsdf
  if (nextIndex < htmlstr.length) {
    text(currentParent, htmlstr.slice(nextIndex));
  }
  return root;
};
export default filterWord;

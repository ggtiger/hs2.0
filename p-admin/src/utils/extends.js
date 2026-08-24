import Vue from 'vue'
import Store from '@/store'
Vue.prototype.$isBusy = false;
Vue.prototype.$isConfirm = false;


Vue.prototype.$alert = async function(message) {
  return this.$Message.success(message)
}
Vue.prototype.$error = async function(message) {
  debugger
  return this.$Message.error(message)
}

Vue.prototype.$confirm = async function(content, title) {
  if (this.$isConfirm) {
    Vue.prototype.$isConfirm = false;
    throw new Error("别老是点返回...");
    return;
  }
  Vue.prototype.$isConfirm = true;
  return new Promise((resolve, reject) => {
    Vue.prototype.$isConfirm = false;
    this.$Confirm(content, title || "确定").then(() => {
      resolve(true)
    });
  });
}

Vue.prototype.$busy = function(content, time) {
  return this.$Message.loading(content || "加载中...", time)
}

Vue.prototype.$free = function(busy) {
  if(busy)
    busy.close();
}

Vue.prototype.$getStoreCall = function({ STORE_NAME }) {
  return ({ action, param, successText, errorText, successCall, errorCall, isBusy, isSuccessBack, isErrorBack, timeOut, successBackParams }) => {
    return this.$callAction({ action: `${STORE_NAME}/${action}`, param, successText, errorText, successCall, errorCall, isBusy, isSuccessBack, isErrorBack, timeOut, successBackParams });
  }
}

Vue.prototype.$callAsync = async function({ method, params, timeOut }) {
  let busy = this.$busy();
  this.ERRMESSAGE = "";
  return new Promise((resolve, reject) => {
    setTimeout(async () => {
      method(...params).then((data) => {
        resolve(data)
      }).catch(e => {
        if (e.response && e.response.status === 401) {
          this.ERRMESSAGE = "登陆超时！";
          this.$router.replace("/loginout");
          return;
        }
        this.$error(e.message);
        if (e.message == "登陆超时！") {
          this.ERRMESSAGE = "登陆超时！";
          this.$router.replace("/loginout");
          return;
        }
        reject(e);
      }).then(() => {
        this.$free(busy);
      });
    }, timeOut || 0);
  })
}

Vue.prototype.$callAction = function({ action, param, successText, errorText, successCall, errorCall, isBusy, isSuccessBack, isErrorBack, timeOut, successBackParams }) {
  this.ERRMESSAGE = "";
  // 返回 Promise，让异步调用方可以 `const ret = await this.$callAction({...})`
  // - 成功: resolve(ret)，并照旧触发 successText/successCall/isSuccessBack
  // - 失败: reject(e)，并照旧触发 $error 弹窗/errorCall/isErrorBack
  //   调用方不 await 时行为与旧版完全等价（fire-and-forget）
  //   调用方 await 时需要自己 try/catch 或 .catch()，否则会有 unhandled rejection
  return new Promise((resolve, reject) => {
    setTimeout(async () => {
      // isBusy === false 时不弹"加载中"（静默后台调用，如 watch 联动加载）
      let busy = isBusy === false ? null : this.$busy();
      try {
        // eslint-disable-next-line no-restricted-syntax
        let ret = await this.$store.dispatch(action, param);
        this.$free(busy);
        if (successText) {
          this.$alert(successText);
        }
        if (successCall) {
          successCall(ret);
        }
        if (isSuccessBack) {
          if (this.$parent && this.$parent.close)
            this.$parent.close();
          else if (this.$parent && this.$parent.setvalue)
            this.$parent.setvalue(false);
        }
        resolve(ret);
      } catch (e) {
        this.$free(busy);
        if (e.response && e.response.status === 401) {
          this.ERRMESSAGE = "登陆超时！";
          this.$router.replace("/loginout");
          reject(e);
          return;
        }
        this.$error(errorText || e.message || "加载失败");
        if (e.message == "登陆超时！") {
          this.ERRMESSAGE = "登陆超时！";
          this.$router.replace("/loginout");
          reject(e);
          return;
        }
        if (errorCall) {
          errorCall();
        }
        if (isErrorBack)
          this.$router.goBack(true, { error: e });
        reject(e);
      }
    }, timeOut || 0);
  });
}



var spillDataNum = 5;
// 设置隐藏函数
var timeout = false;
let setRowDisableNone = function(topNum, showRowNum, binding) {
  if (timeout) {
    clearTimeout(timeout);
  }
  timeout = setTimeout(() => {
    binding.value.call(null, topNum, topNum + showRowNum + spillDataNum);
  });
};
Vue.directive('loadmore', {
  componentUpdated: function(el, binding, vnode, oldVnode) {
    setTimeout(() => {
      const dataSize = vnode.data.attrs['data-size'];
      const oldDataSize = oldVnode.data.attrs['data-size'];
      if (dataSize === oldDataSize) {
        return;
      }
      const selectWrap = el.querySelector('.ivu-table-body');
      const selectTbody = selectWrap.querySelector('table tbody');
      const selectRow = selectWrap.querySelector('table tr');
      if (!selectRow) {
        return;
      }
      const rowHeight = selectRow.clientHeight;
      let showRowNum = Math.round(selectWrap.clientHeight / rowHeight);

      const createElementTR = document.createElement('tr');
      let createElementTRHeight = (dataSize - showRowNum - spillDataNum) * rowHeight;
      createElementTR.setAttribute('style', `height: ${createElementTRHeight}px;`);
      selectTbody.append(createElementTR);

      // 监听滚动后事件
      selectWrap.addEventListener('scroll', function() {
        let topPx = this.scrollTop - spillDataNum * rowHeight;
        let topNum = Math.round(topPx / rowHeight);
        let minTopNum = dataSize - spillDataNum - showRowNum;
        if (topNum > minTopNum) {
          topNum = minTopNum;
        }
        if (topNum < 0) {
          topNum = 0;
          topPx = 0;
        }
        selectTbody.setAttribute('style', `transform: translateY(${topPx}px)`);
        createElementTR.setAttribute('style', `height: ${createElementTRHeight - topPx > 0 ? createElementTRHeight - topPx : 0}px;`);
        setRowDisableNone(topNum, showRowNum, binding);
      })
    });
  }
})

Vue.directive('per', {
  priority: -999,
  bind: function(el, binding, vnode) {
    // 只调用一次，指令第一次绑定到元素时调用，用于在绑定元素时执行一次的初始化动作。
  },
  update: function(el, binding, vnode) {
    // 第一次是紧跟在 bind 之后调用，获得的参数是绑定的初始值，
    // 之后被绑定元素所在的模板更新时调用，而不论绑定值是否变化，可以忽略不必要的模板更新。
    // console.log(Store.state.app.fpoints,binding,binding.value,Store.state.app.fpoints[binding.value]);
    if (binding.value) {
      //Store.state.app.ofpoint;
      // console.log(Store.state.app.fpoints,Store.state.app.fpoints[binding.value]);
      if (!Store.state.app.fpoints[binding.value]){
        el.style.display = "none";
      }
    }
  },
  inserted: function(el, binding, vnode) {
    // 被绑定元素插入父节点时调用（父节点存在即可调用，不必存在于 document 中）。
  },
  componentUpdated: function(el, binding, vnode) {
    // 被绑定元素所在模板完成一次更新周期时调用。
  },
  unbind: function(el, binding, vnode) {
    // 只调用一次， 指令与元素解绑时调用。
  }
})

Math.fixed = function(num, decimalPlaces, dfh) {
  var d = decimalPlaces || 0;
  var m = Math.pow(10, d);
  var n = +(d ? num * m : num).toFixed(8); // Avoid rounding errors
  var i = Math.floor(n), f = n - i;
  var e = 1e-8; // Allow for rounding errors in f
  var r = (f > 0.5 - e && f < 0.5 + e) ?
    ((i % 2 == 0) ? i : i + 1) : Math.round(n);
  var ret = d ? r / m : r;
  if (decimalPlaces || decimalPlaces === 0) {
    ret = ret.toFixed(decimalPlaces);
  }
  if (dfh && ret > 0) {
    ret = '+' + ret;
  }
  return ret;
}

String.prototype.replaceAll = function(s1, s2) {
  return this.replace(new RegExp(s1, "gm"), s2);
}

window.$maxAbs = function(data, decimalPlaces,iaabs) {
  let dd = 0;
  data.map(d => {
    if (Math.abs(d) > Math.abs(dd)) {
      dd = d;
    }
  });
  if(iaabs===true){
    dd = Math.abs(dd);
  }
  if (decimalPlaces ===0||decimalPlaces) {
    dd= Math.fixed(dd, decimalPlaces);
  }
  return dd;
}

//绝对值
window.$abs = function(data) {
  return Math.abs(data);
}

//方差
window.$sqrt = function(data) {
  return Math.sqrt(data);
}

//方差
window.$log = function(data) {
  return Math.log(data);
}

//平方
window.$pow2 = function(data) {
  return data * data;
}

//示值误差
window.$indError = function(a, b, decimalPlaces, dfh) {
  let dd = 0;
  dd = (b - a) / a * 100;
  if (decimalPlaces ===0||decimalPlaces || dfh) {
    return Math.fixed(dd, decimalPlaces, dfh);
  }
  return dd;
}

//汇总值
window.$t = function(data, decimalPlaces) {
  let dd = 0;
  data.map(d => {
    dd += d;
  });
  if (decimalPlaces ===0||decimalPlaces || dfh) {
    return Math.fixed(dd, decimalPlaces);
  }
  return dd;
}

//平均值
window.$avg = function(data, decimalPlaces, dfh) {
  let dd = 0;
  data.map(d => {
    dd += d;
  });
  dd = dd / data.length;
  if (decimalPlaces ===0||decimalPlaces || dfh) {
    return Math.fixed(dd, decimalPlaces, dfh);
  }
  return dd;
}

//示值误差
window.$avgStd = function(data, num, decimalPlaces, dfh) {
  let dd = 0;
  dd = ($avg(data) - num);
  if (decimalPlaces ===0||decimalPlaces || dfh) {
    return Math.fixed(dd, decimalPlaces, dfh);
  }
  return dd;
}

//标准方差
window.$std = function(data, snum, decimalPlaces, dfh) {
  let dd = 0;
  dd = ($avg(data) - snum) / snum;
  if (decimalPlaces ===0||decimalPlaces || dfh) {
    return Math.fixed(dd * 100, decimalPlaces, dfh);
  }
  return dd;
}

window.$maxStd = function(data, snum, decimalPlaces, dfh) {
  let dd = 0;
  let aa = [];
  data.map(d => {
    aa.push(d - snum);
  });
  dd = Math.abs(Math.max.apply(null, aa));
  if (decimalPlaces ===0||decimalPlaces || dfh) {
    return Math.fixed(dd, decimalPlaces, dfh);
  }
  return dd;
}

window.$stdev = function(data, decimalPlaces, dfh) {
  let dd = 0;
  let avg = $avg(data);
  data.map(d => {
    dd += $pow2(d - avg);
  });
  dd = $sqrt(dd / (data.length - 1));
  if (decimalPlaces ===0||decimalPlaces || dfh) {
    return Math.fixed(dd, decimalPlaces, dfh);
  }
  return dd;
}

//ab值差值绝对值
window.$abAbs = function(a, b, decimalPlaces, dfh) {
  let dd = Math.abs(a - b);
  if (decimalPlaces ===0||decimalPlaces || dfh) {
    return Math.fixed(dd, decimalPlaces, dfh);
  }
  return dd;
}

//最大最小值标准
window.$maxminStd = function(data, std, decimalPlaces, dfh) {
  let dd = Math.max_min(data) / std;
  if (decimalPlaces ===0||decimalPlaces || dfh) {
    return Math.fixed(dd, decimalPlaces, dfh);
  }
  return dd;
}

//最大最小值
window.$maxmin = function(data, decimalPlaces, dfh) {
  let dd = Math.max_min(data);
  if (decimalPlaces ===0||decimalPlaces || dfh) {
    return Math.fixed(dd, decimalPlaces, dfh);
  }
  return dd;
}

window.$sqrtpow = function(data, decimalPlaces, dfh) {
  let dd = 0;
  data.map(d => {
    dd += $pow2(d);
  });
  dd = $sqrt(dd);
  if (decimalPlaces ===0||decimalPlaces || dfh) {
    return Math.fixed(dd, decimalPlaces, dfh);
  }
  return dd;
}


window.$maxAbs = function(data, decimalPlaces, iaabs) {
  let dd = 0;
  data.map(d => {
    if (Math.abs(d) > Math.abs(dd)) {
      dd = d;
    }
  });
  if(iaabs===true){
    dd = Math.abs(dd);
  }
  if (decimalPlaces ===0||decimalPlaces) {
    dd= Math.fixed(dd, decimalPlaces);
  }
  return dd;
}

window.$minAbs = function(data, decimalPlaces, iaabs) {
  let dd = data[0];
  data.map(d => {
    if (Math.abs(d) < Math.abs(dd)) {
      dd = d;
    }
  });
  if(iaabs===true){
    dd = Math.abs(dd);
  }
  if (decimalPlaces ===0||decimalPlaces) {
    dd= Math.fixed(dd, decimalPlaces);
  }
  return dd;
}

window.$fixed = function(num, decimalPlaces, dfh) {
  return Math.fixed(num, decimalPlaces, dfh);
}

window.$round = function(num, decimalPlaces, dfh) {
  num = +(num);
  let ret = num.toFixed(num, decimalPlaces);
  if (dfh && ret > 0) {
    ret = '+' + ret;
  }
  return ret;
}


Math.maxAbs = function(data, decimalPlaces, dfh) {
  let dd = 0;
  data.map(d => {
    if (Math.abs(d) > Math.abs(dd)) {
      dd = d;
    }
  });
  return dd;
}

Math.max_min = function(data, decimalPlaces, dfh) {
  let dd = Math.max.apply(null, data) - Math.min.apply(null, data);
  if (decimalPlaces ===0||decimalPlaces || dfh) {
    dd = Math.fixed(dd, decimalPlaces, dfh);
  }
  return dd;
};

// HeyUI Table 固定列滚动同步修复
// 问题：浏览器平滑滚动在 compositor 线程，和 JS 不同步导致滚动过程错位
// 方案：CSS margin-top:0!important 锁死，接管 wheel 事件手动控制 scrollTop + transform
// 同一次 JS 执行中同时更新 scrollTop 和 transform，零延迟
(function() {
  function syncTransform(bodyEl) {
    var table = bodyEl.closest('.h-table');
    if (!table) return;
    var st = bodyEl.scrollTop;
    var leftTable = table.querySelector('.h-table-fixed-left-table');
    var rightTable = table.querySelector('.h-table-fixed-right-table');
    if (leftTable) leftTable.style.transform = 'translateY(' + (-st) + 'px)';
    if (rightTable) rightTable.style.transform = 'translateY(' + (-st) + 'px)';
  }

  // 拦截 body 和固定列上的 wheel 事件，手动控制滚动
  document.addEventListener('wheel', function(e) {
    var body = e.target.closest ? e.target.closest('.h-table-body') : null;
    var fixedCol = e.target.closest ? (e.target.closest('.h-table-fixed-left') || e.target.closest('.h-table-fixed-right')) : null;

    if (!body && !fixedCol) return;

    // 固定列上的 wheel：找到对应的 body
    if (fixedCol && !body) {
      var table = fixedCol.closest('.h-table');
      body = table ? table.querySelector('.h-table-body') : null;
    }
    if (!body) return;

    // 判断用户滚动意图：水平还是垂直
    var isHorizontal = Math.abs(e.deltaX) > Math.abs(e.deltaY);

    // 判断表格是否已滚到边界
    var atTop = body.scrollTop <= 0 && e.deltaY < 0;
    var atBottom = Math.round(body.scrollTop + body.clientHeight) >= body.scrollHeight && e.deltaY > 0;
    var atLeft = body.scrollLeft <= 0 && e.deltaX < 0;
    var atRight = Math.round(body.scrollLeft + body.clientWidth) >= body.scrollWidth && e.deltaX > 0;

    if (isHorizontal) {
      // 水平滚动：只处理 scrollLeft，忽略 deltaY
      if (!atLeft && !atRight) {
        try {
          e.preventDefault();
        } catch (error) {
          console.log(error);
        }
        body.scrollLeft += e.deltaX;
      }
    } else if (!atTop && !atBottom) {
      // 垂直滚动：表格内部还有滚动空间，手动控制滚动并同步固定列
      try {
        e.preventDefault();
      } catch (error) {
        console.log(error);
      }
      body.scrollTop += e.deltaY;
      syncTransform(body);
    } else {
      // 垂直到底/顶时，阻止表格滚动但不阻止外层滚动
      try {
        e.preventDefault();
      } catch (error) {
        console.log(error);
      }
      // 找到外层可滚动容器，手动传递滚动
      var scrollParent = body.parentElement;
      while (scrollParent) {
        var style = window.getComputedStyle(scrollParent);
        var overflowY = style.overflowY || style.overflow;
        if ((overflowY === 'auto' || overflowY === 'scroll') && scrollParent.scrollHeight > scrollParent.clientHeight) {
          scrollParent.scrollTop += e.deltaY;
          break;
        }
        scrollParent = scrollParent.parentElement;
      }
    }
  }, { capture: true, passive: false });

  // 拖动滚动条时用 scroll 事件同步
  document.addEventListener('scroll', function(e) {
    if (e.target.classList && e.target.classList.contains('h-table-body')) {
      syncTransform(e.target);
    }
  }, true);

  function init() {
    document.querySelectorAll('.h-table-body').forEach(syncTransform);
    window.__syncTableTransform = syncTransform;
  }

  if (document.readyState === 'complete') {
    init();
  } else {
    window.addEventListener('load', init);
  }
  setTimeout(init, 300);
  setTimeout(init, 1000);
})();

// HeyUI Table 固定列 header/body 宽度与高度同步
// 问题1：leftWidth 基于 th.clientWidth（不含border），导致固定列宽度偏小，header和body宽度不一致
// 问题2：主表头和固定表头高度不一致（差1px，border计算差异）
// 修复：读取主表头内部 table 的实际高度，强制固定表头内部 table 设为相同高度
(function() {
  function syncFixedColumns(table) {
    var header = table.querySelector('.h-table-header');
    if (!header) return;

    // 用主表头内部 table 的高度作为标准（这是视觉高度）
    var headerTable = header.querySelector('table');
    if (!headerTable) return;
    var referenceHeight = headerTable.offsetHeight;

    // 左侧固定列
    var fixedHeaderLeft = table.querySelector('.h-table-fixed-header-left');
    var fixedBodyLeft = table.querySelector('.h-table-fixed-left');
    if (fixedHeaderLeft && fixedBodyLeft) {
      // 同步固定表头内部 table 高度
      var fhlTable = fixedHeaderLeft.querySelector('table');
      if (fhlTable && referenceHeight > 0 && fhlTable.offsetHeight !== referenceHeight) {
        fhlTable.style.height = referenceHeight + 'px';
      }
      // 同步 div 高度（让 div 高度也一致，避免 overflow:hidden 裁剪）
      if (referenceHeight > 0 && fixedHeaderLeft.offsetHeight !== referenceHeight) {
        fixedHeaderLeft.style.height = referenceHeight + 'px';
      }
      // body 与 header 宽度同步
      var headerWidth = fixedHeaderLeft.offsetWidth;
      if (headerWidth > 0 && fixedBodyLeft.offsetWidth !== headerWidth) {
        fixedBodyLeft.style.width = headerWidth + 'px';
      }
    }

    // 右侧固定列
    var fixedHeaderRight = table.querySelector('.h-table-fixed-header-right');
    var fixedBodyRight = table.querySelector('.h-table-fixed-right');
    if (fixedHeaderRight && fixedBodyRight) {
      var fhrTable = fixedHeaderRight.querySelector('table');
      if (fhrTable && referenceHeight > 0 && fhrTable.offsetHeight !== referenceHeight) {
        fhrTable.style.height = referenceHeight + 'px';
      }
      if (referenceHeight > 0 && fixedHeaderRight.offsetHeight !== referenceHeight) {
        fixedHeaderRight.style.height = referenceHeight + 'px';
      }
      var headerWidthR = fixedHeaderRight.offsetWidth;
      if (headerWidthR > 0 && fixedBodyRight.offsetWidth !== headerWidthR) {
        fixedBodyRight.style.width = headerWidthR + 'px';
      }
    }
  }

  function syncAll() {
    document.querySelectorAll('.h-table').forEach(syncFixedColumns);
  }

  var observer = new MutationObserver(function() {
    requestAnimationFrame(syncAll);
  });

  function observeTables() {
    document.querySelectorAll('.h-table').forEach(function(table) {
      observer.observe(table, { childList: true, subtree: true, attributes: true });
    });
  }

  window.addEventListener('resize', function() {
    requestAnimationFrame(syncAll);
  });

  function init() {
    syncAll();
    observeTables();
    window.__syncTableAll = syncAll;
  }

  if (document.readyState === 'complete') {
    init();
  } else {
    window.addEventListener('load', init);
  }
  setTimeout(init, 300);
  setTimeout(init, 800);
  setTimeout(init, 2000);
})();


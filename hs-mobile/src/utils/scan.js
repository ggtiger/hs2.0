/**
 * 扫码工具封装
 *
 * 多端兼容策略：
 * - 小程序/App：uni.scanCode 直接调起摄像头扫码
 * - H5：使用浏览器原生 BarcodeDetector API（Chrome 88+），
 *       先拍照（chooseImage）再从图片识别条码；
 *       不支持 BarcodeDetector 时降级为手动输入
 */

/**
 * 调起扫码
 * @param {Object} options
 * @param {string[]} [options.scanType] 扫码类型 ['qrCode','barCode']
 * @param {boolean} [options.onlyFromCamera] 仅相机
 * @returns {Promise<{result:string, manual?:boolean}>}
 */
export function scanCode(options = {}) {
  return new Promise((resolve, reject) => {
    // #ifdef MP-WEIXIN || APP-PLUS
    uni.scanCode({
      onlyFromCamera: options.onlyFromCamera || false,
      scanType: options.scanType || ['qrCode', 'barCode'],
      success: (res) => resolve(res),
      fail: (err) => reject(err)
    })
    // #endif

    // #ifdef H5
    scanCodeH5(options, resolve, reject)
    // #endif
  })
}

// #ifdef H5

/**
 * H5 端扫码：拍照 + BarcodeDetector 识别
 * 流程：
 * 1. 调用 uni.chooseImage(sourceType=['camera']) 拍照
 * 2. 用 canvas 绘制图片，再通过 BarcodeDetector API 识别条码
 * 3. 识别成功返回结果，失败降级手动输入
 */
function scanCodeH5(options, resolve, reject) {
  // 检测 BarcodeDetector 支持
  const hasBarcodeDetector = typeof window !== 'undefined' && 'BarcodeDetector' in window

  uni.showActionSheet({
    itemList: hasBarcodeDetector ? ['拍照扫码', '手动输入'] : ['手动输入'],
    success: (res) => {
      if (hasBarcodeDetector && res.tapIndex === 0) {
        // 拍照扫码
        doCameraScan(options, resolve, reject)
      } else {
        // 手动输入
        showManualInput(resolve, reject)
      }
    },
    fail: () => {
      reject(new Error('cancel'))
    }
  })
}

async function doCameraScan(options, resolve, reject) {
  try {
    const chooseRes = await new Promise((ok, fail) => {
      uni.chooseImage({
        count: 1,
        sizeType: ['compressed'],
        sourceType: ['camera'],
        success: ok,
        fail
      })
    })

    const tempFilePath = chooseRes.tempFilePaths[0]
    if (!tempFilePath) {
      showManualInput(resolve, reject)
      return
    }

    // 用 BarcodeDetector 从图片识别条码
    const result = await detectBarcodeFromImage(tempFilePath, options)
    if (result) {
      resolve({ result })
    } else {
      uni.showToast({ title: '未识别到条码，请手动输入', icon: 'none' })
      setTimeout(() => showManualInput(resolve, reject), 800)
    }
  } catch (e) {
    // 拍照取消或失败
    showManualInput(resolve, reject)
  }
}

function detectBarcodeFromImage(imageUrl, options) {
  return new Promise((resolve) => {
    const img = new Image()
    img.crossOrigin = 'anonymous'
    img.onload = async () => {
      try {
        // 根据传入的 scanType 映射 BarcodeDetector 格式
        const formatMap = {
          qrCode: 'qr_code',
          barCode: ['ean_13', 'ean_8', 'code_128', 'code_39', 'codabar', 'itf', 'upc_a', 'upc_e']
        }
        const scanTypes = options.scanType || ['qrCode', 'barCode']
        const formats = scanTypes.flatMap(t => formatMap[t] || []).flat()
        const uniqueFormats = [...new Set(formats)]

        const detector = new BarcodeDetector({ formats: uniqueFormats })
        const results = await detector.detect(img)
        if (results && results.length > 0) {
          resolve(results[0].rawValue)
          return
        }
      } catch (e) {
        console.warn('BarcodeDetector failed:', e)
      }
      resolve(null)
    }
    img.onerror = () => resolve(null)
    img.src = imageUrl
  })
}

function showManualInput(resolve, reject) {
  uni.showModal({
    title: '手动输入',
    editable: true,
    placeholderText: '请输入编号',
    showCancel: true,
    cancelText: '取消',
    confirmText: '确定',
    success: (res) => {
      if (res.confirm && res.content && res.content.trim()) {
        resolve({ result: res.content.trim() })
      } else if (res.confirm) {
        uni.showToast({ title: '请输入编号', icon: 'none' })
        // 再次弹出手动输入
        setTimeout(() => showManualInput(resolve, reject), 500)
      } else {
        reject(new Error('cancel'))
      }
    }
  })
}

// #endif

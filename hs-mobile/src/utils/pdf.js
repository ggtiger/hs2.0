/**
 * PDF 预览工具
 *
 * 后端接口：GET {5001base}/api/file/pdf/{fileId}（FileController.DownLoadPdf）
 * - fileId 为 VSS_FILES 表的文件 ID（证书/记录关联的附件文件）
 * - 无需认证，返回 application/pdf 流（OnlyOffice 自动转换 docx→pdf）
 *
 * 多端实现：
 * - H5：window.open 新窗口查看（走 vite proxy）
 * - 小程序/App：downloadFile + openDocument
 */
import { DATA_BASE_URL } from './config'

/**
 * 预览 PDF
 * @param {string} fileId VSS_FILES 文件 ID
 */
export function previewPdf(fileId) {
  if (!fileId) {
    uni.showToast({ title: '暂无可预览的文件', icon: 'none' })
    return
  }
  const url = `${DATA_BASE_URL}/api/file/pdf/${fileId}`

  // #ifdef H5
  window.open(url, '_blank')
  return
  // #endif

  // #ifndef H5
  uni.showLoading({ title: '加载中', mask: true })
  uni.downloadFile({
    url,
    success: (res) => {
      uni.hideLoading()
      if (res.statusCode === 200) {
        uni.openDocument({
          filePath: res.tempFilePath,
          fileType: 'pdf',
          showMenu: true,
          fail: () => uni.showToast({ title: '打开失败', icon: 'none' })
        })
      } else if (res.statusCode === 404) {
        uni.showToast({ title: 'PDF 文件生成中，请稍后', icon: 'none' })
      } else {
        uni.showToast({ title: '预览失败', icon: 'none' })
      }
    },
    fail: () => {
      uni.hideLoading()
      uni.showToast({ title: '下载失败', icon: 'none' })
    }
  })
  // #endif
}

/**
 * 从业务记录中提取可预览的文件 ID
 * 字段名以后端 VCK 视图为准，联调时补充
 */
export function pickFileId(record) {
  if (!record) return ''
  return record.FILEID || record.PDFID || record.CERTFILEID || record.FILEID2 || ''
}

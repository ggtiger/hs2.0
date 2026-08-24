/**
 * 物流业务 API 封装
 *
 * 对齐 PC 端 p-admin/src/pages/r02/m07/ 模块：
 * - searchAcceptBill → LI_M01/A06 模糊搜索受理单
 * - saveLogistics → R02_M07/A04 保存物流信息（MAIN+DTSA+DTS XML）
 */
import { call } from './db'

/**
 * 模糊搜索受理单（对齐 PC 端 add.vue searchBill 方法）
 * @param {string} keyword 受理单号关键字（≥2字符）
 * @returns {Promise<{list:Array,total:number}>}
 */
export function searchAcceptBill(keyword) {
  return call('LI_M01', 'A06', {
    PageSize: 20,
    PageIndex: 1,
    FilterParams: { INPUT: keyword }
  }).then((res) => ({
    list: (res && res.Items) || [],
    total: (res && res.TotalCount) || 0
  }))
}

/**
 * 构造 DataTable XML（对齐 PC 端 DataTable.getXML()）
 *
 * 格式：
 * <?xml version="1.0" encoding="UTF-8"?>
 * <VCK_LOGISTICS l="u" c="ID,REFID,..." t="">
 *   <a><r c0="" c1="值" .../></a>
 *   <m></m>
 *   <d></d>
 * </VCK_LOGISTICS>
 *
 * @param {string} resourceName 资源名（如 VCK_LOGISTICS）
 * @param {string[]} fields 字段名列表（必须按 ORM ENTRYNUM 排序）
 * @param {Object[]} addRows 新增行数据
 */
export function buildDataTableXML(resourceName, fields, addRows = []) {
  const filterValue = (v) => {
    if (v === undefined || v === null || (typeof v === 'number' && isNaN(v))) return ''
    if (v instanceof Date) {
      const y = v.getFullYear()
      const m = String(v.getMonth() + 1).padStart(2, '0')
      const d = String(v.getDate()).padStart(2, '0')
      return `${y}-${m}-${d}`
    }
    return String(v)
  }

  const cAttr = fields.join(',')

  let xml = `<?xml version="1.0" encoding="UTF-8"?>`
  xml += `<${resourceName} l="u" c="${cAttr}" t="">`

  // 新增行 <a>
  xml += '<a>'
  addRows.forEach((row) => {
    let rAttr = ''
    fields.forEach((f, i) => {
      rAttr += ` c${i}="${encodeURIComponent(filterValue(row[f]))}"`
    })
    xml += `<r${rAttr}/>`
  })
  xml += '</a>'

  // 修改行 <m>（新增场景为空）
  xml += '<m></m>'

  // 删除行 <d>（新增场景为空）
  xml += '<d></d>'

  xml += `</${resourceName}>`
  return xml
}

/**
 * 保存物流信息（对齐 PC 端 Store03.save → R02_M07/A04）
 *
 * @param {Object} data 物流主表数据
 * @param {string} [data.REFTYPE='1'] 类型：1=样品，2=证书
 * @param {string} [data.EXPCOMPANY] 快递公司
 * @param {string} [data.LOGISTICSNO] 物流单号
 * @param {string} [data.SENDDATE] 寄出日期 yyyy-MM-dd
 * @param {string} [data.RECEIVENAME] 收件人
 * @param {string} [data.RECEIVEPHONE] 电话
 * @param {string} [data.RECEIVEADDR] 地址
 * @param {string} [data.REMARK] 备注
 * @param {string} [data.FILES] 图片文件ID列表（逗号分隔）
 * @param {Object[]} acceptList 关联受理单列表 [{ACCEPTID, ACCEPTCODE}]
 * @param {Object[]} [nodes=[]] 物流节点
 */
export function saveLogistics(data, acceptList = [], nodes = []) {
  // MAIN 字段列表（按 ORM 元数据 ENTRYNUM 排序）
  const mainFields = [
    'ID', 'REFID', 'REFTYPE', 'EXPCOMPANY', 'LOGISTICSNO',
    'SENDDATE', 'RECEIVENAME', 'RECEIVEPHONE', 'RECEIVEADDR',
    'STATUS', 'REMARK', 'FILES'
  ]

  const mainRow = {
    ID: '',                            // 后端 GUID 自动生成
    REFID: '',                         // 旧字段保留空值，关联改用 DTSA 中间表
    REFTYPE: data.REFTYPE || '1',
    EXPCOMPANY: data.EXPCOMPANY || '',
    LOGISTICSNO: data.LOGISTICSNO || '',
    SENDDATE: data.SENDDATE || '',
    RECEIVENAME: data.RECEIVENAME || '',
    RECEIVEPHONE: data.RECEIVEPHONE || '',
    RECEIVEADDR: data.RECEIVEADDR || '',
    STATUS: '0',                       // 默认待寄送
    REMARK: data.REMARK || '',
    FILES: data.FILES || ''
  }

  const mainXML = buildDataTableXML('VCK_LOGISTICS', mainFields, [mainRow])

  // DTSA 字段列表（VCK_LOGISTICS_REF - 关联受理单）
  // LOGISTICSID 不传，由后端 ORM 通过 tss_moudlepathrel 自动填充（MAIN.ID → DTSA.LOGISTICSID）
  const dtsaFields = ['ID', 'ACCEPTID', 'ACCEPTCODE']
  const dtsaRows = acceptList.map((a) => ({
    ID: '',
    ACCEPTID: a.ACCEPTID,
    ACCEPTCODE: a.ACCEPTCODE || ''
  }))

  const dtsaXML = buildDataTableXML('VCK_LOGISTICS_REF', dtsaFields, dtsaRows)

  // DTS 字段列表（VCK_LOGISTICS_NODE）
  // LOGISTICSID 不传，由后端 ORM 通过 tss_moudlepathrel 自动填充
  const dtsFields = ['ID', 'NODETIME', 'NODEDESC', 'NODEIMAGE']
  const dtsRows = nodes.map((n) => ({
    ID: '',
    NODETIME: n.NODETIME || n.NODE_TIME || '',
    NODEDESC: n.NODEDESC || n.NODE_DESC || '',
    NODEIMAGE: n.NODEIMAGE || n.NODE_IMAGE || ''
  }))

  const dtsXML = buildDataTableXML('VCK_LOGISTICS_NODE', dtsFields, dtsRows)

  // A04 APIPARAM='MAIN,DTSA,DTS'，需要传三个 PATHNAME 的 XML
  return call('R02_M07', 'A04', {
    MAIN: mainXML,
    DTSA: dtsaXML,
    DTS: dtsXML
  })
}

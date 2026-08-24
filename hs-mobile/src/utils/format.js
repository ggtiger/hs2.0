/**
 * 格式化工具：日期、金额、编号等
 */
import dayjs from 'dayjs'

/**
 * 格式化日期时间
 * @param {string|number|Date} val
 * @param {string} fmt 默认 'YYYY-MM-DD HH:mm'
 */
export function formatDateTime(val, fmt = 'YYYY-MM-DD HH:mm') {
  if (!val) return ''
  const d = dayjs(val)
  return d.isValid() ? d.format(fmt) : ''
}

/** 仅日期 */
export function formatDate(val) {
  return formatDateTime(val, 'YYYY-MM-DD')
}

/** 相对时间（如「3小时前」） */
export function formatRelative(val) {
  if (!val) return ''
  const diff = dayjs().diff(dayjs(val), 'minute')
  if (diff < 1) return '刚刚'
  if (diff < 60) return `${diff}分钟前`
  if (diff < 1440) return `${Math.floor(diff / 60)}小时前`
  if (diff < 43200) return `${Math.floor(diff / 1440)}天前`
  return formatDate(val)
}

/**
 * 格式化金额（分→元，或元→元带千分位）
 * @param {number} val
 * @param {boolean} fromCent 是否从分转换
 */
export function formatMoney(val, fromCent = false) {
  if (val === null || val === undefined || val === '') return '0.00'
  let num = Number(val)
  if (isNaN(num)) return '0.00'
  if (fromCent) num = num / 100
  return num.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',')
}

/**
 * 空值占位
 */
export function placeholder(val, text = '—') {
  return val === null || val === undefined || val === '' ? text : val
}

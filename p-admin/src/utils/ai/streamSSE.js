/**
 * 统一 SSE 流解析工具
 *
 * 合并两份历史实现：
 * - api/aidev.js 的 tail 处理（split('\n\n') + pop 保留尾部不完整片段）
 * - api/sfc-ai.js 的多行 data: 拼接（一帧内多行 data: 拼成一个 JSON）
 *
 * 兼容 "data:" 与 "data: " 两种前缀，解析失败的帧跳过不中断流。
 */

/**
 * 流式读取 fetch Response 的 SSE，按帧解析为 JSON 事件并回调。
 * @param {Response} response - fetch 返回的 Response 对象（必须含 body）
 * @param {function(object):void} onEvent - 每个解析出的事件 JSON 回调
 * @returns {Promise<void>}
 */
export async function streamSSE(response, onEvent) {
  if (!response.ok && !response.body) {
    throw new Error('流式请求失败: HTTP ' + response.status);
  }
  var reader = response.body.getReader();
  var decoder = new TextDecoder('utf-8');
  var buffer = '';

  while (true) {
    var chunk = await reader.read();
    if (chunk.done) break;
    buffer += decoder.decode(chunk.value, { stream: true });

    // SSE 帧以 "\n\n" 分隔，pop 保留最后不完整的片段（tail 处理）
    var events = buffer.split('\n\n');
    buffer = events.pop();
    for (var i = 0; i < events.length; i++) {
      var parsed = parseFrame(events[i]);
      if (parsed) {
        try { onEvent(parsed) } catch (e) { /* 回调异常不中断流 */ }
      }
    }
  }

  // 流结束后处理 buffer 中残留的最后一块
  var tail = parseFrame(buffer);
  if (tail) {
    try { onEvent(tail) } catch (e) { /* ignore */ }
  }
}

/**
 * 解析单个 SSE 帧：合并多行 data: 前缀为一行 JSON
 * 兼容 "data:" 与 "data: " 两种前缀
 * @param {string} frame - 单个 SSE 帧（不含结尾 "\n\n"）
 * @returns {object|null} 解析出的 JSON 对象，解析失败返回 null
 */
function parseFrame(frame) {
  if (!frame) return null;
  var lines = frame.split('\n');
  var dataLine = '';
  for (var i = 0; i < lines.length; i++) {
    var line = lines[i];
    if (line.indexOf('data: ') === 0) {
      dataLine += line.substring(6);
    } else if (line.indexOf('data:') === 0) {
      dataLine += line.substring(5);
    }
  }
  dataLine = dataLine.trim();
  if (!dataLine) return null;
  try {
    return JSON.parse(dataLine);
  } catch (e) {
    return null;
  }
}

export default streamSSE;

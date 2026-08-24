// mock 掉 api/assistant，避免 import 链触发 store/fetch
jest.mock('@/api/assistant', () => ({ sendMessage: jest.fn() }))

import assistant from '@/store/modules/assistant'

describe('assistant store mutations', () => {
  it('连续 text 块累加，实现打字效果', () => {
    const state = { messages: [{ role: 'assistant', blocks: [] }] }
    assistant.mutations.APPEND_BLOCK(state, { type: 'text', text: '你' })
    assistant.mutations.APPEND_BLOCK(state, { type: 'text', text: '好' })
    expect(state.messages[0].blocks.length).toBe(1)
    expect(state.messages[0].blocks[0].text).toBe('你好')
  })

  it('非 text 块独立成块，不累加到 text', () => {
    const state = { messages: [{ role: 'assistant', blocks: [{ type: 'text', text: 'a' }] }] }
    assistant.mutations.APPEND_BLOCK(state, { type: 'thinking', text: '思考' })
    expect(state.messages[0].blocks.length).toBe(2)
  })

  it('空消息列表时 APPEND_BLOCK 安全无操作', () => {
    const state = { messages: [] }
    expect(() => assistant.mutations.APPEND_BLOCK(state, { type: 'text', text: 'x' })).not.toThrow()
  })
})

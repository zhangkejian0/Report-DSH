/** Locale-owned WordInWeb renderer labels and status text. */
export const zh = {
  title: 'Word（WordInWeb）',
  loading: '文档渲染中...',
  failed: '无法渲染这个 Word 文档：{message}',
  unsupported: 'Word 预览需要完整文件内容',
  editMode: '编辑',
  editorTitle: '编辑：{name}',
  save: '保存',
  saving: '保存中...',
  saved: '已保存',
  saveFailed: '保存失败：{message}',
  download: '下载已编辑文档',
  close: '关闭',
  chatToggle: 'AI 对话',
  chatTitle: 'AI 编辑助手（临时对话）',
  chatHint: '用一句话描述要做的修改，AI 会直接编辑当前文档并自动保存。例如：把标题改为“2026 年度检测报告”，或在概述后新增一段结论。',
  chatPlaceholder: '描述要做的修改…（Ctrl+Enter 发送）',
  chatSend: '发送',
  chatThinking: '正在编辑文档…',
  chatWaiting: '已发送到 DSH 会话，AI 正在编辑当前文档…',
  chatInstruction: '请使用 word_document_* 工具编辑工作区文件"{path}"：{instruction}。完成后必须调用 word_document_save 保存文件。',
  chatApplied: '✓ AI 已完成编辑并保存，文档已刷新。详细过程见左侧会话。',
  chatPending: '已提交到会话。本轮未检测到文件变化，可到左侧会话查看进展。',
  chatFailed: '出错了：{message}',
} satisfies Record<string, string>

/** WordInWeb renderer dictionary keys. */
export type WordinwebPreviewKey = keyof typeof zh

/** English dictionary with the same keys as the Chinese dictionary. */
export const en = {
  title: 'Word (WordInWeb)',
  loading: 'Rendering document...',
  failed: 'This Word document could not be rendered: {message}',
  unsupported: 'Word preview requires the complete file contents.',
  editMode: 'Edit',
  editorTitle: 'Edit: {name}',
  save: 'Save',
  saving: 'Saving...',
  saved: 'Saved',
  saveFailed: 'Save failed: {message}',
  download: 'Download edited document',
  close: 'Close',
  chatToggle: 'AI chat',
  chatTitle: 'AI edit assistant (temporary chat)',
  chatHint: 'Describe the change in one sentence; the AI edits this document directly and saves it.',
  chatPlaceholder: 'Describe the change… (Ctrl+Enter to send)',
  chatSend: 'Send',
  chatThinking: 'Editing document…',
  chatWaiting: 'Sent to the DSH session; the AI is editing this document…',
  chatInstruction: 'Edit the workspace file "{path}" with the word_document_* tools: {instruction}. Call word_document_save when done.',
  chatApplied: '✓ The AI finished and saved; the document refreshed. See the session for details.',
  chatPending: 'Submitted to the session. No file change detected this round; check the session for progress.',
  chatFailed: 'Failed: {message}',
} satisfies Record<WordinwebPreviewKey, string>

declare module '@deepseek-ai/dsh-client-ui-slots' {
  interface LocaleNamespaceMap {
    /** WordInWeb preview selection and status text. */
    sidebarWordinweb: WordinwebPreviewKey
  }
}

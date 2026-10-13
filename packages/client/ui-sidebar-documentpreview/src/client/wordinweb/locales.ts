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
  chatTitle: 'AI 编辑助手',
  chatHint: '描述要做的修改。发送后会在本工作区自动创建一个新会话，由该会话的 AI 使用 word_document_* 工具直接编辑并保存当前文档；点「查看对话」可看到与主界面完全一致的完整会话。',
  chatCreating: '正在工作区创建新会话…',
  chatWaiting: 'AI 正在通过会话编辑当前文档，完成后自动刷新…',
  chatOpenSession: '查看对话',
  chatPlaceholder: '描述要做的修改…（Ctrl+Enter 发送）',
  chatSend: '发送',
  chatThinking: '正在编辑文档…',
  chatInstruction: '请使用 word_document_* 工具编辑工作区文件"{path}"：{instruction}。完成后必须调用 word_document_save 保存文件。',
  chatApplied: '✓ AI 已完成编辑并保存，文档已刷新。点「查看对话」可看完整过程。',
  chatPending: '本轮未检测到文件变化。点「查看对话」到会话中查看进展。',
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
  chatTitle: 'AI edit assistant',
  chatHint: 'Describe the change. Sending auto-creates a new session in this workspace whose AI edits and saves the document with the word_document_* tools; open the conversation to see the identical native session UI.',
  chatCreating: 'Creating a new session in the workspace…',
  chatWaiting: 'The AI is editing this document via the session; it refreshes on completion…',
  chatOpenSession: 'Open conversation',
  chatPlaceholder: 'Describe the change… (Ctrl+Enter to send)',
  chatSend: 'Send',
  chatThinking: 'Editing document…',
  chatInstruction: 'Edit the workspace file "{path}" with the word_document_* tools: {instruction}. Call word_document_save when done.',
  chatApplied: '✓ The AI finished and saved; the document refreshed. Open the conversation for details.',
  chatPending: 'No file change detected. Open the conversation for progress.',
  chatFailed: 'Failed: {message}',
} satisfies Record<WordinwebPreviewKey, string>

declare module '@deepseek-ai/dsh-client-ui-slots' {
  interface LocaleNamespaceMap {
    /** WordInWeb preview selection and status text. */
    sidebarWordinweb: WordinwebPreviewKey
  }
}

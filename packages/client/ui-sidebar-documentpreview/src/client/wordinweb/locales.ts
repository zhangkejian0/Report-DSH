/** Locale-owned WordInWeb renderer labels and status text. */
export const zh = {
  title: 'Word（WordInWeb）',
  loading: '文档渲染中...',
  failed: '无法渲染这个 Word 文档：{message}',
  unsupported: 'Word 预览需要完整文件内容',
  editMode: '编辑',
  loadFailed: '无法读取文档：{message}',
  sessionFailed: '无法创建编辑会话：{message}',
  save: '保存',
  saving: '保存中...',
  saved: '已保存',
  saveFailed: '保存失败：{message}',
  download: '下载已编辑文档',
  chatOpenSession: '查看对话',
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
  loadFailed: 'This document could not be read: {message}',
  sessionFailed: 'The edit session could not be created: {message}',
  save: 'Save',
  saving: 'Saving...',
  saved: 'Saved',
  saveFailed: 'Save failed: {message}',
  download: 'Download edited document',
  chatOpenSession: 'Open conversation',
} satisfies Record<WordinwebPreviewKey, string>

declare module '@deepseek-ai/dsh-client-ui-slots' {
  interface LocaleNamespaceMap {
    /** WordInWeb preview selection and status text. */
    sidebarWordinweb: WordinwebPreviewKey
  }
}

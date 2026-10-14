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
  sessionInstruction: '这是「{path}」的编辑会话。后续修改要求都针对该文件，请使用 word_document_* 工具编辑它，并在完成后调用 word_document_save 保存。现在请只确认已准备好，不要修改文档。',
  sessionReady: '已在工作区创建编辑会话',
  'saveAs.title': '另存为模版',
  'saveAs.saving': '保存中...',
  'saveAs.saved': '已存入报告模版',
  'saveAs.failed': '另存失败：{message}',
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
  sessionInstruction: 'This session edits "{path}". Apply later requests to that file with the word_document_* tools and call word_document_save when done. For now, only confirm you are ready; do not modify the document.',
  sessionReady: 'Created an edit session in the workspace',
  'saveAs.title': 'Save as template',
  'saveAs.saving': 'Saving...',
  'saveAs.saved': 'Saved to 报告模版',
  'saveAs.failed': 'Save as template failed: {message}',
} satisfies Record<WordinwebPreviewKey, string>

declare module '@deepseek-ai/dsh-client-ui-slots' {
  interface LocaleNamespaceMap {
    /** WordInWeb preview selection and status text. */
    sidebarWordinweb: WordinwebPreviewKey
  }
}

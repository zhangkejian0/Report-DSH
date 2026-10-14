/** Report-wizard copy: template choice, image preflight, output, and generation states. */

/** Chinese dictionary, the key authority. */
export const zh = {
  'title': '生成报告',
  'guide.title': '生成报告',
  'guide.description': '选报告模版与热像图，一键生成 Word 检测报告',
  'section.templates': '① 报告模版',
  'section.images': '② 选择热像图',
  'section.output': '③ 输出',
  'templates.empty': '「报告模版」目录还没有模版：放入任意 .docx，或先创建一份示范模版再照着改。',
  'templates.createSample': '创建示范模版',
  'templates.creating': '正在写入示范模版…',
  'templates.refresh': '刷新',
  'templates.failed': '模版目录读取失败：{message}',
  'directory.root': '工作区根目录',
  'directory.up': '上一级',
  'directory.failed': '目录读取失败：{message}',
  'images.empty': '此目录没有 jpg/jpeg 图片。',
  'images.selected': '已选 {count} 张',
  'images.selectAll': '全选本目录',
  'images.clear': '清除选择',
  'images.analyzing': '分析中…',
  'images.max': '最高 {max}℃',
  'images.failed': '分析失败',
  'output.name': '报告名',
  'output.location': '位置',
  'output.invalidName': '报告名不能包含 / \\ : * ? " < > | 或为空',
  'preflight': '{ok}/{total} 张分析成功',
  'generate': '生成报告',
  'generating': '正在下发任务…',
  'generate.needTemplate': '请先选择模版',
  'generate.needImages': '请至少勾选一张分析成功的图片',
  'error.sessionCreate': '创建报告会话失败：{message}',
  'error.write': '写入报告素材失败：{message}',
  'error.prompt': '任务下发失败：{message}',
  'fullscreen': '进入全屏',
} satisfies Record<string, string>

/** Report-wizard dictionary key union. */
export type SidebarReportKey = keyof typeof zh

/** English dictionary, checked against the Chinese key set. */
export const en: Record<SidebarReportKey, string> = {
  'title': 'Generate report',
  'guide.title': 'Generate report',
  'guide.description': 'Pick a report template and thermal images, then generate a Word report in one click',
  'section.templates': '① Report template',
  'section.images': '② Thermal images',
  'section.output': '③ Output',
  'templates.empty': 'The 报告模版 directory has no template yet: drop any .docx in, or create the sample template first and adapt it.',
  'templates.createSample': 'Create sample template',
  'templates.creating': 'Writing the sample template…',
  'templates.refresh': 'Refresh',
  'templates.failed': 'Could not read the template directory: {message}',
  'directory.root': 'workspace root',
  'directory.up': 'Up one level',
  'directory.failed': 'Could not read the directory: {message}',
  'images.empty': 'No jpg/jpeg images in this directory.',
  'images.selected': '{count} selected',
  'images.selectAll': 'Select all here',
  'images.clear': 'Clear selection',
  'images.analyzing': 'Analyzing…',
  'images.max': 'max {max}℃',
  'images.failed': 'analysis failed',
  'output.name': 'Report name',
  'output.location': 'Location',
  'output.invalidName': 'The report name is empty or contains / \\ : * ? " < > |',
  'preflight': '{ok}/{total} images analyzed',
  'generate': 'Generate report',
  'generating': 'Dispatching…',
  'generate.needTemplate': 'Pick a template first',
  'generate.needImages': 'Select at least one successfully analyzed image',
  'error.sessionCreate': 'Report session creation failed: {message}',
  'error.write': 'Writing report assets failed: {message}',
  'error.prompt': 'Dispatching the task failed: {message}',
  'fullscreen': 'Fullscreen',
}

declare module '@deepseek-ai/dsh-client-ui-slots' {
  interface LocaleNamespaceMap {
    /** Report-wizard tab, guide entry, and generation states. */
    sidebarReport: SidebarReportKey
  }
}

/** The built-in templates: the composed sample and the blank starter, written into 报告模版/ on demand. */

/**
 * The sample template the wizard offers to write when the template directory
 * is empty: cover token, global info table, conclusion, one {{#each_image}}
 * single-image section, and a closing note. Composed once with the WordInWeb
 * agent toolkit and embedded so no Host round trip is needed to seed it.
 */
export const SAMPLE_TEMPLATE_BASE64 = [
  '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',
  '',
].join('')

/**
 * The blank template 「新建空白模版」 writes: the sample template's package
 * with the body reduced to one empty paragraph on an A4 page, so manual
 * authoring starts from zero in a file every Word tool in this product opens.
 * word/document.xml is stored uncompressed, keeping the empty body readable
 * to tests that scan the decoded bytes.
 */
export const BLANK_TEMPLATE_BASE64 = [
  'UEsDBBQAAAAIAABAIVAE4Gyb6wAAAIoCAAATAAAAW0NvbnRlbnRfVHlwZXNdLnhtbK2SsU7DMBCGXyXyimqnDAwoSQdgBQZewHIu',
  'iYXts+6uwX17lKZ0QFW7dL7//79vuGZXYqhmIPaYWrXVtdp1zdchA1clhsStmkTyszHsJoiWNWZIJYYBKVphjTSabN23HcE81vWT',
  'cZgEkmxk2VBd8wqD3Qep3opAWiklBlW9rLkF1Sqbc/DOisdklqu52CMIfKU4p/6f3eZkpgnCMcOTz/xwInzMQOR7qD4tybuN0Crz',
  'g9SbHt0+QhJ9XfQCD4fBOzj3l7VM6IDZpzEGfb5E69NND5ZDAL6/xbp7Ez8gCtD2/vx1+I9vjt/W/QJQSwMEFAAAAAgAAEAhUF8z',
  'lVKXAAAABwEAAAsAAABfcmVscy8ucmVsc43PsQ7CIBQF0F8h7wP6WgcHU9rFpavpDxB4bYnAI4CKf+/iYI2D683Nubn9WL0Td0rZ',
  'cpDQNS2MQ38hp4rlkDcbs6jehSxhKyWeELPeyKvccKRQvVs4eVVyw2nFqPRVrYSHtj1i+jRgb4rJSEiT6UDMz0j/2LwsVtOZ9c1T',
  'KD8mvhogZpVWKhIenAyad9xU7wCHHncXhxdQSwMEFAAAAAAAAAAhAEueEk6nAQAApwEAABEAAAB3b3JkL2RvY3VtZW50LnhtbDw/',
  'eG1sIHZlcnNpb249IjEuMCIgZW5jb2Rpbmc9IlVURi04IiBzdGFuZGFsb25lPSJ5ZXMiPz48dzpkb2N1bWVudCB4bWxuczp3PSJo',
  'dHRwOi8vc2NoZW1hcy5vcGVueG1sZm9ybWF0cy5vcmcvd29yZHByb2Nlc3NpbmdtbC8yMDA2L21haW4iIHhtbG5zOnI9Imh0dHA6',
  'Ly9zY2hlbWFzLm9wZW54bWxmb3JtYXRzLm9yZy9vZmZpY2VEb2N1bWVudC8yMDA2L3JlbGF0aW9uc2hpcHMiPjx3OmJvZHk+PHc6',
  'cC8+PHc6c2VjdFByPjx3OmZvb3RlclJlZmVyZW5jZSB3OnR5cGU9ImRlZmF1bHQiIHI6aWQ9InJJZDMiLz48dzpwZ1N6IHc6dz0i',
  'MTE5MDYiIHc6aD0iMTY4MzgiLz48dzpwZ01hciB3OnRvcD0iMzYwMCIgdzpyaWdodD0iMjg4MCIgdzpib3R0b209IjM2MDAiIHc6',
  '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',
].join('')

/**
 * The usage note written beside the sample template, so template authors see
 * the token vocabulary and the repeatable-section convention in place.
 */
export const SAMPLE_TEMPLATE_NOTE = `# 报告模版说明

本目录下的 .docx 都是报告模版。快速生成时本地引擎按模版逐字展开；AI 生成时由 AI 按同一占位符填充，另存到「报告/」目录。

## 占位符（可选）

放在文本里即可，生成时自动替换；不放占位符的段落保持原样。除内置占位符外，可在本目录放一份
「<模版名>.tokens.json」字段配置：新增自定义字段（中文可）、改内置字段取值与格式、声明生成时
要填的著录项（向导会动态出输入框）。详见「字段配置」。

- 全局（填一次）：{{report_title}} {{company}} {{inspector}} {{date}} {{device}} {{camera_model}} {{camera_serial}} {{overall_conclusion}}
- 汇总：{{image_count}} {{overall_max_temp}} {{overall_max_at}} {{overall_max_image}} {{overall_min_temp}} {{overall_avg_temp}}
- 单图（每张选中的热像图一份）：{{image_file}} {{image_thermal}}（插图位） {{t_max}} {{t_max_at}} {{t_min}} {{t_avg}} {{marker_table}} {{image_comment}}

## 重复节（每张图一份）

用 {{#each_image}} 与 {{/each_image}} 标记，两种位置语义：

1. 正文段落：两个标记各占一段，把一图的完整版面括起来——括起内容按原样为每张图复制一份（推荐）。
2. 表格行：两个标记放在同一表格行的单元格里——该行为每张图复制一行，适合检测汇总表。

标记所在的段落/单元格只放标记本身；其余内容保持模板样式。快速生成要求必须有标记；AI 生成还支持样例仿写。

## 版式提示

- 每图一页：把单图节第一段设为「段前分页」（Word 段落设置 → 换行和分页），复制后每节自然从新页开始。
- 目录：用 Word 目录域；生成后 Word 打开时会自动刷新页码。

## 修改模版

直接在本目录用任意 Word 工具编辑 .docx；也可以用「编辑」功能让 AI 帮忙改结构、加占位符。

## 字段配置（<模版名>.tokens.json）

与模版同名的 .tokens.json 覆盖/扩展内置字段。三种绑定：

- 取值：{ "最高温": { "path": "stats.max", "format": "celsius", "scope": "image" } }（image 作用域内按分析 JSON 路径取值）
- 著录：{ "检测地点": { "input": { "label": "检测地点" } } }（向导生成时询问）
- 派生：{ "结论": { "template": "最高温 {{t_max}}" } }（引用其它占位符）

格式：text / celsius / number:N / coord / percent0 / date-cn / basename / marker-summary。
免配置也可直接写 {{analysis.params.distance|number:1}} 式的路径占位符。"extends": false 可整体替换内置表。
`
/**
 * Base64-encode UTF-8 text; `btoa` alone rejects characters beyond Latin-1.
 * @param text - UTF-8 text to encode.
 * @returns the base64 form of the text's bytes.
 */
export function base64OfText(text: string): string {
  const bytes = new TextEncoder().encode(text)
  let binary = ''
  const chunk = 0x8000
  for (let offset = 0; offset < bytes.length; offset += chunk) {
    binary += String.fromCharCode(...bytes.subarray(offset, offset + chunk))
  }
  return btoa(binary)
}

/** The sample sidecar: an added input field and a path-bound field, over the built-in vocabulary. */
export const SAMPLE_TEMPLATE_SIDECAR = JSON.stringify({
  version: 1,
  fields: {
    '检测地点': { input: { label: '检测地点' } },
    '发射率': { path: 'analysis.params.emissivity', format: 'number:2', scope: 'image' },
  },
}, null, 2)

/**
 * The file set the sample seeding writes: the composed template, its note,
 * and its field sidecar.
 * @returns workspace-relative path and base64 bytes for each file.
 */
export function sampleTemplateFiles(): readonly { readonly path: string; readonly base64: string }[] {
  return [
    { path: '报告模版/示范模版.docx', base64: SAMPLE_TEMPLATE_BASE64 },
    { path: '报告模版/示范模版.tokens.json', base64: base64OfText(SAMPLE_TEMPLATE_SIDECAR) },
    { path: '报告模版/模版说明.md', base64: base64OfText(SAMPLE_TEMPLATE_NOTE) },
  ]
}

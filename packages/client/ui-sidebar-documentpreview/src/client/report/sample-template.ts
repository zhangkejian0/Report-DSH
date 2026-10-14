/** The built-in sample template: one composed DOCX as base64, written into 报告模版/ on first use. */

/**
 * The sample template the wizard offers to write when the template directory
 * is empty: cover token, global info table, conclusion, one {{#each_image}}
 * single-image section, and a closing note. Composed once with the WordInWeb
 * agent toolkit and embedded so no Host round trip is needed to seed it.
 */
export const SAMPLE_TEMPLATE_BASE64 = [
  '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',
  'bWxQSwUGAAAAAAYABgB+AQAAhwoAAAAA',
].join('')

/**
 * The usage note written beside the sample template, so template authors see
 * the token vocabulary and the repeatable-section convention in place.
 */
export const SAMPLE_TEMPLATE_NOTE = `# 报告模版说明

本目录下的 .docx 都是报告模版。生成报告时，向导把热像分析数据交给 AI，AI 按模版的结构和样式填充，另存到「报告/」目录。

## 占位符（可选）

放在文本里即可，生成时自动替换；不放占位符时 AI 会按模版既有结构仿写。

- 全局：{{report_title}} {{company}} {{inspector}} {{date}} {{device}} {{camera_model}} {{camera_serial}} {{overall_conclusion}}
- 单图（每张选中的热像图一节）：{{image_file}} {{image_thermal}}（插图位） {{t_max}} {{t_max_at}} {{t_min}} {{t_avg}} {{marker_table}} {{image_comment}}

## 单图节的两种写法

1. 显式括起：用 {{#each_image}} 和 {{/each_image}} 两个独占段落把"单图分析"一节括起来——括起内容按原样为每张图复制一份（推荐，结果最可预期）。
2. 样例仿写：不做括起，写一个"样例单图节"，AI 仿照它的结构为每张图生成。

## 修改模版

直接在本目录用任意 Word 工具编辑 .docx；也可以用「编辑」功能让 AI 帮忙改结构、加占位符。
`

/** Base64-encode UTF-8 text; `btoa` alone rejects characters beyond Latin-1. */
export function base64OfText(text: string): string {
  const bytes = new TextEncoder().encode(text)
  let binary = ''
  const chunk = 0x8000
  for (let offset = 0; offset < bytes.length; offset += chunk) {
    binary += String.fromCharCode(...bytes.subarray(offset, offset + chunk))
  }
  return btoa(binary)
}

/**
 * The file pair the sample seeding writes: the composed template and its note.
 * @returns workspace-relative path and base64 bytes for each file.
 */
export function sampleTemplateFiles(): readonly { readonly path: string; readonly base64: string }[] {
  return [
    { path: '报告模版/示范模版.docx', base64: SAMPLE_TEMPLATE_BASE64 },
    { path: '报告模版/模版说明.md', base64: base64OfText(SAMPLE_TEMPLATE_NOTE) },
  ]
}

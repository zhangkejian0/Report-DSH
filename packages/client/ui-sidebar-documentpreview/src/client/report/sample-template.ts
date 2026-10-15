/** The built-in templates: the composed sample and the blank starter, written into 报告模版/ on demand. */

/**
 * The sample template the wizard offers to write when the template directory
 * is empty: cover token, global info table, conclusion, one {{#each_image}}
 * single-image section, and a closing note. Composed once with the WordInWeb
 * agent toolkit and embedded so no Host round trip is needed to seed it.
 */
export const SAMPLE_TEMPLATE_BASE64 = [
  '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',
  'bWxQSwUGAAAAAAYABgB+AQAAigoAAAAA',
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
  'bGVmdD0iMjg4MCIvPjwvdzpzZWN0UHI+PC93OmJvZHk+PC93OmRvY3VtZW50PlBLAwQUAAAACAAAQCFQ/vAV1aUAAAB4AQAAHAAA',
  'AHdvcmQvX3JlbHMvZG9jdW1lbnQueG1sLnJlbHOt0LEOgjAUheFXae4DtKCJgwFcXFgNL9DABRrb3qa3mvL2DsQoCYOD41n+LznV',
  'JTsrnhjZkK+hlAVcmuqGVidDnmcTWGRnPdcwpxTOSnE/o9MsKaDPzo4UnU4sKU4q6P6uJ1SHojip+N2AbVO0Qw2xHUoQ3RLwlzaN',
  'o+nxSv3DoU87hOK0WGQQnY4TphrWLbOzoPb54z/5kShh/PDrLt++2nzavABQSwMEFAAAAAgAAEAhUOWAl1D3AQAAxgcAAA8AAAB3',
  'b3JkL3N0eWxlcy54bWy9k21vmzAQx78K8vuFp0AYKq3abWiTpm7Sug/gwAW8GduznUD26ScMhBWI2ipVxQu4P+d7+N356qapqHUA',
  'qQhnCXJXDrKAZTwnrEjQz4f0XYQspTHLMeUMEnQEhW6ur+pY6SMFZTUVZSquE1RqLWLbVlkJFVYrLoA1Fd1xWWGtVlwWds1lLiTP',
  'QCnCioranuOEdoUJQ23AnGcfYYf3VKvWlN9lb/aWeaWcaWXVMVYZIQm6FZorZNVxecvUybZNfX+tOj5gmiDPG5QP6rFm94HtaTpx',
  'sjqvSW2md6uO9VFAggSWuJBYlG0heeeWILe1jOOXPEH3LQdq+mS4gqGMXjbl/UkNqy6hOfhEqlPwB6IpzGJ3qgm9xQryb2wpK4NG',
  'D/qP/VaPh8Z6DA9TjMAZYUXLf6dBJshbO53zr2wIkgHTIHu6/bl+elvjmnHK5eDtputPm/fTiQULEwumE3sRolNrU0qPe34mqOWx',
  'ncfkhy/BROaYgrsw2PizxY4WFju6BNNnwO3Nd2eYyu6H5b4mp98A4h6aThuhbWHHJXTURoau1zPke00Jg68HOqRxLts2f2HbOu1C',
  'jN5ZjN4bYmzv6IgxOkvRfS5FL90Ed8FsGcOFZQxfgaJ/lqL/hhTd6H+Kw32eUxz25kmKa6d9ZhTXCxSNNqc4fKnrf1BLAwQUAAAA',
  'CAAAQCFQb21SaQgBAAAdAwAAEAAAAHdvcmQvZm9vdGVyMS54bWzdkj1rw0AMhv/KobFQX9whFJNzCMXJ5DbkY+ty2LLjcl+cDtvt',
  'ry8Xt6HZumTJIgnpQXoltFiOWrEePXXWCEiTGTA0la070wo4HtaPz8AoSFNLZQ0K+ESCZb4YsiZ4NmplKBsEnEJwGedUnVBLSqxD',
  'M2rVWK9loMT6lg/W187bCok602rFn2azOdeyMxC7ubPZ+ug+KjZkvVQCKjQBPfB8wS/VyUwxff2S6TxSMfNC1zl+oUPUm5GTFQpw',
  'Hgl9j5BvZYssYmGCz8upet9pp5ANWWcoeAFsu9oU7P2BlcVuU6zfduXqwOAmitIrNfyPnJuMY7b5z/6vxzKeYH+PN/hBpj+bxgWf',
  'fwNQSwECFAAUAAAACAAAQCFQBOBsm+sAAACKAgAAEwAAAAAAAAAAAAAAgAEAAAAAW0NvbnRlbnRfVHlwZXNdLnhtbFBLAQIUABQA',
  'AAAIAABAIVBfM5VSlwAAAAcBAAALAAAAAAAAAAAAAACAARwBAABfcmVscy8ucmVsc1BLAQIUABQAAAAAAAAAIQBLnhJOpwEAAKcB',
  'AAARAAAAAAAAAAAAAACAAdwBAAB3b3JkL2RvY3VtZW50LnhtbFBLAQIUABQAAAAIAABAIVD+8BXVpQAAAHgBAAAcAAAAAAAAAAAA',
  'AACAAbIDAAB3b3JkL19yZWxzL2RvY3VtZW50LnhtbC5yZWxzUEsBAhQAFAAAAAgAAEAhUOWAl1D3AQAAxgcAAA8AAAAAAAAAAAAA',
  'AIABkQQAAHdvcmQvc3R5bGVzLnhtbFBLAQIUABQAAAAIAABAIVBvbVJpCAEAAB0DAAAQAAAAAAAAAAAAAACAAbUGAAB3b3JkL2Zv',
  'b3RlcjEueG1sUEsFBgAAAAAGAAYAfgEAAOsHAAAAAA==',
].join('')

/**
 * The usage note written beside the sample template, so template authors see
 * the token vocabulary and the repeatable-section convention in place.
 */
export const SAMPLE_TEMPLATE_NOTE = `# 报告模版说明

本目录下的 .docx 都是报告模版。快速生成时本地引擎按模版逐字展开；AI 生成时由 AI 按同一占位符填充，另存到「报告/」目录。

## 占位符（可选）

放在文本里即可，生成时自动替换；不放占位符的段落保持原样。

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

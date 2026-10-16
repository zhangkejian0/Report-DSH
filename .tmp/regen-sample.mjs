/** Regenerates the sample template with a summary row-loop table and aggregate tokens. */
const { AgentDocument } = await import('file:///D:/agworkspace/docgc/wordinweb/packages/agent/dist/index.js')

const doc = AgentDocument.create()
await doc.compose({
  revision: doc.revision,
  body: [
    { type: 'paragraph', text: '{{report_title}}', styleId: 'Title', align: 'center' },
    { type: 'paragraph', text: '红外热像检测报告', styleId: 'Subtitle', align: 'center' },
    { type: 'paragraph', text: '' },
    { type: 'paragraph', text: '检测单位' },
    { type: 'paragraph', text: '{{company}}' },
    { type: 'paragraph', text: '检测人员' },
    { type: 'paragraph', text: '{{inspector}}' },
    { type: 'paragraph', text: '检测日期' },
    { type: 'paragraph', text: '{{date}}' },
    { type: 'paragraph', text: '设备名称' },
    { type: 'paragraph', text: '{{device}}' },
    { type: 'paragraph', text: '检测地点' },
    { type: 'paragraph', text: '{{检测地点}}' },
    { type: 'paragraph', text: '仪器型号' },
    { type: 'paragraph', text: '{{camera_model}}' },
    { type: 'paragraph', text: '仪器编号' },
    { type: 'paragraph', text: '{{camera_serial}}' },
    { type: 'paragraph', text: '一、检测结论' },
    { type: 'paragraph', text: '{{overall_conclusion}}' },
    { type: 'paragraph', text: `共 {{image_count}} 张热像图，全场最高 {{overall_max_temp}}（{{overall_max_image}}），最低 {{overall_min_temp}}，平均 {{overall_avg_temp}}。` },
    { type: 'paragraph', text: '二、检测汇总' },
    { type: 'table', rows: [
      ['文件', '最高温度', '平均温度'],
      ['{{#each_image}} {{image_file}}', '{{t_max}} {{/each_image}}', '{{t_avg}}'],
    ], headerRows: 1 },
    { type: 'paragraph', text: '三、单图分析' },
    { type: 'paragraph', text: '{{#each_image}}' },
    { type: 'paragraph', text: '{{image_file}}', styleId: 'Heading2' },
    { type: 'paragraph', text: '热图：{{image_thermal}}' },
    { type: 'table', rows: [
      ['项目', '数值'],
      ['最高温度', '{{t_max}}（{{t_max_at}}）'],
      ['最低温度', '{{t_min}}'],
      ['平均温度', '{{t_avg}}'],
    ], headerRows: 1 },
    { type: 'paragraph', text: '发射率 {{发射率}}' },
    { type: 'paragraph', text: '标记统计：{{marker_table}}' },
    { type: 'paragraph', text: '分析意见：{{image_comment}}' },
    { type: 'paragraph', text: '{{/each_image}}' },
    { type: 'paragraph', text: '四、说明' },
    { type: 'paragraph', text: '本报告由热像分析数据自动生成，检测结果供设备状态评估参考。' },
  ],
  footer: [{ type: 'paragraph', text: '{{company}} · {{date}}' }],
})
const bytes = doc.save()
const base64 = Buffer.from(bytes).toString('base64')
const width = 76
const lines = []
for (let offset = 0; offset < base64.length; offset += width) {
  lines.push(`  '${base64.slice(offset, offset + width)}',`)
}
console.log(`const NEW_SAMPLE = [`)
console.log(lines.join('\n'))
console.log(`].join('')`)
console.error('bytes:', bytes.length)

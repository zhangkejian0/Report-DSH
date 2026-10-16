/** M0 spike step 2: full deterministic pipeline over the sample template.
 * core phase: clone the each_image block range per image, drop marker paragraphs, set updateFields.
 * agent phase: patch token text per instance, insert thermal images, save, verify residue.
 */
import { readFileSync, writeFileSync } from 'node:fs'
import { pathToFileURL } from 'node:url'

const coreUrl = pathToFileURL('D:/agworkspace/docgc/wordinweb/packages/core/dist/index.js').href
const agentUrl = pathToFileURL('D:/agworkspace/docgc/wordinweb/packages/agent/dist/index.js').href
const core = await import(coreUrl)
const { AgentDocument } = await import(agentUrl)
const { DocxDocument, serializeXml, parseXml, localName, child } = core

const src = readFileSync('D:/agworkspace/docgc/deepseek-harness/packages/client/ui-sidebar-documentpreview/src/client/report/sample-template.ts', 'utf8')
const base64 = Array.from(/SAMPLE_TEMPLATE_BASE64 = \[([\s\S]*?)\]/.exec(src)[1].matchAll(/'([^']*)'/g), m => m[1]).join('')
const templateBytes = new Uint8Array(Buffer.from(base64, 'base64'))

/** All w:t text of one element subtree, joined. */
function textOf(el) {
  let out = ''
  const walk = (node) => {
    if (localName(node.name) === 't') out += node.text ?? ''
    for (const c of node.children ?? []) walk(c)
  }
  walk(el)
  return out
}

const IMAGES = [
  { file: 'IR/ir0007.jpg', tMax: '42.31℃', tMaxAt: '(120,88)', tMin: '22.10℃', tAvg: '30.05℃', marker: 'Ar1 最高 42.31℃、最低 22.10℃、平均 30.05℃', comment: '最高温出现在 Ar1 区域，建议复核。' },
  { file: 'IR/ir0008.jpg', tMax: '38.11℃', tMaxAt: '(10,12)', tMin: '21.80℃', tAvg: '28.02℃', marker: 'Ar2 最高 38.11℃、最低 21.80℃、平均 28.02℃', comment: '温度分布均匀，未见异常。' },
]

// ---------- core phase: structure ----------
const doc = DocxDocument.load(templateBytes)
const blocks = doc.sections.flatMap(section => section.blocks)
const byText = (needle) => blocks.filter(b => b.type === 'paragraph' && textOf(b.src).includes(needle))
const open = byText('{{#each_image}}')
const close = byText('{{/each_image}}')
console.log('markers:', open.length, close.length)
if (open.length !== 1 || close.length !== 1) throw new Error('expected exactly one marker pair')

const parent = doc.findParentOf(open[0].src)
const closeParent = doc.findParentOf(close[0].src)
if (parent !== closeParent) throw new Error('markers live in different parents')
const children = parent.children
const openIndex = children.indexOf(open[0].src)
const closeIndex = children.indexOf(close[0].src)
if (openIndex === -1 || closeIndex === -1 || openIndex >= closeIndex) throw new Error('bad marker order')
const rangeElements = children.slice(openIndex + 1, closeIndex)
console.log('range blocks:', rangeElements.length, 'first/last:', localName(rangeElements[0].name), localName(rangeElements.at(-1).name))

// clone the whole range once per image, inserting after the open marker
let insertAt = openIndex + 1
for (let index = 0; index < IMAGES.length; index++) {
  for (const el of rangeElements) {
    const copy = parseXml(serializeXml(el))
    children.splice(insertAt, 0, copy)
    insertAt++
  }
}
// drop both marker paragraphs and the original template range (clones replace it)
children.splice(children.indexOf(close[0].src), 1)
for (const el of rangeElements) children.splice(children.indexOf(el), 1)
children.splice(children.indexOf(open[0].src), 1)

// updateFields: public upstream wrapper
 doc.setUpdateFields(true)

doc.refresh()
const structuredBytes = doc.save()
console.log('structured docx bytes:', structuredBytes.length)

// ---------- agent phase: text ----------
const agent = AgentDocument.load(structuredBytes)
let projection = agent.project({ mode: 'text' })
const lines = projection.text.split('\n')

// instance grouping: template range had 12 lines ({{image_file}} .. 分析意见 line)
const rangeLineCount = 12
const firstInstance = lines.findIndex(line => line.includes('{{image_file}}'))
if (firstInstance === -1) throw new Error('no instance lines after expansion')
console.log('first instance line:', firstInstance, 'total lines:', lines.length)

const valuesFor = (image) => ({
  '{{image_file}}': image.file,
  '{{t_max}}': image.tMax,
  '{{t_max_at}}': image.tMaxAt,
  '{{t_min}}': image.tMin,
  '{{t_avg}}': image.tAvg,
  '{{marker_table}}': image.marker,
  '{{image_comment}}': image.comment,
})
const globals = {
  '{{report_title}}': '变电站巡检报告',
  '{{company}}': '示例检测公司',
  '{{inspector}}': '张三',
  '{{date}}': '2026-10-15',
  '{{device}}': '1号主变',
  '{{camera_model}}': 'Fotric 358XS',
  '{{camera_serial}}': 'SN123',
  '{{overall_conclusion}}': '共 2 张热像图，最高 42.31℃。',
  '{{image_count}}': '2',
}

const hunks = []
lines.forEach((line, index) => {
  let replaced = line
  // per-image tokens inside an instance range
  const instance = Math.floor((index - firstInstance) / rangeLineCount)
  if (index >= firstInstance && instance >= 0 && instance < IMAGES.length) {
    for (const [token, value] of Object.entries(valuesFor(IMAGES[instance]))) replaced = replaced.replaceAll(token, value)
  }
  replaced = replaced.replaceAll('{{image_thermal}}', '')
  for (const [token, value] of Object.entries(globals)) replaced = replaced.replaceAll(token, value)
  // hunks are 1-based line addresses
  if (replaced !== line) hunks.push({ startLine: index + 1, endLine: index + 1, newText: replaced })
})
console.log('hunks:', hunks.length)
const patched = await agent.patch({ revision: projection.revision, mode: 'text', edits: hunks })
console.log('patch status:', patched.status, 'ops:', patched.operations.length)
projection = patched.projection

// ---------- agent phase: images ----------
// 1x1 red PNG
const png = Uint8Array.from(atob('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=='), c => c.charCodeAt(0))
const assetRef = agent.addAsset(png, 'image/png')
const afterLines = projection.text.split('\n')
const imageLine = afterLines.findIndex(line => line.includes('热图：'))
const anchor = projection.anchors[imageLine]
console.log('image line:', imageLine, JSON.stringify(afterLines[imageLine]), 'anchor segments:', JSON.stringify(anchor?.segments))
const runRef = anchor.segments.at(-1)?.runRef ?? anchor.segments[0]?.runRef
const inserted = await agent.edit({ revision: agent.revision, operations: [{ kind: 'insertImage', runRef, assetRef, widthPx: 460, heightPx: 460 }] })
console.log('insertImage:', inserted.operations)

// ---------- verify and save ----------
const finalProjection = agent.project({ mode: 'text' })
const residue = finalProjection.text.split('\n').filter(line => line.includes('{{'))
console.log('residue lines:', JSON.stringify(residue))
if (residue.length > 0) throw new Error(`residue: ${residue.join(' | ')}`)

const outBytes = agent.save()
const outPath = 'D:/agworkspace/docgc/deepseek-harness/.tmp/spike-report.docx'
writeFileSync(outPath, outBytes)
console.log('saved:', outPath, outBytes.length, 'bytes')

// reload check: Word-visibility proxy — re-open with AgentDocument and dump
const reloaded = AgentDocument.load(outBytes)
console.log('--- final text ---')
console.log(reloaded.project({ mode: 'text' }).text)

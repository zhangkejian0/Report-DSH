/** The deterministic engine over composed templates: expansion, substitution, images, and refusals. */
import { inflateRawSync } from 'node:zlib'
import { describe, expect, it } from 'vitest'
import { AgentDocument, DOCMD_ATOMS } from '@wordinweb/agent'
import type { ThermalAnalysis } from '@deepseek-ai/dsh-thermal-analyze'
import { generateReportBytes, ReportEngineError } from '../src/engine.ts'
import { IMAGE_LOOP_CLOSE, IMAGE_LOOP_OPEN } from '../src/tokens.ts'
import type { ReportTokenImage } from '../src/tokens.ts'

/** Minimal structural analysis fixture; only the fields the tokens read. */
function analysisOf(max: number, thermal = RED_PNG): ThermalAnalysis {
  return {
    file: { path: 'ir.jpg', type: 'FJPG', version: 1, modifiedTime: '', width: 640, height: 480 },
    camera: { model: '336X', serialNo: 'A1', brand: 'Fotric', company: 'Fotric' },
    lens: { focalLength: 0, hFov: 0 },
    params: { emissivity: 0.95, distance: 3, reflTemp: 20, atmTemp: 20, relHumidity: 0.45 },
    gain: { mode: 'auto', min: 10, max: 40 },
    stats: { min: max - 10, max, avg: max - 5, minAt: [1, 1], maxAt: [2, 2], count: 100 },
    markers: [{ name: 'Ar1', type: 'rect', rect: [1, 1, 2, 2], max, min: max - 4, avg: max - 2, hotSpot: [2, 2], coldSpot: [1, 1] }],
    grid: { width: 2, height: 2, block: 320, data: [1, 2, 3, 4] },
    frame: { timestamp: '2026-10-14 10:00:00', adMin: 0, adMax: 100, adAvg: 50 },
    images: { thermal, palette: '' },
  }
}

/** A 1×1 red PNG, its header proving decodability and aspect scaling. */
const RED_PNG = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=='

const IMAGES: readonly ReportTokenImage[] = [
  { path: 'IR/ir0007.jpg', analysis: analysisOf(42.31) },
  { path: 'IR/ir0008.jpg', analysis: analysisOf(38.11) },
]

/** Compose one template docx with header/footer stories and both loop shapes. */
async function templateBytes(body: readonly unknown[], footer: readonly unknown[] = []): Promise<Uint8Array> {
  const doc = AgentDocument.create()
  // The shapes are the compose vocabulary; the cast keeps the fixture terse.
  const request = { revision: doc.revision, body: body as Parameters<AgentDocument['compose']>[0]['body'] } as Parameters<AgentDocument['compose']>[0]
  if (footer.length > 0) request.footer = footer as NonNullable<Parameters<AgentDocument['compose']>[0]['footer']>
  await doc.compose(request)
  return doc.save()
}

/** The full-coverage template: globals, summary, a row loop, a range loop with a nested table. */
async function fullTemplate(): Promise<Uint8Array> {
  return templateBytes([
    { type: 'paragraph', text: '{{report_title}}' },
    { type: 'paragraph', text: '检测单位 {{company}} 检测日期 {{date}} 相机 {{camera_model}}/{{camera_serial}}' },
    { type: 'paragraph', text: '汇总：共 {{image_count}} 张，最高 {{overall_max_temp}} @ {{overall_max_at}}（{{overall_max_image}}）' },
    { type: 'table', rows: [
      ['文件', '最高'],
      [`${IMAGE_LOOP_OPEN} {{image_file}}`, `{{t_max}} ${IMAGE_LOOP_CLOSE}`],
    ] },
    { type: 'paragraph', text: IMAGE_LOOP_OPEN },
    { type: 'paragraph', text: '《{{image_file}}》最高 {{t_max}} @ {{t_max_at}}，最低 {{t_min}}，平均 {{t_avg}}' },
    { type: 'paragraph', text: '热图：{{image_thermal}}' },
    { type: 'paragraph', text: '标记：{{marker_table}}；意见：{{image_comment}}' },
    { type: 'paragraph', text: IMAGE_LOOP_CLOSE },
    { type: 'paragraph', text: '{{overall_conclusion}}' },
  ], [
    { type: 'paragraph', text: '{{company}} 第 N 页' },
  ])
}

/** Text of one story after reopening the finished bytes. */
function storyText(bytes: Uint8Array, story: string): string {
  const agent = AgentDocument.load(bytes)
  return agent.project({ story, mode: 'text', maxBlocks: 2000, maxCharacters: 200_000 }).text
}

/** Story ids of one document by kind. */
function storyIds(bytes: Uint8Array, kind: string): string[] {
  const inspected = AgentDocument.load(bytes).inspect({ kind: 'overview' })
  if (!('stories' in inspected)) throw new Error('no stories')
  return inspected.stories.filter(item => item.kind === kind).map(item => item.id)
}

/** The settings.xml text of one docx zip, by local-header walk. */
function settingsXmlOf(bytes: Uint8Array): string {
  const buffer = Buffer.from(bytes)
  let position = 0
  for (;;) {
    const at = buffer.indexOf(Buffer.from([0x50, 0x4b, 0x03, 0x04]), position)
    if (at === -1) throw new Error('settings.xml not found')
    const nameLength = buffer.readUInt16LE(at + 26)
    const extraLength = buffer.readUInt16LE(at + 28)
    const name = buffer.subarray(at + 30, at + 30 + nameLength).toString('latin1')
    const method = buffer.readUInt16LE(at + 8)
    const size = buffer.readUInt32LE(at + 18)
    if (name === 'word/settings.xml') {
      const data = buffer.subarray(at + 30 + nameLength + extraLength, at + 30 + nameLength + extraLength + size)
      return (method === 8 ? inflateRawSync(data) : data).toString('utf8')
    }
    position = at + 4
  }
}

describe('generateReportBytes', () => {
  it('expands both loop shapes and substitutes every token', async () => {
    const bytes = await generateReportBytes({
      templateBytes: await fullTemplate(),
      images: IMAGES,
      fields: { title: '变电站巡检报告', company: '示例检测', inspector: '张三', device: '1号主变' },
      date: new Date(2026, 9, 15),
      imageWidthPx: 460,
    })
    const body = storyText(bytes, 'body')
    expect(body).toContain('变电站巡检报告')
    expect(body).toContain('检测单位 示例检测 检测日期 2026年10月15日 相机 336X/A1')
    expect(body).toContain('汇总：共 2 张，最高 42.31℃ @ (2,2)（ir0007.jpg）')
    // Row loop: one table row per image, markers cleared around the values.
    expect(body).toContain('ir0007.jpg')
    expect(body).toContain('ir0008.jpg')
    // Range loop: both instances, nested values, marker-free.
    expect(body).toContain('《ir0007.jpg》最高 42.31℃ @ (2,2)，最低 32.31℃，平均 37.31℃')
    expect(body).toContain('《ir0008.jpg》最高 38.11℃ @ (2,2)，最低 28.11℃，平均 33.11℃')
    expect(body).toContain('标记：Ar1（rect）最高 42.31℃、最低 38.31℃、平均 40.31℃')
    expect(body).toContain('共 2 张热像图，最高 42.31℃（出现于《ir0007.jpg》）')
    expect(body).not.toContain('{{')
    // Footer story carries the globals too.
    const footer = storyText(bytes, storyIds(bytes, 'footer')[0] ?? '')
    expect(footer).toContain('示例检测 第 N 页')
    expect(footer).not.toContain('{{')
    // One drawing per image site.
    expect(body.split(DOCMD_ATOMS.object).length - 1).toBe(2)
    // Word is asked to refresh fields (TOC page numbers) on open.
    expect(settingsXmlOf(bytes)).toContain('<w:updateFields/>')
  })

  it('refuses a template without a loop region', async () => {
    const bytes = await templateBytes([{ type: 'paragraph', text: '{{report_title}}' }])
    await expect(generateReportBytes({
      templateBytes: bytes, images: IMAGES, fields: {}, date: new Date(), imageWidthPx: 460,
    })).rejects.toMatchObject({ reason: 'no-image-loop' })
  })

  it('refuses an unknown token before any work', async () => {
    const bytes = await templateBytes([
      { type: 'paragraph', text: '{{report_tile}}' },
      { type: 'paragraph', text: IMAGE_LOOP_OPEN },
      { type: 'paragraph', text: '{{image_file}}' },
      { type: 'paragraph', text: IMAGE_LOOP_CLOSE },
    ])
    await expect(generateReportBytes({
      templateBytes: bytes, images: IMAGES, fields: {}, date: new Date(), imageWidthPx: 460,
    })).rejects.toMatchObject({ reason: 'unknown-token' })
  })

  it('refuses unpaired markers', async () => {
    const bytes = await templateBytes([
      { type: 'paragraph', text: IMAGE_LOOP_OPEN },
      { type: 'paragraph', text: '{{image_file}}' },
    ])
    await expect(generateReportBytes({
      templateBytes: bytes, images: IMAGES, fields: {}, date: new Date(), imageWidthPx: 460,
    })).rejects.toMatchObject({ reason: 'template-markers' })
  })

  it('refuses markers split across table rows', async () => {
    const bytes = await templateBytes([
      { type: 'table', rows: [
        [`${IMAGE_LOOP_OPEN} {{image_file}}`, '{{t_max}}'],
        [IMAGE_LOOP_CLOSE, ''],
      ] },
    ])
    await expect(generateReportBytes({
      templateBytes: bytes, images: IMAGES, fields: {}, date: new Date(), imageWidthPx: 460,
    })).rejects.toMatchObject({ reason: 'template-markers' })
  })

  it('refuses a broken template', async () => {
    await expect(generateReportBytes({
      templateBytes: new Uint8Array([1, 2, 3]), images: IMAGES, fields: {}, date: new Date(), imageWidthPx: 460,
    })).rejects.toBeInstanceOf(ReportEngineError)
  })

  it('skips image insertion when an analysis carries no thermal render', async () => {
    const bare = [{ path: 'IR/ir.jpg', analysis: analysisOf(40, '') }]
    const bytes = await generateReportBytes({
      templateBytes: await templateBytes([
        { type: 'paragraph', text: IMAGE_LOOP_OPEN },
        { type: 'paragraph', text: '热图：{{image_thermal}} {{image_file}}' },
        { type: 'paragraph', text: IMAGE_LOOP_CLOSE },
      ]),
      images: bare, fields: {}, date: new Date(), imageWidthPx: 460,
    })
    const body = storyText(bytes, 'body')
    expect(body).toContain('热图： ir.jpg')
    expect(body.split(DOCMD_ATOMS.object).length - 1).toBe(0)
    expect(body).not.toContain('{{')
  })
})

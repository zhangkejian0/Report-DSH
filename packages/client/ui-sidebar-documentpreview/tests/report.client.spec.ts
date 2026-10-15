/**
 * The report wizard's pure halves: name validation and defaults, the task
 * book's data package and working discipline, the generation orchestration's
 * adopt/create/reuse decision and asset writes, and the embedded sample and
 * blank templates' integrity.
 */
// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest'
import type { SessionId } from '@deepseek-ai/dsh-session/types'
import type { ThermalAnalysis } from '@deepseek-ai/dsh-thermal-analyze/types'
import { defaultReportName, isValidReportName, uniqueTemplateName } from '../src/client/report/definition.ts'
import { buildTaskBook, type ReportImageDatum } from '../src/client/report/task-book.ts'
import { startGeneration, type GenerationDeps } from '../src/client/report/generate.ts'
import { BLANK_TEMPLATE_BASE64, SAMPLE_TEMPLATE_BASE64 } from '../src/client/report/sample-template.ts'

/** Minimal structural analysis fixture; only the fields the task book reads. */
function analysisOf(max: number, thermal = 'aG90'): ThermalAnalysis {
  return {
    file: { path: 'ir.jpg', type: 'FJPG', version: 1, modifiedTime: '', width: 640, height: 480 },
    camera: { model: '336X', serialNo: 'A1', brand: 'Fotric', company: 'Fotric' },
    lens: { focalLength: 0, hFov: 0 },
    params: { emissivity: 0.95, distance: 3, reflTemp: 20, atmTemp: 20, relHumidity: 0.45 },
    gain: { mode: 'auto', min: 10, max: 40 },
    stats: { min: max - 10, max, avg: max - 5, minAt: [1, 1], maxAt: [2, 2], count: 100 },
    markers: [{ name: 'Ar3', type: 'rect', rect: [1, 1, 2, 2], max, min: max - 4, avg: max - 2, hotSpot: [2, 2], coldSpot: [1, 1] }],
    grid: { width: 2, height: 2, block: 320, data: [1, 2, 3, 4] },
    frame: { timestamp: '2026-10-14 10:00:00', adMin: 0, adMax: 100, adAvg: 50 },
    images: { thermal, palette: '' },
  }
}

describe('report names', () => {
  it('accepts plain names and refuses path and reserved characters', () => {
    expect(isValidReportName('变电站巡检报告-20261014')).toBe(true)
    expect(isValidReportName('  ')).toBe(false)
    expect(isValidReportName('a/b')).toBe(false)
    expect(isValidReportName('a\\b')).toBe(false)
    expect(isValidReportName('a:b')).toBe(false)
  })

  it('defaults to the template basename stamped with the date', () => {
    expect(defaultReportName('报告模版/变电站巡检模版.docx', new Date(2026, 9, 14)))
      .toBe('变电站巡检模版-20261014')
  })
})

describe('buildTaskBook', () => {
  const images: ReportImageDatum[] = [
    { path: 'IR/ir0007.jpg', assetPath: '报告/demo/assets/ir0007.thermal.png', analysis: analysisOf(42.31) },
    { path: 'IR/ir0008.jpg', assetPath: undefined, analysis: analysisOf(38.11, '') },
  ]

  it('names the template, output, every measurement, and the discipline', () => {
    const book = buildTaskBook({
      templatePath: '报告模版/示范模版.docx',
      outputPath: '报告/demo/demo.docx',
      assetsDir: '报告/demo/assets',
      images,
    })
    expect(book).toContain('模版：报告模版/示范模版.docx')
    expect(book).toContain('输出：报告/demo/demo.docx')
    expect(book).toContain('共 2 张')
    expect(book).toContain('IR/ir0007.jpg')
    expect(book).toContain('最高 42.31℃ @')
    expect(book).toContain('Ar3')
    expect(book).toContain('报告/demo/assets/ir0007.thermal.png')
    expect(book).not.toContain('ir0008.thermal.png')
    expect(book).toContain('{{#each_image}}')
    expect(book).toContain('机械步骤')
    expect(book).toContain('query 为 "{{"')
    expect(book).toContain('不得把正文包进')
    expect(book).toContain('word_document_add_image')
    expect(book).toContain('另存到输出路径')
  })
})

describe('startGeneration', () => {
  const host: SessionId = 's-host' as SessionId
  const signal = new AbortController().signal

  /** Recording deps with controllable stat and no real services. */
  function deps(over?: Partial<GenerationDeps>): GenerationDeps {
    return {
      write: vi.fn(async (_sessionId: SessionId, path: string) => `C:/ws/${path}`),
      stat: vi.fn(async (_sessionId: SessionId, path: string) => {
        if (path.endsWith('.docx')) return `C:/ws/${path}`
        throw new Error('absent')
      }),
      prompt: vi.fn(async () => undefined),
      createSession: vi.fn(async () => 's-new' as SessionId),
      navigate: vi.fn(),
      openEditTab: vi.fn(),
      recordEditSession: vi.fn(),
      markSeeded: vi.fn(),
      editSessionForDocument: vi.fn(() => undefined),
      ...over,
    }
  }

  const request = (over?: { adoptHost?: boolean; blank?: boolean }) => ({
    hostSessionId: host,
    adoptHost: over?.blank === true,
    workspaceRoot: 'C:\\ws',
    templatePath: '报告模版/示范模版.docx',
    reportName: 'demo',
    images: [
      { path: 'IR/ir0007.jpg', analysis: analysisOf(42.31) },
      { path: 'IR/ir0008.jpg', analysis: analysisOf(38.11, '') },
    ],
  })

  it('writes each image asset once and dispatches the task book verbatim', async () => {
    const dep = deps()
    const outcome = await startGeneration(request(), dep, signal)
    expect(outcome).toEqual({ sessionId: 's-new', outputPath: '报告/demo/demo.docx' })
    expect(dep.write).toHaveBeenCalledTimes(1)
    expect(dep.write).toHaveBeenCalledWith(host, '报告/demo/assets/ir0007.thermal.png', 'aG90', signal)
    expect(vi.mocked(dep.prompt)).toHaveBeenCalledTimes(1)
    expect(String(vi.mocked(dep.prompt).mock.calls[0]?.[1])).toContain('IR/ir0007.jpg')
    expect(dep.navigate).toHaveBeenCalledWith('s-new')
  })

  it('adopts the blank host Session instead of creating one', async () => {
    const dep = deps()
    await startGeneration(request({ blank: true, adoptHost: true }), dep, signal)
    expect(dep.createSession).not.toHaveBeenCalled()
    expect(vi.mocked(dep.prompt).mock.calls[0]?.[0]).toBe(host)
  })

  it('reuses the recorded Session for the same output document', async () => {
    const dep = deps({ editSessionForDocument: vi.fn(() => 's-existing' as SessionId) })
    await startGeneration(request(), dep, signal)
    expect(dep.createSession).not.toHaveBeenCalled()
    expect(vi.mocked(dep.prompt).mock.calls[0]?.[0]).toBe('s-existing')
  })

  it('seeds the edit tab once the agent saves the output document', async () => {
    const dep = deps()
    await startGeneration(request(), dep, signal)
    await vi.waitFor(() => { expect(dep.recordEditSession).toHaveBeenCalledOnce() })
    expect(dep.recordEditSession).toHaveBeenCalledWith('s-new', 'C:/ws/报告/demo/demo.docx')
    expect(dep.openEditTab).toHaveBeenCalledWith('s-new', '报告/demo/demo.docx')
    expect(dep.markSeeded).toHaveBeenCalledWith('s-new')
  })

  it('reports an asset-write failure without dispatching anything', async () => {
    const dep = deps({ write: vi.fn(async () => { throw new Error('disk full') }) })
    await expect(startGeneration(request(), dep, signal)).rejects.toThrow('disk full')
    expect(vi.mocked(dep.prompt)).not.toHaveBeenCalled()
  })
})

describe('sample template', () => {
  it('is a complete DOCX zip', () => {
    const bytes = Uint8Array.from(atob(SAMPLE_TEMPLATE_BASE64), char => char.charCodeAt(0))
    expect(bytes[0]).toBe(0x50)
    expect(bytes[1]).toBe(0x4b)
    expect(bytes.length).toBeGreaterThan(1000)
  })
})

describe('blank template', () => {
  it('is a DOCX whose stored document body holds no text', () => {
    const bytes = Uint8Array.from(atob(BLANK_TEMPLATE_BASE64), char => char.charCodeAt(0))
    expect(bytes[0]).toBe(0x50)
    expect(bytes[1]).toBe(0x4b)
    // word/document.xml is stored uncompressed, so the XML is readable in place.
    const text = new TextDecoder().decode(bytes)
    const start = text.indexOf('<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:document')
    const end = text.indexOf('</w:document>', start)
    const xml = text.slice(start, end)
    expect(xml).toContain('<w:body><w:p/>')
    expect(xml).not.toContain('<w:t')
  })

  it('names blank templates without colliding with existing ones', () => {
    expect(uniqueTemplateName(new Set())).toBe('空白模版.docx')
    expect(uniqueTemplateName(new Set(['空白模版.docx']))).toBe('空白模版-2.docx')
    expect(uniqueTemplateName(new Set(['空白模版.docx', '空白模版-2.docx']))).toBe('空白模版-3.docx')
  })
})

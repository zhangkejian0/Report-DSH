/** One authorization, one analyzer pass per image, one engine pass, and one verified write per call. */
import { mkdtemp, readFile, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { Context } from '@deepseek-ai/cordis'
import { AgentDocument } from '@wordinweb/agent'
import { afterEach, expect, it } from 'vitest'
import type { SessionId } from '@deepseek-ai/dsh-session/types'
import { RemoteError } from '@deepseek-ai/dsh-typert-protocol'
import ReportGenerate from '../src/index.ts'
import { IMAGE_LOOP_CLOSE, IMAGE_LOOP_OPEN } from '../src/tokens.ts'
import type { ReportGenerateRequest } from '../src/index.ts'
import type { ThermalAnalysis } from '@deepseek-ai/dsh-thermal-analyze/types'

const scope = { sessionId: 's1' as SessionId, workspaceRoot: '/w' }
const signal = new AbortController().signal

/** Minimal structural analysis fixture; only the fields the tokens read. */
function analysisOf(max: number): ThermalAnalysis {
  return {
    file: { path: 'ir.jpg', type: 'FJPG', version: 1, modifiedTime: '', width: 640, height: 480 },
    camera: { model: '336X', serialNo: 'A1', brand: 'Fotric', company: 'Fotric' },
    lens: { focalLength: 0, hFov: 0 },
    params: { emissivity: 0.95, distance: 3, reflTemp: 20, atmTemp: 20, relHumidity: 0.45 },
    gain: { mode: 'auto', min: 10, max: 40 },
    stats: { min: max - 10, max, avg: max - 5, minAt: [1, 1], maxAt: [2, 2], count: 100 },
    markers: [],
    grid: { width: 2, height: 2, block: 320, data: [1, 2, 3, 4] },
    frame: { timestamp: '2026-10-14 10:00:00', adMin: 0, adMax: 100, adAvg: 50 },
    images: { thermal: '', palette: '' },
  }
}

/** Analyses the stub analyzer serves, by image path. */
const ANALYSES = new Map<string, ThermalAnalysis>([
  ['IR/ir0007.jpg', analysisOf(42.31)],
  ['IR/ir0008.jpg', analysisOf(38.11)],
])

/** A one-instance loop template as a workspace file. */
async function templateFile(): Promise<string> {
  const doc = AgentDocument.create()
  await doc.compose({
    revision: doc.revision,
    body: [
      { type: 'paragraph', text: '{{report_title}}' },
      { type: 'paragraph', text: IMAGE_LOOP_OPEN },
      { type: 'paragraph', text: '{{image_file}} {{t_max}}' },
      { type: 'paragraph', text: IMAGE_LOOP_CLOSE },
    ],
  })
  const directory = await mkdtemp(join(tmpdir(), 'dsh-report-spec-'))
  const file = join(directory, 'template.docx')
  await writeFile(file, doc.save())
  return file
}

afterEach(() => { for (const dispose of disposers.splice(0)) dispose() })
const disposers: Array<() => void> = []

/**
 * Boot the service over stub workspaceFiles and a stub thermal analyzer that
 * read the real template file and capture the written report.
 * @param templatePath - absolute path of the template the stub serves.
 * @returns the service, the write log, and the owning Context.
 */
function service(templatePath: string, sidecarPath?: string) {
  const ctx = new Context()
  disposers.push(() => { void ctx.fiber.dispose() })
  const writes: Array<{ path: string; base64: string }> = []
  ctx.reflect.provide('workspaceFiles', {
    read: () => { throw new Error('unexpected workspaceFiles.read') },
    readBytes: async (_scope: unknown, path: string) => {
      if (path === '报告模版/t.tokens.json' && sidecarPath !== undefined) {
        return { absolutePath: sidecarPath, version: 'v1', offset: 0, eof: true, data: new Uint8Array(await readFile(sidecarPath)) }
      }
      if (path !== '报告模版/t.docx') {
        throw new RemoteError('workspace-file/not-found', 'not found', { path })
      }
      return { absolutePath: templatePath, version: 'v1', offset: 0, eof: true, data: new Uint8Array(await readFile(templatePath)) }
    },
    stat: () => { throw new Error('unexpected workspaceFiles.stat') },
    writeBytes: async (_scope: unknown, path: string, base64: string) => {
      writes.push({ path, base64 })
      return { absolutePath: `/w/${path}`, version: 'v2' }
    },
    list: () => { throw new Error('unexpected workspaceFiles.list') },
    changes: () => { throw new Error('unexpected workspaceFiles.changes') },
  })
  ctx.reflect.provide('thermalAnalyze', {
    analyze: async (_scope: unknown, path: string) => {
      const analysis = ANALYSES.get(path)
      if (analysis === undefined) {
        throw new RemoteError('thermal-analyze/failed', 'not thermal', { reason: 'not-thermal' })
      }
      return analysis
    },
  })
  const instance = new ReportGenerate(ctx, { imageWidthPx: 460, maxImages: 500 })
  return { instance, writes, ctx }
}

/** The standard request against the stubbed workspace paths. */
function request(over?: Partial<ReportGenerateRequest>): ReportGenerateRequest {
  return {
    templatePath: '报告模版/t.docx',
    outputPath: '报告/demo/demo.docx',
    imagePaths: ['IR/ir0007.jpg', 'IR/ir0008.jpg'],
    fields: { report_title: '变电站巡检报告' },
    ...over,
  }
}

it('re-measures every image and writes one residue-free report', async () => {
  const { instance, writes } = service(await templateFile())
  const result = await instance.generate(scope, request(), signal)
  expect(result.images).toBe(2)
  expect(result.absolutePath).toBe('/w/报告/demo/demo.docx')
  expect(writes).toHaveLength(1)
  expect(writes[0]?.path).toBe('报告/demo/demo.docx')
  const reopened = AgentDocument.load(new Uint8Array(Buffer.from(writes[0]!.base64, 'base64')))
  const text = reopened.project({ story: 'body', mode: 'text', maxBlocks: 2000, maxCharacters: 200_000 }).text
  expect(text).toContain('变电站巡检报告')
  expect(text).toContain('ir0007.jpg 42.31℃')
  expect(text).toContain('ir0008.jpg 38.11℃')
  expect(text).not.toContain('{{')
})

it('refuses a template the workspace cannot read', async () => {
  const { instance } = service(await templateFile())
  await expect(instance.generate(scope, request({ templatePath: '报告模版/absent.docx' }), signal))
    .rejects.toMatchObject({ code: 'workspace-file/not-found' })
})

it('passes analyzer refusals through unchanged', async () => {
  const { instance, writes } = service(await templateFile())
  await expect(instance.generate(scope, request({ imagePaths: ['IR/unknown.jpg'] }), signal))
    .rejects.toMatchObject({ code: 'thermal-analyze/failed', details: { reason: 'not-thermal' } })
  expect(writes).toHaveLength(0)
})

it('maps engine refusals onto the report-generate failure reason', async () => {
  const doc = AgentDocument.create()
  await doc.compose({ revision: doc.revision, body: [{ type: 'paragraph', text: '{{report_title}}' }] })
  const directory = await mkdtemp(join(tmpdir(), 'dsh-report-spec-'))
  const file = join(directory, 'loopless-template.docx')
  await writeFile(file, doc.save())
  const { instance, writes } = service(file)
  await expect(instance.generate(scope, request(), signal))
    .rejects.toMatchObject({ code: 'report-generate/failed', details: { reason: 'no-image-loop' } })
  expect(writes).toHaveLength(0)
})

it('refuses an invalid sidecar field configuration', async () => {
  const template = await templateFile()
  const directory = await mkdtemp(join(tmpdir(), 'dsh-report-spec-'))
  const sidecar = join(directory, 't.tokens.json')
  await writeFile(sidecar, '{ "version": 1, "fields": { "x": { "what": 1 } } }', 'utf8')
  const { instance, writes } = service(template, sidecar)
  await expect(instance.generate(scope, request(), signal))
    .rejects.toMatchObject({ code: 'report-generate/failed', details: { reason: 'fields-invalid' } })
  expect(writes).toHaveLength(0)
})

it('refuses an empty image list before touching the workspace', async () => {
  const { instance } = service(await templateFile())
  await expect(instance.generate(scope, request({ imagePaths: [] }), signal))
    .rejects.toMatchObject({ code: 'report-generate/failed', details: { reason: 'failed' } })
})

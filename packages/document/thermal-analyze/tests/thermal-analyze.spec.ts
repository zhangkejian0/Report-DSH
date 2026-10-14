/** One authorization, one analyzer process, and one verified report per call. */
import { mkdtemp, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { Context } from '@deepseek-ai/cordis'
import { afterEach, expect, it } from 'vitest'
import type { SessionId } from '@deepseek-ai/dsh-session/types'
import ThermalAnalyze from '../src/index.ts'
import type { ThermalAnalysis } from '../src/index.ts'

const scope = { sessionId: 's1' as SessionId, workspaceRoot: '/w' }
const signal = new AbortController().signal

/** The analyzer is `node` and the "image" is the stub script, so one spawn covers every platform. */
const report: ThermalAnalysis = {
  file: { path: 'stub.jpg', type: 'ThermalPicture', version: 302, modifiedTime: '', width: 4, height: 4 },
  camera: { model: 'Fotric 358XS#L25', serialNo: 'SN1', brand: '', company: '' },
  lens: { focalLength: 0, hFov: 0 },
  params: { emissivity: 0.95, distance: 2, reflTemp: 20, atmTemp: 20, relHumidity: 0.3 },
  gain: { mode: 'Fixed', min: 18.3, max: 22.8 },
  stats: { min: 17.1, max: 22.6, avg: 18.9, minAt: [3, 1], maxAt: [2, 2], count: 16 },
  markers: [{ name: 'Ar1', type: 'Rectangle', rect: [0, 0, 2, 2], max: 22.6, min: 17.1, avg: 18.9, hotSpot: [2, 2], coldSpot: [3, 1] }],
  grid: { width: 2, height: 2, block: 2, data: [17.5, 18.9, 19.1, 22.4] },
  frame: { timestamp: '2021-03-06 17:42:11', adMin: 32567, adMax: 32649, adAvg: 32594.8 },
  images: { thermal: '', palette: '' },
}

afterEach(() => { for (const dispose of disposers.splice(0)) dispose() })
const disposers: Array<() => void> = []

/**
 * Boot the service over one stub "image" whose bytes are a CommonJS analyzer.
 * @param body - stub script; `outDir` arrives as `process.argv[2]`.
 * @param analyzerPath - executable to spawn; defaults to this Node.
 * @returns the service, the stub's absolute path, and the owning Context.
 */
async function service(body: string, analyzerPath = process.execPath) {
  const ctx = new Context()
  disposers.push(() => { void ctx.fiber.dispose() })
  const directory = await mkdtemp(join(tmpdir(), 'dsh-thermal-spec-'))
  const image = join(directory, 'stub.jpg')
  await writeFile(image, body, 'utf8')
  const unexpected = (name: string): never => { throw new Error(`unexpected workspaceFiles.${name}`) }
  ctx.reflect.provide('workspaceFiles', {
    read: () => unexpected('read'),
    readBytes: async () => ({ absolutePath: image, version: 'v1', offset: 0, eof: true, bytes: 1, data: new Uint8Array(1) }),
    stat: async () => ({ absolutePath: image, version: 'v1' }),
    writeBytes: () => unexpected('writeBytes'),
    list: () => unexpected('list'),
    changes: () => unexpected('changes'),
  })
  const instance = new ThermalAnalyze(ctx, { analyzerPath, timeoutMs: 10_000, maxOutputBytes: 1024 * 1024 })
  return { instance, image, ctx }
}

/** Reject with the RemoteError's code and reason detail. */
async function failureOf(call: Promise<unknown>): Promise<{ code: string; reason: string }> {
  try {
    await call
  } catch (error) {
    const remote = error as { code: string; details: { reason: string } }
    return { code: remote.code, reason: remote.details.reason }
  }
  throw new Error('expected the call to reject')
}

it('rejects a relative analyzer path at construction', () => {
  const ctx = new Context()
  expect(() => new ThermalAnalyze(ctx, { analyzerPath: 'analyzer.exe', timeoutMs: 1000, maxOutputBytes: 1024 }))
    .toThrow('analyzerPath must be absolute.')
  void ctx.fiber.dispose()
})

it('returns the analyzer report for one authorized jpg', async () => {
  const { instance, image } = await service(
    `require('node:fs').writeFileSync(require('node:path').join(process.argv[2], 'result.json'), JSON.stringify(${JSON.stringify(report)}))`,
  )
  await expect(instance.analyze(scope, image, signal)).resolves.toEqual(report)
})

it('maps analyzer refusals and damage onto wire reasons', async () => {
  const refused = await service('process.exit(2)')
  await expect(failureOf(refused.instance.analyze(scope, refused.image, signal))).resolves.toEqual({ code: 'thermal-analyze/failed', reason: 'not-thermal' })
  const broken = await service('require(\'node:fs\').writeFileSync(require(\'node:path\').join(process.argv[2], \'result.json\'), \'{"stats":1}\')')
  await expect(failureOf(broken.instance.analyze(scope, broken.image, signal))).resolves.toEqual({ code: 'thermal-analyze/failed', reason: 'failed' })
})

it('refuses non-JPEG paths and a missing analyzer without spawning', async () => {
  const { instance, image } = await service('process.exit(0)', join(tmpdir(), 'definitely-missing-analyzer'))
  await expect(failureOf(instance.analyze(scope, 'photo.png', signal))).resolves.toEqual({ code: 'thermal-analyze/failed', reason: 'not-thermal' })
  await expect(failureOf(instance.analyze(scope, image, signal))).resolves.toEqual({ code: 'thermal-analyze/failed', reason: 'unavailable' })
})

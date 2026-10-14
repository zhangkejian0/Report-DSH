/**
 * Thermal-image analysis service: runs the Fotric IRtek SDK analyzer over one
 * authorized workspace file and returns its JSON report, exposed as
 * `thermalAnalyze`.
 *
 * The analyzer is an external executable configured per deployment; this
 * service never parses radiometric payloads itself. One call writes the
 * image's report into a private temporary directory, streams it back, and
 * removes the directory. Authorization follows `workspaceFiles`: a one-byte
 * read plus a stat establish the same Session-relative path the document
 * preview shows, and the analyzer receives only the resolved absolute path.
 */

import { execFile, type ExecFileOptionsWithStringEncoding } from 'node:child_process'
import { mkdtemp, readFile, rm, stat } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { isAbsolute, join } from 'node:path'
import { promisify } from 'node:util'
import type { Context } from '@deepseek-ai/cordis'
import z from '@deepseek-ai/schemastery'
import type { WorkspaceFileScope } from '@deepseek-ai/dsh-api-workspace-files'
import { Remote, RemoteError, TypertRemoteService } from '@deepseek-ai/dsh-typert-protocol'
import type { ThermalAnalysis, ThermalAnalyzeReason } from './types.ts'

export type * from './types.ts'

declare module '@deepseek-ai/cordis' {
  interface Context {
    /** Host owner of the `thermalAnalyze` Remote namespace. */
    thermalAnalyze: ThermalAnalyze
  }
}

/** Analyzer process and report limits. */
export interface Config {
  /** Absolute path of the analyzer executable. */
  analyzerPath: string
  /** Analyzer deadline in milliseconds, including process startup. */
  timeoutMs: number
  /** Maximum accepted report size in bytes. */
  maxOutputBytes: number
}

/** Deployment defaults resolved before service construction. */
export const Config: z<Partial<Config>, Config> = z.object({
  analyzerPath: z.string().min(1),
  timeoutMs: z.natural().min(1).max(2_147_483_647).default(60_000),
  maxOutputBytes: z.natural().min(1).max(Number.MAX_SAFE_INTEGER).default(16 * 1024 * 1024),
})

const execFileString = promisify(execFile)

/**
 * A provider lifetime owning the configured analyzer and its in-flight calls.
 */
export class ThermalAnalyze extends TypertRemoteService {
  static Config = Config
  private readonly remoteLifetime = new AbortController()
  private readonly remoteRequests = new Set<Promise<ThermalAnalysis>>()

  /**
   * @param ctx - owning Host context.
   * @param config - resolved analyzer path and process limits.
   */
  constructor(ctx: Context, private readonly config: Config) {
    super(ctx, 'thermalAnalyze')
    if (!isAbsolute(config.analyzerPath)) throw new Error('analyzerPath must be absolute.')
    ctx.effect(() => () => {
      this.remoteLifetime.abort()
    })
  }

  /**
   * Analyze one radiometric JPEG using the Session's ordinary filesystem authorization.
   * @param workspaceFileScope - Session header lookup shared with workspaceFiles.
   * @param path - absolute or workspace-relative path to the image.
   * @param signal - Remote cancellation; provider disposal also stops active work.
   * @returns the analyzer's complete report, temperatures in Celsius.
   * @throws {RemoteError} `thermal-analyze/failed` with a `reason` detail on every failure.
   */
  @Remote
  async analyze(workspaceFileScope: WorkspaceFileScope, path: string, signal: AbortSignal): Promise<ThermalAnalysis> {
    const upstream = AbortSignal.any([signal, this.remoteLifetime.signal])
    const operation = this.analyzeFile(workspaceFileScope, path, upstream)
    this.remoteRequests.add(operation)
    try { return await operation } finally { this.remoteRequests.delete(operation) }
  }

  private async analyzeFile(scope: WorkspaceFileScope, path: string, signal: AbortSignal): Promise<ThermalAnalysis> {
    try {
      signal.throwIfAborted()
      const lowered = path.toLowerCase()
      if (!lowered.endsWith('.jpg') && !lowered.endsWith('.jpeg')) {
        throw new RemoteError('thermal-analyze/failed', 'The path must end in jpg or jpeg.', { reason: 'not-thermal' })
      }
      const files = this.ctx.get('workspaceFiles')
      if (files === undefined) throw new RemoteError('thermal-analyze/failed', 'Thermal analysis requires workspaceFiles.', { reason: 'unavailable' })
      await files.readBytes(scope, path, { range: { offset: 0, length: 1 } }, signal)
      const source = await files.stat(scope, path, signal)
      signal.throwIfAborted()
      return await this.runAnalyzer(source.absolutePath, signal)
    } catch (cause) {
      if (signal.aborted) throw new RemoteError('gateway/cancelled', 'Thermal analysis was cancelled.', {}, { cause })
      if (cause instanceof RemoteError) throw cause
      throw new RemoteError('thermal-analyze/failed', 'Thermal analysis failed.', { reason: 'failed' }, { cause })
    }
  }

  private async runAnalyzer(absolutePath: string, signal: AbortSignal): Promise<ThermalAnalysis> {
    let directory: string | undefined
    try {
      const info = await stat(this.config.analyzerPath).catch(() => undefined)
      if (info === undefined || !info.isFile()) {
        throw new RemoteError('thermal-analyze/failed', 'The configured analyzer executable is missing.', { reason: 'unavailable' })
      }
      signal.throwIfAborted()
      directory = await mkdtemp(join(tmpdir(), 'dsh-thermal-analyze-'))
      const options: ExecFileOptionsWithStringEncoding = {
        signal, timeout: this.config.timeoutMs, windowsHide: true, maxBuffer: this.config.maxOutputBytes,
      }
      await execFileString(this.config.analyzerPath, [absolutePath, directory], options)
      signal.throwIfAborted()
      const report = join(directory, 'result.json')
      const reported = await stat(report).catch(() => undefined)
      if (reported === undefined || !reported.isFile() || reported.size > this.config.maxOutputBytes) {
        throw new RemoteError('thermal-analyze/failed', 'The analyzer wrote no usable report.', { reason: 'too-large' })
      }
      const text = await readFile(report, 'utf8')
      signal.throwIfAborted()
      return verifyAnalysis(JSON.parse(text))
    } catch (cause) {
      signal.throwIfAborted()
      if (cause instanceof RemoteError) throw cause
      throw new RemoteError('thermal-analyze/failed', 'Thermal analysis failed.', { reason: analyzerReason(cause) }, { cause })
    } finally {
      if (directory !== undefined) await rm(directory, { recursive: true, force: true })
    }
  }
}

/** Map one analyzer-process failure onto its wire reason. */
function analyzerReason(cause: unknown): ThermalAnalyzeReason {
  if (typeof cause === 'object' && cause !== null) {
    const record = cause as { killed?: unknown; code?: unknown }
    if (record.killed === true) return 'timeout'
    if (record.code === 2) return 'not-thermal'
    if (record.code === 3) return 'open-failed'
  }
  return 'failed'
}

/** Whether one value is a non-null, non-array object. */
function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

/** Structural check of the analyzer's process-boundary JSON. */
function verifyAnalysis(value: unknown): ThermalAnalysis {
  const record = value
  if (!isRecord(record) || !isRecord(record.file) || !isRecord(record.stats) || !isRecord(record.grid)
    || !isRecord(record.camera) || !isRecord(record.images) || !Array.isArray(record.markers)) {
    throw new RemoteError('thermal-analyze/failed', 'The report is structurally invalid.', { reason: 'failed' })
  }
  const { file, stats, grid } = record
  if (typeof file.width !== 'number' || typeof file.height !== 'number'
    || typeof stats.min !== 'number' || typeof stats.max !== 'number' || typeof stats.avg !== 'number'
    || typeof grid.width !== 'number' || typeof grid.height !== 'number' || !Array.isArray(grid.data)
    || grid.data.length !== grid.width * grid.height) {
    throw new RemoteError('thermal-analyze/failed', 'The report is structurally invalid.', { reason: 'failed' })
  }
  return value as ThermalAnalysis
}

export default ThermalAnalyze

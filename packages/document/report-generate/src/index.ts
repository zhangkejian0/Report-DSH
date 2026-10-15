/**
 * Deterministic report generation service: expands one template .docx over
 * measured thermal images on the Host and writes the report into the Session's
 * workspace, exposed as `reportGenerate`.
 *
 * The request names only paths; this service re-measures every image through
 * the mounted `thermalAnalyze` service and reads the template and writes the
 * report through `workspaceFiles`, all under the Session's ordinary
 * authorization. The generation itself is the pure engine in `./engine.ts`
 * over the WordInWeb document model — no model, no Session, no agent.
 */
import type { Context } from '@deepseek-ai/cordis'
import z from '@deepseek-ai/schemastery'
import type {} from '@deepseek-ai/dsh-thermal-analyze'
import type { WorkspaceFileScope } from '@deepseek-ai/dsh-api-workspace-files'
import { Remote, RemoteError, TypertRemoteService, remoteErrorOf } from '@deepseek-ai/dsh-typert-protocol'
import { generateReportBytes, ReportEngineError } from './engine.ts'
import type { ReportTokenImage } from './tokens.ts'
import type { ReportGenerateRequest, ReportGenerateResult } from './types.ts'

export type * from './types.ts'

declare module '@deepseek-ai/cordis' {
  interface Context {
    /** Host owner of the `reportGenerate` Remote namespace. */
    reportGenerate: ReportGenerate
  }
}

/** Generation limits. */
export interface Config {
  /** Insertion width for thermal images in pixels. */
  imageWidthPx: number
  /** Maximum images one generation may expand. */
  maxImages: number
}

/** Deployment defaults resolved before service construction. */
export const Config: z<Partial<Config>, Config> = z.object({
  imageWidthPx: z.natural().min(80).max(2000).default(460),
  maxImages: z.natural().min(1).max(2000).default(500),
})

/**
 * A provider lifetime owning the configured image width and image cap.
 */
export class ReportGenerate extends TypertRemoteService {
  static Config = Config

  /**
   * @param ctx - owning Host context.
   * @param config - resolved generation limits.
   */
  constructor(ctx: Context, private readonly config: Config) {
    super(ctx, 'reportGenerate')
  }

  /**
   * Generate one report from the Session's template over its thermal images.
   * Every image is re-measured by the Host's own analyzer, so the report
   * carries fresh data whatever the client cached.
   * @param workspaceFileScope - Session header lookup shared with workspaceFiles.
   * @param request - template, output, image paths, and author fields.
   * @param signal - Remote cancellation.
   * @returns the written report's absolute path, size, and image count.
   * @throws {RemoteError} `report-generate/failed` with a `reason` detail on
   * every refusal; nothing is written when generation refuses.
   */
  @Remote
  async generate(
    workspaceFileScope: WorkspaceFileScope,
    request: ReportGenerateRequest,
    signal: AbortSignal,
  ): Promise<ReportGenerateResult> {
    try {
      signal.throwIfAborted()
      if (request.imagePaths.length === 0) {
        throw new ReportEngineError('failed', 'A report needs at least one image.')
      }
      if (request.imagePaths.length > this.config.maxImages) {
        throw new ReportEngineError('too-many-images',
          `One report may expand at most ${this.config.maxImages} images.`)
      }
      if (!request.outputPath.toLowerCase().endsWith('.docx')) {
        throw new ReportEngineError('failed', 'The output path must end in .docx.')
      }
      const files = this.ctx.get('workspaceFiles')
      if (files === undefined) {
        throw new ReportEngineError('failed', 'Report generation requires workspaceFiles.')
      }
      const analyzer = this.ctx.get('thermalAnalyze')
      if (analyzer === undefined) {
        throw new ReportEngineError('analyzer-unavailable', 'Report generation requires the thermal analyzer.')
      }

      const images: ReportTokenImage[] = []
      for (const path of request.imagePaths) {
        try {
          images.push({ path, analysis: await analyzer.analyze(workspaceFileScope, path, signal) })
        } catch (cause) {
          const failure = remoteErrorOf(cause)
          if (failure !== undefined) throw failure
          throw new RemoteError('report-generate/failed', `Analyzing ${path} failed.`, { reason: 'analysis-failed' }, { cause })
        }
      }
      signal.throwIfAborted()

      // The full read itself establishes the Session-relative authorization.
      const template = await files.readBytes(workspaceFileScope, request.templatePath, {}, signal)
      signal.throwIfAborted()

      const bytes = await generateReportBytes({
        templateBytes: new Uint8Array(template.data),
        images,
        fields: request.fields ?? {},
        date: new Date(),
        imageWidthPx: this.config.imageWidthPx,
      })
      const written = await files.writeBytes(
        workspaceFileScope, request.outputPath, Buffer.from(bytes).toString('base64'), signal)
      return { absolutePath: written.absolutePath, bytes: bytes.length, images: images.length }
    } catch (cause) {
      if (signal.aborted) {
        throw new RemoteError('gateway/cancelled', 'Report generation was cancelled.', {}, { cause })
      }
      if (cause instanceof ReportEngineError) {
        throw new RemoteError('report-generate/failed', cause.message, { reason: cause.reason }, { cause })
      }
      const failure = remoteErrorOf(cause)
      if (failure !== undefined) throw failure
      throw new RemoteError('report-generate/failed', 'Report generation failed.', { reason: 'failed' }, { cause })
    }
  }
}

export default ReportGenerate

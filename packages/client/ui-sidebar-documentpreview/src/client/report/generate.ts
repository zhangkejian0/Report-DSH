/** One generation run: the deterministic Host engine, or the agent task book in a report Session. */
import type { SessionId } from '@deepseek-ai/dsh-session/types'
import type { ThermalAnalysis } from '@deepseek-ai/dsh-thermal-analyze/types'
import type {
  ReportGenerateRequest, ReportGenerateResult,
} from '@deepseek-ai/dsh-report-generate/types'
import type { WriteFile } from './report-bridge.ts'
import { buildTaskBook, type ReportImageDatum } from './task-book.ts'
import { REPORTS_DIR } from './definition.ts'

/** One measured image entering a generation. */
export interface GenerationImage {
  /** Workspace-relative path of the radiometric JPEG. */
  readonly path: string
  /** The wizard's cached preflight analysis. */
  readonly analysis: ThermalAnalysis
}

/** Author-supplied report header fields; the fast engine substitutes them. */
export interface GenerationFields {
  readonly company?: string
  readonly inspector?: string
  readonly device?: string
}

/** How one generation runs. */
export type GenerationMode = 'fast' | 'ai'

/** What the wizard collected, plus where the run happens. */
export interface GenerationRequest {
  /** Session hosting the wizard tab; its workspace scope authorizes every write. */
  readonly hostSessionId: SessionId
  /** Which engine takes the run: the Host's deterministic engine or the agent. */
  readonly mode: GenerationMode
  /** Adopt the blank host Session instead of creating a dedicated one. */
  readonly adoptHost: boolean
  /** Absolute host path of the workspace root, either separator style. */
  readonly workspaceRoot: string
  /** Workspace-relative path of the template .docx. */
  readonly templatePath: string
  /** Output report name; the output directory and .docx name follow it. */
  readonly reportName: string
  /** The measured images, in report order. */
  readonly images: readonly GenerationImage[]
  /** Author-supplied header fields for the fast engine's tokens. */
  readonly fields?: GenerationFields
}

/** The services one generation drives; all are plugin-lifetime closures. */
export interface GenerationDeps {
  /** Write asset bytes into the host Session's workspace. */
  readonly write: WriteFile
  /** Resolve one workspace file's absolute host path, rejecting while absent. */
  readonly stat: (sessionId: SessionId, path: string, signal: AbortSignal) => Promise<string>
  /** Queue one user prompt into a Session's agent. */
  readonly prompt: (sessionId: SessionId, text: string, signal: AbortSignal) => Promise<unknown>
  /** Run one deterministic generation on the Host engine. */
  readonly generateFast: (sessionId: SessionId, request: ReportGenerateRequest, signal: AbortSignal) => Promise<ReportGenerateResult>
  /** Whether one Session's Sidebar already holds the document's edit tab. */
  readonly editTabIn: (sessionId: SessionId, docxPath: string) => boolean
  /** Create one Session bound to a workspace directory. */
  readonly createSession: (cwd: string, signal: AbortSignal) => Promise<SessionId>
  /** Move the main view onto one Session. */
  readonly navigate: (sessionId: SessionId) => void
  /** Open the WordInWeb edit tab on one workspace-relative .docx in its Session. */
  readonly openEditTab: (sessionId: SessionId, docxPath: string) => void
  /** Record which document one Session edits. */
  readonly recordEditSession: (sessionId: SessionId, absolutePath: string) => void
  /** Claim the recorded Session's seeded tab. */
  readonly markSeeded: (sessionId: SessionId) => void
  /** This window's recorded Session for one document, when it exists. */
  readonly editSessionForDocument: (absolutePath: string) => SessionId | undefined
}

/** How one dispatch settled: the Session doing the work and where the report lands. */
export interface GenerationOutcome {
  /** Session whose agent took the task. */
  readonly sessionId: SessionId
  /** Workspace-relative path of the report .docx being written. */
  readonly outputPath: string
}

/** Normalize Windows separators, because host paths and workspace paths must join. */
function normalizePath(path: string): string {
  return path.replace(/\\/g, '/')
}

/** Basename without its final extension, for asset naming. */
function stemOf(path: string): string {
  const base = path.replace(/^.*[/\\]/u, '')
  return base.replace(/\.[^.]+$/u, '') || base
}

/** How long the detached seed waits for the agent's first save before giving up. */
const SEED_DEADLINE_MS = 180_000
/** How often the detached seed polls for the agent's first save. */
const SEED_POLL_MS = 1_000

const sleep = (ms: number, signal: AbortSignal): Promise<void> =>
  new Promise((resolve, reject) => {
    const timer = setTimeout(resolve, ms)
    signal.addEventListener('abort', () => { clearTimeout(timer); reject(signal.reason) }, { once: true })
  })

/**
 * Run one generation to its handoff point. The fast mode returns after one
 * Host call with the edit Session recorded; the caller seats the finished
 * document's edit tab after closing the wizard tab. The AI mode returns once
 * every asset is on disk and the task book is queued, so a failure there is
 * fully reportable by the wizard; the edit-tab seeding that follows the
 * agent's first save runs detached: it outlives the wizard tab and claims
 * nothing on timeout, leaving the files tree as the fallback door to the
 * finished report.
 * @param request - the wizard's collected inputs and host Session.
 * @param deps - plugin-lifetime service closures.
 * @param signal - cancellation for the awaited phase only.
 * @returns the Session at work and the workspace-relative output path.
 */
export async function startGeneration(
  request: GenerationRequest,
  deps: GenerationDeps,
  signal: AbortSignal,
): Promise<GenerationOutcome> {
  const { hostSessionId, workspaceRoot, templatePath, reportName, images } = request
  const root = normalizePath(workspaceRoot).replace(/\/+$/u, '')
  const outputDir = `${REPORTS_DIR}/${reportName}`
  const outputPath = `${outputDir}/${reportName}.docx`
  const assetsDir = `${outputDir}/assets`
  const absoluteOutput = `${root}/${outputPath}`

  // Fast mode: one Host round trip, then the finished document's edit tab
  // seats through the detached retry below — the pane's tab churn from the
  // wizard tab closing can drop the first placement, so seating repeats until
  // the tab is present, and the Session-header seed remains the backstop.
  if (request.mode === 'fast') {
    const result = await deps.generateFast(hostSessionId, {
      templatePath,
      outputPath,
      imagePaths: images.map(image => image.path),
      fields: { title: reportName, ...request.fields },
    }, signal)
    deps.recordEditSession(hostSessionId, result.absolutePath)

    const seatLifetime = new AbortController()
    void (async () => {
      try {
        const deadline = Date.now() + SEED_DEADLINE_MS
        for (;;) {
          if (!deps.editTabIn(hostSessionId, outputPath)) deps.openEditTab(hostSessionId, outputPath)
          if (deps.editTabIn(hostSessionId, outputPath)) {
            deps.markSeeded(hostSessionId)
            return
          }
          if (Date.now() >= deadline) {
            console.error('report: fast edit-tab seating gave up; the files tree still reaches the report')
            return
          }
          await sleep(SEED_POLL_MS, seatLifetime.signal)
        }
      } catch (cause) {
        console.error('report: fast edit-tab seating aborted:', cause)
      }
    })()

    return { sessionId: hostSessionId, outputPath }
  }

  const data: ReportImageDatum[] = []
  for (const image of images) {
    const assetPath = image.analysis.images.thermal === ''
      ? undefined
      : `${assetsDir}/${stemOf(image.path)}.thermal.png`
    if (assetPath !== undefined) {
      await deps.write(hostSessionId, assetPath, image.analysis.images.thermal, signal)
    }
    data.push({ path: image.path, assetPath, analysis: image.analysis })
  }
  const taskBook = buildTaskBook({ templatePath, outputPath, assetsDir, images: data })

  // One document keeps one report Session in this window: regenerating the
  // same output continues that conversation instead of forking another.
  let sessionId = deps.editSessionForDocument(absoluteOutput)
  if (sessionId === undefined) sessionId = request.adoptHost ? hostSessionId : await deps.createSession(root, signal)
  const target = sessionId
  await deps.prompt(target, taskBook, signal)
  deps.navigate(target)

  // Detached seeding: wait for the agent's save-first step, then seat the
  // editor tab beside the conversation and arm the re-entry record.
  const seedLifetime = new AbortController()
  void (async () => {
    try {
      const deadline = Date.now() + SEED_DEADLINE_MS
      for (;;) {
        if (Date.now() >= deadline) return
        if (await deps.stat(target, outputPath, seedLifetime.signal).then(() => true, () => false)) break
        await sleep(SEED_POLL_MS, seedLifetime.signal)
      }
      deps.recordEditSession(target, absoluteOutput)
      deps.openEditTab(target, outputPath)
      deps.markSeeded(target)
    } catch (cause) {
      // Seeding is presentation only: the report keeps generating, and the
      // files tree still reaches the finished document.
      console.error('report: edit-tab seeding gave up:', cause)
    }
  })()

  return { sessionId: target, outputPath }
}

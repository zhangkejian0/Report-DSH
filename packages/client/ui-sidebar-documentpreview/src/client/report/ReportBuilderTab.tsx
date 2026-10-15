/** The report wizard hosted as its own right-Sidebar tab. */
import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import type { PropsLocale, PropsRuntime } from '@deepseek-ai/dsh-client-ui-slots'
import type { TranslateNS } from '@deepseek-ai/dsh-client-locale/client'
import type { ThermalAnalysis } from '@deepseek-ai/dsh-thermal-analyze/types'
import type { WorkspaceDirectoryEntry } from '@deepseek-ai/dsh-api-workspace-files/types'
import { IconRefreshOutlineRegular } from '@deepseek-ai/dsh-client-ui-primitives'
import { analyzeBridge } from '../thermal/analyze-bridge.ts'
import { REPORT_TEMPLATES_DIR, REPORTS_DIR, defaultReportName, isValidReportName } from './definition.ts'
import { reportBridge } from './report-bridge.ts'
import { generationBridge } from './generation-bridge.ts'
import { startGeneration, type GenerationMode } from './generate.ts'
import { sampleTemplateFiles } from './sample-template.ts'
import type { SidebarReportKey } from './locales.ts'
import type {} from './locales.ts'
import css from './ReportBuilderTab.module.css'

/** Props of the wizard tab: its own tab reader, session standards, and copy. */
export type ReportBuilderTabProps =
  PropsRuntime<'sidebar.right.pane.tab'> & PropsLocale<'sidebarReport'>

/** One listed image's preflight state. */
interface ImageRow {
  readonly path: string
  readonly name: string
  readonly selected: boolean
  readonly state: 'idle' | 'analyzing' | 'ok' | 'failed'
  readonly analysis?: ThermalAnalysis
  /** Object URL of the file's own JPEG preview, once loaded. */
  readonly previewUrl?: string
  /** Whether the JPEG preview is still loading. */
  readonly loadingPreview?: boolean
}

/** Directory path join over `/`, keeping workspace-relative form. */
const joinDir = (parent: string, name: string): string => parent === '' ? name : `${parent}/${name}`

/** Whether one listed entry is a JPEG this workflow can analyze. */
const isJpeg = (name: string): boolean => /\.jpe?g$/iu.test(name)

/** Gateway failure code of one thrown Remote result, when it carries one. */
const codeOf = (cause: unknown): string | undefined =>
  typeof cause === 'object' && cause !== null && 'code' in cause && typeof cause.code === 'string'
    ? cause.code
    : undefined

/** An image row whose analysis finished, for dispatch. */
interface AnalyzedRow extends ImageRow {
  readonly analysis: ThermalAnalysis
}

/** Engine refusal reasons with copy of their own. */
const REASON_KEYS: Readonly<Record<string, SidebarReportKey>> = {
  'analyzer-unavailable': 'reason.analyzer-unavailable',
  'no-image-loop': 'reason.no-image-loop',
  'template-markers': 'reason.template-markers',
  'unknown-token': 'reason.unknown-token',
  'template-invalid': 'reason.template-invalid',
  'template-too-large': 'reason.template-too-large',
  'too-many-images': 'reason.too-many-images',
  'analysis-failed': 'reason.analysis-failed',
  'residue': 'reason.residue',
}

/**
 * Localize one generation failure: the Host engine's refusal reasons carry
 * their own guidance; everything else keeps its message.
 * @param cause - the thrown failure.
 * @param mode - which engine ran.
 * @param t - the wizard's translate.
 * @returns the line the wizard shows.
 */
function messageFor(cause: unknown, mode: GenerationMode, t: TranslateNS<'sidebarReport'>): string {
  const fallback = cause instanceof Error ? cause.message : String(cause)
  if (mode !== 'fast') return fallback
  if (typeof cause === 'object' && cause !== null && 'code' in cause && cause.code === 'report-generate/failed') {
    const details = (cause as { readonly details?: { readonly reason?: unknown } }).details
    const key = typeof details?.reason === 'string' ? REASON_KEYS[details.reason] : undefined
    if (key !== undefined) return t(key)
    return t('error.generateFast', { message: fallback })
  }
  return fallback
}

/**
 * Collect the two structural inputs of one report — template and thermal
 * images — with per-image preflight analysis, then dispatch the generation
 * task and hand the main view to the report Session. The layout widens to
 * three columns while its pane is fullscreen and stacks in a docked column;
 * generation starts only with a chosen template, at least one analyzed image,
 * and a filesystem-safe report name. A blank host Session is adopted instead
 * of forking another one, so reaching the wizard from the start page leaves
 * no empty Session behind.
 * @param props - this tab's reader, session standards, and the report dictionary.
 * @returns the wizard, or its unavailable notice while the Remote face is down.
 */
export function ReportBuilderTab({ useTabInfo, sessionId, useSessions, t }: ReportBuilderTabProps): ReactNode {
  const { tab, sidebar } = useTabInfo()
  const cwd = useSessions(sessions => sessions.byId[sessionId]?.cwd)
  const blank = useSessions(sessions => sessions.byId[sessionId]?.blank)
  const rootRef = useRef<HTMLDivElement>(null)
  const preselectApplied = useRef(false)
  /** Analyses by image path, surviving directory switches. */
  const analyses = useRef(new Map<string, { analysis: ThermalAnalysis } | { failure: string }>())
  /** Original-image thumbnail object URLs by path, revoked together on unmount. */
  const thumbnails = useRef(new Map<string, string>())

  const [templates, setTemplates] = useState<readonly string[]>()
  const [templatesError, setTemplatesError] = useState<string>()
  const [templatePath, setTemplatePath] = useState<string>()
  const [creatingSample, setCreatingSample] = useState(false)
  const [dir, setDir] = useState('')
  const [entries, setEntries] = useState<readonly WorkspaceDirectoryEntry[]>()
  const [dirError, setDirError] = useState<string>()
  const [rows, setRows] = useState<readonly ImageRow[]>([])
  const [reportName, setReportName] = useState('')
  const [mode, setMode] = useState<GenerationMode>('fast')
  const [company, setCompany] = useState('')
  const [inspector, setInspector] = useState('')
  const [device, setDevice] = useState('')
  const [phase, setPhase] = useState<'collecting' | 'dispatching'>('collecting')
  const [dispatchError, setDispatchError] = useState<string>()

  const listDirectory = reportBridge.list
  const write = reportBridge.write

  const reloadTemplates = useCallback((): (() => void) | undefined => {
    if (cwd === undefined || listDirectory === undefined) return
    const closed = { settled: false }
    void listDirectory(sessionId, REPORT_TEMPLATES_DIR, tab.signal).then((found) => {
      if (closed.settled) return
      setTemplates(found.filter(entry => entry.type === 'file' && /\.docx$/iu.test(entry.name))
        .map(entry => `${REPORT_TEMPLATES_DIR}/${entry.name}`))
      setTemplatesError(undefined)
    }, (cause: unknown) => {
      if (closed.settled) return
      setTemplates([])
      // A workspace without the directory yet is the empty state, not a failure.
      if (codeOf(cause) === 'workspace-file/not-found') setTemplatesError(undefined)
      else setTemplatesError(cause instanceof Error ? cause.message : String(cause))
    })
    return () => { closed.settled = true }
  }, [cwd, listDirectory, sessionId, tab.signal])

  useEffect(() => reloadTemplates(), [reloadTemplates])

  /** One level of the image browser: entries, rows, and directory failures. */
  useEffect(() => {
    if (cwd === undefined || listDirectory === undefined) return
    const closed = { settled: false }
    // The listing endpoint refuses an empty path; the browser's root is the
    // session's own workspace root, every deeper level its relative path.
    void listDirectory(sessionId, dir === '' ? cwd : dir, tab.signal).then((found) => {
      if (closed.settled) return
      setEntries(found)
      setDirError(undefined)
      setRows(found.filter(entry => entry.type === 'file' && isJpeg(entry.name)).map((entry) => {
        const path = joinDir(dir, entry.name)
        const cached = analyses.current.get(path)
        const state = cached === undefined ? 'idle' as const : 'analysis' in cached ? 'ok' as const : 'failed' as const
        const previewUrl = thumbnails.current.get(path)
        return {
          path, name: entry.name, selected: false, state,
          ...(cached !== undefined && 'analysis' in cached ? { analysis: cached.analysis } : {}),
          ...(previewUrl === undefined ? { loadingPreview: true } : { previewUrl }),
        }
      }))
    }, (cause: unknown) => {
      if (closed.settled) return
      setEntries(undefined)
      setRows([])
      setDirError(cause instanceof Error ? cause.message : String(cause))
    })
    return () => { closed.settled = true }
  }, [cwd, listDirectory, sessionId, dir, tab.signal])

  /** Preflight: analyze every newly selected image once, caching the outcome. */
  useEffect(() => {
    const analyze = analyzeBridge.analyze
    if (analyze === undefined) return
    const pending = rows.filter(row => row.selected && row.state === 'idle')
    if (pending.length === 0) return
    setRows(current => current.map(row =>
      row.selected && row.state === 'idle' ? { ...row, state: 'analyzing' } : row))
    for (const row of pending) {
      void analyze(sessionId, row.path, tab.signal).then((analysis) => {
        analyses.current.set(row.path, { analysis })
        return { path: row.path, analysis } as const
      }, (cause: unknown) => {
        const failure = cause instanceof Error ? cause.message : String(cause)
        analyses.current.set(row.path, { failure })
        return { path: row.path, failure } as const
      }).then((result) => {
        setRows(current => current.map(item => item.path === result.path
          ? 'analysis' in result
            ? { ...item, state: 'ok', analysis: result.analysis }
            : { ...item, state: 'failed' }
          : item))
      })
    }
  }, [rows, sessionId, tab.signal])

  /**
   * Load every listed image's own JPEG as a thumbnail: the visible-light
   * preview the file carries, replaced by the palette render once analysis
   * succeeds. Object URLs are cached per path and revoked once on unmount.
   */
  useEffect(() => {
    const read = reportBridge.read
    if (read === undefined) return
    const wanted = rows.filter(row => row.previewUrl === undefined && !thumbnails.current.has(row.path))
    if (wanted.length === 0) return
    setRows(current => current.map(row =>
      wanted.some(pending => pending.path === row.path) ? { ...row, loadingPreview: true } : row))
    for (const row of wanted) {
      void read(sessionId, row.path, tab.signal).then((bytes) => {
        const url = URL.createObjectURL(new Blob([new Uint8Array(bytes)], { type: 'image/jpeg' }))
        thumbnails.current.set(row.path, url)
        return { path: row.path, url } as const
      }, () => ({ path: row.path, url: undefined } as const)).then((result) => {
        setRows(current => current.map(item => item.path === result.path
          ? result.url === undefined
            ? { ...item, loadingPreview: false }
            : { ...item, loadingPreview: false, previewUrl: result.url }
          : item))
      })
    }
  }, [rows, sessionId, tab.signal])

  /** Revoke every thumbnail URL this tab minted. */
  useEffect(() => () => {
    for (const url of thumbnails.current.values()) URL.revokeObjectURL(url)
    thumbnails.current.clear()
  }, [])

  /** A handoff names its image or its template; land on either selected. */
  useEffect(() => {
    const params = tab.navigation.params
    if (typeof params !== 'object' || params === null) return
    if ('preselectTemplate' in params && typeof params.preselectTemplate === 'string') {
      if (templatePath === undefined) setTemplatePath(params.preselectTemplate)
      return
    }
    if (preselectApplied.current || entries === undefined) return
    if (!('preselectImage' in params) || typeof params.preselectImage !== 'string') {
      preselectApplied.current = true
      return
    }
    const preselect = params.preselectImage
    const slash = preselect.lastIndexOf('/')
    const parent = slash === -1 ? '' : preselect.slice(0, slash)
    const name = preselect.slice(slash + 1)
    if (parent !== dir) { setDir(parent); return }
    preselectApplied.current = true
    setRows(current => current.some(row => row.name === name)
      ? current.map(row => row.name === name ? { ...row, selected: true } : row)
      : current)
  })

  const selected = useMemo(() => rows.filter(row => row.selected), [rows])
  const analyzedOk = useMemo(
    () => selected.filter((row): row is AnalyzedRow => row.state === 'ok' && row.analysis !== undefined),
    [selected],
  )
  const defaultName = useMemo(
    () => templatePath === undefined ? '' : defaultReportName(templatePath, new Date()),
    [templatePath],
  )
  const name = reportName.trim() === '' ? defaultName : reportName.trim()
  const nameValid = isValidReportName(name)
  const canGenerate = templatePath !== undefined && analyzedOk.length > 0 && nameValid

  const directories = useMemo(() =>
    (entries ?? []).filter(entry => entry.type === 'directory')
      .map(entry => ({ name: entry.name, path: joinDir(dir, entry.name) })),
  [entries, dir])

  /** Write the embedded sample template and its usage note into the empty directory. */
  const createSample = (): void => {
    if (cwd === undefined || write === undefined) return
    setCreatingSample(true)
    void Promise.all(sampleTemplateFiles().map(file => write(sessionId, file.path, file.base64, tab.signal)))
      .then(() => { reloadTemplates() })
      .finally(() => { setCreatingSample(false) })
  }

  /** Dispatch the generation and step aside into the report Session. */
  const generate = (): void => {
    if (phase !== 'collecting' || !canGenerate || cwd === undefined) return
    const chosenTemplate = templatePath
    if (chosenTemplate === undefined) return
    const deps = generationBridge.deps
    if (deps === undefined) {
      setDispatchError(t('error.prompt', { message: 'remote unavailable' }))
      return
    }
    setPhase('dispatching')
    setDispatchError(undefined)
    const fields: { company?: string; inspector?: string; device?: string } = {}
    if (company.trim() !== '') fields.company = company.trim()
    if (inspector.trim() !== '') fields.inspector = inspector.trim()
    if (device.trim() !== '') fields.device = device.trim()
    void startGeneration({
      hostSessionId: sessionId,
      mode,
      adoptHost: blank === true,
      workspaceRoot: cwd,
      templatePath: chosenTemplate,
      reportName: name,
      images: analyzedOk.map(row => ({ path: row.path, analysis: row.analysis })),
      ...(mode === 'fast' ? { fields } : {}),
    }, deps, tab.signal).then(() => {
      tab.actions.close()
    }, (cause: unknown) => {
      setPhase('collecting')
      setDispatchError(messageFor(cause, mode, t))
    })
  }

  if (cwd === undefined || listDirectory === undefined || write === undefined) {
    return <p className={css.status} role="status">{t('directory.failed', { message: 'workspace unavailable' })}</p>
  }

  return (
    <div ref={rootRef} className={css.root} data-report-wizard data-fullscreen={sidebar.fullscreen ? 'true' : 'false'}>
      {!sidebar.fullscreen && (
        <div className={css.fullscreenRow}>
          <button type="button" className={css.link}
            onClick={() => { const root = rootRef.current; if (root !== null) reportBridge.requestFullscreen?.(root) }}>
            {t('fullscreen')}
          </button>
        </div>
      )}

      <section className={`${css.section} ${css.templates}`}>
        <h2 className={css.sectionTitle}>{t('section.templates')}</h2>
        {templatesError !== undefined && <p className={css.note}>{t('templates.failed', { message: templatesError })}</p>}
        {templates !== undefined && templates.length === 0 && templatesError === undefined && (
          <div className={css.note}>
            <p>{t('templates.empty')}</p>
            <button type="button" className={css.link} onClick={createSample} disabled={creatingSample}>
              {creatingSample ? t('templates.creating') : t('templates.createSample')}
            </button>
          </div>
        )}
        <ul className={css.templateList}>
          {templates?.map(path => (
            <li key={path}>
              <button
                type="button"
                className={path === templatePath ? `${css.template} ${css.templateActive}` : css.template}
                onClick={() => { setTemplatePath(path === templatePath ? undefined : path) }}
              >
                {path.slice(REPORT_TEMPLATES_DIR.length + 1)}
              </button>
            </li>
          ))}
        </ul>
        <button type="button" className={css.link} onClick={() => { reloadTemplates() }}>
          <IconRefreshOutlineRegular /> {t('templates.refresh')}
        </button>
      </section>

      <section className={`${css.section} ${css.images}`}>
        <h2 className={css.sectionTitle}>{t('section.images')}</h2>
        <div className={css.directory}>
          <span className={css.directoryPath}>{dir === '' ? t('directory.root') : dir}</span>
          {dir !== '' && (
            <button type="button" className={css.link}
              onClick={() => { setDir(dir.includes('/') ? dir.slice(0, dir.lastIndexOf('/')) : '') }}>
              {t('directory.up')}
            </button>
          )}
        </div>
        {directories.length > 0 && (
          <div className={css.directoryChips}>
            {directories.map(item => (
              <button type="button" key={item.path} className={css.chip} onClick={() => { setDir(item.path) }}>
                {item.name}
              </button>
            ))}
          </div>
        )}
        {dirError !== undefined && <p className={css.note}>{t('directory.failed', { message: dirError })}</p>}
        {entries !== undefined && rows.length === 0 && dirError === undefined && (
          <p className={css.note}>{t('images.empty')}</p>
        )}
        <div className={css.imageGrid}>
          {rows.map((row) => {
            // The palette render once analyzed, the file's own JPEG until then.
            const thumb = row.analysis !== undefined && row.analysis.images.thermal !== ''
              ? `data:image/png;base64,${row.analysis.images.thermal}`
              : row.previewUrl
            const badge = row.state === 'analyzing' ? t('images.analyzing')
              : row.state === 'ok' && row.analysis !== undefined
                ? t('images.max', { max: row.analysis.stats.max.toFixed(1) })
                : row.state === 'failed' ? t('images.failed')
                  : undefined
            return (
              <label key={row.path} className={row.selected ? `${css.imageCard} ${css.imageCardActive}` : css.imageCard}>
                <input
                  type="checkbox"
                  checked={row.selected}
                  onChange={(event) => {
                    setRows(current => current.map(item => item.path === row.path
                      ? { ...item, selected: event.target.checked }
                      : item))
                  }}
                />
                {thumb !== undefined
                  ? <img className={css.imageThumb} src={thumb} alt={row.name} draggable={false} />
                  : <span className={css.imagePlaceholder} />}
                <span className={css.imageName} title={row.path}>{row.name}</span>
                {badge !== undefined && (
                  <span className={row.state === 'failed' ? css.badgeFailed : css.badge}>{badge}</span>
                )}
              </label>
            )
          })}
        </div>
        <div className={css.imageActions}>
          <span>{t('images.selected', { count: String(selected.length) })}</span>
          <button type="button" className={css.link}
            onClick={() => { setRows(current => current.map(row => ({ ...row, selected: true }))) }}>
            {t('images.selectAll')}
          </button>
          <button type="button" className={css.link}
            onClick={() => { setRows(current => current.map(row => ({ ...row, selected: false }))) }}>
            {t('images.clear')}
          </button>
        </div>
      </section>

      <section className={`${css.section} ${css.output}`}>
        <h2 className={css.sectionTitle}>{t('section.output')}</h2>
        <label className={css.field}>
          <span>{t('output.name')}</span>
          <input
            value={reportName}
            placeholder={defaultName}
            onChange={(event) => { setReportName(event.target.value) }}
            disabled={phase === 'dispatching'}
          />
        </label>
        <p className={css.location}>
          {t('output.location')}：{REPORTS_DIR}/{nameValid ? name : '…'}/
        </p>
        <div className={css.modeRow} role="radiogroup" aria-label={t('mode.label')}>
          <button
            type="button" role="radio" aria-checked={mode === 'fast'}
            className={mode === 'fast' ? `${css.modeButton} ${css.modeButtonActive}` : css.modeButton}
            onClick={() => { setMode('fast') }}
          >
            {t('mode.fast')}
          </button>
          <button
            type="button" role="radio" aria-checked={mode === 'ai'}
            className={mode === 'ai' ? `${css.modeButton} ${css.modeButtonActive}` : css.modeButton}
            onClick={() => { setMode('ai') }}
          >
            {t('mode.ai')}
          </button>
        </div>
        {mode === 'fast' && (
          <>
            <label className={css.field}>
              <span>{t('fields.company')}</span>
              <input value={company} placeholder={t('fields.optional')} disabled={phase === 'dispatching'}
                onChange={(event) => { setCompany(event.target.value) }} />
            </label>
            <label className={css.field}>
              <span>{t('fields.inspector')}</span>
              <input value={inspector} placeholder={t('fields.optional')} disabled={phase === 'dispatching'}
                onChange={(event) => { setInspector(event.target.value) }} />
            </label>
            <label className={css.field}>
              <span>{t('fields.device')}</span>
              <input value={device} placeholder={t('fields.optional')} disabled={phase === 'dispatching'}
                onChange={(event) => { setDevice(event.target.value) }} />
            </label>
          </>
        )}
        {!nameValid && (templatePath !== undefined || reportName.trim() !== '') && (
          <p className={css.note}>{t('output.invalidName')}</p>
        )}
        <p className={css.preflight}>
          {t('preflight', { ok: String(analyzedOk.length), total: String(selected.length) })}
        </p>
        {phase === 'collecting' && templatePath === undefined && <p className={css.note}>{t('generate.needTemplate')}</p>}
        {phase === 'collecting' && templatePath !== undefined && analyzedOk.length === 0
          && <p className={css.note}>{t('generate.needImages')}</p>}
        {dispatchError !== undefined && <p className={css.note}>{dispatchError}</p>}
        <button
          type="button"
          className={css.generate}
          disabled={!canGenerate || phase === 'dispatching'}
          onClick={generate}
        >
          {phase === 'dispatching' ? (mode === 'fast' ? t('generatingFast') : t('generating')) : t('generate')}
        </button>
      </section>
    </div>
  )
}

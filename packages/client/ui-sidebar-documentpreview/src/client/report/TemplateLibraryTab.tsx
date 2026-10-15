/** The template library hosted as its own right-Sidebar tab. */
import { useCallback, useEffect, useState, type ReactNode } from 'react'
import type { PropsLocale, PropsRuntime } from '@deepseek-ai/dsh-client-ui-slots'
import type {} from '@deepseek-ai/dsh-client-ui-sidebar-right/client'
import { sessionFileAddress } from '@deepseek-ai/dsh-util-workspace-path'
import { editAddressFor } from '../wordinweb/edit-address.ts'
import { isValidReportName, REPORT_BUILDER_KIND, REPORT_TEMPLATES_DIR, uniqueTemplateName } from './definition.ts'
import { reportBridge } from './report-bridge.ts'
import { BLANK_TEMPLATE_BASE64, sampleTemplateFiles } from './sample-template.ts'
import type {} from './locales.ts'
import css from './TemplateLibraryTab.module.css'

/** Props of the library tab: its own tab reader, session standards, and copy. */
export type TemplateLibraryTabProps =
  PropsRuntime<'sidebar.right.pane.tab'> & PropsLocale<'sidebarReport'>

/** Gateway failure code of one thrown Remote result, when it carries one. */
const codeOf = (cause: unknown): string | undefined =>
  typeof cause === 'object' && cause !== null && 'code' in cause && typeof cause.code === 'string'
    ? cause.code
    : undefined

/** Why 「新建空白模版」 refused or failed, mapped to one notice line each. */
type BlankProblem =
  | { readonly kind: 'invalid' }
  | { readonly kind: 'exists' }
  | { readonly kind: 'failed'; readonly message: string }

/**
 * List the workspace's report templates with the two doors each one opens:
 * generation, through the wizard with the template preselected, and editing,
 * through the WordInWeb editor's own session chain. A new blank template can
 * be started from zero under a typed name, an empty directory offers the
 * embedded sample template, and a collapsed card keeps the token vocabulary
 * in reach for template authors.
 * @param props - this tab's reader, session standards, and the report dictionary.
 * @returns the library list, its empty state, or its unavailable notice.
 */
export function TemplateLibraryTab({ useTabInfo, sessionId, useSessions, t }: TemplateLibraryTabProps): ReactNode {
  const { tab } = useTabInfo()
  const cwd = useSessions(sessions => sessions.byId[sessionId]?.cwd)
  const [templates, setTemplates] = useState<readonly string[]>()
  const [error, setError] = useState<string>()
  const [creating, setCreating] = useState(false)
  const [showConvention, setShowConvention] = useState(false)
  const [newName, setNewName] = useState('')
  const [creatingBlank, setCreatingBlank] = useState(false)
  const [blankProblem, setBlankProblem] = useState<BlankProblem>()

  const listDirectory = reportBridge.list
  const write = reportBridge.write

  const reload = useCallback((): (() => void) | undefined => {
    if (cwd === undefined || listDirectory === undefined) return
    const closed = { settled: false }
    void listDirectory(sessionId, REPORT_TEMPLATES_DIR, tab.signal).then((found) => {
      if (closed.settled) return
      setTemplates(found.filter(entry => entry.type === 'file' && /\.docx$/iu.test(entry.name))
        .map(entry => `${REPORT_TEMPLATES_DIR}/${entry.name}`))
      setError(undefined)
    }, (cause: unknown) => {
      if (closed.settled) return
      setTemplates([])
      // A workspace without the directory yet is the empty state, not a failure.
      if (codeOf(cause) === 'workspace-file/not-found') setError(undefined)
      else setError(cause instanceof Error ? cause.message : String(cause))
    })
    return () => { closed.settled = true }
  }, [cwd, listDirectory, sessionId, tab.signal])

  useEffect(() => reload(), [reload])

  /** Write the embedded sample template and its usage note into the empty directory. */
  const createSample = (): void => {
    if (cwd === undefined || write === undefined) return
    setCreating(true)
    void Promise.all(sampleTemplateFiles().map(file => write(sessionId, file.path, file.base64, tab.signal)))
      .then(() => { reload() })
      .finally(() => { setCreating(false) })
  }

  /** Write one blank template under the entered name, then open it for hand editing. */
  const createBlank = (): void => {
    if (cwd === undefined || write === undefined || templates === undefined) return
    const entered = newName.trim().replace(/\.docx$/iu, '').trim()
    if (entered !== '' && !isValidReportName(entered)) {
      setBlankProblem({ kind: 'invalid' })
      return
    }
    const taken = new Set(templates.map(path => path.slice(REPORT_TEMPLATES_DIR.length + 1)))
    const name = entered === '' ? uniqueTemplateName(taken) : `${entered}.docx`
    if (entered !== '' && taken.has(name)) {
      setBlankProblem({ kind: 'exists' })
      return
    }
    setCreatingBlank(true)
    setBlankProblem(undefined)
    void write(sessionId, `${REPORT_TEMPLATES_DIR}/${name}`, BLANK_TEMPLATE_BASE64, tab.signal)
      .then(() => {
        setNewName('')
        reload()
        tab.actions.openResource(editAddressFor(sessionFileAddress(sessionId, `${REPORT_TEMPLATES_DIR}/${name}`)))
      }, (cause: unknown) => {
        setBlankProblem({ kind: 'failed', message: cause instanceof Error ? cause.message : String(cause) })
      })
      .finally(() => { setCreatingBlank(false) })
  }

  if (cwd === undefined || listDirectory === undefined || write === undefined) {
    return <p className={css.status} role="status">{t('library.unavailable')}</p>
  }

  return (
    <div className={css.root} data-report-templates>
      <button type="button" className={css.conventionToggle} onClick={() => { setShowConvention(value => !value) }}>
        {t('library.convention')}
      </button>
      {showConvention && (
        <div className={css.convention}>
          <p>{t('library.convention.global')}</p>
          <p>{t('library.convention.image')}</p>
          <p>{t('library.convention.each')}</p>
        </div>
      )}
      <div className={css.create}>
        <input
          className={css.createName}
          value={newName}
          placeholder={t('library.blank.name')}
          aria-label={t('library.blank.name')}
          onChange={(event) => { setNewName(event.target.value); setBlankProblem(undefined) }}
        />
        <button
          type="button"
          className={css.createButton}
          onClick={createBlank}
          disabled={creatingBlank || templates === undefined}
        >
          {creatingBlank ? t('library.blank.busy') : t('library.blank.create')}
        </button>
      </div>
      {blankProblem !== undefined && (
        <p className={css.note}>
          {blankProblem.kind === 'invalid' ? t('library.blank.invalid')
            : blankProblem.kind === 'exists' ? t('library.blank.exists')
              : t('library.blank.failed', { message: blankProblem.message })}
        </p>
      )}
      {error !== undefined && <p className={css.note}>{t('library.failed', { message: error })}</p>}
      {templates !== undefined && templates.length === 0 && error === undefined && (
        <div className={css.note}>
          <p>{t('templates.empty')}</p>
          <button type="button" className={css.link} onClick={createSample} disabled={creating}>
            {creating ? t('templates.creating') : t('templates.createSample')}
          </button>
        </div>
      )}
      <ul className={css.list}>
        {templates?.map(path => (
          <li key={path} className={css.item}>
            <span className={css.name} title={path}>{path.slice(REPORT_TEMPLATES_DIR.length + 1)}</span>
            <span className={css.actions}>
              <button
                type="button"
                className={css.action}
                onClick={() => {
                  tab.actions.openTab(REPORT_BUILDER_KIND, { params: { preselectTemplate: path } })
                }}
              >
                {t('library.generate')}
              </button>
              <button
                type="button"
                className={css.action}
                onClick={() => {
                  tab.actions.openResource(editAddressFor(sessionFileAddress(sessionId, path)))
                }}
              >
                {t('library.edit')}
              </button>
            </span>
          </li>
        ))}
      </ul>
    </div>
  )
}

/** Editable WordInWeb document hosted as its own right-Sidebar tab. */
import { useEffect, useRef, useState, type ReactNode } from 'react'
import type { PropsLocale, PropsRuntime } from '@deepseek-ai/dsh-client-ui-slots'
import { DocxToolbar, DocxView, type DocxViewApi } from 'wordinweb'
import { basenameOf } from '../definition.ts'
import { reconcileBoundCoreProperties } from './core-properties.ts'
import { editTargetOf, parseEditAddress } from './edit-address.ts'
import { editSessionFor, editSessionPathOf, recordEditSession } from './edit-sessions.ts'
import { bridge } from './save-bridge.ts'
import type {} from './locales.ts'
import css from './WordinwebEditTab.module.css'

/** Props of the WordInWeb edit tab: its own tab reader and localized copy. */
export type WordinwebEditTabProps =
  PropsRuntime<'sidebar.right.pane.tab'> & PropsLocale<'sidebarWordinweb'>

/** Base64-encode document bytes in btoa-safe chunks. */
function toBase64(bytes: Uint8Array): string {
  let binary = ''
  const chunk = 0x8000
  for (let offset = 0; offset < bytes.length; offset += chunk) {
    binary += String.fromCharCode(...bytes.subarray(offset, offset + chunk))
  }
  return btoa(binary)
}

/** Strip the last path segment to obtain the directory owning one file. */
function directoryOf(absolutePath: string): string {
  return absolutePath.replace(/[\\/][^\\/]+$/u, '')
}

/** Normalize Windows separators, because resource addresses decode to `/`-separated paths. */
function normalizeSeparators(path: string): string {
  return path.replace(/\\/g, '/')
}

/**
 * Host the advanced WordInWeb ribbon editor for one `.docx`, and open the
 * Session that edits it. Entering edit state creates that Session in the
 * document's own directory, because the `word_document_*` tools reach files
 * only through their owning Workspace; one document keeps one such Session
 * per window. The main view then moves onto the Session — its own Sidebar
 * shows this editor through the tab the record seeds — and the preview-side
 * tab that started the flow closes itself.
 * @param props - this tab's reader and the WordInWeb dictionary.
 * @returns the ribbon, the editable document, and its save and download actions.
 */
export function WordinwebEditTab({ useTabInfo, t }: WordinwebEditTabProps): ReactNode {
  const { tab } = useTabInfo()
  const address = tab.contentId
  const name = basenameOf(parseEditAddress(address) ?? '') || 'document.docx'
  const target = editTargetOf(address)
  const ownerSession = target?.sessionId
  const path = target?.path
  const [data, setData] = useState<Uint8Array>()
  const [api, setApi] = useState<DocxViewApi | null>(null)
  const [saving, setSaving] = useState(false)
  const [status, setStatus] = useState<string>()
  const createdFor = useRef<string>()

  useEffect(() => {
    if (ownerSession === undefined || path === undefined) return
    const read = bridge.read
    if (read === undefined) return
    const closed = { settled: false }
    void read(ownerSession, path, tab.signal).then((bytes) => {
      if (closed.settled) return
      setData(bytes)
    }, (error: unknown) => {
      if (closed.settled) return
      setStatus(t('loadFailed', { message: error instanceof Error ? error.message : String(error) }))
    })
    return () => { closed.settled = true }
  }, [ownerSession, path, tab.signal, t])

  useEffect(() => {
    if (ownerSession === undefined || path === undefined) return
    // The tab an edit Session's own Sidebar shows hosts its Session already:
    // creating another one for the same document would fork the conversation
    // the operator just opened.
    if (editSessionPathOf(ownerSession) === path) return
    const create = bridge.create
    const stat = bridge.stat
    const prompt = bridge.prompt
    const navigate = bridge.navigate
    if (create === undefined || stat === undefined || prompt === undefined || navigate === undefined) return
    const key = `${ownerSession}:${path}`
    if (createdFor.current === key) return
    createdFor.current = key
    const closed = { settled: false }
    void (async () => {
      try {
        const absolutePath = normalizeSeparators(await stat(ownerSession, path, tab.signal))
        // One document keeps one edit Session per window: re-entering edit
        // moves to the Session that already owns it instead of forking another.
        let sessionId = editSessionFor(absolutePath)
        if (sessionId === undefined) {
          sessionId = await create(directoryOf(absolutePath), tab.signal)
          // The record outlives this tab, so the Session's own Sidebar seeds the
          // document tab when the operator reaches it. The absolute path
          // matters: the created Session's Workspace root is the document's own
          // directory, not the previewing Session's root.
          recordEditSession(sessionId, absolutePath)
          // A Session joins its Workspace list only once it takes a turn, so
          // this one opens by naming the document it edits rather than blank.
          await prompt(sessionId, t('sessionInstruction', { path }), tab.signal)
        }
        if (closed.settled) return
        setStatus(t('sessionReady'))
        // The edit Session takes over: the main view moves onto it, its own
        // Sidebar shows the seeded document tab, and this preview-side host
        // steps aside so the editor lives in exactly one place.
        navigate(sessionId)
        tab.actions.close()
      } catch (error: unknown) {
        createdFor.current = undefined
        if (closed.settled) return
        setStatus(t('sessionFailed', { message: error instanceof Error ? error.message : String(error) }))
      }
    })()
    return () => { closed.settled = true }
  }, [ownerSession, path, tab.signal, t])

  if (data === undefined) {
    return <p className={css.status} role="status">{status ?? t('loading')}</p>
  }

  /** Serialize the editor model and sync the core properties its bound controls write to. */
  const serialized = (): Uint8Array<ArrayBuffer> | undefined =>
    api === null ? undefined : reconcileBoundCoreProperties(new Uint8Array(api.save()))

  const download = (): void => {
    const bytes = serialized()
    if (bytes === undefined) return
    const blob = new Blob([bytes], {
      type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    })
    const url = URL.createObjectURL(blob)
    const anchor = document.createElement('a')
    anchor.href = url
    anchor.download = name
    anchor.click()
    URL.revokeObjectURL(url)
  }

  const save = async (): Promise<void> => {
    if (ownerSession === undefined || path === undefined || bridge.save === undefined) return
    const bytes = serialized()
    if (bytes === undefined) return
    setSaving(true)
    setStatus(undefined)
    try {
      await bridge.save(ownerSession, path, toBase64(bytes), tab.signal)
      setStatus(t('saved'))
    } catch (error: unknown) {
      setStatus(t('saveFailed', { message: error instanceof Error ? error.message : String(error) }))
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className={css.root} data-wordinweb-edit>
      <div className={css.bar}>
        {status !== undefined && <span className={css.status} role="status">{status}</span>}
        <button type="button" className={css.button} onClick={download} disabled={api === null}>
          {t('download')}
        </button>
        <button type="button" className={css.primary} onClick={() => { void save() }} disabled={saving || api === null}>
          {saving ? t('saving') : t('save')}
        </button>
      </div>
      <div className={css.editor}>
        {api !== null && <DocxToolbar api={api} mode="advanced" onSave={download} />}
        <DocxView source={data} editable commentAuthor="DSH" fitWidth zoom={1} onReady={setApi} style={{ height: '100%' }} />
      </div>
    </div>
  )
}

/** Editable WordInWeb document hosted as its own right-Sidebar tab. */
import { useEffect, useRef, useState, type ReactNode } from 'react'
import type { PropsLocale, PropsRuntime } from '@deepseek-ai/dsh-client-ui-slots'
import { DocxToolbar, DocxView, type DocxViewApi } from 'wordinweb'
import { basenameOf } from '../definition.ts'
import { reconcileBoundCoreProperties } from './core-properties.ts'
import { editTargetOf, parseEditAddress } from './edit-address.ts'
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

/**
 * Host the advanced WordInWeb ribbon editor for one `.docx`, and open the
 * Session that edits it. Entering edit state creates that Session in the
 * document's own directory, because the `word_document_*` tools reach files
 * only through their owning Workspace; it is archived so the list stays clean
 * while 「查看对话」 still reaches its native conversation.
 * @param props - this tab's reader and the WordInWeb dictionary.
 * @returns the ribbon, the editable document, and its save, download, and conversation actions.
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
    const create = bridge.create
    const stat = bridge.stat
    const prompt = bridge.prompt
    if (create === undefined || stat === undefined || prompt === undefined) return
    const key = `${ownerSession}:${path}`
    if (createdFor.current === key) return
    createdFor.current = key
    const closed = { settled: false }
    void (async () => {
      try {
        const sessionId = await create(directoryOf(await stat(ownerSession, path, tab.signal)), tab.signal)
        // A Session joins its Workspace list only once it takes a turn, so this
        // one opens by naming the document it edits rather than sitting blank.
        await prompt(sessionId, t('sessionInstruction', { path }), tab.signal)
        if (closed.settled) return
        setStatus(t('sessionReady'))
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

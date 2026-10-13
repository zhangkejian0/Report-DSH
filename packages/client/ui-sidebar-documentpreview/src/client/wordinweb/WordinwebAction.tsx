/** Toolbar action that opens the full WordInWeb editor in a modal dialog. */
import { useEffect, useRef, useState, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import type { PropsLocale, PropsRuntime } from '@deepseek-ai/dsh-client-ui-slots'
import { parseFileAddress } from '@deepseek-ai/dsh-util-workspace-path'
import { DocxToolbar, DocxView, type DocxViewApi } from 'wordinweb'
import { basenameOf } from '../definition.ts'
import { bridge } from './save-bridge.ts'
import type {} from './locales.ts'
import css from './WordinwebAction.module.css'

/** WordInWeb toolbar action props: preview content and localized copy. */
export type WordinwebActionProps = PropsRuntime<'sidebar.right.tab.document.action'>
  & PropsLocale<'sidebarWordinweb'>

/** Base64-encode document bytes in btoa-safe chunks. */
function toBase64(bytes: Uint8Array): string {
  let binary = ''
  const chunk = 0x8000
  for (let offset = 0; offset < bytes.length; offset += chunk) {
    binary += String.fromCharCode(...bytes.subarray(offset, offset + chunk))
  }
  return btoa(binary)
}

/**
 * Open the complete WordInWeb editor (advanced ribbon) in a modal dialog.
 * @param props - toolbar content, tab reader, and localized copy.
 * @returns the horizontal edit button; the modal renders through a portal.
 */
export function WordinwebAction({ content, useTabInfo, t }: WordinwebActionProps): ReactNode {
  const [open, setOpen] = useState(false)
  const [api, setApi] = useState<DocxViewApi | null>(null)
  const [saving, setSaving] = useState(false)
  const [status, setStatus] = useState<string>()
  // Document bytes start from the preview content; AI edits swap in fresh
  // bytes from the host, and docSeq remounts the viewer on each swap.
  const initialData = content.kind === 'bytes' ? content.data : undefined
  const [docData, setDocData] = useState<Uint8Array | undefined>(initialData)
  const [docSeq, setDocSeq] = useState(0)
  // Floating AI assistant state: one auto-created workspace session per modal.
  const [chatOpen, setChatOpen] = useState(true)
  const [chatBusy, setChatBusy] = useState(false)
  const [chatInput, setChatInput] = useState('')
  const [chatText, setChatText] = useState<string>()
  const [editSession, setEditSession] = useState<string>()
  const { tab } = useTabInfo()
  // The action slot receives no resource address; the tab's content id is
  // the file address whose decoded basename is the document name.
  const name = basenameOf(String(tab.contentId ?? '')) || 'document.docx'
  const address = parseFileAddress(String(tab.contentId ?? ''))
  const data = docData ?? initialData
  const dialogRef = useRef<HTMLDivElement>(null)

  useEffect(() => { setDocData(undefined); setDocSeq(value => value + 1) }, [initialData])

  useEffect(() => {
    if (!open) return
    const onKey = (event: KeyboardEvent): void => {
      if (event.key === 'Escape') setOpen(false)
    }
    window.addEventListener('keydown', onKey)
    return () => { window.removeEventListener('keydown', onKey) }
  }, [open])

  if (data === undefined) return null

  const download = (): void => {
    if (api === null) return
    const bytes = api.save()
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

  const savable = api !== null && address?.scope === 'session' && bridge.save !== undefined

  const save = async (): Promise<void> => {
    if (api === null || address?.scope !== 'session' || bridge.save === undefined) return
    setSaving(true)
    setStatus(undefined)
    try {
      await bridge.save(address.sessionId, address.path, toBase64(api.save()), tab.signal)
      setStatus(t('saved'))
    } catch (error: unknown) {
      setStatus(t('saveFailed', { message: error instanceof Error ? error.message : String(error) }))
    } finally {
      setSaving(false)
    }
  }

  const chattable = address?.scope === 'session' && bridge.prompt !== undefined && bridge.create !== undefined && bridge.stat !== undefined

  const sendChat = async (): Promise<void> => {
    const instruction = chatInput.trim()
    if (instruction === '' || chatBusy || !chattable || address?.scope !== 'session') return
    setChatInput('')
    setChatBusy(true)
    const before = data?.byteLength ?? 0
    let sessionId = editSession
    try {
      if (sessionId === undefined) {
        setChatText(t('chatCreating'))
        const absolute = await bridge.stat?.(address.sessionId, address.path, tab.signal)
        if (absolute === undefined) throw new Error('cannot resolve absolute path')
        const cwd = absolute.replace(/[\\/][^\\/]+$/u, '')
        sessionId = await bridge.create?.(cwd, tab.signal)
        if (sessionId === undefined) throw new Error('session create returned no id')
        setEditSession(sessionId)
      }
      setChatText(t('chatWaiting'))
      await bridge.prompt?.(sessionId as never, t('chatInstruction', { path: address.path, instruction }), tab.signal)
      // The fresh session's agent edits the file; poll until the bytes change.
      let changed = false
      for (let poll = 0; poll < 120 && changed === false; poll++) {
        await new Promise((resolve) => { setTimeout(resolve, 2000) })
        if (tab.signal.aborted) break
        try {
          const fresh = await bridge.read?.(address.sessionId, address.path, tab.signal)
          if (fresh !== undefined && fresh.byteLength !== before) {
            changed = true
            setDocData(fresh)
            setDocSeq(value => value + 1)
          }
        } catch { /* transient read failure: keep polling */ }
      }
      setChatText(changed ? t('chatApplied') : t('chatPending'))
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : String(error)
      setChatText(t('chatFailed', { message }))
    } finally {
      setChatBusy(false)
    }
  }

  return <>
    <button
      type="button"
      className={css.editButton}
      title={t('editMode')}
      onClick={() => { setOpen(true) }}
    >
      {t('editMode')}
    </button>
    {open && createPortal(
      <div
        ref={dialogRef}
        className={css.overlay}
        role="dialog"
        aria-modal="true"
        aria-label={t('editorTitle', { name })}
        onMouseDown={(event) => { if (event.target === dialogRef.current) setOpen(false) }}
      >
        <div className={css.modal} data-wordinweb-editor-modal>
          <header className={css.header}>
            <span className={css.title} title={name}>{t('editorTitle', { name })}</span>
            <span className={css.spacer} />
            {status !== undefined && <span className={css.status} role="status">{status}</span>}
            {savable && (
              <button type="button" className={css.primaryButton} onClick={save} disabled={saving || api === null}>
                {saving ? t('saving') : t('save')}
              </button>
            )}
            <button type="button" className={css.footerButton} onClick={download} disabled={api === null}>
              {t('download')}
            </button>
            {chattable && (
              <button type="button" className={css.footerButton} onClick={() => { setChatOpen(value => !value) }}>
                {t('chatToggle')}
              </button>
            )}
            <button type="button" className={css.footerButton} onClick={() => { setOpen(false) }}>
              {t('close')}
            </button>
          </header>
          <div className={css.workspace}>
            <div className={css.editor}>
              {api !== null && <DocxToolbar api={api} mode="advanced" onSave={download} />}
              <DocxView
                key={`doc-${docSeq}`}
                source={data}
                editable
                commentAuthor="DSH"
                fitWidth
                zoom={1}
                onReady={setApi}
                style={{ height: '100%' }}
              />
            </div>
            {chattable && chatOpen && (
              <div className={css.float} data-wordinweb-chat>
                <div className={css.floatHeader}>
                  <span>{t('chatTitle')}</span>
                  {editSession !== undefined && (
                    <button type="button" className={css.floatLink} onClick={() => { bridge.openSession?.(editSession) }}>
                      {t('chatOpenSession')}
                    </button>
                  )}
                  <span className={css.spacer} />
                  <button type="button" className={css.floatLink} onClick={() => { setChatOpen(false) }}>×</button>
                </div>
                <div className={css.floatBody}>{chatText ?? t('chatHint')}</div>
                <div className={css.chatInputRow}>
                  <textarea
                    className={css.chatInput}
                    value={chatInput}
                    placeholder={t('chatPlaceholder')}
                    rows={2}
                    disabled={chatBusy}
                    onChange={(event) => { setChatInput(event.target.value) }}
                    onKeyDown={(event) => {
                      if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
                        event.preventDefault()
                        void sendChat()
                      }
                    }}
                  />
                  <button type="button" className={css.primaryButton} onClick={() => { void sendChat() }} disabled={chatBusy || chatInput.trim() === ''}>
                    {t('chatSend')}
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>,
      document.body,
    )}
  </>
}

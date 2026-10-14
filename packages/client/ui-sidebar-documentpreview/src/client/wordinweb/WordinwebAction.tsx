/** Toolbar action opening the full WordInWeb editor as its own right-Sidebar tab. */
import { useState, type ReactNode } from 'react'
import type { PropsLocale, PropsRuntime } from '@deepseek-ai/dsh-client-ui-slots'
import { parseFileAddress } from '@deepseek-ai/dsh-util-workspace-path'
import type { SessionId } from '@deepseek-ai/dsh-session/types'
import { editAddressFor } from './edit-address.ts'
import { REPORT_TEMPLATES_DIR } from '../report/definition.ts'
import { reportBridge } from '../report/report-bridge.ts'
import type {} from './locales.ts'
import css from './WordinwebAction.module.css'

/** WordInWeb toolbar action props: preview content and localized copy. */
export type WordinwebActionProps = PropsRuntime<'sidebar.right.tab.document.action'>
  & PropsLocale<'sidebarWordinweb'>

/** Chunked base64 of complete preview bytes, `btoa`-safe beyond Latin-1. */
function base64OfBytes(bytes: Uint8Array): string {
  let binary = ''
  const chunk = 0x8000
  for (let offset = 0; offset < bytes.length; offset += chunk) {
    binary += String.fromCharCode(...bytes.subarray(offset, offset + chunk))
  }
  return btoa(binary)
}

/**
 * Offer the advanced WordInWeb ribbon editor for the previewed `.docx`, and
 * one-click adoption of it as a report template: the complete bytes copy into
 * `报告模版/` under their own basename. Editing is a tab of its own rather
 * than a dialog over the preview, so the document keeps the Sidebar's ordinary
 * tab affordances — title, close, split, fullscreen — and the AI edit Session
 * is reached through its own conversation. The save outcome stays on the
 * button itself — saved, or the failure message — because the toolbar has no
 * status surface of its own.
 * @param props - toolbar content and the enclosing tab's reader.
 * @returns the edit and save-as-template buttons.
 */
export function WordinwebAction({ content, useTabInfo, t }: WordinwebActionProps): ReactNode {
  const { tab } = useTabInfo()
  const [saveState, setSaveState] = useState<'idle' | 'saving' | 'saved' | 'failed'>('idle')
  const [saveFailure, setSaveFailure] = useState<string>()
  if (content.kind !== 'bytes') return null

  const saveAsTemplate = (): void => {
    const write = reportBridge.write
    const target = parseFileAddress(tab.contentId)
    if (write === undefined || target === undefined || target.scope !== 'session') {
      setSaveState('failed')
      setSaveFailure(t('saveAs.failed', { message: 'remote unavailable' }))
      return
    }
    const sessionId = target.sessionId as SessionId
    const name = target.path.replace(/^.*[/\\]/u, '') || '模版.docx'
    setSaveState('saving')
    setSaveFailure(undefined)
    void write(sessionId, `${REPORT_TEMPLATES_DIR}/${name}`, base64OfBytes(content.data), tab.signal)
      .then(() => { setSaveState('saved') }, (cause: unknown) => {
        setSaveState('failed')
        setSaveFailure(cause instanceof Error ? cause.message : String(cause))
      })
  }

  return (
    <>
      <button
        type="button"
        className={saveState === 'failed' ? css.saveFailed : css.save}
        title={saveState === 'failed' && saveFailure !== undefined
          ? t('saveAs.failed', { message: saveFailure })
          : t('saveAs.title')}
        disabled={saveState === 'saving' || saveState === 'saved'}
        onClick={saveAsTemplate}
      >
        {saveState === 'saving' ? t('saveAs.saving') : saveState === 'saved' ? t('saveAs.saved') : t('saveAs.title')}
      </button>
      <button
        type="button"
        className={css.editButton}
        title={t('editMode')}
        onClick={() => { tab.actions.openResource(editAddressFor(tab.contentId)) }}
      >
        {t('editMode')}
      </button>
    </>
  )
}

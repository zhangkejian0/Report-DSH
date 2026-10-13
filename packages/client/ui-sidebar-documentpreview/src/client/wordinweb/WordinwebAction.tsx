/** Toolbar action opening the full WordInWeb editor as its own right-Sidebar tab. */
import type { ReactNode } from 'react'
import type { PropsLocale, PropsRuntime } from '@deepseek-ai/dsh-client-ui-slots'
import { editAddressFor } from './edit-address.ts'
import type {} from './locales.ts'
import css from './WordinwebAction.module.css'

/** WordInWeb toolbar action props: preview content and localized copy. */
export type WordinwebActionProps = PropsRuntime<'sidebar.right.tab.document.action'>
  & PropsLocale<'sidebarWordinweb'>

/**
 * Offer the advanced WordInWeb ribbon editor for the previewed `.docx`.
 * Editing is a tab of its own rather than a dialog over the preview, so the
 * document keeps the Sidebar's ordinary tab affordances — title, close, split,
 * fullscreen — and the AI edit Session is reached through its own conversation.
 * @param props - toolbar content and the enclosing tab's reader.
 * @returns the horizontal edit button.
 */
export function WordinwebAction({ content, useTabInfo, t }: WordinwebActionProps): ReactNode {
  const { tab } = useTabInfo()
  if (content.kind !== 'bytes') return null
  return (
    <button
      type="button"
      className={css.editButton}
      title={t('editMode')}
      onClick={() => { tab.actions.openResource(editAddressFor(tab.contentId)) }}
    >
      {t('editMode')}
    </button>
  )
}

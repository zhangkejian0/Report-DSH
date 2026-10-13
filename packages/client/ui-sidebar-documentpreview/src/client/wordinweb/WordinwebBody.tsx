/** Complete .docx bytes rendered by the WordInWeb high-fidelity viewer. */
import { useState, type ReactNode } from 'react'
import type { PropsLocale } from '@deepseek-ai/dsh-client-ui-slots'
import { DocxView } from 'wordinweb'
import type { DocumentPreviewProps } from '../document/contract.ts'
import { LoadingIndicator } from '../LoadingIndicator.tsx'
import type {} from './locales.ts'
import css from './WordinwebBody.module.css'

/** Standard document props plus the WordInWeb renderer's dictionary. */
export type WordinwebBodyProps = DocumentPreviewProps & PropsLocale<'sidebarWordinweb'>

/**
 * Present complete OOXML bytes as paginated Word pages. Editing opens from
 * the toolbar action as a full modal editor.
 * @param props - document bytes, resource identity, and locale.
 * @returns the WordInWeb viewer fitted to the preview pane.
 */
export function WordinwebBody(props: WordinwebBodyProps): ReactNode {
  const { content, t } = props
  const data = content.kind === 'bytes' ? content.data : undefined
  const [loaded, setLoaded] = useState(false)
  const [failure, setFailure] = useState<string>()

  if (data === undefined) {
    return <p className={css.status} role="alert">{t('unsupported')}</p>
  }
  if (failure !== undefined) {
    return <p className={css.status} role="alert">{t('failed', { message: failure })}</p>
  }
  return (
    <div className={css.frame} data-wordinweb-preview>
      {!loaded && (
        <div className={css.loading}>
          <LoadingIndicator label={t('loading')} />
        </div>
      )}
      <DocxView
        source={data}
        fitWidth
        zoom={1}
        onLoad={() => { setLoaded(true) }}
        onError={(error: Error) => { setFailure(error.message) }}
        style={{ height: '100%' }}
      />
    </div>
  )
}

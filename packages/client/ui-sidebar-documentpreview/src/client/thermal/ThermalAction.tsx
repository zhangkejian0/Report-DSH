/** Toolbar action opening one image's thermal analysis as its own right-Sidebar tab. */
import type { ReactNode } from 'react'
import type { PropsLocale, PropsRuntime } from '@deepseek-ai/dsh-client-ui-slots'
import { analysisAddressFor, isRadiometricJpeg } from './thermal-address.ts'
import type {} from './locales.ts'
import css from './ThermalAction.module.css'

/** Thermal toolbar action props: preview content and localized copy. */
export type ThermalActionProps = PropsRuntime<'sidebar.right.tab.document.action'>
  & PropsLocale<'sidebarThermal'>

/**
 * Offer thermal-image analysis for the previewed image. Only radiometric JPEGs
 * — files whose payload continues well past the final JPEG EOI marker — carry
 * the analyzer's input, so ordinary photos show no action at all.
 * @param props - toolbar content and the enclosing tab's reader.
 * @returns the analysis button, or null for plain images and text previews.
 */
export function ThermalAction({ content, useTabInfo, t }: ThermalActionProps): ReactNode {
  const { tab } = useTabInfo()
  if (content.kind !== 'bytes' || !isRadiometricJpeg(content.data)) return null
  return (
    <button
      type="button"
      className={css.analyzeButton}
      title={t('title')}
      onClick={() => { tab.actions.openResource(analysisAddressFor(tab.contentId)) }}
    >
      {t('analyze')}
    </button>
  )
}

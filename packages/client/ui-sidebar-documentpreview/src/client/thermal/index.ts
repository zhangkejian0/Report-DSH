/** Thermal analysis: an image-preview action and its own analysis tab. */
import type { Context } from '@deepseek-ai/cordis'
import type { SidebarRightTabDefinition } from '@deepseek-ai/dsh-client-ui-sidebar-right/client'
import type {} from '@deepseek-ai/dsh-thermal-analyze/remote'
import { basenameOf } from '../definition.ts'
import { IMAGE_BODY_ID } from '../image/index.ts'
import { ThermalAction } from './ThermalAction.tsx'
import { ThermalAnalysisTab } from './ThermalAnalysisTab.tsx'
import { analysisAddressFor, parseAnalysisAddress, THERMAL_ANALYSIS_ADDRESS } from './thermal-address.ts'
import { analyzeBridge } from './analyze-bridge.ts'
import { en, zh } from './locales.ts'

/** Tab-type identity for the analysis tab opened from an image preview. */
export const THERMAL_ANALYSIS_TAB_ID = '@deepseek-ai/dsh-client-ui-sidebar-documentpreview/thermal-analysis'

export { THERMAL_ANALYSIS_ADDRESS, analysisAddressFor, parseAnalysisAddress }

/**
 * Claim the thermal-analysis address space as a tab type of its own; one tab
 * per image, because comparing several analyses side by side is the point.
 * @returns the definition registering the analysis view as a Sidebar tab.
 */
export function thermalAnalysisDefinition(): SidebarRightTabDefinition {
  return {
    id: THERMAL_ANALYSIS_TAB_ID,
    kind: 'thermal-analysis',
    patterns: [`${THERMAL_ANALYSIS_ADDRESS}**`],
    multiple: true,
    priority: 'extension',
    canOpen: address => parseAnalysisAddress(address) !== undefined,
    title: address => basenameOf(parseAnalysisAddress(address) ?? '') || 'image.jpg',
  }
}

/**
 * Register the thermal dictionary, the image-header analysis action, the
 * analysis tab type, the tab body, and the Host Remote seam behind them.
 * @param ctx - owning plugin context.
 */
export function apply(ctx: Context): void {
  ctx.effect(() => ctx.locale.register('sidebarThermal', { zh, en }), 'document-thermal: dictionaries')
  ctx.effect(() => ctx.sidebarRightTabs.register(thermalAnalysisDefinition()), 'document-thermal: tab type')
  ctx.effect(() => ctx.slots.inject('sidebar.right.tab.document.action', () => ctx.slots.register({
    name: 'sidebar.right.tab.document.action', key: IMAGE_BODY_ID, locale: 'sidebarThermal',
  }, ThermalAction)), 'document-thermal: action')
  ctx.effect(() => ctx.slots.inject('sidebar.right.pane.tab', () => ctx.slots.register({
    name: 'sidebar.right.pane.tab', key: THERMAL_ANALYSIS_TAB_ID, locale: 'sidebarThermal',
  }, ThermalAnalysisTab)), 'document-thermal: tab')
  ctx.inject(['remote', 'remote.thermalAnalyze'], (scope) => {
    analyzeBridge.analyze = async (sessionId, path, signal) => {
      const result = await scope.remote.thermalAnalyze.analyze(sessionId, path, signal)
      if (result.ok) return result.value
      throw Object.assign(new Error(result.error.message ?? 'thermal analysis failed'), result.error)
    }
    scope.effect(() => () => {
      analyzeBridge.analyze = undefined
    })
  })
}

/** Client-side seam to the Host `thermalAnalyze` Remote, owned by the plugin lifetime. */
import type { SessionId } from '@deepseek-ai/dsh-session/types'
import type { ThermalAnalysis } from '@deepseek-ai/dsh-thermal-analyze/types'

/**
 * Run one authorized analysis when the Host service is mounted; undefined while
 * the plugin runs without it, so the tab can show its unavailable notice.
 */
export const analyzeBridge: {
  analyze?: ((sessionId: SessionId, path: string, signal: AbortSignal) => Promise<ThermalAnalysis>) | undefined
} = {}

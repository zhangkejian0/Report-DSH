/** The address scheme distinguishing one image's analysis tab from its preview tab. */
import { parseFileAddress } from '@deepseek-ai/dsh-util-workspace-path'
import type { SessionId } from '@deepseek-ai/dsh-session/types'

/** Resource-address prefix for a thermal-analysis tab. */
export const THERMAL_ANALYSIS_ADDRESS = 'dsh-resource://thermal-analysis/'

/**
 * Encode one file address as the analysis tab's own resource address.
 * @param fileAddress - the previewed image's `dsh-resource://file/...` address.
 * @returns the address that claims the thermal-analysis tab type.
 */
export function analysisAddressFor(fileAddress: string): string {
  return `${THERMAL_ANALYSIS_ADDRESS}${encodeURIComponent(fileAddress)}`
}

/**
 * Decode an analysis tab address back to the file address it was opened from.
 * @param value - possible thermal-analysis address.
 * @returns the encoded file address, or undefined for another or malformed address.
 */
export function parseAnalysisAddress(value: string): string | undefined {
  if (!value.startsWith(THERMAL_ANALYSIS_ADDRESS)) return undefined
  try {
    const fileAddress = decodeURIComponent(value.slice(THERMAL_ANALYSIS_ADDRESS.length))
    const parsed = parseFileAddress(fileAddress)
    const lowered = fileAddress.toLowerCase()
    const image = lowered.endsWith('.jpg') || lowered.endsWith('.jpeg')
    return parsed?.scope === 'session' && image ? fileAddress : undefined
  } catch (_invalidEncoding) {
    return undefined
  }
}

/** One analyzable image located through its tab address. */
export interface AnalysisTarget {
  /** Session owning the Workspace the image lives in. */
  readonly sessionId: SessionId
  /** Workspace-relative image path. */
  readonly path: string
}

/**
 * Resolve the image one analysis tab analyzes.
 * @param value - the tab's content address.
 * @returns the owning Session and image path, or undefined when the address names neither.
 */
export function analysisTargetOf(value: string): AnalysisTarget | undefined {
  const parsed = parseFileAddress(parseAnalysisAddress(value) ?? '')
  return parsed?.scope === 'session' ? { sessionId: parsed.sessionId as SessionId, path: parsed.path } : undefined
}

/**
 * Whether loaded image bytes plausibly carry a radiometric payload worth analyzing.
 * Plain photos end at their JPEG EOI; Fotric/Fluke-style thermal files append a
 * large proprietary container after it.
 * @param bytes - complete image file bytes.
 * @returns true when at least 16 KiB follows the final JPEG EOI marker.
 */
export function isRadiometricJpeg(bytes: Uint8Array): boolean {
  if (bytes.length < 4 || bytes[0] !== 0xFF || bytes[1] !== 0xD8) return false
  for (let i = bytes.length - 2; i >= 1; i--) {
    if (bytes[i] === 0xFF && bytes[i + 1] === 0xD9) return bytes.length - 2 - i >= 16_384
  }
  return false
}

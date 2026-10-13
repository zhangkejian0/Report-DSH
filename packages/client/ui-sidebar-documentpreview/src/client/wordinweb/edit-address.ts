/** The address scheme distinguishing one `.docx`'s edit tab from its preview tab. */
import { parseFileAddress } from '@deepseek-ai/dsh-util-workspace-path'
import type { SessionId } from '@deepseek-ai/dsh-session/types'

/** Resource-address prefix for a WordInWeb edit tab. */
export const WORDINWEB_EDIT_ADDRESS = 'dsh-resource://wordinweb-edit/'

/**
 * Encode one file address as the edit tab's own resource address.
 * @param fileAddress - the previewed file's `dsh-resource://file/...` address.
 * @returns the address that claims the WordInWeb edit tab type.
 */
export function editAddressFor(fileAddress: string): string {
  return `${WORDINWEB_EDIT_ADDRESS}${encodeURIComponent(fileAddress)}`
}

/**
 * Decode an edit tab address back to the file address it was opened from.
 * @param value - possible WordInWeb edit address.
 * @returns the encoded file address, or undefined for another or malformed address.
 */
export function parseEditAddress(value: string): string | undefined {
  if (!value.startsWith(WORDINWEB_EDIT_ADDRESS)) return undefined
  try {
    const fileAddress = decodeURIComponent(value.slice(WORDINWEB_EDIT_ADDRESS.length))
    const parsed = parseFileAddress(fileAddress)
    return parsed?.scope === 'session' && fileAddress.toLowerCase().endsWith('.docx') ? fileAddress : undefined
  } catch (_invalidEncoding) {
    return undefined
  }
}

/** One editable document located through its tab address. */
export interface EditTarget {
  /** Session owning the Workspace the file lives in. */
  readonly sessionId: SessionId
  /** Workspace-relative file path. */
  readonly path: string
}

/**
 * Resolve the document one edit tab shows.
 * @param value - the tab's content address.
 * @returns the owning Session and file path, or undefined when the address names neither.
 */
export function editTargetOf(value: string): EditTarget | undefined {
  const parsed = parseFileAddress(parseEditAddress(value) ?? '')
  return parsed?.scope === 'session' ? { sessionId: parsed.sessionId as SessionId, path: parsed.path } : undefined
}

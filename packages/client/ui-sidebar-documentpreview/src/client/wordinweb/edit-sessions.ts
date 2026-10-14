/** Durable record of which document one auto-created edit Session edits. */
import type { SessionId } from '@deepseek-ai/dsh-session/types'

/** Persistence namespace for one edit record per Session, mirroring the Sidebar's own. */
const EDIT_SESSION_PERSISTENCE = 'dsh.wordinweb-edit.v1'

/** One recorded edit Session: the absolute host path of the document it edits, and whether its tab was seeded. */
interface EditSessionRecord {
  readonly path: string
  readonly seeded: boolean
}

/** Storage without localStorage (worker, SSR): records live for the window only. */
const memoryRecords = new Map<string, EditSessionRecord>()

/** Window-lifetime document → edit Session reuse, so one document keeps one Session per window. */
const byDocument = new Map<string, SessionId>()

/**
 * Normalize Windows separators, because resource addresses decode to `/`-separated
 * paths while a Host stat may report `\`; recorded and decoded paths must compare.
 * @param path - absolute host path in either separator style.
 * @returns the same path with `/` separators.
 */
function normalizePath(path: string): string {
  return path.replace(/\\/g, '/')
}

/** Read one Session's record, or undefined when the Session edits nothing recorded. */
function readRecord(sessionId: SessionId): EditSessionRecord | undefined {
  try {
    if (typeof localStorage === 'undefined') return memoryRecords.get(sessionId)
    const raw = localStorage.getItem(`${EDIT_SESSION_PERSISTENCE}.${sessionId}`)
    if (raw === null) return undefined
    const parsed: unknown = JSON.parse(raw)
    if (typeof parsed !== 'object' || parsed === null) return undefined
    const { path, seeded } = parsed as { path?: unknown; seeded?: unknown }
    if (typeof path !== 'string' || path === '') return undefined
    return { path: normalizePath(path), seeded: seeded === true }
  } catch (_invalidRecord) {
    return undefined
  }
}

/** Write one Session's record, keeping the window fallback in step. */
function writeRecord(sessionId: SessionId, record: EditSessionRecord): void {
  memoryRecords.set(sessionId, record)
  if (typeof localStorage === 'undefined') return
  try {
    localStorage.setItem(`${EDIT_SESSION_PERSISTENCE}.${sessionId}`, JSON.stringify(record))
  } catch (_storageUnavailable) { /* The in-memory record still serves this window. */ }
}

/**
 * Record that one Session edits one document, and that its edit tab is unseeded.
 * @param sessionId - the Session `WordinwebEditTab` created for the document.
 * @param path - absolute host path of the document (either separator style),
 * resolving identically in every Workspace the Session and its document's
 * directory share; stored with `/` separators.
 */
export function recordEditSession(sessionId: SessionId, path: string): void {
  const normalized = normalizePath(path)
  writeRecord(sessionId, { path: normalized, seeded: false })
  byDocument.set(normalized, sessionId)
}

/**
 * Find this window's edit Session for one document, so re-entering edit reuses
 * it instead of creating another Session for the same file.
 * @param documentPath - absolute host path of the document (either separator style).
 * @returns the Session this window already created for it, or undefined.
 */
export function editSessionFor(documentPath: string): SessionId | undefined {
  return byDocument.get(normalizePath(documentPath))
}

/**
 * Read the document one Session edits, independent of tab seeding.
 * @param sessionId - possible edit Session.
 * @returns its recorded absolute document path, or undefined for any other Session.
 */
export function editSessionPathOf(sessionId: SessionId): string | undefined {
  return readRecord(sessionId)?.path
}

/**
 * Take the document whose edit tab this Session still owes, without claiming it.
 * @param sessionId - possible edit Session.
 * @returns the absolute document path whose tab was never seeded, or undefined.
 */
export function unseededEditSessionPath(sessionId: SessionId): string | undefined {
  const record = readRecord(sessionId)
  return record === undefined || record.seeded ? undefined : record.path
}

/**
 * Mark one Session's edit tab seeded, ending reseed attempts; closing the tab then stays closed.
 * @param sessionId - the edit Session whose tab reached its surface.
 */
export function markEditSessionSeeded(sessionId: SessionId): void {
  const record = readRecord(sessionId)
  if (record !== undefined && !record.seeded) writeRecord(sessionId, { ...record, seeded: true })
}

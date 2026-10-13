/** Client-side bridge to the WordInWeb Remote extensions of `workspaceFiles`. */
import type { SessionId } from '@deepseek-ai/dsh-session/types'

/** One authorized document save over the Remote. */
export type SaveFile =
  (sessionId: SessionId, path: string, base64: string, signal: AbortSignal) => Promise<unknown>

/** Create one edit session bound to the document's directory. */
export type CreateSession = (cwd: string, signal: AbortSignal) => Promise<SessionId>

/** Open one session's native conversation in the workspace UI. */
export type OpenSession = (sessionId: SessionId) => void

/** One authorized byte read for post-edit refresh. */
export type ReadFile =
  (sessionId: SessionId, path: string, signal: AbortSignal) => Promise<Uint8Array>

/** Resolve one workspace file's absolute host path. */
export type StatFile =
  (sessionId: SessionId, path: string, signal: AbortSignal) => Promise<string>

/**
 * The Remote closures: set by the plugin while the connection is up, cleared
 * on dispose. Components read them because the slot system offers no
 * plugin-owned prop channel for store-less keyed registrations.
 */
export const bridge: {
  save: SaveFile | undefined
  create: CreateSession | undefined
  stat: StatFile | undefined
  openSession: OpenSession | undefined
  read: ReadFile | undefined
} = {
  save: undefined,
  create: undefined,
  stat: undefined,
  openSession: undefined,
  read: undefined,
}

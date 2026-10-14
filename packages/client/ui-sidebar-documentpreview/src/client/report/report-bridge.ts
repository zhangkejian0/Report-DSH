/** Client-side bridge to the Remotes and services the report wizard drives. */
import type { SessionId } from '@deepseek-ai/dsh-session/types'
import type { WorkspaceDirectoryEntry } from '@deepseek-ai/dsh-api-workspace-files/types'

/**
 * List one workspace directory, throwing its failure message; the wizard keeps
 * no partial listings.
 */
export type ListDirectory =
  (sessionId: SessionId, path: string, signal: AbortSignal) => Promise<readonly WorkspaceDirectoryEntry[]>

/** Write complete bytes to one workspace file, creating missing parents. */
export type WriteFile =
  (sessionId: SessionId, path: string, base64: string, signal: AbortSignal) => Promise<string>

/** Read one workspace file's complete bytes, for thumbnail previews. */
export type ReadFile =
  (sessionId: SessionId, path: string, signal: AbortSignal) => Promise<Uint8Array>

/** Ask the right Sidebar to present the pane holding one element fullscreen. */
export type RequestFullscreen = (element: HTMLElement) => void

/**
 * The Remote closures: set by the plugin while the connection is up, cleared
 * on dispose. Components read them because the slot system offers no
 * plugin-owned prop channel for store-less keyed registrations.
 */
export const reportBridge: {
  list: ListDirectory | undefined
  write: WriteFile | undefined
  read: ReadFile | undefined
  requestFullscreen: RequestFullscreen | undefined
} = {
  list: undefined,
  write: undefined,
  read: undefined,
  requestFullscreen: undefined,
}

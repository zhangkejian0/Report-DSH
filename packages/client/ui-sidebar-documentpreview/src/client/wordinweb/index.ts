/** WordInWeb metadata, keyed document body, its own edit tab, and the edit Session's tab seed. */
import type { Context } from '@deepseek-ai/cordis'
import type { SessionRequestId } from '@deepseek-ai/dsh-api-remotes/client'
import type { WorkspaceId, WorkspaceView } from '@deepseek-ai/dsh-api-workspace-controller/client'
import type { SidebarRightTabDefinition } from '@deepseek-ai/dsh-client-ui-sidebar-right/client'
import type {} from '@deepseek-ai/dsh-client-ui-workspace/client'
import type { SessionId } from '@deepseek-ai/dsh-session/types'
import { sessionFileAddress } from '@deepseek-ai/dsh-util-workspace-path'
import type {} from '../index.ts'
import type { DocumentPreviewDefinition } from '../document/registry.ts'
import { basenameOf } from '../definition.ts'
import { WordinwebAction } from './WordinwebAction.tsx'
import { WordinwebBody } from './WordinwebBody.tsx'
import { WordinwebEditTab } from './WordinwebEditTab.tsx'
import { WordinwebEditSessionSeed } from './WordinwebEditSessionSeed.tsx'
import { WORDINWEB_EDIT_ADDRESS, editAddressFor, parseEditAddress } from './edit-address.ts'
import { markEditSessionSeeded, unseededEditSessionPath } from './edit-sessions.ts'
import { bridge } from './save-bridge.ts'
import { en, zh } from './locales.ts'

/** WordInWeb implementation identity, shared by metadata and the keyed body slot. */
export const WORDINWEB_BODY_ID = '@deepseek-ai/dsh-client-ui-sidebar-documentpreview/wordinweb'

/** Tab-type identity for the WordInWeb ribbon editor opened from a preview. */
export const WORDINWEB_EDIT_ID = '@deepseek-ai/dsh-client-ui-sidebar-documentpreview/wordinweb-edit'

/** File suffixes rendered by the WordInWeb OOXML viewer. */
export const WORDINWEB_EXTENSIONS = ['docx'] as const

/**
 * Describe the WordInWeb renderer independently from its keyed body slot.
 * @param title - locale-owned implementation name.
 * @returns metadata for complete .docx files.
 */
export function wordinwebBodyDefinition(title: () => string): DocumentPreviewDefinition {
  return {
    id: WORDINWEB_BODY_ID,
    extensions: WORDINWEB_EXTENSIONS,
    binaryExtensions: WORDINWEB_EXTENSIONS,
    priority: 'extension',
    title,
    loading: 'bytes-complete',
    wrap: false,
  }
}

/**
 * Claim the WordInWeb edit address space as a tab type of its own.
 * @returns the definition registering the ribbon editor as a Sidebar tab.
 */
export function wordinwebEditDefinition(): SidebarRightTabDefinition {
  return {
    id: WORDINWEB_EDIT_ID,
    kind: 'wordinweb-edit',
    patterns: [`${WORDINWEB_EDIT_ADDRESS}**`],
    priority: 'extension',
    canOpen: address => parseEditAddress(address) !== undefined,
    title: address => basenameOf(parseEditAddress(address) ?? '') || 'document.docx',
  }
}

/** Normalize a Windows or POSIX directory path so registered paths compare fairly. */
function normalizePath(path: string): string {
  return path.replace(/\\/g, '/').replace(/\/+$/u, '').toLowerCase()
}

/**
 * Find the Workspace that owns one directory.
 * @param items - registered Workspaces.
 * @param directory - absolute directory holding the document.
 * @returns the deepest matching Workspace, or undefined when none contains it.
 */
function owningWorkspaceId(items: readonly WorkspaceView[], directory: string): WorkspaceId | undefined {
  const target = normalizePath(directory)
  let best: WorkspaceView | undefined
  let bestLength = -1
  for (const item of items) {
    const path = normalizePath(item.path)
    if (target !== path && !target.startsWith(`${path}/`)) continue
    if (path.length > bestLength) {
      best = item
      bestLength = path.length
    }
  }
  return best?.workspaceId
}

/**
 * Register the WordInWeb dictionary, metadata, toolbar action, body, edit tab,
 * and the Session-header occupant that seeds an edit Session's own tab.
 * @param ctx - owning plugin context.
 */
export function apply(ctx: Context): void {
  const t = ctx.locale.bind('sidebarWordinweb')
  ctx.effect(() => ctx.locale.register('sidebarWordinweb', { zh, en }), 'document-wordinweb: dictionaries')
  ctx.effect(() => ctx.documentPreviews.register(wordinwebBodyDefinition(() => t('title'))), 'document-wordinweb: metadata')
  ctx.effect(() => ctx.sidebarRightTabs.register(wordinwebEditDefinition()), 'document-wordinweb: edit tab type')
  ctx.effect(() => ctx.slots.inject('sidebar.right.tab.document.action', () => ctx.slots.register({
    name: 'sidebar.right.tab.document.action', key: WORDINWEB_BODY_ID, locale: 'sidebarWordinweb',
  }, WordinwebAction)), 'document-wordinweb: action')
  ctx.effect(() => ctx.slots.inject('sidebar.right.tab.document', () => ctx.slots.register({
    name: 'sidebar.right.tab.document', key: WORDINWEB_BODY_ID, locale: 'sidebarWordinweb',
  }, WordinwebBody)), 'document-wordinweb: body')
  ctx.effect(() => ctx.slots.inject('sidebar.right.pane.tab', () => ctx.slots.register({
    name: 'sidebar.right.pane.tab', key: WORDINWEB_EDIT_ID, locale: 'sidebarWordinweb',
  }, WordinwebEditTab)), 'document-wordinweb: edit tab')
  ctx.inject(['remote', 'remote.workspaceFiles', 'remote.session', 'sessions', 'workspaces', 'sidebarRight', 'uiWorkspace'], (scope) => {
    const unwrap = <T>(result: { ok: true; value: T } | { ok: false; error: { message?: string } }): T => {
      if (result.ok) return result.value
      throw new Error(result.error.message ?? 'remote call failed')
    }
    bridge.save = async (sessionId, path, base64, signal) => {
      unwrap(await scope.remote.workspaceFiles.writeBytes(sessionId, path, base64, signal))
    }
    bridge.prompt = async (sessionId, text, signal) => {
      unwrap(await scope.remote.session.prompt({
        requestId: `wordinweb-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}` as SessionRequestId,
        sessionId,
        mode: 'queue',
        content: [{ type: 'text', text }],
      }, signal))
    }
    bridge.read = async (sessionId, path, signal) =>
      unwrap(await scope.remote.workspaceFiles.readBytes(sessionId, path, {}, signal)).data
    bridge.stat = async (sessionId, path, signal) =>
      unwrap(await scope.remote.workspaceFiles.stat(sessionId, path, signal)).absolutePath
    bridge.navigate = (sessionId) => { scope.uiWorkspace.openSession(sessionId) }
    bridge.create = async (cwd, signal) => {
      signal.throwIfAborted()
      // A `cwd` create publishes a Session the Host leaves Ungrouped; naming the
      // Workspace that owns the directory accounts for it where the user looks.
      const workspaceId = owningWorkspaceId(scope.workspaces.list.getSnapshot().items, cwd)
      return workspaceId === undefined
        ? scope.sessions.create({ cwd })
        : scope.sessions.create({ workspaceId })
    }
    scope.effect(() => ctx.slots.inject('conversation.session.header.utilities', () => ctx.slots.register({
      name: 'conversation.session.header.utilities',
      id: 'wordinweb-edit-session',
      order: 20,
      locale: 'sidebarWordinweb',
      inject: (sessionId: SessionId) => ({
        hooks: { sidebarMounted: scope.sidebarRight.mounted },
        seedEditTab: () => seedEditTab(sessionId),
      }),
    }, WordinwebEditSessionSeed)), 'document-wordinweb: edit session seed')
    /**
     * Place the recorded document's edit tab in one edit Session's own surface.
     * @param sessionId - the edit Session whose header occupant asks.
     * @returns whether the owed tab is present after the attempt; false when
     * none is owed or the surface did not adopt it.
     */
    function seedEditTab(sessionId: SessionId): boolean {
      const documentPath = unseededEditSessionPath(sessionId)
      if (documentPath === undefined) return false
      const fileAddress = sessionFileAddress(sessionId, documentPath)
      const present = (): boolean =>
        scope.sidebarRight.tabsIn(sessionId).some(tab => parseEditAddress(tab.contentId) === fileAddress)
      if (!present()) scope.sidebarRight.openResourceIn(sessionId, editAddressFor(fileAddress))
      // Seeding claims its goal only once the surface really holds the tab, so
      // a surface that was never adopted retries on the Session's next mount.
      if (present()) markEditSessionSeeded(sessionId)
      return present()
    }
    scope.effect(() => () => {
      bridge.save = undefined
      bridge.prompt = undefined
      bridge.create = undefined
      bridge.stat = undefined
      bridge.read = undefined
      bridge.navigate = undefined
    })
  })
}

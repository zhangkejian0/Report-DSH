/** WordInWeb metadata, keyed document body, and its own edit tab. */
import type { Context } from '@deepseek-ai/cordis'
import type { SidebarRightTabDefinition } from '@deepseek-ai/dsh-client-ui-sidebar-right/client'
import type {} from '@deepseek-ai/dsh-client-ui-workspace/client'
import type {} from '../index.ts'
import type { DocumentPreviewDefinition } from '../document/registry.ts'
import { basenameOf } from '../definition.ts'
import { WordinwebAction } from './WordinwebAction.tsx'
import { WordinwebBody } from './WordinwebBody.tsx'
import { WordinwebEditTab } from './WordinwebEditTab.tsx'
import { WORDINWEB_EDIT_ADDRESS, parseEditAddress } from './edit-address.ts'
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

/**
 * Register the WordInWeb dictionary, metadata, toolbar action, body, and edit tab.
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
  ctx.inject(['remote', 'remote.workspaceFiles', 'sessions', 'uiWorkspace'], (scope) => {
    const unwrap = <T>(result: { ok: true; value: T } | { ok: false; error: { message?: string } }): T => {
      if (result.ok) return result.value
      throw new Error(result.error.message ?? 'remote call failed')
    }
    bridge.save = async (sessionId, path, base64, signal) => {
      unwrap(await scope.remote.workspaceFiles.writeBytes(sessionId, path, base64, signal))
    }
    bridge.read = async (sessionId, path, signal) =>
      unwrap(await scope.remote.workspaceFiles.readBytes(sessionId, path, {}, signal)).data
    bridge.stat = async (sessionId, path, signal) =>
      unwrap(await scope.remote.workspaceFiles.stat(sessionId, path, signal)).absolutePath
    bridge.create = async (cwd, signal) => {
      signal.throwIfAborted()
      const sessionId = await scope.sessions.create({ cwd })
      // Edit Sessions are an implementation detail of the editor tab; archiving
      // keeps them out of the Workspace list while 「查看对话」 still opens them.
      scope.uiWorkspace.archiveSession(sessionId).catch(() => { /* cosmetic: a failure leaves the session listed */ })
      return sessionId
    }
    bridge.openSession = (sessionId) => { ctx.uiWorkspace.openSession(sessionId) }
    scope.effect(() => () => {
      bridge.save = undefined
      bridge.create = undefined
      bridge.stat = undefined
      bridge.openSession = undefined
      bridge.read = undefined
    })
  })
}

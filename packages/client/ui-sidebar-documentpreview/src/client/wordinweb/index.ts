/** WordInWeb metadata and keyed document-body registration. */
import type { Context } from '@deepseek-ai/cordis'
import type {} from '../index.ts'
import type { DocumentPreviewDefinition } from '../document/registry.ts'
import { WordinwebAction } from './WordinwebAction.tsx'
import { WordinwebBody } from './WordinwebBody.tsx'
import { bridge } from './save-bridge.ts'
import { en, zh } from './locales.ts'

/** WordInWeb implementation identity, shared by metadata and the keyed slot. */
export const WORDINWEB_BODY_ID = '@deepseek-ai/dsh-client-ui-sidebar-documentpreview/wordinweb'

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
 * Register the WordInWeb dictionary, metadata, toolbar action, and body.
 * @param ctx - owning plugin context.
 */
export function apply(ctx: Context): void {
  const t = ctx.locale.bind('sidebarWordinweb')
  ctx.effect(() => ctx.locale.register('sidebarWordinweb', { zh, en }), 'document-wordinweb: dictionaries')
  ctx.effect(() => ctx.documentPreviews.register(wordinwebBodyDefinition(() => t('title'))), 'document-wordinweb: metadata')
  ctx.effect(() => ctx.slots.inject('sidebar.right.tab.document.action', () => ctx.slots.register({
    name: 'sidebar.right.tab.document.action', key: WORDINWEB_BODY_ID, locale: 'sidebarWordinweb',
  }, WordinwebAction)), 'document-wordinweb: action')
  ctx.effect(() => ctx.slots.inject('sidebar.right.tab.document', () => ctx.slots.register({
    name: 'sidebar.right.tab.document', key: WORDINWEB_BODY_ID, locale: 'sidebarWordinweb',
  }, WordinwebBody)), 'document-wordinweb: body')
  ctx.inject(['remote', 'remote.workspaceFiles', 'remote.session'], (scope) => {
    const unwrap = <T>(result: { ok: true; value: T } | { ok: false; error: { message?: string } }): T => {
      if (result.ok) return result.value
      throw new Error(result.error.message ?? 'remote call failed')
    }
    bridge.save = async (sessionId, path, base64, signal) => {
      unwrap(await scope.remote.workspaceFiles.writeBytes(sessionId, path, base64, signal))
    }
    bridge.prompt = async (sessionId, text, signal) => {
      unwrap(await scope.remote.session.prompt({
        requestId: `wordinweb-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`,
        sessionId,
        mode: 'queue',
        content: [{ type: 'text', text }],
      } as never, signal))
    }
    bridge.read = async (sessionId, path, signal) =>
      unwrap(await scope.remote.workspaceFiles.readBytes(sessionId, path, {}, signal)).data
    scope.effect(() => () => {
      bridge.save = undefined
      bridge.prompt = undefined
      bridge.read = undefined
    })
  })
}

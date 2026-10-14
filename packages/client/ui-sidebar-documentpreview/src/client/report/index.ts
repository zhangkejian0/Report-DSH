/** Report wizard and template library: tab types, bodies, and the generation service wiring. */
import type { Context } from '@deepseek-ai/cordis'
import type { SessionRequestId } from '@deepseek-ai/dsh-api-remotes/client'
import type {} from '@deepseek-ai/dsh-client-ui-sidebar-right/client'
import { sessionFileAddress } from '@deepseek-ai/dsh-util-workspace-path'
import { editAddressFor } from '../wordinweb/edit-address.ts'
import { editSessionFor, markEditSessionSeeded, recordEditSession } from '../wordinweb/edit-sessions.ts'
import { owningWorkspaceId } from '../wordinweb/index.ts'
import {
  REPORT_BUILDER_ID, REPORT_TEMPLATES_ID, reportBuilderDefinition, reportTemplatesDefinition,
} from './definition.ts'
import { ReportBuilderTab } from './ReportBuilderTab.tsx'
import { TemplateLibraryTab } from './TemplateLibraryTab.tsx'
import { reportBridge } from './report-bridge.ts'
import { generationBridge } from './generation-bridge.ts'
import type { GenerationDeps } from './generate.ts'
import { en, zh } from './locales.ts'

declare module '@deepseek-ai/dsh-client-ui-sidebar-right/client' {
  interface SidebarRightTabParamsMap {
    /** What a report-wizard open carries: a thermal handoff's image or a library handoff's template. */
    'report-builder': { readonly preselectImage?: string; readonly preselectTemplate?: string }
  }
}

/** Call-result unwrap shared by the wired Remote closures. */
function expectOk<T>(result: { ok: true; value: T } | { ok: false; error: { message?: string; code?: string } }): T {
  if (result.ok) return result.value
  throw Object.assign(new Error(result.error.message ?? 'remote call failed'), { code: result.error.code })
}

/**
 * Register the report dictionaries, the wizard and library tab types with
 * their start-page entries, the wizard and library bodies, the save-as-template
 * toolbar action, and the Remote closures the wizard and the generation run
 * drive: directory listing, byte reading and writing, session creation, task
 * dispatch, and the fullscreen request the wizard's manual control asks for.
 * @param ctx - owning plugin context.
 */
export function apply(ctx: Context): void {
  const t = ctx.locale.bind('sidebarReport')
  ctx.effect(() => ctx.locale.register('sidebarReport', { zh, en }), 'document-report: dictionaries')
  ctx.effect(() => ctx.sidebarRightTabs.register(reportBuilderDefinition(t)), 'document-report: tab type')
  ctx.effect(() => ctx.sidebarRightTabs.register(reportTemplatesDefinition(t)), 'document-report: library type')
  ctx.effect(() => ctx.slots.inject('sidebar.right.pane.tab', () => ctx.slots.register({
    name: 'sidebar.right.pane.tab', key: REPORT_BUILDER_ID, locale: 'sidebarReport',
  }, ReportBuilderTab)), 'document-report: wizard tab')
  ctx.effect(() => ctx.slots.inject('sidebar.right.pane.tab', () => ctx.slots.register({
    name: 'sidebar.right.pane.tab', key: REPORT_TEMPLATES_ID, locale: 'sidebarReport',
  }, TemplateLibraryTab)), 'document-report: library tab')
  ctx.inject(
    ['remote', 'remote.workspaceFiles', 'remote.session', 'sessions', 'workspaces', 'sidebarRight', 'uiWorkspace'],
    (scope) => {
      const write: GenerationDeps['write'] = async (sessionId, path, base64, signal) =>
        expectOk(await scope.remote.workspaceFiles.writeBytes(sessionId, path, base64, signal)).absolutePath
      reportBridge.list = async (sessionId, path, signal) =>
        expectOk(await scope.remote.workspaceFiles.list(sessionId, path, signal)).entries
      reportBridge.write = write
      reportBridge.read = async (sessionId, path, signal) =>
        expectOk(await scope.remote.workspaceFiles.readBytes(sessionId, path, {}, signal)).data
      reportBridge.requestFullscreen = (element) => {
        const target = scope.sidebarRight.commandTarget(element)
        if (target !== undefined) scope.sidebarRight.toggleFullscreen(target)
      }
      generationBridge.deps = {
        write,
        stat: async (sessionId, path, signal) =>
          expectOk(await scope.remote.workspaceFiles.stat(sessionId, path, signal)).absolutePath,
        prompt: async (sessionId, text, signal) => {
          await expectOk(await scope.remote.session.prompt({
            requestId: `report-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}` as SessionRequestId,
            sessionId,
            mode: 'queue',
            content: [{ type: 'text', text }],
          }, signal))
        },
        createSession: async (cwd, signal) => {
          signal.throwIfAborted()
          // A `cwd` create publishes a Session the Host leaves Ungrouped; naming
          // the Workspace that owns the directory accounts for it where the
          // user looks.
          const workspaceId = owningWorkspaceId(scope.workspaces.list.getSnapshot().items, cwd)
          return workspaceId === undefined
            ? scope.sessions.create({ cwd })
            : scope.sessions.create({ workspaceId })
        },
        navigate: (sessionId) => { scope.uiWorkspace.openSession(sessionId) },
        openEditTab: (sessionId, docxPath) => {
          scope.sidebarRight.openResourceIn(sessionId, editAddressFor(sessionFileAddress(sessionId, docxPath)))
        },
        recordEditSession,
        markSeeded: markEditSessionSeeded,
        editSessionForDocument: editSessionFor,
      } satisfies GenerationDeps
      scope.effect(() => () => {
        reportBridge.list = undefined
        reportBridge.write = undefined
        reportBridge.read = undefined
        reportBridge.requestFullscreen = undefined
        generationBridge.deps = undefined
      })
    },
  )
}

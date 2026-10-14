/**
 * The edit Session's tab seed and lifecycle: the durable Session↔document
 * record with separator normalization, the utilities occupant that waits for
 * the on-screen Session, the seed that places the document's edit tab in the
 * edit Session's own surface once, and the edit tab's create-reuse-navigate
 * hand-off.
 */
// @vitest-environment jsdom
import { afterEach, beforeEach, describe, expect, it, onTestFinished, vi } from 'vitest'
import { act, cleanup, render } from '@testing-library/react'
import { useSyncExternalStore, type ComponentProps } from 'react'
import { Context } from '@deepseek-ai/cordis'
import { createSnapshotStore } from '@deepseek-ai/dsh-client-store'
import { SidebarRightTabRegistry } from '@deepseek-ai/dsh-client-ui-sidebar-right/src/client/tab-registry.ts'
import type { SessionId } from '@deepseek-ai/dsh-session/types'
import { sessionFileAddress } from '@deepseek-ai/dsh-util-workspace-path'
import { apply, inject } from '../src/client/index.ts'
import { WordinwebEditSessionSeed } from '../src/client/wordinweb/WordinwebEditSessionSeed.tsx'
import type { WordinwebEditSessionSeedInjected } from '../src/client/wordinweb/WordinwebEditSessionSeed.tsx'
import { WordinwebEditTab } from '../src/client/wordinweb/WordinwebEditTab.tsx'
import { editAddressFor } from '../src/client/wordinweb/edit-address.ts'
import {
  editSessionFor, editSessionPathOf, markEditSessionSeeded, recordEditSession, unseededEditSessionPath,
} from '../src/client/wordinweb/edit-sessions.ts'
import { bridge } from '../src/client/wordinweb/save-bridge.ts'

vi.mock('wordinweb', () => ({
  DocxView: () => null,
  DocxToolbar: () => null,
}))

afterEach(cleanup)

/** One committed tab record as `sidebarRight.tabsIn` reports it. */
interface SurfaceTab { id: string; kind: string; contentId: string; title: string }

function tabRecord(contentId: string): SurfaceTab {
  return { id: 'tab1', kind: 'wordinweb-edit', contentId, title: 'Test.docx' }
}

/** Boot the plugin with the seed's injected services, over a recording Sidebar face. */
async function boot() {
  const ctx = new Context()
  ctx.provide('resources', {
    pin: vi.fn(), subscribe: () => () => {}, getSnapshot: () => new Map(),
  } as never)
  ctx.provide('configForms', { developerTools: { enabled: createSnapshotStore(true) } } as never)
  const registered: { name: string; id?: string; inject?: unknown; component: unknown }[] = []
  const slots = {
    inject: vi.fn((_name: string, register: () => () => void) => register()),
    register: vi.fn((options: { name: string; id?: string; inject?: unknown }, component: unknown) => {
      const entry = { ...options, component }
      registered.push(entry)
      return () => { registered.splice(registered.indexOf(entry), 1) }
    }),
  }
  ctx.provide('sidebarRightTabs', new SidebarRightTabRegistry(ctx) as never)
  ctx.provide('slots', slots as never)
  ctx.provide('locale', { bind: () => (key: string) => key, register: () => () => {} } as never)
  const read = vi.fn()
  const readBytes = vi.fn()
  ctx.provide('remote', { workspaceFiles: { read, readBytes } } as never)
  ctx.provide('remote.workspaceFiles', { read, readBytes } as never)
  ctx.provide('remote.session', { prompt: vi.fn(async () => ({ ok: true, value: undefined })) } as never)
  ctx.provide('sessions', { create: vi.fn(async () => 's-created' as SessionId) } as never)
  ctx.provide('workspaces', { list: createSnapshotStore({ items: [] }) } as never)
  ctx.provide('uiWorkspace', { openSession: vi.fn() } as never)
  const mounted = createSnapshotStore<SessionId | undefined>(undefined)
  const surfaces = new Map<SessionId, SurfaceTab[]>()
  const surfaceOf = (sessionId: SessionId): SurfaceTab[] => {
    const existing = surfaces.get(sessionId)
    if (existing !== undefined) return existing
    const created: SurfaceTab[] = []
    surfaces.set(sessionId, created)
    return created
  }
  const openResourceIn = vi.fn((sessionId: SessionId, address: string) => { surfaceOf(sessionId).push(tabRecord(address)) })
  ctx.provide('sidebarRight', {
    mounted,
    tabsIn: (sessionId: SessionId) => [...surfaceOf(sessionId)],
    openResourceIn,
  } as never)
  const fiber = ctx.plugin({ inject: [...inject], apply })
  onTestFinished(async () => { await fiber.dispose() })
  await fiber.await()
  const seat = registered.find(entry => entry.name === 'conversation.session.header.utilities')
  if (seat === undefined) throw new Error('wordinweb utilities occupant was not registered')
  const faceOf = (sessionId: SessionId): WordinwebEditSessionSeedInjected =>
    (seat.inject as (id: SessionId) => WordinwebEditSessionSeedInjected)(sessionId)
  return { faceOf, mounted, openResourceIn, surfaceOf }
}

describe('edit Session records', () => {
  it('records a document with unified separators, exposes it, and ends seeding once claimed', () => {
    const sessionId = 's-record' as SessionId
    recordEditSession(sessionId, 'D:\\host\\work\\Test.docx')
    expect(editSessionPathOf(sessionId)).toBe('D:/host/work/Test.docx')
    expect(unseededEditSessionPath(sessionId)).toBe('D:/host/work/Test.docx')
    markEditSessionSeeded(sessionId)
    expect(unseededEditSessionPath(sessionId)).toBeUndefined()
    // Seeding ended, but the binding stays readable: an edit Session's own tab
    // still knows not to create another Session for its document.
    expect(editSessionPathOf(sessionId)).toBe('D:/host/work/Test.docx')
  })

  it('reuses the recorded Session for the same document in either separator style', () => {
    const sessionId = 's-reuse-record' as SessionId
    recordEditSession(sessionId, 'D:/host/work/Report.docx')
    expect(editSessionFor('D:/host/work/Report.docx')).toBe(sessionId)
    expect(editSessionFor('D:\\host\\work\\Report.docx')).toBe(sessionId)
    expect(editSessionFor('D:/host/work/Other.docx')).toBeUndefined()
  })

  it('normalizes stored separators when reading a record back', () => {
    localStorage.setItem('dsh.wordinweb-edit.v1.s-stored', JSON.stringify({ path: 'D:\\a\\b.docx', seeded: false }))
    expect(editSessionPathOf('s-stored' as SessionId)).toBe('D:/a/b.docx')
  })

  it('ignores unreadable records', () => {
    localStorage.setItem('dsh.wordinweb-edit.v1.s-broken', '{not json')
    expect(editSessionPathOf('s-broken' as SessionId)).toBeUndefined()
    localStorage.setItem('dsh.wordinweb-edit.v1.s-empty', JSON.stringify({}))
    expect(unseededEditSessionPath('s-empty' as SessionId)).toBeUndefined()
  })
})

describe('WordinwebEditSessionSeed', () => {
  it('renders nothing and seeds once a Session is on screen', async () => {
    const { faceOf, mounted } = await boot()
    const sessionId = 's-occupant' as SessionId
    recordEditSession(sessionId, 'D:/host/work/Test.docx')
    const face = faceOf(sessionId)
    const seedEditTab = vi.fn(face.seedEditTab)
    // The slot runtime hands hooks in as selector hooks; render mirrors that.
    const useSidebarMounted = <S,>(selector: (value: SessionId | undefined) => S): S =>
      useSyncExternalStore(
        (listener: () => void) => face.hooks.sidebarMounted.subscribe(listener),
        () => selector(face.hooks.sidebarMounted.getSnapshot()),
      )
    render(<WordinwebEditSessionSeed
      {...({} as Omit<ComponentProps<typeof WordinwebEditSessionSeed>, 'useSidebarMounted' | 'seedEditTab'>)}
      useSidebarMounted={useSidebarMounted}
      seedEditTab={seedEditTab}
    />)
    expect(seedEditTab).not.toHaveBeenCalled()
    act(() => { mounted.set(sessionId) })
    expect(seedEditTab).toHaveBeenCalledExactlyOnceWith()
    expect(document.body.textContent).toBe('')
  })
})

describe('the seed face', () => {
  it('opens the recorded document tab in the edit Session and claims the seed when it lands', async () => {
    const { faceOf, openResourceIn } = await boot()
    const sessionId = 's-seed' as SessionId
    const path = 'D:/host/work/Test.docx'
    recordEditSession(sessionId, path)
    expect(faceOf(sessionId).seedEditTab()).toBe(true)
    expect(openResourceIn).toHaveBeenCalledExactlyOnceWith(sessionId, editAddressFor(sessionFileAddress(sessionId, path)))
    // The landed tab claims the seed: a tab the operator closes stays closed.
    expect(unseededEditSessionPath(sessionId)).toBeUndefined()
    expect(faceOf(sessionId).seedEditTab()).toBe(false)
    expect(openResourceIn).toHaveBeenCalledTimes(1)
  })

  it('claims an already-present tab without opening another', async () => {
    const { faceOf, openResourceIn, surfaceOf } = await boot()
    const sessionId = 's-restored' as SessionId
    const path = 'D:/host/work/Test.docx'
    recordEditSession(sessionId, path)
    // The committed surface a reload would rehydrate already holds the tab.
    surfaceOf(sessionId).push(tabRecord(editAddressFor(sessionFileAddress(sessionId, path))))
    expect(faceOf(sessionId).seedEditTab()).toBe(true)
    expect(openResourceIn).not.toHaveBeenCalled()
    expect(unseededEditSessionPath(sessionId)).toBeUndefined()
  })

  it('leaves the seed unclaimed for a surface that never adopted the tab', async () => {
    const { faceOf, openResourceIn } = await boot()
    const sessionId = 's-unadopted' as SessionId
    recordEditSession(sessionId, 'D:/host/work/Test.docx')
    openResourceIn.mockImplementation(() => { /* dropped: the Session's store was never adopted */ })
    expect(faceOf(sessionId).seedEditTab()).toBe(false)
    expect(unseededEditSessionPath(sessionId)).toBe('D:/host/work/Test.docx')
  })
})

describe('WordinwebEditTab create', () => {
  const controller = new AbortController()
  const owner = 's-host' as SessionId
  const created = 's-created' as SessionId
  const absolutePath = 'D:/host/work/reports/Test.docx'
  let closeTab: ReturnType<typeof vi.fn>

  beforeEach(() => {
    closeTab = vi.fn()
    bridge.read = async () => new Uint8Array([1, 2, 3])
    bridge.stat = async () => 'D:\\host\\work\\reports\\Test.docx'
    bridge.create = vi.fn(async () => created)
    bridge.prompt = vi.fn(async () => undefined)
    bridge.navigate = vi.fn()
  })

  afterEach(() => {
    bridge.read = undefined
    bridge.stat = undefined
    bridge.create = undefined
    bridge.prompt = undefined
    bridge.navigate = undefined
  })

  /** Props hosting one edit tab at one address, recording its close action. */
  function tabProps(contentId: string): ComponentProps<typeof WordinwebEditTab> {
    return {
      useTabInfo: () => ({ tab: { contentId, signal: controller.signal, actions: { close: closeTab } } }),
      t: (key: string) => key,
    } as unknown as ComponentProps<typeof WordinwebEditTab>
  }

  it('records the created Session against the document\'s normalized absolute path, then moves onto it and closes itself', async () => {
    render(<WordinwebEditTab {...tabProps(editAddressFor(sessionFileAddress(owner, 'reports/Test.docx')))} />)
    await vi.waitFor(() => { expect(bridge.navigate).toHaveBeenCalledExactlyOnceWith(created) })
    expect(editSessionPathOf(created)).toBe(absolutePath)
    expect(bridge.create).toHaveBeenCalledExactlyOnceWith('D:/host/work/reports', controller.signal)
    expect(closeTab).toHaveBeenCalledExactlyOnceWith()
  })

  it('reuses this window\'s edit Session for the same document instead of creating another', async () => {
    const reused = 's-reused' as SessionId
    const path = 'D:/other/Report2.docx'
    bridge.stat = async () => 'D:\\other\\Report2.docx'
    recordEditSession(reused, path)
    render(<WordinwebEditTab {...tabProps(editAddressFor(sessionFileAddress(owner, 'Report2.docx')))} />)
    await vi.waitFor(() => { expect(bridge.navigate).toHaveBeenCalledExactlyOnceWith(reused) })
    expect(bridge.create).not.toHaveBeenCalled()
    expect(bridge.prompt).not.toHaveBeenCalled()
    expect(closeTab).toHaveBeenCalledExactlyOnceWith()
  })

  it('creates nothing when its host already is the document\'s edit Session', () => {
    // The record may carry Windows separators while the tab address decodes to
    // `/`: the guard must still recognize its own Session (regression).
    recordEditSession(created, 'D:\\host\\work\\reports\\Test.docx')
    render(<WordinwebEditTab {...tabProps(editAddressFor(sessionFileAddress(created, absolutePath)))} />)
    // The guard returns before any await, so an unstarted create is final.
    expect(bridge.create).not.toHaveBeenCalled()
    expect(bridge.prompt).not.toHaveBeenCalled()
    expect(bridge.navigate).not.toHaveBeenCalled()
  })
})

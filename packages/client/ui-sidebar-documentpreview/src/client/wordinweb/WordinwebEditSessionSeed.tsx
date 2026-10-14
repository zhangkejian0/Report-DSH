/** Invisible Session-header occupant that brings an edit Session's document tab into view. */
import { useEffect, type ReactNode } from 'react'
import type { SessionId } from '@deepseek-ai/dsh-session/types'
import type { HostObservable, InjectFace, PropsRuntime } from '@deepseek-ai/dsh-client-ui-slots'
import type {} from '@deepseek-ai/dsh-client-ui-conversation/client'

/** The seed's injected face: the on-screen Session signal and the one-shot tab seed. */
export interface WordinwebEditSessionSeedInjected {
  readonly hooks: {
    /** The Session on screen; naming it precedes the commit that mounts this occupant. */
    readonly sidebarMounted: HostObservable<SessionId | undefined>
  }
  /**
   * Open this Session's recorded edit tab once its surface is committed.
   * @returns whether the owed tab is present after the attempt; false when
   * none is owed or the surface did not adopt it.
   */
  readonly seedEditTab: () => boolean
}

/** The seed occupant's props: the utilities slot's runtime props and its injected face. */
export type WordinwebEditSessionSeedProps =
  PropsRuntime<'conversation.session.header.utilities'> & InjectFace<WordinwebEditSessionSeedInjected>

/**
 * Place the WordInWeb edit tab in an edit Session's own Sidebar when that
 * Session first reaches the screen, so opening it from the Workspace list
 * lands in its conversation with the document editor still beside it instead
 * of an empty column. The seed runs from the effect of the commit that mounts
 * the Session — the same timing the plan review's automatic open uses — and
 * runs once: a tab the user closes stays closed.
 * @param props - the utilities slot's runtime props and the seed face.
 * @returns nothing; the occupant holds layout only for its effect.
 */
export function WordinwebEditSessionSeed({ useSidebarMounted, seedEditTab }: WordinwebEditSessionSeedProps): ReactNode {
  const mounted = useSidebarMounted(session => session !== undefined)
  useEffect(() => {
    if (mounted) seedEditTab()
  }, [mounted, seedEditTab])
  return null
}

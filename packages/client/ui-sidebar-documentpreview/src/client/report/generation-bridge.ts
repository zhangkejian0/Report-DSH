/** Plugin-lifetime holder for the generation deps, mirroring the other bridges. */
import type { GenerationDeps } from './generate.ts'

/**
 * The generation service closures, set by the plugin while its injected
 * services are up and cleared on dispose; the wizard reads them through its
 * tab props because the slot system offers no plugin-owned prop channel.
 */
export const generationBridge: { deps: GenerationDeps | undefined } = { deps: undefined }

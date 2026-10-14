# Thermal Analysis

English | [中文](thermal-analyze.zh.md)

The [document package family](../../packages/document/README.md) analyzes one radiometric thermal JPEG per call through the Fotric IRtek file SDK analyzer on the Host. Consumers authorize reads through the Session's ordinary filesystem authorization; the provider owns analyzer admission, deadlines, and report validation. This subsystem creates no model-facing tool or Session event.

## Ownership

| Owner | Responsibility |
|---|---|
| [thermal-analyze](../../packages/document/thermal-analyze/README.md) | `ctx.thermalAnalyze`: analyzer process, deadline, and report validation |
| [Web bundle](../../packages/bundle/web-app/README.md) | One configurable analyzer provider shared by Host consumers |
| [Thermal preview Client](../../packages/client/ui-sidebar-documentpreview/README.md) | Sidebar action, analysis tab, and report rendering |

## Requests and results

`ThermalAnalyze.analyze(workspaceFileScope, path, signal)` resolves the image through the Session's [Workspace Files](../../packages/api/workspace-files/README.md) authorization (a one-byte read plus a stat establish the same Session-relative path the document preview shows), spawns the analyzer with the resolved absolute path, and returns its complete [`ThermalAnalysis`](../../packages/document/thermal-analyze/src/types.ts) report: camera identity, measurement parameters, whole-frame temperature statistics, embedded measurement markers, a block-averaged temperature grid, and palette-rendered PNG images. Temperatures are Celsius.

Every failure rejects with one `thermal-analyze/failed` Remote error whose `reason` detail is `unavailable` (analyzer or services missing), `not-thermal` (the file is not a radiometric thermal image), `open-failed`, `timeout`, `too-large`, or `failed`; cancellation surfaces as `gateway/cancelled`.

<!-- BEGIN GENERATED cordis-surface (gen-cordis-catalog.ts) — do not edit between markers -->

<a id="cordis-surface"></a>

## Cordis API

Generated from source by `scripts/gen-cordis-catalog.ts` (verified fresh by `pnpm run verify-cordis-catalog` in doc-sync; regenerate with `pnpm run gen-cordis-catalog`) — the language sides differ only in locale-specific paired document paths. Signature blocks use a `ts cordis-catalog` fence and keep the original source JSDoc; dispatch modes are defined in the [primer](../cordis-primer.md#dispatch-modes), and the framework-inherited `ctx` API lives in [cordis-api/inherited.md](../cordis-api/inherited.md).

<a id="ctxthermalanalyze--thermalanalyze"></a>

### `ctx.thermalAnalyze` — `ThermalAnalyze`

A provider lifetime owning the configured analyzer and its in-flight calls.

```ts cordis-catalog
/**
 * Analyze one radiometric JPEG using the Session's ordinary filesystem authorization.
 * @param workspaceFileScope - Session header lookup shared with workspaceFiles.
 * @param path - absolute or workspace-relative path to the image.
 * @param signal - Remote cancellation; provider disposal also stops active work.
 * @returns the analyzer's complete report, temperatures in Celsius.
 * @throws {RemoteError} `thermal-analyze/failed` with a `reason` detail on every failure.
 */
@Remote async analyze(workspaceFileScope: WorkspaceFileScope, path: string, signal: AbortSignal): Promise<ThermalAnalysis>
```

Source: [`packages/document/thermal-analyze/src/index.ts`](../../packages/document/thermal-analyze/src/index.ts)
<!-- END GENERATED cordis-surface -->

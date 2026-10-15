# Report Generation

English | [中文](report-generate.zh.md)

The [document package family](../../packages/document/README.md) expands one Word template into a finished inspection report on the Host, deterministically, from the thermal analyzer's measurements. Consumers authorize reads and writes through the Session's ordinary filesystem authorization; the provider owns template admission, token substitution, per-image expansion, and residue refusal. This subsystem creates no model-facing tool or Session event.

## Ownership

| Owner | Responsibility |
|---|---|
| [report-generate](../../packages/document/report-generate/README.md) | `ctx.reportGenerate`: template validation, expansion engine, and write-back |
| [Web bundle](../../packages/bundle/web-app/README.md) | One configurable generation provider shared by Host consumers |
| [Report wizard Client](../../packages/client/ui-sidebar-documentpreview/README.md) | Wizard fast mode, generation dispatch, and edit-tab seating |

## Requests and results

`ReportGenerate.generate(workspaceFileScope, request, signal)` re-measures every requested image through [Thermal Analysis](thermal-analyze.md), reads the template through the Session's [Workspace Files](../../packages/api/workspace-files/README.md) authorization, expands its `{{#each_image}}` regions once per image, substitutes every token across body, header, and footer stories, inserts each image's thermal render, and writes the finished report back through the same authorization. Output is byte-deterministic for the same inputs, and Word refreshes field results (TOC page numbers) on open.

Every failure rejects with one `report-generate/failed` Remote error whose `reason` detail is `analyzer-unavailable`, `template-invalid`, `template-markers`, `template-too-large`, `no-image-loop`, `unknown-token`, `too-many-images`, `analysis-failed`, `residue`, `write-failed`, or `failed`; workspace and analyzer refusals pass through unchanged, and nothing is written when generation refuses.

<!-- BEGIN GENERATED cordis-surface (gen-cordis-catalog.ts) — do not edit between markers -->

<a id="cordis-surface"></a>

## Cordis API

Generated from source by `scripts/gen-cordis-catalog.ts` (verified fresh by `pnpm run verify-cordis-catalog` in doc-sync; regenerate with `pnpm run gen-cordis-catalog`) — the language sides differ only in locale-specific paired document paths. Signature blocks use a `ts cordis-catalog` fence and keep the original source JSDoc; dispatch modes are defined in the [primer](../cordis-primer.md#dispatch-modes), and the framework-inherited `ctx` API lives in [cordis-api/inherited.md](../cordis-api/inherited.md).

<a id="ctxreportgenerate--reportgenerate"></a>

### `ctx.reportGenerate` — `ReportGenerate`

A provider lifetime owning the configured image width and image cap.

```ts cordis-catalog
/**
 * Generate one report from the Session's template over its thermal images.
 * Every image is re-measured by the Host's own analyzer, so the report
 * carries fresh data whatever the client cached.
 * @param workspaceFileScope - Session header lookup shared with workspaceFiles.
 * @param request - template, output, image paths, and author fields.
 * @param signal - Remote cancellation.
 * @returns the written report's absolute path, size, and image count.
 * @throws {RemoteError} `report-generate/failed` with a `reason` detail on
 * every refusal; nothing is written when generation refuses.
 */
@Remote async generate( workspaceFileScope: WorkspaceFileScope, request: ReportGenerateRequest, signal: AbortSignal, ): Promise<ReportGenerateResult>
```

Source: [`packages/document/report-generate/src/index.ts`](../../packages/document/report-generate/src/index.ts)
<!-- END GENERATED cordis-surface -->

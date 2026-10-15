# 报告生成

[English](report-generate.md) | 中文

[document 包族](../../packages/document/README.zh.md) 在宿主上依据热像分析器的测量结果，把一份 Word 模版确定性地展开为成品检测报告。消费方通过会话的常规文件授权完成读写；提供者负责模版准入、占位符替换、逐图展开与残留拒绝。本子系统不创建任何面向模型的工具或会话事件。

## 职责

| 负责方 | 职责 |
|---|---|
| [report-generate](../../packages/document/report-generate/README.zh.md) | `ctx.reportGenerate`：模版校验、展开引擎与写回 |
| [Web bundle](../../packages/bundle/web-app/README.zh.md) | 一个可配置的生成提供者，供宿主消费方共享 |
| [报告向导 Client](../../packages/client/ui-sidebar-documentpreview/README.zh.md) | 向导快速模式、生成下发与编辑落座 |

## 请求与结果

`ReportGenerate.generate(workspaceFileScope, request, signal)` 通过[热像分析](thermal-analyze.zh.md)重新测量每张图片，经会话的 [Workspace Files](../../packages/api/workspace-files/README.zh.md) 授权读取模版，把 `{{#each_image}}` 区域按每张图片展开一份，在正文/页眉/页脚各 story 替换全部占位符，插入各图热图，再经同一授权写回成品报告。同样输入产出逐字节一致的结果，Word 打开时自动刷新域结果（目录页码）。

每次失败以一个 `report-generate/failed` Remote 错误拒绝，`reason` 细节为 `analyzer-unavailable`、`template-invalid`、`template-markers`、`template-too-large`、`no-image-loop`、`unknown-token`、`too-many-images`、`analysis-failed`、`residue`、`write-failed` 或 `failed`；工作区与分析器拒绝原样透传，生成拒绝时不写任何文件。

<!-- BEGIN GENERATED cordis-surface (gen-cordis-catalog.ts) — do not edit between markers -->

<a id="cordis-surface"></a>

## Cordis API

Generated from source by `scripts/gen-cordis-catalog.ts` (verified fresh by `pnpm run verify-cordis-catalog` in doc-sync; regenerate with `pnpm run gen-cordis-catalog`) — the language sides differ only in locale-specific paired document paths. Signature blocks use a `ts cordis-catalog` fence and keep the original source JSDoc; dispatch modes are defined in the [primer](../cordis-primer.zh.md#dispatch-modes), and the framework-inherited `ctx` API lives in [cordis-api/inherited.md](../cordis-api/inherited.md).

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

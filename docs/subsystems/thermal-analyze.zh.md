# 热像分析

[English](thermal-analyze.md) | 中文

[document 包族](../../packages/document/README.zh.md) 在宿主上通过 Fotric IRtek 文件 SDK 分析器逐次分析一张辐射测温热像 JPEG。消费者通过 Session 的常规文件系统授权完成读取授权；提供方负责分析器准入、时限和报告校验。此子系统不创建面向模型的工具或 Session 事件。

## 所有权

| 所有者 | 职责 |
|---|---|
| [thermal-analyze](../../packages/document/thermal-analyze/README.zh.md) | `ctx.thermalAnalyze`：分析器进程、时限和报告校验 |
| [Web bundle](../../packages/bundle/web-app/README.zh.md) | 由宿主消费者共享的单个可配置分析器提供方 |
| [热像预览 Client](../../packages/client/ui-sidebar-documentpreview/README.zh.md) | 侧栏动作、分析标签页和报告渲染 |

## 请求和结果

`ThermalAnalyze.analyze(workspaceFileScope, path, signal)` 通过 Session 的 [Workspace Files](../../packages/api/workspace-files/README.zh.md) 授权解析图片（一次单字节读取加一次 stat 确立与文档预览相同的 Session 相对路径），用解析后的绝对路径启动分析器，并返回其完整的 [`ThermalAnalysis`](../../packages/document/thermal-analyze/src/types.ts) 报告：相机标识、测量参数、整帧温度统计、内嵌测量标记、分块平均温度网格和调色板渲染的 PNG 图片。温度为摄氏度。

每次失败都以一个 `thermal-analyze/failed` Remote 错误拒绝，其 `reason` 细节为 `unavailable`（分析器或服务缺失）、`not-thermal`（文件不是辐射测温热像）、`open-failed`、`timeout`、`too-large` 或 `failed`；取消以 `gateway/cancelled` 呈现。

<!-- BEGIN GENERATED cordis-surface (gen-cordis-catalog.ts) — do not edit between markers -->

<a id="cordis-surface"></a>

## Cordis API

Generated from source by `scripts/gen-cordis-catalog.ts` (verified fresh by `pnpm run verify-cordis-catalog` in doc-sync; regenerate with `pnpm run gen-cordis-catalog`) — the language sides differ only in locale-specific paired document paths. Signature blocks use a `ts cordis-catalog` fence and keep the original source JSDoc; dispatch modes are defined in the [primer](../cordis-primer.zh.md#dispatch-modes), and the framework-inherited `ctx` API lives in [cordis-api/inherited.md](../cordis-api/inherited.md).

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

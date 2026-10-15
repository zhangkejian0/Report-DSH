---
description: "Expand one Word template into a finished inspection report, deterministically."
kind: "package-reference"
---

# @deepseek-ai/dsh-report-generate

English | [中文](README.zh.md)

## Summary

Generate one Word inspection report on the Host from a template .docx and the thermal analyzer's measurements — no model, no Session, no agent. The engine expands the template's per-image section once per image, substitutes every token (report header, whole-report aggregates, per-image measurements), inserts each image's thermal render, and asks Word to refresh fields such as TOC page numbers on open. Output is byte-deterministic for the same inputs.

## Table of Contents

- [Use this package](#use-this-package)
- [Understand the implementation](#understand-the-implementation)
- [Model Experience](#model-experience)
- [Known Limitations and Deferred Work](#known-limitations-and-deferred-work)
- [Dev Note](#dev-note)

-----

<a id="use-this-package"></a>
## Use this package

The [Web bundle](../../bundle/web-app/README.md) mounts this provider as `report-generate`. Independent compositions mount `@deepseek-ai/dsh-report-generate` as a `cordis.yml` row.

Browsers request generation through the `reportGenerate.generate` Remote method with a Session identity, the template and output workspace paths, the measured images, and optional author fields. The template read and the report write both go through `workspaceFiles` under the Session's ordinary authorization. `api/remotes` mounts the generated Client descriptor.

| Field | Required | Default | Meaning |
|---|---|---|---|
| `imageWidthPx` | no | `460` | Insertion width of thermal images; height keeps the natural ratio. |
| `maxImages` | no | `500` | Maximum images one generation may expand. |

The token vocabulary and loop conventions live in the template note the report wizard seeds (`报告模版/模版说明.md`): `{{report_title}}`-style header tokens, whole-report aggregates such as `{{overall_max_temp}}`, per-image tokens such as `{{t_max}}`, and one `{{#each_image}}`…`{{/each_image}}` region per repeating section — a bracketed block range in the body, or both markers inside one table row for a per-image row.

<a id="understand-the-implementation"></a>
## Understand the implementation

One call runs the pure engine in `src/engine.ts` over the WordInWeb document model — the same library the `word_document_*` tools drive. The engine scans the template's text projection (validating tokens and marker pairing), clones each loop region structurally on the XML tree, substitutes tokens through the projection's patch API across body, header, and footer stories, inserts the thermal PNGs, and refuses to emit anything while a `{{` residue survives. Failures surface as one `report-generate/failed` Remote error whose `reason` detail is `template-invalid`, `template-markers`, `template-too-large`, `no-image-loop`, `unknown-token`, `too-many-images`, `residue`, `write-failed`, or `failed`; workspace refusals pass through unchanged, and nothing is written when generation refuses.

<a id="model-experience"></a>
## Model Experience

None, as this package generates documents deterministically without model-facing tools, messages, or Session events.

#### KV Cache effect

None; generation constructs and modifies no model requests.

## Known Limitations and Deferred Work

<a id="known-limitations-and-deferred-work"></a>

- The report wizard's AI path remains the fallback for templates without explicit loop markers; the deterministic engine refuses them with `no-image-loop`.
- `{{marker_table}}` renders as inline text; a composed table form is deferred.
- The engine projects through one 2000-block window; beyond it a run fails with `template-too-large` rather than paging.
- Table-row loops require both loop markers inside one table row; markers split across rows are refused.

<a id="dev-note"></a>
### Dev Note

<details>
<summary>Working context for maintainers — click to expand</summary>

Depends on `@wordinweb/core`'s public `setUpdateFields`, added beside `setEvenAndOddHeaders` for the `<w:updateFields/>` settings toggle.
</details>

---
description: "Model-facing Word document tools over the WordInWeb agent toolkit."
kind: "package-reference"
---

# @deepseek-ai/dsh-tool-wordinweb

English | [中文](README.zh.md)

## Summary

Register the portable `word_document_*` tool set from the MIT-licensed
[`@wordinweb/agent`](https://www.npmjs.com/package/@wordinweb/agent) package on the Harness
ToolRuntime: schema-enforced DOCX composition, progressive inspection, transactional edits,
text projection and patching, capability schemas, and asset reads, plus three filesystem
conveniences — `word_document_new`, `word_document_open`, and `word_document_save`.
One `AgentDocument` is held per plugin instance; omitted `revision` fields are injected from
the live document. Everything runs in-process with no browser dependency.

## Use this package

The Base bundle mounts this plugin as `tool-wordinweb`. Independent compositions mount
`@deepseek-ai/dsh-tool-wordinweb` as a `cordis.yml` row.

| Field | Default | Meaning |
| --- | --- | --- |
| `workspace` | host process working directory | Directory that relative `.docx` paths resolve against. |
| `editorUrl` | — | Base URL of a Word editor included in save results as `editorUrl`, e.g. `http://127.0.0.1:5180`. |

## Understand the implementation

The plugin harvests names, descriptions, and JSON Schemas from
`AgentDocument.create().tools()` at registration time, so the registered schemas never drift
from the package contract. The package's own `word_document_save` (which returns document
bytes) is replaced by a filesystem-backed tool that writes the `.docx` under the configured
workspace and returns the absolute path. `revision` is removed from each schema's `required`
list and auto-filled at dispatch: edit and patch prefer the caller-supplied revision, then the
last projection revision, then the live document revision. Stale revisions surface as tool
errors from the package's own validation, matching the bridge-free behavior agents see
elsewhere.

| Tool | Role |
| --- | --- |
| `word_document_new` | Start a blank DOCX (required before compose). |
| `word_document_open` | Load a `.docx` from disk; returns the overview. |
| `word_document_compose` | Assemble a complete new document from schema-enforced blocks. |
| `word_document_capabilities` | List operation schemas by category or kind. |
| `word_document_inspect` | Overview, bounded context, read windows, search, object detail, page geometry. |
| `word_document_edit` | Apply a validated transaction against an observed revision. |
| `word_document_project` | Project a story as deterministic text or markdown with line anchors. |
| `word_document_patch` | Apply line-range edits or a unified diff against a projection. |
| `word_document_asset` | Read image or embedded asset bytes as base64. |
| `word_document_save` | Write the current document to disk; returns path, size, revision, optional `editorUrl`. |

## Model Experience

### Request context and condition

#### What the model sees

The ten `word_document_*` tool schemas while the plugin is mounted. The eight package tools
ship their own descriptions and JSON Schemas verbatim (with `revision` removed from
`required`); the three filesystem tools declare small object schemas shown in the table above.

#### Token effect

Conditional: fixed schemas per tool join prompt assembly only while the plugin is mounted in
the active composition. Inspection results are bounded by the package's own budgets (the
default `context` budget is 100 blocks and 24,000 characters).

#### KV Cache effect

Append-only for a stable composition: schemas join the prompt prefix when the plugin mounts
and leave it when the plugin is disposed, invalidating reuse at that boundary. Tool results
are ordinary per-turn message content and do not invalidate the prefix.

## Known Limitations and Deferred Work

- **One document per plugin instance** — the bridge holds a single `AgentDocument`; concurrent agents in one Host share it. Per-agent scoping would use `agent.ctx` plugin instances, deferred until the report product needs multi-session concurrency.
- **No filesystem seam integration** — open/save read and write through `node:fs` rather than `ctx.fs`, so sandbox and observation policy do not apply to `.docx` paths; migrating to the fs seam is deferred.
- **Parameters are not subset-validated** — raw tool parameters pass through to the model provider verbatim; the package's `$defs`/`$ref` usage is accepted by the tested providers, and a sanitizing projection can be added if a provider rejects it.

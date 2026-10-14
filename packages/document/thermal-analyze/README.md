---
description: "Run the Fotric IRtek SDK analyzer over authorized thermal images."
kind: "package-reference"
---

# @deepseek-ai/dsh-thermal-analyze

English | [中文](README.zh.md)

## Summary

Analyze one radiometric thermal JPEG on the Host computer and return the analyzer's complete report: camera identity, measurement parameters, whole-frame temperature statistics, embedded measurement markers, a block-averaged temperature grid, and palette-rendered images as base64 PNG. Temperatures are Celsius.

## Table of Contents

- [Use this package](#use-this-package)
- [Understand the implementation](#understand-the-implementation)
- [Model Experience](#model-experience)
- [Known Limitations and Deferred Work](#known-limitations-and-deferred-work)
- [Dev Note](#dev-note)

-----

<a id="use-this-package"></a>
## Use this package

The [Web bundle](../../bundle/web-app/README.md) mounts this provider as `thermal-analyze`. Independent compositions mount `@deepseek-ai/dsh-thermal-analyze` as a `cordis.yml` row.

Browsers request analysis through the `thermalAnalyze.analyze` Remote method with a Session identity and image path. The entry uses `workspaceFiles` for authorization: a one-byte read plus a stat establish the same Session-relative path the document preview shows, and the analyzer receives only the resolved absolute path. `api/remotes` mounts the generated Client descriptor.

| Field | Required | Default | Meaning |
|---|---|---|---|
| `analyzerPath` | yes | — | Absolute path of the analyzer executable. |
| `timeoutMs` | no | `60000` | Analyzer deadline, including process startup. |
| `maxOutputBytes` | no | `16777216` | Maximum accepted report size. |

The analyzer executable is the Fotric IRtek file SDK CLI in the repository workspace (`fotric_sdk/AnalyzeImage/ThermalAnalyzeJson.exe`, Windows x86). It validates the file through the SDK, writes one `result.json` into a private temporary directory, and exits non-zero with an `ERROR <code>` line on stdout otherwise.

<a id="understand-the-implementation"></a>
## Understand the implementation

One call spawns the analyzer, waits within `timeoutMs`, size-checks and structurally verifies the report against the declared result vocabulary, and always removes the temporary directory. Failures surface as one `thermal-analyze/failed` Remote error whose `reason` detail is `unavailable` (analyzer or services missing), `not-thermal` (the file is not a radiometric thermal image), `open-failed`, `timeout`, `too-large`, or `failed`. Cancellation surfaces as `gateway/cancelled`.

<a id="model-experience"></a>
## Model Experience

None, as this package runs an external analyzer without model-facing tools, messages, or Session events.

#### KV Cache effect

None; analysis constructs and modifies no model requests.

## Known Limitations and Deferred Work

<a id="known-limitations-and-deferred-work"></a>

- The analyzer executable runs on Windows x86 only; other platforms fail with reason `unavailable` or `failed` at the first call, not at load.
- Beyond structural field checks, report parsing trusts the analyzer executable this repository ships beside itself; a replaced executable is a deployment concern, not a validation one.
- Thermal videos and acoustic files are out of scope; the analyzer rejects them with reason `not-thermal`.

<a id="dev-note"></a>
### Dev Note

<details>
<summary>Working context for maintainers — click to expand</summary>

None.
</details>

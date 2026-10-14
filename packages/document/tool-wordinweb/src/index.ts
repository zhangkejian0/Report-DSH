/**
 * Model-facing Word document tools over the WordInWeb agent toolkit.
 *
 * Registers the portable `word_document_*` tool set from `@wordinweb/agent`
 * (compose, capabilities, inspect, edit, project, patch, asset) plus four
 * filesystem conveniences (`word_document_new`, `word_document_open`,
 * `word_document_save`, `word_document_add_image`). Every agent (session)
 * holds its own `AgentDocument`, so concurrent sessions edit independent
 * documents; `revision` fields are auto-filled from the calling agent's live
 * document when omitted.
 * @module @deepseek-ai/dsh-tool-wordinweb
 */

import { mkdir, readFile, writeFile } from 'node:fs/promises'
import { basename, dirname, isAbsolute, join, resolve } from 'node:path'
import type { Context } from '@deepseek-ai/cordis'
import z from '@deepseek-ai/schemastery'
import type { ContentBlock, ToolSchema } from '@deepseek-ai/dsh-llm'
import type { ToolDefinition, ToolRunContext } from '@deepseek-ai/dsh-tools'
import type { SessionId } from '@deepseek-ai/dsh-session/types'
import type {} from '@deepseek-ai/dsh-session'
import { AgentDocument } from '@wordinweb/agent'
import type { AgentDocument as AgentDocumentType, AgentTool } from '@wordinweb/agent'

type CapabilityCategory = Parameters<AgentDocumentType['capabilities']>[0]
type CapabilityKind = Parameters<AgentDocumentType['capabilities']>[1]

/** Live document state owned by one agent's tool calls. */
interface DocumentState {
  doc: AgentDocumentType | null
  lastProjectionRevision: string | undefined
}

/**
 * Document states keyed by the agent whose executions carry it: concurrent
 * sessions never observe one another's document. Agent-less executions (direct
 * registry calls, tests) share one fallback state; a disposed session's entry
 * is dropped with its session.
 */
interface DocumentStore {
  readonly shared: DocumentState
  readonly byAgent: Map<SessionId, DocumentState>
}

/** The state one execution addresses: its agent's, or the shared fallback. */
function stateFor(store: DocumentStore, exec: ToolRunContext | undefined): DocumentState {
  const id = exec?.agent?.id
  if (id === undefined) return store.shared
  let state = store.byAgent.get(id)
  if (state === undefined) {
    state = { doc: null, lastProjectionRevision: undefined }
    store.byAgent.set(id, state)
  }
  return state
}

/** Resolved plugin configuration. */
interface ResolvedConfig {
  workspaceRoot: string
  editorUrl: string | undefined
}

/**
 * The package tool whose result carries .docx bytes is replaced by the
 * filesystem-backed `word_document_save` below, so it is not registered.
 */
const PACKAGE_TOOLS_REPLACED = new Set(['word_document_save'])

function requireDoc(state: DocumentState): AgentDocumentType {
  if (state.doc === null) {
    throw new Error('No document loaded. Call word_document_new or word_document_open first.')
  }
  return state.doc
}

function defaultSavePath(config: ResolvedConfig): string {
  const now = new Date()
  const pad = (value: number): string => String(value).padStart(2, '0')
  const stamp = `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}-${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`
  return join(config.workspaceRoot, `report-${stamp}.docx`)
}

function resolvePath(config: ResolvedConfig, path: string | undefined): string {
  if (path === undefined || path.trim() === '') return defaultSavePath(config)
  return isAbsolute(path) ? path : resolve(config.workspaceRoot, path)
}

function asRecord(value: unknown): Record<string, unknown> {
  if (value === null || typeof value !== 'object' || Array.isArray(value)) {
    throw new Error('Tool arguments must be a JSON object.')
  }
  return value as Record<string, unknown>
}

/** The caller-supplied revision when it is a string, otherwise the fallback. */
function revisionOr(input: Record<string, unknown>, fallback: string): string {
  return typeof input.revision === 'string' ? input.revision : fallback
}

/**
 * Drop `undefined` property values recursively: the toolkit's results carry
 * absent optional fields as undefined keys, which lossless JSON rejects.
 */
function pruneUndefined(value: unknown): unknown {
  if (Array.isArray(value)) return value.map(pruneUndefined)
  if (value !== null && typeof value === 'object') {
    const out: Record<string, unknown> = {}
    for (const [key, item] of Object.entries(value)) {
      if (item !== undefined) out[key] = pruneUndefined(item)
    }
    return out
  }
  return value
}

/**
 * Copy a package tool's JSON Schema with `revision` removed from `required`:
 * the dispatcher injects the live revision, so the model may omit it.
 */
function stripRevisionRequired(schema: unknown): Record<string, unknown> {
  const copy = JSON.parse(JSON.stringify(schema)) as { required?: string[] }
  if (Array.isArray(copy.required)) copy.required = copy.required.filter(key => key !== 'revision')
  return copy as Record<string, unknown>
}

const jsonOutput = {
  schema: {},
  render(_args: unknown, value: unknown): ContentBlock[] {
    return [{ type: 'text', text: JSON.stringify(value) }]
  },
}

/** Dispatch one tool call against the calling agent's live document state. */
async function dispatch(
  store: DocumentStore,
  config: ResolvedConfig,
  name: string,
  input: Record<string, unknown>,
  exec: ToolRunContext | undefined,
): Promise<unknown> {
  const state = stateFor(store, exec)
  switch (name) {
    case 'word_document_new':
      state.doc = AgentDocument.create()
      state.lastProjectionRevision = undefined
      return { status: 'created', revision: state.doc.revision }

    case 'word_document_open': {
      if (typeof input.path !== 'string' || input.path.trim() === '') {
        throw new Error('word_document_open requires a non-empty string `path`.')
      }
      const file = resolvePath(config, input.path)
      const bytes = await readFile(file)
      state.doc = AgentDocument.load(bytes)
      state.lastProjectionRevision = undefined
      return state.doc.inspect({ kind: 'overview' })
    }

    case 'word_document_save': {
      const doc = requireDoc(state)
      const file = resolvePath(config, typeof input.path === 'string' ? input.path : undefined)
      if (!file.toLowerCase().endsWith('.docx')) throw new Error('Save target must end with .docx')
      await mkdir(dirname(file), { recursive: true })
      const bytes = doc.save()
      await writeFile(file, bytes)
      const saved = basename(file)
      return {
        saved: file,
        bytes: bytes.length,
        revision: doc.revision,
        ...config.editorUrl === undefined ? {} : { editorUrl: `${config.editorUrl.replace(/\/$/, '')}/?file=${encodeURIComponent(saved)}` },
      }
    }

    case 'word_document_add_image': {
      const doc = requireDoc(state)
      if (typeof input.path !== 'string' || input.path.trim() === '') {
        throw new Error('word_document_add_image requires a non-empty string `path`.')
      }
      const file = resolvePath(config, input.path)
      const mediaType = typeof input.mediaType === 'string' && input.mediaType.trim() !== ''
        ? input.mediaType
        : imageMediaTypeOf(file)
      if (mediaType === undefined) throw new Error('word_document_add_image requires a .png or .jpg path, or an explicit mediaType.')
      const bytes = await readFile(file)
      const assetRef = doc.addAsset(bytes, mediaType)
      const size = imageDimensionsOf(bytes)
      return {
        assetRef,
        bytes: bytes.length,
        mediaType,
        ...size === undefined ? {} : { naturalWidthPx: size.widthPx, naturalHeightPx: size.heightPx },
        hint: 'Reference assetRef in a compose image block or an insertImage edit; scale widthPx/heightPx keeping the natural aspect ratio.',
      }
    }

    case 'word_document_compose': {
      const doc = requireDoc(state)
      return await doc.compose({ ...input, revision: revisionOr(input, doc.revision) } as Parameters<AgentDocumentType['compose']>[0])
    }

    case 'word_document_capabilities':
      return requireDoc(state).capabilities(
        input.category as CapabilityCategory,
        input.kind as CapabilityKind,
      )

    case 'word_document_inspect':
      return requireDoc(state).inspect(input as Parameters<AgentDocumentType['inspect']>[0])

    case 'word_document_edit': {
      const doc = requireDoc(state)
      return await doc.edit({ ...input, revision: revisionOr(input, doc.revision) } as Parameters<AgentDocumentType['edit']>[0])
    }

    case 'word_document_project': {
      const result = requireDoc(state).project(input as Parameters<AgentDocumentType['project']>[0])
      state.lastProjectionRevision = result.revision
      return result
    }

    case 'word_document_patch': {
      const doc = requireDoc(state)
      const fallback = state.lastProjectionRevision ?? doc.revision
      return await doc.patch({
        ...input,
        revision: revisionOr(input, fallback),
      } as Parameters<AgentDocumentType['patch']>[0])
    }

    case 'word_document_asset': {
      const asset = requireDoc(state).asset(String(input.ref ?? ''))
      return {
        ref: asset.ref,
        mediaType: asset.mediaType,
        base64: Buffer.from(asset.bytes).toString('base64'),
      }
    }

    default:
      throw new Error(`Unknown tool: ${name}`)
  }
}

/** One raw ToolRuntime definition wrapping a package tool. */
function packageToolDefinition(store: DocumentStore, config: ResolvedConfig, tool: AgentTool): ToolDefinition {
  return {
    name: tool.name,
    description: tool.description,
    parameters: stripRevisionRequired(tool.inputSchema),
    output: jsonOutput,
    async execute(args: unknown, exec: ToolRunContext): Promise<unknown> {
      return pruneUndefined(await dispatch(store, config, tool.name, asRecord(args), exec))
    },
  }
}

const OPEN_PARAMETERS: ToolSchema['parameters'] = {
  type: 'object',
  properties: {
    path: { type: 'string', description: 'File path of the .docx to open. Relative paths resolve against the configured workspace.' },
  },
  required: ['path'],
  additionalProperties: false,
}

const SAVE_PARAMETERS: ToolSchema['parameters'] = {
  type: 'object',
  properties: {
    path: { type: 'string', description: 'Target .docx path. Relative paths resolve against the configured workspace; omit for a timestamped default name.' },
  },
  additionalProperties: false,
}

const ADD_IMAGE_PARAMETERS: ToolSchema['parameters'] = {
  type: 'object',
  properties: {
    path: { type: 'string', description: 'Image file path (.png or .jpg). Relative paths resolve against the configured workspace.' },
    mediaType: { type: 'string', description: 'Override the extension-derived media type, e.g. image/png.' },
  },
  required: ['path'],
  additionalProperties: false,
}

/** Media type of one image path by extension, `undefined` for unknown suffixes. */
function imageMediaTypeOf(path: string): string | undefined {
  const lowered = path.toLowerCase()
  if (lowered.endsWith('.png')) return 'image/png'
  if (lowered.endsWith('.jpg') || lowered.endsWith('.jpeg')) return 'image/jpeg'
  return undefined
}

/**
 * Natural pixel dimensions of one PNG or JPEG image, so image blocks can keep
 * their aspect ratio; `undefined` when the bytes are not a decodable header.
 * @param bytes - image bytes as stored on disk.
 * @returns width and height in pixels, or undefined for an unrecognized format.
 */
function imageDimensionsOf(bytes: Uint8Array): { widthPx: number; heightPx: number } | undefined {
  const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength)
  if (bytes.length >= 24 && bytes[0] === 0x89 && bytes[1] === 0x50 && bytes[2] === 0x4e && bytes[3] === 0x47) {
    return { widthPx: view.getUint32(16), heightPx: view.getUint32(20) }
  }
  if (bytes.length >= 4 && bytes[0] === 0xff && bytes[1] === 0xd8) {
    let offset = 2
    while (offset + 9 < bytes.length) {
      if (bytes[offset] !== 0xff) { offset += 1; continue }
      const marker = bytes[offset + 1]
      if (marker === undefined) break
      const length = view.getUint16(offset + 2)
      // SOF0..SOF3, SOF5..SOF7, SOF9..SOF11, SOF13..SOF15 carry the frame size.
      if ((marker >= 0xc0 && marker <= 0xc3) || (marker >= 0xc5 && marker <= 0xc7)
        || (marker >= 0xc9 && marker <= 0xcb) || (marker >= 0xcd && marker <= 0xcf)) {
        return { heightPx: view.getUint16(offset + 5), widthPx: view.getUint16(offset + 7) }
      }
      if (marker === 0xd8 || marker === 0x01 || (marker >= 0xd0 && marker <= 0xd7)) { offset += 2; continue }
      offset += 2 + length
    }
  }
  return undefined
}

/** All tool definitions owned by this plugin. */
function toolDefinitions(store: DocumentStore, config: ResolvedConfig): ToolDefinition[] {
  const definitions: ToolDefinition[] = [
    {
      name: 'word_document_new',
      description: 'Start a blank DOCX. Required before word_document_compose (compose only works on a fresh blank document).',
      parameters: { type: 'object', properties: {}, additionalProperties: false },
      output: jsonOutput,
      async execute(_args: unknown, exec: ToolRunContext): Promise<unknown> {
        return pruneUndefined(await dispatch(store, config, 'word_document_new', {}, exec))
      },
    },
    {
      name: 'word_document_open',
      description: 'Load an existing .docx from disk and replace the current document. Returns the document overview.',
      parameters: OPEN_PARAMETERS,
      output: jsonOutput,
      async execute(args: unknown, exec: ToolRunContext): Promise<unknown> {
        return pruneUndefined(await dispatch(store, config, 'word_document_open', asRecord(args), exec))
      },
    },
    {
      name: 'word_document_save',
      description: 'Write the current document to a .docx file and return the absolute saved path'
        + (config.editorUrl === undefined ? '.' : ' plus an editorUrl the user can open.'),
      parameters: SAVE_PARAMETERS,
      output: jsonOutput,
      async execute(args: unknown, exec: ToolRunContext): Promise<unknown> {
        return pruneUndefined(await dispatch(store, config, 'word_document_save', asRecord(args), exec))
      },
    },
    {
      name: 'word_document_add_image',
      description: 'Register one on-disk image (.png/.jpg) as a document asset and return its assetRef with natural dimensions, for image blocks in word_document_compose and insertImage edits.',
      parameters: ADD_IMAGE_PARAMETERS,
      output: jsonOutput,
      async execute(args: unknown, exec: ToolRunContext): Promise<unknown> {
        return pruneUndefined(await dispatch(store, config, 'word_document_add_image', asRecord(args), exec))
      },
    },
  ]
  for (const tool of AgentDocument.create().tools()) {
    if (PACKAGE_TOOLS_REPLACED.has(tool.name)) continue
    definitions.push(packageToolDefinition(store, config, tool))
  }
  return definitions
}

export const name = 'tool-wordinweb'
export const inject = ['tools']

/** Configuration for the WordInWeb document tools. */
export interface Config {
  /** Directory that relative .docx paths resolve against (default: host process working directory). */
  workspace?: string
  /** Base URL of a Word editor to include in save results, e.g. http://127.0.0.1:5180. */
  editorUrl?: string
}

/** Runtime configuration schema for the WordInWeb document tools. */
export const Config: z<Config> = z.object({
  workspace: z.string(),
  editorUrl: z.string(),
})

/** Register the `word_document_*` tools over the WordInWeb agent toolkit. */
export function apply(ctx: Context, config: Config): void {
  const resolved: ResolvedConfig = {
    workspaceRoot: resolve(config.workspace ?? process.cwd()),
    editorUrl: config.editorUrl === undefined || config.editorUrl.trim() === ''
      ? undefined
      : config.editorUrl,
  }
  const store: DocumentStore = {
    shared: { doc: null, lastProjectionRevision: undefined },
    byAgent: new Map(),
  }
  for (const definition of toolDefinitions(store, resolved)) ctx.tools.register(definition)
  ctx.on('session/disposed', (session) => { store.byAgent.delete(session.id) })
}

/** Names of every registered `word_document_*` tool, so tests and hosts name tool calls without string literals. */
export const WORD_DOCUMENT_TOOL_NAMES = [
  'word_document_new',
  'word_document_open',
  'word_document_save',
  'word_document_add_image',
  'word_document_compose',
  'word_document_capabilities',
  'word_document_inspect',
  'word_document_edit',
  'word_document_project',
  'word_document_patch',
  'word_document_asset',
] as const

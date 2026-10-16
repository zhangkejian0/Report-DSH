#!/usr/bin/env node
/**
 * Fotric IRtek thermal MCP server (stdio, zero dependencies).
 *
 * Exposes the repository's IRtek file-SDK executables as four tools so any
 * MCP client — DSH sessions through `dsh-mcp-client`, or other agents — can
 * converse over radiometric thermal JPEGs:
 *
 *   thermal_analyze  measurement report of one image (temperatures in Celsius)
 *   thermal_mark     write ROI measurement markers into an annotated copy
 *   thermal_edit     re-parameter a copy (params/gain/palette/rotate/mirror/note)
 *   thermal_render   return the palette-rendered image as an image content block
 *
 * Every write goes to a numbered `<stem>_AI.jpg` copy beside the source; the
 * radiometric payload survives, so copies stay measurable. Marker coordinates
 * are file pixels — the same space the rendered image shows.
 *
 * Executables resolve beside this script's `../AnalyzeImage/` unless
 * FOTRIC_SDK_DIR overrides. All temperatures are Celsius.
 */

import { spawn } from 'node:child_process'
import { existsSync } from 'node:fs'
import { mkdtemp, readFile, rm, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { basename, dirname, isAbsolute, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { createInterface } from 'node:readline'

const SDK_DIR = process.env.FOTRIC_SDK_DIR ?? resolve(dirname(fileURLToPath(import.meta.url)), '..')
const ANALYZER = join(SDK_DIR, 'AnalyzeImage', 'ThermalAnalyzeJson.exe')
const MARKER = join(SDK_DIR, 'AnalyzeImage', 'MarkImage.exe')
const EDITOR = join(SDK_DIR, 'AnalyzeImage', 'EditImage.exe')
const TIMEOUT_MS = Number.parseInt(process.env.FOTRIC_TIMEOUT_MS ?? '60000', 10)

/** Run one SDK executable and return its stdout, mapping failures to messages. */
function runExecutable(command, args) {
  return new Promise((resolveRun, rejectRun) => {
    const child = spawn(command, args, { windowsHide: true })
    let stdout = ''
    let stderr = ''
    const timer = setTimeout(() => { child.kill() }, TIMEOUT_MS)
    child.stdout.on('data', chunk => { stdout += chunk })
    child.stderr.on('data', chunk => { stderr += chunk })
    child.on('error', error => { clearTimeout(timer); rejectRun(new Error(`${basename(command)} failed to start: ${error.message}`)) })
    child.on('close', code => {
      clearTimeout(timer)
      if (code === 0) { resolveRun(stdout); return }
      const detail = (stdout || stderr).trim()
      if (code === 2) rejectRun(new Error(`Not a radiometric thermal JPEG${detail === '' ? '' : `: ${detail}`}`))
      else if (code === 3) rejectRun(new Error(`The radiometric file could not be opened${detail === '' ? '' : `: ${detail}`}`))
      else rejectRun(new Error(`${basename(command)} failed (exit ${code})${detail === '' ? '' : `: ${detail}`}`))
    })
  })
}

/** Resolve one model-supplied path against the server process unless absolute. */
const resolvePath = path => (isAbsolute(path) ? path : resolve(path))

/** Require an existing file; a miss tells the caller how to retry. */
async function requireImage(path) {
  const resolved = resolvePath(path)
  if (!existsSync(resolved)) {
    throw new Error(`No image file at ${path}. If this is a workspace-relative path, retry with the absolute path built from your working directory.`)
  }
  return resolved
}

/** Pick the produced copy's path: explicit output, or `<stem>_AI[n].jpg` beside the source. */
function resolveOutput(sourcePath, output) {
  if (output !== undefined) {
    const resolved = resolvePath(output)
    const lowered = resolved.toLowerCase()
    if (!lowered.endsWith('.jpg') && !lowered.endsWith('.jpeg')) throw new Error('The output path must end in jpg or jpeg.')
    return resolved
  }
  const directory = dirname(sourcePath)
  const stem = basename(sourcePath).replace(/\.[^.]+$/u, '')
  for (let index = 1; index <= 99; index++) {
    const suffix = index === 1 ? 'AI' : `AI${index}`
    const candidate = join(directory, `${stem}_${suffix}.jpg`)
    if (!existsSync(candidate)) return candidate
  }
  throw new Error('Too many AI copies beside this image; pass an explicit output path.')
}

/** Run the analyzer over one image and return its parsed report. */
async function analyzeReport(imagePath) {
  const directory = await mkdtemp(join(tmpdir(), 'fotric-mcp-'))
  try {
    await runExecutable(ANALYZER, [imagePath, directory])
    return JSON.parse(await readFile(join(directory, 'result.json'), 'utf8'))
  } finally {
    await rm(directory, { recursive: true, force: true })
  }
}

/** The report minus the UI-only payload (hover grid, palette images). */
function summarize(report) {
  return {
    file: { width: report.file.width, height: report.file.height, modifiedTime: report.file.modifiedTime },
    camera: report.camera, lens: report.lens, params: report.params, gain: report.gain,
    stats: report.stats, markers: report.markers, frame: report.frame,
  }
}

const PATH_SCHEMA = {
  type: 'string',
  description: 'Absolute path of the radiometric JPEG (relative paths resolve against the server working directory).',
}

const MARKER_SHAPES = [
  {
    type: 'object', additionalProperties: false,
    properties: {
      type: { type: 'string', const: 'rectangle' },
      name: { type: 'string', description: 'Unique marker name; Chinese names are supported.' },
      rect: { type: 'array', items: { type: 'number' }, description: '[x, y, width, height] in file pixels.' },
    },
    required: ['type', 'name', 'rect'],
  },
  {
    type: 'object', additionalProperties: false,
    properties: {
      type: { type: 'string', const: 'ellipse' },
      name: { type: 'string' },
      rect: { type: 'array', items: { type: 'number' }, description: '[x, y, width, height] of the bounding box.' },
    },
    required: ['type', 'name', 'rect'],
  },
  {
    type: 'object', additionalProperties: false,
    properties: {
      type: { type: 'string', const: 'polygon' },
      name: { type: 'string' },
      points: { type: 'array', items: { type: 'array', items: { type: 'number' } }, description: 'At least 3 [x, y] vertices.' },
    },
    required: ['type', 'name', 'points'],
  },
  {
    type: 'object', additionalProperties: false,
    properties: {
      type: { type: 'string', const: 'polyline' },
      name: { type: 'string' },
      points: { type: 'array', items: { type: 'array', items: { type: 'number' } }, description: 'At least 2 [x, y] vertices.' },
    },
    required: ['type', 'name', 'points'],
  },
  {
    type: 'object', additionalProperties: false,
    properties: {
      type: { type: 'string', const: 'spot' },
      name: { type: 'string' },
      point: { type: 'array', items: { type: 'number' }, description: '[x, y].' },
    },
    required: ['type', 'name', 'point'],
  },
]

const EDIT_SECTIONS = {
  type: 'object',
  description: 'At least one section; temperatures are Celsius; sections combine in one call.',
  additionalProperties: false,
  properties: {
    params: {
      type: 'object', additionalProperties: false,
      properties: {
        emissivity: { type: 'number', description: '0.01..1.' },
        distance: { type: 'number', description: 'Target distance in meters.' },
        reflTemp: { type: 'number', description: 'Reflected temperature, Celsius.' },
        atmTemp: { type: 'number', description: 'Atmospheric temperature, Celsius.' },
        relHumidity: { type: 'number', description: '0.01..1.' },
      },
    },
    gain: {
      type: 'object', additionalProperties: false,
      properties: {
        mode: { type: 'string', enum: ['Fixed', 'Auto'] },
        min: { type: 'number', description: 'Window minimum, Celsius.' },
        max: { type: 'number', description: 'Window maximum, Celsius.' },
      },
    },
    palette: {
      type: 'object', additionalProperties: false,
      properties: {
        refNo: { type: 'integer', description: 'Palette number 0..255 as the camera enumerates them.' },
        inverted: { type: 'boolean' },
      },
    },
    rotate: { type: 'integer', enum: [90, 180, 270] },
    mirror: { type: 'string', enum: ['horizontal', 'vertical'] },
    note: { type: 'string', description: 'Text note stored in the file.' },
  },
}

/**
 * Server instructions: joined into the session system prompt by MCP clients,
 * invisible in the conversation. Carries the routing and budget discipline so
 * user drafts can stay plain human language.
 */
const INSTRUCTIONS = [
  'Radiometric thermal JPEGs (Fotric/IRtek cameras; a normal JPEG preview followed by a large radiometric payload) are processed ONLY with the thermal tools of this server:',
  '- thermal_render: see the palette image once; measure marker coordinates directly on it (image pixels = file pixels, x right, y down).',
  '- thermal_mark: write ROI markers into a numbered _AI.jpg copy beside the source; pass the complete marker list every call (copies rebuild from the original; repeated calls replace).',
  '- thermal_edit: change measurement params (emissivity, distance, Celsius temperatures, humidity), gain window, palette, rotation, mirror, or note.',
  '- thermal_analyze: read the measurement report (whole-frame statistics, embedded markers, params).',
  'Working rules:',
  '- Paths: pass ABSOLUTE paths. When the user references a workspace-relative path, first build the absolute path from the working directory in your context, then call.',
  '- Whenever the user mentions or attaches such an image, route through these tools instead of shell work; never call the SDK executables, export temperature matrices, or crop/annotate images yourself.',
  '- Budget discipline: render once, then one mark/edit call, then report. Temperature judgments come from tool results only.',
  '- Never modify an original image; every write produces a copy and returns the readback temperatures.',
  '- Marker names: short Chinese names are supported (e.g. 绝缘子1) and must be unique within one call.',
].join('\n')

/** Tool implementations; every one returns MCP content blocks. */
const handlers = {
  async thermal_analyze(args) {
    const image = await requireImage(args.image_path)
    const summary = summarize(await analyzeReport(image))
    return [{ type: 'text', text: JSON.stringify(summary) }]
  },

  async thermal_render(args) {
    const image = await requireImage(args.image_path)
    const report = await analyzeReport(image)
    if (report.images?.thermal === '') throw new Error('The analyzer rendered no palette image for this file.')
    return [
      {
        type: 'text',
        text: `Palette-rendered thermal image: ${report.file.width}×${report.file.height} pixels. ` +
          `Marker coordinates are this image's pixels (x right, y down). ` +
          `Frame: max ${report.stats.max.toFixed(2)}℃, min ${report.stats.min.toFixed(2)}℃, avg ${report.stats.avg.toFixed(2)}℃.`,
      },
      { type: 'image', data: report.images.thermal, mimeType: 'image/png' },
    ]
  },

  async thermal_mark(args) {
    if (!Array.isArray(args.markers) || args.markers.length === 0) throw new Error('markers must be a non-empty array.')
    const image = await requireImage(args.image_path)
    const output = resolveOutput(image, args.output)
    const directory = await mkdtemp(join(tmpdir(), 'fotric-mcp-mark-'))
    try {
      const specPath = join(directory, 'markers.json')
      await writeFile(specPath, JSON.stringify({ markers: args.markers }), 'utf8')
      const stdout = await runExecutable(MARKER, [image, output, specPath])
      return [{ type: 'text', text: stdout.trim() }]
    } finally {
      await rm(directory, { recursive: true, force: true })
    }
  },

  async thermal_edit(args) {
    if (typeof args.edits !== 'object' || args.edits === null || Array.isArray(args.edits)) throw new Error('edits must be an object.')
    const image = await requireImage(args.image_path)
    const output = resolveOutput(image, args.output)
    const directory = await mkdtemp(join(tmpdir(), 'fotric-mcp-edit-'))
    try {
      const editPath = join(directory, 'edit.json')
      await writeFile(editPath, JSON.stringify(args.edits), 'utf8')
      const stdout = await runExecutable(EDITOR, [image, output, editPath])
      return [{ type: 'text', text: stdout.trim() }]
    } finally {
      await rm(directory, { recursive: true, force: true })
    }
  },
}

const tools = [
  {
    name: 'thermal_analyze',
    description: 'Analyze one radiometric thermal JPEG and return its measurement report: image size in pixels, whole-frame temperature statistics, every embedded measurement marker with max/min/avg, capture parameters, and camera identity. Temperatures are Celsius. The coordinate space is file pixels — the same space thermal_mark expects.',
    inputSchema: { type: 'object', properties: { image_path: PATH_SCHEMA }, required: ['image_path'], additionalProperties: false },
  },
  {
    name: 'thermal_render',
    description: 'Render one radiometric thermal JPEG with its palette and return the image itself, so targets can be identified visually and coordinates measured directly on it (image pixels = marker coordinates). Cheaper than re-reading files: one render is usually enough for a whole annotation task.',
    inputSchema: { type: 'object', properties: { image_path: PATH_SCHEMA }, required: ['image_path'], additionalProperties: false },
  },
  {
    name: 'thermal_mark',
    description: 'Write measurement markers (ROI annotations such as insulator boxes or polygons) into a copy of one radiometric thermal JPEG, keeping the radiometric payload so the file stays measurable. Coordinates are file pixels — measure them on the thermal_render image. Pass the complete marker list in every call; the copy is rebuilt from the original, so calls replace markers rather than appending. Returns per-marker max/min/avg temperatures in Celsius read back from the written copy.',
    inputSchema: {
      type: 'object',
      properties: {
        image_path: PATH_SCHEMA,
        markers: { type: 'array', items: { oneOf: MARKER_SHAPES } },
        output: { type: 'string', description: 'Optional output path (jpg/jpeg). Defaults to <stem>_AI.jpg beside the image, numbered past existing copies.' },
      },
      required: ['image_path', 'markers'],
      additionalProperties: false,
    },
  },
  {
    name: 'thermal_edit',
    description: 'Edit one radiometric thermal JPEG into a copy keeping the radiometric payload: measurement parameters (emissivity, distance, temperatures in Celsius, humidity), the displayed gain window, the palette, 90/180/270 rotation, horizontal/vertical mirror, or a text note. Sections combine in one call. Returns the edited copy read back.',
    inputSchema: {
      type: 'object',
      properties: {
        image_path: PATH_SCHEMA,
        edits: EDIT_SECTIONS,
        output: { type: 'string', description: 'Optional output path (jpg/jpeg). Defaults to <stem>_AI.jpg beside the image, numbered past existing copies.' },
      },
      required: ['image_path', 'edits'],
      additionalProperties: false,
    },
  },
]

/** One JSON-RPC request → one response record, or nothing for notifications. */
async function handleMessage(message) {
  if (message.jsonrpc !== '2.0' || typeof message.method !== 'string') return undefined
  const { id, method, params } = message
  const isRequest = id !== undefined
  try {
    if (method === 'initialize') {
      return reply(id, {
        protocolVersion: params?.protocolVersion ?? '2025-06-18',
        capabilities: { tools: { listChanged: false } },
        serverInfo: { name: 'fotric-thermal', version: '0.1.0' },
        instructions: INSTRUCTIONS,
      })
    }
    if (method === 'notifications/initialized' || method.startsWith('notifications/')) return undefined
    if (method === 'ping') return reply(id, {})
    if (method === 'tools/list') return reply(id, { tools })
    if (method === 'tools/call') {
      const name = params?.name
      const handler = handlers[name]
      if (handler === undefined) return reply(id, { content: [{ type: 'text', text: `Unknown tool: ${String(name)}` }], isError: true })
      const content = await handler(params?.arguments ?? {})
      return reply(id, { content })
    }
    if (isRequest) return reply(id, undefined, -32601, `Method not found: ${method}`)
    return undefined
  } catch (error) {
    const text = error instanceof Error ? error.message : String(error)
    if (method === 'tools/call' && isRequest) {
      return reply(id, { content: [{ type: 'text', text }], isError: true })
    }
    return isRequest ? reply(id, undefined, -32000, text) : undefined
  }
}

const reply = (id, result, code, message) =>
  id === undefined ? undefined
    : { jsonrpc: '2.0', id, ...(code === undefined ? { result } : { error: { code, message } }) }

const line = createInterface({ input: process.stdin })
line.on('line', text => {
  if (text.trim() === '') return
  let message
  try {
    message = JSON.parse(text)
  } catch (_invalidJson) {
    return
  }
  void handleMessage(message).then(response => {
    if (response !== undefined) process.stdout.write(`${JSON.stringify(response)}\n`)
  })
})

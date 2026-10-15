/**
 * The deterministic template engine: expands one report .docx from a template
 * without any model in the loop.
 *
 * The engine rests on one projection invariant, asserted per run: the body
 * story projects exactly one line per paragraph in document order, table-cell
 * paragraphs included. Phase A validates tokens and locates loop markers by
 * mapping marker paragraphs onto line indexes. Phase B performs all structural
 * work on the WordInWeb core XML tree — cloning the per-image section (a
 * bracketed block range, or a marker-carrying table row) once per image,
 * removing spent markers, and asking Word to refresh fields on open. Phase C
 * substitutes tokens through the projection's patch API; Phase D inserts each
 * image's thermal PNG at its recorded site; Phase E refuses to emit anything
 * while a `{{` residue survives.
 */
import { AgentDocument, type AgentPatchHunk, type AgentProjectResult } from '@wordinweb/agent'
import { DocxDocument, localName, parseXml, serializeXml, type XmlElement } from '@wordinweb/core'
import {
  IMAGE_LOOP_CLOSE, IMAGE_LOOP_OPEN, applyTokenValues, globalTokenValues, imageTokenValues,
  knownTokens,
} from './tokens.ts'
import type { ReportFields, ReportGenerateReason } from './types.ts'
import type { ReportTokenImage } from './tokens.ts'

/** One failed generation, with the wire-ready reason and an actionable message. */
export class ReportEngineError extends Error {
  constructor(readonly reason: ReportGenerateReason, message: string) {
    super(message)
  }
}

/** Everything the engine consumes besides the template bytes. */
export interface EngineInput {
  readonly templateBytes: Uint8Array
  readonly images: readonly ReportTokenImage[]
  readonly fields: ReportFields
  readonly date: Date
  /** Insertion width for thermal images; height keeps the natural ratio. */
  readonly imageWidthPx: number
}

/** One projection window covering every realistic report; larger templates fail loud. */
const MAX_BLOCKS = 2000
const MAX_CHARACTERS = 200_000

/** Every token-looking text in one line, braces included. */
const TOKEN_LIKE = /\{\{[^{}]*\}\}/gu

/** All `w:t` text of one element subtree, joined. */
function textOf(element: XmlElement): string {
  let out = ''
  const walk = (node: XmlElement): void => {
    if (localName(node.name) === 't') out += node.text
    for (const child of node.children) walk(child)
  }
  walk(element)
  return out
}

/** Body-story paragraphs in document order: body paragraphs and table-cell paragraphs. */
function documentParagraphs(doc: DocxDocument): XmlElement[] {
  const paragraphs: XmlElement[] = []
  const pushCellParagraphs = (element: XmlElement): void => {
    for (const child of element.children) {
      if (localName(child.name) === 'p') paragraphs.push(child)
      else pushCellParagraphs(child)
    }
  }
  for (const block of doc.sections.flatMap(section => section.blocks)) {
    if (block.src === undefined) continue
    if (localName(block.src.name) === 'p') paragraphs.push(block.src)
    else pushCellParagraphs(block.src)
  }
  return paragraphs
}

/** One text projection over one story, refusing truncation. */
function projectAll(agent: AgentDocument, story: string): AgentProjectResult {
  const projection = agent.project({ story, mode: 'text', maxBlocks: MAX_BLOCKS, maxCharacters: MAX_CHARACTERS })
  if (projection.truncated) {
    throw new ReportEngineError('template-too-large',
      'The report exceeds the projection window; generate with fewer images.')
  }
  return projection
}

/** One bracketed per-image region with its template geometry. */
interface TemplateRegion {
  /** Whether the region is a bracketed block range or a marker table row. */
  readonly kind: 'range' | 'row'
  /** Template-projection line where an instance's lines begin. */
  readonly startLine: number
  /** Lines one instance projects to; row clones keep their marker paragraphs. */
  readonly linesPerInstance: number
}

/** The validated template's stories and loop regions. */
interface ScannedTemplate {
  /** Every story id and kind, body included. */
  readonly stories: ReadonlyArray<{ readonly id: string; readonly kind: string }>
  readonly regions: readonly TemplateRegion[]
}

/** Marker paragraphs at body-block level, with their role. */
function bodyMarkerParagraphs(doc: DocxDocument): Map<XmlElement, 'open' | 'close'> {
  const markers = new Map<XmlElement, 'open' | 'close'>()
  for (const paragraph of doc.sections.flatMap(section => section.blocks)) {
    if (paragraph.type !== 'paragraph' || paragraph.src === undefined) continue
    const trimmed = textOf(paragraph.src).trim()
    if (trimmed === IMAGE_LOOP_OPEN) markers.set(paragraph.src, 'open')
    else if (trimmed === IMAGE_LOOP_CLOSE) markers.set(paragraph.src, 'close')
  }
  return markers
}

/** Table rows carrying any loop marker, with which markers they carry. */
function markerTableRows(doc: DocxDocument): Map<XmlElement, { open: boolean; close: boolean }> {
  const rows = new Map<XmlElement, { open: boolean; close: boolean }>()
  for (const block of doc.sections.flatMap(section => section.blocks)) {
    if (block.type !== 'table' || block.src === undefined) continue
    const walkRows = (element: XmlElement): void => {
      for (const child of element.children) {
        if (localName(child.name) === 'tr') {
          const text = textOf(child)
          const open = text.includes(IMAGE_LOOP_OPEN)
          const close = text.includes(IMAGE_LOOP_CLOSE)
          if (open || close) rows.set(child, { open, close })
        } else {
          walkRows(child)
        }
      }
    }
    walkRows(block.src)
  }
  return rows
}

/**
 * Validate the template's tokens and locate its loop regions. The marker
 * paragraphs are mapped onto line indexes through the document-order
 * paragraph list, which the projection invariant makes an identity map.
 * @param templateBytes - the raw template .docx.
 * @returns the story inventory and each region's template geometry.
 * @throws {ReportEngineError} on unreadable templates, unknown tokens, loop
 * markers outside the body story, or broken marker pairing.
 */
function scanTemplate(templateBytes: Uint8Array): ScannedTemplate {
  let agent: AgentDocument
  try {
    agent = AgentDocument.load(templateBytes)
  } catch (cause) {
    throw new ReportEngineError('template-invalid',
      `The template is not a readable .docx: ${cause instanceof Error ? cause.message : String(cause)}`)
  }
  const inspected = agent.inspect({ kind: 'overview' })
  if (!('stories' in inspected)) throw new ReportEngineError('template-invalid', 'The template reports no stories.')
  const stories = inspected.stories.map(story => ({ id: story.id, kind: story.kind }))

  const known = knownTokens()
  for (const story of stories) {
    if (story.kind === 'textbox') continue
    for (const [index, line] of projectAll(agent, story.id).text.split('\n').entries()) {
      if (story.id !== 'body' && (line.includes(IMAGE_LOOP_OPEN) || line.includes(IMAGE_LOOP_CLOSE))) {
        throw new ReportEngineError('template-markers',
          `Loop markers are only supported in the body, not ${story.id}.`)
      }
      for (const match of line.match(TOKEN_LIKE) ?? []) {
        if (!known.has(match)) {
          throw new ReportEngineError('unknown-token',
            `Unknown token ${match} at ${story.id === 'body' ? '' : `${story.id} `}line ${index + 1}.`)
        }
      }
    }
  }

  const core = DocxDocument.load(templateBytes)
  const paragraphs = documentParagraphs(core)
  const bodyLines = projectAll(agent, 'body').text.split('\n')
  if (paragraphs.length !== bodyLines.length) {
    throw new ReportEngineError('template-invalid', 'The template does not project one line per paragraph.')
  }
  const lineOf = new Map<XmlElement, number>()
  paragraphs.forEach((paragraph, index) => lineOf.set(paragraph, index))

  const regions: TemplateRegion[] = []
  for (const [row, markers] of markerTableRows(core)) {
    if (!markers.open || !markers.close) {
      throw new ReportEngineError('template-markers',
        'A table-row loop needs both loop markers inside the same table row.')
    }
    const rowParagraphs = paragraphs.filter(paragraph => rowContains(row, paragraph))
    const first = rowParagraphs[0]
    const startLine = first === undefined ? undefined : lineOf.get(first)
    if (startLine === undefined) throw new ReportEngineError('template-markers', 'A marker row projects to no lines.')
    // Clones keep their marker paragraphs; the text phase clears the marker
    // text, so every instance projects the row's full paragraph count.
    regions.push({ kind: 'row', startLine, linesPerInstance: rowParagraphs.length })
  }

  let pending: number | undefined
  for (const [paragraph, role] of bodyMarkerParagraphs(core)) {
    const line = lineOf.get(paragraph)
    if (line === undefined) continue
    if (role === 'open') {
      if (pending !== undefined) throw new ReportEngineError('template-markers', 'Nested loop markers are not supported.')
      pending = line
    } else {
      if (pending === undefined) throw new ReportEngineError('template-markers', 'A loop closes before it opens.')
      const linesPerInstance = line - pending - 1
      if (linesPerInstance <= 0) throw new ReportEngineError('template-markers', 'A loop brackets no content.')
      regions.push({ kind: 'range', startLine: pending + 1, linesPerInstance })
      pending = undefined
    }
  }
  if (pending !== undefined) throw new ReportEngineError('template-markers', 'A loop opens without closing.')

  regions.sort((left, right) => left.startLine - right.startLine)
  return { stories, regions }
}

/** Whether one paragraph element sits inside one row element. */
function rowContains(row: XmlElement, paragraph: XmlElement): boolean {
  for (const child of row.children) {
    if (child === paragraph) return true
    if (rowContains(child, paragraph)) return true
  }
  return false
}

/** Clone one element into an independent tree. */
function cloneElement(element: XmlElement): XmlElement {
  return parseXml(serializeXml(element))
}

/** One region's final-projection coordinates. */
interface RegionSpan {
  readonly startLine: number
  readonly linesPerInstance: number
  readonly count: number
}

/**
 * Expand every region structurally on the core XML tree. Paragraph-range
 * regions clone their bracketed blocks and drop markers and the spent
 * template range; table-row regions clone the marker row — clones keep their
 * marker paragraphs, which the text phase clears — and drop the template row.
 * @param templateBytes - the validated template.
 * @param images - the report's images, driving clone counts.
 * @param regions - the template's loop regions in document order.
 * @returns the structured bytes and each region's final line span.
 * @throws {ReportEngineError} when the XML tree disagrees with the scan.
 */
function expandStructure(
  templateBytes: Uint8Array,
  images: readonly ReportTokenImage[],
  regions: readonly TemplateRegion[],
): { bytes: Uint8Array; spans: readonly RegionSpan[] } {
  const doc = DocxDocument.load(templateBytes)
  const markerParagraphs = bodyMarkerParagraphs(doc)
  const markerRows = markerTableRows(doc)
  const expectedRegions = markerParagraphs.size / 2 + markerRows.size
  if (expectedRegions !== regions.length) {
    throw new ReportEngineError('template-markers',
      `The scan found ${regions.length} loop(s) but the template carries ${expectedRegions}.`)
  }

  for (const [row, markers] of markerRows) {
    if (!markers.open || !markers.close) {
      throw new ReportEngineError('template-markers',
        'A table-row loop needs both loop markers inside the same table row.')
    }
    const table = doc.findParentOf(row)
    if (table === undefined) throw new ReportEngineError('template-markers', 'A marker row is detached.')
    const rows = table.children
    const rowIndex = rows.indexOf(row)
    if (rowIndex === -1) throw new ReportEngineError('template-markers', 'A marker row is detached.')
    let insertAt = rowIndex + 1
    for (let copy = 0; copy < images.length; copy++) {
      rows.splice(insertAt, 0, cloneElement(row))
      insertAt++
    }
    rows.splice(rowIndex, 1)
  }

  let pending: XmlElement | undefined
  for (const paragraph of doc.sections.flatMap(section => section.blocks)) {
    if (paragraph.type !== 'paragraph' || paragraph.src === undefined) continue
    const role = markerParagraphs.get(paragraph.src)
    if (role === 'open') {
      if (pending !== undefined) throw new ReportEngineError('template-markers', 'Nested loop markers are not supported.')
      pending = paragraph.src
    } else if (role === 'close') {
      if (pending === undefined) throw new ReportEngineError('template-markers', 'A loop closes before it opens.')
      const open = pending
      pending = undefined
      const closeParent = doc.findParentOf(paragraph.src)
      const parent = doc.findParentOf(open)
      if (parent === undefined || closeParent === undefined || parent !== closeParent) {
        throw new ReportEngineError('template-markers', 'Loop markers must sit at the same body level.')
      }
      const children = parent.children
      const openIndex = children.indexOf(open)
      const closeIndex = children.indexOf(paragraph.src)
      if (openIndex === -1 || closeIndex === -1 || openIndex >= closeIndex) {
        throw new ReportEngineError('template-markers', 'Loop markers are out of document order.')
      }
      const templateElements = children.slice(openIndex + 1, closeIndex)
      let insertAt = openIndex + 1
      for (let copy = 0; copy < images.length; copy++) {
        for (const element of templateElements) {
          children.splice(insertAt, 0, cloneElement(element))
          insertAt++
        }
      }
      children.splice(children.indexOf(paragraph.src), 1)
      for (const element of templateElements) children.splice(children.indexOf(element), 1)
      children.splice(children.indexOf(open), 1)
    }
  }
  if (pending !== undefined) throw new ReportEngineError('template-markers', 'A loop opens without closing.')

  doc.setUpdateFields(true)
  doc.refresh()

  // Final line spans: a range's own open marker sits before its content, so
  // its start shifts by one, and every region shifts later regions by the
  // lines it adds — a range also deletes its two marker lines.
  const spans: RegionSpan[] = []
  let offset = 0
  for (const region of regions) {
    spans.push({
      startLine: region.startLine + offset - (region.kind === 'range' ? 1 : 0),
      linesPerInstance: region.linesPerInstance,
      count: images.length,
    })
    offset += (images.length - 1) * region.linesPerInstance - (region.kind === 'range' ? 2 : 0)
  }
  return { bytes: doc.save(), spans }
}

/** Instance index of one final-projection line, when it sits inside a region. */
function instanceOfLine(spans: readonly RegionSpan[], line: number): number | undefined {
  for (const span of spans) {
    if (line >= span.startLine && line < span.startLine + span.count * span.linesPerInstance) {
      return Math.floor((line - span.startLine) / span.linesPerInstance)
    }
  }
  return undefined
}

/** Natural pixel dimensions of one PNG image, or undefined for a foreign header. */
function pngDimensionsOf(bytes: Uint8Array): { widthPx: number; heightPx: number } | undefined {
  if (bytes.length < 24 || bytes[0] !== 0x89 || bytes[1] !== 0x50) return undefined
  const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength)
  return { widthPx: view.getUint32(16), heightPx: view.getUint32(20) }
}

/**
 * Generate one report from a validated template and measured images.
 * @param input - template bytes, images, author fields, date, and image width.
 * @returns the finished report bytes, free of every `{{` token.
 * @throws {ReportEngineError} with the wire reason at every refusal point.
 */
export async function generateReportBytes(input: EngineInput): Promise<Uint8Array> {
  const { templateBytes, images, fields, date, imageWidthPx } = input
  if (images.length === 0) throw new ReportEngineError('failed', 'A report needs at least one image.')

  const { stories, regions } = scanTemplate(templateBytes)
  if (regions.length === 0) {
    throw new ReportEngineError('no-image-loop',
      `The template has no ${IMAGE_LOOP_OPEN}…${IMAGE_LOOP_CLOSE} region; bracket one per-image section or use AI generation.`)
  }
  const { bytes: structuredBytes, spans } = expandStructure(templateBytes, images, regions)

  const agent = AgentDocument.load(structuredBytes)
  const globals = globalTokenValues({ images, fields, date })
  globals.set(IMAGE_LOOP_OPEN, '')
  globals.set(IMAGE_LOOP_CLOSE, '')

  // Text phase: body carries per-instance values; every story carries globals.
  let bodyProjection = projectAll(agent, 'body')
  const hunks: AgentPatchHunk[] = []
  /** Sites of {{image_thermal}} by final line, with their instance index. */
  const imageSites: Array<{ line: number; instance: number }> = []
  for (const [index, line] of bodyProjection.text.split('\n').entries()) {
    let replaced = line
    const instance = instanceOfLine(spans, index)
    if (instance !== undefined) {
      const image = images[instance]
      if (image !== undefined) {
        if (line.includes('{{image_thermal}}')) imageSites.push({ line: index, instance })
        replaced = applyTokenValues(replaced, imageTokenValues(image))
      }
    }
    replaced = applyTokenValues(replaced, globals)
    if (replaced !== line) hunks.push({ startLine: index + 1, endLine: index + 1, newText: replaced })
  }
  if (hunks.length > 0) {
    bodyProjection = (await agent.patch({
      revision: bodyProjection.revision, story: 'body', mode: 'text',
      maxBlocks: MAX_BLOCKS, maxCharacters: MAX_CHARACTERS, edits: hunks,
    })).projection
  }

  for (const story of stories) {
    if (story.id === 'body' || story.kind === 'textbox') continue
    const projection = projectAll(agent, story.id)
    const storyHunks: AgentPatchHunk[] = []
    for (const [index, line] of projection.text.split('\n').entries()) {
      const replaced = applyTokenValues(line, globals)
      if (replaced !== line) storyHunks.push({ startLine: index + 1, endLine: index + 1, newText: replaced })
    }
    if (storyHunks.length > 0) {
      await agent.patch({
        revision: projection.revision, story: story.id, mode: 'text',
        maxBlocks: MAX_BLOCKS, maxCharacters: MAX_CHARACTERS, edits: storyHunks,
      })
    }
  }

  // Image phase: one drawing per recorded site, batched into one transaction.
  const assetByInstance = new Map<number, { assetRef: string; widthPx: number; heightPx: number }>()
  for (const { instance } of imageSites) {
    if (assetByInstance.has(instance)) continue
    const image = images[instance]
    if (image === undefined) continue
    const base64 = image.analysis.images.thermal
    if (base64 === '') continue
    const bytes = new Uint8Array(Buffer.from(base64, 'base64'))
    const size = pngDimensionsOf(bytes)
    if (size === undefined) {
      throw new ReportEngineError('failed', `The thermal image of ${image.path} is not a decodable PNG.`)
    }
    const assetRef = agent.addAsset(bytes, 'image/png')
    assetByInstance.set(instance, {
      assetRef,
      widthPx: imageWidthPx,
      heightPx: Math.max(1, Math.round(imageWidthPx * size.heightPx / size.widthPx)),
    })
  }
  const operations = imageSites.flatMap(({ line, instance }): Array<{
    kind: 'insertImage'
    runRef: string
    assetRef: string
    widthPx: number
    heightPx: number
  }> => {
    const asset = assetByInstance.get(instance)
    const anchor = bodyProjection.anchors[line]
    const runRef = anchor?.segments.at(-1)?.runRef ?? anchor?.segments[0]?.runRef
    if (asset === undefined || anchor === undefined || runRef === undefined) return []
    return [{ kind: 'insertImage', runRef, assetRef: asset.assetRef, widthPx: asset.widthPx, heightPx: asset.heightPx }]
  })
  if (operations.length > 0) {
    await agent.edit({ revision: agent.revision, operations })
  }

  // Residue refusal: nothing is emitted while any {{ survives anywhere.
  for (const story of stories) {
    if (story.kind === 'textbox') continue
    const residue = projectAll(agent, story.id).text.split('\n').filter(line => line.includes('{{'))
    if (residue.length > 0) {
      throw new ReportEngineError('residue',
        `Unreplaced tokens remain (${residue[0]?.trim() ?? ''}…); the report was not written.`)
    }
  }
  return agent.save()
}

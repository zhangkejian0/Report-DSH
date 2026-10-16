/**
 * The deterministic template engine: expands one report .docx from a template
 * without any model in the loop.
 *
 * The engine rests on one projection invariant, asserted per run: the body
 * story projects exactly one line per paragraph in document order, table-cell
 * paragraphs included. Phase A validates tokens against the merged field
 * bindings (built-in vocabulary plus the template's sidecar) and locates loop
 * markers by mapping marker paragraphs onto line indexes. Phase B performs
 * all structural work on the WordInWeb core XML tree — cloning the per-image
 * section (a bracketed block range, or a marker-carrying table row) once per
 * image, cloning the flattened marker-detail row once per image–marker pair,
 * removing spent markers, and asking Word to refresh fields on open. Phase C
 * substitutes tokens through the projection's patch API, each line against
 * its scope's bindings; Phase D inserts each image's thermal PNG at its
 * recorded site; Phase E refuses to emit anything while a `{{` residue
 * survives.
 */
import { AgentDocument, type AgentPatchHunk, type AgentProjectResult } from '@wordinweb/agent'
import { DocxDocument, localName, parseXml, serializeXml, type XmlElement } from '@wordinweb/core'
import {
  IMAGE_LOOP_CLOSE, IMAGE_LOOP_OPEN, MARKER_LOOP_CLOSE, MARKER_LOOP_OPEN,
  BUILTIN_FIELD_CONFIGURATION, type ReportTokenImage,
} from './tokens.ts'
import {
  looksLikePathToken, mergeFieldConfiguration, resolveToken,
  type Aggregates, type FieldConfiguration, type MergedFields, type ResolveScope,
} from './bindings.ts'
import type { ReportGenerateReason } from './types.ts'

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
  /** Author-supplied values keyed by token text. */
  readonly fields: Readonly<Record<string, string>>
  readonly date: Date
  /** Insertion width for thermal images; height keeps the natural ratio. */
  readonly imageWidthPx: number
  /** The template's sidecar field configuration, when one exists. */
  readonly fieldConfiguration?: FieldConfiguration
}

/** One projection window covering every realistic report; larger templates fail loud. */
const MAX_BLOCKS = 2000
const MAX_CHARACTERS = 200_000

/** Every token-looking text in one line, braces included. */
const TOKEN_LIKE = /\{\{[^{}]*\}\}/gu

/** The image slot token; the engine inserts a drawing at its site. */
const IMAGE_THERMAL = '{{image_thermal}}'

/** Token texts cleared wherever they appear; expansion removes their sections. */
const STRUCTURAL_CLEARS = [IMAGE_LOOP_OPEN, IMAGE_LOOP_CLOSE, MARKER_LOOP_OPEN, MARKER_LOOP_CLOSE, IMAGE_THERMAL] as const

/** One image and one of its measured markers, as a marker row instance. */
interface MarkerPair {
  readonly image: ReportTokenImage
  readonly marker: ReportTokenImage['analysis']['markers'][number]
}

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

/** One bracketed loop region with its template geometry. */
interface TemplateRegion {
  /** Range (body blocks), image row (table row per image), or marker row (per image–marker pair). */
  readonly kind: 'range' | 'row' | 'markerRow'
  /** Template-projection line where an instance's lines begin. */
  readonly startLine: number
  /** Lines one instance projects to; row clones keep their marker paragraphs. */
  readonly linesPerInstance: number
}

/** The validated template's stories, loop regions, and merged fields. */
interface ScannedTemplate {
  /** Every story id and kind, body included. */
  readonly stories: ReadonlyArray<{ readonly id: string; readonly kind: string }>
  readonly regions: readonly TemplateRegion[]
  readonly merged: MergedFields
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

/** Which loop markers one table row carries, by loop kind. */
interface RowMarkers { readonly open: string | undefined; readonly close: string | undefined }

/** Table rows carrying loop markers of one kind, with which markers they carry. */
function loopTableRows(doc: DocxDocument, openMarker: string, closeMarker: string): Map<XmlElement, RowMarkers> {
  const rows = new Map<XmlElement, RowMarkers>()
  for (const block of doc.sections.flatMap(section => section.blocks)) {
    if (block.type !== 'table' || block.src === undefined) continue
    const walkRows = (element: XmlElement): void => {
      for (const child of element.children) {
        if (localName(child.name) === 'tr') {
          const text = textOf(child)
          const open = text.includes(openMarker) ? openMarker : undefined
          const close = text.includes(closeMarker) ? closeMarker : undefined
          if (open !== undefined || close !== undefined) rows.set(child, { open, close })
        } else {
          walkRows(child)
        }
      }
    }
    walkRows(block.src)
  }
  return rows
}

/** Whether every value in one row-marker pair is present. */
function completeRow(markers: RowMarkers | undefined): boolean {
  return markers?.open !== undefined && markers.close !== undefined
}

/**
 * Validate the template's tokens and locate its loop regions. The marker
 * paragraphs are mapped onto line indexes through the document-order
 * paragraph list, which the projection invariant makes an identity map.
 * @param templateBytes - the raw template .docx.
 * @param fieldConfiguration - the template's sidecar configuration, if any.
 * @returns the story inventory, each region's template geometry, and merged fields.
 * @throws {ReportEngineError} on unreadable templates, unknown tokens, loop
 * markers outside the body story, or broken marker pairing.
 */
function scanTemplate(templateBytes: Uint8Array, fieldConfiguration: FieldConfiguration | undefined): ScannedTemplate {
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

  const merged = mergeFieldConfiguration(BUILTIN_FIELD_CONFIGURATION, fieldConfiguration)
  const known = new Set<string>([
    ...Object.keys(merged.fields).map(token => `{{${token}}}`),
    IMAGE_LOOP_OPEN, IMAGE_LOOP_CLOSE, MARKER_LOOP_OPEN, MARKER_LOOP_CLOSE, IMAGE_THERMAL,
  ])
  for (const story of stories) {
    if (story.kind === 'textbox') continue
    for (const [index, line] of projectAll(agent, story.id).text.split('\n').entries()) {
      for (const marker of [IMAGE_LOOP_OPEN, IMAGE_LOOP_CLOSE, MARKER_LOOP_OPEN, MARKER_LOOP_CLOSE]) {
        if (story.id !== 'body' && line.includes(marker)) {
          throw new ReportEngineError('template-markers',
            `Loop markers are only supported in the body, not ${story.id}.`)
        }
      }
      for (const match of line.match(TOKEN_LIKE) ?? []) {
        const token = match.slice(2, -2).trim()
        if (!known.has(`{{${token}}}`) && !looksLikePathToken(token)) {
          const available = Object.keys(merged.fields).slice(0, 12).join(' ')
          throw new ReportEngineError('unknown-token',
            `Unknown token ${match} at ${story.id === 'body' ? '' : `${story.id} `}line ${index + 1}`
            + `; available fields: ${available}${Object.keys(merged.fields).length > 12 ? ' …' : ''}.`)
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
  for (const [row, markers] of loopTableRows(core, IMAGE_LOOP_OPEN, IMAGE_LOOP_CLOSE)) {
    if (!completeRow(markers)) {
      throw new ReportEngineError('template-markers',
        'A table-row loop needs both loop markers inside the same table row.')
    }
    regions.push({ kind: 'row', ...rowGeometry(row, paragraphs, lineOf) })
  }
  for (const [row, markers] of loopTableRows(core, MARKER_LOOP_OPEN, MARKER_LOOP_CLOSE)) {
    if (!completeRow(markers)) {
      throw new ReportEngineError('template-markers',
        'A marker-row loop needs both loop markers inside the same table row.')
    }
    regions.push({ kind: 'markerRow', ...rowGeometry(row, paragraphs, lineOf) })
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

  // Marker rows iterate a flattened pair list, so they cannot sit inside a
  // per-image section whose clone count differs; keep them outside ranges.
  const markerRows = regions.filter(region => region.kind === 'markerRow')
  const ranges = regions.filter(region => region.kind === 'range')
  for (const markerRow of markerRows) {
    for (const range of ranges) {
      if (markerRow.startLine > range.startLine && markerRow.startLine < range.startLine + range.linesPerInstance) {
        throw new ReportEngineError('template-markers',
          'A marker-row loop must sit outside the per-image section; it repeats over every image itself.')
      }
    }
  }

  regions.sort((left, right) => left.startLine - right.startLine)
  return { stories, regions, merged }
}

/** One marker table row's start line and per-instance line count. */
function rowGeometry(row: XmlElement, paragraphs: readonly XmlElement[], lineOf: Map<XmlElement, number>):
{ startLine: number; linesPerInstance: number } {
  const rowParagraphs = paragraphs.filter(paragraph => rowContains(row, paragraph))
  const first = rowParagraphs[0]
  const startLine = first === undefined ? undefined : lineOf.get(first)
  if (startLine === undefined) throw new ReportEngineError('template-markers', 'A marker row projects to no lines.')
  // Clones keep their marker paragraphs; the text phase clears the marker
  // text, so every instance projects the row's full paragraph count.
  return { startLine, linesPerInstance: rowParagraphs.length }
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
  /** Which instance list the region iterates. */
  readonly variant: 'image' | 'marker'
}

/**
 * Expand every region structurally on the core XML tree. Paragraph-range
 * regions clone their bracketed blocks and drop markers and the spent
 * template range; table-row regions clone the marker row — clones keep their
 * marker paragraphs, which the text phase clears — and drop the template row;
 * marker rows clone once per image–marker pair.
 * @param templateBytes - the validated template.
 * @param images - the report's images, driving image clone counts.
 * @param markerPairs - the flattened image–marker pairs, driving marker-row counts.
 * @param regions - the template's loop regions in document order.
 * @returns the structured bytes and each region's final line span.
 * @throws {ReportEngineError} when the XML tree disagrees with the scan.
 */
function expandStructure(
  templateBytes: Uint8Array,
  images: readonly ReportTokenImage[],
  markerPairs: readonly MarkerPair[],
  regions: readonly TemplateRegion[],
): { bytes: Uint8Array; spans: readonly RegionSpan[] } {
  const doc = DocxDocument.load(templateBytes)
  const markerParagraphs = bodyMarkerParagraphs(doc)
  const imageRows = loopTableRows(doc, IMAGE_LOOP_OPEN, IMAGE_LOOP_CLOSE)
  const markerRows = loopTableRows(doc, MARKER_LOOP_OPEN, MARKER_LOOP_CLOSE)
  const expectedRegions = markerParagraphs.size / 2 + imageRows.size + markerRows.size
  if (expectedRegions !== regions.length) {
    throw new ReportEngineError('template-markers',
      `The scan found ${regions.length} loop(s) but the template carries ${expectedRegions}.`)
  }

  for (const [row, markers] of imageRows) {
    if (!completeRow(markers)) {
      throw new ReportEngineError('template-markers',
        'A table-row loop needs both loop markers inside the same table row.')
    }
    expandTableRow(doc, row, images.length)
  }
  for (const [row, markers] of markerRows) {
    if (!completeRow(markers)) {
      throw new ReportEngineError('template-markers',
        'A marker-row loop needs both loop markers inside the same table row.')
    }
    expandTableRow(doc, row, markerPairs.length)
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
    const count = region.kind === 'markerRow' ? markerPairs.length : images.length
    spans.push({
      startLine: region.startLine + offset - (region.kind === 'range' ? 1 : 0),
      linesPerInstance: region.linesPerInstance,
      count,
      variant: region.kind === 'markerRow' ? 'marker' : 'image',
    })
    offset += (count - 1) * region.linesPerInstance - (region.kind === 'range' ? 2 : 0)
  }
  return { bytes: doc.save(), spans }
}

/** Clone one marker table row in place once per instance, then drop the template row. */
function expandTableRow(doc: DocxDocument, row: XmlElement, count: number): void {
  const table = doc.findParentOf(row)
  if (table === undefined) throw new ReportEngineError('template-markers', 'A marker row is detached.')
  const rows = table.children
  const rowIndex = rows.indexOf(row)
  if (rowIndex === -1) throw new ReportEngineError('template-markers', 'A marker row is detached.')
  let insertAt = rowIndex + 1
  for (let copy = 0; copy < count; copy++) {
    rows.splice(insertAt, 0, cloneElement(row))
    insertAt++
  }
  rows.splice(rowIndex, 1)
}

/** The instance a final-projection line belongs to, when it sits inside a region. */
function lineInstance(spans: readonly RegionSpan[], line: number): { variant: 'image' | 'marker'; instance: number } | undefined {
  for (const span of spans) {
    if (line >= span.startLine && line < span.startLine + span.count * span.linesPerInstance) {
      return { variant: span.variant, instance: Math.floor((line - span.startLine) / span.linesPerInstance) }
    }
  }
  return undefined
}

/** Whole-report aggregates the global scope exposes by path. */
function aggregatesOf(images: readonly ReportTokenImage[]): Aggregates {
  const hottest = images.reduce((best, image) => image.analysis.stats.max > best.analysis.stats.max ? image : best)
  const maxOf = (pick: (image: ReportTokenImage) => number): number => Math.max(...images.map(pick))
  const minOf = (pick: (image: ReportTokenImage) => number): number => Math.min(...images.map(pick))
  const avgOf = (pick: (image: ReportTokenImage) => number): number =>
    images.map(pick).reduce((sum, value) => sum + value, 0) / images.length
  return {
    count: images.length,
    overall: {
      max: maxOf(image => image.analysis.stats.max),
      min: minOf(image => image.analysis.stats.min),
      avg: avgOf(image => image.analysis.stats.avg),
      maxAt: hottest.analysis.stats.maxAt,
      maxImage: hottest.path,
    },
  }
}

/** Substitute one line's tokens against the merged fields and the line's scope. */
function substituteLine(line: string, merged: MergedFields, scope: ResolveScope): string {
  let out = line
  for (const structural of STRUCTURAL_CLEARS) out = out.replaceAll(structural, '')
  return out.replace(TOKEN_LIKE, match => resolveToken(match.slice(2, -2).trim(), merged, scope) ?? match)
}

/** Natural pixel dimensions of one PNG image, or undefined for a foreign header. */
function pngDimensionsOf(bytes: Uint8Array): { widthPx: number; heightPx: number } | undefined {
  if (bytes.length < 24 || bytes[0] !== 0x89 || bytes[1] !== 0x50) return undefined
  const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength)
  return { widthPx: view.getUint32(16), heightPx: view.getUint32(20) }
}

/**
 * Generate one report from a validated template and measured images.
 * @param input - template bytes, images, author fields, date, image width,
 * and the template's sidecar field configuration.
 * @returns the finished report bytes, free of every `{{` token.
 * @throws {ReportEngineError} with the wire reason at every refusal point.
 */
export async function generateReportBytes(input: EngineInput): Promise<Uint8Array> {
  const { templateBytes, images, fields, date, imageWidthPx, fieldConfiguration } = input
  if (images.length === 0) throw new ReportEngineError('failed', 'A report needs at least one image.')

  const { stories, regions, merged } = scanTemplate(templateBytes, fieldConfiguration)
  if (!regions.some(region => region.kind !== 'markerRow')) {
    throw new ReportEngineError('no-image-loop',
      `The template has no ${IMAGE_LOOP_OPEN}…${IMAGE_LOOP_CLOSE} region; bracket one per-image section or use AI generation.`)
  }
  const markerPairs: MarkerPair[] = images.flatMap(image =>
    image.analysis.markers.filter(marker => marker.name !== 'global').map(marker => ({ image, marker })))
  const { bytes: structuredBytes, spans } = expandStructure(templateBytes, images, markerPairs, regions)

  const agent = AgentDocument.load(structuredBytes)
  const aggregates = aggregatesOf(images)
  const globalScope: ResolveScope = { kind: 'global', inputs: fields, date, aggregates,
    ...(images[0] === undefined ? {} : { image: images[0] }) }

  // Text phase: body lines resolve against their own scope; other stories global.
  let bodyProjection = projectAll(agent, 'body')
  const hunks: AgentPatchHunk[] = []
  /** Sites of the image slot by final line, with their image instance. */
  const imageSites: Array<{ line: number; instance: number }> = []
  for (const [index, line] of bodyProjection.text.split('\n').entries()) {
    const site = lineInstance(spans, index)
    let scope = globalScope
    if (site?.variant === 'image' && images[site.instance] !== undefined) {
      const lineImage = images[site.instance]
      if (lineImage !== undefined) scope = { kind: 'image', inputs: fields, date, image: lineImage }
      if (line.includes(IMAGE_THERMAL)) imageSites.push({ line: index, instance: site.instance })
    } else if (site?.variant === 'marker') {
      const pair = markerPairs[site.instance]
      if (pair !== undefined) scope = { kind: 'marker', inputs: fields, date, image: pair.image, marker: pair.marker }
    }
    const replaced = substituteLine(line, merged, scope)
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
      const replaced = substituteLine(line, merged, globalScope)
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

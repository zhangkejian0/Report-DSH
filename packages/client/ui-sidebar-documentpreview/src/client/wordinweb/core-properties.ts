/** Write edited data-bound content controls back into the core properties they bind. */
import { unzipSync, zipSync, type Unzipped } from 'fflate'

/** OPC store id of the core-properties item that `w:dataBinding` reads and writes. */
const CORE_PROPERTIES_STORE_ID = '{6C3C8BC8-F283-45AE-878A-BAB7291924A1}'

/** Core-property element each bound building-block control writes, keyed by the xpath's final local name. */
const BOUND_ELEMENTS: Record<string, string> = {
  title: 'dc:title',
  subject: 'dc:subject',
  keywords: 'cp:keywords',
  category: 'cp:category',
  comments: 'dc:description',
}

/** One property update: the element to rewrite and the text the document now carries. */
interface PropertyUpdate {
  readonly element: string
  readonly value: string
}

/**
 * Sync `docProps/core.xml` with the text of every content control bound to it.
 *
 * A building-block control such as a document title stores its text twice: in
 * `word/document.xml` inside `w:sdtContent`, and in the core property named by
 * its `w:dataBinding` xpath. Readers resolve the binding on load, so editing
 * the control without updating the property reverts the edit on the next open.
 * The OOXML editor writes only the body copy, so this writes the property side.
 *
 * @param bytes - The document the editor just serialized.
 * @returns The input unchanged when nothing is bound or every property already agrees,
 * otherwise the same package with the bound core properties rewritten.
 */
export function reconcileBoundCoreProperties(bytes: Uint8Array<ArrayBuffer>): Uint8Array<ArrayBuffer> {
  const archive = unzipSync(bytes)
  const documentPart = findPart(archive, 'word/document.xml')
  const corePart = findPart(archive, 'docProps/core.xml')
  if (documentPart === undefined || corePart === undefined) return bytes
  const updates = collectUpdates(decodeXml(documentPart.data))
  if (updates.length === 0) return bytes
  let core = decodeXml(corePart.data)
  let changed = false
  for (const update of updates) {
    const replaced = rewriteElement(core, update.element, update.value)
    if (replaced === core) continue
    core = replaced
    changed = true
  }
  if (!changed) return bytes
  return new Uint8Array(zipSync({ ...archive, [corePart.name]: encodeXml(core) }, { level: 6 }))
}

/** Collect the property writes one serialized document calls for. */
function collectUpdates(documentXml: string): PropertyUpdate[] {
  const updates: PropertyUpdate[] = []
  const binding = /<w:dataBinding\b([^>]*)\/>/g
  for (const match of documentXml.matchAll(binding)) {
    const attributes = match[1] ?? ''
    if (!attributes.toLowerCase().includes(`w:storeitemid="${CORE_PROPERTIES_STORE_ID.toLowerCase()}"`)) continue
    const element = boundElement(attributes)
    if (element === undefined) continue
    const value = controlText(documentXml, match.index + match[0].length)
    if (value !== undefined) updates.push({ element, value })
  }
  return updates
}

/** Resolve the core-property element one binding's xpath targets, or undefined when unsupported. */
function boundElement(attributes: string): string | undefined {
  const xpath = /w:xpath="([^"]*)"/.exec(attributes)?.[1]
  if (xpath === undefined) return undefined
  const localName = /\/(?:[A-Za-z0-9_]+:)?([A-Za-z0-9_-]+)(?:\[\d+\])?$/.exec(xpath)?.[1]
  return localName === undefined ? undefined : BOUND_ELEMENTS[localName.toLowerCase()]
}

/**
 * Read the text a control carries, from the `w:sdtContent` following one binding.
 * @param documentXml - Complete main document part.
 * @param from - Offset just past the `w:dataBinding` element.
 * @returns The concatenated runs' text, or undefined when the control has no content.
 */
function controlText(documentXml: string, from: number): string | undefined {
  const open = documentXml.indexOf('<w:sdtContent', from)
  const close = open === -1 ? -1 : documentXml.indexOf('</w:sdtContent>', open)
  if (close === -1) return undefined
  let text = ''
  for (const run of documentXml.slice(open, close).matchAll(/<w:t(?:\s[^>]*)?>([\s\S]*?)<\/w:t>/g)) {
    text += decodeEntities(run[1] ?? '')
  }
  return text
}

/** Replace one core-property element's text, expanding a self-closing element when it carries text. */
function rewriteElement(coreXml: string, element: string, value: string): string {
  const escaped = encodeEntities(value)
  if (escaped === '') return coreXml
  const paired = new RegExp(`(<${element}\\s*)([^>]*?)(>)([\\s\\S]*?)(</${element}\\s*>)`)
  if (paired.test(coreXml)) return coreXml.replace(paired, `$1$2$3${escaped}$5`)
  return coreXml.replace(new RegExp(`<${element}\\s*([^>]*?)/>`), `<${element}$1>${escaped}</${element}>`)
}

/** Locate a part ignoring ASCII case, as OPC name comparison does, keeping the archived name for re-zipping. */
function findPart(archive: Unzipped, wanted: string): { readonly name: string; readonly data: Uint8Array } | undefined {
  const lower = wanted.toLowerCase()
  const name = Object.keys(archive).find(part => part.toLowerCase() === lower)
  const data = name === undefined ? undefined : archive[name]
  return name === undefined || data === undefined ? undefined : { name, data }
}

/** Decode the XML encodings an OPC part may use. */
function decodeXml(bytes: Uint8Array): string {
  const encoding = (bytes[0] === 0xff && bytes[1] === 0xfe) || (bytes[0] === 0x3c && bytes[1] === 0)
    ? 'utf-16le'
    : (bytes[0] === 0xfe && bytes[1] === 0xff) || (bytes[0] === 0 && bytes[1] === 0x3c)
      ? 'utf-16be' : 'utf-8'
  return new TextDecoder(encoding).decode(bytes)
}

function encodeXml(text: string): Uint8Array {
  return new TextEncoder().encode(text)
}

/** Restore the five XML predefined entities in control text. */
function decodeEntities(text: string): string {
  return text.replace(/&(lt|gt|amp|quot|apos);/g, (entity, name: string) => (
    { lt: '<', gt: '>', amp: '&', quot: '"', apos: '\'' })[name] ?? entity)
}

function encodeEntities(text: string): string {
  return text.replace(/[&<>"]/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[character] ?? character)
}

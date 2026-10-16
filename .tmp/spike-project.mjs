/** M0 spike step 1: load the sample template and dump its text projection with anchors. */
import { readFileSync, writeFileSync } from 'node:fs'
import { pathToFileURL } from 'node:url'

const agentUrl = pathToFileURL('D:/agworkspace/docgc/wordinweb/packages/agent/dist/index.js').href
const { AgentDocument } = await import(agentUrl)

const src = readFileSync('D:/agworkspace/docgc/deepseek-harness/packages/client/ui-sidebar-documentpreview/src/client/report/sample-template.ts', 'utf8')
const arrayMatch = /SAMPLE_TEMPLATE_BASE64 = \[([\s\S]*?)\]/.exec(src)
if (arrayMatch === null) throw new Error('sample base64 not found')
const realBase64 = Array.from(arrayMatch[1].matchAll(/'([^']*)'/g), m => m[1]).join('')

const bytes = Buffer.from(realBase64, 'base64')
writeFileSync('D:/agworkspace/docgc/deepseek-harness/.tmp/spike-template.docx', bytes)

const doc = AgentDocument.load(new Uint8Array(bytes))
const projection = doc.project({ mode: 'text' })
console.log('story:', projection.story, 'lines:', projection.lines, 'truncated:', projection.truncated)
projection.text.split('\n').forEach((text, index) => {
  const anchor = projection.anchors[index]
  const refs = anchor?.segments.map(s => `${s.runRef}[${s.start},${s.end})${s.editable ? '' : '!'}`).join(' ')
  console.log(String(index).padStart(3), JSON.stringify(text), anchor ? `role=${anchor.role} block=${anchor.blockRef ?? '-'} mk=${anchor.marker} ${refs ?? ''}` : 'NO-ANCHOR')
})
console.log('revision:', doc.revision)

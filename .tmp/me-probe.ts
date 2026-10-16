import { readFileSync } from 'node:fs'
import { markdownHeadingLines, markdownProseLines } from '../scripts/markdown.ts'

for (const file of ['packages/document/office-to-pdf/README.md', 'packages/document/thermal-analyze/README.md', 'packages/document/tool-wordinweb/README.md']) {
  const text = readFileSync(file, 'utf8')
  const lines = markdownProseLines(text)
  const headings = markdownHeadingLines(text)
  const h2s = headings.filter(h => h.depth === 2)
  const me = headings.find(h => h.depth === 2 && h.text.trim().toLowerCase() === 'model experience')
  if (me === undefined) { console.log(file, 'NO ME'); continue }
  const meAt = lines.findIndex(l => l.index === me.index)
  const body = lines.slice(meAt + 1)
  const h2Set = new Set(h2s.map(h => h.index))
  const nextH2 = body.findIndex(l => h2Set.has(l.index))
  const section = nextH2 < 0 ? body : body.slice(0, nextH2)
  console.log('=====', file, 'ME at raw line', me.index)
  for (const l of section) console.log(`  [${l.index}] ${JSON.stringify(l.raw.slice(0, 90))}`)
}

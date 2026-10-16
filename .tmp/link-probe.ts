import { readFileSync } from 'node:fs'
import { translationLinkLocaleViolations } from '../scripts/translation-links.ts'
import { languageSwitcherTargets } from '../scripts/translation-pairing.ts'
import { existsSync, statSync } from 'node:fs'
import { join } from 'node:path'

const root = process.cwd()
const repositoryFileExists = (f: string) => existsSync(join(root, f)) && statSync(join(root, f)).isFile()

for (const zh of ['packages/document/thermal-analyze/README.zh.md', 'packages/document/tool-wordinweb/README.zh.md']) {
  const source = zh.replace('.zh.md', '.md')
  const text = readFileSync(zh, 'utf8')
  const v = translationLinkLocaleViolations(text, {
    repoRoot: root, sourcePath: zh,
    isTranslationPairSource: () => true,
    repositoryFileExists,
  }, languageSwitcherTargets(source))
  console.log(zh, JSON.stringify(v))
}

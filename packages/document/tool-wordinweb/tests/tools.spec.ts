import { mkdtemp, readFile, rm, stat } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { afterEach, describe, expect, it } from 'vitest'
import { Context } from '@deepseek-ai/cordis'
import { ToolCallId } from '@deepseek-ai/dsh-llm'
import SystemPrompt from '@deepseek-ai/dsh-system-prompt'
import ToolRuntime from '@deepseek-ai/dsh-tools'
import * as ToolWordinweb from '@deepseek-ai/dsh-tool-wordinweb'

const contexts: Context[] = []
const roots: string[] = []
let callNumber = 0

afterEach(async () => {
  for (const ctx of contexts.splice(0)) await ctx.fiber.dispose()
  for (const root of roots.splice(0)) await rm(root, { recursive: true, force: true })
})

interface ToolResult { content: { type: string; text?: string }[]; isError?: boolean }

async function call(ctx: Context, name: string, args: unknown): Promise<any> {
  const result: ToolResult = await ctx.tools.execute({
    signal: new AbortController().signal,
    callId: ToolCallId(`wordinweb-${++callNumber}`),
    name,
    arguments: args,
  })
  const text = (result.content ?? []).filter(block => block.type === 'text').map(block => block.text).join('')
  if (result.isError) throw new Error(text)
  return JSON.parse(text)
}

async function setup(config: ToolWordinweb.Config = {}): Promise<Context> {
  const ctx = new Context()
  contexts.push(ctx)
  await ctx.plugin(SystemPrompt)
  await ctx.plugin(ToolRuntime)
  await ctx.plugin(ToolWordinweb, config)
  return ctx
}

describe('tool-wordinweb', () => {
  it('registers the ten word_document tools', async () => {
    const ctx = await setup()
    const names = ctx.tools.schemas().map(schema => schema.name).sort()
    expect(names).toEqual([...ToolWordinweb.WORD_DOCUMENT_TOOL_NAMES].sort())
  })

  it('composes, saves, reopens, and patches a Chinese report', async () => {
    const root = await mkdtemp(join(tmpdir(), 'dsh-tool-wordinweb-'))
    roots.push(root)
    const ctx = await setup({ workspace: root })

    await call(ctx, 'word_document_new', {})
    const composed = await call(ctx, 'word_document_compose', {
      page: { margins: { top: 0.75, right: 0.75, bottom: 0.75, left: 0.75 } },
      body: [
        { type: 'paragraph', text: '定制化报告验证样例', styleId: 'Title' },
        { type: 'heading', level: 1, text: '一、项目概述' },
        { type: 'paragraph', text: '由 dsh-tool-wordinweb 生成，验证中文排版、表格与图表。' },
        {
          type: 'table',
          headerRows: 1,
          rows: [['指标', '本周', '上周'], ['交付进度', '82%', '64%']],
        },
      ],
      footer: [{ type: 'pageNumber', fieldKind: 'pageOfTotal', align: 'center', fontSizePt: 8 }],
    })
    expect(composed.overview?.outline?.length).toBeGreaterThan(0)

    const saved = await call(ctx, 'word_document_save', { path: 'roundtrip.docx' })
    expect(saved.saved).toContain(join(root, 'roundtrip.docx'))
    expect((await stat(join(root, 'roundtrip.docx'))).size).toBeGreaterThan(0)

    const opened = await call(ctx, 'word_document_open', { path: 'roundtrip.docx' })
    expect(Array.isArray(opened.stories)).toBe(true)

    const projection = await call(ctx, 'word_document_project', { story: 'body', mode: 'text' })
    expect(String(projection.text)).toContain('定制化报告验证样例')

    const patched = await call(ctx, 'word_document_patch', {
      mode: 'text',
      edits: [{ startLine: 1, endLine: 1, newText: '定制化报告验证样例（已修订）' }],
    })
    expect(patched.status).toBeDefined()

    await call(ctx, 'word_document_save', {})
    const bytes = await readFile(join(root, 'roundtrip.docx'))
    expect(bytes.length).toBeGreaterThan(0)
  })

  it('requires a loaded document before editing', async () => {
    const ctx = await setup()
    await expect(call(ctx, 'word_document_inspect', { kind: 'overview' })).rejects.toThrow(/No document loaded/)
  })
})

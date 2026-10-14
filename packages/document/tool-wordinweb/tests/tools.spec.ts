import { mkdtemp, readFile, rm, stat, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { afterEach, describe, expect, it } from 'vitest'
import { Context } from '@deepseek-ai/cordis'
import { ToolCallId } from '@deepseek-ai/dsh-llm'
import type { Agent } from '@deepseek-ai/dsh-agent'
import type { SessionId } from '@deepseek-ai/dsh-session/types'
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

async function call<T>(ctx: Context, name: string, args: unknown, agent?: Agent): Promise<T> {
  const result: ToolResult = await ctx.tools.execute({
    signal: new AbortController().signal,
    callId: ToolCallId(`wordinweb-${++callNumber}`),
    name,
    arguments: args,
    ...agent === undefined ? {} : { agent },
  })
  const text = (result.content ?? []).filter(block => block.type === 'text').map(block => block.text).join('')
  if (result.isError) throw new Error(text)
  return JSON.parse(text) as T
}

/** One session-keyed agent identity for scoped calls. */
function agentOf(id: string): Agent {
  return { id: id as SessionId } as Agent
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
  it('registers the eleven word_document tools', async () => {
    const ctx = await setup()
    const names = ctx.tools.schemas().map(schema => schema.name).sort()
    expect(names).toEqual([...ToolWordinweb.WORD_DOCUMENT_TOOL_NAMES].sort())
  })

  it('composes, saves, reopens, and patches a Chinese report', async () => {
    const root = await mkdtemp(join(tmpdir(), 'dsh-tool-wordinweb-'))
    roots.push(root)
    const ctx = await setup({ workspace: root })

    await call(ctx, 'word_document_new', {})
    const composed = await call<{ overview: { outline: readonly unknown[] } }>(ctx, 'word_document_compose', {
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

    const saved = await call<{ saved: string }>(ctx, 'word_document_save', { path: 'roundtrip.docx' })
    expect(saved.saved).toContain(join(root, 'roundtrip.docx'))
    expect((await stat(join(root, 'roundtrip.docx'))).size).toBeGreaterThan(0)

    const opened = await call<{ stories: unknown }>(ctx, 'word_document_open', { path: 'roundtrip.docx' })
    expect(Array.isArray(opened.stories)).toBe(true)

    const projection = await call<{ text: string }>(ctx, 'word_document_project', { story: 'body', mode: 'text' })
    expect(String(projection.text)).toContain('定制化报告验证样例')

    const patched = await call<{ status: unknown }>(ctx, 'word_document_patch', {
      mode: 'text',
      edits: [{ startLine: 1, endLine: 1, newText: '定制化报告验证样例（已修订）' }],
    })
    expect(patched.status).toBeDefined()

    await call(ctx, 'word_document_save', {})
    const bytes = await readFile(join(root, 'roundtrip.docx'))
    expect(bytes.length).toBeGreaterThan(0)
  })

  it('registers an on-disk image and composes it into the document', async () => {
    const root = await mkdtemp(join(tmpdir(), 'dsh-tool-wordinweb-'))
    roots.push(root)
    const ctx = await setup({ workspace: root })
    // 1x1 transparent PNG.
    const png = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==', 'base64')
    const source = join(root, 'thermal.png')
    await writeFile(source, png)

    await call(ctx, 'word_document_new', {})
    const added = await call<{ assetRef: string; naturalWidthPx: number; naturalHeightPx: number }>(ctx, 'word_document_add_image', { path: 'thermal.png' })
    expect(added.assetRef).toMatch(/^asset:/)
    expect(added.naturalWidthPx).toBe(1)
    expect(added.naturalHeightPx).toBe(1)

    const composed = await call<{ status: string }>(ctx, 'word_document_compose', {
      body: [
        { type: 'heading', level: 1, text: '热像分析' },
        { type: 'image', assetRef: added.assetRef, widthPx: 1, heightPx: 1, alt: 'thermal' },
      ],
    })
    expect(composed.status).toBe('applied')

    const saved = await call<{ saved: string; bytes: number }>(ctx, 'word_document_save', { path: 'with-image.docx' })
    expect(saved.bytes).toBeGreaterThan(png.length)
    const reopened = await call<unknown>(ctx, 'word_document_open', { path: 'with-image.docx' })
    expect(JSON.stringify(reopened)).toContain('image')
  })

  it('requires a loaded document before editing', async () => {
    const ctx = await setup()
    await expect(call(ctx, 'word_document_inspect', { kind: 'overview' })).rejects.toThrow(/No document loaded/)
  })

  it('isolates concurrent agents on independent documents', async () => {
    const root = await mkdtemp(join(tmpdir(), 'dsh-tool-wordinweb-'))
    roots.push(root)
    const ctx = await setup({ workspace: root })
    const first = agentOf('session-first')
    const second = agentOf('session-second')

    for (const [agent, text] of [[first, '第一会话文档'] as const, [second, '第二会话文档'] as const]) {
      await call(ctx, 'word_document_new', {}, agent)
      await call(ctx, 'word_document_compose', { body: [{ type: 'paragraph', text }] }, agent)
    }

    const firstText = await call<{ text: string }>(ctx, 'word_document_project', { story: 'body', mode: 'text' }, first)
    expect(String(firstText.text)).toContain('第一会话文档')
    const secondText = await call<{ text: string }>(ctx, 'word_document_project', { story: 'body', mode: 'text' }, second)
    expect(String(secondText.text)).toContain('第二会话文档')

    await call(ctx, 'word_document_save', { path: 'first.docx' }, first)
    await call(ctx, 'word_document_save', { path: 'second.docx' }, second)
    const reopened = await call<unknown>(ctx, 'word_document_open', { path: 'first.docx' }, second)
    expect(JSON.stringify(reopened)).not.toContain('第二会话文档')

    // Agent-less executions share one fallback document, untouched above.
    await expect(call(ctx, 'word_document_inspect', { kind: 'overview' })).rejects.toThrow(/No document loaded/)
  })
})

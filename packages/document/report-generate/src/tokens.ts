/**
 * The report token vocabulary and its deterministic values, shared by the
 * Host generation engine and the wizard's AI-path task book so both modes
 * present byte-identical numbers.
 *
 * This module stays browser-loadable: no Node imports.
 */
import type { ThermalAnalysis } from '@deepseek-ai/dsh-thermal-analyze/types'
import type { ReportFields } from './types.ts'

/** One measured image the token values project from. */
export interface ReportTokenImage {
  /** Workspace-relative path of the radiometric JPEG. */
  readonly path: string
  /** The analyzer's complete report for the image. */
  readonly analysis: ThermalAnalysis
}

/** Loop markers bracketing the per-image section (paragraph range or table row). */
export const IMAGE_LOOP_OPEN = '{{#each_image}}'

/** The closing loop marker; pairs with {@link IMAGE_LOOP_OPEN}. */
export const IMAGE_LOOP_CLOSE = '{{/each_image}}'

/** Report-header tokens, substituted document-wide. */
export const GLOBAL_TOKENS = [
  '{{report_title}}', '{{company}}', '{{inspector}}', '{{device}}', '{{date}}',
  '{{camera_model}}', '{{camera_serial}}', '{{overall_conclusion}}',
] as const

/** Whole-report aggregate tokens, substituted document-wide. */
export const SUMMARY_TOKENS = [
  '{{image_count}}', '{{overall_max_temp}}', '{{overall_max_at}}', '{{overall_max_image}}',
  '{{overall_min_temp}}', '{{overall_avg_temp}}',
] as const

/** Per-image tokens, substituted inside each expanded section. */
export const IMAGE_TOKENS = [
  '{{image_file}}', '{{image_thermal}}', '{{t_max}}', '{{t_max_at}}', '{{t_min}}', '{{t_avg}}',
  '{{marker_table}}', '{{image_comment}}',
] as const

/** Every token a valid template may contain, structural markers included. */
/**
 * Every token a valid template may contain, structural markers included.
 * @returns the full token vocabulary, with braces.
 */
export function knownTokens(): Set<string> {
  return new Set<string>([
    IMAGE_LOOP_OPEN, IMAGE_LOOP_CLOSE,
    ...GLOBAL_TOKENS, ...SUMMARY_TOKENS, ...IMAGE_TOKENS,
  ])
}

/**
 * Fixed temperature presentation, identical to the AI path's task book.
 * @param value - temperature in Celsius.
 * @returns the temperature with two decimals and the ℃ unit.
 */
export function formatCelsius(value: number): string {
  return `${value.toFixed(2)}℃`
}

/** Pixel-coordinate presentation. */
function formatAt(at: readonly [number, number]): string {
  return `(${at[0]},${at[1]})`
}

/** `basename` of a workspace-relative path. */
function basenameOf(path: string): string {
  return path.replace(/^.*[/\\]/u, '') || path
}

/** Everything one generation run knows besides the template. */
export interface TokenInput {
  readonly images: readonly ReportTokenImage[]
  readonly fields: ReportFields
  /** Generation date; renders as the report date. */
  readonly date: Date
}

/**
 * Document-wide token values: header fields, camera identity from the first
 * image, whole-report aggregates, and the rule-built conclusion.
 * @param input - images, author fields, and the generation date.
 * @returns token text (with braces) to replacement text.
 */
export function globalTokenValues(input: TokenInput): Map<string, string> {
  const { images, fields, date } = input
  const hottest = images.reduce((best, image) =>
    image.analysis.stats.max > best.analysis.stats.max ? image : best)
  const maxTemps = images.map(image => image.analysis.stats.max)
  const minTemps = images.map(image => image.analysis.stats.min)
  const avgTemps = images.map(image => image.analysis.stats.avg)
  const dateText = `${date.getFullYear()}年${date.getMonth() + 1}月${date.getDate()}日`
  const camera = images[0]?.analysis.camera
  const conclusion = images.length === 0
    ? ''
    : `共 ${images.length} 张热像图，最高 ${formatCelsius(hottest.analysis.stats.max)}`
      + `（出现于《${basenameOf(hottest.path)}》），`
      + `最低 ${formatCelsius(Math.min(...minTemps))}，平均 ${formatCelsius(avgTemps.reduce((a, b) => a + b, 0) / avgTemps.length)}。`
  return new Map<string, string>([
    ['{{report_title}}', fields.title ?? ''],
    ['{{company}}', fields.company ?? ''],
    ['{{inspector}}', fields.inspector ?? ''],
    ['{{device}}', fields.device ?? ''],
    ['{{date}}', dateText],
    ['{{camera_model}}', camera?.model ?? ''],
    ['{{camera_serial}}', camera?.serialNo ?? ''],
    ['{{overall_conclusion}}', conclusion],
    ['{{image_count}}', String(images.length)],
    ['{{overall_max_temp}}', images.length === 0 ? '' : formatCelsius(Math.max(...maxTemps))],
    ['{{overall_max_at}}', images.length === 0 ? '' : formatAt(hottest.analysis.stats.maxAt)],
    ['{{overall_max_image}}', images.length === 0 ? '' : basenameOf(hottest.path)],
    ['{{overall_min_temp}}', images.length === 0 ? '' : formatCelsius(Math.min(...minTemps))],
    ['{{overall_avg_temp}}', images.length === 0
      ? ''
      : formatCelsius(avgTemps.reduce((a, b) => a + b, 0) / avgTemps.length)],
  ])
}

/** Inline marker-statistics text for one image, mirroring the task book's wording. */
function markerTableOf(image: ReportTokenImage): string {
  const measured = image.analysis.markers.filter(marker => marker.name !== 'global')
  if (measured.length === 0) return '无标记'
  return measured.map(marker =>
    `${marker.name}（${marker.type}）最高 ${formatCelsius(marker.max)}、最低 ${formatCelsius(marker.min)}、平均 ${formatCelsius(marker.avg)}`).join('；')
}

/**
 * One image's per-section token values.
 * @param image - the image the section presents.
 * @returns token text (with braces) to replacement text.
 */
export function imageTokenValues(image: ReportTokenImage): Map<string, string> {
  const { stats } = image.analysis
  return new Map<string, string>([
    ['{{image_file}}', basenameOf(image.path)],
    ['{{t_max}}', formatCelsius(stats.max)],
    ['{{t_max_at}}', formatAt(stats.maxAt)],
    ['{{t_min}}', formatCelsius(stats.min)],
    ['{{t_avg}}', formatCelsius(stats.avg)],
    ['{{marker_table}}', markerTableOf(image)],
    ['{{image_comment}}', `最高 ${formatCelsius(stats.max)} @ ${formatAt(stats.maxAt)}，平均 ${formatCelsius(stats.avg)}`],
  ])
}

/**
 * Substitute every mapped token in one text line; `{{image_thermal}}` clears
 * because the image phase inserts a drawing at its site.
 * @param text - one projected line.
 * @param values - token mappings applied in order.
 * @returns the replaced line.
 */
export function applyTokenValues(text: string, ...values: readonly Map<string, string>[]): string {
  let out = text
  for (const map of values) {
    for (const [token, value] of map) out = out.replaceAll(token, value)
  }
  return out.replaceAll('{{image_thermal}}', '')
}

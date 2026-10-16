/**
 * Field bindings: how one template token resolves to a value — from the
 * analyzer's JSON by path, from author input, or from a derived template
 * string. The built-in vocabulary in `./tokens.ts` is one instance of this
 * schema; a sidecar `<template>.tokens.json` beside a template overrides or
 * extends it token by token.
 */
import type { ThermalAnalysis, ThermalMarker } from '@deepseek-ai/dsh-thermal-analyze/types'

/** One named value format, applied after a path resolves. */
export type FieldFormatName =
  | 'text'
  | 'celsius'
  | 'number'
  | 'coord'
  | 'percent0'
  | 'date-cn'
  | 'basename'
  | 'marker-summary'

/** One parsed format: a name plus its parameter, when it carries one. */
export interface FieldFormat {
  readonly name: FieldFormatName
  /** Digits after the decimal point; `number` only. */
  readonly decimals?: number
}

/** Where a token is substitutable. */
export type FieldScope = 'global' | 'image' | 'marker'

/** Binds a token to a value read by path from the scope's data. */
export interface PathBinding {
  /** Dot-separated path with `[n]` indexes into the scope's data. */
  readonly path: string
  /** Format name, optionally parameterized as `number:2`. */
  readonly format?: string
  /** Text appended after formatting. */
  readonly suffix?: string
  readonly scope?: FieldScope
}

/** Binds a token to a value the author supplies at generation time. */
export interface InputBinding {
  readonly input: {
    /** Wizard form label; defaults to the token text. */
    readonly label?: string
    /** Whether generation may proceed without a value; default true. */
    readonly optional?: boolean
    /** Pre-filled value. */
    readonly default?: string
  }
}

/** Binds a token to derived text that references other tokens. */
export interface TemplateBinding {
  readonly template: string
  readonly scope?: FieldScope
}

/** Any one field binding. */
export type FieldBinding = PathBinding | InputBinding | TemplateBinding

/** A template's whole field configuration. */
export interface FieldConfiguration {
  readonly version: 1
  /** Whether the built-in vocabulary merges underneath; default true. */
  readonly extends?: boolean
  /** Token text (without braces) to its binding. */
  readonly fields: Readonly<Record<string, FieldBinding>>
}

/** The built-in vocabulary plus a sidecar's overrides, token by token. */
export interface MergedFields {
  /** Token text to its binding; the built-in table when no sidecar applies. */
  readonly fields: Readonly<Record<string, FieldBinding>>
}

/** Scope data one line of the document resolves against. */
export interface ResolveScope {
  readonly kind: FieldScope
  /** Author inputs, keyed by token text. */
  readonly inputs: Readonly<Record<string, string>>
  /** Generation date. */
  readonly date: Date
  /** Whole-report aggregates; global scope only. */
  readonly aggregates?: Aggregates
  /** The line's image; image and marker scopes. */
  readonly image?: { readonly path: string; readonly analysis: ThermalAnalysis }
  /** The line's marker; marker scope (marker rows iterate image–marker pairs). */
  readonly marker?: ThermalMarker
}

/** Whole-report aggregates the global scope exposes by path. */
export interface Aggregates {
  readonly count: number
  readonly overall: {
    readonly max: number
    readonly min: number
    readonly avg: number
    readonly maxAt: readonly [number, number]
    readonly maxImage: string
  }
}

/** Parse one `name` or `name:N` format string.
 * @param text - the format text, as a sidecar or escape-hatch token carries it.
 * @returns the parsed format with its parameter, when it carries one.
 */
export function parseFieldFormat(text: string): FieldFormat {
  if (text === '') return { name: 'text' }
  const numbered = /^number:(\d)$/u.exec(text)
  if (numbered !== null) return { name: 'number', decimals: Number(numbered[1]) }
  const names: readonly FieldFormatName[] = ['text', 'celsius', 'coord', 'percent0', 'date-cn', 'basename', 'marker-summary']
  const name = names.find(candidate => candidate === text)
  if (name === undefined) throw new Error(`Unknown field format "${text}".`)
  return { name }
}

/** Render one value as text without Object's default stringification. */
function textOf(value: unknown): string {
  if (typeof value === 'object' && value !== null) return JSON.stringify(value)
  return String(value)
}

/** Format one resolved value.
 * @param value - the path-read value; undefined and null render empty.
 * @param format - the named format with its parameter.
 * @returns the formatted text.
 */
export function formatFieldValue(value: unknown, format: FieldFormat): string {
  if (value === undefined || value === null) return ''
  switch (format.name) {
    case 'text':
      return textOf(value)
    case 'celsius':
      return `${Number(value).toFixed(2)}℃`
    case 'number':
      return Number(value).toFixed(format.decimals ?? 2)
    case 'coord': {
      const pair = value as readonly [number, number]
      return Array.isArray(pair) ? `(${pair[0]},${pair[1]})` : textOf(value)
    }
    case 'percent0':
      return `${Math.round(Number(value) * 100)}%`
    case 'date-cn': {
      const date = value instanceof Date ? value : new Date(textOf(value))
      return `${date.getFullYear()}年${date.getMonth() + 1}月${date.getDate()}日`
    }
    case 'basename':
      return textOf(value).replace(/^.*[/\\]/u, '') || textOf(value)
    case 'marker-summary':
      return markerSummary(value)
  }
}

/** Join every marker into the summary line the built-in vocabulary uses. */
function markerSummary(value: unknown): string {
  if (!Array.isArray(value)) return ''
  const measured = value.filter((marker): marker is ThermalMarker =>
    typeof marker === 'object' && marker !== null && (marker as ThermalMarker).name !== 'global')
  if (measured.length === 0) return '无标记'
  return measured.map(marker =>
    `${marker.name}（${marker.type}）最高 ${formatFieldValue(marker.max, { name: 'celsius' })}`
    + `、最低 ${formatFieldValue(marker.min, { name: 'celsius' })}`
    + `、平均 ${formatFieldValue(marker.avg, { name: 'celsius' })}`).join('；')
}

/** Read one `a.b[0].c` path from a root object.
 * @param root - the scope's data root.
 * @param path - the dotted path with `[n]` indexes.
 * @returns the addressed value, or undefined when the path leaves the data.
 */
export function readPath(root: unknown, path: string): unknown {
  const segments = path.split('.')
  let current: unknown = root
  for (const segment of segments) {
    if (current === undefined || current === null) return undefined
    const index = /^(\w*)\[(\d+)\]$/u.exec(segment)
    if (index !== null) {
      const name = index[1] ?? ''
      const position = index[2]
      if (position === undefined) return undefined
      const container = name === '' ? current : (current as Record<string, unknown>)[name]
      if (!Array.isArray(container)) return undefined
      current = container[Number(position)]
      continue
    }
    if (typeof current !== 'object') return undefined
    current = (current as Record<string, unknown>)[segment]
  }
  return current
}

/** The data root one scope's paths address.
 * @param scope - the line's scope data.
 * @returns the object its path bindings read from.
 */
export function scopeRoot(scope: ResolveScope): Record<string, unknown> {
  const imageRoot = scope.image === undefined ? undefined
    : { path: scope.image.path, analysis: scope.image.analysis }
  if (scope.kind === 'global') {
    return {
      count: scope.aggregates?.count ?? 0,
      overall: scope.aggregates?.overall,
      first: imageRoot,
      date: scope.date,
      fields: scope.inputs,
    }
  }
  if (scope.kind === 'image') {
    return { ...imageRoot, fields: scope.inputs, date: scope.date }
  }
  return { ...imageRoot, marker: scope.marker, fields: scope.inputs, date: scope.date }
}

/** Parse one sidecar document, refusing anything the schema does not cover.
 * @param text - the sidecar's JSON text.
 * @returns the validated configuration.
 */
export function parseFieldConfiguration(text: string): FieldConfiguration {
  let raw: unknown
  try {
    raw = JSON.parse(text)
  } catch (cause) {
    throw new Error(`The field configuration is not valid JSON: ${cause instanceof Error ? cause.message : String(cause)}`)
  }
  if (typeof raw !== 'object' || raw === null) throw new Error('The field configuration must be a JSON object.')
  const record = raw as { version?: unknown; extends?: unknown; fields?: unknown }
  if (record.version !== 1) throw new Error('The field configuration must declare "version": 1.')
  if (record.extends !== undefined && typeof record.extends !== 'boolean') {
    throw new Error('The field configuration "extends" must be a boolean.')
  }
  if (typeof record.fields !== 'object' || record.fields === null || Array.isArray(record.fields)) {
    throw new Error('The field configuration "fields" must be an object of token bindings.')
  }
  for (const [token, binding] of Object.entries(record.fields)) {
    validateBinding(token, binding)
  }
  return {
    version: 1,
    ...(record.extends === undefined ? {} : { extends: record.extends }),
    fields: record.fields as Readonly<Record<string, FieldBinding>>,
  }
}

/** Validate one token's binding shape. */
function validateBinding(token: string, binding: unknown): void {
  if (typeof binding !== 'object' || binding === null) {
    throw new Error(`Field "${token}" must be a binding object.`)
  }
  const record = binding as Record<string, unknown>
  const keys = Object.keys(record).filter(key => !['scope', 'format', 'suffix'].includes(key))
  if (keys.length === 0) throw new Error(`Field "${token}" binds nothing.`)
  for (const key of keys) {
    if (!['path', 'input', 'template'].includes(key)) {
      throw new Error(`Field "${token}" has unknown key "${key}"; use path, input, or template.`)
    }
  }
  if (record.path === undefined && (record.format !== undefined || record.suffix !== undefined)) {
    throw new Error(`Field "${token}" carries format or suffix without a path.`)
  }
  if (record.path !== undefined) {
    if (typeof record.path !== 'string' || record.path === '') throw new Error(`Field "${token}" path must be a non-empty string.`)
    if (record.format !== undefined && typeof record.format !== 'string') throw new Error(`Field "${token}" format must be a string.`)
    if (record.suffix !== undefined && typeof record.suffix !== 'string') throw new Error(`Field "${token}" suffix must be a string.`)
    if (record.format !== undefined) parseFieldFormat(record.format)
  }
  if (record.input !== undefined) {
    if (typeof record.input !== 'object' || record.input === null) throw new Error(`Field "${token}" input must be an object.`)
    const input = record.input as Record<string, unknown>
    for (const key of Object.keys(input)) {
      if (!['label', 'optional', 'default'].includes(key)) {
        throw new Error(`Field "${token}" input has unknown key "${key}".`)
      }
    }
  }
  if (record.template !== undefined && typeof record.template !== 'string') {
    throw new Error(`Field "${token}" template must be a string.`)
  }
  if (record.scope !== undefined && typeof record.scope === 'string'
    && !['global', 'image', 'marker'].includes(record.scope)) {
    throw new Error(`Field "${token}" scope must be global, image, or marker.`)
  }
}

/** Merge a sidecar configuration over the built-in vocabulary.
 * @param builtin - the built-in vocabulary.
 * @param sidecar - the template's configuration, when one exists.
 * @returns the merged fields, token by token.
 */
export function mergeFieldConfiguration(builtin: FieldConfiguration, sidecar: FieldConfiguration | undefined): MergedFields {
  if (sidecar === undefined) return { fields: builtin.fields }
  if (sidecar.extends === false) return { fields: sidecar.fields }
  return { fields: { ...builtin.fields, ...sidecar.fields } }
}

/** One `path|format` escape-hatch token, when it addresses a known root. */
const ESCAPE_ROOTS = '(?:analysis|path|marker|first|overall|count|date|fields)'
const escapeToken = new RegExp(`^${ESCAPE_ROOTS}(?:[.[][^|]*)?(?:\\|(text|celsius|coord|percent0|date-cn|basename|marker-summary|number:\\d))?$`, 'u')

/**
 * Resolve one token against the merged fields and the line's scope. Input
 * bindings read the author's value; path bindings read the scope's data;
 * template bindings substitute recursively. Escape-hatch tokens address the
 * same roots directly.
 * @param token - token text without braces, possibly `path|format`.
 * @param merged - the template's merged fields.
 * @param scope - the line's scope data.
 * @returns the replacement text, or undefined to leave the token in place.
 */
export function resolveToken(token: string, merged: MergedFields, scope: ResolveScope): string | undefined {
  const binding = merged.fields[token]
  if (binding === undefined) return resolveEscapeToken(token, merged, scope)
  if ('input' in binding) return scope.inputs[token] ?? (binding.input.default ?? '')
  if ('path' in binding) {
    const root = scopeRoot(scope)
    const value = readPath(root, binding.path)
    if (value === undefined) return undefined
    const format = binding.format === undefined ? { name: 'text' } satisfies FieldFormat : parseFieldFormat(binding.format)
    return formatFieldValue(value, format) + (binding.suffix ?? '')
  }
  return substituteTemplate(binding.template, merged, scope, 0)
}

/** Substitute `{{token}}` references inside one derived template string. */
function substituteTemplate(template: string, merged: MergedFields, scope: ResolveScope, depth: number): string {
  if (depth > 3) return template
  return template.replace(/\{\{([^{}]+)\}\}/gu, (_match, inner: string) =>
    resolveToken(inner.trim(), merged, scope) ?? `{{${inner}}}`)
}

/** Resolve one `root.path|format` token that no binding names. */
function resolveEscapeToken(token: string, _merged: MergedFields, scope: ResolveScope): string | undefined {
  const match = escapeToken.exec(token)
  if (match === null) return undefined
  const body = token.split('|')[0] ?? token
  const formatText = match[1]
  const root = scopeRoot(scope)
  const value = readPath(root, body)
  if (value === undefined) return undefined
  const format: FieldFormat = formatText === undefined ? { name: 'text' } : parseFieldFormat(formatText)
  return formatFieldValue(value, format)
}

/** Whether one token is escape-hatch shaped: `root.path` with an optional format.
 * @param token - token text without braces.
 * @returns whether the token could address a scope root by path.
 */
export function looksLikePathToken(token: string): boolean {
  return escapeToken.test(token)
}

/**
 * The token names one field configuration makes substitutable.
 * @param merged - the template's merged fields.
 * @returns substitutable token text, without braces.
 */
export function substitutableTokens(merged: MergedFields): readonly string[] {
  return Object.keys(merged.fields)
}

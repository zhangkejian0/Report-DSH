/**
 * The built-in field vocabulary and the loop markers, expressed as one field
 * configuration; a sidecar `<template>.tokens.json` merges over it token by
 * token. This module stays browser-loadable: no Node imports.
 */
import type { FieldConfiguration } from './bindings.ts'
import type { ThermalAnalysis } from '@deepseek-ai/dsh-thermal-analyze/types'

/** Loop markers bracketing the per-image section (paragraph range or table row). */
export const IMAGE_LOOP_OPEN = '{{#each_image}}'

/** The closing per-image loop marker; pairs with {@link IMAGE_LOOP_OPEN}. */
export const IMAGE_LOOP_CLOSE = '{{/each_image}}'

/**
 * Loop markers bracketing one table row that repeats for every measured
 * marker of every image (the flattened marker detail table); both markers
 * must sit inside the same table row, outside any per-image section.
 */
export const MARKER_LOOP_OPEN = '{{#each_marker}}'

/** The closing marker-row loop marker; pairs with {@link MARKER_LOOP_OPEN}. */
export const MARKER_LOOP_CLOSE = '{{/each_marker}}'

/** One measured image the token values project from. */
export interface ReportTokenImage {
  /** Workspace-relative path of the radiometric JPEG. */
  readonly path: string
  /** The analyzer's complete report for the image. */
  readonly analysis: ThermalAnalysis
}

/**
 * The built-in vocabulary. Global tokens cover author inputs, the generation
 * date, camera identity from the first image, and whole-report aggregates;
 * image tokens cover one image's measurements inside its expanded section.
 */
export const BUILTIN_FIELD_CONFIGURATION: FieldConfiguration = {
  version: 1,
  fields: {
    'report_title': { input: {} },
    'company': { input: {} },
    'inspector': { input: {} },
    'device': { input: {} },
    'date': { path: 'date', format: 'date-cn' },
    'camera_model': { path: 'first.analysis.camera.model' },
    'camera_serial': { path: 'first.analysis.camera.serialNo' },
    'image_count': { path: 'count' },
    'overall_max_temp': { path: 'overall.max', format: 'celsius' },
    'overall_max_at': { path: 'overall.maxAt', format: 'coord' },
    'overall_max_image': { path: 'overall.maxImage', format: 'basename' },
    'overall_min_temp': { path: 'overall.min', format: 'celsius' },
    'overall_avg_temp': { path: 'overall.avg', format: 'celsius' },
    'overall_conclusion': {
      template: '共 {{image_count}} 张热像图，最高 {{overall_max_temp}}（出现于《{{overall_max_image}}》），最低 {{overall_min_temp}}，平均 {{overall_avg_temp}}。',
    },
    'image_file': { path: 'path', format: 'basename', scope: 'image' },
    't_max': { path: 'analysis.stats.max', format: 'celsius', scope: 'image' },
    't_max_at': { path: 'analysis.stats.maxAt', format: 'coord', scope: 'image' },
    't_min': { path: 'analysis.stats.min', format: 'celsius', scope: 'image' },
    't_avg': { path: 'analysis.stats.avg', format: 'celsius', scope: 'image' },
    'marker_table': { path: 'analysis.markers', format: 'marker-summary', scope: 'image' },
    'image_comment': {
      template: '最高 {{t_max}} @ {{t_max_at}}，平均 {{t_avg}}',
      scope: 'image',
    },
  },
}

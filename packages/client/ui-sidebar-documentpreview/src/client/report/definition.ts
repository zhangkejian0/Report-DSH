/** The report wizard's tab-type identity, workspace conventions, and guide entry. */
import type { SidebarRightTabDefinition } from '@deepseek-ai/dsh-client-ui-sidebar-right/client'
import type { TranslateNS } from '@deepseek-ai/dsh-client-locale/client'
import { GuideArtworkFiles } from '@deepseek-ai/dsh-client-ui-primitives'
import type {} from './locales.ts'

/** The tab kind this module owns; the guide page opens the wizard by it. */
export const REPORT_BUILDER_KIND = 'report-builder'

/** This implementation's identity in the tab system, and its body's seat key. */
export const REPORT_BUILDER_ID = '@deepseek-ai/dsh-client-ui-sidebar-documentpreview/report-builder'

/** The template library's tab kind; the guide page opens the library by it. */
export const REPORT_TEMPLATES_KIND = 'report-templates'

/** The template library's identity in the tab system, and its body's seat key. */
export const REPORT_TEMPLATES_ID = '@deepseek-ai/dsh-client-ui-sidebar-documentpreview/report-templates'

/** Workspace directory holding report templates, by product convention. */
export const REPORT_TEMPLATES_DIR = '报告模版'

/** Workspace directory receiving generated reports, by product convention. */
export const REPORTS_DIR = '报告'

/**
 * The report wizard's registry definition: a page type with a guide entry, so
 * the start page offers generation beside the workspace files door.
 * @param t - namespace-bound translate, read fresh on every label call.
 * @returns the definition to register.
 */
export function reportBuilderDefinition(t: TranslateNS<'sidebarReport'>): SidebarRightTabDefinition {
  return {
    id: REPORT_BUILDER_ID,
    kind: REPORT_BUILDER_KIND,
    priority: 'extension',
    title: () => t('title'),
    guide: [{
      id: 'generate',
      order: 15,
      title: () => t('guide.title'),
      description: () => t('guide.description'),
      icon: GuideArtworkFiles,
    }],
  }
}

/**
 * The template library's registry definition: a page type with its own guide
 * entry, so the start page offers template management beside generation.
 * @param t - namespace-bound translate, read fresh on every label call.
 * @returns the definition to register.
 */
export function reportTemplatesDefinition(t: TranslateNS<'sidebarReport'>): SidebarRightTabDefinition {
  return {
    id: REPORT_TEMPLATES_ID,
    kind: REPORT_TEMPLATES_KIND,
    priority: 'extension',
    title: () => t('library.title'),
    guide: [{
      id: 'templates',
      order: 16,
      title: () => t('library.guide.title'),
      description: () => t('library.guide.description'),
      icon: GuideArtworkFiles,
    }],
  }
}

/** Characters a Windows or POSIX report directory name cannot carry. */
const INVALID_NAME = /[\\/:*?"<>|]/u

/**
 * Whether one report name is usable as the output directory and file name.
 * @param name - user-entered report name.
 * @returns true when non-empty and free of path and reserved characters.
 */
export function isValidReportName(name: string): boolean {
  const trimmed = name.trim()
  return trimmed !== '' && !INVALID_NAME.test(trimmed)
}

/**
 * The default report name for one generation: the template's basename and the
 * local date, unique enough for repeated same-day runs by its time suffix.
 * @param templatePath - workspace-relative template path.
 * @param now - clock the name stamps.
 * @returns the default report name without extension.
 */
export function defaultReportName(templatePath: string, now: Date): string {
  const base = templatePath.replace(/^.*[/\\]/u, '').replace(/\.docx$/iu, '') || '报告'
  const pad = (value: number): string => String(value).padStart(2, '0')
  return `${base}-${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}`
}

/** Request and result vocabulary of deterministic report generation. */

/** Failure stages of one generation, carried as the `reason` detail. */
export type ReportGenerateReason =
  | 'analyzer-unavailable'
  | 'template-invalid'
  | 'template-markers'
  | 'template-too-large'
  | 'no-image-loop'
  | 'unknown-token'
  | 'too-many-images'
  | 'analysis-failed'
  | 'residue'
  | 'write-failed'
  | 'failed'

declare module '@deepseek-ai/dsh-typert-protocol' {
  interface RemoteErrorDetailsMap {
    /** Report generation refused or failed; `reason` names the stage. */
    'report-generate/failed': { readonly reason: ReportGenerateReason }
  }
}

/** Author-supplied report fields; absent fields substitute as empty text. */
export interface ReportFields {
  /** Report title; the wizard prefills the report name. */
  readonly title?: string
  /** Inspecting company. */
  readonly company?: string
  /** Inspector name. */
  readonly inspector?: string
  /** Name of the device under inspection. */
  readonly device?: string
}

/** Everything one deterministic generation needs, except the template bytes. */
export interface ReportGenerateRequest {
  /** Workspace-relative path of the template .docx. */
  readonly templatePath: string
  /** Workspace-relative path of the report .docx to write. */
  readonly outputPath: string
  /** Workspace-relative paths of the radiometric JPEGs, in report order. */
  readonly imagePaths: readonly string[]
  /** Author-supplied values for the report-header tokens. */
  readonly fields?: ReportFields
}

/** How one generation settled. */
export interface ReportGenerateResult {
  /** Absolute host path of the written report. */
  readonly absolutePath: string
  /** Size of the written .docx in bytes. */
  readonly bytes: number
  /** Number of per-image sections expanded. */
  readonly images: number
}

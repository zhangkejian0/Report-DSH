/** Result vocabulary of one thermal-image analysis; every temperature is Celsius. */

/** Failure stages of one analysis, carried as the `reason` detail. */
export type ThermalAnalyzeReason = 'unavailable' | 'not-thermal' | 'open-failed' | 'timeout' | 'too-large' | 'failed'

declare module '@deepseek-ai/dsh-typert-protocol' {
  interface RemoteErrorDetailsMap {
    /** Thermal analysis refused or failed; `reason` names the stage. */
    'thermal-analyze/failed': { readonly reason: ThermalAnalyzeReason }
  }
}

/** Camera identity recorded in the radiometric file. */
export interface ThermalCamera {
  readonly model: string
  readonly serialNo: string
  readonly brand: string
  readonly company: string
}

/** Lens fields the file records; zeros when the file carries none. */
export interface ThermalLens {
  readonly focalLength: number
  readonly hFov: number
}

/** Measurement parameters the file was captured with. */
export interface ThermalParams {
  readonly emissivity: number
  readonly distance: number
  readonly reflTemp: number
  readonly atmTemp: number
  readonly relHumidity: number
}

/** Displayed gain window the camera embedded. */
export interface ThermalGain {
  readonly mode: string
  readonly min: number
  readonly max: number
}

/** Whole-frame temperature statistics in file pixel coordinates. */
export interface ThermalStats {
  readonly min: number
  readonly max: number
  readonly avg: number
  readonly minAt: readonly [number, number]
  readonly maxAt: readonly [number, number]
  readonly count: number
}

/** One measurement rectangle the camera or an operator drew into the file. */
export interface ThermalMarker {
  readonly name: string
  readonly type: string
  /** `[x, y, width, height]` in file pixel coordinates. */
  readonly rect: readonly [number, number, number, number]
  readonly max: number
  readonly min: number
  readonly avg: number
  readonly hotSpot: readonly [number, number]
  readonly coldSpot: readonly [number, number]
}

/** Block-averaged temperature grid for hover readout over the rendered image. */
export interface ThermalGrid {
  readonly width: number
  readonly height: number
  /** Averaging block edge in file pixels. */
  readonly block: number
  /** Row-major averages, `width * height` entries, Celsius. */
  readonly data: readonly number[]
}

/** Raw AD frame summary the file embeds. */
export interface ThermalFrame {
  readonly timestamp: string
  readonly adMin: number
  readonly adMax: number
  readonly adAvg: number
}

/** Palette-rendered images embedded as base64 PNG. */
export interface ThermalImages {
  readonly thermal: string
  readonly palette: string
}

/** One complete analyzer report for a radiometric JPEG. */
export interface ThermalAnalysis {
  readonly file: {
    readonly path: string
    readonly type: string
    readonly version: number
    readonly modifiedTime: string
    readonly width: number
    readonly height: number
  }
  readonly camera: ThermalCamera
  readonly lens: ThermalLens
  readonly params: ThermalParams
  readonly gain: ThermalGain
  readonly stats: ThermalStats
  readonly markers: readonly ThermalMarker[]
  readonly grid: ThermalGrid
  readonly frame: ThermalFrame
  readonly images: ThermalImages
}

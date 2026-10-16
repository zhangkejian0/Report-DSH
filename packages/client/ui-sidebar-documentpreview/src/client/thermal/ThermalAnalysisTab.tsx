/** Thermal-image analysis hosted as its own right-Sidebar tab. */
import { useEffect, useMemo, useState, type MouseEvent as ReactMouseEvent, type ReactNode } from 'react'
import type { PropsLocale, PropsRuntime } from '@deepseek-ai/dsh-client-ui-slots'
import type { ThermalAnalysis, ThermalMarker } from '@deepseek-ai/dsh-thermal-analyze/types'
import { basenameOf } from '../definition.ts'
import { analysisTargetOf, parseAnalysisAddress } from './thermal-address.ts'
import { analyzeBridge } from './analyze-bridge.ts'
import { REPORT_BUILDER_KIND } from '../report/definition.ts'
import type {} from './locales.ts'
import css from './ThermalAnalysisTab.module.css'

/** Props of the analysis tab: its own tab reader and localized copy. */
export type ThermalAnalysisTabProps =
  PropsRuntime<'sidebar.right.pane.tab'> & PropsLocale<'sidebarThermal'>

/** One hovered readout over the rendered thermal field. */
interface HoverPoint {
  readonly x: number
  readonly y: number
  readonly temp: number
}

/** Marker identity colors shared by field boxes and table dots. */
const MARKER_COLORS = ['#ff5a52', '#ffc53d', '#51d88a', '#5aa9ff', '#c084fc'] as const

/** Stable color for one marker by its table position. */
const colorOfMarker = (markers: readonly ThermalMarker[], marker: ThermalMarker): string =>
  MARKER_COLORS[markers.indexOf(marker) % MARKER_COLORS.length] ?? '#5aa9ff'

/** Map one wire failure reason onto its locale key. */
function reasonKey(reason: unknown): 'reasonNotThermal' | 'reasonOpenFailed' | 'reasonTimeout' | 'reasonTooLarge' | 'reasonUnavailable' | 'reasonFailed' {
  switch (reason) {
    case 'not-thermal': return 'reasonNotThermal'
    case 'open-failed': return 'reasonOpenFailed'
    case 'timeout': return 'reasonTimeout'
    case 'too-large': return 'reasonTooLarge'
    case 'unavailable': return 'reasonUnavailable'
    default: return 'reasonFailed'
  }
}

/** Extract the wire reason detail from one gateway error value. */
function reasonOf(error: unknown): unknown {
  if (typeof error === 'object' && error !== null && 'details' in error) {
    const details = (error as { details?: unknown }).details
    if (typeof details === 'object' && details !== null && 'reason' in details) {
      return (details).reason
    }
  }
  return undefined
}

/**
 * Run the analyzer for one image and present its report as a compact
 * instrument panel: identity chips, max/min/avg cards, the palette-rendered
 * field with its legend, hover readout and embedded-marker boxes in table
 * colors, capture parameters, and the marker table. Analysis runs on the Host
 * through the `thermalAnalyze` Remote, so a deployment without the analyzer
 * shows its unavailable notice instead of a broken tab.
 * @param props - this tab's reader and the thermal dictionary.
 * @returns the analysis panel, or its loading, failure, or unavailable state.
 */
export function ThermalAnalysisTab({ useTabInfo, t }: ThermalAnalysisTabProps): ReactNode {
  const { tab } = useTabInfo()
  const address = tab.contentId
  const name = basenameOf(parseAnalysisAddress(address) ?? '') || 'image.jpg'
  const target = analysisTargetOf(address)
  const [report, setReport] = useState<ThermalAnalysis>()
  const [error, setError] = useState<string>()
  const [reload, setReload] = useState(0)
  const [hover, setHover] = useState<HoverPoint>()

  useEffect(() => {
    if (target === undefined) return
    const analyze = analyzeBridge.analyze
    if (analyze === undefined) return
    const closed = { settled: false }
    void analyze(target.sessionId, target.path, tab.signal).then((report) => {
      if (closed.settled) return
      setReport(report)
      setError(undefined)
    }, (cause: unknown) => {
      if (closed.settled) return
      setError(t(reasonKey(reasonOf(cause))))
    })
    return () => { closed.settled = true }
  }, [target, tab.signal, reload, t])

  const thermalUrl = useMemo(
    () => report === undefined || report.images.thermal === '' ? undefined : `data:image/png;base64,${report.images.thermal}`,
    [report],
  )
  const paletteUrl = useMemo(
    () => report === undefined || report.images.palette === '' ? undefined : `data:image/png;base64,${report.images.palette}`,
    [report],
  )

  if (target === undefined) {
    return <p className={css.status}>{t('reasonNotThermal')}</p>
  }
  if (analyzeBridge.analyze === undefined) {
    return <p className={css.status} role="status">{t('unavailable')}</p>
  }
  if (report === undefined) {
    return (
      <div className={css.status}>
        <p role="status">{t('loading')}</p>
        {error !== undefined && (
          <p>
            {t('failed', { message: error })}
            {' '}
            <button type="button" className={css.retry} onClick={() => { setReload(value => value + 1) }}>
              {t('retry')}
            </button>
          </p>
        )}
      </div>
    )
  }

  const { file, camera, params, gain, stats, markers, frame, grid } = report
  const fieldMarkers = markers.filter(marker => marker.name !== 'global')
  const vectorMarkers = fieldMarkers.filter(marker =>
    (marker.type === 'Polygon' || marker.type === 'Polyline') && marker.points !== undefined)
  const tableMarkers = markers

  /** Hover readout over the rendered field, in file pixel coordinates. */
  const onFieldMove = (event: ReactMouseEvent<HTMLDivElement>): void => {
    if (thermalUrl === undefined) return
    const bounds = event.currentTarget.getBoundingClientRect()
    const px = (event.clientX - bounds.left) / bounds.width
    const py = (event.clientY - bounds.top) / bounds.height
    if (px < 0 || px >= 1 || py < 0 || py >= 1) { setHover(undefined); return }
    const gx = Math.min(grid.width - 1, Math.floor(px * grid.width))
    const gy = Math.min(grid.height - 1, Math.floor(py * grid.height))
    setHover({
      x: Math.min(file.width - 1, Math.floor(px * file.width)),
      y: Math.min(file.height - 1, Math.floor(py * file.height)),
      temp: grid.data[gy * grid.width + gx] ?? Number.NaN,
    })
  }

  return (
    <div className={css.root} data-thermal-analysis>
      <header className={css.header}>
        <strong className={css.name}>{name}</strong>
        <div className={css.chips}>
          {camera.model !== '' && <span className={css.chip}>{camera.model}</span>}
          {camera.serialNo !== '' && <span className={css.chip}>{t('serialNo')} {camera.serialNo}</span>}
          {frame.timestamp !== '' && <span className={css.chip}>{frame.timestamp}</span>}
          <button
            type="button"
            className={css.reportButton}
            title={t('reportHint')}
            onClick={() => {
              tab.actions.openTab(REPORT_BUILDER_KIND, { params: { preselectImage: target.path } })
            }}
          >
            {t('report')}
          </button>
        </div>
      </header>

      <div className={css.kpis}>
        <div className={`${css.kpi} ${css.kpiHot}`}>
          <span className={css.kpiLabel}>{t('max')}</span>
          <span className={css.kpiValue}>{stats.max.toFixed(2)}℃</span>
          <span className={css.kpiSub}>@{stats.maxAt[0]},{stats.maxAt[1]}</span>
        </div>
        <div className={`${css.kpi} ${css.kpiCold}`}>
          <span className={css.kpiLabel}>{t('min')}</span>
          <span className={css.kpiValue}>{stats.min.toFixed(2)}℃</span>
          <span className={css.kpiSub}>@{stats.minAt[0]},{stats.minAt[1]}</span>
        </div>
        <div className={css.kpi}>
          <span className={css.kpiLabel}>{t('avg')}</span>
          <span className={css.kpiValue}>{stats.avg.toFixed(2)}℃</span>
          <span className={css.kpiSub}>{t('gain')} {gain.min.toFixed(1)}~{gain.max.toFixed(1)}℃</span>
        </div>
      </div>

      {thermalUrl !== undefined && (
        <figure className={css.fieldCard}>
          <div className={css.field} onMouseMove={onFieldMove} onMouseLeave={() => { setHover(undefined) }}>
            <img className={css.fieldImage} src={thermalUrl} alt={name} draggable={false} />
            {vectorMarkers.length > 0 && (
              <svg
                className={css.vectorOverlay}
                viewBox={`0 0 ${file.width} ${file.height}`}
                preserveAspectRatio="none"
              >
                {vectorMarkers.map((marker) => {
                  const points = (marker.points ?? [])
                    .map(([x, y]) => `${x},${y}`)
                    .join(' ')
                  const color = colorOfMarker(markers, marker)
                  return marker.type === 'Polygon' ? (
                    <polygon
                      key={marker.name}
                      points={points}
                      fill="none"
                      stroke={color}
                      strokeWidth={2}
                      vectorEffect="non-scaling-stroke"
                    />
                  ) : (
                    <polyline
                      key={marker.name}
                      points={points}
                      fill="none"
                      stroke={color}
                      strokeWidth={2}
                      strokeLinejoin="round"
                      vectorEffect="non-scaling-stroke"
                    />
                  )
                })}
              </svg>
            )}
            {fieldMarkers.map((marker) => {
              const color = colorOfMarker(markers, marker)
              if (marker.type === 'Spot' && marker.point !== undefined) {
                return (
                  <span
                    key={marker.name}
                    className={css.markerSpot}
                    style={{
                      left: `${marker.point[0] / file.width * 100}%`,
                      top: `${marker.point[1] / file.height * 100}%`,
                      background: color,
                    }}
                    title={marker.name}
                  />
                )
              }
              return (
                <span
                  key={marker.name}
                  className={css.markerBox}
                  style={{
                    left: `${marker.rect[0] / file.width * 100}%`,
                    top: `${marker.rect[1] / file.height * 100}%`,
                    width: `${marker.rect[2] / file.width * 100}%`,
                    height: `${marker.rect[3] / file.height * 100}%`,
                    borderColor: color,
                    ...(marker.type === 'Ellipse' ? { borderRadius: '50%' } : {}),
                  }}
                  title={marker.name}
                />
              )
            })}
            <span
              className={css.hotSpot}
              style={{ left: `${stats.maxAt[0] / file.width * 100}%`, top: `${stats.maxAt[1] / file.height * 100}%` }}
              title={t('hotSpot')}
            />
            {hover !== undefined && (
              <span className={css.hoverBadge}>{t('hover', { x: String(hover.x), y: String(hover.y), temp: Number.isNaN(hover.temp) ? '--' : hover.temp.toFixed(2) })}</span>
            )}
          </div>
          {paletteUrl !== undefined && (
            <figcaption className={css.legend}>
              <img className={css.legendBar} src={paletteUrl} alt="" draggable={false} />
              <div className={css.legendTicks}>
                <span>{gain.min.toFixed(1)}℃</span>
                <span>{gain.max.toFixed(1)}℃</span>
              </div>
            </figcaption>
          )}
        </figure>
      )}

      <dl className={css.params}>
        <div className={css.param}><dt>{t('emissivity')}</dt><dd>{params.emissivity.toFixed(2)}</dd></div>
        <div className={css.param}><dt>{t('distance')}</dt><dd>{params.distance.toFixed(1)}m</dd></div>
        <div className={css.param}><dt>{t('humidity')}</dt><dd>{(params.relHumidity * 100).toFixed(0)}%</dd></div>
        <div className={css.param}><dt>{t('adRange')}</dt><dd>{frame.adMin}~{frame.adMax}</dd></div>
      </dl>

      {tableMarkers.length > 0 && (
        <table className={css.markers}>
          <thead>
            <tr>
              <th>{t('markerName')}</th>
              <th>{t('markerMax')}</th>
              <th>{t('markerMin')}</th>
              <th>{t('markerAvg')}</th>
            </tr>
          </thead>
          <tbody>
            {tableMarkers.map(marker => (
              <tr key={marker.name} className={marker.name === 'global' ? css.markerRowGlobal : undefined}>
                <td>
                  {marker.name !== 'global' && (
                    <span className={css.markerDot} style={{ background: colorOfMarker(markers, marker) }} />
                  )}
                  {marker.name}
                </td>
                <td className={css.hotValue}>{marker.max.toFixed(2)}</td>
                <td>{marker.min.toFixed(2)}</td>
                <td>{marker.avg.toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}

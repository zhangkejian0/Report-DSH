/** Locale-owned thermal-analysis labels, status text, and reason lines. */
export const zh = {
  analyze: '分析',
  title: '热像图分析',
  loading: '分析中...',
  unavailable: '热像分析服务不可用',
  failed: '分析失败：{message}',
  reasonNotThermal: '这不是可分析的热像图文件',
  reasonOpenFailed: '热像文件无法打开（文件可能损坏）',
  reasonTimeout: '分析超时',
  reasonTooLarge: '分析结果超出大小上限',
  reasonFailed: '分析器运行失败',
  reasonUnavailable: '分析器未部署（缺 ThermalAnalyzeJson.exe）',
  camera: '相机',
  serialNo: '序列号',
  capturedAt: '拍摄时间',
  emissivity: '发射率',
  distance: '距离',
  humidity: '湿度',
  gain: '温宽',
  range: '量程',
  max: '最高',
  min: '最低',
  avg: '平均',
  hotSpot: '热点',
  coldSpot: '冷点',
  markers: '内置测量区',
  markerName: '名称',
  markerMax: '最高(℃)',
  markerMin: '最低(℃)',
  markerAvg: '平均(℃)',
  adRange: 'AD 原始值',
  hover: '({x}, {y})  {temp}℃',
  retry: '重试',
} satisfies Record<string, string>

/** Thermal-analysis dictionary keys. */
export type ThermalAnalysisKey = keyof typeof zh

/** English dictionary with the same keys as the Chinese dictionary. */
export const en = {
  analyze: 'Analyze',
  title: 'Thermal image analysis',
  loading: 'Analyzing...',
  unavailable: 'Thermal analysis is unavailable',
  failed: 'Analysis failed: {message}',
  reasonNotThermal: 'This file is not an analyzable thermal image',
  reasonOpenFailed: 'The thermal file could not be opened (it may be damaged)',
  reasonTimeout: 'Analysis timed out',
  reasonTooLarge: 'The report exceeded its size cap',
  reasonFailed: 'The analyzer failed',
  reasonUnavailable: 'The analyzer is not deployed (ThermalAnalyzeJson.exe missing)',
  camera: 'Camera',
  serialNo: 'Serial',
  capturedAt: 'Captured',
  emissivity: 'Emissivity',
  distance: 'Distance',
  humidity: 'Humidity',
  gain: 'Gain',
  range: 'Range',
  max: 'Max',
  min: 'Min',
  avg: 'Avg',
  hotSpot: 'Hot spot',
  coldSpot: 'Cold spot',
  markers: 'Embedded markers',
  markerName: 'Name',
  markerMax: 'Max (°C)',
  markerMin: 'Min (°C)',
  markerAvg: 'Avg (°C)',
  adRange: 'Raw AD',
  hover: '({x}, {y})  {temp}°C',
  retry: 'Retry',
} satisfies Record<ThermalAnalysisKey, string>

declare module '@deepseek-ai/dsh-client-ui-slots' {
  interface LocaleNamespaceMap {
    /** Thermal-image analysis tab and action text. */
    sidebarThermal: ThermalAnalysisKey
  }
}

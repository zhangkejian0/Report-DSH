/** Assembly of the generation task book handed to the report Session's agent. */
import type { ThermalAnalysis } from '@deepseek-ai/dsh-thermal-analyze/types'

/** One image's complete generation payload: where it lives and what the analyzer measured. */
export interface ReportImageDatum {
  /** Workspace-relative path of the radiometric JPEG. */
  readonly path: string
  /** Workspace-relative path of the written palette-rendered thermal PNG, when one was written. */
  readonly assetPath: string | undefined
  /** The analyzer's complete report for the image. */
  readonly analysis: ThermalAnalysis
}

/** Everything the task book names: template, output, and the measured images. */
export interface TaskBookInput {
  /** Workspace-relative path of the template .docx. */
  readonly templatePath: string
  /** Workspace-relative path of the report .docx to write. */
  readonly outputPath: string
  /** Workspace-relative directory holding the written image assets. */
  readonly assetsDir: string
  /** The measured images, in report order. */
  readonly images: readonly ReportImageDatum[]
}

/** Fixed temperature presentation. */
const celsius = (value: number): string => `${value.toFixed(2)}℃`

/**
 * One image's compact measurement block: identity line, whole-frame extremes,
 * each embedded marker, and the capture parameters the report table needs.
 * @param image - the image to render.
 * @returns the block without its heading indentation.
 */
function imageBlock(image: ReportImageDatum): string {
  const { analysis } = image
  const { file, camera, params, gain, stats, markers, frame } = analysis
  const lines = [
    `- 原图：${image.path}（${file.width}×${file.height}，拍摄时间 ${frame.timestamp || '未知'}）`,
    `- 相机：${camera.brand} ${camera.model}（编号 ${camera.serialNo || '未知'}）`,
    `- 全场温度：最高 ${celsius(stats.max)} @(${stats.maxAt[0]},${stats.maxAt[1]})；最低 ${celsius(stats.min)} @(${stats.minAt[0]},${stats.minAt[1]})；平均 ${celsius(stats.avg)}；量程 ${celsius(gain.min)}~${celsius(gain.max)}`,
    `- 拍摄参数：发射率 ${params.emissivity.toFixed(2)}，距离 ${params.distance.toFixed(1)}m，反射温度 ${params.reflTemp.toFixed(1)}℃，环境温度 ${params.atmTemp.toFixed(1)}℃，相对湿度 ${(params.relHumidity * 100).toFixed(0)}%`,
    ...(image.assetPath === undefined
      ? []
      : [`- 热图素材：${image.assetPath}（用 word_document_add_image 注册后插图）`]),
  ]
  const measured = markers.filter(marker => marker.name !== 'global')
  if (measured.length > 0) {
    lines.push(`- 标记统计：${measured.map(marker =>
      `${marker.name}（${marker.type}）最高 ${celsius(marker.max)}、最低 ${celsius(marker.min)}、平均 ${celsius(marker.avg)}`).join('；')}`)
  }
  return lines.join('\n')
}

/**
 * Build the generation task book: the deterministic data package plus the
 * working discipline (save-first, per-step saves, the expansion ladder), so
 * the agent's freedom stays in wording and never in structure or numbers.
 * @param input - template, output, and the measured images.
 * @returns the complete prompt text.
 */
export function buildTaskBook(input: TaskBookInput): string {
  const { templatePath, outputPath, assetsDir, images } = input
  return [
    '请基于报告模版生成一份热像检测报告，全程使用 word_document_* 工具。',
    '',
    `模版：${templatePath}`,
    `输出：${outputPath}（报告 docx；素材已在 ${assetsDir}/ 备好）`,
    '',
    `一、图片测量数据（共 ${images.length} 张，顺序即报告顺序）`,
    ...images.map((image, index) => `${index + 1}. ${image.path}\n${imageBlock(image)}`),
    '',
    '二、填充规则',
    '1. 全局占位符（模版中出现才填，保留未出现者不添加）：{{report_title}} {{company}} {{inspector}} {{date}} {{device}} {{camera_model}} {{camera_serial}} {{overall_conclusion}}。其中相机类取第一张图的数据，{{date}} 取今天，{{overall_conclusion}} 由你根据各图最高温与负荷情况总结。逐个替换：每处一次编辑、替换后立即保存，不得把多段文字合并进同一次替换，也不得把正文包进 {{ }} 里。',
    '2. 单图节展开（三阶梯，按最先命中者执行）：',
    '   a. 模版含 {{#each_image}} 与 {{/each_image}}：按以下机械步骤——(1) 投影定位两标记之间的行范围；(2) 对第 1 张图：把该范围内容复制一份并逐 token 填入该图数据；(3) 紧接着对第 2 张图再复制一份并填入，依此类推，每张图独立一节、节与节紧邻；(4) 全部展开后删除 {{#each_image}} 与 {{/each_image}} 两行标记本身。禁止把两张图的数据写进同一节。',
    '   b. 无括起标记但存在样例单图节（含 {{image_file}} 或温度表格特征的节）：仿照其结构为每张图生成独立一节；',
    '   c. 两者皆无：按内置结构生成——每图一节，含文件名、热图、全场温度表（最高/最低/平均/热点坐标）、标记统计表、一句分析意见。',
    '3. 单图占位符：{{image_file}} {{t_max}} {{t_max_at}} {{t_min}} {{t_avg}} {{marker_table}} {{image_comment}}。温度一律保留两位小数并带 ℃。',
    '4. 插图：在 {{image_thermal}} 标记处（或每节表格上方）用 word_document_add_image 注册该图热图素材路径，取得 assetRef 后插入图片（宽度约 460px，按原始尺寸等比），并删除 {{image_thermal}} 标记文字；数据未附热图素材的图跳过插图，仅删除标记。',
    '',
    '三、工作纪律',
    '1. 第一步：word_document_open 打开模版，立即 word_document_save 另存到输出路径，此后只在输出文件上继续；',
    '2. 每完成一个章节或一处填充就 word_document_save 保存一次；不要在 patch 数据绑定内容控件后又 edit 其它部分再一次性保存；',
    '3. 只改需要改的内容，模版的样式、页眉页脚、表格样式保持原样；不得在文档中新增任何 {{ }} 形态的文字；',
    '4. 完成判据（必须执行）：用 word_document_inspect 的 search（query 为 "{{"）检索全文——这是 ASCII 查询，可用；若 matches 不为空，逐个清除后再检索，直至为 0。汇报里写明残留检索结果为 0。',
    '5. 全部完成后，用一段话向用户汇报：输出路径、每张图的最高温、以及你总结的检测结论。',
  ].join('\n')
}

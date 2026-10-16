# 报告确定性生成（无模型）实现落地方案

日期：2026-10-15。状态：方案定稿，未开工。

## 0. 目标与范围

把报告向导的「生成」从 agent 会话（模型驱动 `word_document_*` 工具调用）改为默认走**宿主进程内的确定性模板引擎**：同样的输入（模版 .docx + 各图 `ThermalAnalysis`）在本地代码里直接产出报告 .docx，零 token、秒级、完全可重复、无 key 可用。AI 路径保留为可选模式（样例仿写等开放场景）。

不在范围：热像分析本身（已是确定性 SDK）、WordInWeb 编辑会话行为、报告模版库管理 UI。

## 1. 架构总览

```
向导 ReportBuilderTab（③输出 加模式开关 + 可选著录字段）
   │ mode='fast'                              │ mode='ai'（现状不动）
   ▼                                          ▼
remote.reportGenerate.generate(...)     startGeneration → createSession/prompt 任务书
   │（typert Remote，热像分析同模式）              （generate.ts 现路径）
   ▼
宿主服务 ReportGenerate（新包 packages/document/report-generate）
   │ workspaceFiles.readBytes 读模版（同 thermal-analyze 的授权模型）
   │ workspaceFiles.writeBytes 写输出
   ▼
确定性引擎（同包 src/engine.ts，纯库）
   AgentDocument.load(template) → 投影/结构克隆/补丁/插图 → save()
   （@wordinweb/agent，与 agent 路径同一引擎、同一编辑原语）
```

关键事实（已逐项核实）：
- `word_document_*` 底层 `AgentDocument` 是纯代码库（`@wordinweb/agent` link 依赖已存在，`packages/document/tool-wordinweb/package.json`）。模型只是"驾驶员"；去掉它只需把任务书里的机械步骤写成代码。
- 投影锚点模型齐备：`project({story, mode})` 返回每行 `AgentAnchorLine`（`blockRef` + 每段 `runRef`），stories 覆盖 body/`header:*`/`footer:*`/脚注尾注；`patch({edits|hunks})` 按行替换；`edit` 暴露 `pasteBlocks(afterBlockRef, blocksXml)`、`insertImage(runRef, assetRef, widthPx, heightPx)`、`mergeParagraph(blockRef)`；`addAsset(bytes, mediaType)`、`save(): Uint8Array`。
- typert remote 客户端是**构建期自动生成**：包声明 `./typert`、`./remote` exports 即可（蓝本：thermal-analyze）。
- 组合点：`packages/bundle/web-app/cordis.patch.yml`（本功能仅 Web 端）+ `packages/bundle/web-app/package.json` manifest。

## 2. 新包 `packages/document/report-generate`

`@deepseek-ai/dsh-report-generate`，蓝本 `packages/document/thermal-analyze`（README 三件套、exports 配对、schemastery Config、TypertRemoteService）。

### 2.1 目录与 exports

```
src/
  index.ts     服务：ReportGenerate extends TypertRemoteService('reportGenerate')
  types.ts     请求/结果/错误 reason 类型
  tokens.ts    纯模块：token 词表 + 取值函数 + 温度格式化（浏览器可导入）
  engine.ts    确定性引擎（纯函数，Node）
tests/
  engine.spec.ts        引擎全链路（fixture 模版 + 结构化分析 JSON）
  report-generate.spec.ts  服务授权/错误语义（stub workspaceFiles，蓝本 thermal-analyze.spec.ts）
```

package.json exports：`.`（服务）、`./types`、`./tokens`（客户端任务书共用取值/格式化）、`./typert`、`./remote`（生成物）。依赖：`@wordinweb/agent` link（同 tool-wordinweb 写法）、`@deepseek-ai/dsh-typert-protocol`、`@deepseek-ai/schemastery`、`@deepseek-ai/dsh-thermal-analyze/types`（workspace:*）。

### 2.2 Config（无硬编码可调项）

```ts
imageWidthPx: z.natural().min(80).max(2000).default(460)   // 单图热图插入宽度
maxImages:    z.natural().min(1).max(2000).default(500)     // 一次生成的图片上限
```

### 2.3 Remote API 与错误语义

```ts
interface ReportGenerateImage { path: string; analysis: ThermalAnalysis }
interface ReportGenerateRequest {
  templatePath: string          // workspace 相对
  outputPath: string            // workspace 相对，.docx 结尾
  images: readonly ReportGenerateImage[]   // 报告顺序
  fields?: { title?: string; company?: string; inspector?: string; device?: string }
}
interface ReportGenerateResult { absolutePath: string; bytes: number; images: number }

@Remote async generate(workspaceFileScope, request, signal): Promise<ReportGenerateResult>
```

`RemoteError('report-generate/failed', msg, { reason })`，reason 枚举（fail loud，客户端按 reason 本地化）：
`'template-missing' | 'template-invalid' | 'too-many-images' | 'no-image-loop' | 'unknown-token' | 'residue' | 'write-failed' | 'failed'`。
`no-image-loop` 的文案指引：模版需 `{{#each_image}}` 括起单图节，或改用 AI 生成。
`unknown-token`：模版出现词表外 `{{...}}`（含拼写错误）→ 生成前静态体检直接拒绝（校验前移，AGENTS「prove each changed acceptance path rejects an invalid case」）。

授权模型照抄 thermal-analyze：`workspaceFiles.readBytes(scope, templatePath, {range:1B})` + `stat` 建立 Session 相对路径授权，输出走 `writeBytes(scope, outputPath, base64)`。

### 2.4 引擎管线（engine.ts，八个阶段）

输入：模版 bytes + images + fields + config。输出：docx bytes。

1. **加载** `AgentDocument.load(templateBytes)`。
2. **投影扫描** `project({story:'body', mode:'text'})`；逐行找 `{{#each_image}}` / `{{/each_image}}` / `{{image_thermal}}` / 全部 `{{token}}`。不成对 → `no-image-loop`；词表外 token → `unknown-token`。
3. **区域分类**：标记行的块若位于表格单元格内 → **行循环**（循环体 = 该 `<w:tr>`）；否则 → **段落范围循环**（循环体 = 两标记间的块）。分类通过锚点 blockRef 找祖先 `tbl`（core `findParentOf`/`localName`，document.ts 同款写法）。
4. **结构克隆**（每个区域，倒序遍历避免引用漂移）：
   - 段落范围：core `serializeXml` 序列化范围内块 → 每张图 `edit({operations:[{kind:'pasteBlocks', afterBlockRef, blocksXml, nodeIds}]})` 逐份插入（nodeIds 由 StableIds 分配器铸造，见 §5 风险 R1）。
   - 表格行：`cloneXml(tr)` 后 splice 进 `tbl.children`（engine 直用 core API），或在 wordinweb 上游加 `tableOp.duplicateRow`（见 §5 R2）。段落属性（含"段前分页"）随 XML 原样克隆，**每图一页由模版承担，引擎不感知**。
5. **文本替换**：重新投影；按新投影把行分组到各克隆实例，逐行计算替换后文本（单图 token 用该实例图片取值），连同全局 token、汇总 token 一起构造成 `AgentPatchHunk[]` → `patch({edits})`；stories = body + 每个 `header:*`/`footer:*`（全局 token 覆盖封面/页眉页脚）。标记行替换为空串后用 `mergeParagraph(blockRef)` 收掉空段。
6. **插图**：再投影；对每个 `{{image_thermal}}` 所在 run：`addAsset(PNG bytes,'image/png')` → `edit({operations:[{kind:'insertImage', runRef, assetRef, widthPx: config.imageWidthPx, heightPx: 等比}]})`（PNG/JPEG 头解析函数从 `dsh-tool-wordinweb/src/index.ts:299` 提到 tokens/engine 复用）。无热图素材（`images.thermal === ''`）→ 仅删标记。
7. **残留自检**：全部 stories 投影后 `/\{\{/` 检索，非 0 → `residue` 失败（不落盘）。
8. **保存**：`save()` → zip 层把 `settings.xml` 加 `<w:updateFields w:val="true"/>`（core 导出 `Package`，词目录页码在 Word 打开时刷新；见 §5 R3）→ `writeBytes` 写输出。

确定性模式**不写 assets/ 目录**（图片内嵌 docx，磁盘素材只有 agent 路径需要）。

### 2.5 token 词表（tokens.ts，两轨共用）

- 全局（现词表保留）：`{{report_title}} {{company}} {{inspector}} {{device}} {{date}} {{camera_model}} {{camera_serial}} {{overall_conclusion}}`。`date` 取宿主当日；camera 两项取第一张图；`overall_conclusion` 规则合成（见下）；其余取 `fields`，未提供替换为空串。
- 汇总（新增）：`{{image_count}} {{overall_max_temp}} {{overall_max_at}} {{overall_max_image}} {{overall_min_temp}} {{overall_avg_temp}}`。
- 单图（现词表保留）：`{{image_file}} {{image_thermal}} {{t_max}} {{t_max_at}} {{t_min}} {{t_avg}} {{marker_table}} {{image_comment}}`。
- 结构：`{{#each_image}} {{/each_image}}`。

`{{overall_conclusion}}` 规则合成：`本次检测共 N 张热像图，最高温度 X℃ 出现于《文件名》（标记名），整体未见/存在异常发热`（阈值 `alarmTempC` 暂不进 Config，v1 固定不判定，只陈述极值——避免拍脑袋阈值）。`{{image_comment}}` v1 同理：`最高 X℃ @坐标，均值 Y℃`；向导可选加"每图分析意见"编辑列（M4，用户改字优先于规则）。
`{{marker_table}}`：在该位置用 `compose` 块语法合成标记统计表（表头/行样式用文档当前默认——保真度不如行循环，模版说明里引导优先用行循环）。

温度格式化 `formatCelsius` 与取值函数 `buildTokenValues(request)` 放 tokens.ts；**客户端 task-book.ts 改为从 `@deepseek-ai/dsh-report-generate/tokens` 导入**格式化与数据块拼装，两轨数值口径逐字符一致。

## 3. 客户端改动（ui-sidebar-documentpreview）

- `report/generate.ts`：`GenerationRequest` 增 `mode: 'fast' | 'ai'`；`fast` 分支调 `generationBridge.deps.generateFast`（新闭包，绑 `remote.reportGenerate.generate`），**不建会话、不 prompt、不轮询**——写盘成功后立即 `recordEditSession(hostSessionId, absoluteOutput)` + `openEditTab(hostSessionId, outputPath)`（编辑会话落在向导所在会话；确定性输出不需要专属对话会话）。
- `report/index.ts`：`ctx.inject` 增加 `'remote.reportGenerate'`，挂 `generationBridge.deps.generateFast`。
- `ReportBuilderTab.tsx` ③输出区：模式单选（`快速生成`默认 / `AI 生成`）+ 可选著录字段（报告标题默认=报告名、委托单位、检测人、设备名称）；fast 模式错误按 reason 映射本地化文案。
- `locales.ts`：新增 zh/en 词条（走 `t`，过 `verify-client-ui-i18n`）。
- `task-book.ts`：数据块改用共享 tokens.ts（AI 路径行为不变，仅同源化）。
- `sample-template.ts` + `模版说明.md`（M4）：示范模版补汇总行循环表与"段前分页"约定；说明改写为"生成时由本地引擎填充（快速）或 AI 填充"。

## 4. 组合、文档与门禁清单

- `packages/bundle/web-app/cordis.patch.yml`：thermal-analyze 条目后加 `report-generate` 插件（无必填 config，default 即可）。
- `packages/bundle/web-app/package.json`：dependencies 加 `"@deepseek-ai/dsh-report-generate": "workspace:*"`（resolver manifest 规则）。
- 新包 README.md / README.zh.md / README.i18n.yaml（dsh-doc 规范）；`pnpm run doc-sync` 把 Config 收进 config catalog。
- 提交前检查（按 dsh-pre-push-checks 裁剪）：新包 vitest + `tests/report.client.spec.ts`、`pnpm run typecheck`、`pnpm run lint`、`pnpm run doc-sync`、改动客户端后 `build:lib:client` + `build:web` 冒烟。
- 快照策略：确定性路径不经过会话、无模型可见输入，不新增 recorded-session 快照；AI 路径未变。若评审认为模式开关需要快照，再补（与 testing.md 对齐）。

## 5. 风险与上游（wordinweb，本仓外）改动

- **R1 pasteBlocks 的 nodeIds**：intent 要求携带新节点 ids。spike 验证铸造方式（agent `cloneWithIds` 的 sidecar 导入模式可参考，`wordinweb/packages/agent/src/document.ts:43`）。不行则退路：core XML 层直接 splice 克隆块 + `doc.refresh()`（同一上游包内已有同款手法）。
- **R2 表格行克隆**：`tableOp rowAbove/rowBelow` 造的是**空行**（`emptyCellLike` 只保留 tcPr，run 格式丢失，`wordinweb/packages/core/src/edit/tables.ts:135,344`）。首选上游加 `duplicateRow` op（同一 splice、`cloneXml(tr)` 代替空单元格，约 5 行）；engine 直用 core splice 亦可，不阻塞。
- **R3 updateFields**：core 尚无 settings.xml 的 updateFields 属性。v1 用引擎层 zip 改写（`Package` 已导出）；上游补属性是后续优化。已知展示差异：WordInWeb 预览显示目录缓存页码，Word 打开后刷新。
- **R4 mergeParagraph 边界**：标记段在文档首段/表格首格时的合并语义，spike 覆盖；退化策略是保留空段（视觉一行空行）。
- **R5 大图片数**：pasteBlocks 逐份插入在几百图量级无性能问题（纯内存 XML）；maxImages 兜底。

## 6. 里程碑（总计约 8–10 人日）

| 里程碑 | 内容 | 验收 |
|---|---|---|
| M0 spike（1d） | `.tmp` Node 脚本：加载示范模版 → pasteBlocks 克隆 each_image×2 → patch 填 token → insertImage → save；顺带验证 R1/R2/R4 | 产物 docx 在 Word 打开：两节、数字正确、热图在位、无 `{{` |
| M1 引擎（2–3d） | 新包骨架 + engine v1（全局/汇总 token、段落范围循环、插图、残留自检、updateFields zip 改写）+ engine.spec | 单测绿：fixture 模版 ×N 图，重开投影断言全文与插图 |
| M2 服务（1d） | TypertRemoteService + web-app 组合 + README 三件套 + doc-sync | Web 端 `remote.reportGenerate.generate` 可调通（临时按钮/控制台） |
| M3 向导（1–2d） | 模式开关 + 著录字段 + fast 分流 + 即时编辑 tab + 错误映射 + client spec | 界面走通：选模版/图 → 快速生成 → 编辑 tab 打开成品 |
| M4 行循环与模板（2d） | R2 行克隆 + 汇总 token + marker_table + 示范模版/说明更新 + task-book 共用 tokens | 汇总表每图一行、样式保真；两轨数值一致测试 |
| M5 收尾（1d） | 全量门禁 + 文档 + （可选）每图意见编辑列 | §4 清单全绿 |

迁移与回退：默认模式切 fast，AI 路径代码原样保留；任一环回退只需把默认值切回 `'ai'`，零删除。

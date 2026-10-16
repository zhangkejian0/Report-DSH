# 热像 AI 标注方案：把 ROI 写进辐射热像文件

状态：**M0–M3 已全部实现（2026-10-16，未提交）**；29 项相关单测全绿，真实 exe 集成冒烟通过，客户端/host 产物已重建（dev server 重启后生效）。本文件保留为设计记录。
前置阅读：`fotric_sdk/SDK使用文档.md`、memory `fotric-irtek-sdk-usage`、`thermal-analysis-web-feature`

## 1. 问题与目标

现状：AI 标注（如"标注绝缘子"）在会话里对**渲染后的 PNG** 画框，产物是普通图片（如 `IR_20210306_0007_标注绝缘子.png`）——热像辐射数据全部丢失，无法再测温、调色板、改温宽，也进不了报告的测温链路。

目标：分析标签页发起标注 → 打开对应会话 → AI 视觉识别目标后**调用 fotric_sdk 把 ROI marker 写进热像 JPEG 的私有容器** → 产物仍是完整辐射热像文件（可继续分析/测温/报告）。标注副本上的 ROI 在分析标签页直接显示为彩色框 + 温度表。

## 2. 已验证的技术事实（2026-10-16 本机实测）

用 `MarkerProbe`/`DumpMarkers`/`ThermalAnalyzeJson` + 新写 `CnNameProbe` 在样例 `IR_20210306_0007.jpg` 上跑通全回路：

| 事实 | 证据 |
|------|------|
| 5 种 marker（Spot/Rectangle/Ellipse/Polygon/Polyline）写入 + `SaveAs` 副本后全部原样回读，几何与温度值正确 | DumpMarkers 输出 9 个 marker（原 4 + 新 5），`Mpg` 三点多边形、`Mel` 椭圆均准确 |
| `SaveAs` 保留辐射数据 | 副本上 `ThermalAnalyzeJson` 正常输出全场温度（min 17.06 / max 22.55℃）与 result.json |
| **中文 marker 名** UTF-8 完整往返 | `CnNameProbe` 写入 `绝缘子1`，回读 bytes `E7-BB-9D-...-31` |
| SDK 渲染热图 PNG 尺寸 = 热像分辨率 | `thermal.png`/`cover.png` 均 640×480，与 marker 坐标空间 1:1 |
| 写入非破坏 | marker 存于 EOI 后私有容器，JPEG 预览不变；原文件不动（写副本策略） |

结论：**SDK 侧能力完全满足需求，且无需逆向容器格式**——`CreateMarker*` + `Markers.Add` + `SaveAs` 即全部所需。

## 3. 现有链路盘点（复用点）

- 只读分析：`packages/document/thermal-analyze`（Remote `thermalAnalyze.analyze`，跑 `ThermalAnalyzeJson.exe <img> <outdir>`，workspaceFiles 1 字节读 + stat 授权，退出码 2/3 → not-thermal/open-failed）。
- 分析标签页：`packages/client/ui-sidebar-documentpreview/src/client/thermal/`（`{sessionId, path}` 目标、marker 框叠加渲染、marker 温度表、手动重试刷新）。
- 会话派发先例（直接照抄的模板）：报告向导 AI 模式 `report/generate.ts` —— `writeBytes` 写资产 → `buildTaskBook` 任务书 → `createSession(root)`/复用记录的会话 → `prompt` → `navigate(sessionId)` → 轮询产物回种 tab。
- agent 工具先例：`packages/document/tool-wordinweb`（`ctx.tools.register`，按 `exec.agent` 隔离状态，base bundle 装配）。
- 写路径与刷新：`workspaceFiles.writeBytes`（写授权已存在）；`workspaceFiles.changes(target)`（文件 watch，可做自动刷新）。
- 装配点：`thermal-analyze` 在 `packages/bundle/web-app/cordis.patch.yml`（`analyzerPath` 指向 exe）；`tool-wordinweb` 在 `packages/bundle/base/cordis.patch.yml`。

## 4. 方案总览

四层，每层独立可测可交付：

```
分析标签页 [AI 标注] 按钮                     ← M3（UI 入口 + 会话任务书 + 回种）
        │ createSession + taskBook + navigate
        ▼
标注会话（agent）──调用──▶ tool-thermal 工具   ← M2（thermal_image_analyze / thermal_image_mark）
        │ Host 进程内授权 + execFile
        ▼
MarkImage.exe（x86 .NET，fotric_sdk）          ← M0（写 marker + SaveAs 副本）
        ▼
<stem>_标注.jpg（辐射热像 + ROI marker）
        ▲
ThermalAnalyzeJson.exe 全类型 marker 导出      ← M1（分析报告 + 客户端渲染增强）
```

**形态决策：agent tool 包为主，不引入 MCP/独立 CLI 服务。** 理由：
- 用户诉求是"DSH 会话里 AI 调用"，`ctx.tools.register` 是仓库既有模式（tool-wordinweb 先例），无新进程、无跨进程协议。
- exe 本身就是 CLI（fotric_sdk 工具集一员），命令行直用/脚本化天然可用。
- MCP 包装（供 Claude/ZCode 等外部客户端）留作可选 M4——同一 exe 换一层 stdio 壳，零核心改动。

## 5. 分层设计

### M0 — `MarkImage.exe`（fotric_sdk/AnalyzeImage/）

```
用法: MarkImage.exe <src.jpg> <dst.jpg> <markers.json>
markers.json:
{
  "markers": [
    {"type":"rectangle","name":"绝缘子1","rect":[x,y,w,h]},
    {"type":"ellipse",  "name":"绝缘子2","rect":[x,y,w,h]},
    {"type":"polygon",  "name":"套管",   "points":[[x,y],[x,y],...]},
    {"type":"polyline", "name":"线温",   "points":[[x,y],[x,y],...]},
    {"type":"spot",     "name":"热点",   "point":[x,y]}
  ]
}
stdout 成功: {"ok":true,"dst":"...","count":N,
             "markers":[{"name":"绝缘子1","type":"rectangle",
                         "max":..,"min":..,"avg":..,"hotSpot":[x,y],"coldSpot":[x,y]}]}
退出码: 0 成功 / 2 非热像 / 3 打开失败 / 4 spec 无效或异常 / 5 SaveAs 失败（对齐现有工具约定）
```

实现要点（全部有探针先例）：
- `validator.IsValidFile` 校验 → `Create`/`Open` → 逐个 `CreateMarker<类型>(name, new LocalThermalParams(), geometry)` → `Markers.Add` → 坐标 clamp 进 `[0,w)×[0,h)` → `SaveAs(dst)` → 重开 dst 回读每个 marker 的统计值进 stdout JSON（AI 立即拿到 ROI 测温结果，无需再调一次分析）。
- `LocalThermalParams` 用默认全局参数（MarkerProbe 先例）；后续可扩展 per-marker `emissivity` 等字段。
- 编译：`-platform:x86`，DLL 就近依赖（Amur.IRtekFileSDK/Amur.ThermalImageFile/IRtekFileSDK）。
- **每次标注从原始文件 fork**（src=原图，dst=新副本）→ 天然幂等，不依赖 `Markers.Clear`；命名冲突时由调用方（Node 侧）加序号。

压测项（M0 验收）：单文件写 50/100 个 marker 后 SaveAs + 回读正常（容器是否有数量上限未知）。

### M1 — 分析报告与渲染支持全类型 marker

`ThermalAnalyzeJson.cs` 现状只导出 `IMarkerRectangle`（其余 `continue` 跳过），椭圆/多边形标注将不可见。扩展：

- JSON markers 数组每项增加：`type` 全类型；Polygon/Polyline 加 `"points":[[x,y],...]`；Spot 加 `"point":[x,y]`；Rectangle/Ellipse 保持 `rect`（Ellipse 的外接矩形 = 其 Rectangle 属性）。`rect` 对所有类型保留（外接矩形，UI 兜底）。
- `thermal-analyze/src/types.ts` `ThermalMarker` 加可选 `points`/`point` 字段（同版本安全变更：verifyAnalysis 不变，客户端按 type 分支渲染）。
- `ThermalAnalysisTab.tsx` 渲染分支：Rectangle 画框（现状）；Ellipse 画框 + `border-radius:50%`；Polygon 用 SVG overlay 画闭合折线；Polyline 画折线；Spot 画圆点。表格已有 max/min/avg 列（Spot 的 min/avg 显示同 value）。UI copy 走 `sidebarThermal` 字典（i18n 门禁）。

### M2 — `packages/document/tool-thermal`（agent 工具）

两个模型可见工具（`ctx.tools.register`，`inject: ['tools']`）：

- `thermal_image_analyze(path)`：跑 `ThermalAnalyzeJson.exe`，返回完整 `ThermalAnalysis`（stats/markers/grid/尺寸），让 AI 可二次查询温度场（如"找温度最高的绝缘子"）。exe 与 thermal-analyze 服务共用同一个（`analyzerPath` 配置），报告 JSON 代码零重复。
- `thermal_image_mark(path, markers, output?)`：跑 `MarkImage.exe`。`path`=原始辐射 JPEG；`markers`=全量 marker 列表（schema：类型判别联合 + 中继 `assertNever`）；`output` 缺省 `<stem>_标注.jpg`（冲突自动加序号）。返回回读的 ROI 温度统计 + 输出路径。

配置（`Config`，cordis.yml 注入，无硬编码 tunable）：

```yaml
- id: tool-thermal
  name: '@deepseek-ai/dsh-tool-thermal'
  config:
    analyzerPath: D:/agworkspace/docgc/deepseek-harness/fotric_sdk/AnalyzeImage/ThermalAnalyzeJson.exe
    markerPath:   D:/agworkspace/docgc/deepseek-harness/fotric_sdk/AnalyzeImage/MarkImage.exe
    timeoutMs: 60000
```

授权与路径：工具按 `ToolRunContext` 的 agent/session 解析会话工作区根，相对路径约束在工作区内（沿用 thermal-analyze 的"1 字节读 + stat 取绝对路径"模式；mark 另需写目标落在工作区内）。实现期对齐 `resolveWorkingDirectory(exec)` 的既有机制（`core/tools/src/ptc.ts` 已有该 seam）。

工具描述要点（写给模型看）：坐标 = 热图像素坐标（0..width-1, 0..height-1），与调色板热图 PNG 同尺寸 1:1；markers 每次全量输出；输出是新副本、原图不动。

交付物：包 + 单测（含 workspaceFiles 授权、退出码映射、marker spec 校验拒绝路径）+ base bundle 装配 + agent 工具列表快照（SDK 双投影要求）。**M2 完成即闭环最小可用**：会话里手输"标注 XX"AI 已能完成写文件。

### M3 — 分析标签页 "AI 标注" 入口 + 会话任务书

`ThermalAnalysisTab` header（report 按钮旁）加 `AI 标注` 按钮 → 弹出目标输入（默认空，placeholder 例："标注图中所有绝缘子"）→ 确认后走报告向导 AI 模式同一骨架（`report/generate.ts` 为模板抽公共或薄封装）：

1. **写资产**：`writeBytes`（host session 授权）把 `images.thermal`（调色板热图 PNG）与 `ThermalAnalysis` 摘要 JSON 写入 `标注资产/<stem>/`。
2. **派发**：一图一标注会话（记录复用，同报告编辑会话的 adopt-if-fresh 模式；无记录则 `createSession(root)`）→ `prompt(taskBook)` → `navigate(sessionId)`。
3. **任务书**（`buildTaskBook` 同风格）：
   - 用户目标（原文）；
   - 原图 workspace 相对路径 + 热像尺寸（width×height，坐标即热图像素）；
   - 热图 PNG 资产路径（视觉识别用图，与坐标 1:1）；
   - 已有 marker 摘要与全场统计（供 AI 参考）；
   - 工具约定：识别 → 全量 markers → `thermal_image_mark` 一次写入；需要温度判据时先 `thermal_image_analyze`；
   - 输出约定：`<stem>_标注.jpg`；完成后在会话里逐 ROI 汇报温度发现（max/avg、异常判断）。
4. **回种**：轮询 dst 出现（报告向导 SEED 模式同款，3 分钟上限）→ `openResource(analysisAddressFor(dst 文件地址))` 在当前会话打开副本的分析标签页 → ROI 框 + 温度表即刻可见。

可选增强（M3 内或后续）：分析标签页用 `workspaceFiles.changes(path)` watch 目标文件，变更自动重跑分析（覆盖"原地写"或外部修改场景）。

### M4 — 可选扩展（不在本方案承诺内）

- MCP stdio server 包装同一对 exe（供 ZCode/Claude 等外部客户端）：`thermal_analyze` / `thermal_mark` 两 tool。
- 分析标签页人工标注模式（手画 ROI → 同一 `thermal_image_mark` 落盘）。
- Delta（温差）marker：SDK `CreateDelta` 已支持，报告与 UI 增列。

## 6. 关键设计决策

| 决策 | 选择 | 理由 |
|------|------|------|
| 写副本 vs 原地改 | **写副本** `<stem>_标注.jpg` | 原图不可变（用户数据安全）；每次从原图 fork 天然幂等；产物命名与用户现有 PNG 标注习惯一致 |
| 工具形态 | agent tool 包 | DSH 会话内闭环、仓库既有模式；exe 即 CLI；MCP 可后包 |
| marker 类型 | 全 5 种，AI 按目标选 | 绝缘子串=多边形更准、区域=矩形、线状=折线；需 M1 渲染扩展配套 |
| AI 识图输入 | SDK 渲染调色板热图 PNG | 比灰度 JPEG 预览信息量大得多；且与坐标 1:1（已实测 640×480） |
| 中文 marker 名 | 直接写 | 已实测 UTF-8 往返无损，UI/报告零映射层 |

## 7. 风险与待验证

- ⚠️ marker 数量上限：容器对几十~上百个 marker 的承载未测（M0 压测项）。
- ⚠️ `Clear(ClearType.UserMarkers)` 语义：是否误清设备自带 Ar1..Ar3（本方案不依赖 Clear，仅人工迭代场景需要，实现前探针确认）。
- ⚠️ agent 工具的会话工作区解析：`ToolRunContext → cwd/workspace` 的既有 seam 需实现期对齐（tool-wordinweb 用全局 config.root 的做法不适用于多工作区）。
- ⚠️ 大 marker JSON 的工具参数体量：几十个框无碍；如需百级 polygon，可在 mark 工具支持引用会话工作区内 spec 文件路径。
- ✅ 已消除：SaveAs 辐射完整性、全类型回读、中文名、坐标空间 1:1。

## 8. 交付顺序与验收

| 里程碑 | 交付物 | 验收 |
|--------|--------|------|
| M0 | `MarkImage.exe` + 源码入 `fotric_sdk/AnalyzeImage/` | 样例图写 5 类型 + 50 个矩形压测回读全对 |
| M1 | ThermalAnalyzeJson 全类型导出；types；tab 渲染 | 标注副本在分析页显示多边形/椭圆/点框与温度表；单测 + i18n 门禁 |
| M2 | `tool-thermal` 包 + base 装配 | 会话手输"标注绝缘子"产出 `_标注.jpg`；单测（授权、退出码、spec 拒绝）+ 工具快照 |
| M3 | 分析页入口 + 任务书 + 回种 | 端到端：按钮 → 会话 → 副本分析页自动打开；关键路径 snapshot |

实施遵循仓库门禁：pre-push 按 `dsh-pre-push-checks` 选最小相关集（新工具单测、UI i18n、快照、doc-sync）；UI copy 走字典；model-visible 输入（新工具）进 session 事件与快照。

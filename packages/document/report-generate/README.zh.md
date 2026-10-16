---
description: "把一份 Word 模版确定性地展开为成品检测报告。"
kind: "package-reference"
---

# @deepseek-ai/dsh-report-generate

[English](README.md) | 中文

## 概述

在宿主上用一份模版 .docx 和热像分析器的测量结果生成一份 Word 检测报告——无模型、无会话、无 agent。引擎按每张图片展开模版的单图节、替换全部占位符（报告头、全报告汇总、单图测量值）、插入各图热图，并让 Word 打开时刷新目录页码等域。同样输入产出逐字节一致的报告。

## 目录

- [使用本包](#use-this-package)
- [理解实现](#understand-the-implementation)
- [Model Experience](#model-experience)
- [已知限制与后续工作](#known-limitations-and-deferred-work)
- [开发备注](#dev-note)

-----

<a id="use-this-package"></a>
## 使用本包

[Web bundle](../../bundle/web-app/README.zh.md) 以 `report-generate` 挂载本提供者。独立组合把 `@deepseek-ai/dsh-report-generate` 作为 `cordis.yml` 的一行挂载。

浏览器以 `reportGenerate.generate` Remote 方法发起生成：带会话身份、模版与输出的工作区路径、已测图片和可选的著录字段。模版读取与报告写入都走 `workspaceFiles`，沿用会话的常规授权。`api/remotes` 挂载生成的客户端描述符。

| 字段 | 必填 | 默认 | 含义 |
|---|---|---|---|
| `imageWidthPx` | 否 | `460` | 热图插入宽度；高度按原始比例。 |
| `maxImages` | 否 | `500` | 单次生成最多展开的图片数。 |

占位符词表与循环约定写在报告向导播种的模版说明里（`报告模版/模版说明.md`）：`{{report_title}}` 一类的报告头占位符、`{{overall_max_temp}}` 一类的全报告汇总、`{{t_max}}` 一类的单图占位符，以及每个重复节一个 `{{#each_image}}`…`{{/each_image}}` 区域——正文中的括起块范围，或两个标记同处一行的表格行循环；`{{#each_marker}}` 表格行为每图每个标记复制一行（展平的标记明细表）。

字段按模板可配置：模板旁的 `<模版名>.tokens.json` 可新增自定义占位符（中文名可用）、把内置字段改绑到其它分析路径与格式、声明生成时的著录项（向导随之渲染表单）。三种绑定覆盖取值（`path` 加命名 `format`）、著录（`input`）与派生文本（`template` 引用其它占位符）；免配置时 `{{analysis.params.distance|number:1}}` 一类的路径占位符直接取分析 JSON，无 sidecar 的模板行为与内置词表完全一致。

<a id="understand-the-implementation"></a>
## 理解实现

一次调用在 `src/engine.ts` 的纯引擎上运行，基于 WordInWeb 文档模型——与 `word_document_*` 工具驱动的是同一个库。引擎扫描模版文本投影（校验占位符与标记配对）、在 XML 树上结构性地克隆各循环区域、通过投影补丁 API 在正文/页眉/页脚各 story 替换占位符、插入热图 PNG，并在任何 `{{` 残留时拒绝产出。失败以一个 `report-generate/failed` Remote 错误出现，`reason` 细节为 `template-invalid`、`template-markers`、`template-too-large`、`no-image-loop`、`unknown-token`、`too-many-images`、`residue`、`write-failed` 或 `failed`；工作区拒绝原样透传，生成拒绝时不写任何文件。

<a id="model-experience"></a>
## Model Experience

无。本包确定性生成文档，不含面向模型的工具、消息或会话事件。

#### KV Cache effect

无；生成不构造、不修改任何模型请求。

<a id="known-limitations-and-deferred-work"></a>
## 已知限制与后续工作

- 报告向导的 AI 路径仍是未写显式循环标记模版的退路；确定性引擎以 `no-image-loop` 拒绝它们。
- `{{marker_table}}` 渲染为行内文本；表格形式用 `{{#each_marker}}` 行循环覆盖。
- 引擎按单个 2000 块投影窗口工作；超出时以 `template-too-large` 失败，不做分页。
- 表格行循环要求两个循环标记在同一表格行内；跨行拆分的标记会被拒绝。

<a id="dev-note"></a>
### 开发备注

<details>
<summary>维护者的工作上下文——点击展开</summary>

依赖 `@wordinweb/core` 公开的 `setUpdateFields`；该方法为 `<w:updateFields/>` 设置开关，添加在 `setEvenAndOddHeaders` 旁。
</details>

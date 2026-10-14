---
description: "基于 WordInWeb agent 工具包的模型可调用 Word 文档工具。"
kind: "package-reference"
---

# @deepseek-ai/dsh-tool-wordinweb

[English](README.md) | 中文

## 概述

将 MIT 许可的 [`@wordinweb/agent`](https://www.npmjs.com/package/@wordinweb/agent) 包提供的可移植 `word_document_*` 工具集注册到 Harness ToolRuntime：schema 化的 DOCX 组装、渐进式检查、事务化编辑、文本投影与补丁、能力 schema 与资源读取，另有四个文件系统工具——`word_document_new`、`word_document_open`、`word_document_save`、`word_document_add_image`。每个 agent（会话）持有一份独立的 `AgentDocument`，并发会话互不串扰；省略的 `revision` 字段由调用 agent 的活动文档自动填充。全部在进程内完成，不依赖浏览器。

## 目录

- [使用此包](#use-this-package)
- [了解实现](#understand-the-implementation)
- [模型体验](#model-experience)
- [已知限制与延后工作](#known-limitations-and-deferred-work)
- [开发备注](#dev-note)

-----

<a id="use-this-package"></a>
## 使用本包

Base 组合包以 `tool-wordinweb` 挂载本插件。独立组合以 `cordis.yml` 行挂载 `@deepseek-ai/dsh-tool-wordinweb`。

| 字段 | 默认 | 含义 |
| --- | --- | --- |
| `workspace` | 宿主进程工作目录 | 相对 `.docx` 路径的解析基准目录。 |
| `editorUrl` | — | 保存结果中附带 `editorUrl` 的 Word 编辑器基础 URL，如 `http://127.0.0.1:5180`。 |

<a id="understand-the-implementation"></a>
## 了解实现

插件在注册时从 `AgentDocument.create().tools()` 采集名称、描述与 JSON Schema，注册的 schema 因此不会偏离包契约。包自带的 `word_document_save`（返回文档字节）被写文件的实现替换：在配置的 workspace 下写出 `.docx` 并返回绝对路径。各 schema 的 `required` 中移除了 `revision` 并在分发时自动填充：edit 与 patch 优先用调用方提供的 revision，其次上次投影的 revision，最后活动文档的 revision。过期 revision 由包自身的校验以工具错误形式暴露。

| 工具 | 职责 |
| --- | --- |
| `word_document_new` | 新建空白 DOCX（compose 前必须调用）。 |
| `word_document_open` | 从磁盘加载 `.docx`；返回 overview。 |
| `word_document_compose` | 用 schema 化的块组装完整新文档。 |
| `word_document_add_image` | 将磁盘图片（.png/.jpg）注册为文档资产；返回 `assetRef` 与原始像素尺寸，供 compose 的 image 块与 insertImage 编辑引用。 |
| `word_document_capabilities` | 按类别或 kind 列出操作 schema。 |
| `word_document_inspect` | overview、有界 context、读取窗口、搜索、对象详情、页面几何。 |
| `word_document_edit` | 对观察到的 revision 应用已校验的事务。 |
| `word_document_project` | 将 story 投影为带行锚点的确定性文本或 markdown。 |
| `word_document_patch` | 对投影应用行范围编辑或统一 diff。 |
| `word_document_asset` | 以 base64 读取图片或内嵌资源。 |
| `word_document_save` | 将当前文档写入磁盘；返回路径、大小、revision 与可选 `editorUrl`。 |

<a id="model-experience"></a>
## 模型体验

### 请求上下文与条件

#### 模型看到的内容

插件挂载期间的十一个 `word_document_*` 工具 schema。七个包工具原样携带自身描述与 JSON Schema（仅从 `required` 移除 `revision`）；四个文件系统工具声明上表所示的小型对象 schema。

#### Token 影响

条件性：仅当插件在活动组合中挂载时，各工具的固定 schema 加入提示词组装。检查结果受包自身的预算约束（默认 `context` 预算为 100 块、24,000 字符）。

#### KV Cache 影响

对稳定组合为追加式：schema 在插件挂载时加入提示词前缀，卸载时离开并使复用失效。工具结果是普通的逐轮消息内容，不使前缀失效。

## 已知限制与延后工作

<a id="known-limitations-and-deferred-work"></a>

- **文档按 agent 作用域化** —— 每个 agent（会话）的调用解析到自己的 `AgentDocument`；不带 agent 的执行（直连注册表、测试）共享一份回退文档。会话处置时其条目随之移除。
- **未接入文件系统 seam** —— open/save 通过 `node:fs` 读写而非 `ctx.fs`，沙箱与观察策略不作用于 `.docx` 路径；迁移到 fs seam 已推迟。
- **参数不做子集校验** —— 原始工具参数原样透传给模型提供方；包内 `$defs`/`$ref` 用法经测试提供方接受，如有提供方拒绝可增加清洗投影。

<a id="dev-note"></a>
### 开发备注

<details>
<summary>维护者的工作上下文 —— 点击展开</summary>

无。
</details>

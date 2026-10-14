---
description: "通过 Fotric IRtek SDK 分析器处理经授权的热像图。"
kind: "package-reference"
---

# @deepseek-ai/dsh-thermal-analyze

[English](README.md) | 中文

## 概述

在宿主计算机上分析一张辐射测温热像 JPEG，并返回分析器的完整报告：相机信息、测温参数、全幅温度统计、内置测量标记、分块平均温度网格，以及调色板渲染的 base64 PNG 图像。温度单位为摄氏度。

## 目录

- [使用此包](#use-this-package)
- [理解实现](#understand-the-implementation)
- [模型体验](#model-experience)
- [已知限制与延后工作](#known-limitations-and-deferred-work)
- [开发备注](#dev-note)

-----

<a id="use-this-package"></a>
## 使用此包

[Web bundle](../../bundle/web-app/README.zh.md) 以 `thermal-analyze` 挂载本提供方。独立组合以 `cordis.yml` 行挂载 `@deepseek-ai/dsh-thermal-analyze`。

浏览器通过 `thermalAnalyze.analyze` Remote 方法提交 Session 身份与图片路径。该入口使用 `workspaceFiles` 完成授权：一次单字节读取加一次 stat 确认与文档预览一致的 Session 相对路径，分析器只接收解析后的绝对路径。`api/remotes` 挂载生成的 Client 描述符。

| 字段 | 必填 | 默认值 | 含义 |
|---|---|---|---|
| `analyzerPath` | 是 | — | 分析器可执行文件的绝对路径。 |
| `timeoutMs` | 否 | `60000` | 分析器时限，含进程启动。 |
| `maxOutputBytes` | 否 | `16777216` | 可接受的报告最大字节数。 |

分析器可执行文件是仓库工作区内的 Fotric IRtek 文件 SDK CLI（`fotric_sdk/AnalyzeImage/ThermalAnalyzeJson.exe`，Windows x86）。它通过 SDK 校验文件，把一份 `result.json` 写入私有临时目录；否则以非零退出码在 stdout 输出一行 `ERROR <code>`。

<a id="understand-the-implementation"></a>
## 理解实现

一次调用拉起分析器，在 `timeoutMs` 内等待，对报告做大小与结构校验（对照声明的结果词汇表），并总是删除临时目录。失败统一表现为 `thermal-analyze/failed` Remote 错误，`reason` 细节为 `unavailable`（分析器或服务缺失）、`not-thermal`（非辐射热像文件）、`open-failed`、`timeout`、`too-large` 或 `failed`；取消表现为 `gateway/cancelled`。

<a id="model-experience"></a>
## 模型体验

无，因为此包只运行外部分析器，不提供面向模型的工具、消息或 Session 事件。

#### KV Cache effect

无；分析不构造也不修改模型请求。

## 已知限制与延后工作

<a id="known-limitations-and-deferred-work"></a>

- 分析器可执行文件仅支持 Windows x86；其他平台在首次调用时以 `unavailable` 或 `failed` 失败，而不是在加载时。
- 除结构字段校验外，报告解析信任仓库随自身交付的分析器可执行文件；替换该可执行文件属于部署事项，不属于校验事项。
- 热像视频与声像文件不在范围内；分析器以 `not-thermal` 拒绝它们。

<a id="dev-note"></a>
### 开发备注

<details>
<summary>维护者的工作上下文 —— 点击展开</summary>

无。
</details>

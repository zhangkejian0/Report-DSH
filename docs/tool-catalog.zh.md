<!-- 英文源文件由 scripts/gen-tool-catalog.ts 生成；本中文文件是通过双语配对维护的经评审对侧。
     更新时先运行 `pnpm run gen-tool-catalog` 更新英文，再更新本文件并运行 `pnpm run verify-translation-pairing --write docs/tool-catalog.md` 重新记录配对。 -->

# 工具 Schema 目录

[English](tool-catalog.md) | 中文

已发布插件向 `ctx.tools` 提供的所有面向模型的工具：模型通过系统提示词组装获得的 `name`、`description` 和 JSON Schema `parameters`。本目录是[子系统页面](subsystems/core.zh.md)（类型及每页生成的 `cordis-surface` 接线区域）的补充；本页列出的是向 agent（智能体）提供的*工具*。

英文源文件由系统**生成**，并通过 `pnpm run verify-tool-catalog`（`doc-sync`（文档同步门禁）的一部分）验证新鲜度；本中文文件作为经评审对侧通过双语配对维护。与 Cordis 目录（纯源码 AST 处理）不同，英文生成器会在真实上下文中**启动**每个工具插件并读取 `ctx.tools.schemas()`，因为工具 schema 无法通过静态分析完全确定，例如运行时展开的枚举、拼接的描述、由配置决定的名称以及使用原始 JSON Schema 的 MCP 工具。完整性守卫会 glob 匹配 `packages/*/tool-*`；如果生成器的启动 manifest（元数据清单）遗漏任何包，检查就会失败，因此新工具不会在无人察觉的情况下缺少文档。

范围：`packages/*/tool-*` 下已发布的产品工具，每个工具均使用其**默认**配置启动；但如果某个 Config 字段是**必填项**且没有默认值，生成器就必须作出选择，对应包的说明会记录本页展示的是哪个分支。注册的工具**名称**可以是加载时配置，例如 `tool-subagent` 的 `toolName`，因此部署可能以不同名称或额外名称提供某个包；如果存在随产品发布的别名，对应包的说明会予以记录。`examples/` 中的演示工具（例如 `echo`）不在范围内，这与 Cordis 目录仅涵盖包的范围一致。

<a id="tool-package-map"></a>

## 工具包映射

下表将模型可见的工具名称与其背后的插件包和服务 seam 对应起来。各包章节随后给出确切的 JSON Schema。

| 工具包 | 模型可见名称 | 依赖 | 写入／影响 | 随产品发布的别名 | 部署说明 |
| --- | --- | --- | --- | --- | --- |
| `@deepseek-ai/dsh-plugin-manager` | `plugin_manager` | `ctx.tools`, `ctx.pluginManager`, `ctx.sandboxPolicy` | `tool/call`, `tool/result`, `user/message` | - | - |
| `@deepseek-ai/dsh-mcp-resources` | `list_mcp_resource_templates`, `list_mcp_resources`, `read_mcp_resource` | `ctx.tools`, `ctx.mcpResources` | `tool/call`, `tool/result` | - | - |
| `@deepseek-ai/dsh-experimental-browser-use-stagehand-native` | `stagehand_act`、`stagehand_extract`、`stagehand_navigate`、`stagehand_observe`、`stagehand_screenshot`、`stagehand_tabs` | `ctx.browserUse`、`ctx.agents`、`ctx.tools`、`ctx.systemPrompt` | `tool/call`、`tool/result` | - | - |
| `@deepseek-ai/dsh-tool-ask-user` | `ask_user_question` | `ctx.tools`、`ctx.userQuestions` | `tool/call`、`tool/result after an answer or timeout`、`late user/message` | - | ask_user_question 默认保持原有阻塞行为；设置 `mode: timed` 后才启用前台超时与 pending 结果，同时问题仍可回答；timed 模式内 `timeout: -1` 让本次调用无限期阻塞。 |
| `@deepseek-ai/dsh-tools` | `run_code` | `ctx.workingDirectory (Agent executions)`、`ctx.tools`、`ctx.ptcRuntime (execution time)`、`ctx.systemPrompt` | `tool/call`、`one tool/ptc-dispatch-start + tool/ptc-dispatch pair per bridged sub-call`、`tool/result` | - | 在 `mode: ptc` 下，它由工具注册表所有，作为可过滤能力层之外的保留传输机制（参见 PTC mode Agent Note）。在 `ptc` 下，它是注册表对协议格式（wire format）的唯一贡献；其他可见能力在使用已加载运行时语言生成的 SDK 章节中声明。程序通过 binding 调用这些能力，调用按照原生并发约定调度：启动顺序和策略遵循提交顺序，并发安全的函数体最多重叠执行 `maxParallelSubCalls` 个。调用会重新进入完整且受守卫保护的工具流水线，并将每个嵌套执行关联到此外层结果。 |
| `@deepseek-ai/dsh-plan-mode` | `exit_plan_mode` | `ctx.tools`、`ctx.systemPrompt`、`ctx.userQuestions (execution time, opportunistic)` | `tool/call`、`plan/mode inactive on an approved review`、`tool/result` | - | 规划未激活时，exit_plan_mode 仍保留在面向模型的 schema 中，这样状态转换不会在规划策略变更之外额外造成工具目录变动。其执行路径会拒绝规划模式之外的调用；在规划模式下，它通过用户交互 seam 提交计划（批准／根据反馈继续规划），批准后会在步骤边界记录规划模式已停用。 |
| `@deepseek-ai/dsh-tool-bash` | `bash` | `ctx.workingDirectory`、`ctx.tools`、`ctx.shell`、`ctx.systemPrompt`、`ctx.shellEnv`、`ctx.jobs for run_in_background and the job-backed foreground path` | `tool/call`、`tool/result` | - | bash 工具是 bash 执行器 seam 面向模型的消费方。组合中有 job 注册表时，每次调用一启动就注册到通用 `ctx.jobs` 运行时，并通过 `job_*` 工具（来自 `@deepseek-ai/dsh-tool-jobs`）收集／停止；没有注册表或 `enableRunInBackground: false` 时，工具注册不带 `run_in_background` 参数的纯前台 schema。 |
| `@deepseek-ai/dsh-tool-present` | `present` | `ctx.workingDirectory`、`ctx.tools`, `ctx.fs`, `ctx.sessionProjections` | `tool/call`, `deliverables/presented 在成功的最终结果之后`, `tool/result` | - | 交付归调用方 Session 所有；Web ui-deliverables 提供源文件打开与卡片。 |
| `@deepseek-ai/dsh-tool-pwsh` | `pwsh` | `ctx.workingDirectory`、`ctx.tools`、`ctx.shell`、`ctx.systemPrompt`、`ctx.shellEnv`、`ctx.jobs for run_in_background and the job-backed foreground path` | `tool/call`、`tool/result` | - | pwsh 工具是 Windows 组合中 bash 执行器 seam 的 PowerShell 方言消费方（由 `@deepseek-ai/dsh-pwsh-local` 等 PowerShell 执行器为 `ctx.shell` 提供后端）；除沙箱接口外，它逐项对应 bash 工具调用。使用 `run_in_background` 的运行会注册到通用 `ctx.jobs` 运行时，并通过 `job_*` 工具收集／停止；托管的 `DSH_*` 环境来自 `@deepseek-ai/dsh-shell-env`。每次调用都在新进程中运行，不使用持久 PTY 会话。路径采用原生 `C:\...` 形式，变量采用 `$env:NAME`。 |
| `@deepseek-ai/dsh-tool-cordis` | `cordis_inspect_list`, `cordis_inspect_query` | `ctx.tools`, `ctx.cordisInspect` | `tool/call`, `tool/result` | - | 创造模式提供两个只读运行时检查工具。Cordis host runner 提供检查注册表；Client 查询需要已连接页面。持久化变更编写为组合包，再通过 plugin_manager 安装。 |
| `@deepseek-ai/dsh-tool-bash-persistent` | `bash` | `ctx.workingDirectory`、`ctx.tools`、`ctx.terminals`、`an owning Agent at execution time` | `tool/call`、`PTY shell state`、`tool/result` | - | 一个按所有者隔离的持久 bash 工具；部署组合提供 PTY 后端，并可覆盖面向模型的环境描述。 |
| `@deepseek-ai/dsh-tool-pwsh-persistent` | `pwsh` | `ctx.workingDirectory`、`ctx.tools`、`ctx.terminals`、`an owning Agent at execution time` | `tool/call`、`PTY shell state`、`tool/result` | - | 一个按所有者隔离的持久 pwsh 工具，持久 bash 工具的 Windows 对应物；部署组合提供 pwsh 方言的 PTY 后端，并可覆盖面向模型的环境描述。 |
| `@deepseek-ai/dsh-tool-str-replace-editor` | `str_replace_editor` | `ctx.tools`、`ctx.fs` | `tool/call`、`fs/observed after view presence/absence, edit absence, or successful mutation`、`tool/result` | - | 基于文件系统 seam 的独立查看／创建／唯一字面量替换／按行插入工具；可与任何 shell 或终端接口组合。 |
| `@deepseek-ai/dsh-tool-fs` | `edit`、`read`、`read_image`、`write` | `ctx.workingDirectory`、`ctx.tools`、`ctx.fs`、`ctx.systemPrompt`、`ctx.attachments (image-tool registration)`、`ctx.llm + an image-capable route (image-tool execution)` | `tool/call`、`fs/write-intent or fs/edit-intent for mutations`、`fs/observed after read presence/absence or successful file operation`、`durable attachment (read_image)`、`tool/result` | - | 先读后写／编辑策略由 `@deepseek-ai/dsh-fs-observation-policy` 添加；它是一个 `fs/*` 事件门禁插件，不会改变 schema。加载这些工具的部署按预期也应加载该插件。没有 `ctx.attachments` 时图片工具不会注册；其 schema 与路由无关，执行时除非确切路由的模型声明图片输入，否则拒绝。 |
| `@deepseek-ai/dsh-tool-fs-search` | `glob`、`grep` | `ctx.workingDirectory`、`ctx.tools`、`ctx.subprocess`、`ctx.systemPrompt` | `tool/call`、`tool/result` | - | glob 和 grep 是无条件可用的发现工具，通过 ctx.subprocess spawn 随包提供的 ripgrep 二进制文件（`@vscode/ripgrep`），并作为普通前台调用运行，绝不作为后台任务；无需在宿主机安装 `rg`，也不经过 shell 层。本目录使用 `sampleOverCapGlobResults: true`；部署必须显式选择该行为。结果超过上限时，会通过可选的 ctx.spillStore 后端保存完整的格式化列表；在共置部署中，如果后端公开本地路径，返回的定位信息可供后续读取／搜索。 |
| `@deepseek-ai/dsh-tool-terminal` | `terminal_close`、`terminal_list`、`terminal_open`、`terminal_read`、`terminal_send`、`terminal_signal` | `ctx.workingDirectory`、`ctx.tools`、`ctx.terminals`、`ctx.systemPrompt`、`ctx.jobs at call time for run_in_background` | `tool/call`、`tool/result` | - | 这 6 个终端工具需要选择启用，用于补充一次性 bash／文件系统工具。`terminal_send(run_in_background: true)` 会注册到 `ctx.jobs`；schema 不包含 TUI、具名按键序列、BEL、调整尺寸、自动启动和跨 agent 共享。 |
| `@deepseek-ai/dsh-tool-goal` | `create_goal`、`get_goal`、`update_goal` | `ctx.tools`、`ctx.agents`、`ctx.goals`、`ctx.systemPrompt`、`a calling Agent in an authorized open turn` | `tool/call`、`goal/change for mutations`、`tool/result` | - | create、edit、pause 和 resume 要求直接来自人类的根权限；complete 和 blocked 也接受确切的当前 Goal Round。blocked 的默认下限是 3 个获准的 Round。 |
| `@deepseek-ai/dsh-tool-schedule` | `schedule_create`、`schedule_delete`、`schedule_list`、`schedule_update` | `ctx.tools`、`ctx.schedule` | `tool/call`、Schedule storage domain 创建、更新或删除、`tool/result` | - | preset 或 Agent scope 挂载本包；由 preset 决定哪些 agent 获得这四个管理工具。每次调用都作用于调用方 Agent 的 Session。接受 after_seconds、显式绝对 at、有界固定速率 every_seconds、带显式 IANA 时区的每日与每周本地时间，以及作为五字段表达式的 cron。管理使用宿主 storage domain；到期消息会恢复原 Session。 |
| `@deepseek-ai/dsh-tool-lsp` | `lsp` | `ctx.workingDirectory`、`ctx.tools`、`ctx.lsp`、`ctx.systemPrompt` | `tool/call`、`tool/result` | - | lsp 工具将提供方选择和语言服务器子进程置于 ctx.lsp 之后，因此其模型可见 schema 在更换提供方时保持稳定。运行时要求已注册提供方，例如 `@deepseek-ai/dsh-lsp-stdio`；如果没有提供方，查询会返回结构化 `LSP_UNAVAILABLE` 错误，而不会改变 schema。 |
| `@deepseek-ai/dsh-tool-ralph` | `ralph` | `ctx.tools`、`ctx.workflowEngine`、`ctx.subagents`、`ctx.systemPrompt`、`a calling Agent (exec.agent parents every fresh round)` | `tool/call`、`tool/result`、`workflow and child session events during execution` | - | 固定的前台工作流会在每个 Round 启动一个全新的结构化子级；模型只能选择不可变目标和可选的 Round 上限。 |
| `@deepseek-ai/dsh-tool-skill` | `skill` | `ctx.workingDirectory`、`ctx.tools`、`ctx.agents`、`ctx.skills` | `tool/call`、`tool/result`、`user/message replacement catalogs via agent.inject()` | - | - |
| `@deepseek-ai/dsh-tool-session-query` | `session_event_read`、`session_event_search`、`session_event_trace`、`session_search`、`session_trace` | `ctx.tools`、`ctx.systemPrompt`、`ctx.sessionQuery`、`a calling Agent for workspace authority` | `tool/call`、`tool/result` | - | 这 5 个只读工具会隐藏提供方游标，并根据不可变的调用 agent 会话为每个结果授权。该包需要选择启用；需要强制截止时间或限制行内输出的组合还会挂载通用超时或 spill 策略。 |
| `@deepseek-ai/dsh-tool-subagent` | `list_subagent_models`、`subagent` | `ctx.tools`、`ctx.subagents`、`ctx.systemPrompt`、`用于模型发现和所选路由校验的 ctx.llm` | `tool/call`、`tool/result`、`child session events through the chosen provider` | `subagent`、`subagent_fork` | 注册的委派工具名称取决于加载时 `toolName` 配置（默认为 `subagent`）；上述默认 schema 关闭模型选择，而发现 schema 则展示为已启用 Session 中可用的固定配套工具。Web preset 会在每个新顶层 Session 创建时读取插件页偏好，并为其子 Session 保留该决定；`subagent_fork` 始终使用固定路由。每个实例通过 `provider` 选择后端，通过 `modelSelectionSettings` 读取模型选择设置。每次委派都会立即返回子代理 id；本地子代理接受后续消息，外部后端自动汇报单次执行结果。 |
| `@deepseek-ai/dsh-tool-subagent-control` | `interrupt_agent`、`list_agents`、`send_message` | `ctx.tools`、`ctx.subagents`、`ctx.agents and ctx.sessionProjections (list_agents only)` | `tool/call`、`tool/result`、`child session events through ctx.subagents` | - | 这些是用于发现、中断托管 subagent 和向本地子代理发送消息的全局命名工具：绑定提供方的 `tool-subagent` 实例注册不同的委派工具；本包注册一次 `send_message` 和 `interrupt_agent`，另由 `list_agents` 通过单独加载的 `/list-agents` 插件提供，其目录行使用 sessionProjections 和实时 Agent 注册表。 |
| `@deepseek-ai/dsh-tool-jobs` | `job_kill`、`job_list`、`job_output` | `ctx.tools`、`ctx.jobs`、`ctx.systemPrompt` | `tool/call`、`tool/result`、`user/message via agent.inject() for background completion notices` | - | 与任务种类无关的后台任务控制器：后台 bash 命令与 PTY 发送 都通过相同的 3 个工具读取、列出和终止。加载该插件会挂接控制器，从而启用生产方的 `ctx.jobs.start()`。 |
| `@deepseek-ai/dsh-experimental-tool-agent-team` | `interrupt_agent`、`list_agents`、`send_message`、`spawn_teammate`、`team_task_create`、`team_task_get`、`team_task_list`、`team_task_update`、`wait_agent` | `ctx.tools`、`ctx.systemPrompt`、`ctx.agentTeams`、`an exact live Team member Agent` | `tool/call`、`team/member`、`team/task`、`agent/inbox/spliced`、`user/message on inbox claim`、`tool/result` | - | 这 9 个工具限定于隐式 Team Lead 与持久 teammate 作用域。随产品发布的 dsh-base bundle 默认禁用该包；文档中的 Agent Teams profile patch 会启用它，并禁用旧 continuable child 的同名控制工具。 |
| `@deepseek-ai/dsh-tool-todo` | `todo_write` | `ctx.tools`、`owning Agent session` | `tool/call`、`todo/write`、`tool/result` | - | todo_write 是会话所有的状态；UI 将最新的 todo/write 事件渲染为检查清单。`allowParallelInProgress` 是没有默认值的必填项，因此本目录明确选择 `true`，对应描述允许同时存在多个 `in_progress` 项。选择 `false` 的部署会获得同一工具，但描述会要求只能有 1 个活动任务。 |
| `@deepseek-ai/dsh-tool-working-directory` | `working_directory` | `ctx.tools`, `ctx.workingDirectory` | `tool/call`, `working-directory/change`, `user context for directory changes`, `tool/result` | - | 读取或改变调用方 Session 的目录。已有 shell 和进程保留自己的目录；原始元数据和权限根目录保持不变。 |
| `@deepseek-ai/dsh-experimental-tool-worktree` | `create_worktree` | `ctx.tools`, `ctx.worktrees`, `a calling Agent` | `tool/call`, `Git branch and checkout`, `working-directory/change`, `tool/result` | - | 仅通过显式实验组合加载。在现有写权限下从固定的本地提交创建新分支与检出目录，再改变 Session 工作目录。离开时保留分支与检出目录。 |
| `@deepseek-ai/dsh-tool-workflow` | `workflow` | `ctx.tools`、`ctx.workflowEngine`、`ctx.systemPrompt`、`a calling Agent (exec.agent parents the script children)` | `tool/call`、`tool/result` | - | - |
| `@deepseek-ai/dsh-tool-workspace-dependencies` | `load_workspace_dependencies` | `ctx.tools` | `tool/call`, `tool/result` | - | - |
| `@deepseek-ai/dsh-tool-web` | `web_fetch`、`web_search` | `ctx.tools`、`ctx.web`、`ctx.systemPrompt` | `tool/call`、`tool/result` | - | web_search 和 web_fetch 将提供方选择置于 ctx.web 之后，使模型可见 schema 在更换后端时保持稳定。 |
| `@deepseek-ai/dsh-tool-wordinweb` | `word_document_asset`、`word_document_capabilities`、`word_document_compose`、`word_document_edit`、`word_document_inspect`、`word_document_new`、`word_document_open`、`word_document_patch`、`word_document_project`、`word_document_save` | `ctx.tools` | `tool/call`、`tool/result` | - | - |

<a id="deepseek-aidsh-plugin-manager"></a>

## `@deepseek-ai/dsh-plugin-manager`

### `plugin_manager`

列出当前 profile 中的插件或组合包，启用或禁用它们，安装组合包或移除已安装的组合包。每项操作都要求 danger-full-access 权限或本次调用的批准。批准不改变会话权限模式。变更影响该 profile 的所有会话。先列出条目以获取准确标识。包安装可能运行已获批准的构建脚本。支持热更新的 profile 立即应用变更；仅启动时加载的 profile 需要重启。不兼容的 DSH peer 依赖会阻止安装和激活。版本豁免可能导致崩溃和数据丢失：授权前必须警告用户，并获得用户对精确插件版本与运行时版本组合的明确许可。

```json
{
  "type": "object",
  "properties": {
    "action": {
      "type": "string",
      "description": "Management operation.",
      "enum": [
        "list_plugins",
        "list_bundles",
        "set_plugin",
        "set_bundle",
        "install_bundle",
        "remove_bundle",
        "list_version_exemptions",
        "set_version_exemption"
      ]
    },
    "target": {
      "type": "string",
      "description": "Plugin entry id, bundle package name, or installation spec, according to action. For an Official catalog entry, pass list_bundles installTarget.spec to install_bundle."
    },
    "enabled": {
      "type": "boolean",
      "description": "Required for set operations; defaults to true for installation. For set_version_exemption, true grants and false revokes."
    },
    "runtimeVersion": {
      "type": "string",
      "description": "For set_version_exemption: exact DSH version from list_version_exemptions. Target must be the manifest package-name@version, not an alias or version range."
    },
    "acceptRisk": {
      "type": "boolean",
      "description": "For granting an exemption: true only after warning the user about possible crashes and data loss and receiving explicit permission for this exact plugin/runtime pair. General installation permission is not enough."
    },
    "approvedBuilds": {
      "type": "array",
      "description": "For install_bundle: pass names from pendingBuilds only after the user explicitly approves running their install scripts in the conversation. This grants persistent permission for this profile.",
      "items": {
        "type": "string"
      }
    },
    "registry": {
      "type": "string",
      "description": "For install_bundle: the npm registry URL asked first, when the user names one; otherwise the configured registry is asked, and its configured fallbacks while a registry is unreachable."
    },
    "offset": {
      "type": "number",
      "description": "Zero-based list offset; defaults to 0."
    },
    "limit": {
      "type": "number",
      "description": "List page size, from 1 to 100; defaults to 25."
    }
  },
  "required": [
    "action"
  ]
}
```

来源： [`packages/boot/plugin-manager/src/tools.ts`](../packages/boot/plugin-manager/src/tools.ts)

<a id="deepseek-aidsh-mcp-resources"></a>

## `@deepseek-ai/dsh-mcp-resources`

### `list_mcp_resource_templates`

列出 MCP 服务器提供的参数化资源 URI 模板。

```json
{
  "type": "object",
  "properties": {
    "server": {
      "type": "string",
      "description": "Configured MCP server name."
    },
    "cursor": {
      "type": "string",
      "description": "Continuation cursor returned by this server."
    }
  },
  "required": [
    "server"
  ]
}
```

来源： [`packages/mcp/mcp-resources/src/tools.ts`](../packages/mcp/mcp-resources/src/tools.ts)

### `list_mcp_resources`

列出 MCP 服务器提供的资源。

```json
{
  "type": "object",
  "properties": {
    "server": {
      "type": "string",
      "description": "Configured MCP server name."
    },
    "cursor": {
      "type": "string",
      "description": "Continuation cursor returned by this server."
    }
  },
  "required": [
    "server"
  ]
}
```

来源： [`packages/mcp/mcp-resources/src/tools.ts`](../packages/mcp/mcp-resources/src/tools.ts)

### `read_mcp_resource`

按 URI 从指定服务器读取 MCP 资源。使用已列出的 URI 或展开后的资源模板。

```json
{
  "type": "object",
  "properties": {
    "server": {
      "type": "string",
      "description": "Configured MCP server name."
    },
    "uri": {
      "type": "string",
      "description": "Resource URI to read."
    }
  },
  "required": [
    "server",
    "uri"
  ]
}
```

来源： [`packages/mcp/mcp-resources/src/tools.ts`](../packages/mcp/mcp-resources/src/tools.ts)

<a id="deepseek-aidsh-experimental-browser-use-stagehand-native"></a>

## `@deepseek-ai/dsh-experimental-browser-use-stagehand-native`

### `stagehand_act`

使用配置的 Stagehand 模型执行一次自然语言浏览器操作。

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "properties": {
    "pageId": {
      "type": "string",
      "minLength": 1
    },
    "instruction": {
      "type": "string",
      "minLength": 1
    }
  },
  "required": [
    "instruction"
  ],
  "additionalProperties": false
}
```

来源：[`packages/experimental/browser-use-stagehand-native/src/index.ts`](../packages/experimental/browser-use-stagehand-native/src/index.ts)

### `stagehand_extract`

使用配置的 Stagehand 模型与可选的 JSON Schema 提取页面数据。

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "properties": {
    "pageId": {
      "type": "string",
      "minLength": 1
    },
    "instruction": {
      "type": "string",
      "minLength": 1
    },
    "schema": {
      "type": "object",
      "propertyNames": {
        "type": "string"
      },
      "additionalProperties": {
        "$ref": "#/$defs/__schema0"
      }
    }
  },
  "required": [
    "instruction"
  ],
  "additionalProperties": false,
  "$defs": {
    "__schema0": {
      "anyOf": [
        {
          "type": "string"
        },
        {
          "type": "number"
        },
        {
          "type": "boolean"
        },
        {
          "type": "null"
        },
        {
          "type": "array",
          "items": {
            "$ref": "#/$defs/__schema0"
          }
        },
        {
          "type": "object",
          "propertyNames": {
            "type": "string"
          },
          "additionalProperties": {
            "$ref": "#/$defs/__schema0"
          }
        }
      ]
    }
  }
}
```

来源：[`packages/experimental/browser-use-stagehand-native/src/index.ts`](../packages/experimental/browser-use-stagehand-native/src/index.ts)

### `stagehand_navigate`

将 Stagehand 浏览器标签页导航至指定 URL。

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "properties": {
    "pageId": {
      "type": "string",
      "minLength": 1
    },
    "url": {
      "type": "string",
      "format": "uri"
    }
  },
  "required": [
    "url"
  ],
  "additionalProperties": false
}
```

来源：[`packages/experimental/browser-use-stagehand-native/src/index.ts`](../packages/experimental/browser-use-stagehand-native/src/index.ts)

### `stagehand_observe`

使用配置的 Stagehand 模型查找符合指令的浏览器操作。

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "properties": {
    "pageId": {
      "type": "string",
      "minLength": 1
    },
    "instruction": {
      "type": "string",
      "minLength": 1
    }
  },
  "required": [
    "instruction"
  ],
  "additionalProperties": false
}
```

来源：[`packages/experimental/browser-use-stagehand-native/src/index.ts`](../packages/experimental/browser-use-stagehand-native/src/index.ts)

### `stagehand_screenshot`

截取 Stagehand 标签页图像以供视觉检查。

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "properties": {
    "pageId": {
      "type": "string",
      "minLength": 1
    },
    "fullPage": {
      "default": false,
      "type": "boolean"
    }
  },
  "required": [
    "fullPage"
  ],
  "additionalProperties": false
}
```

来源：[`packages/experimental/browser-use-stagehand-native/src/index.ts`](../packages/experimental/browser-use-stagehand-native/src/index.ts)

### `stagehand_tabs`

列出、创建、选择或关闭 Stagehand 浏览器标签页。

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "oneOf": [
    {
      "type": "object",
      "properties": {
        "action": {
          "type": "string",
          "const": "list"
        }
      },
      "required": [
        "action"
      ],
      "additionalProperties": false
    },
    {
      "type": "object",
      "properties": {
        "action": {
          "type": "string",
          "const": "new"
        },
        "url": {
          "type": "string",
          "format": "uri"
        }
      },
      "required": [
        "action"
      ],
      "additionalProperties": false
    },
    {
      "type": "object",
      "properties": {
        "action": {
          "type": "string",
          "enum": [
            "select",
            "close"
          ]
        },
        "pageId": {
          "type": "string",
          "minLength": 1
        }
      },
      "required": [
        "action",
        "pageId"
      ],
      "additionalProperties": false
    }
  ],
  "type": "object"
}
```

来源：[`packages/experimental/browser-use-stagehand-native/src/index.ts`](../packages/experimental/browser-use-stagehand-native/src/index.ts)

<a id="deepseek-aidsh-tool-ask-user"></a>

## `@deepseek-ai/dsh-tool-ask-user`

### `ask_user_question`

继续操作前，如果需要确认、选择或缺失的信息，请向用户提出简明问题。

```json
{
  "type": "object",
  "properties": {
    "questions": {
      "type": "array",
      "description": "Questions to ask the user before continuing.",
      "items": {
        "type": "object",
        "additionalProperties": true,
        "properties": {
          "id": {
            "type": "string",
            "description": "Stable id for this question; echoed in the answer."
          },
          "question": {
            "type": "string",
            "description": "The specific question to ask the user."
          },
          "header": {
            "type": "string",
            "description": "Optional short heading for the question, such as \"Confirm\" or \"Choose Mode\"."
          },
          "options": {
            "type": "array",
            "description": "Optional choices to show the user. If you recommend one, put it first and append \"(Recommended)\" to that label.",
            "items": {
              "type": "object",
              "additionalProperties": true,
              "properties": {
                "label": {
                  "type": "string",
                  "description": "Short user-facing option label."
                },
                "description": {
                  "type": "string",
                  "description": "One sentence explaining the tradeoff or impact."
                }
              },
              "required": [
                "label"
              ]
            }
          },
          "multi_select": {
            "type": "boolean",
            "description": "Whether the user may select more than one option. Defaults to false."
          }
        },
        "required": [
          "id",
          "question"
        ]
      }
    }
  },
  "required": [
    "questions"
  ]
}
```

来源：[`packages/interaction/tool-ask-user/src/index.ts`](../packages/interaction/tool-ask-user/src/index.ts)

ask_user_question 默认保持原有阻塞行为；设置 `mode: timed` 后才启用前台超时与 pending 结果，同时问题仍可回答；timed 模式内 `timeout: -1` 让本次调用无限期阻塞。

<a id="deepseek-aidsh-tools"></a>

## `@deepseek-ai/dsh-tools`

### `run_code`

针对可用工具执行 TypeScript 程序。接受两个必填参数：`description`，简要说明该程序做什么；以及 `code`，即异步函数的**函数体**（仅使用可擦除语法；支持顶层 `await` 和 `return`）。请根据系统提示词中的声明，以 `await tools.name(args)` 形式调用工具。只有打印或返回的内容属于程序输出，请谨慎筛选。含图片的子工具结果会在运行结束后附加。

```json
{
  "type": "object",
  "properties": {
    "description": {
      "type": "string",
      "description": "Clear, concise description of what this program does in active voice, 5-10 words (shown in the UI). Provide `description` before `code` in the arguments. Examples: \"Count TODO markers across packages\"; \"Read failing test and its fixture\"; \"Rename config key in every cordis.yml\"."
    },
    "code": {
      "type": "string",
      "description": "The program: the body of an async TypeScript function."
    },
    "timeoutMs": {
      "type": "number",
      "description": "Positive elapsed-time budget in milliseconds, capped by the deployment maximum."
    },
    "sandbox_permissions": {
      "type": "string",
      "description": "Wider sandbox mode for this complete program execution; requires justification and approval.",
      "enum": [
        "workspace-write",
        "danger-full-access"
      ]
    },
    "justification": {
      "type": "string",
      "description": "Reason this complete program needs wider access, shown to the user for approval. Use the language of the user’s current request."
    }
  },
  "required": [
    "description",
    "code"
  ]
}
```

来源：[`packages/core/tools/src/ptc.ts`](../packages/core/tools/src/ptc.ts)

在 `mode: ptc` 下，它由工具注册表所有，作为可过滤能力层之外的保留传输机制（参见 PTC mode Agent Note）。在 `ptc` 下，它是注册表对协议格式的唯一贡献；其他可见能力在使用已加载运行时语言生成的 SDK 章节中声明。程序通过 binding 调用这些能力，调用按照原生并发约定调度：启动顺序和策略遵循提交顺序，并发安全的函数体最多重叠执行 `maxParallelSubCalls` 个。调用会重新进入完整且受守卫保护的工具流水线，并将每个嵌套执行关联到此外层结果。

<a id="deepseek-aidsh-plan-mode"></a>

## `@deepseek-ai/dsh-plan-mode`

### `exit_plan_mode`

仅在规划模式下使用。提交计划供用户评审，并在获批后退出规划模式。用户可以批准（从你的下一步骤起执行计划），也可以要求继续规划；其反馈会通过工具结果返回，请修改后再次提交。

```json
{
  "type": "object",
  "properties": {
    "plan": {
      "type": "string",
      "description": "The complete plan, as markdown, starting with a # heading that names it."
    }
  },
  "required": [
    "plan"
  ]
}
```

来源：[`packages/plan/plan-mode/src/index.ts`](../packages/plan/plan-mode/src/index.ts)

规划未激活时，exit_plan_mode 仍保留在面向模型的 schema 中，这样状态转换不会在规划策略变更之外额外造成工具目录变动。其执行路径会拒绝规划模式之外的调用；在规划模式下，它通过用户交互 seam 提交计划（批准／根据反馈继续规划），批准后会在步骤边界记录规划模式已停用。

<a id="deepseek-aidsh-tool-bash"></a>

## `@deepseek-ai/dsh-tool-bash`

### `bash`

执行 bash 命令（`bash -c`）并返回 stdout/stderr。每次调用都在新 shell 中运行；请传入 `workdir`，不要使用 `cd`。托管的 `$DSH_*` 变量公开当前 harness 环境信息。较长的输出会截断，只保留尾部；如可用，完整输出会保存到文件并报告其路径。请在参数中先提供 `description`，再提供 `command`。在任何删除或移动之前，请确认解析后的绝对目标路径正是预期路径；绝不要对未经检查的计算路径执行此类操作。未设置的变量会展开为空字符串，因此请用 `${VAR:?}` 保护此类路径中的变量。命令可能在文件沙箱中运行；被阻止的文件操作报告为 `[sandbox: file access denied under <mode> mode]`，这是策略拒绝：请勿换一种方式重试。

```json
{
  "type": "object",
  "properties": {
    "description": {
      "type": "string",
      "description": "Clear, concise description of what this command does in active voice, 5-10 words (shown in the UI). Examples: \"ls\" → \"List files in current directory\"; \"git status\" → \"Show working tree status\"; \"npm install\" → \"Install package dependencies\"."
    },
    "command": {
      "type": "string",
      "description": "The bash command to execute."
    },
    "timeoutMs": {
      "type": "number",
      "description": "Timeout in milliseconds. The executor applies its configured default and cap; on expiry the command moves to the background as a job instead of being killed."
    },
    "workdir": {
      "type": "string",
      "description": "Working directory for this command. Defaults to the session workspace; a relative path is resolved against it."
    },
    "run_in_background": {
      "type": "boolean",
      "description": "Run in the background and return a job id immediately (collect with job_output, stop with job_kill). No timeout applies."
    }
  },
  "required": [
    "description",
    "command"
  ]
}
```

来源：[`packages/shell/tool-bash/src/index.ts`](../packages/shell/tool-bash/src/index.ts)

bash 工具是 bash 执行器 seam 面向模型的消费方。组合中有 job 注册表时，每次调用一启动就注册到通用 `ctx.jobs` 运行时，并通过 `job_*` 工具（来自 `@deepseek-ai/dsh-tool-jobs`）收集／停止；没有注册表或 `enableRunInBackground: false` 时，工具注册不带 `run_in_background` 参数的纯前台 schema。

<a id="deepseek-aidsh-tool-present"></a>

## `@deepseek-ai/dsh-tool-present`

### `present`

将已有文件声明为交付给用户的最终交付物。当用户需要独立文件时使用，尤其是 Office 文档、电子表格和演示文稿；如果最终回复已经足够，优先使用最终回复。用户打开的是当前文件；不复制其内容。

```json
{
  "type": "object",
  "properties": {
    "files": {
      "type": "array",
      "description": "Usually the 1-2 most important deliverables; at most 4 per call.",
      "items": {
        "type": "object",
        "additionalProperties": false,
        "properties": {
          "path": {
            "type": "string",
            "description": "Path of an existing regular file. Relative paths use the Session working directory."
          },
          "description": {
            "type": "string",
            "description": "Brief description for the user."
          }
        },
        "required": [
          "path"
        ]
      }
    }
  },
  "required": [
    "files"
  ]
}
```

来源： [`packages/deliverables/tool-present/src/index.ts`](../packages/deliverables/tool-present/src/index.ts)

交付归调用方 Session 所有；Web ui-deliverables 提供源文件打开与卡片。

<a id="deepseek-aidsh-tool-pwsh"></a>

## `@deepseek-ai/dsh-tool-pwsh`

### `pwsh`

执行 PowerShell 命令（`pwsh -Command`）并返回 stdout/stderr。每次调用都在新的 pwsh 进程中运行；请传入 `workdir`，不要使用 `cd`。路径采用 Windows 原生形式（`C:\...`）；使用 `$env:NAME` 读取环境变量。托管的 `$env:DSH_*` 变量公开当前 harness 环境信息。较长的输出会截断，只保留尾部；如可用，完整输出会保存到文件并报告其路径。在 Windows 上，被强制终止的命令会以 `[exit code: 1]` 结算且不带信号标记，请将其视为中断，而不是命令失败。请在参数中先提供 `description`，再提供 `command`。在任何删除或移动之前，请确认解析后的绝对目标路径正是预期路径；绝不要对未经检查的计算路径执行此类操作。不要给 `$HOME` 等自动变量赋值；变量名不区分大小写，因此 `$home` 就是同一个只读变量。命令可能在文件沙箱中运行；被阻止的文件操作报告为 `[sandbox: file access denied under <mode> mode]`，这是策略拒绝：请勿换一种方式重试。

```json
{
  "type": "object",
  "properties": {
    "description": {
      "type": "string",
      "description": "Clear, concise description of what this command does in active voice, 5-10 words (shown in the UI). Examples: \"ls\" → \"List files in current directory\"; \"git status\" → \"Show working tree status\"; \"Get-Process\" → \"List running processes\"."
    },
    "command": {
      "type": "string",
      "description": "The PowerShell command to execute."
    },
    "timeoutMs": {
      "type": "number",
      "description": "Timeout in milliseconds. The executor applies its configured default and cap; on expiry the command moves to the background as a job instead of being killed."
    },
    "workdir": {
      "type": "string",
      "description": "Working directory for this command. Defaults to the session workspace; a relative path is resolved against it."
    },
    "run_in_background": {
      "type": "boolean",
      "description": "Run in the background and return a job id immediately (collect with job_output, stop with job_kill). No timeout applies."
    }
  },
  "required": [
    "description",
    "command"
  ]
}
```

来源：[`packages/shell/tool-pwsh/src/index.ts`](../packages/shell/tool-pwsh/src/index.ts)

pwsh 工具是 Windows 组合中 bash 执行器 seam 的 PowerShell 方言消费方（由 `@deepseek-ai/dsh-pwsh-local` 等 PowerShell 执行器为 `ctx.shell` 提供后端）；除沙箱接口外，它逐项对应 bash 工具调用。使用 `run_in_background` 的运行会注册到通用 `ctx.jobs` 运行时，并通过 `job_*` 工具收集／停止；托管的 `DSH_*` 环境来自 `@deepseek-ai/dsh-shell-env`。每次调用都在新进程中运行，不使用持久 PTY 会话。路径采用原生 `C:\...` 形式，变量采用 `$env:NAME`。

<a id="deepseek-aidsh-tool-cordis"></a>

## `@deepseek-ai/dsh-tool-cordis`

### `cordis_inspect_list`

列出 Host 当前已知的所有 Cordis Inspect Provider，包括本地 Host Provider 和 Client 同步的最新清单。每项包含平台、用途、只读方法以及输入输出 schema。编写或配置插件前先调用本工具，再从结果选择 cordis_inspect_query 的 provider 和方法。不要猜测名称，也不要把 Inspect 方法当作插件代码可调用的业务 Service。

```json
{
  "type": "object",
  "properties": {}
}
```

来源： [`packages/extensions/tool-cordis/src/index.ts`](../packages/extensions/tool-cordis/src/index.ts)

### `cordis_inspect_query`

执行 Inspect Provider 声明的只读查询。platform、provider 和 method 必须来自 cordis_inspect_list，input 必须符合该方法的 schema。编写插件代码前，用本工具读取准确的 Service 方法、Event 模式、插件 Config schema、Tool schema、主题 token，或实时 Slot 树与 props。Host 查询在本地运行。Client 查询在配置的超时内等待页面首个有效响应；否则返回 Client 错误，或提示重新连接后重试。本工具不能调用业务 Service 方法或修改运行时。

```json
{
  "type": "object",
  "properties": {
    "platform": {
      "type": "string",
      "description": "Runtime platform that owns the Provider.",
      "enum": [
        "host",
        "client"
      ]
    },
    "provider": {
      "type": "string",
      "description": "Exact Provider ID returned by cordis_inspect_list."
    },
    "method": {
      "type": "string",
      "description": "Exact method name declared by the Provider manifest."
    },
    "input": {
      "description": "Optional query input; it must satisfy the method input schema."
    }
  },
  "required": [
    "platform",
    "provider",
    "method"
  ]
}
```

来源： [`packages/extensions/tool-cordis/src/index.ts`](../packages/extensions/tool-cordis/src/index.ts)

创造模式提供两个只读运行时检查工具。Cordis host runner 提供检查注册表；Client 查询需要已连接页面。持久化变更编写为组合包，再通过 plugin_manager 安装。

<a id="deepseek-aidsh-tool-bash-persistent"></a>

## `@deepseek-ai/dsh-tool-bash-persistent`

### `bash`

在持久 bash shell 中运行命令。包括当前目录和已导出环境变量在内的状态会在此 agent 的多次调用之间保留。

```json
{
  "type": "object",
  "properties": {
    "command": {
      "type": "string",
      "description": "The bash command to run. Relative path is preferred in the command."
    }
  },
  "required": [
    "command"
  ]
}
```

来源：[`packages/shell/tool-bash-persistent/src/index.ts`](../packages/shell/tool-bash-persistent/src/index.ts)

一个按所有者隔离的持久 bash 工具；部署组合提供 PTY 后端，并可覆盖面向模型的环境描述。

<a id="deepseek-aidsh-tool-pwsh-persistent"></a>

## `@deepseek-ai/dsh-tool-pwsh-persistent`

### `pwsh`

在持久 PowerShell shell 中运行命令。包括当前目录和已导出环境变量在内的状态会在此 agent 的多次调用之间保留。

```json
{
  "type": "object",
  "properties": {
    "command": {
      "type": "string",
      "description": "The PowerShell command to run. Relative path is preferred in the command."
    }
  },
  "required": [
    "command"
  ]
}
```

来源：[`packages/shell/tool-pwsh-persistent/src/index.ts`](../packages/shell/tool-pwsh-persistent/src/index.ts)

一个按所有者隔离的持久 pwsh 工具，持久 bash 工具的 Windows 对应物；部署组合提供 pwsh 方言的 PTY 后端，并可覆盖面向模型的环境描述。

<a id="deepseek-aidsh-tool-str-replace-editor"></a>

## `@deepseek-ai/dsh-tool-str-replace-editor`

### `str_replace_editor`

用于查看、创建和编辑文件的自定义编辑工具：

* 状态会在命令调用以及与用户的讨论之间持久保留
* 如果 `path` 是文件，`view` 会显示应用 `cat -n` 后的结果。如果 `path` 是目录，`view` 会列出最多向下 2 层的非隐藏文件和目录
* 如果指定的 `create` 命令目标 `path` 已作为文件存在，则不能使用该命令
* 如果 `command` 产生较长输出，输出会被截断并标记为 `<response clipped>`
* 当前命令不使用某个参数时，值为 `null` 的占位参数视为未提供。必填参数仍须提供值；删除匹配内容时应省略 `str_replace.new_str`，而不是将其设为 `null`

使用 `str_replace` 命令时请注意：

* `old_str` 参数应与原文件中一行或多行连续内容**完全**匹配。请留意空白字符！
* 如果 `old_str` 参数在文件中不唯一，则不会执行替换。请确保在 `old_str` 中包含足够的上下文，使其唯一
* `new_str` 参数应包含用于替换 `old_str` 的已编辑行

```json
{
  "type": "object",
  "properties": {
    "command": {
      "type": "string",
      "description": "The commands to run. Allowed options are: `view`, `create`, `str_replace`, `insert`.",
      "enum": [
        "view",
        "create",
        "str_replace",
        "insert"
      ]
    },
    "path": {
      "type": "string",
      "description": "Absolute path to file or directory, e.g. `/repo/file.py` or `/repo`."
    },
    "file_text": {
      "oneOf": [
        {
          "type": "string"
        },
        {
          "type": "null"
        }
      ],
      "description": "Required string parameter of `create` command, with the content of the file to be created. A null placeholder is treated as omitted by commands that do not use this parameter."
    },
    "insert_line": {
      "oneOf": [
        {
          "type": "integer"
        },
        {
          "type": "null"
        }
      ],
      "description": "Required integer parameter of `insert` command. The `new_str` will be inserted AFTER the line `insert_line` of `path`. A null placeholder is treated as omitted by commands that do not use this parameter."
    },
    "new_str": {
      "oneOf": [
        {
          "type": "string"
        },
        {
          "type": "null"
        }
      ],
      "description": "Optional string parameter of `str_replace` command containing the new string (if omitted, no string will be added). Required string parameter of `insert` command containing the string to insert. A null placeholder is accepted only by commands that do not use this parameter."
    },
    "old_str": {
      "oneOf": [
        {
          "type": "string"
        },
        {
          "type": "null"
        }
      ],
      "description": "Required string parameter of `str_replace` command containing the string in `path` to replace. A null placeholder is treated as omitted by commands that do not use this parameter."
    },
    "view_range": {
      "oneOf": [
        {
          "type": "array",
          "items": {
            "type": "integer"
          }
        },
        {
          "type": "null"
        }
      ],
      "description": "Optional parameter of `view` command when `path` points to a file. If omitted or null, the full file is shown. If provided, the file will be shown in the indicated line number range, e.g. [11, 12] will show lines 11 and 12. Indexing at 1 to start. Setting `[start_line, -1]` shows all lines from `start_line` to the end of the file."
    }
  },
  "required": [
    "command",
    "path"
  ]
}
```

来源：[`packages/fs/tool-str-replace-editor/src/index.ts`](../packages/fs/tool-str-replace-editor/src/index.ts)

基于文件系统 seam 的独立查看／创建／唯一字面量替换／按行插入工具；可与任何 shell 或终端接口组合。

<a id="deepseek-aidsh-tool-fs"></a>

## `@deepseek-ai/dsh-tool-fs`

### `edit`

通过替换字面量文本来编辑现有 UTF-8 文本文件。

```json
{
  "type": "object",
  "properties": {
    "file_path": {
      "type": "string",
      "description": "Path to edit, resolved by the filesystem backend. Provide `file_path` before `old_string` and `new_string` in the arguments."
    },
    "old_string": {
      "type": "string",
      "description": "Literal text to replace."
    },
    "new_string": {
      "type": "string",
      "description": "Literal replacement text. Use an empty string to delete the match."
    },
    "replace_all": {
      "type": "boolean",
      "description": "Replace all matches. Defaults to false; when false, old_string must appear exactly once."
    }
  },
  "required": [
    "file_path",
    "old_string",
    "new_string"
  ]
}
```

来源：[`packages/fs/tool-fs/src/index.ts`](../packages/fs/tool-fs/src/index.ts)

### `read`

读取 UTF-8 文本文件，并返回带行号的内容。

```json
{
  "type": "object",
  "properties": {
    "file_path": {
      "type": "string",
      "description": "Path to read, resolved by the filesystem backend."
    },
    "offset": {
      "type": "number",
      "description": "1-based first line to return. Defaults to 1."
    },
    "limit": {
      "type": "number",
      "description": "Maximum number of lines to return. Defaults to 2000."
    }
  },
  "required": [
    "file_path"
  ]
}
```

来源：[`packages/fs/tool-fs/src/index.ts`](../packages/fs/tool-fs/src/index.ts)

### `read_image`

读取 PNG/JPEG/WebP/GIF 文件并返回图像本身。大图会自动缩小；不要为了查看图片而安装图片库或创建缩略图。

```json
{
  "type": "object",
  "properties": {
    "file_path": {
      "type": "string",
      "description": "Path to the image file, resolved by the filesystem backend."
    }
  },
  "required": [
    "file_path"
  ]
}
```

来源：[`packages/fs/tool-fs/src/index.ts`](../packages/fs/tool-fs/src/index.ts)

### `write`

创建或完全替换 UTF-8 文本文件。

```json
{
  "type": "object",
  "properties": {
    "file_path": {
      "type": "string",
      "description": "Path to write, resolved by the filesystem backend. Provide `file_path` before `content` in the arguments."
    },
    "content": {
      "type": "string",
      "description": "Full UTF-8 text content to write."
    }
  },
  "required": [
    "file_path",
    "content"
  ]
}
```

来源：[`packages/fs/tool-fs/src/index.ts`](../packages/fs/tool-fs/src/index.ts)

先读后写／编辑策略由 `@deepseek-ai/dsh-fs-observation-policy` 添加；它是一个 `fs/*` 事件门禁插件，不会改变 schema。加载这些工具的部署按预期也应加载该插件。没有 `ctx.attachments` 时图片工具不会注册；其 schema 与路由无关，执行时除非确切路由的模型声明图片输入，否则拒绝。

<a id="deepseek-aidsh-tool-fs-search"></a>

## `@deepseek-ai/dsh-tool-fs-search`

### `glob`

查找路径匹配 glob 模式的文件（不含目录），包括隐藏文件和被忽略的文件。最多按修改时间顺序返回 100 条路径；更大的结果会从顶层条目中抽样，并报告完整列表的保存位置。

```json
{
  "type": "object",
  "properties": {
    "pattern": {
      "type": "string",
      "description": "Glob pattern to match file paths against (e.g. \"**/*.ts\", \"src/**/*.test.js\"). A pattern with no \"/\" matches the basename at any depth, so \"*\" and \"*.ts\" both search the whole tree; include a separator to anchor the depth."
    },
    "path": {
      "type": "string",
      "description": "Directory to search in. Defaults to the session workspace; a relative path resolves against it."
    }
  },
  "required": [
    "pattern"
  ]
}
```

来源：[`packages/fs/tool-fs-search/src/index.ts`](../packages/fs/tool-fs-search/src/index.ts)

### `grep`

使用 ripgrep 正则表达式搜索文件内容。返回带行号的匹配行，并按文件分组。最多返回 250 条匹配；更大的结果会报告完整匹配列表的保存位置。

```json
{
  "type": "object",
  "properties": {
    "pattern": {
      "type": "string",
      "description": "Regular expression to search for (ripgrep syntax)."
    },
    "path": {
      "type": "string",
      "description": "File or directory to search. Defaults to the session workspace; a relative path resolves against it."
    },
    "include": {
      "type": "string",
      "description": "One glob filter for which files to search (e.g. \"*.ts\", \"*.{js,jsx}\"). Not a list; negation is not supported."
    }
  },
  "required": [
    "pattern"
  ]
}
```

来源：[`packages/fs/tool-fs-search/src/index.ts`](../packages/fs/tool-fs-search/src/index.ts)

glob 和 grep 是无条件可用的发现工具，通过 ctx.subprocess spawn 随包提供的 ripgrep 二进制文件（`@vscode/ripgrep`），并作为普通前台调用运行，绝不作为后台任务；无需在宿主机安装 `rg`，也不经过 shell 层。本目录使用 `sampleOverCapGlobResults: true`；部署必须显式选择该行为。结果超过上限时，会通过可选的 ctx.spillStore 后端保存完整的格式化列表；在共置部署中，如果后端公开本地路径，返回的定位信息可供后续读取／搜索。

<a id="deepseek-aidsh-experimental-tool-terminal"></a>

## `@deepseek-ai/dsh-tool-terminal`

### `terminal_close`

关闭一个持久终端，并等待其捕获且所有的进程树完全退出。

```json
{
  "type": "object",
  "properties": {
    "sessionId": {
      "type": "string",
      "description": "Terminal session id."
    }
  },
  "required": [
    "sessionId"
  ]
}
```

来源：[`packages/experimental/tool-terminal/src/index.ts`](../packages/experimental/tool-terminal/src/index.ts)

### `terminal_list`

列出当前 agent 所有的持久终端会话。

```json
{
  "type": "object",
  "properties": {}
}
```

来源：[`packages/experimental/tool-terminal/src/index.ts`](../packages/experimental/tool-terminal/src/index.ts)

### `terminal_open`

通过已注册的后端类型创建按所有者隔离的持久终端会话。需要在多次工具调用之间保留 shell 或 REPL 状态时，请使用此工具。

```json
{
  "type": "object",
  "properties": {
    "type": {
      "type": "string",
      "description": "Registered terminal backend type, usually \"shell\"."
    },
    "name": {
      "type": "string",
      "description": "Optional owner-local display name such as \"main\" or \"gdb\"."
    },
    "cwd": {
      "type": "string",
      "description": "Initial working directory, relative to the Session current directory when not absolute."
    }
  },
  "required": [
    "type"
  ]
}
```

来源：[`packages/experimental/tool-terminal/src/index.ts`](../packages/experimental/tool-terminal/src/index.ts)

### `terminal_read`

从持久终端读取一页有界的保留输出，不发送输入。

```json
{
  "type": "object",
  "properties": {
    "sessionId": {
      "type": "string",
      "description": "Terminal session id."
    },
    "offset": {
      "type": "number",
      "description": "Newest-relative line offset (default 0)."
    },
    "count": {
      "type": "number",
      "description": "Requested line count (default 500; backend caps apply)."
    }
  },
  "required": [
    "sessionId"
  ]
}
```

来源：[`packages/experimental/tool-terminal/src/index.ts`](../packages/experimental/tool-terminal/src/index.ts)

### `terminal_send`

向持久终端发送文本。默认会提交 Enter，并等待提示符、stdin 等待、输出静默、超时或会话退出。后台模式会返回供 job_output／job_kill 使用的 job id。

```json
{
  "type": "object",
  "properties": {
    "sessionId": {
      "type": "string",
      "description": "Terminal session id returned by terminal_open or terminal_list."
    },
    "text": {
      "type": "string",
      "description": "UTF-8 text to write to the terminal."
    },
    "submit": {
      "type": "boolean",
      "description": "Submit Enter after text (default true). Set false for control characters or incomplete REPL input."
    },
    "run_in_background": {
      "type": "boolean",
      "description": "Return a job id immediately; collect with job_output or stop with job_kill."
    }
  },
  "required": [
    "sessionId",
    "text"
  ]
}
```

来源：[`packages/experimental/tool-terminal/src/index.ts`](../packages/experimental/tool-terminal/src/index.ts)

### `terminal_signal`

向持久终端当前的前台进程组发送允许的信号。

```json
{
  "type": "object",
  "properties": {
    "sessionId": {
      "type": "string",
      "description": "Terminal session id."
    },
    "signal": {
      "type": "string",
      "description": "Signal to deliver. Shell-targeted SIGKILL is rejected; use terminal_close.",
      "enum": [
        "SIGINT",
        "SIGTERM",
        "SIGKILL",
        "SIGTSTP",
        "SIGHUP"
      ]
    }
  },
  "required": [
    "sessionId",
    "signal"
  ]
}
```

来源：[`packages/experimental/tool-terminal/src/index.ts`](../packages/experimental/tool-terminal/src/index.ts)

这 6 个终端工具需要选择启用，用于补充一次性 bash／文件系统工具。`terminal_send(run_in_background: true)` 会注册到 `ctx.jobs`；schema 不包含 TUI、具名按键序列、BEL、调整尺寸、自动启动和跨 agent 共享。

<a id="deepseek-aidsh-tool-goal"></a>

## `@deepseek-ai/dsh-tool-goal`

### `create_goal`

创建一个持久化目标，使当前会话跨自动延续 Round 持续工作。当直接人类请求是长期目标时使用，即使用户没有说「目标」；不要用于单轮工作。

```json
{
  "type": "object",
  "properties": {
    "objective": {
      "type": "string",
      "description": "The concrete completion objective inferred from the direct human request."
    },
    "max_goal_rounds": {
      "type": "number",
      "description": "Optional positive safe-integer limit on automatic continuation rounds."
    }
  },
  "required": [
    "objective"
  ]
}
```

来源：[`packages/goal/tool-goal/src/index.ts`](../packages/goal/tool-goal/src/index.ts)

### `get_goal`

读取当前会话目标，包括 update_goal 所需的 id 和 revision。

```json
{
  "type": "object",
  "properties": {}
}
```

来源：[`packages/goal/tool-goal/src/index.ts`](../packages/goal/tool-goal/src/index.ts)

### `update_goal`

更新当前目标。

```json
{
  "type": "object",
  "properties": {
    "goal_id": {
      "type": "string",
      "description": "Exact id returned by get_goal."
    },
    "revision": {
      "type": "number",
      "description": "Exact positive revision returned by get_goal."
    },
    "action": {
      "type": "string",
      "description": "edit, pause, and resume require a direct top-level human request. complete and blocked are also allowed during an automatic continuation of this goal; blocked is rejected before the configured minimum round count.",
      "enum": [
        "edit",
        "pause",
        "resume",
        "complete",
        "blocked"
      ]
    },
    "objective": {
      "type": "string",
      "description": "Replacement objective; valid only with action edit."
    },
    "max_goal_rounds": {
      "type": "number",
      "description": "Replacement cap; valid only with action edit."
    },
    "blocked_reason": {
      "type": "string",
      "description": "Required only with action blocked: the concrete condition that persisted across rounds and blocks progress."
    }
  },
  "required": [
    "goal_id",
    "revision",
    "action"
  ]
}
```

来源：[`packages/goal/tool-goal/src/index.ts`](../packages/goal/tool-goal/src/index.ts)

create、edit、pause 和 resume 要求直接来自人类的根权限；complete 和 blocked 也接受确切的当前 Goal Round。blocked 的默认下限是 3 个获准的 Round。

<a id="deepseek-aidsh-tool-schedule"></a>

## `@deepseek-ai/dsh-tool-schedule`

### `schedule_create`

在当前会话中创建一条提醒，到期时投递 prompt。请恰好提供一个时间参数：after_seconds、at、every_seconds、daily、weekly 或 cron。时区中不存在的本地时间会被跳过；重复出现的本地时间只在较早的时刻触发一次。停机后，重复提醒只投递最近错过的一次。崩溃后可能重复投递。

```json
{
  "type": "object",
  "properties": {
    "prompt": {
      "type": "string",
      "description": "Reminder content to present when the target becomes due."
    },
    "title": {
      "type": "string",
      "description": "Task name of at most 120 characters, shown on the task card and in task lists."
    },
    "after_seconds": {
      "type": "number",
      "description": "Delay in whole seconds."
    },
    "every_seconds": {
      "type": "number",
      "description": "Fixed-rate interval in whole seconds, at least 60, aligned to the creation time; changing it with schedule_update re-aligns it to the save time."
    },
    "daily": {
      "type": "object",
      "description": "Every day at a local time.",
      "additionalProperties": false,
      "properties": {
        "time": {
          "type": "string",
          "description": "HH:mm:ss with optional 1-3 fractional digits, for example 23:00:00."
        },
        "time_zone": {
          "type": "string",
          "description": "UTC or IANA Area/Location, for example Asia/Shanghai."
        }
      },
      "required": [
        "time",
        "time_zone"
      ]
    },
    "weekly": {
      "type": "object",
      "description": "On the given weekdays at a local time.",
      "additionalProperties": false,
      "properties": {
        "time": {
          "type": "string",
          "description": "HH:mm:ss with optional 1-3 fractional digits, for example 09:00:00."
        },
        "time_zone": {
          "type": "string",
          "description": "UTC or IANA Area/Location, for example Asia/Shanghai."
        },
        "weekdays": {
          "type": "array",
          "description": "ISO weekdays, Monday 1 through Sunday 7, without repetitions.",
          "items": {
            "type": "integer"
          }
        }
      },
      "required": [
        "time",
        "time_zone",
        "weekdays"
      ]
    },
    "cron": {
      "type": "object",
      "description": "Five-field Vixie cron expression in a time zone.",
      "additionalProperties": false,
      "properties": {
        "expression": {
          "type": "string",
          "description": "minute hour day-of-month month day-of-week, for example \"*/15 9-17 * * 1-5\". When both day fields are restricted, a date matches if either one matches."
        },
        "time_zone": {
          "type": "string",
          "description": "UTC or IANA Area/Location, for example Asia/Shanghai."
        }
      },
      "required": [
        "expression",
        "time_zone"
      ]
    },
    "at": {
      "oneOf": [
        {
          "type": "string"
        },
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "date": {
              "type": "string"
            },
            "time": {
              "type": "string"
            },
            "time_zone": {
              "type": "string"
            }
          },
          "required": [
            "date",
            "time",
            "time_zone"
          ]
        }
      ],
      "description": "Absolute target: an RFC 3339 date-time with offset, or a local date, time, and IANA time_zone."
    }
  },
  "required": [
    "prompt",
    "title"
  ]
}
```

来源：[`packages/schedule/tool-schedule/src/index.ts`](../packages/schedule/tool-schedule/src/index.ts)

### `schedule_delete`

删除当前会话中的一条提醒，活动或已结束的均可。删除不会撤回已经入队的提醒消息。

```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "description": "Exact schedule id."
    }
  },
  "required": [
    "id"
  ]
}
```

来源：[`packages/schedule/tool-schedule/src/index.ts`](../packages/schedule/tool-schedule/src/index.ts)

### `schedule_list`

列出当前会话中的活动提醒。

```json
{
  "type": "object",
  "properties": {}
}
```

来源：[`packages/schedule/tool-schedule/src/index.ts`](../packages/schedule/tool-schedule/src/index.ts)

### `schedule_update`

原地修改一条提醒并保留其 id。提供新的 title、prompt，或至多一个时间参数；未提供的字段保持原值。需要相对延迟时请新建一条提醒。

```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "description": "Schedule id returned by schedule_list."
    },
    "title": {
      "type": "string",
      "description": "New task name of at most 120 characters."
    },
    "prompt": {
      "type": "string",
      "description": "New reminder content."
    },
    "every_seconds": {
      "type": "number",
      "description": "Fixed-rate interval in whole seconds, at least 60, aligned to the creation time; changing it with schedule_update re-aligns it to the save time."
    },
    "daily": {
      "type": "object",
      "description": "Every day at a local time.",
      "additionalProperties": false,
      "properties": {
        "time": {
          "type": "string",
          "description": "HH:mm:ss with optional 1-3 fractional digits, for example 23:00:00."
        },
        "time_zone": {
          "type": "string",
          "description": "UTC or IANA Area/Location, for example Asia/Shanghai."
        }
      },
      "required": [
        "time",
        "time_zone"
      ]
    },
    "weekly": {
      "type": "object",
      "description": "On the given weekdays at a local time.",
      "additionalProperties": false,
      "properties": {
        "time": {
          "type": "string",
          "description": "HH:mm:ss with optional 1-3 fractional digits, for example 09:00:00."
        },
        "time_zone": {
          "type": "string",
          "description": "UTC or IANA Area/Location, for example Asia/Shanghai."
        },
        "weekdays": {
          "type": "array",
          "description": "ISO weekdays, Monday 1 through Sunday 7, without repetitions.",
          "items": {
            "type": "integer"
          }
        }
      },
      "required": [
        "time",
        "time_zone",
        "weekdays"
      ]
    },
    "cron": {
      "type": "object",
      "description": "Five-field Vixie cron expression in a time zone.",
      "additionalProperties": false,
      "properties": {
        "expression": {
          "type": "string",
          "description": "minute hour day-of-month month day-of-week, for example \"*/15 9-17 * * 1-5\". When both day fields are restricted, a date matches if either one matches."
        },
        "time_zone": {
          "type": "string",
          "description": "UTC or IANA Area/Location, for example Asia/Shanghai."
        }
      },
      "required": [
        "expression",
        "time_zone"
      ]
    },
    "at": {
      "oneOf": [
        {
          "type": "string"
        },
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "date": {
              "type": "string"
            },
            "time": {
              "type": "string"
            },
            "time_zone": {
              "type": "string"
            }
          },
          "required": [
            "date",
            "time",
            "time_zone"
          ]
        }
      ],
      "description": "Absolute target: an RFC 3339 date-time with offset, or a local date, time, and IANA time_zone."
    }
  },
  "required": [
    "id"
  ]
}
```

Source: [`packages/schedule/tool-schedule/src/index.ts`](../packages/schedule/tool-schedule/src/index.ts)

preset 或 Agent scope 挂载本包；由 preset 决定哪些 agent 获得这四个管理工具。每次调用都作用于调用方 Agent 的 Session。接受 after_seconds、显式绝对 at、有界固定速率 every_seconds、带显式 IANA 时区的每日与每周本地时间，以及作为五字段表达式的 cron。管理使用宿主 storage domain；到期消息会恢复原 Session。

<a id="deepseek-aidsh-tool-lsp"></a>

## `@deepseek-ai/dsh-tool-lsp`

### `lsp`

查询语言服务器，以精确导航代码。operation 可取 goToDefinition、findReferences、goToImplementation 或 hover。line 和 character 是从 1 开始的 UTF-16 光标坐标。findReferences 包含声明。

```json
{
  "type": "object",
  "properties": {
    "operation": {
      "type": "string",
      "description": "goToDefinition, findReferences, goToImplementation, or hover.",
      "enum": [
        "goToDefinition",
        "findReferences",
        "goToImplementation",
        "hover"
      ]
    },
    "file_path": {
      "type": "string",
      "description": "The source file to query, relative to the workspace or absolute."
    },
    "line": {
      "type": "number",
      "description": "One-based line of the cursor."
    },
    "character": {
      "type": "number",
      "description": "One-based UTF-16 column of the cursor."
    }
  },
  "required": [
    "operation",
    "file_path",
    "line",
    "character"
  ]
}
```

来源：[`packages/lsp/tool-lsp/src/index.ts`](../packages/lsp/tool-lsp/src/index.ts)

lsp 工具将提供方选择和语言服务器子进程置于 ctx.lsp 之后，因此其模型可见 schema 在更换提供方时保持稳定。运行时要求已注册提供方，例如 `@deepseek-ai/dsh-lsp-stdio`；如果没有提供方，查询会返回结构化 `LSP_UNAVAILABLE` 错误，而不会改变 schema。

<a id="deepseek-aidsh-experimental-tool-ralph"></a>

## `@deepseek-ai/dsh-tool-ralph`

### `ralph`

围绕一个不可变目标运行使用全新 agent 的前台 Ralph 循环。仅当直接人类明确要求 Ralph 或使用全新 agent 迭代时使用。每个 Round 都会启动一个全新子级，该子级看不到父级对话或先前子会话；共享工作区充当长期记忆，Round 之间只传递有界的结构化报告。当工作进程报告完成、报告具体阻塞项或达到 Round 上限时，调用返回。普通的长期同会话工作应使用 goal 工具。

```json
{
  "type": "object",
  "properties": {
    "objective": {
      "type": "string",
      "description": "The immutable completion objective for every fresh Ralph round."
    },
    "maxRounds": {
      "type": "number",
      "description": "Optional positive safe-integer round cap, bounded by the deployment ceiling."
    }
  },
  "required": [
    "objective"
  ]
}
```

来源：[`packages/experimental/tool-ralph/src/index.ts`](../packages/experimental/tool-ralph/src/index.ts)

固定的前台工作流会在每个 Round 启动一个全新的结构化子级；模型只能选择不可变目标和可选的 Round 上限。

<a id="deepseek-aidsh-tool-skill"></a>

## `@deepseek-ai/dsh-tool-skill`

### `skill`

加载某项 skill（技能）的完整说明。在执行点名某项 skill 或与会话 skill 目录中某项 skill 明确匹配的任务前，请调用此工具。

```json
{
  "type": "object",
  "properties": {
    "name": {
      "type": "string",
      "description": "The exact skill name from the available skills list."
    }
  },
  "required": [
    "name"
  ]
}
```

来源：[`packages/skill/tool-skill/src/index.ts`](../packages/skill/tool-skill/src/index.ts)

<a id="deepseek-aidsh-tool-session-query"></a>

## `@deepseek-ai/dsh-tool-session-query`

### `session_event_read`

从一个已获授权的会话中读取一个完整且未删节的事件，以及可选的相邻原始事件概述。

```json
{
  "type": "object",
  "properties": {
    "session_id": {
      "type": "string",
      "description": "Target session id. Omit for the current session."
    },
    "seq": {
      "type": "integer",
      "description": "Target event sequence number."
    },
    "before": {
      "type": "integer",
      "description": "Number of preceding raw events to summarize. Omit for none."
    },
    "after": {
      "type": "integer",
      "description": "Number of following raw events to summarize. Omit for none."
    }
  },
  "required": [
    "seq"
  ]
}
```

来源：[`packages/experimental/tool-session-query/src/index.ts`](../packages/experimental/tool-session-query/src/index.ts)

### `session_event_search`

在一个已获授权的会话中搜索先前事件；如果搜索当前会话，则排除执行此次调用的步骤。

```json
{
  "type": "object",
  "properties": {
    "session_id": {
      "type": "string",
      "description": "Target session id. Omit for the current session."
    },
    "query": {
      "type": "string",
      "description": "Literal full-text query over the target session."
    },
    "seq_from": {
      "type": "integer",
      "description": "Inclusive event sequence lower bound."
    },
    "seq_to": {
      "type": "integer",
      "description": "Inclusive event sequence upper bound."
    },
    "time_from": {
      "type": "string",
      "description": "Inclusive timezone-qualified ISO 8601 event-time lower bound."
    },
    "time_to": {
      "type": "string",
      "description": "Inclusive timezone-qualified ISO 8601 event-time upper bound."
    },
    "event_types": {
      "type": "array",
      "description": "Event types to include.",
      "items": {
        "type": "string"
      }
    },
    "surfaces": {
      "type": "array",
      "description": "Event surfaces to include.",
      "items": {
        "type": "string",
        "enum": [
          "current",
          "shadowed",
          "log-only"
        ]
      }
    }
  },
  "required": [
    "query"
  ]
}
```

来源：[`packages/experimental/tool-session-query/src/index.ts`](../packages/experimental/tool-session-query/src/index.ts)

### `session_event_trace`

读取已获授权会话中某个事件的所有直接替换关系，以及该事件与其引用的来源事件之间的关系。

```json
{
  "type": "object",
  "properties": {
    "session_id": {
      "type": "string",
      "description": "Target session id. Omit for the current session."
    },
    "seq": {
      "type": "integer",
      "description": "Target event sequence number."
    }
  },
  "required": [
    "seq"
  ]
}
```

来源：[`packages/experimental/tool-session-query/src/index.ts`](../packages/experimental/tool-session-query/src/index.ts)

### `session_search`

搜索调用方工作区中的先前会话，并从每个会话返回匹配度最高的事件。

```json
{
  "type": "object",
  "properties": {
    "query": {
      "type": "string",
      "description": "Literal full-text query over prior session history."
    },
    "session_ids": {
      "type": "array",
      "description": "Optional session ids to include.",
      "items": {
        "type": "string"
      }
    },
    "created_at_from": {
      "type": "string",
      "description": "Inclusive timezone-qualified ISO 8601 creation-time lower bound."
    },
    "created_at_to": {
      "type": "string",
      "description": "Inclusive timezone-qualified ISO 8601 creation-time upper bound."
    },
    "parent_session_ids": {
      "type": "array",
      "description": "Optional direct parent session ids.",
      "items": {
        "type": "string"
      }
    },
    "include_root_sessions": {
      "type": "boolean",
      "description": "Include sessions with no parent in the parent filter."
    },
    "availability": {
      "type": "array",
      "description": "Require at least one selected source availability.",
      "items": {
        "type": "string",
        "enum": [
          "live",
          "persisted"
        ]
      }
    },
    "event_seq_from": {
      "type": "integer",
      "description": "Inclusive event sequence lower bound."
    },
    "event_seq_to": {
      "type": "integer",
      "description": "Inclusive event sequence upper bound."
    },
    "event_time_from": {
      "type": "string",
      "description": "Inclusive timezone-qualified ISO 8601 event-time lower bound."
    },
    "event_time_to": {
      "type": "string",
      "description": "Inclusive timezone-qualified ISO 8601 event-time upper bound."
    },
    "event_types": {
      "type": "array",
      "description": "Event types to include.",
      "items": {
        "type": "string"
      }
    },
    "event_surfaces": {
      "type": "array",
      "description": "Event surfaces to include.",
      "items": {
        "type": "string",
        "enum": [
          "current",
          "shadowed",
          "log-only"
        ]
      }
    }
  },
  "required": [
    "query"
  ]
}
```

来源：[`packages/experimental/tool-session-query/src/index.ts`](../packages/experimental/tool-session-query/src/index.ts)

### `session_trace`

读取围绕一个会话的已授权会话谱系，包括完整可见的祖先和后代关系。

```json
{
  "type": "object",
  "properties": {
    "session_id": {
      "type": "string",
      "description": "Target session id. Omit for the current session."
    }
  }
}
```

来源：[`packages/experimental/tool-session-query/src/index.ts`](../packages/experimental/tool-session-query/src/index.ts)

这 5 个只读工具会隐藏提供方游标，并根据不可变的调用 agent 会话为每个结果授权。该包需要选择启用；需要强制截止时间或限制行内输出的组合还会挂载通用超时或 spill 策略。

<a id="deepseek-aidsh-tool-subagent"></a>

## `@deepseek-ai/dsh-tool-subagent`

### `list_subagent_models`

发现 subagent 可用的 LLM 路由，不更改当前 Agent。无参数调用会列出已注册提供方；提供 `provider` 时会列出其公布的模型；同时提供 `provider` 和 `model` 时会检查该精确模型及其推理强度。目录条目只提供建议：adapter 可能接受未列出的模型 id。把返回的 id 用于委派工具的 `provider`、`model` 与 `reasoning_effort` 字段。

```json
{
  "type": "object",
  "properties": {
    "provider": {
      "type": "string",
      "description": "Registered LLM provider id. Omit to list providers."
    },
    "model": {
      "type": "string",
      "description": "Exact model id to inspect. Requires provider; omit to list that provider's advertised models."
    }
  }
}
```

来源：[`packages/subagent/tool-subagent/src/list-models.ts`](../packages/subagent/tool-subagent/src/list-models.ts)

### `subagent`

将一项自包含任务委派给 subagent（在自身上下文中工作的独立 agent），用它卸载聚焦且独立的工作，例如研究、限定范围的实现或分析，以免消耗当前对话的上下文。subagent 会返回结果，但不会返回中间步骤。请提供完整、独立的提示词，因为它看不到当前对话。此工具启动独立管理的 subagent 并立即返回其 id。运行时会在任务结束时通知你。子代理通过 `send_message` 汇报结果；你可以使用 `send_message` 在运行时引导它，或在结束后继续对话。

```json
{
  "type": "object",
  "properties": {
    "cwd": {
      "type": "string",
      "description": "Initial child working directory. Relative paths use your current directory; omitted inherits it. Later directory changes in either agent are independent."
    },
    "description": {
      "type": "string",
      "description": "A short (3-5 word) description of the delegated task, for display."
    },
    "prompt": {
      "type": "string",
      "description": "The complete, self-contained task for the subagent. It does not share this conversation's context, so include everything it needs."
    }
  },
  "required": [
    "description",
    "prompt"
  ]
}
```

来源：[`packages/subagent/tool-subagent/src/index.ts`](../packages/subagent/tool-subagent/src/index.ts)

注册的委派工具名称取决于加载时 `toolName` 配置（默认为 `subagent`）；上述默认 schema 关闭模型选择，而发现 schema 则展示为已启用 Session 中可用的固定配套工具。Web preset 会在每个新顶层 Session 创建时读取插件页偏好，并为其子 Session 保留该决定；`subagent_fork` 始终使用固定路由。每个实例通过 `provider` 选择后端，通过 `modelSelectionSettings` 读取模型选择设置。每次委派都会立即返回子代理 id；本地子代理接受后续消息，外部后端自动汇报单次执行结果。

<a id="deepseek-aidsh-tool-subagent-control"></a>

## `@deepseek-ai/dsh-tool-subagent-control`

### `interrupt_agent`

根据 agent id 请求取消后台 agent 的当前轮次。目标可以是你的直接子级，也可以是在你下方创建的更深层 agent。对于本地 agent，只有当前轮次会停止：已经排队发给该 agent 的消息会一直搁置到后续的 send_message；它启动的 agent 会继续运行；该 agent 本身仍可接受后续操作。外部执行会永久停止，无法接收后续消息。停止请求被接受后，此调用立即返回，因此目标可能还会短暂运行；中断一个已经完成的 agent 是可接受的空操作。

```json
{
  "type": "object",
  "properties": {
    "agent_id": {
      "type": "string",
      "description": "The id of an agent created under you: your direct child or a deeper descendant."
    }
  },
  "required": [
    "agent_id"
  ]
}
```

来源：[`packages/subagent/tool-subagent-control/src/index.ts`](../packages/subagent/tool-subagent-control/src/index.ts)

### `list_agents`

按持久 ID 和标签列出调用方目录中可继续的直接子代理。使用此工具回忆已启动的子代理，不要轮询完成状态；完成时会收到通知。状态来自在线注册表：running 表示当前正在工作；inactive 表示当前没有执行轮次，无论子代理已加载还是需要恢复，并不表示任务完成、成功、失败或正在等待其他代理。`send_message` 可在最近的步骤边界引导正在运行的子代理，或启动、恢复非活跃子代理的轮次；任何状态下的直接子级都可作为 `send_message` 的候选目标。列表快照不保证投递成功，`send_message` 仍进行权威检查，且可能失败。

```json
{
  "type": "object",
  "properties": {
    "scope": {
      "type": "string",
      "description": "children (default) lists direct children, which accept send_message in any status. descendants lists the whole tree below you with each entry's parent session id and depth; entries deeper than 1 accept only interrupt_agent.",
      "enum": [
        "children",
        "descendants"
      ]
    }
  }
}
```

来源：[`packages/subagent/tool-subagent-control/src/list-agents.ts`](../packages/subagent/tool-subagent-control/src/list-agents.ts)

### `send_message`

根据 agent id 向直接可继续 child 发送消息。如果你是驻留的可继续 child，也可以把自己的直接 parent 作为目标。如果目标仍在工作，消息会 steer 其最近的 step；如果目标处于 idle，消息会启动一个轮次。此调用不会返回该 agent 的答案，只会确认消息已投递。调用失败表示消息**未**投递。

```json
{
  "type": "object",
  "properties": {
    "agent_id": {
      "type": "string",
      "description": "The agent id of your direct continuable child, or your direct parent when you are a resident continuable child."
    },
    "message": {
      "type": "string",
      "description": "The message to deliver to the agent."
    }
  },
  "required": [
    "agent_id",
    "message"
  ]
}
```

来源：[`packages/subagent/tool-subagent-control/src/index.ts`](../packages/subagent/tool-subagent-control/src/index.ts)

这些是用于发现、中断托管 subagent 和向本地子代理发送消息的全局命名工具：绑定提供方的 `tool-subagent` 实例注册不同的委派工具；本包注册一次 `send_message` 和 `interrupt_agent`，另由 `list_agents` 通过单独加载的 `/list-agents` 插件提供，其目录行使用 sessionProjections 和实时 Agent 注册表。

<a id="deepseek-aidsh-tool-jobs"></a>

## `@deepseek-ai/dsh-tool-jobs`

### `job_kill`

请求取消正在运行的后台任务。

```json
{
  "type": "object",
  "properties": {
    "job_id": {
      "type": "string",
      "description": "Job id returned by the tool that started the background work."
    },
    "reason": {
      "type": "string",
      "description": "Optional short reason, recorded in the log and forwarded to the job."
    }
  },
  "required": [
    "job_id"
  ]
}
```

来源：[`packages/jobs/tool-jobs/src/index.ts`](../packages/jobs/tool-jobs/src/index.ts)

### `job_list`

列出你的后台任务（包括正在运行和已完成的任务）及其 id、种类和状态。

```json
{
  "type": "object",
  "properties": {}
}
```

来源：[`packages/jobs/tool-jobs/src/index.ts`](../packages/jobs/tool-jobs/src/index.ts)

### `job_output`

读取后台任务：流式任务返回自上次读取以来的输出，已完成的最终输出任务返回其结果。

```json
{
  "type": "object",
  "properties": {
    "job_id": {
      "type": "string",
      "description": "Job id returned by the tool that started the background work."
    },
    "wait": {
      "type": "boolean",
      "description": "Block until the job finishes or the timeout expires; a timed-out wait leaves the job running. Defaults to false."
    },
    "timeout_ms": {
      "type": "number",
      "description": "Max wait in milliseconds with wait: true. Defaults to and is capped by configuration."
    }
  },
  "required": [
    "job_id"
  ]
}
```

来源：[`packages/jobs/tool-jobs/src/index.ts`](../packages/jobs/tool-jobs/src/index.ts)

与任务种类无关的后台任务控制器：后台 bash 命令与 PTY 发送 都通过相同的 3 个工具读取、列出和终止。加载该插件会挂接控制器，从而启用生产方的 `ctx.jobs.start()`。

<a id="deepseek-aidsh-experimental-tool-agent-team"></a>

## `@deepseek-ai/dsh-experimental-tool-agent-team`

### `interrupt_agent`

中断一名 teammate 的当前 turn，同时保留其待处理 inbox。仅 Team Lead 可用。

```json
{
  "type": "object",
  "properties": {
    "target": {
      "type": "string",
      "description": "Teammate target returned by spawn_teammate or list_agents."
    }
  },
  "required": [
    "target"
  ]
}
```

来源：[`packages/experimental/tool-agent-team/src/index.ts`](../packages/experimental/tool-agent-team/src/index.ts)

### `list_agents`

列出 Lead 与所有持久 teammate，以及可用于寻址的 target 和当前可用状态。inactive 表示没有轮次在执行，不表示任务结果。provisioning 与 failed 描述成员创建状态。

```json
{
  "type": "object",
  "properties": {}
}
```

来源：[`packages/experimental/tool-agent-team/src/index.ts`](../packages/experimental/tool-agent-team/src/index.ts)

### `send_message`

向另一名 Team member 发送一条消息。running target 会在最近的步骤边界收到消息；inactive target 会启动或恢复一个 turn。

```json
{
  "type": "object",
  "properties": {
    "target": {
      "type": "string",
      "description": "Member target returned by spawn_teammate or list_agents, including lead."
    },
    "message": {
      "type": "string",
      "description": "Self-contained message for the target."
    }
  },
  "required": [
    "target",
    "message"
  ]
}
```

来源：[`packages/experimental/tool-agent-team/src/index.ts`](../packages/experimental/tool-agent-team/src/index.ts)

### `spawn_teammate`

创建一名具名、持久的 teammate。只有 Team Lead 可以调用此工具。

```json
{
  "type": "object",
  "properties": {
    "name": {
      "type": "string",
      "description": "Unique lower-kebab-case teammate name."
    },
    "description": {
      "type": "string",
      "description": "Short description of the delegated responsibility."
    },
    "prompt": {
      "type": "string",
      "description": "Complete initial task for the teammate."
    },
    "context": {
      "type": "string",
      "description": "fresh starts without Lead history; fork inherits completed Lead turns. Defaults to fresh.",
      "enum": [
        "fresh",
        "fork"
      ]
    }
  },
  "required": [
    "name",
    "description",
    "prompt"
  ]
}
```

来源：[`packages/experimental/tool-agent-team/src/index.ts`](../packages/experimental/tool-agent-team/src/index.ts)

### `team_task_create`

在共享 Team 任务板上创建一个无 owner 的 pending task。

```json
{
  "type": "object",
  "properties": {
    "subject": {
      "type": "string",
      "description": "Concise task title."
    },
    "description": {
      "type": "string",
      "description": "Complete task details and acceptance criteria."
    },
    "blocked_by": {
      "type": "array",
      "description": "Task ids that must complete first.",
      "items": {
        "type": "string"
      }
    },
    "write_scopes": {
      "type": "array",
      "description": "Advisory workspace-relative file or directory prefixes this task expects to modify.",
      "items": {
        "type": "string"
      }
    }
  },
  "required": [
    "subject",
    "description"
  ]
}
```

来源：[`packages/experimental/tool-agent-team/src/index.ts`](../packages/experimental/tool-agent-team/src/index.ts)

### `team_task_get`

在修改或执行共享任务前，读取其完整的最新值。

```json
{
  "type": "object",
  "properties": {
    "task_id": {
      "type": "string",
      "description": "Shared task id."
    }
  },
  "required": [
    "task_id"
  ]
}
```

来源：[`packages/experimental/tool-agent-team/src/index.ts`](../packages/experimental/tool-agent-team/src/index.ts)

### `team_task_list`

列出共享任务，包括 readiness、owner、revision、blocker 与 write-scope warning。

```json
{
  "type": "object",
  "properties": {
    "status": {
      "type": "string",
      "description": "Optional exact status filter.",
      "enum": [
        "pending",
        "in_progress",
        "completed"
      ]
    },
    "owner": {
      "type": "string",
      "description": "Optional member target from spawn_teammate or list_agents, matching ownerName; use unowned for tasks without an owner."
    },
    "ready": {
      "type": "boolean",
      "description": "Optional readiness filter."
    },
    "cursor": {
      "type": "integer",
      "description": "Zero-based result offset. Defaults to 0."
    },
    "limit": {
      "type": "integer",
      "description": "Number of rows, 1 through 100. Defaults to 50."
    }
  }
}
```

来源：[`packages/experimental/tool-agent-team/src/index.ts`](../packages/experimental/tool-agent-team/src/index.ts)

### `team_task_update`

使用 team_task_get 或 team_task_list 返回的最新 revision，对共享任务操作执行 compare-and-set。

```json
{
  "type": "object",
  "properties": {
    "task_id": {
      "type": "string",
      "description": "Shared task id."
    },
    "expected_revision": {
      "type": "integer",
      "description": "Current task revision used as the CAS precondition."
    },
    "action": {
      "type": "string",
      "description": "Task transition to apply.",
      "enum": [
        "claim",
        "release",
        "edit",
        "set_dependencies",
        "complete",
        "reopen",
        "reassign",
        "delete"
      ]
    },
    "subject": {
      "type": "string",
      "description": "Replacement title for edit."
    },
    "description": {
      "type": "string",
      "description": "Replacement details for edit."
    },
    "blocked_by": {
      "type": "array",
      "description": "Complete blocker list for set_dependencies.",
      "items": {
        "type": "string"
      }
    },
    "write_scopes": {
      "type": "array",
      "description": "Replacement advisory write scopes for edit.",
      "items": {
        "type": "string"
      }
    },
    "owner": {
      "type": "string",
      "description": "Member target from spawn_teammate or list_agents for Lead-only reassign; omit to unassign."
    }
  },
  "required": [
    "task_id",
    "expected_revision",
    "action"
  ]
}
```

来源：[`packages/experimental/tool-agent-team/src/index.ts`](../packages/experimental/tool-agent-team/src/index.ts)

### `wait_agent`

等待本次调用开始后下一次 teammate 状态、消息或共享任务变更。它绝不会唤醒 inactive member；若没有其他 member 正在 running 或 provisioning，则立即返回 noProgress。唤醒或超时后应重新列出状态，而不是轮询。

```json
{
  "type": "object",
  "properties": {
    "timeout_ms": {
      "type": "integer",
      "description": "Wait duration in milliseconds, from 10000 through 3600000. Defaults to 30000."
    }
  }
}
```

来源：[`packages/experimental/tool-agent-team/src/index.ts`](../packages/experimental/tool-agent-team/src/index.ts)

这 10 个工具限定于隐式 Team Lead 与持久 teammate 作用域。随产品发布的 dsh-base bundle 默认禁用该包；文档中的 Agent Teams profile patch 会启用它，并禁用旧 continuable child 的同名控制工具。


<a id="deepseek-aidsh-tool-todo"></a>

## `@deepseek-ai/dsh-tool-todo`

### `todo_write`

记录并更新任务列表，用于规划多步骤工作并展示进度；简单的单步骤任务无需使用。开始前为每个具体步骤添加一项 todo。只要工作尚未完成，就将正在处理的 todo 标记为 `in_progress`，仅在工作并行运行时同时标记多项。某项 todo 完成后立即标记为 `completed`。

```json
{
  "type": "object",
  "properties": {
    "todos": {
      "type": "array",
      "description": "The COMPLETE task list, replacing any previous list.",
      "items": {
        "type": "object",
        "additionalProperties": false,
        "properties": {
          "content": {
            "type": "string",
            "description": "What the task is — a short imperative line."
          },
          "status": {
            "type": "string",
            "description": "pending (not started) | in_progress (now) | completed (done).",
            "enum": [
              "pending",
              "in_progress",
              "completed"
            ]
          }
        },
        "required": [
          "content",
          "status"
        ]
      }
    }
  },
  "required": [
    "todos"
  ]
}
```

来源：[`packages/todo/tool-todo/src/index.ts`](../packages/todo/tool-todo/src/index.ts)

todo_write 是会话所有的状态；UI 将最新的 todo/write 事件渲染为检查清单。`allowParallelInProgress` 是没有默认值的必填项，因此本目录明确选择 `true`，对应描述允许同时存在多个 `in_progress` 项。选择 `false` 的部署会获得同一工具，但描述会要求只能有 1 个活动任务。

<a id="deepseek-aidsh-tool-working-directory"></a>

## `@deepseek-ai/dsh-tool-working-directory`

### `working_directory`

读取当前工作目录，或通过 cd 改变它。相对路径使用当前目录。已有 shell 和运行中的进程保留自己的目录。

```json
{
  "type": "object",
  "properties": {
    "cd": {
      "type": "string",
      "description": "Existing directory to enter. Omit to read the current directory."
    }
  }
}
```

来源：[`packages/session/tool-working-directory/src/index.ts`](../packages/session/tool-working-directory/src/index.ts)

读取或改变调用方 Session 的目录。已有 shell 和进程保留自己的目录；原始元数据和权限根目录保持不变。

<a id="deepseek-aidsh-experimental-tool-worktree"></a>

## `@deepseek-ai/dsh-experimental-tool-worktree`

### `create_worktree`

从本地提交、分支或标签创建新的 Git 分支与工作树，再将本 Session 的工作目录切换到该位置。默认使用 HEAD 和自动生成的名称。未提交文件保留在源检出目录中。名称已存在时失败。使用 working_directory 的 cd 参数离开；检出目录与分支仍然保留。

```json
{
  "type": "object",
  "properties": {
    "name": {
      "type": "string",
      "description": "New branch name, also used as the checkout directory. Omit to generate a unique name."
    },
    "from": {
      "type": "string",
      "description": "Local commit, branch, or tag to start from. Defaults to HEAD; no fetch is performed."
    }
  }
}
```

来源：[`packages/experimental/tool-worktree/src/index.ts`](../packages/experimental/tool-worktree/src/index.ts)

仅通过显式实验组合加载。在现有写权限下从固定的本地提交创建新分支与检出目录，再改变 Session 工作目录。离开时保留分支与检出目录。

<a id="deepseek-aidsh-tool-workflow"></a>

## `@deepseek-ai/dsh-tool-workflow`

### `workflow`

运行用于大规模编排 subagent 的 JavaScript 工作流脚本。当工作会分散到许多相互独立的部分时，请使用此工具，例如审查大量文件、执行迁移、开展多角度研究或对发现进行对抗式验证；此时应将编排写成脚本，而不是逐轮委派。

脚本函数体提供以下钩子：

- `agent(prompt, opts?): Promise<any>`：运行一个 subagent 直至完成。不提供 `opts.schema` 时，解析为子级最终文本；提供 `opts.schema` 时，它必须是以对象为根、且**只能**使用 type/properties/required/additionalProperties/items/enum/const/oneOf 的 JSON Schema，此时解析为通过校验的对象。子级失败时解析为 `null`，可使用 `.filter(Boolean)` 过滤。其他选项包括 `label`（显示名称）、`phase`（进度组），以及相互独立的 `provider`／`model` LLM（大语言模型）目标覆盖项。
- `pipeline(items, ...stages): Promise<any[]>`：让每个条目分别经过各阶段，阶段之间**没有**屏障；多阶段工作优先使用它。每个阶段接收 `(prev, item, index)`。阶段异常会将该**条目**变为 `null`，并跳过它的剩余阶段。
- `parallel(thunks): Promise<any[]>`：并发运行零参数函数并等待**全部**完成。它会形成屏障，仅当某个阶段确实需要汇总全部先前结果时使用。抛出异常的 thunk 解析为 `null`。
- `phase(title)`：开始一个进度阶段；`log(message)`：说明进度；`args`：工具调用的 `args` 输入，原样提供。

如果误用钩子（参数错误、未知选项、不受支持的 schema、触发上限），整个脚本会终止，而不会产生 `null`。脚本没有文件系统、网络、定时器或 Node.js API；具体工作由 agent 完成。

```json
{
  "type": "object",
  "properties": {
    "script": {
      "type": "string",
      "description": "The plain JavaScript body, not TypeScript and without an `export const meta` statement; top-level await is allowed. End with `return <value>`; the JSON-serializable value is this tool's result."
    },
    "meta": {
      "type": "object",
      "description": "The workflow identity as plain JSON, not code.",
      "additionalProperties": true,
      "properties": {
        "name": {
          "type": "string",
          "description": "Short kebab-case workflow name."
        },
        "description": {
          "type": "string",
          "description": "One-line description of what the workflow does."
        },
        "whenToUse": {
          "type": "string",
          "description": "Optional guidance on when this workflow applies."
        },
        "phases": {
          "type": "array",
          "description": "Optional phase declarations matched by phase() calls.",
          "items": {
            "type": "object",
            "additionalProperties": true,
            "properties": {
              "title": {
                "type": "string",
                "description": "The phase title phase() calls match by exact string."
              },
              "detail": {
                "type": "string",
                "description": "Optional one-line description of the phase."
              },
              "provider": {
                "type": "string",
                "description": "Optional provider override this phase is expected to use."
              },
              "model": {
                "type": "string",
                "description": "Optional model override this phase is expected to use."
              }
            },
            "required": [
              "title"
            ]
          }
        }
      },
      "required": [
        "name",
        "description"
      ]
    },
    "args": {
      "type": "object",
      "description": "Optional JSON input exposed to the script as the `args` global (wrap a bare list as a field, e.g. {\"files\": [...]}).",
      "additionalProperties": true
    },
    "run_in_background": {
      "type": "boolean",
      "description": "Run as a background job: return a job id immediately instead of waiting; the return value arrives with the completion notice."
    }
  },
  "required": [
    "script",
    "meta"
  ]
}
```

来源：[`packages/workflow/tool-workflow/src/index.ts`](../packages/workflow/tool-workflow/src/index.ts)

<a id="deepseek-aidsh-tool-workspace-dependencies"></a>

## `@deepseek-ai/dsh-tool-workspace-dependencies`

### `load_workspace_dependencies`

获取随包附带的 Python 和库目录的绝对路径，以及随包 Python 发行版的版本。payload 提供 Node.js 和 pnpm 时才返回对应路径。Python 含 numpy、pandas、python-docx、python-pptx、openpyxl、Pillow、lxml 与 XlsxWriter。除非用户或工作区指令选择了别的环境，Office 文件请使用这些库。返回 Node.js 和 pnpm 路径时，用该 Node 可执行文件和 pnpm 脚本路径运行 pnpm。本工具不改 PATH，也不改包管理器设置。

```json
{
  "type": "object",
  "properties": {}
}
```

来源：[`packages/skill/tool-workspace-dependencies/src/index.ts`](../packages/skill/tool-workspace-dependencies/src/index.ts)

<a id="deepseek-aidsh-tool-web"></a>

## `@deepseek-ai/dsh-tool-web`

### `web_fetch`

获取指定 HTTP(S) URL 的内容，并将其解码为文本后返回。

```json
{
  "type": "object",
  "properties": {
    "url": {
      "type": "string",
      "description": "The HTTP(S) URL to fetch."
    }
  },
  "required": [
    "url"
  ]
}
```

来源：[`packages/web/tool-web/src/index.ts`](../packages/web/tool-web/src/index.ts)

### `web_search`

在 Web 上搜索最新信息。返回可选的摘要答案和来源 URL 列表。

```json
{
  "type": "object",
  "properties": {
    "queries": {
      "type": "array",
      "description": "1–4 search queries; their results are merged.",
      "items": {
        "type": "string"
      }
    }
  },
  "required": [
    "queries"
  ]
}
```

来源：[`packages/web/tool-web/src/index.ts`](../packages/web/tool-web/src/index.ts)

web_search 和 web_fetch 将提供方选择置于 ctx.web 之后，使模型可见 schema 在更换后端时保持稳定。

<a id="deepseek-aidsh-tool-wordinweb"></a>

## `@deepseek-ai/dsh-tool-wordinweb`

### `word_document_asset`

读取文档检查引用的图片或内嵌资源字节。

```json
{
  "type": "object",
  "properties": {
    "ref": {
      "type": "string"
    }
  },
  "required": [
    "ref"
  ],
  "additionalProperties": false
}
```

来源： [`packages/document/tool-wordinweb/src/index.ts`](../packages/document/tool-wordinweb/src/index.ts)

### `word_document_capabilities`

列出 schema 化的文档编辑操作。按类别过滤以压缩上下文。

```json
{
  "type": "object",
  "properties": {
    "category": {
      "enum": [
        "text",
        "paragraph",
        "review",
        "table",
        "insert",
        "drawing",
        "math",
        "document"
      ]
    },
    "kind": {
      "enum": [
        "insertText",
        "insertSeparator",
        "deleteSeparator",
        "deleteText",
        "splitParagraph",
        "formatRun",
        "formatParagraph",
        "formatRange",
        "tableOp",
        "mergeParagraph",
        "commentRun",
        "pasteBlocks",
        "insertImage",
        "suggestRevision",
        "insertBreak",
        "insertMath",
        "insertShape",
        "replyComment",
        "adjustIndent",
        "setSpacing",
        "insertPageField",
        "setLink",
        "insertFootnote",
        "setDropCap",
        "setDivider",
        "insertBookmark",
        "insertBlankPage",
        "insertSectionBreak",
        "insertCrossRef",
        "insertCoverPage",
        "setPageLayout",
        "setListLevel",
        "insertWordArt",
        "insertChart",
        "insertSmartArt",
        "setLineNumbering",
        "insertDateTimeField",
        "insertField",
        "setDrawingRotation",
        "setDrawingFill",
        "setChartData",
        "setSmartArtNodeText",
        "setDrawingWordArtText",
        "setDrawingWordArtStyle",
        "setDrawingLineStyle",
        "setImageAltText",
        "removeLink",
        "setImageWrap",
        "setDrawingOrder",
        "setSmartArtData",
        "setSmartArtFill",
        "setSmartArtTextFormat",
        "setFloatingPagePosition",
        "resizeDrawing",
        "resizeTableColumn",
        "moveTable",
        "removeDrawing",
        "setMathLinear",
        "deleteMath",
        "moveMath",
        "ensureHeaderFooter",
        "deleteComment",
        "resolveComment",
        "editComment",
        "insertBookmarkRange",
        "acceptRevision",
        "rejectRevision",
        "acceptAllRevisions",
        "rejectAllRevisions",
        "setListType",
        "insertTable",
        "resizeTableRow",
        "toggleCheckbox",
        "updateFields",
        "insertToc",
        "insertCaption",
        "ensureRefBookmark",
        "insertIndexEntry",
        "insertIndex",
        "refreshIndex",
        "createStyle",
        "modifyStyle",
        "deleteStyle",
        "setNumberingLevel",
        "setNumberingRestart",
        "setParagraphBorders",
        "setTabStops",
        "setTableBorders",
        "setTableStyle",
        "setTableLook",
        "setTableWidth",
        "setTableColumnWidth",
        "setTableLayout",
        "setTableCellMargins",
        "setTableHeaderRows",
        "insertTableFormula",
        "sortTableRows",
        "convertTextToTable",
        "convertTableToText",
        "insertEndnote",
        "setCrop",
        "setModel3DRotation",
        "setDrawingTextFit",
        "insertMergeField",
        "insertCitation",
        "insertBibliography",
        "refreshBibliography",
        "createCitationSource",
        "editCitationSource",
        "deleteCitationSource",
        "setCitationStyle",
        "createBuildingBlock",
        "insertBuildingBlock",
        "deleteBuildingBlock",
        "insertWatermark",
        "insertPictureWatermark",
        "removeWatermark",
        "setTitlePage",
        "setEvenOddHeaders",
        "setPageNumberFormat",
        "setHyphenation",
        "insertPageNumberPosition",
        "removePageNumbers",
        "insertHeaderFooterPreset",
        "setFootnoteOptions",
        "setEndnoteOptions"
      ]
    }
  },
  "additionalProperties": false
}
```

来源： [`packages/document/tool-wordinweb/src/index.ts`](../packages/document/tool-wordinweb/src/index.ts)

### `word_document_compose`

用 schema 化的块、表格、媒体、原生对象、页眉页脚组装完整新文档。

```json
{
  "type": "object",
  "properties": {
    "revision": {
      "type": "string",
      "maxLength": 100,
      "minLength": 1
    },
    "body": {
      "type": "array",
      "minItems": 1,
      "maxItems": 500,
      "items": {
        "anyOf": [
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "paragraph"
              },
              "text": {
                "type": "string",
                "maxLength": 100000
              },
              "runs": {
                "type": "array",
                "minItems": 1,
                "maxItems": 1000,
                "items": {
                  "type": "object",
                  "properties": {
                    "text": {
                      "type": "string",
                      "maxLength": 100000
                    },
                    "bold": {
                      "type": "boolean"
                    },
                    "italic": {
                      "type": "boolean"
                    },
                    "underline": {
                      "type": "boolean"
                    },
                    "color": {
                      "type": "string",
                      "pattern": "^#?[0-9A-Fa-f]{6}$"
                    },
                    "highlight": {
                      "type": "string",
                      "maxLength": 40,
                      "minLength": 1
                    },
                    "fontSizePt": {
                      "type": "number",
                      "minimum": 1,
                      "maximum": 1638
                    },
                    "fontFamily": {
                      "type": "string",
                      "maxLength": 100,
                      "minLength": 1
                    }
                  },
                  "required": [
                    "text"
                  ],
                  "additionalProperties": false
                }
              },
              "styleId": {
                "enum": [
                  "Normal",
                  "Title",
                  "Subtitle",
                  "Heading1",
                  "Heading2",
                  "Heading3"
                ]
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right",
                  "both"
                ]
              },
              "spacing": {
                "type": "object",
                "properties": {
                  "beforePt": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 2000
                  },
                  "afterPt": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 2000
                  },
                  "lineMultiple": {
                    "type": "number",
                    "minimum": 0.1,
                    "maximum": 20
                  }
                },
                "required": [],
                "additionalProperties": false
              },
              "list": {
                "enum": [
                  "bullet",
                  "number"
                ]
              }
            },
            "required": [
              "type"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "heading"
              },
              "level": {
                "type": "integer",
                "minimum": 1,
                "maximum": 3
              },
              "text": {
                "type": "string",
                "maxLength": 100000,
                "minLength": 1
              }
            },
            "required": [
              "type",
              "level",
              "text"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "table"
              },
              "rows": {
                "type": "array",
                "minItems": 1,
                "maxItems": 50,
                "items": {
                  "type": "array",
                  "minItems": 1,
                  "maxItems": 50,
                  "items": {
                    "anyOf": [
                      {
                        "type": "string",
                        "maxLength": 100000
                      },
                      {
                        "type": "object",
                        "properties": {
                          "text": {
                            "type": "string",
                            "maxLength": 100000
                          },
                          "runs": {
                            "type": "array",
                            "minItems": 1,
                            "maxItems": 1000,
                            "items": {
                              "type": "object",
                              "properties": {
                                "text": {
                                  "type": "string",
                                  "maxLength": 100000
                                },
                                "bold": {
                                  "type": "boolean"
                                },
                                "italic": {
                                  "type": "boolean"
                                },
                                "underline": {
                                  "type": "boolean"
                                },
                                "color": {
                                  "type": "string",
                                  "pattern": "^#?[0-9A-Fa-f]{6}$"
                                },
                                "highlight": {
                                  "type": "string",
                                  "maxLength": 40,
                                  "minLength": 1
                                },
                                "fontSizePt": {
                                  "type": "number",
                                  "minimum": 1,
                                  "maximum": 1638
                                },
                                "fontFamily": {
                                  "type": "string",
                                  "maxLength": 100,
                                  "minLength": 1
                                }
                              },
                              "required": [
                                "text"
                              ],
                              "additionalProperties": false
                            }
                          },
                          "fill": {
                            "type": "string",
                            "pattern": "^#?[0-9A-Fa-f]{6}$"
                          },
                          "align": {
                            "enum": [
                              "left",
                              "center",
                              "right"
                            ]
                          },
                          "verticalAlign": {
                            "enum": [
                              "top",
                              "center",
                              "bottom"
                            ]
                          }
                        },
                        "required": [],
                        "additionalProperties": false
                      }
                    ]
                  }
                }
              },
              "headerRows": {
                "type": "integer",
                "minimum": 0,
                "maximum": 50
              },
              "headerFill": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "headerTextColor": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "columnWidths": {
                "type": "array",
                "minItems": 1,
                "maxItems": 50,
                "items": {
                  "type": "number",
                  "minimum": 1,
                  "maximum": 5000
                }
              }
            },
            "required": [
              "type",
              "rows"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "equation"
              },
              "mathText": {
                "type": "string"
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              }
            },
            "required": [
              "type",
              "mathText"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "chart"
              },
              "chart": {
                "type": "object",
                "properties": {
                  "type": {
                    "enum": [
                      "column",
                      "bar",
                      "line",
                      "pie",
                      "doughnut",
                      "area",
                      "scatter"
                    ]
                  },
                  "title": {
                    "type": "string",
                    "maxLength": 200
                  },
                  "categories": {
                    "type": "array",
                    "minItems": 1,
                    "maxItems": 100,
                    "items": {
                      "type": "string",
                      "maxLength": 200
                    }
                  },
                  "series": {
                    "type": "array",
                    "minItems": 1,
                    "maxItems": 24,
                    "items": {
                      "type": "object",
                      "properties": {
                        "name": {
                          "type": "string",
                          "maxLength": 200
                        },
                        "values": {
                          "type": "array",
                          "maxItems": 100,
                          "items": {
                            "type": "number"
                          }
                        }
                      },
                      "required": [
                        "name",
                        "values"
                      ],
                      "additionalProperties": false
                    }
                  },
                  "grouping": {
                    "enum": [
                      "clustered",
                      "stacked",
                      "percentStacked"
                    ]
                  }
                },
                "required": [
                  "type",
                  "categories",
                  "series"
                ],
                "additionalProperties": false
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              }
            },
            "required": [
              "type",
              "chart"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "smartArt"
              },
              "smartArt": {
                "type": "object",
                "properties": {
                  "layout": {
                    "enum": [
                      "process",
                      "cycle",
                      "hierarchy",
                      "list"
                    ]
                  },
                  "items": {
                    "type": "array",
                    "minItems": 1,
                    "maxItems": 50,
                    "items": {
                      "type": "string",
                      "maxLength": 500
                    }
                  }
                },
                "required": [
                  "layout",
                  "items"
                ],
                "additionalProperties": false
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              }
            },
            "required": [
              "type",
              "smartArt"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "image"
              },
              "assetRef": {
                "type": "string",
                "pattern": "^asset:.+$"
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "alt": {
                "type": "string",
                "maxLength": 1000
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              },
              "wrap": {
                "enum": [
                  "inline",
                  "square",
                  "topAndBottom",
                  "none",
                  "behind"
                ]
              },
              "position": {
                "type": "object",
                "properties": {
                  "xPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  },
                  "yPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  }
                },
                "required": [
                  "xPx",
                  "yPx"
                ],
                "additionalProperties": false
              }
            },
            "required": [
              "type",
              "assetRef",
              "widthPx",
              "heightPx"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "shape"
              },
              "preset": {
                "enum": [
                  "line",
                  "verticalLine",
                  "rectangle",
                  "roundedRectangle",
                  "ellipse",
                  "diamond",
                  "textBox",
                  "accentBorderCallout1",
                  "accentBorderCallout2",
                  "accentBorderCallout3",
                  "accentCallout1",
                  "accentCallout2",
                  "accentCallout3",
                  "arc",
                  "bentArrow",
                  "bentUpArrow",
                  "bevel",
                  "blockArc",
                  "borderCallout1",
                  "borderCallout2",
                  "borderCallout3",
                  "bracePair",
                  "bracketPair",
                  "callout1",
                  "callout2",
                  "callout3",
                  "can",
                  "chartPlus",
                  "chartStar",
                  "chartX",
                  "chevron",
                  "chord",
                  "circularArrow",
                  "cloud",
                  "cloudCallout",
                  "corner",
                  "cornerTabs",
                  "cube",
                  "curvedDownArrow",
                  "curvedLeftArrow",
                  "curvedRightArrow",
                  "curvedUpArrow",
                  "decagon",
                  "diagStripe",
                  "dodecagon",
                  "donut",
                  "doubleWave",
                  "downArrow",
                  "downArrowCallout",
                  "ellipseRibbon",
                  "ellipseRibbon2",
                  "flowChartAlternateProcess",
                  "flowChartCollate",
                  "flowChartConnector",
                  "flowChartDecision",
                  "flowChartDelay",
                  "flowChartDisplay",
                  "flowChartDocument",
                  "flowChartExtract",
                  "flowChartInputOutput",
                  "flowChartInternalStorage",
                  "flowChartMagneticDisk",
                  "flowChartMagneticDrum",
                  "flowChartMagneticTape",
                  "flowChartManualInput",
                  "flowChartManualOperation",
                  "flowChartMerge",
                  "flowChartMultidocument",
                  "flowChartOfflineStorage",
                  "flowChartOffpageConnector",
                  "flowChartOnlineStorage",
                  "flowChartOr",
                  "flowChartPredefinedProcess",
                  "flowChartPreparation",
                  "flowChartProcess",
                  "flowChartPunchedCard",
                  "flowChartPunchedTape",
                  "flowChartSort",
                  "flowChartSummingJunction",
                  "flowChartTerminator",
                  "foldedCorner",
                  "frame",
                  "funnel",
                  "gear6",
                  "gear9",
                  "halfFrame",
                  "heart",
                  "heptagon",
                  "hexagon",
                  "homePlate",
                  "horizontalScroll",
                  "irregularSeal1",
                  "irregularSeal2",
                  "leftArrow",
                  "leftArrowCallout",
                  "leftBrace",
                  "leftBracket",
                  "leftCircularArrow",
                  "leftRightArrow",
                  "leftRightArrowCallout",
                  "leftRightCircularArrow",
                  "leftRightRibbon",
                  "leftRightUpArrow",
                  "leftUpArrow",
                  "lightningBolt",
                  "lineInv",
                  "mathDivide",
                  "mathEqual",
                  "mathMinus",
                  "mathMultiply",
                  "mathNotEqual",
                  "mathPlus",
                  "moon",
                  "nonIsoscelesTrapezoid",
                  "noSmoking",
                  "notchedRightArrow",
                  "octagon",
                  "parallelogram",
                  "pentagon",
                  "pie",
                  "pieWedge",
                  "plaque",
                  "plaqueTabs",
                  "plus",
                  "quadArrow",
                  "quadArrowCallout",
                  "rect",
                  "ribbon",
                  "ribbon2",
                  "rightArrow",
                  "rightArrowCallout",
                  "rightBrace",
                  "rightBracket",
                  "round1Rect",
                  "round2DiagRect",
                  "round2SameRect",
                  "roundRect",
                  "rtTriangle",
                  "smileyFace",
                  "snip1Rect",
                  "snip2DiagRect",
                  "snip2SameRect",
                  "snipRoundRect",
                  "squareTabs",
                  "star10",
                  "star12",
                  "star16",
                  "star24",
                  "star32",
                  "star4",
                  "star5",
                  "star6",
                  "star7",
                  "star8",
                  "stripedRightArrow",
                  "sun",
                  "swooshArrow",
                  "teardrop",
                  "trapezoid",
                  "triangle",
                  "upArrowCallout",
                  "upDownArrow",
                  "upArrow",
                  "upDownArrowCallout",
                  "uturnArrow",
                  "verticalScroll",
                  "wave",
                  "wedgeEllipseCallout",
                  "wedgeRectCallout",
                  "wedgeRoundRectCallout"
                ]
              },
              "text": {
                "type": "string",
                "maxLength": 20000
              },
              "textStyle": {
                "type": "object",
                "properties": {
                  "bold": {
                    "type": "boolean"
                  },
                  "italic": {
                    "type": "boolean"
                  },
                  "underline": {
                    "type": "boolean"
                  },
                  "color": {
                    "type": "string",
                    "pattern": "^#?[0-9A-Fa-f]{6}$"
                  },
                  "fontSizePt": {
                    "type": "number",
                    "minimum": 1,
                    "maximum": 1638
                  },
                  "fontFamily": {
                    "type": "string",
                    "maxLength": 100,
                    "minLength": 1
                  },
                  "align": {
                    "enum": [
                      "left",
                      "center",
                      "right"
                    ]
                  }
                },
                "required": [],
                "additionalProperties": false
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "position": {
                "type": "object",
                "properties": {
                  "xPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  },
                  "yPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  }
                },
                "required": [
                  "xPx",
                  "yPx"
                ],
                "additionalProperties": false
              },
              "fill": {
                "anyOf": [
                  {
                    "type": "string",
                    "pattern": "^#?[0-9A-Fa-f]{6}$"
                  },
                  {
                    "type": "null"
                  }
                ]
              },
              "line": {
                "anyOf": [
                  {
                    "type": "null"
                  },
                  {
                    "type": "object",
                    "properties": {
                      "color": {
                        "type": "string",
                        "pattern": "^#?[0-9A-Fa-f]{6}$"
                      },
                      "widthPx": {
                        "type": "number",
                        "minimum": 1,
                        "maximum": 5000
                      },
                      "dash": {
                        "enum": [
                          "solid",
                          "dashed",
                          "dotted"
                        ]
                      }
                    },
                    "required": [
                      "color",
                      "widthPx",
                      "dash"
                    ],
                    "additionalProperties": false
                  }
                ]
              },
              "wrap": {
                "enum": [
                  "inline",
                  "square",
                  "topAndBottom",
                  "none",
                  "behind"
                ]
              },
              "order": {
                "enum": [
                  "front",
                  "back"
                ]
              }
            },
            "required": [
              "type",
              "preset"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "wordArt"
              },
              "text": {
                "type": "string",
                "minLength": 1,
                "maxLength": 500
              },
              "preset": {
                "enum": [
                  "plain",
                  "archUp",
                  "archDown",
                  "wave",
                  "chevron",
                  "circle",
                  "button",
                  "chevronDown"
                ]
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "position": {
                "type": "object",
                "properties": {
                  "xPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  },
                  "yPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  }
                },
                "required": [
                  "xPx",
                  "yPx"
                ],
                "additionalProperties": false
              },
              "rotation": {
                "type": "number"
              },
              "fill": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "opacity": {
                "type": "number",
                "minimum": 0,
                "maximum": 1
              },
              "wrap": {
                "enum": [
                  "inline",
                  "square",
                  "topAndBottom",
                  "none",
                  "behind"
                ]
              },
              "order": {
                "enum": [
                  "front",
                  "back"
                ]
              }
            },
            "required": [
              "type",
              "text",
              "preset"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "pageNumber"
              },
              "fieldKind": {
                "enum": [
                  "page",
                  "pageOfTotal"
                ]
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              },
              "color": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "fontSizePt": {
                "type": "number",
                "minimum": 1,
                "maximum": 1638
              },
              "fontFamily": {
                "type": "string",
                "maxLength": 100,
                "minLength": 1
              },
              "bold": {
                "type": "boolean"
              }
            },
            "required": [
              "type",
              "fieldKind"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "pageBreak"
              }
            },
            "required": [
              "type"
            ],
            "additionalProperties": false
          }
        ]
      }
    },
    "header": {
      "type": "array",
      "minItems": 1,
      "maxItems": 100,
      "items": {
        "anyOf": [
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "paragraph"
              },
              "text": {
                "type": "string",
                "maxLength": 100000
              },
              "runs": {
                "type": "array",
                "minItems": 1,
                "maxItems": 1000,
                "items": {
                  "type": "object",
                  "properties": {
                    "text": {
                      "type": "string",
                      "maxLength": 100000
                    },
                    "bold": {
                      "type": "boolean"
                    },
                    "italic": {
                      "type": "boolean"
                    },
                    "underline": {
                      "type": "boolean"
                    },
                    "color": {
                      "type": "string",
                      "pattern": "^#?[0-9A-Fa-f]{6}$"
                    },
                    "highlight": {
                      "type": "string",
                      "maxLength": 40,
                      "minLength": 1
                    },
                    "fontSizePt": {
                      "type": "number",
                      "minimum": 1,
                      "maximum": 1638
                    },
                    "fontFamily": {
                      "type": "string",
                      "maxLength": 100,
                      "minLength": 1
                    }
                  },
                  "required": [
                    "text"
                  ],
                  "additionalProperties": false
                }
              },
              "styleId": {
                "enum": [
                  "Normal",
                  "Title",
                  "Subtitle",
                  "Heading1",
                  "Heading2",
                  "Heading3"
                ]
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right",
                  "both"
                ]
              },
              "spacing": {
                "type": "object",
                "properties": {
                  "beforePt": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 2000
                  },
                  "afterPt": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 2000
                  },
                  "lineMultiple": {
                    "type": "number",
                    "minimum": 0.1,
                    "maximum": 20
                  }
                },
                "required": [],
                "additionalProperties": false
              },
              "list": {
                "enum": [
                  "bullet",
                  "number"
                ]
              }
            },
            "required": [
              "type"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "heading"
              },
              "level": {
                "type": "integer",
                "minimum": 1,
                "maximum": 3
              },
              "text": {
                "type": "string",
                "maxLength": 100000,
                "minLength": 1
              }
            },
            "required": [
              "type",
              "level",
              "text"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "table"
              },
              "rows": {
                "type": "array",
                "minItems": 1,
                "maxItems": 50,
                "items": {
                  "type": "array",
                  "minItems": 1,
                  "maxItems": 50,
                  "items": {
                    "anyOf": [
                      {
                        "type": "string",
                        "maxLength": 100000
                      },
                      {
                        "type": "object",
                        "properties": {
                          "text": {
                            "type": "string",
                            "maxLength": 100000
                          },
                          "runs": {
                            "type": "array",
                            "minItems": 1,
                            "maxItems": 1000,
                            "items": {
                              "type": "object",
                              "properties": {
                                "text": {
                                  "type": "string",
                                  "maxLength": 100000
                                },
                                "bold": {
                                  "type": "boolean"
                                },
                                "italic": {
                                  "type": "boolean"
                                },
                                "underline": {
                                  "type": "boolean"
                                },
                                "color": {
                                  "type": "string",
                                  "pattern": "^#?[0-9A-Fa-f]{6}$"
                                },
                                "highlight": {
                                  "type": "string",
                                  "maxLength": 40,
                                  "minLength": 1
                                },
                                "fontSizePt": {
                                  "type": "number",
                                  "minimum": 1,
                                  "maximum": 1638
                                },
                                "fontFamily": {
                                  "type": "string",
                                  "maxLength": 100,
                                  "minLength": 1
                                }
                              },
                              "required": [
                                "text"
                              ],
                              "additionalProperties": false
                            }
                          },
                          "fill": {
                            "type": "string",
                            "pattern": "^#?[0-9A-Fa-f]{6}$"
                          },
                          "align": {
                            "enum": [
                              "left",
                              "center",
                              "right"
                            ]
                          },
                          "verticalAlign": {
                            "enum": [
                              "top",
                              "center",
                              "bottom"
                            ]
                          }
                        },
                        "required": [],
                        "additionalProperties": false
                      }
                    ]
                  }
                }
              },
              "headerRows": {
                "type": "integer",
                "minimum": 0,
                "maximum": 50
              },
              "headerFill": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "headerTextColor": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "columnWidths": {
                "type": "array",
                "minItems": 1,
                "maxItems": 50,
                "items": {
                  "type": "number",
                  "minimum": 1,
                  "maximum": 5000
                }
              }
            },
            "required": [
              "type",
              "rows"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "equation"
              },
              "mathText": {
                "type": "string"
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              }
            },
            "required": [
              "type",
              "mathText"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "chart"
              },
              "chart": {
                "type": "object",
                "properties": {
                  "type": {
                    "enum": [
                      "column",
                      "bar",
                      "line",
                      "pie",
                      "doughnut",
                      "area",
                      "scatter"
                    ]
                  },
                  "title": {
                    "type": "string",
                    "maxLength": 200
                  },
                  "categories": {
                    "type": "array",
                    "minItems": 1,
                    "maxItems": 100,
                    "items": {
                      "type": "string",
                      "maxLength": 200
                    }
                  },
                  "series": {
                    "type": "array",
                    "minItems": 1,
                    "maxItems": 24,
                    "items": {
                      "type": "object",
                      "properties": {
                        "name": {
                          "type": "string",
                          "maxLength": 200
                        },
                        "values": {
                          "type": "array",
                          "maxItems": 100,
                          "items": {
                            "type": "number"
                          }
                        }
                      },
                      "required": [
                        "name",
                        "values"
                      ],
                      "additionalProperties": false
                    }
                  },
                  "grouping": {
                    "enum": [
                      "clustered",
                      "stacked",
                      "percentStacked"
                    ]
                  }
                },
                "required": [
                  "type",
                  "categories",
                  "series"
                ],
                "additionalProperties": false
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              }
            },
            "required": [
              "type",
              "chart"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "smartArt"
              },
              "smartArt": {
                "type": "object",
                "properties": {
                  "layout": {
                    "enum": [
                      "process",
                      "cycle",
                      "hierarchy",
                      "list"
                    ]
                  },
                  "items": {
                    "type": "array",
                    "minItems": 1,
                    "maxItems": 50,
                    "items": {
                      "type": "string",
                      "maxLength": 500
                    }
                  }
                },
                "required": [
                  "layout",
                  "items"
                ],
                "additionalProperties": false
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              }
            },
            "required": [
              "type",
              "smartArt"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "image"
              },
              "assetRef": {
                "type": "string",
                "pattern": "^asset:.+$"
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "alt": {
                "type": "string",
                "maxLength": 1000
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              },
              "wrap": {
                "enum": [
                  "inline",
                  "square",
                  "topAndBottom",
                  "none",
                  "behind"
                ]
              },
              "position": {
                "type": "object",
                "properties": {
                  "xPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  },
                  "yPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  }
                },
                "required": [
                  "xPx",
                  "yPx"
                ],
                "additionalProperties": false
              }
            },
            "required": [
              "type",
              "assetRef",
              "widthPx",
              "heightPx"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "shape"
              },
              "preset": {
                "enum": [
                  "line",
                  "verticalLine",
                  "rectangle",
                  "roundedRectangle",
                  "ellipse",
                  "diamond",
                  "textBox",
                  "accentBorderCallout1",
                  "accentBorderCallout2",
                  "accentBorderCallout3",
                  "accentCallout1",
                  "accentCallout2",
                  "accentCallout3",
                  "arc",
                  "bentArrow",
                  "bentUpArrow",
                  "bevel",
                  "blockArc",
                  "borderCallout1",
                  "borderCallout2",
                  "borderCallout3",
                  "bracePair",
                  "bracketPair",
                  "callout1",
                  "callout2",
                  "callout3",
                  "can",
                  "chartPlus",
                  "chartStar",
                  "chartX",
                  "chevron",
                  "chord",
                  "circularArrow",
                  "cloud",
                  "cloudCallout",
                  "corner",
                  "cornerTabs",
                  "cube",
                  "curvedDownArrow",
                  "curvedLeftArrow",
                  "curvedRightArrow",
                  "curvedUpArrow",
                  "decagon",
                  "diagStripe",
                  "dodecagon",
                  "donut",
                  "doubleWave",
                  "downArrow",
                  "downArrowCallout",
                  "ellipseRibbon",
                  "ellipseRibbon2",
                  "flowChartAlternateProcess",
                  "flowChartCollate",
                  "flowChartConnector",
                  "flowChartDecision",
                  "flowChartDelay",
                  "flowChartDisplay",
                  "flowChartDocument",
                  "flowChartExtract",
                  "flowChartInputOutput",
                  "flowChartInternalStorage",
                  "flowChartMagneticDisk",
                  "flowChartMagneticDrum",
                  "flowChartMagneticTape",
                  "flowChartManualInput",
                  "flowChartManualOperation",
                  "flowChartMerge",
                  "flowChartMultidocument",
                  "flowChartOfflineStorage",
                  "flowChartOffpageConnector",
                  "flowChartOnlineStorage",
                  "flowChartOr",
                  "flowChartPredefinedProcess",
                  "flowChartPreparation",
                  "flowChartProcess",
                  "flowChartPunchedCard",
                  "flowChartPunchedTape",
                  "flowChartSort",
                  "flowChartSummingJunction",
                  "flowChartTerminator",
                  "foldedCorner",
                  "frame",
                  "funnel",
                  "gear6",
                  "gear9",
                  "halfFrame",
                  "heart",
                  "heptagon",
                  "hexagon",
                  "homePlate",
                  "horizontalScroll",
                  "irregularSeal1",
                  "irregularSeal2",
                  "leftArrow",
                  "leftArrowCallout",
                  "leftBrace",
                  "leftBracket",
                  "leftCircularArrow",
                  "leftRightArrow",
                  "leftRightArrowCallout",
                  "leftRightCircularArrow",
                  "leftRightRibbon",
                  "leftRightUpArrow",
                  "leftUpArrow",
                  "lightningBolt",
                  "lineInv",
                  "mathDivide",
                  "mathEqual",
                  "mathMinus",
                  "mathMultiply",
                  "mathNotEqual",
                  "mathPlus",
                  "moon",
                  "nonIsoscelesTrapezoid",
                  "noSmoking",
                  "notchedRightArrow",
                  "octagon",
                  "parallelogram",
                  "pentagon",
                  "pie",
                  "pieWedge",
                  "plaque",
                  "plaqueTabs",
                  "plus",
                  "quadArrow",
                  "quadArrowCallout",
                  "rect",
                  "ribbon",
                  "ribbon2",
                  "rightArrow",
                  "rightArrowCallout",
                  "rightBrace",
                  "rightBracket",
                  "round1Rect",
                  "round2DiagRect",
                  "round2SameRect",
                  "roundRect",
                  "rtTriangle",
                  "smileyFace",
                  "snip1Rect",
                  "snip2DiagRect",
                  "snip2SameRect",
                  "snipRoundRect",
                  "squareTabs",
                  "star10",
                  "star12",
                  "star16",
                  "star24",
                  "star32",
                  "star4",
                  "star5",
                  "star6",
                  "star7",
                  "star8",
                  "stripedRightArrow",
                  "sun",
                  "swooshArrow",
                  "teardrop",
                  "trapezoid",
                  "triangle",
                  "upArrowCallout",
                  "upDownArrow",
                  "upArrow",
                  "upDownArrowCallout",
                  "uturnArrow",
                  "verticalScroll",
                  "wave",
                  "wedgeEllipseCallout",
                  "wedgeRectCallout",
                  "wedgeRoundRectCallout"
                ]
              },
              "text": {
                "type": "string",
                "maxLength": 20000
              },
              "textStyle": {
                "type": "object",
                "properties": {
                  "bold": {
                    "type": "boolean"
                  },
                  "italic": {
                    "type": "boolean"
                  },
                  "underline": {
                    "type": "boolean"
                  },
                  "color": {
                    "type": "string",
                    "pattern": "^#?[0-9A-Fa-f]{6}$"
                  },
                  "fontSizePt": {
                    "type": "number",
                    "minimum": 1,
                    "maximum": 1638
                  },
                  "fontFamily": {
                    "type": "string",
                    "maxLength": 100,
                    "minLength": 1
                  },
                  "align": {
                    "enum": [
                      "left",
                      "center",
                      "right"
                    ]
                  }
                },
                "required": [],
                "additionalProperties": false
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "position": {
                "type": "object",
                "properties": {
                  "xPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  },
                  "yPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  }
                },
                "required": [
                  "xPx",
                  "yPx"
                ],
                "additionalProperties": false
              },
              "fill": {
                "anyOf": [
                  {
                    "type": "string",
                    "pattern": "^#?[0-9A-Fa-f]{6}$"
                  },
                  {
                    "type": "null"
                  }
                ]
              },
              "line": {
                "anyOf": [
                  {
                    "type": "null"
                  },
                  {
                    "type": "object",
                    "properties": {
                      "color": {
                        "type": "string",
                        "pattern": "^#?[0-9A-Fa-f]{6}$"
                      },
                      "widthPx": {
                        "type": "number",
                        "minimum": 1,
                        "maximum": 5000
                      },
                      "dash": {
                        "enum": [
                          "solid",
                          "dashed",
                          "dotted"
                        ]
                      }
                    },
                    "required": [
                      "color",
                      "widthPx",
                      "dash"
                    ],
                    "additionalProperties": false
                  }
                ]
              },
              "wrap": {
                "enum": [
                  "inline",
                  "square",
                  "topAndBottom",
                  "none",
                  "behind"
                ]
              },
              "order": {
                "enum": [
                  "front",
                  "back"
                ]
              }
            },
            "required": [
              "type",
              "preset"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "wordArt"
              },
              "text": {
                "type": "string",
                "minLength": 1,
                "maxLength": 500
              },
              "preset": {
                "enum": [
                  "plain",
                  "archUp",
                  "archDown",
                  "wave",
                  "chevron",
                  "circle",
                  "button",
                  "chevronDown"
                ]
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "position": {
                "type": "object",
                "properties": {
                  "xPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  },
                  "yPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  }
                },
                "required": [
                  "xPx",
                  "yPx"
                ],
                "additionalProperties": false
              },
              "rotation": {
                "type": "number"
              },
              "fill": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "opacity": {
                "type": "number",
                "minimum": 0,
                "maximum": 1
              },
              "wrap": {
                "enum": [
                  "inline",
                  "square",
                  "topAndBottom",
                  "none",
                  "behind"
                ]
              },
              "order": {
                "enum": [
                  "front",
                  "back"
                ]
              }
            },
            "required": [
              "type",
              "text",
              "preset"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "pageNumber"
              },
              "fieldKind": {
                "enum": [
                  "page",
                  "pageOfTotal"
                ]
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              },
              "color": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "fontSizePt": {
                "type": "number",
                "minimum": 1,
                "maximum": 1638
              },
              "fontFamily": {
                "type": "string",
                "maxLength": 100,
                "minLength": 1
              },
              "bold": {
                "type": "boolean"
              }
            },
            "required": [
              "type",
              "fieldKind"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "pageBreak"
              }
            },
            "required": [
              "type"
            ],
            "additionalProperties": false
          }
        ]
      }
    },
    "footer": {
      "type": "array",
      "minItems": 1,
      "maxItems": 100,
      "items": {
        "anyOf": [
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "paragraph"
              },
              "text": {
                "type": "string",
                "maxLength": 100000
              },
              "runs": {
                "type": "array",
                "minItems": 1,
                "maxItems": 1000,
                "items": {
                  "type": "object",
                  "properties": {
                    "text": {
                      "type": "string",
                      "maxLength": 100000
                    },
                    "bold": {
                      "type": "boolean"
                    },
                    "italic": {
                      "type": "boolean"
                    },
                    "underline": {
                      "type": "boolean"
                    },
                    "color": {
                      "type": "string",
                      "pattern": "^#?[0-9A-Fa-f]{6}$"
                    },
                    "highlight": {
                      "type": "string",
                      "maxLength": 40,
                      "minLength": 1
                    },
                    "fontSizePt": {
                      "type": "number",
                      "minimum": 1,
                      "maximum": 1638
                    },
                    "fontFamily": {
                      "type": "string",
                      "maxLength": 100,
                      "minLength": 1
                    }
                  },
                  "required": [
                    "text"
                  ],
                  "additionalProperties": false
                }
              },
              "styleId": {
                "enum": [
                  "Normal",
                  "Title",
                  "Subtitle",
                  "Heading1",
                  "Heading2",
                  "Heading3"
                ]
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right",
                  "both"
                ]
              },
              "spacing": {
                "type": "object",
                "properties": {
                  "beforePt": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 2000
                  },
                  "afterPt": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 2000
                  },
                  "lineMultiple": {
                    "type": "number",
                    "minimum": 0.1,
                    "maximum": 20
                  }
                },
                "required": [],
                "additionalProperties": false
              },
              "list": {
                "enum": [
                  "bullet",
                  "number"
                ]
              }
            },
            "required": [
              "type"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "heading"
              },
              "level": {
                "type": "integer",
                "minimum": 1,
                "maximum": 3
              },
              "text": {
                "type": "string",
                "maxLength": 100000,
                "minLength": 1
              }
            },
            "required": [
              "type",
              "level",
              "text"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "table"
              },
              "rows": {
                "type": "array",
                "minItems": 1,
                "maxItems": 50,
                "items": {
                  "type": "array",
                  "minItems": 1,
                  "maxItems": 50,
                  "items": {
                    "anyOf": [
                      {
                        "type": "string",
                        "maxLength": 100000
                      },
                      {
                        "type": "object",
                        "properties": {
                          "text": {
                            "type": "string",
                            "maxLength": 100000
                          },
                          "runs": {
                            "type": "array",
                            "minItems": 1,
                            "maxItems": 1000,
                            "items": {
                              "type": "object",
                              "properties": {
                                "text": {
                                  "type": "string",
                                  "maxLength": 100000
                                },
                                "bold": {
                                  "type": "boolean"
                                },
                                "italic": {
                                  "type": "boolean"
                                },
                                "underline": {
                                  "type": "boolean"
                                },
                                "color": {
                                  "type": "string",
                                  "pattern": "^#?[0-9A-Fa-f]{6}$"
                                },
                                "highlight": {
                                  "type": "string",
                                  "maxLength": 40,
                                  "minLength": 1
                                },
                                "fontSizePt": {
                                  "type": "number",
                                  "minimum": 1,
                                  "maximum": 1638
                                },
                                "fontFamily": {
                                  "type": "string",
                                  "maxLength": 100,
                                  "minLength": 1
                                }
                              },
                              "required": [
                                "text"
                              ],
                              "additionalProperties": false
                            }
                          },
                          "fill": {
                            "type": "string",
                            "pattern": "^#?[0-9A-Fa-f]{6}$"
                          },
                          "align": {
                            "enum": [
                              "left",
                              "center",
                              "right"
                            ]
                          },
                          "verticalAlign": {
                            "enum": [
                              "top",
                              "center",
                              "bottom"
                            ]
                          }
                        },
                        "required": [],
                        "additionalProperties": false
                      }
                    ]
                  }
                }
              },
              "headerRows": {
                "type": "integer",
                "minimum": 0,
                "maximum": 50
              },
              "headerFill": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "headerTextColor": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "columnWidths": {
                "type": "array",
                "minItems": 1,
                "maxItems": 50,
                "items": {
                  "type": "number",
                  "minimum": 1,
                  "maximum": 5000
                }
              }
            },
            "required": [
              "type",
              "rows"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "equation"
              },
              "mathText": {
                "type": "string"
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              }
            },
            "required": [
              "type",
              "mathText"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "chart"
              },
              "chart": {
                "type": "object",
                "properties": {
                  "type": {
                    "enum": [
                      "column",
                      "bar",
                      "line",
                      "pie",
                      "doughnut",
                      "area",
                      "scatter"
                    ]
                  },
                  "title": {
                    "type": "string",
                    "maxLength": 200
                  },
                  "categories": {
                    "type": "array",
                    "minItems": 1,
                    "maxItems": 100,
                    "items": {
                      "type": "string",
                      "maxLength": 200
                    }
                  },
                  "series": {
                    "type": "array",
                    "minItems": 1,
                    "maxItems": 24,
                    "items": {
                      "type": "object",
                      "properties": {
                        "name": {
                          "type": "string",
                          "maxLength": 200
                        },
                        "values": {
                          "type": "array",
                          "maxItems": 100,
                          "items": {
                            "type": "number"
                          }
                        }
                      },
                      "required": [
                        "name",
                        "values"
                      ],
                      "additionalProperties": false
                    }
                  },
                  "grouping": {
                    "enum": [
                      "clustered",
                      "stacked",
                      "percentStacked"
                    ]
                  }
                },
                "required": [
                  "type",
                  "categories",
                  "series"
                ],
                "additionalProperties": false
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              }
            },
            "required": [
              "type",
              "chart"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "smartArt"
              },
              "smartArt": {
                "type": "object",
                "properties": {
                  "layout": {
                    "enum": [
                      "process",
                      "cycle",
                      "hierarchy",
                      "list"
                    ]
                  },
                  "items": {
                    "type": "array",
                    "minItems": 1,
                    "maxItems": 50,
                    "items": {
                      "type": "string",
                      "maxLength": 500
                    }
                  }
                },
                "required": [
                  "layout",
                  "items"
                ],
                "additionalProperties": false
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              }
            },
            "required": [
              "type",
              "smartArt"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "image"
              },
              "assetRef": {
                "type": "string",
                "pattern": "^asset:.+$"
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "alt": {
                "type": "string",
                "maxLength": 1000
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              },
              "wrap": {
                "enum": [
                  "inline",
                  "square",
                  "topAndBottom",
                  "none",
                  "behind"
                ]
              },
              "position": {
                "type": "object",
                "properties": {
                  "xPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  },
                  "yPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  }
                },
                "required": [
                  "xPx",
                  "yPx"
                ],
                "additionalProperties": false
              }
            },
            "required": [
              "type",
              "assetRef",
              "widthPx",
              "heightPx"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "shape"
              },
              "preset": {
                "enum": [
                  "line",
                  "verticalLine",
                  "rectangle",
                  "roundedRectangle",
                  "ellipse",
                  "diamond",
                  "textBox",
                  "accentBorderCallout1",
                  "accentBorderCallout2",
                  "accentBorderCallout3",
                  "accentCallout1",
                  "accentCallout2",
                  "accentCallout3",
                  "arc",
                  "bentArrow",
                  "bentUpArrow",
                  "bevel",
                  "blockArc",
                  "borderCallout1",
                  "borderCallout2",
                  "borderCallout3",
                  "bracePair",
                  "bracketPair",
                  "callout1",
                  "callout2",
                  "callout3",
                  "can",
                  "chartPlus",
                  "chartStar",
                  "chartX",
                  "chevron",
                  "chord",
                  "circularArrow",
                  "cloud",
                  "cloudCallout",
                  "corner",
                  "cornerTabs",
                  "cube",
                  "curvedDownArrow",
                  "curvedLeftArrow",
                  "curvedRightArrow",
                  "curvedUpArrow",
                  "decagon",
                  "diagStripe",
                  "dodecagon",
                  "donut",
                  "doubleWave",
                  "downArrow",
                  "downArrowCallout",
                  "ellipseRibbon",
                  "ellipseRibbon2",
                  "flowChartAlternateProcess",
                  "flowChartCollate",
                  "flowChartConnector",
                  "flowChartDecision",
                  "flowChartDelay",
                  "flowChartDisplay",
                  "flowChartDocument",
                  "flowChartExtract",
                  "flowChartInputOutput",
                  "flowChartInternalStorage",
                  "flowChartMagneticDisk",
                  "flowChartMagneticDrum",
                  "flowChartMagneticTape",
                  "flowChartManualInput",
                  "flowChartManualOperation",
                  "flowChartMerge",
                  "flowChartMultidocument",
                  "flowChartOfflineStorage",
                  "flowChartOffpageConnector",
                  "flowChartOnlineStorage",
                  "flowChartOr",
                  "flowChartPredefinedProcess",
                  "flowChartPreparation",
                  "flowChartProcess",
                  "flowChartPunchedCard",
                  "flowChartPunchedTape",
                  "flowChartSort",
                  "flowChartSummingJunction",
                  "flowChartTerminator",
                  "foldedCorner",
                  "frame",
                  "funnel",
                  "gear6",
                  "gear9",
                  "halfFrame",
                  "heart",
                  "heptagon",
                  "hexagon",
                  "homePlate",
                  "horizontalScroll",
                  "irregularSeal1",
                  "irregularSeal2",
                  "leftArrow",
                  "leftArrowCallout",
                  "leftBrace",
                  "leftBracket",
                  "leftCircularArrow",
                  "leftRightArrow",
                  "leftRightArrowCallout",
                  "leftRightCircularArrow",
                  "leftRightRibbon",
                  "leftRightUpArrow",
                  "leftUpArrow",
                  "lightningBolt",
                  "lineInv",
                  "mathDivide",
                  "mathEqual",
                  "mathMinus",
                  "mathMultiply",
                  "mathNotEqual",
                  "mathPlus",
                  "moon",
                  "nonIsoscelesTrapezoid",
                  "noSmoking",
                  "notchedRightArrow",
                  "octagon",
                  "parallelogram",
                  "pentagon",
                  "pie",
                  "pieWedge",
                  "plaque",
                  "plaqueTabs",
                  "plus",
                  "quadArrow",
                  "quadArrowCallout",
                  "rect",
                  "ribbon",
                  "ribbon2",
                  "rightArrow",
                  "rightArrowCallout",
                  "rightBrace",
                  "rightBracket",
                  "round1Rect",
                  "round2DiagRect",
                  "round2SameRect",
                  "roundRect",
                  "rtTriangle",
                  "smileyFace",
                  "snip1Rect",
                  "snip2DiagRect",
                  "snip2SameRect",
                  "snipRoundRect",
                  "squareTabs",
                  "star10",
                  "star12",
                  "star16",
                  "star24",
                  "star32",
                  "star4",
                  "star5",
                  "star6",
                  "star7",
                  "star8",
                  "stripedRightArrow",
                  "sun",
                  "swooshArrow",
                  "teardrop",
                  "trapezoid",
                  "triangle",
                  "upArrowCallout",
                  "upDownArrow",
                  "upArrow",
                  "upDownArrowCallout",
                  "uturnArrow",
                  "verticalScroll",
                  "wave",
                  "wedgeEllipseCallout",
                  "wedgeRectCallout",
                  "wedgeRoundRectCallout"
                ]
              },
              "text": {
                "type": "string",
                "maxLength": 20000
              },
              "textStyle": {
                "type": "object",
                "properties": {
                  "bold": {
                    "type": "boolean"
                  },
                  "italic": {
                    "type": "boolean"
                  },
                  "underline": {
                    "type": "boolean"
                  },
                  "color": {
                    "type": "string",
                    "pattern": "^#?[0-9A-Fa-f]{6}$"
                  },
                  "fontSizePt": {
                    "type": "number",
                    "minimum": 1,
                    "maximum": 1638
                  },
                  "fontFamily": {
                    "type": "string",
                    "maxLength": 100,
                    "minLength": 1
                  },
                  "align": {
                    "enum": [
                      "left",
                      "center",
                      "right"
                    ]
                  }
                },
                "required": [],
                "additionalProperties": false
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "position": {
                "type": "object",
                "properties": {
                  "xPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  },
                  "yPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  }
                },
                "required": [
                  "xPx",
                  "yPx"
                ],
                "additionalProperties": false
              },
              "fill": {
                "anyOf": [
                  {
                    "type": "string",
                    "pattern": "^#?[0-9A-Fa-f]{6}$"
                  },
                  {
                    "type": "null"
                  }
                ]
              },
              "line": {
                "anyOf": [
                  {
                    "type": "null"
                  },
                  {
                    "type": "object",
                    "properties": {
                      "color": {
                        "type": "string",
                        "pattern": "^#?[0-9A-Fa-f]{6}$"
                      },
                      "widthPx": {
                        "type": "number",
                        "minimum": 1,
                        "maximum": 5000
                      },
                      "dash": {
                        "enum": [
                          "solid",
                          "dashed",
                          "dotted"
                        ]
                      }
                    },
                    "required": [
                      "color",
                      "widthPx",
                      "dash"
                    ],
                    "additionalProperties": false
                  }
                ]
              },
              "wrap": {
                "enum": [
                  "inline",
                  "square",
                  "topAndBottom",
                  "none",
                  "behind"
                ]
              },
              "order": {
                "enum": [
                  "front",
                  "back"
                ]
              }
            },
            "required": [
              "type",
              "preset"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "wordArt"
              },
              "text": {
                "type": "string",
                "minLength": 1,
                "maxLength": 500
              },
              "preset": {
                "enum": [
                  "plain",
                  "archUp",
                  "archDown",
                  "wave",
                  "chevron",
                  "circle",
                  "button",
                  "chevronDown"
                ]
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "position": {
                "type": "object",
                "properties": {
                  "xPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  },
                  "yPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  }
                },
                "required": [
                  "xPx",
                  "yPx"
                ],
                "additionalProperties": false
              },
              "rotation": {
                "type": "number"
              },
              "fill": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "opacity": {
                "type": "number",
                "minimum": 0,
                "maximum": 1
              },
              "wrap": {
                "enum": [
                  "inline",
                  "square",
                  "topAndBottom",
                  "none",
                  "behind"
                ]
              },
              "order": {
                "enum": [
                  "front",
                  "back"
                ]
              }
            },
            "required": [
              "type",
              "text",
              "preset"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "pageNumber"
              },
              "fieldKind": {
                "enum": [
                  "page",
                  "pageOfTotal"
                ]
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              },
              "color": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "fontSizePt": {
                "type": "number",
                "minimum": 1,
                "maximum": 1638
              },
              "fontFamily": {
                "type": "string",
                "maxLength": 100,
                "minLength": 1
              },
              "bold": {
                "type": "boolean"
              }
            },
            "required": [
              "type",
              "fieldKind"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "pageBreak"
              }
            },
            "required": [
              "type"
            ],
            "additionalProperties": false
          }
        ]
      }
    },
    "firstHeader": {
      "type": "array",
      "minItems": 1,
      "maxItems": 100,
      "items": {
        "anyOf": [
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "paragraph"
              },
              "text": {
                "type": "string",
                "maxLength": 100000
              },
              "runs": {
                "type": "array",
                "minItems": 1,
                "maxItems": 1000,
                "items": {
                  "type": "object",
                  "properties": {
                    "text": {
                      "type": "string",
                      "maxLength": 100000
                    },
                    "bold": {
                      "type": "boolean"
                    },
                    "italic": {
                      "type": "boolean"
                    },
                    "underline": {
                      "type": "boolean"
                    },
                    "color": {
                      "type": "string",
                      "pattern": "^#?[0-9A-Fa-f]{6}$"
                    },
                    "highlight": {
                      "type": "string",
                      "maxLength": 40,
                      "minLength": 1
                    },
                    "fontSizePt": {
                      "type": "number",
                      "minimum": 1,
                      "maximum": 1638
                    },
                    "fontFamily": {
                      "type": "string",
                      "maxLength": 100,
                      "minLength": 1
                    }
                  },
                  "required": [
                    "text"
                  ],
                  "additionalProperties": false
                }
              },
              "styleId": {
                "enum": [
                  "Normal",
                  "Title",
                  "Subtitle",
                  "Heading1",
                  "Heading2",
                  "Heading3"
                ]
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right",
                  "both"
                ]
              },
              "spacing": {
                "type": "object",
                "properties": {
                  "beforePt": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 2000
                  },
                  "afterPt": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 2000
                  },
                  "lineMultiple": {
                    "type": "number",
                    "minimum": 0.1,
                    "maximum": 20
                  }
                },
                "required": [],
                "additionalProperties": false
              },
              "list": {
                "enum": [
                  "bullet",
                  "number"
                ]
              }
            },
            "required": [
              "type"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "heading"
              },
              "level": {
                "type": "integer",
                "minimum": 1,
                "maximum": 3
              },
              "text": {
                "type": "string",
                "maxLength": 100000,
                "minLength": 1
              }
            },
            "required": [
              "type",
              "level",
              "text"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "table"
              },
              "rows": {
                "type": "array",
                "minItems": 1,
                "maxItems": 50,
                "items": {
                  "type": "array",
                  "minItems": 1,
                  "maxItems": 50,
                  "items": {
                    "anyOf": [
                      {
                        "type": "string",
                        "maxLength": 100000
                      },
                      {
                        "type": "object",
                        "properties": {
                          "text": {
                            "type": "string",
                            "maxLength": 100000
                          },
                          "runs": {
                            "type": "array",
                            "minItems": 1,
                            "maxItems": 1000,
                            "items": {
                              "type": "object",
                              "properties": {
                                "text": {
                                  "type": "string",
                                  "maxLength": 100000
                                },
                                "bold": {
                                  "type": "boolean"
                                },
                                "italic": {
                                  "type": "boolean"
                                },
                                "underline": {
                                  "type": "boolean"
                                },
                                "color": {
                                  "type": "string",
                                  "pattern": "^#?[0-9A-Fa-f]{6}$"
                                },
                                "highlight": {
                                  "type": "string",
                                  "maxLength": 40,
                                  "minLength": 1
                                },
                                "fontSizePt": {
                                  "type": "number",
                                  "minimum": 1,
                                  "maximum": 1638
                                },
                                "fontFamily": {
                                  "type": "string",
                                  "maxLength": 100,
                                  "minLength": 1
                                }
                              },
                              "required": [
                                "text"
                              ],
                              "additionalProperties": false
                            }
                          },
                          "fill": {
                            "type": "string",
                            "pattern": "^#?[0-9A-Fa-f]{6}$"
                          },
                          "align": {
                            "enum": [
                              "left",
                              "center",
                              "right"
                            ]
                          },
                          "verticalAlign": {
                            "enum": [
                              "top",
                              "center",
                              "bottom"
                            ]
                          }
                        },
                        "required": [],
                        "additionalProperties": false
                      }
                    ]
                  }
                }
              },
              "headerRows": {
                "type": "integer",
                "minimum": 0,
                "maximum": 50
              },
              "headerFill": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "headerTextColor": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "columnWidths": {
                "type": "array",
                "minItems": 1,
                "maxItems": 50,
                "items": {
                  "type": "number",
                  "minimum": 1,
                  "maximum": 5000
                }
              }
            },
            "required": [
              "type",
              "rows"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "equation"
              },
              "mathText": {
                "type": "string"
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              }
            },
            "required": [
              "type",
              "mathText"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "chart"
              },
              "chart": {
                "type": "object",
                "properties": {
                  "type": {
                    "enum": [
                      "column",
                      "bar",
                      "line",
                      "pie",
                      "doughnut",
                      "area",
                      "scatter"
                    ]
                  },
                  "title": {
                    "type": "string",
                    "maxLength": 200
                  },
                  "categories": {
                    "type": "array",
                    "minItems": 1,
                    "maxItems": 100,
                    "items": {
                      "type": "string",
                      "maxLength": 200
                    }
                  },
                  "series": {
                    "type": "array",
                    "minItems": 1,
                    "maxItems": 24,
                    "items": {
                      "type": "object",
                      "properties": {
                        "name": {
                          "type": "string",
                          "maxLength": 200
                        },
                        "values": {
                          "type": "array",
                          "maxItems": 100,
                          "items": {
                            "type": "number"
                          }
                        }
                      },
                      "required": [
                        "name",
                        "values"
                      ],
                      "additionalProperties": false
                    }
                  },
                  "grouping": {
                    "enum": [
                      "clustered",
                      "stacked",
                      "percentStacked"
                    ]
                  }
                },
                "required": [
                  "type",
                  "categories",
                  "series"
                ],
                "additionalProperties": false
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              }
            },
            "required": [
              "type",
              "chart"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "smartArt"
              },
              "smartArt": {
                "type": "object",
                "properties": {
                  "layout": {
                    "enum": [
                      "process",
                      "cycle",
                      "hierarchy",
                      "list"
                    ]
                  },
                  "items": {
                    "type": "array",
                    "minItems": 1,
                    "maxItems": 50,
                    "items": {
                      "type": "string",
                      "maxLength": 500
                    }
                  }
                },
                "required": [
                  "layout",
                  "items"
                ],
                "additionalProperties": false
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              }
            },
            "required": [
              "type",
              "smartArt"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "image"
              },
              "assetRef": {
                "type": "string",
                "pattern": "^asset:.+$"
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "alt": {
                "type": "string",
                "maxLength": 1000
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              },
              "wrap": {
                "enum": [
                  "inline",
                  "square",
                  "topAndBottom",
                  "none",
                  "behind"
                ]
              },
              "position": {
                "type": "object",
                "properties": {
                  "xPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  },
                  "yPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  }
                },
                "required": [
                  "xPx",
                  "yPx"
                ],
                "additionalProperties": false
              }
            },
            "required": [
              "type",
              "assetRef",
              "widthPx",
              "heightPx"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "shape"
              },
              "preset": {
                "enum": [
                  "line",
                  "verticalLine",
                  "rectangle",
                  "roundedRectangle",
                  "ellipse",
                  "diamond",
                  "textBox",
                  "accentBorderCallout1",
                  "accentBorderCallout2",
                  "accentBorderCallout3",
                  "accentCallout1",
                  "accentCallout2",
                  "accentCallout3",
                  "arc",
                  "bentArrow",
                  "bentUpArrow",
                  "bevel",
                  "blockArc",
                  "borderCallout1",
                  "borderCallout2",
                  "borderCallout3",
                  "bracePair",
                  "bracketPair",
                  "callout1",
                  "callout2",
                  "callout3",
                  "can",
                  "chartPlus",
                  "chartStar",
                  "chartX",
                  "chevron",
                  "chord",
                  "circularArrow",
                  "cloud",
                  "cloudCallout",
                  "corner",
                  "cornerTabs",
                  "cube",
                  "curvedDownArrow",
                  "curvedLeftArrow",
                  "curvedRightArrow",
                  "curvedUpArrow",
                  "decagon",
                  "diagStripe",
                  "dodecagon",
                  "donut",
                  "doubleWave",
                  "downArrow",
                  "downArrowCallout",
                  "ellipseRibbon",
                  "ellipseRibbon2",
                  "flowChartAlternateProcess",
                  "flowChartCollate",
                  "flowChartConnector",
                  "flowChartDecision",
                  "flowChartDelay",
                  "flowChartDisplay",
                  "flowChartDocument",
                  "flowChartExtract",
                  "flowChartInputOutput",
                  "flowChartInternalStorage",
                  "flowChartMagneticDisk",
                  "flowChartMagneticDrum",
                  "flowChartMagneticTape",
                  "flowChartManualInput",
                  "flowChartManualOperation",
                  "flowChartMerge",
                  "flowChartMultidocument",
                  "flowChartOfflineStorage",
                  "flowChartOffpageConnector",
                  "flowChartOnlineStorage",
                  "flowChartOr",
                  "flowChartPredefinedProcess",
                  "flowChartPreparation",
                  "flowChartProcess",
                  "flowChartPunchedCard",
                  "flowChartPunchedTape",
                  "flowChartSort",
                  "flowChartSummingJunction",
                  "flowChartTerminator",
                  "foldedCorner",
                  "frame",
                  "funnel",
                  "gear6",
                  "gear9",
                  "halfFrame",
                  "heart",
                  "heptagon",
                  "hexagon",
                  "homePlate",
                  "horizontalScroll",
                  "irregularSeal1",
                  "irregularSeal2",
                  "leftArrow",
                  "leftArrowCallout",
                  "leftBrace",
                  "leftBracket",
                  "leftCircularArrow",
                  "leftRightArrow",
                  "leftRightArrowCallout",
                  "leftRightCircularArrow",
                  "leftRightRibbon",
                  "leftRightUpArrow",
                  "leftUpArrow",
                  "lightningBolt",
                  "lineInv",
                  "mathDivide",
                  "mathEqual",
                  "mathMinus",
                  "mathMultiply",
                  "mathNotEqual",
                  "mathPlus",
                  "moon",
                  "nonIsoscelesTrapezoid",
                  "noSmoking",
                  "notchedRightArrow",
                  "octagon",
                  "parallelogram",
                  "pentagon",
                  "pie",
                  "pieWedge",
                  "plaque",
                  "plaqueTabs",
                  "plus",
                  "quadArrow",
                  "quadArrowCallout",
                  "rect",
                  "ribbon",
                  "ribbon2",
                  "rightArrow",
                  "rightArrowCallout",
                  "rightBrace",
                  "rightBracket",
                  "round1Rect",
                  "round2DiagRect",
                  "round2SameRect",
                  "roundRect",
                  "rtTriangle",
                  "smileyFace",
                  "snip1Rect",
                  "snip2DiagRect",
                  "snip2SameRect",
                  "snipRoundRect",
                  "squareTabs",
                  "star10",
                  "star12",
                  "star16",
                  "star24",
                  "star32",
                  "star4",
                  "star5",
                  "star6",
                  "star7",
                  "star8",
                  "stripedRightArrow",
                  "sun",
                  "swooshArrow",
                  "teardrop",
                  "trapezoid",
                  "triangle",
                  "upArrowCallout",
                  "upDownArrow",
                  "upArrow",
                  "upDownArrowCallout",
                  "uturnArrow",
                  "verticalScroll",
                  "wave",
                  "wedgeEllipseCallout",
                  "wedgeRectCallout",
                  "wedgeRoundRectCallout"
                ]
              },
              "text": {
                "type": "string",
                "maxLength": 20000
              },
              "textStyle": {
                "type": "object",
                "properties": {
                  "bold": {
                    "type": "boolean"
                  },
                  "italic": {
                    "type": "boolean"
                  },
                  "underline": {
                    "type": "boolean"
                  },
                  "color": {
                    "type": "string",
                    "pattern": "^#?[0-9A-Fa-f]{6}$"
                  },
                  "fontSizePt": {
                    "type": "number",
                    "minimum": 1,
                    "maximum": 1638
                  },
                  "fontFamily": {
                    "type": "string",
                    "maxLength": 100,
                    "minLength": 1
                  },
                  "align": {
                    "enum": [
                      "left",
                      "center",
                      "right"
                    ]
                  }
                },
                "required": [],
                "additionalProperties": false
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "position": {
                "type": "object",
                "properties": {
                  "xPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  },
                  "yPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  }
                },
                "required": [
                  "xPx",
                  "yPx"
                ],
                "additionalProperties": false
              },
              "fill": {
                "anyOf": [
                  {
                    "type": "string",
                    "pattern": "^#?[0-9A-Fa-f]{6}$"
                  },
                  {
                    "type": "null"
                  }
                ]
              },
              "line": {
                "anyOf": [
                  {
                    "type": "null"
                  },
                  {
                    "type": "object",
                    "properties": {
                      "color": {
                        "type": "string",
                        "pattern": "^#?[0-9A-Fa-f]{6}$"
                      },
                      "widthPx": {
                        "type": "number",
                        "minimum": 1,
                        "maximum": 5000
                      },
                      "dash": {
                        "enum": [
                          "solid",
                          "dashed",
                          "dotted"
                        ]
                      }
                    },
                    "required": [
                      "color",
                      "widthPx",
                      "dash"
                    ],
                    "additionalProperties": false
                  }
                ]
              },
              "wrap": {
                "enum": [
                  "inline",
                  "square",
                  "topAndBottom",
                  "none",
                  "behind"
                ]
              },
              "order": {
                "enum": [
                  "front",
                  "back"
                ]
              }
            },
            "required": [
              "type",
              "preset"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "wordArt"
              },
              "text": {
                "type": "string",
                "minLength": 1,
                "maxLength": 500
              },
              "preset": {
                "enum": [
                  "plain",
                  "archUp",
                  "archDown",
                  "wave",
                  "chevron",
                  "circle",
                  "button",
                  "chevronDown"
                ]
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "position": {
                "type": "object",
                "properties": {
                  "xPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  },
                  "yPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  }
                },
                "required": [
                  "xPx",
                  "yPx"
                ],
                "additionalProperties": false
              },
              "rotation": {
                "type": "number"
              },
              "fill": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "opacity": {
                "type": "number",
                "minimum": 0,
                "maximum": 1
              },
              "wrap": {
                "enum": [
                  "inline",
                  "square",
                  "topAndBottom",
                  "none",
                  "behind"
                ]
              },
              "order": {
                "enum": [
                  "front",
                  "back"
                ]
              }
            },
            "required": [
              "type",
              "text",
              "preset"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "pageNumber"
              },
              "fieldKind": {
                "enum": [
                  "page",
                  "pageOfTotal"
                ]
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              },
              "color": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "fontSizePt": {
                "type": "number",
                "minimum": 1,
                "maximum": 1638
              },
              "fontFamily": {
                "type": "string",
                "maxLength": 100,
                "minLength": 1
              },
              "bold": {
                "type": "boolean"
              }
            },
            "required": [
              "type",
              "fieldKind"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "pageBreak"
              }
            },
            "required": [
              "type"
            ],
            "additionalProperties": false
          }
        ]
      }
    },
    "firstFooter": {
      "type": "array",
      "minItems": 1,
      "maxItems": 100,
      "items": {
        "anyOf": [
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "paragraph"
              },
              "text": {
                "type": "string",
                "maxLength": 100000
              },
              "runs": {
                "type": "array",
                "minItems": 1,
                "maxItems": 1000,
                "items": {
                  "type": "object",
                  "properties": {
                    "text": {
                      "type": "string",
                      "maxLength": 100000
                    },
                    "bold": {
                      "type": "boolean"
                    },
                    "italic": {
                      "type": "boolean"
                    },
                    "underline": {
                      "type": "boolean"
                    },
                    "color": {
                      "type": "string",
                      "pattern": "^#?[0-9A-Fa-f]{6}$"
                    },
                    "highlight": {
                      "type": "string",
                      "maxLength": 40,
                      "minLength": 1
                    },
                    "fontSizePt": {
                      "type": "number",
                      "minimum": 1,
                      "maximum": 1638
                    },
                    "fontFamily": {
                      "type": "string",
                      "maxLength": 100,
                      "minLength": 1
                    }
                  },
                  "required": [
                    "text"
                  ],
                  "additionalProperties": false
                }
              },
              "styleId": {
                "enum": [
                  "Normal",
                  "Title",
                  "Subtitle",
                  "Heading1",
                  "Heading2",
                  "Heading3"
                ]
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right",
                  "both"
                ]
              },
              "spacing": {
                "type": "object",
                "properties": {
                  "beforePt": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 2000
                  },
                  "afterPt": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 2000
                  },
                  "lineMultiple": {
                    "type": "number",
                    "minimum": 0.1,
                    "maximum": 20
                  }
                },
                "required": [],
                "additionalProperties": false
              },
              "list": {
                "enum": [
                  "bullet",
                  "number"
                ]
              }
            },
            "required": [
              "type"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "heading"
              },
              "level": {
                "type": "integer",
                "minimum": 1,
                "maximum": 3
              },
              "text": {
                "type": "string",
                "maxLength": 100000,
                "minLength": 1
              }
            },
            "required": [
              "type",
              "level",
              "text"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "table"
              },
              "rows": {
                "type": "array",
                "minItems": 1,
                "maxItems": 50,
                "items": {
                  "type": "array",
                  "minItems": 1,
                  "maxItems": 50,
                  "items": {
                    "anyOf": [
                      {
                        "type": "string",
                        "maxLength": 100000
                      },
                      {
                        "type": "object",
                        "properties": {
                          "text": {
                            "type": "string",
                            "maxLength": 100000
                          },
                          "runs": {
                            "type": "array",
                            "minItems": 1,
                            "maxItems": 1000,
                            "items": {
                              "type": "object",
                              "properties": {
                                "text": {
                                  "type": "string",
                                  "maxLength": 100000
                                },
                                "bold": {
                                  "type": "boolean"
                                },
                                "italic": {
                                  "type": "boolean"
                                },
                                "underline": {
                                  "type": "boolean"
                                },
                                "color": {
                                  "type": "string",
                                  "pattern": "^#?[0-9A-Fa-f]{6}$"
                                },
                                "highlight": {
                                  "type": "string",
                                  "maxLength": 40,
                                  "minLength": 1
                                },
                                "fontSizePt": {
                                  "type": "number",
                                  "minimum": 1,
                                  "maximum": 1638
                                },
                                "fontFamily": {
                                  "type": "string",
                                  "maxLength": 100,
                                  "minLength": 1
                                }
                              },
                              "required": [
                                "text"
                              ],
                              "additionalProperties": false
                            }
                          },
                          "fill": {
                            "type": "string",
                            "pattern": "^#?[0-9A-Fa-f]{6}$"
                          },
                          "align": {
                            "enum": [
                              "left",
                              "center",
                              "right"
                            ]
                          },
                          "verticalAlign": {
                            "enum": [
                              "top",
                              "center",
                              "bottom"
                            ]
                          }
                        },
                        "required": [],
                        "additionalProperties": false
                      }
                    ]
                  }
                }
              },
              "headerRows": {
                "type": "integer",
                "minimum": 0,
                "maximum": 50
              },
              "headerFill": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "headerTextColor": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "columnWidths": {
                "type": "array",
                "minItems": 1,
                "maxItems": 50,
                "items": {
                  "type": "number",
                  "minimum": 1,
                  "maximum": 5000
                }
              }
            },
            "required": [
              "type",
              "rows"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "equation"
              },
              "mathText": {
                "type": "string"
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              }
            },
            "required": [
              "type",
              "mathText"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "chart"
              },
              "chart": {
                "type": "object",
                "properties": {
                  "type": {
                    "enum": [
                      "column",
                      "bar",
                      "line",
                      "pie",
                      "doughnut",
                      "area",
                      "scatter"
                    ]
                  },
                  "title": {
                    "type": "string",
                    "maxLength": 200
                  },
                  "categories": {
                    "type": "array",
                    "minItems": 1,
                    "maxItems": 100,
                    "items": {
                      "type": "string",
                      "maxLength": 200
                    }
                  },
                  "series": {
                    "type": "array",
                    "minItems": 1,
                    "maxItems": 24,
                    "items": {
                      "type": "object",
                      "properties": {
                        "name": {
                          "type": "string",
                          "maxLength": 200
                        },
                        "values": {
                          "type": "array",
                          "maxItems": 100,
                          "items": {
                            "type": "number"
                          }
                        }
                      },
                      "required": [
                        "name",
                        "values"
                      ],
                      "additionalProperties": false
                    }
                  },
                  "grouping": {
                    "enum": [
                      "clustered",
                      "stacked",
                      "percentStacked"
                    ]
                  }
                },
                "required": [
                  "type",
                  "categories",
                  "series"
                ],
                "additionalProperties": false
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              }
            },
            "required": [
              "type",
              "chart"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "smartArt"
              },
              "smartArt": {
                "type": "object",
                "properties": {
                  "layout": {
                    "enum": [
                      "process",
                      "cycle",
                      "hierarchy",
                      "list"
                    ]
                  },
                  "items": {
                    "type": "array",
                    "minItems": 1,
                    "maxItems": 50,
                    "items": {
                      "type": "string",
                      "maxLength": 500
                    }
                  }
                },
                "required": [
                  "layout",
                  "items"
                ],
                "additionalProperties": false
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              }
            },
            "required": [
              "type",
              "smartArt"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "image"
              },
              "assetRef": {
                "type": "string",
                "pattern": "^asset:.+$"
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "alt": {
                "type": "string",
                "maxLength": 1000
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              },
              "wrap": {
                "enum": [
                  "inline",
                  "square",
                  "topAndBottom",
                  "none",
                  "behind"
                ]
              },
              "position": {
                "type": "object",
                "properties": {
                  "xPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  },
                  "yPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  }
                },
                "required": [
                  "xPx",
                  "yPx"
                ],
                "additionalProperties": false
              }
            },
            "required": [
              "type",
              "assetRef",
              "widthPx",
              "heightPx"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "shape"
              },
              "preset": {
                "enum": [
                  "line",
                  "verticalLine",
                  "rectangle",
                  "roundedRectangle",
                  "ellipse",
                  "diamond",
                  "textBox",
                  "accentBorderCallout1",
                  "accentBorderCallout2",
                  "accentBorderCallout3",
                  "accentCallout1",
                  "accentCallout2",
                  "accentCallout3",
                  "arc",
                  "bentArrow",
                  "bentUpArrow",
                  "bevel",
                  "blockArc",
                  "borderCallout1",
                  "borderCallout2",
                  "borderCallout3",
                  "bracePair",
                  "bracketPair",
                  "callout1",
                  "callout2",
                  "callout3",
                  "can",
                  "chartPlus",
                  "chartStar",
                  "chartX",
                  "chevron",
                  "chord",
                  "circularArrow",
                  "cloud",
                  "cloudCallout",
                  "corner",
                  "cornerTabs",
                  "cube",
                  "curvedDownArrow",
                  "curvedLeftArrow",
                  "curvedRightArrow",
                  "curvedUpArrow",
                  "decagon",
                  "diagStripe",
                  "dodecagon",
                  "donut",
                  "doubleWave",
                  "downArrow",
                  "downArrowCallout",
                  "ellipseRibbon",
                  "ellipseRibbon2",
                  "flowChartAlternateProcess",
                  "flowChartCollate",
                  "flowChartConnector",
                  "flowChartDecision",
                  "flowChartDelay",
                  "flowChartDisplay",
                  "flowChartDocument",
                  "flowChartExtract",
                  "flowChartInputOutput",
                  "flowChartInternalStorage",
                  "flowChartMagneticDisk",
                  "flowChartMagneticDrum",
                  "flowChartMagneticTape",
                  "flowChartManualInput",
                  "flowChartManualOperation",
                  "flowChartMerge",
                  "flowChartMultidocument",
                  "flowChartOfflineStorage",
                  "flowChartOffpageConnector",
                  "flowChartOnlineStorage",
                  "flowChartOr",
                  "flowChartPredefinedProcess",
                  "flowChartPreparation",
                  "flowChartProcess",
                  "flowChartPunchedCard",
                  "flowChartPunchedTape",
                  "flowChartSort",
                  "flowChartSummingJunction",
                  "flowChartTerminator",
                  "foldedCorner",
                  "frame",
                  "funnel",
                  "gear6",
                  "gear9",
                  "halfFrame",
                  "heart",
                  "heptagon",
                  "hexagon",
                  "homePlate",
                  "horizontalScroll",
                  "irregularSeal1",
                  "irregularSeal2",
                  "leftArrow",
                  "leftArrowCallout",
                  "leftBrace",
                  "leftBracket",
                  "leftCircularArrow",
                  "leftRightArrow",
                  "leftRightArrowCallout",
                  "leftRightCircularArrow",
                  "leftRightRibbon",
                  "leftRightUpArrow",
                  "leftUpArrow",
                  "lightningBolt",
                  "lineInv",
                  "mathDivide",
                  "mathEqual",
                  "mathMinus",
                  "mathMultiply",
                  "mathNotEqual",
                  "mathPlus",
                  "moon",
                  "nonIsoscelesTrapezoid",
                  "noSmoking",
                  "notchedRightArrow",
                  "octagon",
                  "parallelogram",
                  "pentagon",
                  "pie",
                  "pieWedge",
                  "plaque",
                  "plaqueTabs",
                  "plus",
                  "quadArrow",
                  "quadArrowCallout",
                  "rect",
                  "ribbon",
                  "ribbon2",
                  "rightArrow",
                  "rightArrowCallout",
                  "rightBrace",
                  "rightBracket",
                  "round1Rect",
                  "round2DiagRect",
                  "round2SameRect",
                  "roundRect",
                  "rtTriangle",
                  "smileyFace",
                  "snip1Rect",
                  "snip2DiagRect",
                  "snip2SameRect",
                  "snipRoundRect",
                  "squareTabs",
                  "star10",
                  "star12",
                  "star16",
                  "star24",
                  "star32",
                  "star4",
                  "star5",
                  "star6",
                  "star7",
                  "star8",
                  "stripedRightArrow",
                  "sun",
                  "swooshArrow",
                  "teardrop",
                  "trapezoid",
                  "triangle",
                  "upArrowCallout",
                  "upDownArrow",
                  "upArrow",
                  "upDownArrowCallout",
                  "uturnArrow",
                  "verticalScroll",
                  "wave",
                  "wedgeEllipseCallout",
                  "wedgeRectCallout",
                  "wedgeRoundRectCallout"
                ]
              },
              "text": {
                "type": "string",
                "maxLength": 20000
              },
              "textStyle": {
                "type": "object",
                "properties": {
                  "bold": {
                    "type": "boolean"
                  },
                  "italic": {
                    "type": "boolean"
                  },
                  "underline": {
                    "type": "boolean"
                  },
                  "color": {
                    "type": "string",
                    "pattern": "^#?[0-9A-Fa-f]{6}$"
                  },
                  "fontSizePt": {
                    "type": "number",
                    "minimum": 1,
                    "maximum": 1638
                  },
                  "fontFamily": {
                    "type": "string",
                    "maxLength": 100,
                    "minLength": 1
                  },
                  "align": {
                    "enum": [
                      "left",
                      "center",
                      "right"
                    ]
                  }
                },
                "required": [],
                "additionalProperties": false
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "position": {
                "type": "object",
                "properties": {
                  "xPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  },
                  "yPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  }
                },
                "required": [
                  "xPx",
                  "yPx"
                ],
                "additionalProperties": false
              },
              "fill": {
                "anyOf": [
                  {
                    "type": "string",
                    "pattern": "^#?[0-9A-Fa-f]{6}$"
                  },
                  {
                    "type": "null"
                  }
                ]
              },
              "line": {
                "anyOf": [
                  {
                    "type": "null"
                  },
                  {
                    "type": "object",
                    "properties": {
                      "color": {
                        "type": "string",
                        "pattern": "^#?[0-9A-Fa-f]{6}$"
                      },
                      "widthPx": {
                        "type": "number",
                        "minimum": 1,
                        "maximum": 5000
                      },
                      "dash": {
                        "enum": [
                          "solid",
                          "dashed",
                          "dotted"
                        ]
                      }
                    },
                    "required": [
                      "color",
                      "widthPx",
                      "dash"
                    ],
                    "additionalProperties": false
                  }
                ]
              },
              "wrap": {
                "enum": [
                  "inline",
                  "square",
                  "topAndBottom",
                  "none",
                  "behind"
                ]
              },
              "order": {
                "enum": [
                  "front",
                  "back"
                ]
              }
            },
            "required": [
              "type",
              "preset"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "wordArt"
              },
              "text": {
                "type": "string",
                "minLength": 1,
                "maxLength": 500
              },
              "preset": {
                "enum": [
                  "plain",
                  "archUp",
                  "archDown",
                  "wave",
                  "chevron",
                  "circle",
                  "button",
                  "chevronDown"
                ]
              },
              "widthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 5000
              },
              "position": {
                "type": "object",
                "properties": {
                  "xPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  },
                  "yPx": {
                    "type": "number",
                    "minimum": -5000,
                    "maximum": 5000
                  }
                },
                "required": [
                  "xPx",
                  "yPx"
                ],
                "additionalProperties": false
              },
              "rotation": {
                "type": "number"
              },
              "fill": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "opacity": {
                "type": "number",
                "minimum": 0,
                "maximum": 1
              },
              "wrap": {
                "enum": [
                  "inline",
                  "square",
                  "topAndBottom",
                  "none",
                  "behind"
                ]
              },
              "order": {
                "enum": [
                  "front",
                  "back"
                ]
              }
            },
            "required": [
              "type",
              "text",
              "preset"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "pageNumber"
              },
              "fieldKind": {
                "enum": [
                  "page",
                  "pageOfTotal"
                ]
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              },
              "color": {
                "type": "string",
                "pattern": "^#?[0-9A-Fa-f]{6}$"
              },
              "fontSizePt": {
                "type": "number",
                "minimum": 1,
                "maximum": 1638
              },
              "fontFamily": {
                "type": "string",
                "maxLength": 100,
                "minLength": 1
              },
              "bold": {
                "type": "boolean"
              }
            },
            "required": [
              "type",
              "fieldKind"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "type": {
                "const": "pageBreak"
              }
            },
            "required": [
              "type"
            ],
            "additionalProperties": false
          }
        ]
      }
    },
    "page": {
      "type": "object",
      "properties": {
        "margins": {
          "type": "object",
          "properties": {
            "top": {
              "type": "number",
              "minimum": 0,
              "maximum": 22
            },
            "right": {
              "type": "number",
              "minimum": 0,
              "maximum": 22
            },
            "bottom": {
              "type": "number",
              "minimum": 0,
              "maximum": 22
            },
            "left": {
              "type": "number",
              "minimum": 0,
              "maximum": 22
            }
          },
          "required": [],
          "additionalProperties": false
        },
        "mirrorMargins": {
          "type": "boolean"
        },
        "size": {
          "type": "object",
          "properties": {
            "width": {
              "type": "number",
              "minimum": 1,
              "maximum": 22
            },
            "height": {
              "type": "number",
              "minimum": 1,
              "maximum": 22
            }
          },
          "required": [
            "width",
            "height"
          ],
          "additionalProperties": false
        },
        "orientation": {
          "enum": [
            "portrait",
            "landscape"
          ]
        },
        "columns": {
          "type": "integer",
          "minimum": 1,
          "maximum": 12
        },
        "columnSeparator": {
          "type": "boolean"
        },
        "pageBorders": {
          "anyOf": [
            {
              "type": "object",
              "properties": {
                "sz": {
                  "type": "number",
                  "minimum": 0.125,
                  "maximum": 96
                },
                "color": {
                  "type": "string",
                  "pattern": "^#?[0-9A-Fa-f]{6}$"
                },
                "offsetFrom": {
                  "enum": [
                    "text",
                    "page"
                  ]
                }
              },
              "required": [],
              "additionalProperties": false
            },
            {
              "type": "null"
            }
          ]
        }
      },
      "required": [],
      "additionalProperties": false
    }
  },
  "required": [
    "body"
  ],
  "additionalProperties": false
}
```

来源： [`packages/document/tool-wordinweb/src/index.ts`](../packages/document/tool-wordinweb/src/index.ts)

### `word_document_edit`

对观察到的 revision 应用已校验的文档事务。

```json
{
  "type": "object",
  "properties": {
    "revision": {
      "type": "string",
      "minLength": 1
    },
    "operations": {
      "type": "array",
      "minItems": 1,
      "maxItems": 100,
      "items": {
        "anyOf": [
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertText"
              },
              "at": {
                "type": "object",
                "properties": {
                  "blockRef": {
                    "type": "string",
                    "pattern": "^block:[0-9]+$"
                  },
                  "runRef": {
                    "type": "string",
                    "pattern": "^run:[0-9]+$"
                  },
                  "offset": {
                    "type": "integer",
                    "minimum": 0
                  }
                },
                "required": [
                  "blockRef",
                  "runRef",
                  "offset"
                ],
                "additionalProperties": false
              },
              "text": {
                "type": "string",
                "minLength": 1,
                "maxLength": 100000
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "at",
              "text"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertSeparator"
              },
              "at": {
                "type": "object",
                "properties": {
                  "blockRef": {
                    "type": "string",
                    "pattern": "^block:[0-9]+$"
                  },
                  "runRef": {
                    "type": "string",
                    "pattern": "^run:[0-9]+$"
                  },
                  "offset": {
                    "type": "integer",
                    "minimum": 0
                  }
                },
                "required": [
                  "blockRef",
                  "runRef",
                  "offset"
                ],
                "additionalProperties": false
              },
              "separator": {
                "enum": [
                  "br",
                  "tab"
                ]
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "at",
              "separator"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "deleteSeparator"
              },
              "at": {
                "type": "object",
                "properties": {
                  "blockRef": {
                    "type": "string",
                    "pattern": "^block:[0-9]+$"
                  },
                  "runRef": {
                    "type": "string",
                    "pattern": "^run:[0-9]+$"
                  },
                  "offset": {
                    "type": "integer",
                    "minimum": 0
                  }
                },
                "required": [
                  "blockRef",
                  "runRef",
                  "offset"
                ],
                "additionalProperties": false
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "at"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "deleteText"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "start": {
                "type": "integer",
                "minimum": 0
              },
              "end": {
                "type": "integer",
                "minimum": 0
              }
            },
            "required": [
              "kind",
              "blockRef",
              "runRef",
              "start",
              "end"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "splitParagraph"
              },
              "at": {
                "type": "object",
                "properties": {
                  "blockRef": {
                    "type": "string",
                    "pattern": "^block:[0-9]+$"
                  },
                  "runRef": {
                    "type": "string",
                    "pattern": "^run:[0-9]+$"
                  },
                  "offset": {
                    "type": "integer",
                    "minimum": 0
                  }
                },
                "required": [
                  "blockRef",
                  "runRef",
                  "offset"
                ],
                "additionalProperties": false
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "at"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "formatRun"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "patch": {
                "$ref": "#/$defs/patch"
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "blockRef",
              "runRef",
              "patch"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "formatParagraph"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right",
                  "justify"
                ]
              },
              "styleId": {
                "type": [
                  "string",
                  "null"
                ]
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "blockRef"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "formatRange"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "start": {
                "type": "integer",
                "minimum": 0
              },
              "end": {
                "type": "integer",
                "minimum": 0
              },
              "patch": {
                "$ref": "#/$defs/patch"
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "blockRef",
              "runRef",
              "start",
              "end",
              "patch"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "tableOp"
              },
              "cellRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "op": {
                "anyOf": [
                  {
                    "enum": [
                      "deleteRow",
                      "deleteCol",
                      "deleteTable",
                      "rowAbove",
                      "rowBelow",
                      "colLeft",
                      "colRight",
                      "mergeRight",
                      "mergeDown",
                      "splitCell"
                    ]
                  },
                  {
                    "type": "object",
                    "properties": {
                      "kind": {
                        "const": "cellShading"
                      },
                      "fill": {
                        "$ref": "#/$defs/color2"
                      }
                    },
                    "required": [
                      "kind",
                      "fill"
                    ],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "kind": {
                        "const": "cellVAlign"
                      },
                      "v": {
                        "enum": [
                          "top",
                          "center",
                          "bottom"
                        ]
                      }
                    },
                    "required": [
                      "kind",
                      "v"
                    ],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "kind": {
                        "const": "textWrapping"
                      },
                      "wrapping": {
                        "enum": [
                          "none",
                          "around"
                        ]
                      },
                      "xPx": {
                        "$ref": "#/$defs/xPx"
                      },
                      "yPx": {
                        "$ref": "#/$defs/xPx"
                      }
                    },
                    "required": [
                      "kind",
                      "wrapping",
                      "xPx",
                      "yPx"
                    ],
                    "additionalProperties": false
                  }
                ]
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "cellRef",
              "op"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "mergeParagraph"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "blockRef"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "commentRun"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "text": {
                "$ref": "#/$defs/text"
              },
              "initials": {
                "type": "string"
              }
            },
            "required": [
              "kind",
              "runRef",
              "text"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "pasteBlocks"
              },
              "afterBlockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "blocksXml": {
                "type": "string",
                "minLength": 1,
                "maxLength": 2000000
              }
            },
            "required": [
              "kind",
              "afterBlockRef",
              "blocksXml"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertImage"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "assetRef": {
                "type": "string",
                "pattern": "^asset:.+$"
              },
              "widthPx": {
                "$ref": "#/$defs/widthPx"
              },
              "heightPx": {
                "$ref": "#/$defs/widthPx"
              }
            },
            "required": [
              "kind",
              "runRef",
              "assetRef",
              "widthPx",
              "heightPx"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "suggestRevision"
              },
              "ranges": {
                "type": "array",
                "items": {
                  "type": "object",
                  "properties": {
                    "blockRef": {
                      "type": "string",
                      "pattern": "^block:[0-9]+$"
                    },
                    "runRef": {
                      "type": "string",
                      "pattern": "^run:[0-9]+$"
                    },
                    "start": {
                      "type": "integer",
                      "minimum": 0
                    },
                    "end": {
                      "type": "integer",
                      "minimum": 1
                    }
                  },
                  "required": [
                    "blockRef",
                    "runRef",
                    "start",
                    "end"
                  ],
                  "additionalProperties": false
                }
              },
              "marks": {
                "type": "array",
                "items": {
                  "type": "object",
                  "properties": {
                    "blockRef": {
                      "type": "string",
                      "pattern": "^block:[0-9]+$"
                    },
                    "glyph": {
                      "enum": [
                        "ins",
                        "del"
                      ]
                    }
                  },
                  "required": [
                    "blockRef",
                    "glyph"
                  ],
                  "additionalProperties": false
                }
              }
            },
            "required": [
              "kind"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertBreak"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "breakKind": {
                "enum": [
                  "page",
                  "column"
                ]
              }
            },
            "required": [
              "kind",
              "runRef",
              "breakKind"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertMath"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "mathText": {
                "type": "string"
              }
            },
            "required": [
              "kind",
              "runRef",
              "mathText"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertShape"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "preset": {
                "enum": [
                  "line",
                  "verticalLine",
                  "rectangle",
                  "roundedRectangle",
                  "ellipse",
                  "diamond",
                  "textBox",
                  "accentBorderCallout1",
                  "accentBorderCallout2",
                  "accentBorderCallout3",
                  "accentCallout1",
                  "accentCallout2",
                  "accentCallout3",
                  "arc",
                  "bentArrow",
                  "bentUpArrow",
                  "bevel",
                  "blockArc",
                  "borderCallout1",
                  "borderCallout2",
                  "borderCallout3",
                  "bracePair",
                  "bracketPair",
                  "callout1",
                  "callout2",
                  "callout3",
                  "can",
                  "chartPlus",
                  "chartStar",
                  "chartX",
                  "chevron",
                  "chord",
                  "circularArrow",
                  "cloud",
                  "cloudCallout",
                  "corner",
                  "cornerTabs",
                  "cube",
                  "curvedDownArrow",
                  "curvedLeftArrow",
                  "curvedRightArrow",
                  "curvedUpArrow",
                  "decagon",
                  "diagStripe",
                  "dodecagon",
                  "donut",
                  "doubleWave",
                  "downArrow",
                  "downArrowCallout",
                  "ellipseRibbon",
                  "ellipseRibbon2",
                  "flowChartAlternateProcess",
                  "flowChartCollate",
                  "flowChartConnector",
                  "flowChartDecision",
                  "flowChartDelay",
                  "flowChartDisplay",
                  "flowChartDocument",
                  "flowChartExtract",
                  "flowChartInputOutput",
                  "flowChartInternalStorage",
                  "flowChartMagneticDisk",
                  "flowChartMagneticDrum",
                  "flowChartMagneticTape",
                  "flowChartManualInput",
                  "flowChartManualOperation",
                  "flowChartMerge",
                  "flowChartMultidocument",
                  "flowChartOfflineStorage",
                  "flowChartOffpageConnector",
                  "flowChartOnlineStorage",
                  "flowChartOr",
                  "flowChartPredefinedProcess",
                  "flowChartPreparation",
                  "flowChartProcess",
                  "flowChartPunchedCard",
                  "flowChartPunchedTape",
                  "flowChartSort",
                  "flowChartSummingJunction",
                  "flowChartTerminator",
                  "foldedCorner",
                  "frame",
                  "funnel",
                  "gear6",
                  "gear9",
                  "halfFrame",
                  "heart",
                  "heptagon",
                  "hexagon",
                  "homePlate",
                  "horizontalScroll",
                  "irregularSeal1",
                  "irregularSeal2",
                  "leftArrow",
                  "leftArrowCallout",
                  "leftBrace",
                  "leftBracket",
                  "leftCircularArrow",
                  "leftRightArrow",
                  "leftRightArrowCallout",
                  "leftRightCircularArrow",
                  "leftRightRibbon",
                  "leftRightUpArrow",
                  "leftUpArrow",
                  "lightningBolt",
                  "lineInv",
                  "mathDivide",
                  "mathEqual",
                  "mathMinus",
                  "mathMultiply",
                  "mathNotEqual",
                  "mathPlus",
                  "moon",
                  "nonIsoscelesTrapezoid",
                  "noSmoking",
                  "notchedRightArrow",
                  "octagon",
                  "parallelogram",
                  "pentagon",
                  "pie",
                  "pieWedge",
                  "plaque",
                  "plaqueTabs",
                  "plus",
                  "quadArrow",
                  "quadArrowCallout",
                  "rect",
                  "ribbon",
                  "ribbon2",
                  "rightArrow",
                  "rightArrowCallout",
                  "rightBrace",
                  "rightBracket",
                  "round1Rect",
                  "round2DiagRect",
                  "round2SameRect",
                  "roundRect",
                  "rtTriangle",
                  "smileyFace",
                  "snip1Rect",
                  "snip2DiagRect",
                  "snip2SameRect",
                  "snipRoundRect",
                  "squareTabs",
                  "star10",
                  "star12",
                  "star16",
                  "star24",
                  "star32",
                  "star4",
                  "star5",
                  "star6",
                  "star7",
                  "star8",
                  "stripedRightArrow",
                  "sun",
                  "swooshArrow",
                  "teardrop",
                  "trapezoid",
                  "triangle",
                  "upArrowCallout",
                  "upDownArrow",
                  "upArrow",
                  "upDownArrowCallout",
                  "uturnArrow",
                  "verticalScroll",
                  "wave",
                  "wedgeEllipseCallout",
                  "wedgeRectCallout",
                  "wedgeRoundRectCallout"
                ]
              },
              "text": {
                "type": "string",
                "maxLength": 20000
              }
            },
            "required": [
              "kind",
              "runRef",
              "preset"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "replyComment"
              },
              "parentId": {
                "type": "string"
              },
              "text": {
                "$ref": "#/$defs/text"
              },
              "initials": {
                "type": "string"
              }
            },
            "required": [
              "kind",
              "parentId",
              "text"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "adjustIndent"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "direction": {
                "enum": [
                  1,
                  -1
                ]
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "blockRef",
              "direction"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setSpacing"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "patch": {
                "type": "object",
                "properties": {
                  "lineMultiple": {
                    "type": "number",
                    "minimum": 0.1,
                    "maximum": 20
                  },
                  "exactLinePt": {
                    "type": "number",
                    "minimum": 0.1,
                    "maximum": 2000
                  },
                  "beforePt": {
                    "type": [
                      "number",
                      "null"
                    ]
                  },
                  "afterPt": {
                    "type": [
                      "number",
                      "null"
                    ]
                  }
                },
                "required": [],
                "additionalProperties": false
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "blockRef",
              "patch"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertPageField"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "fieldKind": {
                "enum": [
                  "page",
                  "pageOfTotal"
                ]
              }
            },
            "required": [
              "kind",
              "runRef",
              "fieldKind"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setLink"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "url": {
                "type": "string",
                "minLength": 1,
                "maxLength": 4000
              }
            },
            "required": [
              "kind",
              "runRef",
              "url"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertFootnote"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "text": {
                "$ref": "#/$defs/text"
              }
            },
            "required": [
              "kind",
              "runRef",
              "text"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setDropCap"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "mode": {
                "enum": [
                  "drop",
                  "margin",
                  null
                ]
              }
            },
            "required": [
              "kind",
              "blockRef",
              "mode"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setDivider"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "divider": {
                "anyOf": [
                  {
                    "type": "object",
                    "properties": {
                      "style": {
                        "enum": [
                          "single",
                          "double",
                          "dotted",
                          "dashed",
                          "thinThickSmallGap"
                        ]
                      },
                      "color": {
                        "$ref": "#/$defs/color4"
                      },
                      "widthPt": {
                        "type": "number",
                        "minimum": 0.1,
                        "maximum": 100
                      },
                      "spacePt": {
                        "type": "number",
                        "minimum": 0,
                        "maximum": 1000
                      }
                    },
                    "required": [
                      "style",
                      "color",
                      "widthPt",
                      "spacePt"
                    ],
                    "additionalProperties": false
                  },
                  {
                    "type": "null"
                  }
                ]
              }
            },
            "required": [
              "kind",
              "blockRef",
              "divider"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertBookmark"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "name": {
                "type": "string"
              }
            },
            "required": [
              "kind",
              "runRef",
              "name"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertBlankPage"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              }
            },
            "required": [
              "kind",
              "runRef"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertSectionBreak"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "breakType": {
                "enum": [
                  "nextPage",
                  "continuous"
                ]
              }
            },
            "required": [
              "kind",
              "runRef",
              "breakType"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertCrossRef"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "bookmark": {
                "type": "string"
              },
              "refKind": {
                "enum": [
                  "text",
                  "page"
                ]
              }
            },
            "required": [
              "kind",
              "runRef",
              "bookmark",
              "refKind"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertCoverPage"
              },
              "content": {
                "type": "object",
                "properties": {
                  "title": {
                    "type": "string",
                    "minLength": 1,
                    "maxLength": 1000
                  },
                  "subtitle": {
                    "type": "string",
                    "maxLength": 1000
                  },
                  "author": {
                    "type": "string",
                    "maxLength": 500
                  },
                  "layout": {
                    "type": "string",
                    "enum": [
                      "title",
                      "banner",
                      "sidebar"
                    ]
                  }
                },
                "required": [
                  "title"
                ],
                "additionalProperties": false
              }
            },
            "required": [
              "kind",
              "content"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setPageLayout"
              },
              "patch": {
                "type": "object",
                "properties": {
                  "margins": {
                    "type": "object",
                    "properties": {
                      "top": {
                        "$ref": "#/$defs/top5"
                      },
                      "right": {
                        "$ref": "#/$defs/top5"
                      },
                      "bottom": {
                        "$ref": "#/$defs/top5"
                      },
                      "left": {
                        "$ref": "#/$defs/top5"
                      }
                    },
                    "required": [],
                    "additionalProperties": false
                  },
                  "mirrorMargins": {
                    "type": "boolean"
                  },
                  "size": {
                    "type": "object",
                    "properties": {
                      "width": {
                        "type": "number",
                        "minimum": 1,
                        "maximum": 22
                      },
                      "height": {
                        "type": "number",
                        "minimum": 1,
                        "maximum": 22
                      }
                    },
                    "required": [
                      "width",
                      "height"
                    ],
                    "additionalProperties": false
                  },
                  "orientation": {
                    "enum": [
                      "portrait",
                      "landscape"
                    ]
                  },
                  "columns": {
                    "type": "integer",
                    "minimum": 1,
                    "maximum": 12
                  },
                  "columnSeparator": {
                    "type": "boolean"
                  },
                  "pageBorders": {
                    "anyOf": [
                      {
                        "type": "object",
                        "properties": {
                          "sz": {
                            "type": "number",
                            "minimum": 0.125,
                            "maximum": 96
                          },
                          "color": {
                            "$ref": "#/$defs/color4"
                          },
                          "offsetFrom": {
                            "enum": [
                              "text",
                              "page"
                            ]
                          }
                        },
                        "required": [],
                        "additionalProperties": false
                      },
                      {
                        "type": "null"
                      }
                    ]
                  }
                },
                "required": [],
                "additionalProperties": false
              }
            },
            "required": [
              "kind",
              "patch"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setListLevel"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "delta": {
                "enum": [
                  1,
                  -1
                ]
              }
            },
            "required": [
              "kind",
              "blockRef",
              "delta"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertWordArt"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "text": {
                "type": "string",
                "minLength": 1,
                "maxLength": 500
              },
              "preset": {
                "enum": [
                  "plain",
                  "archUp",
                  "archDown",
                  "wave",
                  "chevron",
                  "circle",
                  "button",
                  "chevronDown"
                ]
              },
              "style": {
                "type": "object",
                "properties": {
                  "fill": {
                    "$ref": "#/$defs/fill"
                  },
                  "outline": {
                    "type": "object",
                    "properties": {
                      "color": {
                        "$ref": "#/$defs/fill"
                      },
                      "widthPt": {
                        "type": "number",
                        "minimum": 0.25,
                        "maximum": 50
                      }
                    },
                    "required": [
                      "color",
                      "widthPt"
                    ],
                    "additionalProperties": false
                  },
                  "shadow": {
                    "type": "boolean"
                  }
                },
                "required": [
                  "fill"
                ],
                "additionalProperties": false
              }
            },
            "required": [
              "kind",
              "runRef",
              "text",
              "preset"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertChart"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "chart": {
                "$ref": "#/$defs/chart"
              }
            },
            "required": [
              "kind",
              "runRef",
              "chart"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertSmartArt"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "smartArt": {
                "$ref": "#/$defs/smartArt"
              }
            },
            "required": [
              "kind",
              "runRef",
              "smartArt"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setLineNumbering"
              },
              "patch": {
                "type": "object",
                "properties": {
                  "enabled": {
                    "type": "boolean"
                  },
                  "countBy": {
                    "type": "integer",
                    "minimum": 1,
                    "maximum": 100
                  },
                  "restart": {
                    "enum": [
                      "continuous",
                      "newPage",
                      "newSection"
                    ]
                  },
                  "start": {
                    "type": "integer",
                    "minimum": 0,
                    "maximum": 100000
                  }
                },
                "required": [
                  "enabled"
                ],
                "additionalProperties": false
              }
            },
            "required": [
              "kind",
              "patch"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertDateTimeField"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "dtKind": {
                "enum": [
                  "date",
                  "time"
                ]
              },
              "picture": {
                "$ref": "#/$defs/fontFamily"
              }
            },
            "required": [
              "kind",
              "runRef",
              "dtKind",
              "picture"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertField"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "instruction": {
                "type": "string",
                "minLength": 1,
                "maxLength": 200
              },
              "cachedResult": {
                "type": "string",
                "maxLength": 1000
              }
            },
            "required": [
              "kind",
              "runRef",
              "instruction"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setDrawingRotation"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              },
              "degrees": {
                "type": "number"
              }
            },
            "required": [
              "kind",
              "objectRef",
              "degrees"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setDrawingFill"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              },
              "color": {
                "$ref": "#/$defs/color3"
              }
            },
            "required": [
              "kind",
              "objectRef",
              "color"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setChartData"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              },
              "chart": {
                "$ref": "#/$defs/chart"
              }
            },
            "required": [
              "kind",
              "objectRef",
              "chart"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setSmartArtNodeText"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              },
              "index": {
                "type": "integer",
                "minimum": 0,
                "maximum": 100000
              },
              "text": {
                "type": "string",
                "maxLength": 500
              }
            },
            "required": [
              "kind",
              "objectRef",
              "index",
              "text"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setDrawingWordArtText"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              },
              "text": {
                "type": "string",
                "minLength": 1,
                "maxLength": 500
              }
            },
            "required": [
              "kind",
              "objectRef",
              "text"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setDrawingWordArtStyle"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              },
              "color": {
                "$ref": "#/$defs/fill"
              },
              "opacity": {
                "type": "number",
                "minimum": 0,
                "maximum": 1
              }
            },
            "required": [
              "kind",
              "objectRef",
              "color",
              "opacity"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setDrawingLineStyle"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              },
              "color": {
                "$ref": "#/$defs/color3"
              },
              "widthPx": {
                "$ref": "#/$defs/widthPx"
              },
              "dash": {
                "enum": [
                  "solid",
                  "dashed",
                  "dotted"
                ]
              }
            },
            "required": [
              "kind",
              "objectRef",
              "color"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setImageAltText"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              },
              "alt": {
                "type": "string",
                "maxLength": 1000
              }
            },
            "required": [
              "kind",
              "objectRef",
              "alt"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "removeLink"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              }
            },
            "required": [
              "kind",
              "runRef"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setImageWrap"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              },
              "mode": {
                "enum": [
                  "inline",
                  "square",
                  "topAndBottom",
                  "none",
                  "behind"
                ]
              }
            },
            "required": [
              "kind",
              "objectRef",
              "mode"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setDrawingOrder"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              },
              "order": {
                "enum": [
                  "front",
                  "back"
                ]
              }
            },
            "required": [
              "kind",
              "objectRef",
              "order"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setSmartArtData"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              },
              "smartArt": {
                "$ref": "#/$defs/smartArt"
              }
            },
            "required": [
              "kind",
              "objectRef",
              "smartArt"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setSmartArtFill"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              },
              "color": {
                "$ref": "#/$defs/color3"
              },
              "nodeIndex": {
                "type": "integer",
                "minimum": 0,
                "maximum": 100000
              }
            },
            "required": [
              "kind",
              "objectRef",
              "color"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setSmartArtTextFormat"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              },
              "format": {
                "type": "object",
                "properties": {
                  "fontFamily": {
                    "$ref": "#/$defs/fontFamily"
                  },
                  "fontSizePt": {
                    "$ref": "#/$defs/fontSizePt"
                  },
                  "color": {
                    "$ref": "#/$defs/fill"
                  },
                  "bold": {
                    "type": "boolean"
                  },
                  "italic": {
                    "type": "boolean"
                  },
                  "alignment": {
                    "enum": [
                      "left",
                      "center",
                      "right"
                    ]
                  }
                },
                "required": [
                  "fontFamily",
                  "fontSizePt",
                  "color",
                  "bold",
                  "italic",
                  "alignment"
                ],
                "additionalProperties": false
              },
              "nodeIndex": {
                "type": "integer",
                "minimum": 0,
                "maximum": 100000
              }
            },
            "required": [
              "kind",
              "objectRef",
              "format"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setFloatingPagePosition"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              },
              "xPx": {
                "$ref": "#/$defs/xPx2"
              },
              "yPx": {
                "$ref": "#/$defs/xPx2"
              }
            },
            "required": [
              "kind",
              "objectRef",
              "xPx",
              "yPx"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "resizeDrawing"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              },
              "widthPx": {
                "$ref": "#/$defs/widthPx"
              },
              "heightPx": {
                "$ref": "#/$defs/widthPx"
              }
            },
            "required": [
              "kind",
              "objectRef",
              "widthPx",
              "heightPx"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "resizeTableColumn"
              },
              "cellRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "boundary": {
                "type": "integer",
                "minimum": 1,
                "maximum": 200
              },
              "deltaPx": {
                "$ref": "#/$defs/xPx2"
              },
              "renderedWidths": {
                "$ref": "#/$defs/renderedWidths"
              }
            },
            "required": [
              "kind",
              "cellRef",
              "boundary",
              "deltaPx"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "moveTable"
              },
              "cellRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "xPx": {
                "$ref": "#/$defs/xPx"
              },
              "yPx": {
                "$ref": "#/$defs/xPx"
              },
              "preservePageStart": {
                "type": "boolean"
              },
              "pageDelta": {
                "type": "integer",
                "minimum": -500,
                "maximum": 500
              }
            },
            "required": [
              "kind",
              "cellRef",
              "xPx",
              "yPx",
              "preservePageStart",
              "pageDelta"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "removeDrawing"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              }
            },
            "required": [
              "kind",
              "objectRef"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setMathLinear"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "mathText": {
                "type": "string"
              },
              "mathIndex": {
                "type": "integer",
                "minimum": 0,
                "maximum": 1000
              }
            },
            "required": [
              "kind",
              "blockRef",
              "mathText"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "deleteMath"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "mathIndex": {
                "type": "integer",
                "minimum": 0,
                "maximum": 1000
              }
            },
            "required": [
              "kind",
              "blockRef"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "moveMath"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "at": {
                "type": "object",
                "properties": {
                  "blockRef": {
                    "type": "string",
                    "pattern": "^block:[0-9]+$"
                  },
                  "runRef": {
                    "type": "string",
                    "pattern": "^run:[0-9]+$"
                  },
                  "offset": {
                    "type": "integer",
                    "minimum": 0
                  }
                },
                "required": [
                  "blockRef",
                  "runRef",
                  "offset"
                ],
                "additionalProperties": false
              },
              "mathIndex": {
                "type": "integer",
                "minimum": 0,
                "maximum": 1000
              }
            },
            "required": [
              "kind",
              "blockRef",
              "at"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "ensureHeaderFooter"
              },
              "hfKind": {
                "enum": [
                  "header",
                  "footer"
                ]
              }
            },
            "required": [
              "kind",
              "hfKind"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "deleteComment"
              },
              "commentId": {
                "type": "string"
              }
            },
            "required": [
              "kind",
              "commentId"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "resolveComment"
              },
              "commentId": {
                "type": "string"
              },
              "resolved": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "commentId",
              "resolved"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "editComment"
              },
              "commentId": {
                "type": "string"
              },
              "text": {
                "$ref": "#/$defs/text"
              }
            },
            "required": [
              "kind",
              "commentId",
              "text"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertBookmarkRange"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "name": {
                "type": "string",
                "pattern": "^[A-Za-z_][A-Za-z0-9_]{0,39}$"
              },
              "start": {
                "type": "integer",
                "minimum": 0
              },
              "end": {
                "type": "integer",
                "minimum": 0
              }
            },
            "required": [
              "kind",
              "runRef",
              "name",
              "start",
              "end"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "acceptRevision"
              },
              "index": {
                "type": "integer",
                "minimum": 0,
                "maximum": 100000
              }
            },
            "required": [
              "kind",
              "index"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "rejectRevision"
              },
              "index": {
                "type": "integer",
                "minimum": 0,
                "maximum": 100000
              }
            },
            "required": [
              "kind",
              "index"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "acceptAllRevisions"
              }
            },
            "required": [
              "kind"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "rejectAllRevisions"
              }
            },
            "required": [
              "kind"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setListType"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "listKind": {
                "enum": [
                  "bullet",
                  "number",
                  null
                ]
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "blockRef",
              "listKind"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertTable"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "rows": {
                "$ref": "#/$defs/rows"
              },
              "cols": {
                "$ref": "#/$defs/rows"
              },
              "cells": {
                "type": "array",
                "maxItems": 50,
                "items": {
                  "type": "array",
                  "maxItems": 50,
                  "items": {
                    "type": "string",
                    "maxLength": 2000
                  }
                }
              },
              "headerRow": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "runRef",
              "rows",
              "cols"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "resizeTableRow"
              },
              "cellRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "rowIdx": {
                "$ref": "#/$defs/rowIdx"
              },
              "heightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 20000
              }
            },
            "required": [
              "kind",
              "cellRef",
              "rowIdx",
              "heightPx"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "toggleCheckbox"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              }
            },
            "required": [
              "kind",
              "runRef"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "updateFields"
              },
              "results": {
                "type": "array",
                "maxItems": 20000,
                "items": {
                  "type": "string",
                  "maxLength": 4096
                }
              }
            },
            "required": [
              "kind",
              "results"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertToc"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "entryCount": {
                "$ref": "#/$defs/entryCount"
              },
              "levels": {
                "type": "array",
                "minItems": 2,
                "maxItems": 2,
                "items": {
                  "type": "integer",
                  "minimum": 1,
                  "maximum": 9
                }
              },
              "leader": {
                "enum": [
                  "dot",
                  "hyphen",
                  "underscore",
                  "none"
                ]
              },
              "captionLabel": {
                "type": "string",
                "pattern": "^[A-Za-z][A-Za-z0-9]{0,31}$"
              }
            },
            "required": [
              "kind",
              "runRef",
              "entryCount"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertCaption"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "label": {
                "type": "string",
                "pattern": "^[A-Za-z][A-Za-z0-9]{0,31}$"
              },
              "text": {
                "type": "string",
                "maxLength": 2000
              },
              "position": {
                "enum": [
                  "below",
                  "above"
                ]
              }
            },
            "required": [
              "kind",
              "blockRef",
              "label"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "ensureRefBookmark"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "name": {
                "type": "string",
                "pattern": "^_Ref[0-9]{1,12}$"
              }
            },
            "required": [
              "kind",
              "blockRef",
              "name"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertIndexEntry"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "entry": {
                "type": "string",
                "minLength": 1,
                "maxLength": 128
              }
            },
            "required": [
              "kind",
              "runRef",
              "entry"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertIndex"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "entryCount": {
                "$ref": "#/$defs/entryCount"
              }
            },
            "required": [
              "kind",
              "runRef",
              "entryCount"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "refreshIndex"
              },
              "entryCount": {
                "$ref": "#/$defs/entryCount"
              }
            },
            "required": [
              "kind",
              "entryCount"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "createStyle"
              },
              "style": {
                "type": "object",
                "properties": {
                  "styleId": {
                    "$ref": "#/$defs/characterStyleId2"
                  },
                  "type": {
                    "enum": [
                      "paragraph",
                      "character",
                      "table",
                      "numbering"
                    ]
                  },
                  "name": {
                    "type": "string",
                    "minLength": 1,
                    "maxLength": 253
                  },
                  "basedOn": {
                    "$ref": "#/$defs/characterStyleId"
                  },
                  "next": {
                    "$ref": "#/$defs/characterStyleId"
                  },
                  "quickStyle": {
                    "type": "boolean"
                  },
                  "uiPriority": {
                    "type": "integer",
                    "minimum": 0,
                    "maximum": 99
                  },
                  "paragraph": {
                    "$ref": "#/$defs/paragraph"
                  },
                  "run": {
                    "$ref": "#/$defs/run"
                  },
                  "linked": {
                    "type": "boolean"
                  },
                  "table": {
                    "type": "object",
                    "properties": {
                      "borders": {
                        "type": "object",
                        "properties": {
                          "top": {
                            "$ref": "#/$defs/top"
                          },
                          "bottom": {
                            "$ref": "#/$defs/top"
                          },
                          "left": {
                            "$ref": "#/$defs/top"
                          },
                          "right": {
                            "$ref": "#/$defs/top"
                          },
                          "insideH": {
                            "$ref": "#/$defs/top"
                          },
                          "insideV": {
                            "$ref": "#/$defs/top"
                          }
                        },
                        "required": [],
                        "additionalProperties": false
                      }
                    },
                    "required": [],
                    "additionalProperties": false
                  },
                  "numbering": {
                    "type": "object",
                    "properties": {
                      "numId": {
                        "type": "integer",
                        "minimum": 1,
                        "maximum": 32767
                      }
                    },
                    "required": [
                      "numId"
                    ],
                    "additionalProperties": false
                  }
                },
                "required": [
                  "styleId",
                  "type",
                  "name"
                ],
                "additionalProperties": false
              }
            },
            "required": [
              "kind",
              "style"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "modifyStyle"
              },
              "styleId": {
                "$ref": "#/$defs/characterStyleId2"
              },
              "patch": {
                "type": "object",
                "properties": {
                  "name": {
                    "type": "string",
                    "minLength": 1,
                    "maxLength": 253
                  },
                  "basedOn": {
                    "$ref": "#/$defs/characterStyleId"
                  },
                  "next": {
                    "$ref": "#/$defs/characterStyleId"
                  },
                  "quickStyle": {
                    "type": "boolean"
                  },
                  "uiPriority": {
                    "type": "integer",
                    "minimum": 0,
                    "maximum": 99
                  },
                  "paragraph": {
                    "$ref": "#/$defs/paragraph"
                  },
                  "run": {
                    "$ref": "#/$defs/run"
                  }
                },
                "required": [],
                "additionalProperties": false
              }
            },
            "required": [
              "kind",
              "styleId",
              "patch"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "deleteStyle"
              },
              "styleId": {
                "$ref": "#/$defs/characterStyleId2"
              }
            },
            "required": [
              "kind",
              "styleId"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setNumberingLevel"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "ilvl": {
                "$ref": "#/$defs/outlineLevel"
              },
              "patch": {
                "type": "object",
                "properties": {
                  "format": {
                    "enum": [
                      "decimal",
                      "decimalZero",
                      "upperRoman",
                      "lowerRoman",
                      "upperLetter",
                      "lowerLetter",
                      "ordinal",
                      "bullet",
                      "none"
                    ]
                  },
                  "text": {
                    "$ref": "#/$defs/fontFamily"
                  },
                  "start": {
                    "type": "integer",
                    "minimum": 0,
                    "maximum": 32767
                  },
                  "alignment": {
                    "enum": [
                      "left",
                      "center",
                      "right"
                    ]
                  },
                  "indentLeftPt": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 1584
                  },
                  "hangingPt": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 1584
                  }
                },
                "required": [],
                "additionalProperties": false
              }
            },
            "required": [
              "kind",
              "blockRef",
              "ilvl",
              "patch"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setNumberingRestart"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "start": {
                "anyOf": [
                  {
                    "type": "integer",
                    "minimum": 0,
                    "maximum": 32767
                  },
                  {
                    "type": "null"
                  }
                ]
              }
            },
            "required": [
              "kind",
              "blockRef",
              "start"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setParagraphBorders"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "patch": {
                "type": "object",
                "properties": {
                  "borders": {
                    "type": "object",
                    "additionalProperties": false,
                    "properties": {
                      "top": {
                        "$ref": "#/$defs/top2"
                      },
                      "left": {
                        "$ref": "#/$defs/top2"
                      },
                      "bottom": {
                        "$ref": "#/$defs/top2"
                      },
                      "right": {
                        "$ref": "#/$defs/top2"
                      },
                      "between": {
                        "$ref": "#/$defs/top2"
                      },
                      "bar": {
                        "$ref": "#/$defs/top2"
                      }
                    }
                  },
                  "shading": {
                    "$ref": "#/$defs/color2"
                  }
                },
                "additionalProperties": false
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "blockRef",
              "patch"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setTabStops"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "stops": {
                "type": "array",
                "maxItems": 64,
                "items": {
                  "type": "object",
                  "properties": {
                    "posPt": {
                      "$ref": "#/$defs/indentLeftPt2"
                    },
                    "align": {
                      "enum": [
                        "left",
                        "center",
                        "right",
                        "decimal",
                        "bar"
                      ]
                    },
                    "leader": {
                      "enum": [
                        "none",
                        "dot",
                        "hyphen",
                        "underscore",
                        "middleDot"
                      ]
                    }
                  },
                  "required": [
                    "posPt",
                    "align",
                    "leader"
                  ],
                  "additionalProperties": false
                }
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "blockRef",
              "stops"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setTableBorders"
              },
              "cellRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "scope": {
                "enum": [
                  "cell",
                  "table"
                ]
              },
              "edges": {
                "type": "array",
                "minItems": 1,
                "maxItems": 8,
                "items": {
                  "enum": [
                    "top",
                    "bottom",
                    "left",
                    "right",
                    "insideH",
                    "insideV",
                    "tl2br",
                    "tr2bl"
                  ]
                }
              },
              "border": {
                "anyOf": [
                  {
                    "$ref": "#/$defs/top"
                  },
                  {
                    "type": "null"
                  }
                ]
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "cellRef",
              "scope",
              "edges",
              "border"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setTableStyle"
              },
              "cellRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "styleId": {
                "type": [
                  "string",
                  "null"
                ]
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "cellRef",
              "styleId"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setTableLook"
              },
              "cellRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "look": {
                "type": "object",
                "properties": {
                  "firstRow": {
                    "type": "boolean"
                  },
                  "lastRow": {
                    "type": "boolean"
                  },
                  "firstColumn": {
                    "type": "boolean"
                  },
                  "lastColumn": {
                    "type": "boolean"
                  },
                  "bandedRows": {
                    "type": "boolean"
                  },
                  "bandedCols": {
                    "type": "boolean"
                  }
                },
                "required": [],
                "additionalProperties": false
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "cellRef",
              "look"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setTableWidth"
              },
              "cellRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "unit": {
                "enum": [
                  "pt",
                  "pct",
                  "auto"
                ]
              },
              "value": {
                "type": "number",
                "minimum": 0.1,
                "maximum": 1584
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "cellRef",
              "unit"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setTableColumnWidth"
              },
              "cellRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "colIdx": {
                "type": "integer",
                "minimum": 0,
                "maximum": 200
              },
              "widthPt": {
                "type": "number",
                "minimum": 1,
                "maximum": 1584
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "cellRef",
              "colIdx",
              "widthPt"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setTableLayout"
              },
              "cellRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "layout": {
                "enum": [
                  "fixed",
                  "autofit"
                ]
              },
              "renderedWidths": {
                "$ref": "#/$defs/renderedWidths"
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "cellRef",
              "layout"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setTableCellMargins"
              },
              "cellRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "scope": {
                "enum": [
                  "cell",
                  "table"
                ]
              },
              "margins": {
                "anyOf": [
                  {
                    "type": "object",
                    "properties": {
                      "top": {
                        "$ref": "#/$defs/top4"
                      },
                      "left": {
                        "$ref": "#/$defs/top4"
                      },
                      "bottom": {
                        "$ref": "#/$defs/top4"
                      },
                      "right": {
                        "$ref": "#/$defs/top4"
                      }
                    },
                    "required": [],
                    "additionalProperties": false
                  },
                  {
                    "type": "null"
                  }
                ]
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "cellRef",
              "scope",
              "margins"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setTableHeaderRows"
              },
              "cellRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "count": {
                "$ref": "#/$defs/rowIdx"
              },
              "suggest": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "cellRef",
              "count"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertTableFormula"
              },
              "cellRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "formula": {
                "type": "string",
                "minLength": 1,
                "maxLength": 129
              },
              "numFmt": {
                "type": "string",
                "minLength": 1,
                "maxLength": 32
              }
            },
            "required": [
              "kind",
              "cellRef",
              "formula"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "sortTableRows"
              },
              "cellRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "colIdx": {
                "type": "integer",
                "minimum": 0,
                "maximum": 200
              },
              "order": {
                "enum": [
                  "asc",
                  "desc"
                ]
              },
              "compare": {
                "enum": [
                  "text",
                  "number"
                ]
              },
              "hasHeader": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "cellRef",
              "colIdx",
              "order",
              "compare"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "convertTextToTable"
              },
              "blockRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "separator": {
                "enum": [
                  "tab",
                  "comma"
                ]
              },
              "cellCount": {
                "$ref": "#/$defs/entryCount"
              }
            },
            "required": [
              "kind",
              "blockRef",
              "separator",
              "cellCount"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "convertTableToText"
              },
              "cellRef": {
                "type": "string",
                "pattern": "^block:[0-9]+$"
              },
              "separator": {
                "enum": [
                  "tab",
                  "comma"
                ]
              },
              "rowCount": {
                "$ref": "#/$defs/entryCount"
              }
            },
            "required": [
              "kind",
              "cellRef",
              "separator",
              "rowCount"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertEndnote"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "text": {
                "$ref": "#/$defs/text"
              }
            },
            "required": [
              "kind",
              "runRef",
              "text"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setCrop"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              },
              "crop": {
                "type": "object",
                "properties": {
                  "l": {
                    "$ref": "#/$defs/l"
                  },
                  "t": {
                    "$ref": "#/$defs/l"
                  },
                  "r": {
                    "$ref": "#/$defs/l"
                  },
                  "b": {
                    "$ref": "#/$defs/l"
                  }
                },
                "required": [
                  "l",
                  "t",
                  "r",
                  "b"
                ],
                "additionalProperties": false
              }
            },
            "required": [
              "kind",
              "objectRef",
              "crop"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setModel3DRotation"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              },
              "rotation": {
                "type": "object",
                "properties": {
                  "x": {
                    "type": "number"
                  },
                  "y": {
                    "type": "number"
                  },
                  "z": {
                    "type": "number"
                  }
                },
                "required": [
                  "x",
                  "y",
                  "z"
                ],
                "additionalProperties": false
              }
            },
            "required": [
              "kind",
              "objectRef",
              "rotation"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setDrawingTextFit"
              },
              "objectRef": {
                "type": "string",
                "pattern": "^object:[0-9]+:[0-9]+$"
              },
              "mode": {
                "enum": [
                  "none",
                  "resizeShape",
                  "shrinkText"
                ]
              },
              "fontScalePct": {
                "type": "number",
                "minimum": 1,
                "maximum": 100
              }
            },
            "required": [
              "kind",
              "objectRef",
              "mode"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertMergeField"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "name": {
                "$ref": "#/$defs/fontFamily2"
              }
            },
            "required": [
              "kind",
              "runRef",
              "name"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertCitation"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "tag": {
                "$ref": "#/$defs/tag"
              }
            },
            "required": [
              "kind",
              "runRef",
              "tag"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertBibliography"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "entryCount": {
                "$ref": "#/$defs/entryCount"
              }
            },
            "required": [
              "kind",
              "runRef",
              "entryCount"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "refreshBibliography"
              },
              "entryCount": {
                "$ref": "#/$defs/entryCount"
              }
            },
            "required": [
              "kind",
              "entryCount"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "createCitationSource"
              },
              "source": {
                "type": "object",
                "properties": {
                  "tag": {
                    "$ref": "#/$defs/tag"
                  },
                  "type": {
                    "enum": [
                      "book",
                      "article",
                      "website",
                      "report"
                    ]
                  },
                  "authors": {
                    "$ref": "#/$defs/authors"
                  },
                  "corporate": {
                    "type": "string",
                    "maxLength": 255
                  },
                  "title": {
                    "type": "string",
                    "maxLength": 512
                  },
                  "year": {
                    "type": "string",
                    "maxLength": 16
                  },
                  "publisher": {
                    "type": "string",
                    "maxLength": 255
                  },
                  "journal": {
                    "type": "string",
                    "maxLength": 255
                  },
                  "url": {
                    "type": "string",
                    "maxLength": 2048
                  }
                },
                "required": [
                  "tag",
                  "type"
                ],
                "additionalProperties": false
              }
            },
            "required": [
              "kind",
              "source"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "editCitationSource"
              },
              "tag": {
                "$ref": "#/$defs/tag"
              },
              "patch": {
                "type": "object",
                "properties": {
                  "type": {
                    "enum": [
                      "book",
                      "article",
                      "website",
                      "report"
                    ]
                  },
                  "authors": {
                    "$ref": "#/$defs/authors"
                  },
                  "corporate": {
                    "type": "string",
                    "maxLength": 255
                  },
                  "title": {
                    "type": "string",
                    "maxLength": 512
                  },
                  "year": {
                    "type": "string",
                    "maxLength": 16
                  },
                  "publisher": {
                    "type": "string",
                    "maxLength": 255
                  },
                  "journal": {
                    "type": "string",
                    "maxLength": 255
                  },
                  "url": {
                    "type": "string",
                    "maxLength": 2048
                  }
                },
                "required": [],
                "additionalProperties": false
              }
            },
            "required": [
              "kind",
              "tag",
              "patch"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "deleteCitationSource"
              },
              "tag": {
                "$ref": "#/$defs/tag"
              }
            },
            "required": [
              "kind",
              "tag"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setCitationStyle"
              },
              "style": {
                "enum": [
                  "APA",
                  "MLA"
                ]
              }
            },
            "required": [
              "kind",
              "style"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "createBuildingBlock"
              },
              "name": {
                "$ref": "#/$defs/fontFamily2"
              },
              "blocksXml": {
                "type": "string",
                "minLength": 1,
                "maxLength": 2000000
              },
              "category": {
                "type": "string",
                "maxLength": 64
              }
            },
            "required": [
              "kind",
              "name",
              "blocksXml"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertBuildingBlock"
              },
              "runRef": {
                "type": "string",
                "pattern": "^run:[0-9]+$"
              },
              "name": {
                "$ref": "#/$defs/fontFamily2"
              },
              "blockCount": {
                "$ref": "#/$defs/rowIdx"
              }
            },
            "required": [
              "kind",
              "runRef",
              "name",
              "blockCount"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "deleteBuildingBlock"
              },
              "name": {
                "$ref": "#/$defs/fontFamily2"
              }
            },
            "required": [
              "kind",
              "name"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertWatermark"
              },
              "text": {
                "type": "string",
                "minLength": 1,
                "maxLength": 255
              },
              "headerCount": {
                "$ref": "#/$defs/rows"
              },
              "diagonal": {
                "type": "boolean"
              },
              "color": {
                "$ref": "#/$defs/fill"
              },
              "opacity": {
                "type": "number",
                "minimum": 0,
                "maximum": 1
              }
            },
            "required": [
              "kind",
              "text",
              "headerCount"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertPictureWatermark"
              },
              "blobSha": {
                "type": "string",
                "pattern": "^[0-9a-f]{64}$"
              },
              "bytesLen": {
                "type": "integer",
                "minimum": 1
              },
              "ext": {
                "enum": [
                  "png",
                  "jpg",
                  "jpeg",
                  "gif",
                  "bmp",
                  "webp"
                ]
              },
              "naturalWidthPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 20000
              },
              "naturalHeightPx": {
                "type": "number",
                "minimum": 1,
                "maximum": 20000
              },
              "headerCount": {
                "$ref": "#/$defs/rows"
              },
              "iv": {
                "type": "string",
                "pattern": "^[A-Za-z0-9+/]{16}$"
              },
              "washout": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "blobSha",
              "bytesLen",
              "ext",
              "naturalWidthPx",
              "naturalHeightPx",
              "headerCount"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "removeWatermark"
              }
            },
            "required": [
              "kind"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setTitlePage"
              },
              "enabled": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "enabled"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setEvenOddHeaders"
              },
              "enabled": {
                "type": "boolean"
              }
            },
            "required": [
              "kind",
              "enabled"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setPageNumberFormat"
              },
              "fmt": {
                "anyOf": [
                  {
                    "enum": [
                      "decimal",
                      "lowerRoman",
                      "upperRoman",
                      "lowerLetter",
                      "upperLetter"
                    ]
                  },
                  {
                    "type": "null"
                  }
                ]
              },
              "start": {
                "anyOf": [
                  {
                    "type": "integer",
                    "minimum": 0,
                    "maximum": 32767
                  },
                  {
                    "type": "null"
                  }
                ]
              }
            },
            "required": [
              "kind"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setHyphenation"
              },
              "auto": {
                "type": "boolean"
              },
              "zonePt": {
                "anyOf": [
                  {
                    "type": "number",
                    "minimum": 0.1,
                    "maximum": 1584
                  },
                  {
                    "type": "null"
                  }
                ]
              },
              "noCaps": {
                "type": "boolean"
              }
            },
            "required": [
              "kind"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertPageNumberPosition"
              },
              "position": {
                "enum": [
                  "top",
                  "bottom"
                ]
              },
              "align": {
                "enum": [
                  "left",
                  "center",
                  "right"
                ]
              }
            },
            "required": [
              "kind",
              "position",
              "align"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "removePageNumbers"
              }
            },
            "required": [
              "kind"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "insertHeaderFooterPreset"
              },
              "hfKind": {
                "enum": [
                  "header",
                  "footer"
                ]
              },
              "preset": {
                "enum": [
                  "blank",
                  "centeredTitle",
                  "titleAndDate",
                  "threeColumn"
                ]
              }
            },
            "required": [
              "kind",
              "hfKind",
              "preset"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setFootnoteOptions"
              },
              "fmt": {
                "type": "string"
              },
              "start": {
                "type": "integer",
                "minimum": 0
              },
              "restart": {
                "type": "string"
              },
              "pos": {
                "type": "string"
              }
            },
            "required": [
              "kind"
            ],
            "additionalProperties": false
          },
          {
            "type": "object",
            "properties": {
              "kind": {
                "const": "setEndnoteOptions"
              },
              "fmt": {
                "type": "string"
              },
              "start": {
                "type": "integer",
                "minimum": 0
              },
              "restart": {
                "type": "string"
              },
              "pos": {
                "type": "string"
              }
            },
            "required": [
              "kind"
            ],
            "additionalProperties": false
          }
        ]
      }
    }
  },
  "required": [
    "operations"
  ],
  "additionalProperties": false,
  "$defs": {
    "paragraph": {
      "type": "object",
      "properties": {
        "alignment": {
          "enum": [
            "left",
            "center",
            "right",
            "both",
            null
          ]
        },
        "spacingBeforePt": {
          "$ref": "#/$defs/spacingBeforePt"
        },
        "spacingAfterPt": {
          "$ref": "#/$defs/spacingBeforePt"
        },
        "lineMultiple": {
          "anyOf": [
            {
              "type": "number",
              "minimum": 0.1,
              "maximum": 132
            },
            {
              "type": "null"
            }
          ]
        },
        "indentLeftPt": {
          "$ref": "#/$defs/indentLeftPt"
        },
        "indentFirstLinePt": {
          "$ref": "#/$defs/indentLeftPt"
        },
        "keepNext": {
          "type": [
            "boolean",
            "null"
          ]
        },
        "outlineLevel": {
          "$ref": "#/$defs/outlineLevel"
        }
      },
      "required": [],
      "additionalProperties": false
    },
    "chart": {
      "type": "object",
      "properties": {
        "type": {
          "$ref": "#/$defs/type"
        },
        "title": {
          "$ref": "#/$defs/title"
        },
        "categories": {
          "$ref": "#/$defs/categories"
        },
        "series": {
          "$ref": "#/$defs/series"
        },
        "grouping": {
          "enum": [
            "clustered",
            "stacked",
            "percentStacked"
          ]
        }
      },
      "required": [
        "type",
        "categories",
        "series"
      ],
      "additionalProperties": false
    },
    "patch": {
      "type": "object",
      "properties": {
        "bold": {
          "type": "boolean"
        },
        "italic": {
          "type": "boolean"
        },
        "underline": {
          "type": "boolean"
        },
        "strike": {
          "type": "boolean"
        },
        "color": {
          "$ref": "#/$defs/color2"
        },
        "highlight": {
          "type": [
            "string",
            "null"
          ]
        },
        "fontSizePt": {
          "$ref": "#/$defs/fontSizePt"
        },
        "fontFamily": {
          "$ref": "#/$defs/fontFamily"
        },
        "verticalAlign": {
          "enum": [
            "superscript",
            "subscript",
            null
          ]
        },
        "characterStyleId": {
          "$ref": "#/$defs/characterStyleId"
        },
        "clear": {
          "type": "boolean"
        }
      },
      "required": [],
      "additionalProperties": false
    },
    "run": {
      "type": "object",
      "properties": {
        "bold": {
          "type": "boolean"
        },
        "italic": {
          "type": "boolean"
        },
        "underline": {
          "type": "boolean"
        },
        "strike": {
          "type": "boolean"
        },
        "color": {
          "$ref": "#/$defs/color2"
        },
        "highlight": {
          "type": [
            "string",
            "null"
          ]
        },
        "fontSizePt": {
          "$ref": "#/$defs/fontSizePt"
        },
        "fontFamily": {
          "$ref": "#/$defs/fontFamily2"
        },
        "verticalAlign": {
          "enum": [
            "superscript",
            "subscript",
            null
          ]
        },
        "characterStyleId": {
          "$ref": "#/$defs/characterStyleId"
        }
      },
      "required": [],
      "additionalProperties": false
    },
    "top": {
      "type": "object",
      "properties": {
        "style": {
          "$ref": "#/$defs/style"
        },
        "sz": {
          "$ref": "#/$defs/sz"
        },
        "color": {
          "$ref": "#/$defs/color"
        },
        "space": {
          "$ref": "#/$defs/space"
        }
      },
      "required": [
        "style"
      ],
      "additionalProperties": false
    },
    "top2": {
      "anyOf": [
        {
          "$ref": "#/$defs/top3"
        },
        {
          "type": "null"
        }
      ]
    },
    "top3": {
      "type": "object",
      "properties": {
        "style": {
          "$ref": "#/$defs/style"
        },
        "sz": {
          "$ref": "#/$defs/sz"
        },
        "color": {
          "type": "string"
        },
        "space": {
          "$ref": "#/$defs/space"
        }
      },
      "required": [
        "style"
      ],
      "additionalProperties": false
    },
    "series": {
      "type": "array",
      "minItems": 1,
      "maxItems": 24,
      "items": {
        "$ref": "#/$defs/series2"
      }
    },
    "smartArt": {
      "type": "object",
      "properties": {
        "layout": {
          "enum": [
            "process",
            "cycle",
            "hierarchy",
            "list"
          ]
        },
        "items": {
          "$ref": "#/$defs/items"
        }
      },
      "required": [
        "layout",
        "items"
      ],
      "additionalProperties": false
    },
    "authors": {
      "type": "array",
      "maxItems": 20,
      "items": {
        "$ref": "#/$defs/authors2"
      }
    },
    "series2": {
      "type": "object",
      "properties": {
        "name": {
          "$ref": "#/$defs/title"
        },
        "values": {
          "type": "array",
          "maxItems": 100,
          "items": {
            "type": "number"
          }
        }
      },
      "required": [
        "name",
        "values"
      ],
      "additionalProperties": false
    },
    "authors2": {
      "type": "object",
      "properties": {
        "last": {
          "$ref": "#/$defs/fontFamily"
        },
        "first": {
          "type": "string",
          "maxLength": 100
        }
      },
      "required": [
        "last"
      ],
      "additionalProperties": false
    },
    "style": {
      "enum": [
        "single",
        "thick",
        "double",
        "dotted",
        "dashed",
        "dotDash",
        "dotDotDash",
        "thinThickSmallGap",
        "triple",
        "wave",
        "none"
      ]
    },
    "categories": {
      "type": "array",
      "minItems": 1,
      "maxItems": 100,
      "items": {
        "$ref": "#/$defs/title"
      }
    },
    "items": {
      "type": "array",
      "minItems": 1,
      "maxItems": 50,
      "items": {
        "type": "string",
        "maxLength": 500
      }
    },
    "renderedWidths": {
      "type": "array",
      "maxItems": 200,
      "items": {
        "type": "number",
        "minimum": 0,
        "maximum": 20000
      }
    },
    "characterStyleId": {
      "anyOf": [
        {
          "$ref": "#/$defs/characterStyleId2"
        },
        {
          "type": "null"
        }
      ]
    },
    "color": {
      "anyOf": [
        {
          "$ref": "#/$defs/color4"
        },
        {
          "const": "auto"
        }
      ]
    },
    "color2": {
      "anyOf": [
        {
          "$ref": "#/$defs/color4"
        },
        {
          "type": "null"
        }
      ]
    },
    "indentLeftPt": {
      "anyOf": [
        {
          "$ref": "#/$defs/indentLeftPt2"
        },
        {
          "type": "null"
        }
      ]
    },
    "color3": {
      "anyOf": [
        {
          "$ref": "#/$defs/fill"
        },
        {
          "type": "null"
        }
      ]
    },
    "spacingBeforePt": {
      "anyOf": [
        {
          "type": "number",
          "minimum": 0,
          "maximum": 1584
        },
        {
          "type": "null"
        }
      ]
    },
    "outlineLevel": {
      "anyOf": [
        {
          "type": "integer",
          "minimum": 0,
          "maximum": 8
        },
        {
          "type": "null"
        }
      ]
    },
    "type": {
      "enum": [
        "column",
        "bar",
        "line",
        "pie",
        "doughnut",
        "area",
        "scatter"
      ]
    },
    "characterStyleId2": {
      "type": "string",
      "pattern": "^[A-Za-z0-9\\-_]{1,253}$"
    },
    "tag": {
      "type": "string",
      "pattern": "^[A-Za-z0-9_-]{1,64}$"
    },
    "xPx": {
      "type": "number",
      "minimum": -5000,
      "maximum": 20000
    },
    "text": {
      "type": "string",
      "minLength": 1,
      "maxLength": 20000
    },
    "color4": {
      "type": "string",
      "pattern": "^#?[0-9A-Fa-f]{6}$"
    },
    "xPx2": {
      "type": "number",
      "minimum": -5000,
      "maximum": 5000
    },
    "indentLeftPt2": {
      "type": "number",
      "minimum": -1584,
      "maximum": 1584
    },
    "fontFamily": {
      "type": "string",
      "minLength": 1,
      "maxLength": 100
    },
    "fill": {
      "type": "string",
      "pattern": "^[0-9A-Fa-f]{6}$"
    },
    "entryCount": {
      "type": "integer",
      "minimum": 1,
      "maximum": 10000
    },
    "fontFamily2": {
      "type": "string",
      "minLength": 1,
      "maxLength": 64
    },
    "rowIdx": {
      "type": "integer",
      "minimum": 0,
      "maximum": 5000
    },
    "fontSizePt": {
      "type": "number",
      "minimum": 1,
      "maximum": 1638
    },
    "widthPx": {
      "type": "number",
      "minimum": 1,
      "maximum": 5000
    },
    "l": {
      "type": "number",
      "minimum": 0,
      "maximum": 0.99
    },
    "rows": {
      "type": "integer",
      "minimum": 1,
      "maximum": 50
    },
    "top4": {
      "type": "number",
      "minimum": 0,
      "maximum": 720
    },
    "top5": {
      "type": "number",
      "minimum": 0,
      "maximum": 22
    },
    "sz": {
      "type": "number",
      "minimum": 1,
      "maximum": 96
    },
    "space": {
      "type": "number",
      "minimum": 0,
      "maximum": 31
    },
    "title": {
      "type": "string",
      "maxLength": 200
    }
  }
}
```

来源： [`packages/document/tool-wordinweb/src/index.ts`](../packages/document/tool-wordinweb/src/index.ts)

### `word_document_inspect`

检查紧凑的批量文本上下文、overview、详细 story 区间、搜索结果、单个对象或页面几何。

```json
{
  "type": "object",
  "properties": {
    "kind": {
      "enum": [
        "overview",
        "context",
        "read",
        "search",
        "object",
        "spatial",
        "fit"
      ],
      "description": "overview: whole-document shape. context: bulk text for stories. read: one story range. search: find text (requires query). object: one object (requires ref). spatial: page geometry. fit: per-drawing text fit plus page fill — check it after inserting or resizing a drawing, to see whether the drawing's text still fits its box."
    },
    "stories": {
      "type": "array",
      "minItems": 1,
      "maxItems": 100,
      "uniqueItems": true,
      "items": {
        "type": "string",
        "minLength": 1
      },
      "description": "context only"
    },
    "maxBlocks": {
      "type": "integer",
      "minimum": 1,
      "maximum": 200,
      "description": "context or read"
    },
    "maxCharacters": {
      "type": "integer",
      "minimum": 1,
      "maximum": 100000,
      "description": "context or read"
    },
    "include": {
      "type": "array",
      "uniqueItems": true,
      "items": {
        "enum": [
          "bookmarks",
          "objects"
        ]
      },
      "description": "context only"
    },
    "includeEmpty": {
      "type": "boolean",
      "description": "context only"
    },
    "story": {
      "type": "string",
      "minLength": 1,
      "description": "read only"
    },
    "cursor": {
      "type": "object",
      "properties": {
        "value": {
          "type": "string",
          "minLength": 1
        }
      },
      "required": [
        "value"
      ],
      "additionalProperties": false,
      "description": "read only"
    },
    "query": {
      "type": "string",
      "minLength": 1,
      "maxLength": 1000,
      "description": "search only, required"
    },
    "maxResults": {
      "type": "integer",
      "minimum": 1,
      "maximum": 500,
      "description": "search only"
    },
    "ref": {
      "type": "string",
      "pattern": "^(object|view):",
      "description": "object only, required"
    },
    "pages": {
      "type": "object",
      "properties": {
        "start": {
          "type": "integer",
          "minimum": 1
        },
        "count": {
          "type": "integer",
          "minimum": 1,
          "maximum": 100
        }
      },
      "required": [
        "start",
        "count"
      ],
      "additionalProperties": false,
      "description": "spatial or fit"
    },
    "includeOverlaps": {
      "type": "boolean",
      "description": "spatial only"
    }
  },
  "required": [
    "kind"
  ],
  "additionalProperties": false
}
```

来源： [`packages/document/tool-wordinweb/src/index.ts`](../packages/document/tool-wordinweb/src/index.ts)

### `word_document_new`

新建空白 DOCX。word_document_compose 之前必须调用（compose 只在全新空白文档上工作）。

```json
{
  "type": "object",
  "properties": {},
  "additionalProperties": false
}
```

来源： [`packages/document/tool-wordinweb/src/index.ts`](../packages/document/tool-wordinweb/src/index.ts)

### `word_document_open`

从磁盘加载已有 .docx 并替换当前文档。返回文档 overview。

```json
{
  "type": "object",
  "properties": {
    "path": {
      "type": "string",
      "description": "File path of the .docx to open. Relative paths resolve against the configured workspace."
    }
  },
  "required": [
    "path"
  ],
  "additionalProperties": false
}
```

来源： [`packages/document/tool-wordinweb/src/index.ts`](../packages/document/tool-wordinweb/src/index.ts)

### `word_document_patch`

应用针对投影窗口编写的行范围编辑或统一 diff。

```json
{
  "type": "object",
  "properties": {
    "revision": {
      "type": "string",
      "minLength": 1
    },
    "story": {
      "type": "string",
      "minLength": 1
    },
    "mode": {
      "enum": [
        "text",
        "md",
        "outline"
      ]
    },
    "cursor": {
      "type": "object",
      "properties": {
        "value": {
          "type": "string",
          "minLength": 1
        }
      },
      "required": [
        "value"
      ],
      "additionalProperties": false
    },
    "maxBlocks": {
      "type": "integer",
      "minimum": 1,
      "maximum": 2000
    },
    "maxCharacters": {
      "type": "integer",
      "minimum": 1,
      "maximum": 200000
    },
    "edits": {
      "type": "array",
      "minItems": 1,
      "maxItems": 100,
      "items": {
        "type": "object",
        "properties": {
          "startLine": {
            "type": "integer",
            "minimum": 1
          },
          "endLine": {
            "type": "integer",
            "minimum": 1
          },
          "newText": {
            "type": "string",
            "maxLength": 100000
          }
        },
        "required": [
          "startLine",
          "endLine",
          "newText"
        ],
        "additionalProperties": false
      }
    },
    "diff": {
      "type": "string",
      "minLength": 1,
      "maxLength": 1000000
    },
    "suggest": {
      "type": "boolean"
    }
  },
  "required": [],
  "additionalProperties": false
}
```

来源： [`packages/document/tool-wordinweb/src/index.ts`](../packages/document/tool-wordinweb/src/index.ts)

### `word_document_project`

将 story 投影为带行锚点映射的确定性文本或结构化 markdown，按游标分窗。

```json
{
  "type": "object",
  "properties": {
    "story": {
      "type": "string",
      "minLength": 1
    },
    "mode": {
      "enum": [
        "text",
        "md",
        "outline"
      ]
    },
    "cursor": {
      "type": "object",
      "properties": {
        "value": {
          "type": "string",
          "minLength": 1
        }
      },
      "required": [
        "value"
      ],
      "additionalProperties": false
    },
    "maxBlocks": {
      "type": "integer",
      "minimum": 1,
      "maximum": 2000
    },
    "maxCharacters": {
      "type": "integer",
      "minimum": 1,
      "maximum": 200000
    }
  },
  "additionalProperties": false
}
```

来源： [`packages/document/tool-wordinweb/src/index.ts`](../packages/document/tool-wordinweb/src/index.ts)

### `word_document_save`

将当前文档写入 .docx 文件并返回保存后的绝对路径。

```json
{
  "type": "object",
  "properties": {
    "path": {
      "type": "string",
      "description": "Target .docx path. Relative paths resolve against the configured workspace; omit for a timestamped default name."
    }
  },
  "additionalProperties": false
}
```

来源： [`packages/document/tool-wordinweb/src/index.ts`](../packages/document/tool-wordinweb/src/index.ts)

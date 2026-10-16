# wordinweb 汉化方案（调研 + 设计）

日期：2026-10-16 · 状态：待评审

## 一、调研结论

### 1. wordinweb 是什么，DSH 怎么用它

wordinweb（github.com/theRealestAEP/wordinweb，MIT）是开源的高保真 .docx 渲染/编辑组件，monorepo 含 core（解析布局引擎）、react（发布为 npm 包 `wordinweb`：`DocxView` 渲染器 + `DocxToolbar` ribbon 工具栏）、agent（模型侧编辑工具包）、collab、server。

DSH 中两个 UI 消费点，都在 `packages/client/ui-sidebar-documentpreview/src/client/wordinweb/`：

| 入口 | 组件 | 用法 |
|---|---|---|
| `WordinwebBody.tsx`（.docx 预览） | `DocxView` | 只读渲染，几乎无自带文案 |
| `WordinwebEditTab.tsx`（编辑页） | `DocxToolbar mode="advanced"` + `DocxView editable` | 未传 `features` 裁剪 → **整条 ribbon 全功能暴露** |

另有 `packages/document/tool-wordinweb` 依赖 `@wordinweb/agent`，以 `link:../../../../wordinweb/packages/agent` 指向本地检出（D:\agworkspace\docgc\wordinweb）——模型侧工具，不涉及 UI 汉化。

DSH 宿主层文案（保存/下载/加载失败/会话提示）已经走 typed locale（`sidebarWordinweb` 命名空间，zh/en 双字典）——**宿主层已汉化完毕**。

### 2. 未汉化的面 = wordinweb 包内部 UI

- `toolbar.tsx`（7881 行）：ribbon 五个标签页（Home/Insert/Draw/Layout/Review + Format/Table 上下文页）全部文字，约 125 个 `title` tooltip，全部弹层（查找替换、链接、书签、脚注选项、颜色选择器、批注、交叉引用、题注、日期时间…）。
- `index.tsx`（4198 行，DocxView 内嵌 UI）：新建批注、Go to page、目录空态、以及**错误消息**——这些会经 `onError` 冒泡进 DSH 的失败提示（`t('failed', {message})`），目前对中文用户显示英文。
- `collab.tsx`（批注协作面板，~86 条）、`help.tsx`（帮助面板，~98 条英文长文）。
- 粗 grep 唯一字符串：toolbar 1188 / index 307 / help 98 / collab 86，其中混有字体名（Calibri…）、错误码（AbortError）、样式名等不可译项；**实际可译词条估计 400–600 条**。
- 文案含插值模板：`Replaced ${n} matches`、`Insert ${kind} (at the caret)`、`Highlight ${color}`、`${fieldPrefix} start at`。

### 3. 关键事实与约束

- **wordinweb 无任何 i18n 机制**：无 locale prop、无 context、无集中字符串表，全部内联硬编码英文。
- `DocxToolbar` 现有 props：`api, onSave, mode, features, className, style, commentMentions, defaultExpanded, hostShortcuts` —— 无文案入口。
- **本地检出即上游工作副本**：origin = theRealestAEP/wordinweb，main 领先 1 个本地提交（`64152e0 core: expose setUpdateFields…`），团队已直接在其上提交，具备"在上游加 i18n"的现实条件。
- **版本漂移**：npm 最新 `wordinweb@0.3.7` / DSH 安装 0.3.3 / 本地检出 0.3.2。动手前必须先拉平基线。
- DSH 规则：Client UI copy is locale-owned（typed dictionary + `t()`，`verify-client-ui-i18n` 门禁）——DSH 自有文案必须走字典；wordinweb 包内默认英文属第三方组件，经 prop 注入不违规。

## 二、方案：wordinweb 增加 `strings` 字典 prop，DSH 以 zh 字典接入

### 设计要点

**wordinweb 侧（机制，上游化）**

1. 新增 `packages/react/src/strings.ts`：导出完整 `en` 字典与 `DocxStrings` 类型。
   - 参数化词条用**函数式**（如 `replacedMatches: (n: number) => string`），类型安全、免运行时模板解析；`Partial<DocxStrings>` 对函数词条整体替换可接受。
2. `DocxToolbar`、`DocxView`、collab、help 增加 `strings?: Partial<DocxStrings>`；组件内 `{ ...defaultDocxStrings, ...strings }` 一次合并，全部内联文案改为读字典。
   - **缺词条回退英文**：对版本演进安全——上游新增词条未译只露英文，不会 undefined 崩溃。
   - `title` 与 `aria-label` 同源词条一起替换。
3. 分阶段抽取（按用户可见频率排序）：
   - **P1** toolbar 常驻面：五个 tab 名 + ~125 tooltip + 查找替换/链接/批注/颜色等高频弹层。
   - **P2** `index.tsx` 可见状态与错误消息 + `collab.tsx` 批注面板（错误消息汉化后，DSH 失败提示才是完整中文）。
   - **P3** `help.tsx` 帮助长文：翻译工作量大、价值相对低，可最后做或先保留英文。

**DSH 侧（词条归属 + 接入）**

4. zh 词条放 DSH：`ui-sidebar-documentpreview/src/client/wordinweb/` 下新增 `strings.ts`（`satisfies Partial<DocxStrings>`），en 侧不传、吃上游默认——词条跟随上游 key，维护面最小。后续也可把 zh 字典贡献回上游内置（可选）。
5. `WordinwebEditTab` / `WordinwebBody` 按 DSH 当前语言（`LOCALE_IDS` = zh/en）决定是否传入字典。
6. 新词条门禁：DSH 侧加一条轻量测试，断言所传 key 都存在于上游 `DocxStrings` 类型（类型系统天然保证，`satisfies` 即可），并在升级 wordinweb 时人工核对新增英文词条。

### 版本交付路径（两步走）

1. **先拉平基线**：本地检出从 origin/main 更新到 0.3.7 基线（rebase 本地那 1 个提交），避免在旧基线上做抽取。
2. **短期（link）**：本地检出实现 i18n 并构建（react 包 tsup 产出 dist），DSH 把 `wordinweb` 从 `^0.3.3` 改为 `link:../../../../wordinweb/packages/react`（与 `@wordinweb/agent` 同套路），立即可用。
3. **上游化**：向 theRealestAEP/wordinweb 提 PR（机制 + en 抽取；zh 词条可留在 DSH 或一并贡献）；合并发布后 DSH 切回 registry 版本、删除 link。

### 备选方案（不推荐）

- **B. 消费侧 DOM 翻译**（MutationObserver 替换文本/title/aria-label）：零改第三方，但 7881 行工具栏 + 插值模板 + React 重渲染会持续回英文，脆弱且有性能风险；仅作不能改上游时的应急。
- **C. DSH 自建 ribbon 替换 DocxToolbar**：等于复刻整个工具栏，工作量远超汉化本身，放弃。

## 三、测试与验收

- wordinweb 侧：vitest 单测断言字典完备（`DocxStrings` key 集合 === en key 集合）；传入 Partial 字典的渲染冒烟。
- DSH 侧：`wordinweb-edit-session.client.spec.ts` 补一条"zh 字典下工具栏出现中文标签"的渲染断言；按既有套路 `build:lib:client` + `build:web` 后在 Web 端人工核对。
- 人工验收：打开编辑页，逐 tab、逐弹层截图核对；预览页核对错误路径文案。

## 四、工作量估算（单人）

| 项 | 估时 |
|---|---|
| wordinweb i18n 基建（strings.ts + prop 管道 + 合并回退） | 0.5–1 天 |
| toolbar.tsx 词条抽取（P1） | 2–3 天（机械量大） |
| index.tsx + collab.tsx（P2） | 1 天 |
| help.tsx 长文（P3，可砍） | 0.5–1 天 |
| DSH 接入 + 测试 + 截图验收 | 0.5–1 天 |
| **合计** | **约 5–7 天** |

## 五、风险

- 0.3.3→0.3.7 之间上游可能新增词条，抽取必须以拉平后的 main 为基线。
- 函数式插值词条在 `Partial` 合并下只能整体替换，不能覆盖单个参数文案（可接受）。
- link 期间 DSH 构建引入本地路径依赖，需确认 web client 打包（tsdown/vite）解析 link 正常——agent 包已有先例，react 包同理验证一次即可。

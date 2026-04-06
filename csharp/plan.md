# Copilot-only cutover plan

## Goal
Remove Anthropic as a provider and make the application operate as a Copilot-powered coding agent without losing current functionality.

## Phase 1: Provider and client cutover
- Replace the generic client abstraction name so it no longer references Anthropic.
- Remove Anthropic provider branching from startup and config resolution.
- Normalize defaults, config, and doctor output to Copilot-only behavior.
- Remove the Anthropic service implementation and its references.

## Phase 2: Copilot-only product cleanup
- Remove Anthropic-specific constants, environment variables, and model defaults.
- Update CLI/provider text to reflect Copilot-only operation.
- Keep existing tooling and agent loop behavior intact.

## Phase 3: Agent capability hardening
- [x] Preserve richer turn history when tools are used.
- [x] Expand coding-agent tool orchestration where Copilot requires additional nudging.
- [x] Continue validating build and interactive scenarios.

## Phase 4: Editing workflow hardening
- [x] Add a structured patch tool for multi-edit code changes.
- [ ] Strengthen build/test reflection loops after edits.
- [ ] Improve edit verification and recovery when tool-driven changes fail.

---

# Missing features vs. original TypeScript source

The original TypeScript version (`src/`) is a full-featured production application built on React/Ink
for terminal rendering (~1,884 files, ~4.7 M lines).  The C# version is currently a minimal
reference implementation (~51 files, ~3,300 lines).  The sections below catalogue every major gap
and describe the steps needed to close each one.

---

## Gap 1 – Rich Terminal UI (TUI)

**What the TS version has**
The TypeScript application is built on a custom React/Ink terminal rendering engine
(`src/ink/`, 45+ files, 780 KB).  This gives it a full component-based TUI with:
- Flexbox/Yoga layout engine running entirely in the terminal.
- A DOM-like reconciler (incremental updates, no full redraws).
- Built-in terminal I/O, ANSI colour/cursor control, text selection.
- 111 TSX components: `REPL.tsx` (main interactive screen), `Doctor.tsx`,
  `ResumeConversation.tsx`, `FullscreenLayout.tsx`, `GlobalSearchDialog.tsx`,
  `ContextVisualization.tsx`, `FileEditToolDiff.tsx`, `ToolUseLoader.tsx`, and many more.
- A design-system layer (`ThemedBox`, `ThemedText`, `ThemeProvider`, colour tokens).

**What the C# version has**
Only the `Spectre.Console` library is used for basic ANSI markup output.  There is no
interactive layout, no component lifecycle, and no structured UI.

**Steps to implement**
1. Choose a .NET TUI framework that can replace Ink.js:
   - **Terminal.Gui** (open-source, MIT) – most feature-complete, supports layout, modals,
     keyboard events, colour themes.  Recommended.
   - Alternatively, build a thin reactive wrapper on top of `Spectre.Console` + raw `Console`
     streams if a lighter solution is preferred.
2. Create a `ClaudeCode.Tui` project.
3. Implement a `FullscreenLayout` root component (status bar, main content pane, input bar).
4. Implement `ReplScreen` – the main interactive chat screen:
   - Scrollable message list with virtual rendering for large histories.
   - Streaming output (append new tokens as they arrive).
   - Colour-coded user vs. assistant vs. tool messages.
5. Implement a `DoctorScreen` – diagnostics/health-check view.
6. Implement a `ResumeConversationScreen` – pick up a prior session.
7. Add a theme system (light/dark, colour tokens).
8. Wire the new TUI into `Program.cs` so it launches instead of the current plain console loop.

---

## Gap 2 – Keybinding system

**What the TS version has**
`src/keybindings/` (14 files, 176 KB) provides a full keybinding framework:
parser, matcher, resolver, schema validation, user-override loading, and a React hook
`useKeybinding`.  Default bindings cover navigation, editing, commands, and Vim mode.

**What the C# version has**
Nothing.  The CLI accepts only typed text input via `Console.ReadLine()`.

**Steps to implement**
1. Add a `Keybindings/` module inside `ClaudeCode.Tui` (or `ClaudeCode.Core`).
2. Define a `KeyBinding` record: `{ Keys, Action, Description, Category }`.
3. Load default bindings from an embedded JSON resource (mirror `defaultBindings.ts`).
4. Support user-override file (`~/.claude/keybindings.json` or equivalent).
5. Integrate with the TUI input loop so raw `ConsoleKeyInfo` events are dispatched
   to the binding resolver before falling through to text input.
6. Expose a `doctor`-style listing command (`/keybindings`) to display all active bindings.

---

## Gap 3 – Vim mode

**What the TS version has**
`src/vim/` (5 files, 60 KB): full modal editing for the input buffer.
Motions (h/j/k/l, w/b/e, ^/$), operators (d/c/y/>/</z), text objects (w/s/p/t),
and a state machine for Normal/Insert/Visual/etc.

**What the C# version has**
None.

**Steps to implement**
1. Add a `VimMode/` module inside `ClaudeCode.Tui`.
2. Port the state machine from `transitions.ts` to a `VimStateMachine` class
   (states: Normal, Insert, Visual, VisualLine, Replace).
3. Implement motions, operators, and text objects as delegates applied to a `TextBuffer`.
4. Integrate with the keybinding system so `<Esc>` and `i`/`a`/`v` toggle Vim states.
5. Add a config flag (`vim: true/false`) to enable/disable Vim mode.

---

## Gap 4 – Voice / speech-to-text input

**What the TS version has**
A full voice pipeline:
- `src/services/voice.ts` (17 KB) – STT service abstraction.
- `src/services/voiceStreamSTT.ts` (21 KB) – streaming audio via WebRTC.
- `src/services/voiceKeyterms.ts` (3 KB) – keyword detection.
- `src/hooks/useVoice.ts` (45 KB) and `useVoiceIntegration.tsx` (99 KB) – state & UI.

**What the C# version has**
None.

**Steps to implement**
1. Add a `ClaudeCode.Voice` project (optional, feature-flagged).
2. Use `Microsoft.CognitiveServices.Speech` SDK (or `System.Speech.Recognition` for
   offline) to capture microphone input.
3. Implement a `VoiceService` with `StartListening()` / `StopListening()` and
   `IAsyncEnumerable<string>` for transcribed text.
4. Integrate with the TUI input bar: pressing the voice keybinding activates dictation
   and streams transcribed text into the input buffer.
5. Add `--voice` CLI flag to enable the feature.

---

## Gap 5 – Skills system

**What the TS version has**
`src/skills/` (3 files, 60 KB):
- `bundledSkills.ts` – pre-packaged agent skills.
- `loadSkillsDir.ts` – scans a directory for skill definitions.
- `mcpSkillBuilders.ts` – builds skills from MCP resource definitions.

**What the C# version has**
None.

**Steps to implement**
1. Define a `ISkill` interface in `ClaudeCode.Core` (`Name`, `Description`,
   `IReadOnlyList<ITool> Tools`, `string SystemPromptAddendum`).
2. Add a `SkillRegistry` service in `ClaudeCode.Services`.
3. Implement `BundledSkillsProvider` with the same skills present in `bundledSkills.ts`.
4. Implement `DirectorySkillLoader` that reads YAML/JSON skill definitions from
   `~/.claude/skills/`.
5. Wire skills into the agent loop so the system prompt is augmented and skill tools
   are available when a skill is active.

---

## Gap 6 – Plugin system

**What the TS version has**
`src/plugins/` + `src/services/plugins/` – a full plugin infrastructure for extending
the agent with third-party tools, hooks, and commands.

**What the C# version has**
`PluginTypes.cs` (type definitions only) – no actual plugin loading or execution.

**Steps to implement**
1. Define a `IPlugin` interface (`Name`, `Version`, `IReadOnlyList<ITool> Tools`,
   `IReadOnlyList<ICommand> Commands`).
2. Implement a `PluginLoader` that discovers `.dll` plugins via MEF
   (`System.Composition`) from `~/.claude/plugins/`.
3. Add a `PluginRegistry` in `ClaudeCode.Services`.
4. Expose `/plugins` CLI command to list, enable, and disable installed plugins.
5. Integrate with `ToolRegistry` so plugin tools are available during chat.

---

## Gap 7 – MCP (Model Context Protocol) support

**What the TS version has**
Four MCP-related tools (`MCPTool`, `McpAuthTool`, `ListMcpResourcesTool`,
`ReadMcpResourceTool`) plus a dedicated MCP service layer and `mcp` CLI command.

**What the C# version has**
None.

**Steps to implement**
1. Add a `ClaudeCode.Mcp` project (or namespace inside `ClaudeCode.Services`).
2. Implement an `McpClient` that speaks the MCP JSON-RPC protocol over stdio or HTTP.
3. Implement `McpTool`, `McpListResourcesTool`, `McpReadResourceTool` in
   `ClaudeCode.Tools`.
4. Add `McpAuthService` for token/device-code auth flows.
5. Add `/mcp` command to configure and connect MCP servers.

---

## Gap 8 – LSP (Language Server Protocol) integration

**What the TS version has**
`LSPTool` (`src/tools/LSPTool/`) – sends requests to a language server and surfaces
diagnostics, hover info, and completions to the agent.

**What the C# version has**
None.

**Steps to implement**
1. Add an `LspTool` in `ClaudeCode.Tools` that uses `OmniSharp.Extensions.LanguageServer.Client`
   (or similar) to connect to an LSP server for the current workspace.
2. Expose operations: `getDiagnostics`, `getHover`, `getDefinition`, `getReferences`.
3. Wire into the agent loop so the agent can query code intelligence without shelling out.

---

## Gap 9 – Expanded tool set

**What the TS version has** (44 tools total):
`AgentTool`, `AskUserQuestionTool`, `BashTool`, `BriefTool`, `ConfigTool`,
`EnterPlanModeTool`, `ExitPlanModeTool`, `FileEditTool`, `FileReadTool`,
`FileWriteTool`, `GlobTool`, `GrepTool`, `LSPTool`, `ListMcpResourcesTool`,
`MCPTool`, `McpAuthTool`, `NotebookEditTool`, `PowerShellTool`, `REPLTool`,
`ReadMcpResourceTool`, `RemoteTriggerTool`, `ScheduleCronTool` (3 variants),
`SendMessageTool`, `SkillTool`, `SleepTool`, `TaskCompleteTool`, `ThinkTool`,
`TodoReadTool`, `TodoWriteTool`, `WebFetchTool`, `WebSearchTool` and more.

**What the C# version has** (10 tools):
`BashTool`, `FileReadTool`, `FileWriteTool`, `GlobTool`, `GrepTool`,
`FileEditTool`, `MultiEditTool`, `TodoReadTool`, `TodoWriteTool`, `ThinkTool`.

**Steps to implement**
1. `WebFetchTool` – `HttpClient`-based URL fetcher (markdown/plain-text output).
2. `WebSearchTool` – Bing/DuckDuckGo search via HTTP or Copilot search.
3. `AgentTool` – spawn a sub-agent with its own turn loop.
4. `NotebookEditTool` – read/write Jupyter `.ipynb` files.
5. `PowerShellTool` – run PowerShell commands (already natural for .NET).
6. `SleepTool` – `Task.Delay`-based pause for agent loops.
7. `AskUserQuestionTool` – present a question and await interactive answer via TUI.
8. `ConfigTool` – read/write `AppConfig` values at runtime.
9. `ScheduleCronTool` – create/list/delete cron-style scheduled tasks.
10. `SkillTool` – invoke a registered skill (depends on Gap 5).
11. `MCPTool` / `ReadMcpResourceTool` / `ListMcpResourcesTool` (depends on Gap 7).

---

## Gap 10 – Expanded command set

**What the TS version has** (88+ slash/CLI commands):
`advisor`, `brief`, `commit`, `commit-push-pr`, `review`, `security-review`,
`ultraplan`, `config`, `context`, `cost`, `doctor`, `export`, `feedback`,
`help`, `hooks`, `ide`, `init`, `insights`, `keybindings`, `login`, `logout`,
`mcp`, `memory`, `model`, `onboarding`, `permissions`, `plan`, `plugin`,
`pr_comments`, `resume`, `session`, `share`, `skills`, `stats`, `summary`,
`tasks`, `theme`, `upgrade`, `usage`, `version`, `vim`, `voice`, and more.

**What the C# version has** (1 command): `ChatCommand`.

**Steps to implement (priority order)**
1. `/help` – list available commands and keybindings.
2. `/version` – print version and build info.
3. `/model` – view/change the active Copilot model.
4. `/config` – view/edit configuration.
5. `/doctor` – diagnostic health-check screen (mirrors the TS `Doctor.tsx`).
6. `/resume` – list and resume previous sessions.
7. `/session` – show current session info.
8. `/memory` – inspect/manage the agent's memory context.
9. `/commit` – generate a commit message and optionally commit.
10. `/review` – trigger a code-review agent pass.
11. `/plan` – enter plan mode (read-only tool use).
12. `/stats` / `/cost` – display token usage and cost.
13. `/theme` – toggle light/dark theme.
14. `/vim` – toggle Vim mode (depends on Gap 3).
15. `/voice` – toggle voice input (depends on Gap 4).
16. `/plugins` (depends on Gap 6).
17. `/mcp` (depends on Gap 7).
18. `/skills` (depends on Gap 5).

---

## Gap 11 – State management

**What the TS version has**
`src/state/` (6 files, 80 KB): `AppState`, `AppStateStore`, selectors, change
listeners, teammate-view helpers.  Central reactive store.

**What the C# version has**
`AppConfig.cs` – minimal configuration record only.

**Steps to implement**
1. Add an `AppState` class in `ClaudeCode.Core` that aggregates:
   - Active session, conversation history, running tools, task list,
     current model, feature flags, theme.
2. Add an `AppStateStore` singleton service with `GetState()`, `Dispatch(action)`,
   and `IObservable<AppState>` (using `System.Reactive` or a simple event bus).
3. Replace scattered mutable fields in `ChatCommand` with reads from `AppStateStore`.
4. Subscribe the TUI (Gap 1) to state changes for reactive rendering.

---

## Gap 12 – Task system

**What the TS version has**
`src/tasks/` (8 files, 56 KB): `LocalMainSessionTask`, `LocalAgentTask`,
`LocalShellTask`, `InProcessTeammateTask`, `RemoteAgentTask`, `DreamTask`,
`pillLabel`, `stopTask`.

**What the C# version has**
Nothing.

**Steps to implement**
1. Define a `ITask` interface in `ClaudeCode.Core`
   (`Id`, `Status`, `Label`, `CancellationToken`, `RunAsync()`).
2. Implement `MainSessionTask` – wraps the primary chat/agent loop.
3. Implement `AgentTask` – sub-agent execution (depends on `AgentTool`).
4. Implement `ShellTask` – fire-and-forget shell command.
5. Add `TaskManager` service (register, cancel, list tasks).
6. Expose `/tasks` command to list running/completed tasks.

---

## Gap 13 – Session persistence & resume

**What the TS version has**
`src/history.ts`, `src/screens/ResumeConversation.tsx`, and the `/resume` command
allow saving and reloading full conversation histories including tool results.

**What the C# version has**
In-memory history only; lost on exit.

**Steps to implement**
1. Add a `SessionStore` service that serialises `ConversationHistory` to
   `~/.claude/sessions/<id>.json` at configurable intervals and on exit.
2. Add a `SessionIndex` that records session metadata (timestamp, summary, model).
3. Implement `ResumeConversationScreen` in the TUI to browse and reopen sessions.
4. Wire into the `/resume` command.

---

## Gap 14 – Analytics & diagnostics

**What the TS version has**
`src/services/analytics/`, `src/services/diagnosticTracking.ts`,
`src/services/internalLogging.ts`, and a `Doctor` screen with live
health checks.

**What the C# version has**
Basic `ILogger` wiring only.

**Steps to implement**
1. Add a `DiagnosticsService` that checks: Copilot auth status, model availability,
   tool availability, disk space, .NET runtime version.
2. Implement the `DoctorScreen` TUI view (depends on Gap 1).
3. Add structured usage logging (tokens per session, tool call counts) to a local
   `~/.claude/usage.jsonl` file.
4. Add `/stats` command to display aggregated usage.

---

## Phased roadmap summary

| Phase | Focus areas | Priority gaps |
|-------|-------------|---------------|
| **5** | TUI foundation + keybindings | Gap 1, Gap 2 |
| **6** | Expanded tools + state management | Gap 9, Gap 11 |
| **7** | Expanded commands + session persistence | Gap 10, Gap 13 |
| **8** | Vim mode + skills + plugins | Gap 3, Gap 5, Gap 6 |
| **9** | MCP + LSP + task system | Gap 7, Gap 8, Gap 12 |
| **10** | Voice + analytics | Gap 4, Gap 14 |

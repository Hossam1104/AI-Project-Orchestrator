# APO-34 — SOL ACCEPTANCE CHECKPOINT

**Story:** APO-34 — First Usable AI Capacity Workspace

**Branch:** `feat/APO-34-ai-capacity-workspace`

**Exact baseline:** `a585ed40ea0e8652c50e4627ee66f7109c67d591`

**Base:** `main` and `origin/main` were verified at the exact baseline before implementation.

**Authority:** GPT-5.6 Sol is the acceptance authority. This checkpoint is not a merge approval.

## Delivered scope

APO-34 implements the first usable local WPF workspace for AI capacity decisions while staying
inside the approved C# / .NET 10 / WPF / MVVM / JSON / Credential Manager architecture.

- The bounded shell contains Overview and AI Capacity. AI Capacity is the initial workspace;
  Overview remains navigable. Projects, Agents, and Activity are visibly disabled/planned.
- AI Capacity shows exactly Codex, Claude / Anthropic, Kimi, GitHub Copilot, and Antigravity in
  stable enum order.
- Cards distinguish configured, local-detected, unsupported/manual, authentication-required,
  error, and stale states. One provider failure does not hide another provider.
- Capacity cards render arbitrary quota windows, remaining percentage as remaining, usage-only
  values without invented limits or progress, local reset times, source/confidence, and explicit
  unavailable values.
- Refresh All and per-provider refresh are asynchronous, isolated, cancellation-aware, and
  protected against overlapping refreshes.
- Copilot connection editing supports PersonalUser and Organization scope; personal username is
  optional and organization is required for organization scope.
- Claude/Anthropic editing is explicitly labelled as the organization Admin API channel and does
  not conflate it with Claude consumer subscription capacity.
- Kimi editing defaults to `http://127.0.0.1:58627/` and validates absolute loopback HTTP/HTTPS
  before credential retrieval or HTTP use.
- Connection persistence stores only non-secret configuration plus an opaque credential reference.
  Credential replacement stages the new secure value, persists the JSON reference atomically,
  removes the staged value on persistence failure, and removes the previous value only after the
  new state is committed. Removing a credential persists the null reference before cleanup.
- Startup loading and saves update immutable runtime settings snapshots used by the already
  registered provider adapters on the next refresh without restart. Providers capture one
  complete snapshot per refresh.
- The connection editor never reads saved secret material back; a saved credential is represented
  only by truthful “credential saved” state.

## Required validation completed

| Check | Result |
|---|---|
| Restore | `dotnet restore AIUsageMonitor.sln` — SUCCESS |
| Baseline tests | **123/123** — Domain 28, Provider 45, Infrastructure 50 |
| New desktop tests | **12/12** |
| New connection transaction tests | **3/3** |
| Solution/WPF test build | SUCCESS; x64 desktop test mapping; 0 warnings, 0 errors in final build/test pass |
| Self-contained publish | SUCCESS for `win-x64`, `win-x86`, and `win-arm64`; single-file, trimmed false |
| x64 runtime smoke | SUCCESS; published executable launched and rendered AI Capacity |
| Runtime evidence | [APO-34-ai-capacity-workspace.png](docs/evidence/APO-34-ai-capacity-workspace.png) inspected |
| Secret review | No real credentials; only sanitized synthetic fixture text in tests |
| Live provider calls | NOT RUN; no authenticated credentials or live calls are allowed in CI/validation |

## Files and areas changed

- Application provider connection service, edit model, configuration keys, identity catalog seam,
  and runtime-settings updater contract.
- Domain provider connection configuration and truthful `NotConfigured` status.
- Infrastructure connection record mapping, repository/service DI registration, and secure-save
  transaction tests in the separate connection test project.
- Providers runtime snapshot accessor and live settings injection for Claude, Copilot, and Kimi.
- Desktop MVVM workspace, capacity cards, quota presentation, navigation, connection editor,
  startup compatibility guard, and WPF resources/bindings.
- Solution configuration for x64 WPF test execution and new focused test projects.
- Runtime screenshot evidence and this handoff metadata.

## Explicitly out of scope

No new provider, speculative provider endpoint, browser-cookie extraction, conversation/prompt
collection, AI chat, routing/orchestration runtime, Jira/GitHub adapter, project registry,
database/ORM, cloud backend, Node/Angular/Electron/Tauri/embedded browser, LocalAppData migration,
installer/signing/updater, or full product release qualification was added.

## Review and delivery boundary

- Do not invoke Claude Opus for this work item.
- Do not create or update Jira from this executor checkpoint.
- Do not merge, rebase, force-push, modify `main`, or create a second PR.
- The executor must commit and push this feature branch, create exactly one Draft PR against
  `main`, and stop for GPT-5.6 Sol acceptance.
- Sol may request bounded remediation or accept the feature. No next Story is authorized by this
  file.

**Expected next planner boundary:** GPT-5.6 Sol acceptance of APO-34 against the pushed feature
branch and Draft PR, with the implementation intentionally unmerged.

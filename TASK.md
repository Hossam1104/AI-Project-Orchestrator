# APO-35 OPUS PROMPT 5/5 FINDINGS REMEDIATION — PROMPT 1/5 HANDOFF

Story: APO-35 — Implement First Usable Projects Workspace and Project Registry Management

Epic: APO-5 — Project Registry & Workspace Management

Exact main base: `34569abee50bdb708770e134e9db7db18752a80d`

Feature branch: `feat/APO-35-projects-workspace`

Draft PR: [#6](https://github.com/Hossam1104/AI-Project-Orchestrator/pull/6), OPEN / DRAFT /
UNMERGED

Pre-remediation branch head: `bad6380` (`docs/evidence: complete APO-35 Prompt 4 validation`)

Final branch SHA: recorded in the executor completion report after this documentation commit is
pushed

## Scope authority

Claude Opus 5 independently reviewed the APO-35 Projects UI (Opus cadence Prompt 5/5) and returned
verdict `CHANGES REQUIRED` against eight findings: `OPUS-01`..`OPUS-04` (P2) and `OPUS-05`..`OPUS-08`
(P3). GPT-5.6 Sol adjudicated the review in Jira comment `11838`, accepted `OPUS-01`..`OPUS-04` as
blocking, deferred `OPUS-05`..`OPUS-08`, and assigned this bounded remediation to Claude Sonnet 5 as
Prompt 1/5 of a new Opus cadence. Opus itself was not invoked in this Prompt. These `OPUS-01`..
`OPUS-04` codes are a new cycle scoped to the Projects UI, unrelated to the differently-scoped
`OPUS-01`..`OPUS-08` codes already recorded elsewhere in `.ai/CURRENT_STATE.md` for the earlier
APO-27 storage-durability review.

## OPUS-01 — registry ListBox scroll region (P2, BLOCKING — FIXED)

**Root cause.** The registry `ListBox` lived inside an unbounded `StackPanel`, which gives children
infinite available height, so the list never received a bounded region to scroll within.

**Fix.** `src/AIUsageMonitor.Desktop/MainWindow.xaml`: replaced the enclosing `StackPanel` with a
`Grid` (`Auto`/`Auto`/`Auto`/`*` rows for search box, status filter, empty/no-match messaging, and
the list); the `ListBox` now occupies the trailing star row with
`ScrollViewer.VerticalScrollBarVisibility="Auto"`.

**Related regression found and fixed in the same change.** Verifying this fix against the real
published `win-x64` executable surfaced a previously dormant rendering defect: with the registry
empty, the `ListBox`'s default theme chrome rendered an opaque white fill behind the "No projects
yet" message instead of the transparent background already set on the control. This was invisible
before the `OPUS-01` fix because the empty `ListBox` previously collapsed to near-zero height inside
the old `StackPanel` and never rendered a visible area. Confirmed as a genuine on-screen rendering
defect (reproduced identically via both `PrintWindow` and `CopyFromScreen` capture of the real
executable) and confirmed absent from a pure in-process `RenderTargetBitmap` render of the identical
visual tree — i.e. specific to the real GPU/theme-composited render path, not visible from reading
XAML alone. Fixed with a new `ProjectRegistryListBoxStyle` in `MainWindow.xaml` that replaces the
`ListBox`'s default control template with an explicit `Border`/`ScrollViewer`/`ItemsPresenter`
template guaranteeing a transparent background regardless of the resolved theme.

## OPUS-02 — 860x580 minimum window contract (P2, BLOCKING — FIXED)

**Root cause.** The registry/editor column `MinWidth`s (`235` / spacer `18` / `330`) summed, with
window chrome/padding, to exceed the declared `860x580` minimum window contract, clipping content.

**Fix.** `MainWindow.xaml`: reduced the three registry `Grid.ColumnDefinitions` `MinWidth`s to `200`
(left) / `16` (spacer) / `295` (right). `Window.MinWidth`/`MinHeight` were left unchanged at
`860`/`580` as required. Verified against the real published executable resized to exactly
`860x580`: the editor's right edge, all field labels, and `Cancel`/`Save changes` remain visible
with no horizontal clipping.

## OPUS-03 — status ComboBox dark theming/contrast (P2, BLOCKING — FIXED)

**Root cause.** Both Projects status `ComboBox`es (status filter and editor status) used a manually
styled `Style` with only brush/font setters and no custom `ControlTemplate`, so the closed box,
dropdown popup, and item chrome fell back to default light WPF chrome with inadequate contrast
against the dark shell.

**Fix.** New `BrandComboBoxStyle`/`BrandComboBoxItemStyle` in
`src/AIUsageMonitor.Desktop/Resources/Controls.xaml`, built from existing brand brushes only
(`BrandNavyBlueBrush`, `BrandTextBrush`, `BrandBorderBrush`, `BrandCyanBrush`, `BrandFocusBrush`,
`BrandSurfaceRaisedBrush`, `BrandMutedTextBrush`), covering closed/hover/open/focus/disabled states
and a themed popup with hover/selection item states. `MainWindow.xaml`'s
`ProjectInputComboBoxStyle` now reads `BasedOn="{StaticResource BrandComboBoxStyle}"` so both
Projects `ComboBox`es pick up the new template.

## OPUS-04 — search/filter rebuild loses selection (P2, non-blocking, fixed per Sol instruction — FIXED)

**Root cause.** `ApplyFilter()` rebuilds a fresh `ProjectCardViewModel` for every visible project on
every keystroke. WPF's `Selector` requires `SelectedItem` to be reference-equal to an instance
present in `ItemsSource`; the rebuild always broke that reference, silently clearing the UI selection
even when the selected project still matched the active filter.

**Fix.** `src/AIUsageMonitor.Desktop/ViewModels/ProjectsViewModel.cs`: `ApplyFilter()` now captures
the selected project's id before rebuilding `FilteredProjects`, then re-resolves
`SelectedProjectCard` to the matching new instance by id (or `null` if it no longer matches).
`ReplaceProject(saved)` was changed the same way — it selects the exact new instance from
`FilteredProjects` by id, or `null` if the saved project fell out of the active filter, and no
longer constructs a detached card outside the bound collection.

**New regression tests** (`tests/AIUsageMonitor.Desktop.Tests/ProjectsWorkspaceTests.cs`):
`SearchPreservesSelectionWhenSelectedProjectStillMatches`,
`StatusFilterPreservesSelectionWhenProjectStillMatches`,
`SearchClearsSelectionWhenProjectNoLongerMatches`,
`StatusFilterClearsSelectionWhenProjectNoLongerMatches`,
`SavedProjectSelectionUsesFilteredCollectionInstance`,
`ChangingProjectStatusSoItLeavesCurrentFilterClearsSelection`.

## Tests

Full solution: 225 executed, 225 passed, 0 failed, 0 skipped — Domain 28, Provider 46,
Infrastructure 86, Connection 10, Desktop 55 (up from 219 total / 49 Desktop; +6 new OPUS-04
regression tests).

## Build / publish / runtime verification

- `dotnet build AIUsageMonitor.sln --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test AIUsageMonitor.sln --no-restore`: 225/225 passed.
- `win-x64`: self-contained single-file publish passed, rebuilt after the `OPUS-01` follow-on
  `ListBox` template fix.
- `win-x86` / `win-arm64`: not rerun; no project/package/publish configuration changed.
- `git diff --check`: clean (benign LF/CRLF notices only).
- Diff secret-pattern scan (key/secret/token/password/credential/api-key): clean.

**Real published executable (`win-x64`), both required sizes.** Windows UI Automation drove the
real published executable (Projects nav, then "New project") with no synthetic data written to the
owner's real LocalAppData registry. Capture used `PrintWindow` with `PW_RENDERFULLCONTENT`, cropped
to the true visible window bounds via `DwmGetWindowAttribute(DWMWA_EXTENDED_FRAME_BOUNDS)` to
exclude the invisible DWM resize-border margin. `CopyFromScreen`/`SetForegroundWindow` was evaluated
and rejected: `SetForegroundWindow` was not reliably honored from the automating process in this
environment, and a `CopyFromScreen` attempt was, at least once, observed to capture whichever
unrelated window actually had focus instead — that leaked image was deleted immediately and never
written anywhere durable, committed, or shown as evidence. `PrintWindow` reads directly from the
target window's own handle regardless of foreground/z-order state and does not have this failure
mode, so it is the method actually used for both evidence screenshots below. At `1180x760` and at
the exact `860x580` minimum-window-contract size, the real executable shows a real empty registry
("0 total", "No projects registered yet."), a fully visible New Project editor with all fields
blank, `Cancel`/`Save changes` reachable, no horizontal clipping at `860x580`, and the corrected dark
`ComboBox` chrome. Both images were inspected before commit and contain no real project data, paths,
repository URLs, tracker ids, or credentials.

- `docs/evidence/APO-35-projects-workspace.png` (`1180x760`, recaptured)
- `docs/evidence/APO-35-projects-workspace-minimum.png` (`860x580`, new)

**Populated-list scroll proof (`OPUS-01`), without touching the real registry or adding a production
demo mode.** A temporary, non-shipped, non-committed WPF harness (scratchpad-only) constructed the
real `MainWindow`/`MainWindowViewModel`/`ProjectsViewModel`/`ProjectRegistryService` production
classes wired to a sanitized in-memory `IProjectRepository` fake seeded with 40 fake
`Sample Workspace NN` projects (fake `C:\Sample\WorkspaceNN` paths). Nothing in this object graph
touches the real registry implementation. Rendered in-process via `RenderTargetBitmap` at both
`1180x760` and `860x580`, proving the list is bounded to its `Grid` star row, shows `40 total`, and
exposes a working vertical scrollbar rather than expanding without limit.

- `docs/evidence/APO-35-registry-scroll-proof-1180x760.png` (supplementary)
- `docs/evidence/APO-35-registry-scroll-proof-860x580.png` (supplementary)

## Markdown files changed

- `.ai/CURRENT_STATE.md` — new `-3c` section recording the Opus Prompt 5/5 verdict, Sol comment
  `11838`, and this Prompt 1/5 `OPUS-01`..`OPUS-04` remediation.
- `TASK.md` — this handoff (replaces the prior Prompt-4 delta-acceptance handoff).
- `README.md` — updated Desktop/full-suite test counts (55 / 225).
- `docs/IMPLEMENTATION_PLAN.md` — new note recording the Opus Prompt 5/5 review and this
  remediation.
- `AGENTS.md`, `CLAUDE.md`, `docs/BRD.md`, `docs/SESSION_PROMPTS.md`,
  `docs/LEGACY_IMPLEMENTATION_MAP.md`, and `docs/APO-31_PROVIDER_EVIDENCE.md` reviewed and
  intentionally unchanged.

## Jira

- One comment posted to APO-35: functional commit SHA, final branch SHA, `OPUS-01`..`OPUS-04`
  resolution (including the `ListBox` regression fix), full test results, build, publish, both
  required runtime evidence screenshot paths, populated-scroll proof paths and methodology, P3
  deferrals, "Prompt 1/5", next gate.
- No Jira status transition was made. APO-35, APO-5 remain unchanged.

## Out-of-scope confirmation

`OPUS-05`..`OPUS-08` (P3) not implemented. No rebase/force-push/reset/merge-main. PR #6 left
OPEN/DRAFT/UNMERGED and not marked ready for review. APO-35/APO-5 not transitioned. No other Story
started. No Claude Opus invocation. No Application-layer change. No new provider/routing/execution/
orchestration/database/cloud-backend behavior.

Opus cadence: Prompt 1/5 of the new cycle complete.

Next gate: GPT-5.6 Sol delta acceptance of this remediation against the exact final pushed branch
SHA. Do not invoke Opus, merge, or start another Story from this checkpoint.

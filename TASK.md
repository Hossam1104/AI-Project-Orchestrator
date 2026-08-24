# APO-37 SOL ACCEPTANCE HANDOFF

## Story / scope

- Story: APO-37 — Implement Read-Only Local Git Repository Verification in Projects
- Epic: APO-6 — Git & GitHub Integration
- Exact main base: `8a81017b25fe0cfd8efcd4febafd66a1bee6c41e`
- Branch: `feat/APO-37-local-git-verification`
- Draft PR: [#7](https://github.com/Hossam1104/AI-Project-Orchestrator/pull/7), OPEN / DRAFT / UNMERGED
- Functional SHA: `3fa4791641e13383cca3ac36ef4e632045bf2704`
- Documentation / handoff SHA: `e30581d36bd9273dfc7477287c8f16ce1bd33eb7`
- Final branch SHA: recorded in the executor completion report after metadata synchronization
- Opus cadence: Prompt 3/5
- Opus review: NOT PERFORMED; Claude Opus was not invoked
- Next gate: GPT-5.6 Sol acceptance

## Application contract / model

Application owns provider-independent repository verification semantics:

- `RepositoryVerificationStatus`: NotInspected, PathMissing, PathUnavailable, GitUnavailable,
  NotGitRepository, AvailableClean, AvailableDirty, Failed.
- `RepositoryChangedFile` with repository-relative path, old path when a rename is known, and
  staged/modified/deleted/renamed/untracked/conflicted flags.
- `RepositoryRemote` with name and sanitized URL only.
- `RepositoryRemoteComparison`: NotConfigured, NoLocalRemote, Match, Different,
  ComparisonUnavailable.
- `LocalRepositoryInspection` for Infrastructure output and `RepositoryStateSnapshot` carrying
  the selected ProjectId through publication.
- `ILocalRepositoryInspector` as the Infrastructure boundary.
- `IProjectRepositoryStateService` and `ProjectRepositoryStateService` for project-aware use-case
  semantics and registered URL comparison.

The model preserves absent values as null/unavailable. It supports repository root relationship,
branch, detached HEAD, HEAD SHA/short SHA, upstream, clean/dirty state, bounded changed files,
sanitized remotes, capture time, and safe bounded failure text. Empty/unborn repositories remain
valid repositories with a known branch where available and no fabricated HEAD SHA.

## Infrastructure Git process architecture

`GitLocalRepositoryInspector` and `SystemGitCommandRunner` are Infrastructure-only. Desktop/WPF
does not call `Process.Start`, inspect `.git`, know executable paths, parse porcelain, read file
contents, or depend on Provider `IExecutableLocator`.

The runner uses `System.Diagnostics.Process` / `ProcessStartInfo` with:

- `UseShellExecute = false`;
- `ArgumentList`, with the user-controlled LocalPath as one argument;
- `GIT_TERMINAL_PROMPT=0`;
- `GIT_OPTIONAL_LOCKS=0`;
- asynchronous stdout/stderr handling;
- cancellation-aware process-tree termination; and
- a deterministic ten-second timeout per Git command.

## Exact production read-only Git commands

Production APO-37 uses only:

1. `git --version`
2. `git -C <path> rev-parse --show-toplevel`
3. `git -C <path> symbolic-ref --quiet --short HEAD`
4. `git -C <path> rev-parse --verify HEAD`
5. `git -C <path> rev-parse --abbrev-ref --symbolic-full-name @{upstream}`
6. `git -C <path> status --porcelain=v1 -z --untracked-files=all`
7. `git -C <path> remote -v`

No fetch, pull, push, checkout, switch, reset, clean, add, commit, merge, rebase, stash, branch
mutation, tag mutation, config write, remote mutation, worktree mutation, submodule mutation,
`git ls-remote`, GitHub API, or other network operation is executed by production code.

## Path, repository, and state truthfulness

- Missing path is distinct from inaccessible path and from a non-repository directory.
- A file registered as LocalPath is unavailable, not silently treated as missing.
- A valid repository may be clean, dirty, detached, or unborn.
- Detached HEAD exposes `IsDetachedHead=true` and does not fabricate a branch.
- Unborn repositories expose no fabricated HEAD SHA and may still expose branch/untracked state.
- Non-repository paths are `NotGitRepository`, not generic verification failures.
- Timeout and unexpected failures are bounded as `Failed` with non-secret user-facing text.

## Repository metadata and remote safety

The snapshot exposes repository root, whether the registered LocalPath equals that root using
Windows case-insensitive path comparison, branch, detached state, full/short HEAD, upstream,
working-tree state, capture time, configured remote names/URLs, and conservative comparison to the
registered RepositoryUrl. Remote comparison never claims reachability, connectivity, sync, health,
or freshness. SSH/HTTPS equivalents are compared only where a safe host/path key is available;
uncertain cases are `ComparisonUnavailable`.

Remote URI userinfo, passwords, tokens, query strings, and fragments are removed before state or
UI display. Unparseable values become a bounded `Remote URL unavailable` value. Raw `git remote -v`
output and raw stderr are not logged or shown.

## Changed-file parsing and bounds

`status --porcelain=v1 -z` is parsed without reading file contents, running diff, or rendering a
patch. Staged, modified, deleted, renamed, untracked, and conflicted categories are normalized.
Rename old/new paths are preserved where Git supplies them. Paths are repository-relative and each
path is bounded to 512 characters. At most 100 entries are exposed; `ChangedFileTotal` and
`ChangedFilesTruncated` make the bounded list explicit to the UI.

## Desktop UI and project isolation

The selected-project detail surface contains a Repository Verification card with accessible
`Verify repository` and `Refresh repository state` buttons. It shows Not inspected, Verifying
repository…, Local path missing, Local path unavailable, Git unavailable, Not a Git repository,
Repository verified — clean, Repository verified — changes present, and Repository verification
failed. Known details include branch, HEAD, upstream, root relationship, root path, working tree,
configured sanitized remotes, comparison state, capture time, and bounded changed-file status.

Selection does not auto-run Git. Each selected project starts at Not inspected. Selection change
cancels the previous request and increments a generation; late results are rejected unless both
generation and selected ProjectId still match. Editing disables verification, and a successful
project save or registry refresh resets repository evidence to Not inspected.

The changed-files list is bounded, scrollable, keyboard/focus accessible, and status text is shown
alongside supplemental color. The existing 1180×760 and 860×580 shell minimum-size contract is
preserved.

## Tests

Deterministic Infrastructure coverage includes missing/unavailable paths, Git unavailable, clean
and dirty repositories, tracked/untracked/staged/deleted/renamed/conflicted parsing, detached and
unborn states, missing remotes/upstream, credential/query-bearing remote sanitization, bounded
changes, timeout, cancellation, single-argument paths, no-shell execution, and safe environment
variables.

Projects workspace coverage includes initial Not inspected state, command predicates, clean/dirty
presentation, truthful unavailable states, bounded errors, selection cancellation and late-result
protection, successful-edit reset, existing SOL-35-01 edit-target regressions, and existing
selection/filter regressions.

Production composition tests use `AddInfrastructure()`, `AddProviders()`, and
`AddDesktopWorkspaceServices()` and resolve the repository inspector, repository-state service,
ProjectsViewModel, MainWindowViewModel, and normal AI Capacity view model from the real container.

Current executed totals:

- Domain: 28 passed
- Provider: 46 passed
- Infrastructure: 101 passed
- Connection: 10 passed
- Desktop: 70 passed
- Full solution: 255 passed, 0 failed, 0 skipped
- Baseline before APO-37: 225 / 225

## Build / publish / runtime evidence

- `dotnet restore AIUsageMonitor.sln`: succeeded
- `dotnet build AIUsageMonitor.sln --no-restore`: 0 warnings, 0 errors
- `dotnet test AIUsageMonitor.sln --no-restore`: 255 passed, 0 failed, 0 skipped
- `win-x64` self-contained single-file publish: succeeded; `AIUsageMonitor.Desktop.exe` produced
- Normal-shell runtime smoke: succeeded; published executable remained alive after five seconds
- Temporary sanitized Git repository validation: succeeded; dirty state and sanitized remote evidence captured
- Visual evidence: captured from a real WPF render harness using only the temporary sanitized repository
- `git diff --check`: clean
- Targeted added-line secret scan: no real credentials or owner-private paths found; only deliberate sanitized test fixtures

## Documentation / Jira synchronization

Mutable documentation is synchronized in `.ai/CURRENT_STATE.md`, `README.md`,
`docs/IMPLEMENTATION_PLAN.md`, and this `TASK.md`. The accepted ProjectStatus values are exactly
Active, Paused, Blocked, Archived; accepted status filters are All, Active, Paused, Blocked,
Archived. Draft and Completed are not ProjectStatus values.

Jira status must remain:

- APO-37: In Progress
- APO-6: In Progress

At completion, add one concise APO-37 comment containing branch, functional SHA, final SHA, Draft
PR, architecture, read-only command guarantees, test totals, build/publish, runtime/visual
evidence, known limitations, Prompt 3/5, and the next GPT-5.6 Sol acceptance gate. No duplicate
Story and no status transition are authorized. The Jira comment remains pending if no authenticated
Jira connector is available in the execution environment.

## Out-of-scope confirmation

Git writes, branch creation, checkout/switch, stage/add, commit, reset, clean, stash, merge/rebase,
fetch/pull/push, GitHub authentication/API/PR operations beyond the delivery Draft PR, CI/status
checks, diff/patch rendering, Jira/Azure integration, routing, execution, agent UI, activity,
validation/review/acceptance runtime, orchestration, database/cloud backend, provider coupling,
and deferred APO-35 OPUS-05..OPUS-08 are out of scope.

## Delivery boundary

Work item: APO-37
Status: COMPLETE / awaiting GPT-5.6 Sol acceptance after final evidence and synchronization

Implemented:
- Read-only, manual local Git repository verification across Application, Infrastructure, and WPF.
- Truthful repository states, bounded status evidence, sanitized remotes, cancellation, and project isolation.
- Production DI and deterministic regression coverage.

Validated:
- 255 / 255 full-suite tests passing.
- 0 build warnings and 0 build errors.
- `win-x64` self-contained single-file publish and five-second startup smoke succeeded.
- Sanitized temporary-repository validation and visual evidence completed.

Not validated:
- Jira comment/status synchronization if authenticated Jira delivery tooling is unavailable.

Blockers / limitations:
- No remote reachability or synchronization claim is possible because APO-37 is local-only.
- Jira synchronization requires available authenticated delivery tooling; the requested GitHub Draft
  PR was created as #7 and remains open/draft/unmerged.

Files/areas changed:
- Application Projects repository-state contracts and service.
- Infrastructure Git runner, inspector, sanitization, parsing, and DI.
- ProjectsViewModel, selected-project WPF detail card, composition regression tests, and focused tests.
- Mutable APO handoff documentation.

CURRENT_STATE updated: Yes

Next planner boundary:
- GPT-5.6 Sol acceptance of the exact final pushed feature SHA.

NO Opus review performed.

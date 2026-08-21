# TASK - APO-20 REMEDIATION RECOVERY CHECKPOINT

**Status:** PARTIAL - repository rename complete; physical local-folder rename blocked
**Product:** AI Project Orchestrator (APO)
**Current Work Item:** APO-20 - Rename repository and local project root to AI Project Orchestrator
**Current Epic:** APO-1 - APO Product Rebrand & Governance Rebaseline
**Planner / Acceptance Authority:** GPT-5.6 Sol
**Execution State:** Recovery only; no implementation Story is assigned or authorized

## Accepted partial state

- APO-18 is complete.
- APO-19 is complete and Sol-accepted. It must not be rerun as part of APO-20.
- The GitHub repository rename is complete:
  `https://github.com/Hossam1104/AI-Project-Orchestrator`
- Git origin is complete:
  `https://github.com/Hossam1104/AI-Project-Orchestrator.git`
- The target local root is:
  `D:\AI Tools\Hossam\AI Project Orchestrator`
- The old local root still exists because Windows refused the physical rename with
  `IOException` / `RenameItemIOError` (“The process cannot access the file because it is being
  used by another process”).
- `docs/LEGACY_IMPLEMENTATION_MAP.md` remains the approved backlog input for Sol's Jira planning.
- The historical APO-19 inspected starting SHA remains
  `ae712335696d827a7a1a2d2464cf667f43430c33`.
- The accepted APO-19 final main SHA remains
  `9659bf65bda4defc91b2383cf7f195637678485f`.

## Owner recovery action

After closing the process that holds the repository directory, run this exact command from
`D:\AI Tools\Hossam`:

```powershell
Rename-Item -LiteralPath 'D:\AI Tools\Hossam\AI Usage Monitor Tool' -NewName 'AI Project Orchestrator'
```

Then verify that `D:\AI Tools\Hossam\AI Project Orchestrator` exists, the old path is absent, and
the repository opens with the new root. A fresh owner instruction is required after that recovery
action; do not execute APO-19 automatically.

## Scope boundary

This recovery checkpoint does not authorize:

- execution of APO-19 or any other Story;
- technical solution/project/namespace/assembly/test renaming;
- `%LOCALAPPDATA%\AIUsageMonitor` migration;
- product source or test changes;
- provider implementation, orchestration runtime, routing, GitHub/Jira adapters, UI redesign, or
  any other future capability; or
- automatic Jira backfill creation.

Technical identifiers containing `AIUsageMonitor` remain intentionally unchanged until a later,
explicitly assigned planner-controlled refactoring Story.

## Completion condition for APO-20

APO-20 may be marked COMPLETE only after the target local root physically exists, the final
repository is verified from that root, and the final Git state is committed, pushed, fetched, and
clean with `HEAD == origin/main`. On completion, replace this recovery checkpoint with the safe Sol
planner checkpoint that states:

- APO-18 complete;
- APO-19 complete and accepted;
- APO-20 complete;
- repository/local identity rename complete;
- `docs/LEGACY_IMPLEMENTATION_MAP.md` is the approved backlog input;
- no Jira backfill recommendations are automatically created;
- no source refactor is authorized; and
- GPT-5.6 Sol must review the final state, create the actual backfill Stories under APO-2/APO-3/
  APO-4/APO-17, select the first implementation Story, and issue its full execution contract.

Then stop.

# TASK.md - AI_Orchestrator Authority Boundary

**Project:** `AI_Orchestrator`
**Mode:** `FAST V1 CLOSEOUT MODE`

This file is a short authority boundary, not an executable executor prompt.

## Current authority

- `APO-48 = Done`.
- Current next gate: `APO-51`.
- `APO-51 = NOT STARTED` and remains `To Do`.
- No implementation is authorized by `TASK.md` itself.
- The next executor or reviewer prompt must come from GPT-5.6 Sol.

## FAST V1 order

`APO-51 -> APO-49 -> APO-63 -> APO-50 -> APO-33 -> release audit`

## Resource boundary

- Active V1 resources: OpenAI + Claude + Antigravity Plus.
- `COPILOT = POST-V1`.
- Inactive/new provider work is `POST-V1`.
- Existing optional provider code is preserved; provider cleanup/removal is deferred.

## Execution boundary

- No feature creep.
- No automatic roadmap execution.
- Do not start a downstream Story without a fresh Sol-authored contract.
- GitHub remains V1 infrastructure; GitHub Actions remains APO-33 and is not yet delivered.

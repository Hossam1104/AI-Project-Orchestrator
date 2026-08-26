# APO-40 Prompt 4/5 — Versioned Planning and Execution Contracts

## Authorization

- Story: APO-40 — Define Versioned Planning and Execution Contracts
- Parent Epic: APO-10 — Planning & Execution Contracts
- Executor: GPT-5.6 Luna xHigh
- Planner / acceptance authority: GPT-5.6 Sol
- Jira: APO-40 In Progress
- Jira implementation-start comment: \`12043\`
- Authorized main base: \`ac1b7445f4120304b76845ba307c54111c557ec8\`
- Feature branch: \`feat/APO-40-versioned-planning-execution-contracts\`

APO-39 is accepted as merged and Done at \`ac1b7445f4120304b76845ba307c54111c557ec8\`.
The accepted APO-39 source and squash merge share tree
\`4dfe83703e1899a4e5eb35a1530e0434924eb3db\`.

## Bounded objective

Implement immutable, inspectable, machine-readable planning/execution contracts with:

- distinct schema version and planner-authorized revision;
- project/context binding to an APO-39 Ready context;
- caller-supplied APO-38 planner identity and effective Planner authorization;
- bounded work-item and immutable repository target identity;
- included scope, constraints, forbidden scope, deliverables, validations, acceptance criteria,
  budgets, stop conditions, and inherited policy references;
- deterministic SHA-256 content-integrity evidence;
- GUID-derived project-isolated JSON persistence with create-new/no-overwrite revision files;
- explicit valid/missing/invalid/integrity/unavailable/future/older schema read states; and
- focused semantic, lineage, integrity, compatibility, isolation, and DI tests.

Contract schema version is \`1\`. Contract revisions are independent immutable records under:

\`projects/<project-guid>/contracts/<contract-guid>/revision-000001.json\`

## Required boundaries

This task is data and validation only. Do not implement APO-41 dependency graphs/scheduling,
APO-42 handoff package generation, APO-43 Smart Continue/checkpoints, APO-44 routing, APO-45
execution, tracker integration, validation/approval engines, model invocation, prompt transport,
worktree creation, Git mutation, remote SCM, or a contract designer UI.

APO-41, APO-42, APO-43, APO-44, and APO-45 remain unauthorized and must remain To Do.

## Required validation

Run restore, build with zero warnings/errors, focused contract/repository/integrity/compatibility/
isolation/DI tests, full solution tests, \`git diff --check\`, credential-shaped scans, and a
base-to-head scope review. Push this branch and create exactly one OPEN/DRAFT/UNMERGED PR against
\`main\`; do not merge or mark APO-40 Done.

At final handoff, replace this file with a Sol acceptance handoff for APO-40 and stop.

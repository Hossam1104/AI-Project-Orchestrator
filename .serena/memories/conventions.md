# AI_Orchestrator conventions

- Preserve dependency direction: Domain stays free of WPF, JSON/file, HTTP, provider, filesystem, and Windows UI concerns; Application owns provider-independent contracts/use cases; Infrastructure owns persistence, paths, credentials, logging, OS integration; Providers own detection/collection/parsing/normalization; Desktop consumes application services/view models.
- Prefer existing abstractions and direct boring code; do not add speculative providers, database layers, mediator/factory/event-bus abstractions, or product scope.
- Preserve truthful unavailable/stale/manual/partial states and remaining-capacity semantics. Never guess provider schemas/endpoints or store raw credentials/tokens.
- Persistence writes must remain atomic and schema-versioned; JSONL history remains partitioned and streaming. Do not change schema V1 without explicit authorization.
- Runtime execution is bounded, cancellable, provider-independent, no-retry by default, and must stop at RunValidation rather than acceptance. APO must be stopped after each prompt unless explicitly overridden.
- Jira APO is work-tracking authority; one assigned work item per executor; Draft PR and Sol acceptance are required delivery gates.
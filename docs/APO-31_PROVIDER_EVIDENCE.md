# APO-31 Provider Capacity Evidence

**Verification date:** 22 August 2026
**Story:** APO-31 - Official Provider Capacity Adapters
**Scope:** Codex, Claude, Kimi, GitHub Copilot, and Antigravity

This record captures the official provider surfaces verified for APO-31, what each surface
actually means, and the resulting adapter boundary. The application reports only values supported
by the source. A usage report is not converted into an allowance, remaining percentage, reset
window, plan, or billing entitlement unless the source provides that fact.

## Support matrix

| Provider / surface | Official source and authentication | Verified semantics | APO-31 result | Explicit limitation |
|---|---|---|---|---|
| GitHub Copilot - personally billed user | [GitHub REST billing usage](https://docs.github.com/en/rest/billing/usage), `GET /users/{username}/settings/billing/ai_credit/usage`; authenticated GitHub token with the documented user Plan permission | Usage report containing usage items and quantities; it is not a universal remaining allowance or reset endpoint | **Implemented** as usage-only, `Partial`, with opaque credential reference | No fabricated plan, allowance, remaining value, or reset. The adapter does not claim to cover organization-managed or enterprise-managed billing through the personal endpoint |
| GitHub Copilot - organization | [GitHub REST billing usage](https://docs.github.com/en/rest/billing/usage), `GET /organizations/{org}/settings/billing/ai_credit/usage`; authenticated token with documented organization Administration permission | Organization AI-credit usage report | **Implemented** as configured organization usage-only, `Partial` | No fabricated allowance, remaining value, reset, or subscription. Enterprise scope is explicitly unsupported until a separately verified surface is approved |
| GitHub Copilot - enterprise | [GitHub REST billing usage](https://docs.github.com/en/rest/billing/usage) documents distinct organization and enterprise-managed billing scopes | Scope exists in GitHub documentation, but no enterprise adapter contract was approved for this story | **Unsupported** | No guessed enterprise route or cross-scope fallback |
| OpenAI Codex / ChatGPT subscription | [Codex CLI help](https://help.openai.com/en/articles/11096431) and the official [OpenAI status surface](https://status.openai.com/) | The documented consumer status experience is interactive; OpenAI organization Usage/Costs APIs describe API organization activity, not ChatGPT/Codex subscription allowance | **CLI detected; refresh unsupported/manual** | No browser scraping, no interactive `/status` scraping, and no conflation with OpenAI API billing/usage |
| OpenAI API organization usage | [OpenAI organization Usage API reference](https://developers.openai.com/api/reference/resources/admin/subresources/organization/subresources/usage) | Organization API activity/cost reporting | **Not adapted in APO-31** | It is a different product/accounting surface from Codex/ChatGPT subscription capacity |
| Claude / Claude Code consumer subscription | [Claude Code CLI usage](https://code.claude.com/docs/en/cli-usage) | CLI output modes are documented, but subscription status/usage remains an interactive local status surface rather than a verified non-interactive capacity API | **CLI detected; refresh unsupported/manual** | No CLI TUI scraping and no Anthropic API-to-consumer-subscription inference |
| Anthropic API organization usage | [Anthropic Messages Usage Report](https://platform.claude.com/docs/zh-CN/api/admin/usage_report/retrieve_messages), `GET /v1/organizations/usage_report/messages`; Admin API key in `x-api-key` | Organization API token/message usage report; no allowance or remaining entitlement | **Implemented** as token usage-only `Partial` | Requires explicitly configured opaque Admin API credential reference and an optional verified start time; no consumer subscription claim |
| Kimi Code membership / local Kimi server | [Kimi CLI server API](https://www.kimi.com/code/docs/en/kimi-code-cli/reference/server-api.html), [server guide](https://www.kimi.com/code/docs/en/kimi-code-cli/guides/server.html), and [membership](https://www.kimi.com/code/docs/en/kimi-code/membership.html); bearer token supplied through secure credential storage | Documented local `kimi web` REST endpoint exposes OAuth usage rows, used/limit/remaining/reset data, extra usage, and user membership metadata | **Implemented when explicitly configured** as quota windows, reset data, plan metadata, and optional extra usage | The documented REST API is experimental and requires an already-running local server plus an explicitly configured opaque credential reference. APO does not launch the server or read CLI auth files |
| Moonshot Open Platform API | [Kimi Code FAQ](https://www.kimi.com/en/help/kimi-code/faq) distinguishes Kimi Code membership from Open Platform pay-as-you-go | Separate API billing product | **Unsupported/manual** | No endpoint or billing interpretation is guessed, and Open Platform spend is not presented as Kimi Code capacity |
| Google Antigravity subscription | [Antigravity CLI reference](https://antigravity.google/docs/cli/reference/) and [usage command](https://antigravity.google/docs/cli/commands/usage?authuser=2) | `/usage` (alias `/quota`) is documented as an interactive TUI panel | **CLI detected; refresh unsupported/manual** | No TUI scraping, browser-cookie extraction, or guessed quota endpoint |

## Implementation truthfulness rules

- Provider detection is safe executable discovery only. It does not execute a provider CLI to
  scrape an interactive display.
- Credentials are represented in configuration only by `CredentialReference`; secret material is
  read through the approved secure-credential contract and is never written to diagnostics,
  provider state, source, or tests.
- HTTP failures are typed as authentication, permission, unsupported, rate-limit, timeout,
  network, malformed, server, or provider errors. Response bodies are not copied into errors.
- A successful refresh retains the last-known-good snapshot when a later refresh fails and marks
  that retained data stale/error according to the provider refresh contract.
- Usage-only reports remain usage-only. The adapters do not manufacture a limit, remaining value,
  reset time, plan, billing period, or consumer subscription from an unrelated source.
- Kimi `remaining` is preserved when provided. Where the official row provides used and limit but
  omits remaining, the domain derives the mathematically equivalent remaining percentage; no
  provider claim is added.

## Code and validation evidence

- Provider adapters live under `src/AIUsageMonitor.Providers/` and are registered as all five
  `ProviderCode` values in `ProviderRegistry`.
- `tests/AIUsageMonitor.Provider.Tests/OfficialProviderAdapterTests.cs` covers successful source
  mapping, credential absence, authentication and permission failures, rate-limit stale retention,
  malformed payloads, cancellation, CLI-only unsupported paths, and Kimi metadata/extra usage.
- `tests/AIUsageMonitor.Provider.Tests/ProviderRegistrationTests.cs` verifies complete registry
  resolution and enum order.
- Focused provider suite: **20 passed, 0 failed**.

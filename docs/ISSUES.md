# Reporting bugs, requesting features and tracking work

[Türkçe](ISSUES.tr.md) · [Home](../README.md) · [Open a report](https://github.com/tamerinal/sap_mcp_windows/issues/new/choose) · [All issues](https://github.com/tamerinal/sap_mcp_windows/issues)

GitHub Issues is the public record for this application's bugs and feature requests. Each submitted report gets an issue number and its own discussion/history. English and Turkish reports are welcome. Reading reports is public; creating or subscribing to one requires a GitHub account. The application currently does not submit reports or upload logs automatically.

## Choose the right form

| Form | Use it for | Include |
|---|---|---|
| [Bug report](https://github.com/tamerinal/sap_mcp_windows/issues/new?template=bug_report.yml) | Existing behavior fails or differs from what is documented. | App version and Online/Offline installer, Windows build/UI language/theme, affected area, steps, expected/actual behavior and optional redacted diagnostic ID/text. |
| [Feature request](https://github.com/tamerinal/sap_mcp_windows/issues/new?template=feature_request.yml) | New behavior or an improvement to an existing flow. | User need, proposed behavior, related area, an observable acceptance example and optional alternatives/current version. |

Read [Troubleshooting](TROUBLESHOOTING.md) and search both open and closed issues first. For the same problem, add your reproduction details to the existing issue or react with 👍 instead of opening a duplicate. Create one issue per separate problem or request. Examples should use fictional systems/users. A useful title describes the trigger and result, such as “SDK validation fails after moving the SDK folder” or “Show a report link in Help”.

## Information that stays private

Issues and attachments are public. Do not upload passwords, DPAPI files, actual SAP landscape/mapping files, client configuration files, tokens, authorization headers, SDK archives or raw SAP business data/source. Prefer a short, manually reviewed redacted error excerpt and the stable diagnostic ID. Screenshots are optional: remove account/system names, paths and any other sensitive information yourself before posting. A full log bundle is not required. Security vulnerabilities follow [Security reporting](../SECURITY.md), not a public issue containing exploit details or credentials.

## Follow progress

Use **Subscribe** on an issue to receive updates according to your GitHub notification settings. For repository-wide updates, choose **Watch → Custom → Issues** on GitHub. These are personal GitHub preferences; submitting a request does not enable a separate email service or background Codex monitor.

| View | What it shows |
|---|---|
| [Open bugs](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aopen%20label%3Abug) | Current defects |
| [Open feature requests](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aopen%20label%3Aenhancement) | Requested improvements |
| [Awaiting review](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aopen%20label%3Astatus%3Atriage) | New reports |
| [Waiting for information](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aopen%20label%3Astatus%3Aneeds-info) | Reporter input is needed |
| [Accepted backlog](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aopen%20label%3Astatus%3Aaccepted) | Accepted work not yet started |
| [In progress](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aopen%20label%3Astatus%3Ain-progress) | Active implementation |
| [Blocked](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aopen%20label%3Astatus%3Ablocked) | External dependency or unresolved prerequisite |
| [Awaiting verification/release](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aopen%20label%3Astatus%3Averification) | Implemented work awaiting validation or delivery |
| [High priority](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aopen%20label%3Apriority%3Ahigh) | Maintainer-assessed urgent work |
| [Closed records](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aclosed) | Completed, duplicate or not-planned records; read the closing explanation |

An empty view means no matching reports exist yet. Acceptance or a priority label is not a promised delivery date. Milestones are assigned only when a target release is actually planned. Confirmed delivery is recorded with the release link in the issue and the changelog.

## Maintainer workflow

1. **Review:** Forms automatically apply `bug` or `enhancement` plus `status:triage`. Reports submitted through an API or CLI may bypass forms; label those manually. Check duplicates, reproducibility, scope and sensitive content. Assign the appropriate area label and assess priority.
2. **Clarify or accept:** Use `status:needs-info` with specific questions when details are missing. When sufficient, replace it with `status:accepted`. Describe agreed scope and acceptance criteria. Update statuses manually; a new comment does not automatically move a record.
3. **Start:** Assign the responsible person when work starts and use `status:in-progress`. Link a branch, commit or PR using the issue number. For dependencies, use `status:blocked` and explain what is needed; link a public related issue where possible.
4. **Verify:** Use `status:verification`, record the change and actual test evidence, and identify any unperformed clean Windows/live SAP tests. Keep the issue open while a promised delivered build or required verification is pending.
5. **Deliver and close:** Link the published release and validation result, remove the status label and close as **Completed** once the agreed acceptance criteria are met. For duplicates, link the original and label `duplicate`; for declined requests, explain the reason and use `wontfix`/Not planned. Do not silently close a request as completed when it was declined. If the problem persists, discuss reopening the same record with new version/reproduction details.

Use exactly one `status:*` label on an open actionable issue, at most one `priority:*`, and one or more relevant `area:*` labels. The pinned reporting guide is a `tracking`/`documentation` record, not backlog work, and has no status label.

| Label group | Values |
|---|---|
| Type | `bug`, `enhancement`; existing `documentation`, `duplicate`, `wontfix` where relevant |
| Priority | `priority:high` (blocking/serious impact), `priority:normal`, `priority:low` |
| Area | `area:installer`, `area:rfc`, `area:adt`, `area:connections`, `area:clients`, `area:ui`, `area:docs` |

Private upstream RFC/ADT repository work can be referenced here using a sanitized public summary and its dependency/result. Do not copy private source, secret URLs or customer information into this public tracker. Manual forwarding to those projects remains a maintainer action.

For a PR targeting the default branch `YAPAY_ZEKA`, use `Refs #123` while verification/release is pending. `Fixes #123` / `Closes #123` automatically closes an issue on merge into the default branch, so use closing keywords only when closure at merge genuinely meets acceptance. Otherwise close manually after delivery. Keep English/Turkish documentation and changelog entries aligned.

## Maintaining this setup

Form definitions are in `.github/ISSUE_TEMPLATE/`; managed label definitions are in `.github/labels.json`. An authorized maintainer can run `./scripts/Sync-IssueLabels.ps1` with GitHub CLI signed in to create/update those labels. Unlisted labels and existing issues are preserved. This script does not process future reports automatically. Keep the pinned reporting guide and README links current; maintainers review queues when handling reports. No response-time SLA, scheduled monitoring or automatic implementation is configured.

GitHub references: [issue form syntax](https://docs.github.com/en/communities/using-templates-to-encourage-useful-issues-and-pull-requests/syntax-for-issue-forms), [linking PRs and issues](https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/linking-a-pull-request-to-an-issue), [notification subscriptions](https://docs.github.com/en/account-and-profile/managing-subscriptions-and-notifications-on-github).

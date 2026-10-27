# Changelog

[Türkçe](CHANGELOG.tr.md)

## 0.1.0-preview.3 — 2026-10-27

- Fix saved SDK lib propagation in child PATH across Host, Node and Python; preserve OS PATH and existing configuration.
- Require x64 SAP/ICU DLLs and isolated ICU/PyRFC/SDK initialization before Ready; safe stage/decimal/hex exit diagnostics for Manager and Host.
- Add SDK regression tests, bilingual help and a separate pending RFC worker exitCode/signal request. Issue #3. Private MCP payloads remain unchanged.
- New Online/Offline preview installers; clean Windows and live TED/TEX acceptance remain pending.

## v0.1.0-preview.2 — 2026-10-27

- Added read-only SAP GUI and editable managed JSON connection sources, automatic MCP XML projection, confirmed target/profile migration and deletion with retained DPAPI files.
- Added recoverable catalog/mapping/XML/settings saves, source error recovery, GUI detection hints and bilingual connection help/examples.


- Documented English/Turkish bug and feature intake, triage, ownership, validation and release tracking.
- Expanded issue forms with default type/triage labels, repository tracking labels and an idempotent label setup script.
- Added reporting links to README/contribution guidance and a pinned GitHub reporting hub.

## v0.1.0-preview.1 — 2026-10-27

- Initial Windows x64 Avalonia/.NET 10 manager, per-user Online/Offline Inno installers and independent stdio host.
- Selective RFC/ADT install, prerequisite checks/individual/bulk install, SDK validation and standalone ADT.
- SAP GUI discovery, multiple account/client profiles, shared active profile and DPAPI password management.
- Preserving Codex/Claude merges, generic export and metadata-only redacted rolling logs.
- Instant English/Turkish UI, persisted language, localized help and preserved open forms.
- Public documentation, MIT/third-party notices, issue forms, Windows CI and publication scan.
- Isolated distribution path adaptations and PyRFC debug-path sanitization with provenance.
- Unsigned preview; clean Windows 10/11 and live SAP acceptance remain pending. See release notes.

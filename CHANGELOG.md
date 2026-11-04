# Changelog

## 0.1.0-preview.14 — OpenCode integration and Advanced verification

Basic and Advanced can register SAP MCP servers in OpenCode V1 or V2, with global/project file selection, previews, backups and JSONC comment/instruction preservation. Advanced Connections adds **Automatically discover and verify**, sharing Basic discovery, consent and separate ADT/RFC results. It fills the form without saving; review the URL/TLS fields and press Save. Existing Basic behavior and MCP/runtime archives are retained.

## 0.1.0-preview.13 — Discovery startup and public documentation

Includes the Basic discovery settling wrapper from issue #7 and the merged public-content privacy cleanup. The wrapper warms up and settles inside the same Node process before invoking the existing discovery function. ADT/RFC package archives and runtime pins are unchanged. Documentation uses fictional examples and omits environment-specific test notes. Production confirmation of the discovery fix, clean Windows acceptance and live SAP testing remain pending for this release.

## 0.1.0-preview.12 — Custom MCP integration help

Basic and Advanced client integration screens, and the final Basic summary, now provide local stdio MCP setup help for other agents and applications. Copy complete JSON, server entries for an existing `mcpServers` object, TOML tables or individual command/argument fields with actual installation paths. Basic examples follow current verification and RFC readiness; no eligible component produces no runnable example. Opening help and copying code do not start network checks or write client configurations. ADT/RFC packages are unchanged. See [custom integration help](docs/CUSTOM-INTEGRATION.md).

## 0.1.0-preview.11 — Fresh RFC readiness and ADT 0.4.3

Basic Verify recalculates RFC readiness from current settings after connection consent. Fixing the SDK or prerequisites in Advanced no longer requires restarting Manager. Python, PyRFC, Visual C++ runtimes, SDK, native import and RFC package blockers are reported separately. Explicit ADT-only mode remains `RFC_DISABLED_BY_MODE`.

A PyRFC file on disk does not prove native loading. A separate Python process imports `pyrfc.Connection` without preloading ICU or connecting to SAP. Native failure and timeout remain separate from missing dependencies and SAP ping outcomes. SDK settings affect child processes only, never persistent environment variables. Client registration requires both successful RFC verification and current readiness.

The package includes ADT 0.4.3: TLS warnings do not invalidate an otherwise successful connection; ping and repository tools share acceptance criteria. Origin-scoped certificate fingerprint pins are supported by the ADT package; no trust is granted automatically from the network. Reverify existing profiles. Online/Offline installers and SHA256 files are provided in the preview.11 prerelease.

## 0.1.0-preview.10 — 2026-11-02

Basic separates Save from Verify and never discovers while editing. SID is required. With explicit acceptance, HTTP/HTTPS works without uploaded certificates. RFC ping, server-list permission, RFC-assisted candidates, consented smart/full TCP scanning and final ADT authentication are separate stages. Denied RFC discovery preserves a successful ping. Failed RFC candidates offer a consented scan instead of claiming the service is inactive. Candidates continue after login rejection and stop at the first complete success.

Full TCP scanning targets 30 seconds and stops with partial results at 60 seconds; HTTP probing has a separate budget. ADT 0.4.2 discovery protocol 2 is required. Advanced, Host and shared production policy behavior are preserved. Online/Offline EXEs and SHA256 files are published in the GitHub prerelease. See the [Basic guide](docs/BASIC-SETUP.md).


## 0.1.0-preview.9 — SID and MCP output fixes

RFC 0.1.1 and ADT 0.4.1 return complete JSON in text as well as structured content. Legacy ADT list/ping responses follow their schemas. Syntax diagnostics, DDIC details, class/interface components, search descriptions and BW endpoint failures are visible to text-only clients. Both tool name sets remain available.

SID is required when saving connections/profiles. Basic can obtain it from a successful RFC connection, or the user can enter it. A backend SID mismatch prevents saving. Existing blank records remain readable. HTTP profiles retain the plaintext warning; TLS inspection explains that certificates do not apply to HTTP. Configured custom URLs are included first in endpoint discovery.

Native MCP operation/session logs are separate from Host process logs, honor level/rotation/retention, exclude SAP payloads, and expose operation IDs. Backend release information remains unavailable when SAP does not explicitly expose it. UTF-8 and declared Turkish response encodings are decoded at the HTTP boundary.

Local Online/Offline installers only; no GitHub publication. Live writes and activations are excluded from acceptance.

## 0.1.0-preview.8 — 2026-10-28 (local release)

- Existing Basic installations open a status summary. Installation/packages, SAP connection, and app integrations remain directly accessible. Saved profiles are restored with an empty password field. Startup does not install software or contact SAP.
- Initial preparation installs missing prerequisites only. Subsequent Basic use checks prerequisites and directs prerequisite or same-version package repairs to Advanced. Missing/older RFC and ADT MCP packages update only on request. ADT-only preference persists.
- Codex, Claude Desktop, and Claude Code are independent targets. Claude Code uses user-scoped `.claude.json`, honoring `CLAUDE_CONFIG_DIR` and preserving project and unrelated settings.
- SAP-only previews distinguish New / Will update / Up to date and refresh when selections change. Unchanged registrations are not rewritten. Replacement permission, backups, and per-app results are explicit.
- Basic control labels are corrected. Existing connections, DPAPI secrets, RFC/ADT payload versions, and Advanced repair options remain available.

Local Online/Offline installers and SHA256 files are delivered without publishing to GitHub. Verification results and clean-Windows/live-SAP acceptance limits are recorded in the delivery report.


## 0.1.0-preview.7 — 2026-10-28 (local release)

Basic discovers HTTP and HTTPS without sending credentials. HTTPS 443/44300–44399 and HTTP 80/80NN/8080/44380 are covered; trusted HTTPS wins. HTTP is visibly unencrypted. Certificate-only HTTPS failures require Advanced; verification is never bypassed. One selected endpoint receives account, ADT discovery and read-only compatibility verification.

ADT 0.4.0 normalizes BOM/line endings in encrypted DPAPI files without trimming the real password. Discovery and account results are separate; edits invalidate prior results. Discovery IDs, timestamps and safe diagnostics persist. RFC/runtime assets are retained. See the delivered verification report for actual local/live results; clean Windows and other-machine acceptance remain separate. Not published to GitHub.


## 0.1.0-preview.6 — 2026-10-28

Fix Basic host-only ADT discovery: omit absent optional port fields and accept legacy null fields in the updated ADT package. Show safe input/process error codes. Add exact Manager-to-Node and local HTTPS regressions; update bilingual help. RFC package unchanged. Clean Windows and live affected-machine acceptance remain pending.

## 0.1.0-preview.5 — 2026-10-28

Basic/Advanced modes; SDK-first automatic preparation; bounded credential-free HTTPS discovery; independent ADT/RFC verification and partial success; shared record preservation; bilingual help and regression tests. RFC payload unchanged.

[Türkçe](CHANGELOG.tr.md)

## v0.1.0-preview.4 — 2026-10-28

- Explicit ADT endpoints, address-finding help, opt-in unauthenticated suggestions and migration without password-bearing guesses.
- Profile-only PEM/DER trust, certificate inspection, shared Node transport, staged diagnostics and masked support export.
- Selected-profile Manager tests leave shared active accounts unchanged; old ADT packages require repair. Updated ADT package; RFC/PyRFC/runtime hashes preserved.
- Local HTTP/TLS regression coverage and bilingual documentation. Clean Windows/live SAP remain pending.

## 0.1.0-preview.3 — 2026-10-27

- Fix saved SDK lib propagation in child PATH across Host, Node and Python; preserve OS PATH and existing configuration.
- Require x64 SAP/ICU DLLs and isolated ICU/PyRFC/SDK initialization before Ready; safe stage/decimal/hex exit diagnostics for Manager and Host.
- Add SDK regression tests, bilingual help and a separate pending RFC worker exitCode/signal request. Issue #3. Private MCP payloads remain unchanged.
- New Online/Offline preview installers; clean Windows and live SAP acceptance remain pending.

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

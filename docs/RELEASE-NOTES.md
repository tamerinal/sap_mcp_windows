# Release notes

## 0.1.0-preview.13 — Discovery startup and public documentation

Includes the Basic discovery settling wrapper from issue #7 and the merged public-content privacy cleanup. The wrapper warms up and settles inside the same Node process before importing the existing discovery entry point. ADT/RFC package archives and runtime pins are unchanged. Documentation uses fictional examples and omits environment-specific test notes. Production confirmation of the discovery fix, clean Windows acceptance and live SAP testing remain pending for this release.

## 0.1.0-preview.12 — Custom MCP integration help

Basic and Advanced client integration screens, and the final Basic summary, now provide local stdio MCP setup help for other agents and applications. Copy complete JSON, server entries for an existing `mcpServers` object, TOML tables or individual command/argument fields with actual installation paths. Basic examples follow current verification and RFC readiness; no eligible component produces no runnable example. Opening help and copying code do not start network checks or write client configurations. ADT/RFC packages are unchanged. See [custom integration help](CUSTOM-INTEGRATION.md).

## 0.1.0-preview.11 — Fresh RFC readiness and ADT 0.4.3

Basic Verify recalculates RFC readiness from current settings after connection consent. Fixing the SDK or prerequisites in Advanced no longer requires restarting Manager. Python, PyRFC, Visual C++ runtimes, SDK, native import and RFC package blockers are reported separately. Explicit ADT-only mode remains `RFC_DISABLED_BY_MODE`.

A PyRFC file on disk does not prove native loading. A separate Python process imports `pyrfc.Connection` without preloading ICU or connecting to SAP. Native failure and timeout remain separate from missing dependencies and SAP ping outcomes. SDK settings affect child processes only, never persistent environment variables. Client registration requires both successful RFC verification and current readiness.

The package includes ADT 0.4.3: TLS warnings do not invalidate an otherwise successful connection; ping and repository tools share acceptance criteria. Origin-scoped certificate fingerprint pins are supported by the ADT package; no trust is granted automatically from the network. Reverify existing profiles. Online/Offline installers and SHA256 files are provided in the preview.11 prerelease.

## 0.1.0-preview.10 — 2026-11-02

Basic separates Save from Verify and never discovers while editing. SID is required. With explicit acceptance, HTTP/HTTPS works without uploaded certificates. RFC ping, server-list permission, RFC-assisted candidates, consented smart/full TCP scanning and final ADT authentication are separate stages. Denied RFC discovery preserves a successful ping. Failed RFC candidates offer a consented scan instead of claiming the service is inactive. Candidates continue after login rejection and stop at the first complete success.

Full TCP scanning targets 30 seconds and stops with partial results at 60 seconds; HTTP probing has a separate budget. ADT 0.4.2 discovery protocol 2 is required. Advanced, Host and shared production policy behavior are preserved. Online/Offline EXEs and SHA256 files are published in the GitHub prerelease. See the [Basic guide](BASIC-SETUP.md).


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


[Türkçe](RELEASE-NOTES.tr.md)

Fix a Preview.5 regression: entering a server without a port could send `explicitPort: null`, which the ADT process rejected before making any network request. The generic warning concealed this input error. RFC could succeed independently. Manager now omits absent optional fields, and updated ADT 0.3.1 accepts legacy null fields. Safe input/process codes distinguish local discovery failures from network/TLS results.

Upgrade Manager, then **Prepare installation** in Basic or install/repair ADT in Advanced; restart client MCP processes. Both the Manager and its ADT package must be updated. Profiles, passwords, settings and RFC archives are preserved. Known HTTPS endpoints can be entered manually. An instance number does not establish the HTTPS port; DNS names and CA trust must still match the actual service.

Regression coverage includes the exact Manager host-only JSON, the legacy null request through the real Node CLI, and Manager-to-Node discovery against a local HTTPS fixture. Actual test counts and limitations are in the validation assets. [Basic help](https://github.com/tamerinal/sap_mcp_windows/blob/v0.1.0-preview.6/docs/BASIC-SETUP.md) · [issue #6](https://github.com/tamerinal/sap_mcp_windows/issues/6).

Unsigned preview. Clean Windows and live SAP/affected-machine acceptance remain pending. SDK is not bundled. Inno Setup 6.7.3 reports non-commercial use only; commercial distribution requires appropriate licensing. MIT does not replace third-party terms. Compiled private MCP JavaScript/dependencies are extractable from EXEs. Previous releases are preserved. No unconditional compatibility claim.

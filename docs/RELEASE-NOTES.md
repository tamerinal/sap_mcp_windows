# v0.1.0-preview.3 — RFC SDK startup hotfix

[Türkçe](RELEASE-NOTES.tr.md)

Fixes RFC EmptyStdout on Windows machines where the SDK lib directory is absent from inherited PATH. Saved sdkDirectory now sets child-only SAPNWRFC_HOME and prepends SDK lib to PATH for Host → Node → Python and RFC-dependent ADT launches. The machine/user PATH is unchanged. Manager connection tests use the same Host.

SDK Ready now requires all five x64 SAP/ICU DLLs, actual ICU initialization and a separate PyRFC/SDK startup probe with no SAP destination/account/password. Missing files, crashes, timeouts and malformed output are blocked. Safe diagnostic metadata includes code, stage, class and available decimal/hex exit code. Host/Node metadata is distinguished from Python worker exit details.

**Upgrade:** install Online or Offline over the existing manager; preserve user data. Open SAP RFC SDK, select the SDK root with lib, verify and save it, then restart RFC/ADT MCP servers in Codex/Claude (restart clients if needed). Existing profiles, DPAPI secrets and client registrations remain valid. SDK is user-supplied and is never bundled. Both installers remain self-contained for .NET; Offline carries runtime packages. No SAP GUI is required. Basic ADT remains independent of RFC SDK.

[Issue #3](https://github.com/tamerinal/sap_mcp_windows/issues/3) tracks the fix and affected-machine acceptance. A separate [RFC worker diagnostic request](https://github.com/tamerinal/sap_mcp_windows/blob/v0.1.0-preview.3/docs/MCP-RFC-WORKER-DIAGNOSTICS.md) remains pending: Python exitCode/signal cannot be recovered by the Host from the current MCP package. Private RFC/ADT repositories and approved runtime/MCP archive hashes are unchanged. MCP tools, target_key, credential format and settings schema are unchanged. Never upload raw stderr/traces, PATH or customer files.

See the release VALIDATION-SUMMARY.md for actual automated and local test results. SDK tests use an isolated private copy with Turkish/spaced paths; it is excluded from source and release assets. The pre-fix failure was reproduced locally; SDK lib in child PATH reached the expected local parameter error. Live TED/TEX RFC_PING must be retried on the affected environment after MCP restart and is not claimed complete.

**Unsigned preview.** Clean Windows 10 22H2/11 (including a GUI-free, network-disabled installation), native accessibility/keyboard checks and live SAP acceptance remain pending. The developer workstation has SAP GUI installed; automated tests do not invoke it. Inno Setup 6.7.3 reports non-commercial use only; appropriate licensing is needed for commercial builds/distribution. MIT covers author-owned code, not third-party terms. Compiled MCP JS is extractable. Existing PyRFC provenance and native logging limitations remain documented.

Preview.1 and preview.2 tags/assets are preserved. Managed connections, read-only external SAP GUI XML, profile migration and DPAPI protection remain available. Message-server groups, SNC/SSO, landscape includes, password-bearing SAProuter routes, SAP-side password changes, macOS and automatic updates remain outside this preview.

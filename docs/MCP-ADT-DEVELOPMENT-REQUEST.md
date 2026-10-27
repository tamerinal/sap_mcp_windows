# SAP ADT MCP — standalone portable release contract

[Türkçe](MCP-ADT-DEVELOPMENT-REQUEST.tr.md)

Target: `tamerinal/sap_adt_mcp`. This is a request for the separate project; it is not sent automatically and that repository is not modified.

1. Publish a tagged portable Node 24 Windows x64 ZIP with compiled dist/, production dependencies, package/lock, version/commit/build-info, dependency licenses and SHA-256. No customer files, personal paths, SDK/Python requirements or Git/source build on end-user machines.
2. Take landscape, mapping, PowerShell, all log paths and optional RFC command/args/cwd from explicit configuration/environment. Keep overridesByTarget/secretRef and next-call profile refresh. Preserve stdio, tool names, target_key and existing wire formats. Do not retain personal default paths.
3. Add explicit RFC integration enable/disable capability. Basic ADT must start and work with no SDK, Python or RFC. Operations that require RFC must check availability **before any SAP mutation**, and return RFC_UNAVAILABLE with required remediation. In particular remote-enabled function creation and function source update/activation must not partially modify SAP then discover RFC is missing. Use capability-based guards and test every mutation path; merely disabling post-write verification is insufficient.
4. Implement `--diagnose --json` without SAP login: schema/version, config/dependency/path/DPAPI checks, optional RFC state, stable IDs and actionable messages. Exit 0 ready, 2 incomplete/blocked. ADT-only readiness must not fail because optional RFC is disabled. Hide PowerShell/helpers and keep stdout JSON-RPC only.
5. Implement actual SAP_ADT_LOG_LEVEL filtering (Off/Error/Warn/Info/Debug/Trace), folder/size/count/retention and redaction at every level. Never log passwords, authorization headers, CSRF tokens or raw sensitive SAP payloads. Emit operational metadata and sanitized errors only.

Acceptance: ADT-only initialize/tools/list/basic read tools without SDK/Python, disabled-RFC mutation prechecks before writes, optional-RFC integration through the manager's stable host, no personal paths, DPAPI Unicode/spaces compatibility, shared active profile on next call, error/trace redaction and standalone diagnose.

Until upstream capability support arrives, the manager conservatively blocks function source update/activation and remote-enabled function creation without ready RFC; the host owns metadata logging and suppresses native raw payload tracing. The package reports these limitations in provenance/capabilities rather than claiming native diagnose/log-policy support.

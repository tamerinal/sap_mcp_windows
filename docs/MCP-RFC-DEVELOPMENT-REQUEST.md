# SAP RFC MCP — portable release contract

[Türkçe](MCP-RFC-DEVELOPMENT-REQUEST.tr.md)

Target: `tamerinal/SAP_RFC_MCP`. This is a request for the separate project; the manager does not modify that repository or send it automatically.

## Deliverables

1. Tagged portable ZIP containing compiled dist/, python/worker.py, production node_modules, package.json/lock, dependency licenses and build-info.json. Exclude SDK DLLs, customer files, logs and personal paths. Include SHA-256 and upstream/version provenance.
2. Pin Windows x64 Node 24, CPython 3.11 and PyRFC 3.4 cp311-win_amd64. The initial approved PyRFC source commit is `5d4a20a5aee37accf32fc44ed8d668bd69332169`. Provide a reproducible, maintained wheel build from approved source; SDK is supplied in the authorized build environment and must not be included in the wheel. Preserve Apache-2.0 and dependency notices. Archived PyRFC upstream requires an explicit maintenance plan.
3. Read all paths from configuration/environment: SAPNWRFC_HOME, SAP_RFC_PYTHON_EXECUTABLE, SAP_RFC_PYTHON_WORKER_FILE, landscape/mapping, PowerShell and logs. Remove personal fallback paths or report a clear missing-setting error. No Git, compiler, source checkout or developer virtual environment on end-user machines.

## Offline diagnostics and security

`--diagnose --json` must work without a SAP login and check configuration, Python architecture/version, PyRFC import, SDK actual load/version, DPAPI provider and folder permissions. Return schema version, component version, stable check IDs/states and actionable errors; exit 0 ready, 2 incomplete/blocked. Suggested IDs: SDK_NOT_CONFIGURED, SDK_ARCH_MISMATCH, SDK_LOAD_FAILED, PYTHON_UNAVAILABLE, PYRFC_UNAVAILABLE, SECRET_UNREADABLE, CONFIG_INVALID. Never print decrypted secrets or sensitive subprocess output. Hide helper console windows.

Retain overridesByTarget/secretRef and read active credentials on subsequent calls so manager profile switching does not require tool-parameter changes. Keep PowerShell-compatible DPAPI CurrentUser hex files, Unicode/spaces and hidden helper processes. Never log passwords, headers or connection payloads.

## Logs and acceptance

Implement actual SAP_RFC_LOG_LEVEL filtering (Off/Error/Warn/Info/Debug/Trace), configurable folder, max bytes/count and retention days, redaction at every level and stderr-only startup errors. Stdout is MCP JSON-RPC only. Support paths containing spaces/Turkish characters. SDK missing/moved/incompatible and Python/PyRFC/VC failures must be classified without a SAP network call.

Acceptance: initialize/tools/list without login, diagnose with/without SDK, cp311 wheel load on clean Windows x64, DPAPI interoperability, profile changes on next call, no raw secret in any logs, atomic release manifest and reproducible dependency inventory. Until supplied, this manager uses an isolated snapshot build and host-owned metadata logging; it is not full upstream diagnose/native logging support.

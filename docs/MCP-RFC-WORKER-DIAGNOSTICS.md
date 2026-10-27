# RFC worker diagnostics — separate project request

Target: tamerinal/SAP_RFC_MCP. Status: **pending; not implemented in the Manager repository**. The private repository and existing MCP archive are unchanged in preview.3. Track delivery through [#3](https://github.com/tamerinal/sap_mcp_windows/issues/3).

In src/sap/python-worker.ts, change child close handling to close(code, signal). Extend PythonWorkerError and its safely serialized diagnostic details with optional exitCode, exitCodeHex (unsigned Windows representation), signal and terminationReason. Preserve current MCP tools/arguments, error kind/code and normal response compatibility. Preserve null for unavailable exit code or signal; never invent a Python exit code from the Node/Host code.

Classify no-output/nonzero-exit as worker process termination, no-output/zero-exit as empty response, malformed stdout as invalid JSON, and spawn failure, timeout and user cancellation separately. Resolve/reject once, clear timers on every terminal path and drain streams before close classification. Timeout/cancellation must not be overwritten by a later close event. Treat a nonzero worker exit as failure even with misleading success JSON.

Return only fixed identifiers and numeric exit metadata. Do not forward raw stderr, complete environment/PATH, stdin/connection dictionaries, passwords or arbitrary trace payloads to MCP clients/logs. The worker must inherit the Host environment (SAPNWRFC_HOME and SDK lib in PATH); it must not silently substitute personal defaults. Keep helper processes hidden on Windows.

Test nonzero exit with empty stdout and stderr, successful/empty zero exit, invalid JSON, valid error envelope, native Windows unsigned exit, signal termination where supported, spawn errors, timeout/close races, cancellation, and secrets in stderr. Produce a new pinned portable release archive with manifest/SHA-256 so Manager can adopt it in a later release. No automatic message or changes to the private project are made by this request.

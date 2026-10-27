# v0.1.0-preview.5 — Basic / Advanced setup

New installations start in Basic; existing users retain Advanced. Existing Advanced pages are preserved. Basic: SDK and one-action automatic preparation → one SAP connection → existing Codex/Claude integration. ADT-only works without an SDK.

Credential-free HTTPS discovery checks only the specified host with 500 ms debounce, at most 16 concurrent probes, 1.5 seconds per candidate and 15 seconds total. No HTTP fallback/redirect following. 401, HTML and arbitrary XML are not ADT success. Trust is profile-scoped and Basic never bypasses TLS validation. Manual URL/port and SMICM/Basis help are available.

Save uses the actual ADT MCP and separate RFC_PING. First rejected credentials stop automatic retries. One verified MCP permits a warning/partial save and only that MCP in client integration; neither verified preserves the previous profile. Profiles/DPAPI/source XML/unrelated client entries are preserved. RFC source and payload hashes are unchanged.

Upgrade: install the new Manager, then use **Prepare installation** in Basic or install/repair ADT in Advanced; restart client MCP processes. Earlier releases remain. [Basic guide](https://github.com/tamerinal/sap_mcp_windows/blob/v0.1.0-preview.5/docs/BASIC-SETUP.md) and [issue #5](https://github.com/tamerinal/sap_mcp_windows/issues/5) explain behavior. Actual test outcomes are in VALIDATION-SUMMARY assets.

Unsigned preview. Clean Windows, GUI-free/network-disabled installation, native accessibility and live SAP acceptance remain pending. The builder has SAP GUI installed; tests do not invoke GUI. SDK is not bundled. Inno Setup 6.7.3 reports non-commercial use only; commercial distribution requires appropriate licensing. MIT does not replace third-party terms. Private ADT source remains private; compiled JS/dependencies are extractable from EXEs. No unconditional compatibility claim.

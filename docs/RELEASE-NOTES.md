# v0.1.0-preview.2 — Windows x64 preview

[Türkçe](RELEASE-NOTES.tr.md)

This preview adds SAP GUI-independent connection management. Choose application-managed mode to add/edit/delete systems in a private JSON catalog; compatible XML is generated for the existing RFC/ADT MCPs. Or select SAP GUI's folder/XML in read-only mode and manage systems in SAP Logon. Detection suggests a mode; the saved explicit choice wins. Profiles and DPAPI remain shared by target.

- Confirmed host/system-number changes migrate associated profiles and active mappings; collisions are rejected.
- Confirmed deletion removes the local system, profiles and mapping, retaining DPAPI files. External SAP GUI XML is never modified.
- Old schema-1 settings continue using their existing landscape. Source errors leave other manager pages accessible. Failed saves attempt rollback; unsaved profile changes prompt before source changes.
- English/Turkish UI/help, connection examples, GitHub reporting forms and status/area/priority tracking are included. Feature tracking: [#2](https://github.com/tamerinal/sap_mcp_windows/issues/2).

Download Online for runtime downloads or Offline with bundled runtimes. SAP GUI is not required; SAP RFC still requires the user-supplied SDK. Basic ADT does not require Python/SDK; RFC-dependent ADT operations remain guarded. No MCP tool names, target_key or credential formats changed. Private MCP repositories were not modified; the previously verified pinned MCP/runtime payloads are reused.

See VALIDATION-SUMMARY.md for the actual source commit, CI and test results. Automated checks cover both source modes, generated XML in both MCPs, profiles, errors/rollback, bilingual UI and four installer scenarios.

**Unsigned preview. Clean Windows 10/11 (including a GUI-free machine), manual native accessibility and live SAP acceptance remain pending.** The existing Windows development workstation has SAP GUI installed; managed-mode tests do not invoke it but are not a clean-machine test. Inno Setup 6.7.3 in this environment reports non-commercial use only; appropriate licensing is required for commercial build/distribution. MIT for author-owned code does not replace third-party terms. SDK binaries and customer credentials are excluded. Compiled MCP JS is extractable from installers. The approved PyRFC build/provenance and native logging limitations remain as documented.

Message-server groups, SNC/SSO, include expansion, SAP-side password changes, macOS and automatic updates are outside this preview. Existing preview.1 assets/tag are preserved. Read the bundled user/connection guides before configuring real SAP accounts.

Managed mode supports password-free SAProuter strings. Password segments `/W/` and legacy `/P/` are rejected rather than stored in JSON/XML; secret-aware router support requires a future MCP change. See [SAP route-string syntax](https://help.sap.com/saphelp_em92/helpdata/en/48/6c62b71d5a350ce10000000a42189d/content.htm).

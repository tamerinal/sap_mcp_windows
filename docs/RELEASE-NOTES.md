# v0.1.0-preview.1 — Windows x64 preview

[Türkçe](RELEASE-NOTES.tr.md)

First public preview of SAP MCP Manager: selective RFC/ADT setup, managed prerequisites, user-supplied SDK, DPAPI profiles, Codex/Claude integration, English/Turkish UI and help. Download Online for runtime downloads or Offline for bundled runtimes. Neither contains SAP SDK. No GitHub account or source build is required to install.

## Validation and limits

Core/UI tests, local MCP integration checks and four installer scenarios run on the authorized development Windows environment. The published VALIDATION-SUMMARY.md describes actual results. Headless UI coverage includes six screens × two languages × two themes × two widths and unsaved field/password preservation. This does **not** replace native desktop accessibility/manual checks or clean-machine acceptance.

- **Unsigned** application and EXEs; Windows/enterprise policies may warn or block them. Verify published SHA-256 values.
- **Clean Windows 10 22H2 and Windows 11 VM tests are not completed. Live SAP tests are not completed.** Do not treat this as a production-qualified release.
- The Inno Setup 6.7.3 compiler in this environment reports **non-commercial use only**. Appropriate Inno licensing is required for commercial builds/distribution. The compiler is not bundled; MIT for project code does not replace third-party terms.
- RFC SDK is user-supplied. PyRFC is an approved local 3.4 build from archived upstream; fully reproducible upstream wheel delivery remains a separate request. SDK binaries are excluded.
- Upstream MCP repositories remain private; compiled JS/dependencies in these EXEs are extractable. Dirty source snapshots are identified in package provenance; distribution adaptations neutralize personal defaults and debug paths.
- Current upstream native logging lacks full policy support; the host records redacted session/tool metadata and suppresses raw payload traces. ADT function update/activate calls are conservatively blocked without ready RFC.
- No macOS, SNC/SSO, message-server groups, SAP-side password changes or automatic updater in this version.

Settings and secrets survive repair/update. Uninstall preserves user data by default; only explicitly confirmed default owned data is deleted interactively. Read the user guide before configuring real SAP accounts.

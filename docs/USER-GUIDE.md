# User guide — v0.1.0-preview.1

[Türkçe](USER-GUIDE.tr.md) · [Home](../README.md)

## Installation and prerequisites

Download Online when runtime downloads are possible, Offline otherwise. Run the EXE as the Windows user who will run your MCP clients. The manager includes .NET and installs under `%LOCALAPPDATA%\Programs\SapMcpManager`; user data is in `%LOCALAPPDATA%\SapMcpManager`.

Both components start checked. Keep one or both; empty selection is rejected. The EXE places prepared archives on disk. In the manager, **Prerequisites → Check again → Install Required Applications** displays missing installable prerequisites, checked by default, with version/status/description. Use each row's Install or Install Selected. Microsoft Visual C++ may request UAC. Cancellation preserves completed packages; retry after correcting a failure. Checks repeat after installation. Then use **Install / repair selected MCP components** to install only selected servers.

Common checks cover Windows 10 22H2/11 x64, at least 1 GB free space, writable directories, PowerShell DPAPI commands, Node.js and catalog integrity. RFC also needs managed Python 3.11/PyRFC, Visual C++ runtimes and SDK. Basic ADT requires no Python/SDK. SDK-waiting RFC can be staged while ADT is used. Add/remove components later from Overview; removing a component preserves profiles/secrets.

## SDK now or later

Obtain NetWeaver RFC SDK for Windows x64 from SAP's official connector page with your authorized SAP account. SDK is never included. Extract and select the **parent `nwrfcsdk` folder**, containing `lib\sapnwrfc.dll` and `lib\libsapucum.dll` with matching dependencies. Select Verify and save SDK. This checks DLL architecture and actually loads the SDK through Python/PyRFC without a SAP login. Install Python/PyRFC first for the load test. Do not mix SDK versions' DLLs.

If no valid SDK is available, RFC stays blocked and basic ADT remains usable. Attach SDK later on the same page. Moving/deleting SDK blocks RFC again at startup; select the new path and revalidate.

## Systems and profiles

SAP GUI landscape is discovered in common locations; choose XML manually if needed, then Load systems. The source XML is never changed. Direct SAPGUI host:port entries, aliases and router strings are supported. Message-server groups, SNC/SSO and include expansion are outside this preview.

Select a system, then New profile. Enter name, SAP user, three-digit client, SAP language, current password and optional ADT/BW Modeling URLs. Keep TLS certificate verification enabled unless deliberately accepting a known development-system risk. Save profile. You can create several user/client profiles for one system. Each system has **one shared active profile** for Codex and Claude; Make active changes subsequent calls. In-flight calls keep their initial account. Select systems in MCP calls using `target_key` (`host:sysnr`).

Import mapping accepts existing `sap_credentials.json` and preserves the original. Import resolves default → SID → name → target precedence, preserves advanced options and re-protects decryptable secrets. Active profiles are materialized as `overridesByTarget` and `secretRef`; no tool parameters are changed.

## Password updates

The masked password field accepts your existing SAP password. Saving uses DPAPI CurrentUser in the hexadecimal format compatible with PowerShell ConvertFrom-SecureString. JSON holds only a secret file reference. Select a saved profile, enter the new current password and save to update it. Leave the field blank to keep the existing secret. Leading/trailing password spaces are preserved.

This updates the local protected copy, **not the actual password on SAP**. After a SAP-side change, update it here. Another Windows account or computer may not decrypt copied secrets; enter the password again. Never attach secret files, real landscape/mapping files, client settings or unreviewed logs to public issues.

## Clients and validation

Choose Codex and/or Claude Desktop. Codex defaults to `CODEX_HOME/config.toml` or `~/.codex/config.toml`; Claude uses `%APPDATA%\Claude\claude_desktop_config.json`. Review paths and Preview changes. Only generated SAP entries are displayed; unrelated credentials are not shown. Conflicting `sap_rfc_call` / `sap_adt_mcp` entries require explicit replacement selection. Apply creates timestamped backups and preserves other servers. If the file changed after preview, preview again.

Clients run `SapMcpHost.exe --component rfc|adt --config <absolute settings path>`. RFC waiting for SDK is not registered. Restart client MCP servers after applying. The manager need not stay open. Generic MCP JSON export supports other clients and contains no passwords.

Verify local MCP checks startup and tools/list without contacting SAP. Test RFC/ADT connection explicitly sends a read-only ping to the selected system. Prepare VPN/network and SAP authorizations first. Upstream technical error text may retain its original language; the manager provides a localized failure summary.

## Language, settings and logs

English / Türkçe in the header applies immediately, saves preference and preserves open form values, including passwords. First run uses Turkish for Turkish system UI, English otherwise. UI language never changes SAP profile language, active accounts or client records. System/light/dark themes are available.

Settings manages mapping/secret/runtime paths and separate RFC/ADT log folders and levels: Off, Error, Warn, Info (default), Debug, Trace. Defaults: 30 days, 10 MB per file, 10 rotated files. Restart MCP in clients after path/log changes. Profile activation applies to subsequent calls without restart.

Manager logs cover installs, landscape loading, profiles and explicit tests. Host logs cover session/tool metadata, duration and error status. Raw business payloads and upstream stderr are excluded, and secrets/authorization headers are masked at every level. Current upstream packages lack full native severity/rotation support: the host owns logging and suppresses native payload tracing. Trace records metadata, not business payloads. Use Refresh logs or Open log folder.

## Repair, updates and uninstall

Re-run the EXE to repair manager files; recheck prerequisites and use Install / repair selected MCP components for payloads. Settings/profiles/secrets live outside the application directory and survive repair/update. Do not migrate by copying another Windows user's encrypted secrets.

Uninstall through Windows Installed apps. Only this host's SAP client records are removed, with backups. User data is preserved by default and in silent uninstall. Interactive uninstall has a separate explicit permanent-delete choice for the default owned data directory; custom external paths are not deleted. Securely back up settings/secrets if needed; DPAPI account restrictions also apply to backups.

See [Troubleshooting](TROUBLESHOOTING.md) and [Release notes](RELEASE-NOTES.md). This same guide is available through the app's Help button in the selected language.

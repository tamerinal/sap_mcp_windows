# User guide — v0.1.0-preview.3

[Türkçe](USER-GUIDE.tr.md) · [Home](../README.md)

## Installation and prerequisites

Download Online when runtime downloads are possible, Offline otherwise. Run the EXE as the Windows user who will run your MCP clients. The manager includes .NET and installs under `%LOCALAPPDATA%\Programs\SapMcpManager`; user data is in `%LOCALAPPDATA%\SapMcpManager`.

Both components start checked. Keep one or both; empty selection is rejected. The EXE places prepared archives on disk. In the manager, **Prerequisites → Check again → Install Required Applications** displays missing installable prerequisites, checked by default, with version/status/description. Use each row's Install or Install Selected. Microsoft Visual C++ may request UAC. Cancellation preserves completed packages; retry after correcting a failure. Checks repeat after installation. Then use **Install / repair selected MCP components** to install only selected servers.

Common checks cover Windows 10 22H2/11 x64, at least 1 GB free space, writable directories, PowerShell DPAPI commands, Node.js and catalog integrity. RFC also needs managed Python 3.11/PyRFC, Visual C++ runtimes and SDK. Basic ADT requires no Python/SDK. SDK-waiting RFC can be staged while ADT is used. Add/remove components later from Overview; removing a component preserves profiles/secrets.

## SDK now or later

Obtain NetWeaver RFC SDK for Windows x64 from SAP's official connector page with your authorized SAP account. SDK is never included. Extract and select the **parent `nwrfcsdk` folder**, containing `lib\sapnwrfc.dll` and `lib\libsapucum.dll` with matching dependencies. Select Verify and save SDK. This checks DLL architecture and actually loads the SDK through Python/PyRFC without a SAP login. Install Python/PyRFC first for the load test. Do not mix SDK versions' DLLs.

If no valid SDK is available, RFC stays blocked and basic ADT remains usable. Attach SDK later on the same page. Moving/deleting SDK blocks RFC again at startup; select the new path and revalidate.

SAP GUI is not required to run these MCP servers. RFC uses PyRFC and the user-supplied NetWeaver RFC SDK; basic ADT uses HTTP/HTTPS. You still need network/VPN access, valid SAP credentials and authorizations. Some ADT operations require ready RFC. No actual SAP password is changed by this application.

## Choose the source

Open **SAP connections**. The manager checks known Windows SAP GUI registry/install locations and suggests a mode. Detection is a hint: choose either mode and select **Use selected source**. Your choice is saved and is not automatically changed when SAP GUI is installed or removed. The two system lists are separate; they are not merged. Switching sources preserves saved accounts and protected secrets. Profiles for the same `host:sysnr` are shared by both clients and both source modes.

If the open account form has unsaved changes, source switching, refreshing or system editing asks before discarding them. Cancel keeps the current form. Switching English/Türkçe preserves field values. Restart MCP servers in Codex/Claude after a source/path change so they use the new XML path.

## Without SAP GUI: application-managed connections

1. Select **Application-managed connections → Use selected source**. An empty local catalog and an MCP-compatible XML are created automatically if needed.
2. Select **New connection**. Enter a descriptive name, application-server hostname/IP and a two-digit system number (00–99). Do not enter a URL or port in the host field. SID (up to three letters/digits) and SAProuter are optional.
3. Select **Save connection**. Then select the system and enter profile name, user, client, SAP language, password and optional ADT/BW URLs in the account form below. Save the profile and choose the shared active account.
4. Install/validate the required runtimes and your SDK for RFC. Configure the MCP clients. Local MCP verification checks startup/tool discovery; an explicit Test connection contacts SAP.

Use **Edit connection** to change the name, server, system number, SID or router. The ID stays stable. A host/system-number change shows the affected profiles and asks before moving them and their active mapping to the new target for both clients. An existing connection or profile target is never silently overwritten.

**Delete connection** shows the system and associated profiles. Confirming removes the local system, its account profiles and active mappings. DPAPI secret files are retained, as with profile deletion; it does not delete a SAP GUI entry or account on SAP. Cancel makes no change. Removing a shared target's profiles also affects that target when used from the other source mode.

## With SAP GUI: read-only system list

1. Add/change/remove systems in SAP Logon itself.
2. In the manager choose **SAP GUI connections (read-only)**. Choose the SAP GUI folder or select `SAPUILandscape.xml` / `SAPUILandscapeGlobal.xml` directly. If both files exist, choose which one to use.
3. Select **Use selected source**. The manager reads direct SAPGUI host:port entries. Later select **Refresh systems** after SAP Logon changes the source file.
4. Select a loaded system and manage its user/client profiles and DPAPI passwords in the manager. System New/Edit/Delete buttons remain disabled in this mode, including at the service layer.

The source XML is never a write target. You may point to an existing XML even if SAP GUI is not installed. A missing/broken XML displays a connection-source error; the manager's other pages remain accessible. Select a valid file and apply/refresh. There is no silent fallback to a different source. Message-server groups, SNC/SSO and landscape include expansion are not supported in this preview.

## Which file does what?

| File | Purpose and ownership |
|---|---|
| External `SAPUILandscape.xml` / `SAPUILandscapeGlobal.xml` | SAP GUI's system catalog; read-only to this manager. It is XML, not JSON. |
| `config/connections.json` | Application-owned system catalog with schemaVersion, stable IDs, names, hosts, system numbers, SID and router. No passwords. |
| `config/SAPMcpLandscape.xml` | Application-generated projection of the JSON catalog, read by the existing RFC/ADT MCPs. Do not edit it by hand. |
| `config/sap_credentials.json` | Account mappings using overridesByTarget/secretRef, generated from active profiles. It is not a system catalog. |
| `secrets/*.password.dpapi` | Passwords protected with DPAPI CurrentUser. Other users/computers may require re-entry. |

Paths above are relative to `%LOCALAPPDATA%\SapMcpManager` by default; custom configurations keep their own data directory. The UI shows the managed JSON path. Existing mapping JSON can still be imported through **Import mapping**; its source is preserved. Importing a mapping does not discover or create servers.

Settings schema remains version 1 with optional `connectionSourceMode`, `externalLandscapeFile` and `managedConnectionsFile`. Old settings continue to use their existing landscape as the external read-only source until you explicitly choose another mode. Existing profiles, DPAPI files and client records remain valid. The manager repairs/rebuilds its generated projection when reopening a saved managed configuration; it never repairs the SAP GUI XML by writing into it.

All candidates are validated before a save. If a bundle write fails, the manager attempts to restore the previous catalog, mapping, XML and settings, and reports failure. If restoration itself fails, close MCP clients and repair the indicated configuration before continuing. Keep application-owned outputs separate from the external landscape and each other. An in-flight SAP call may already have resolved its account; list changes apply to later calls.

## Troubleshooting and support

- Empty list: apply the selected source first, then create a local system or choose a supported external XML.
- Folder contains no candidates: select the XML directly; only the two standard filenames are searched in that selected folder.
- Duplicate/target conflict: use a different endpoint or manage the existing system; no automatic profile merging occurs.
- Invalid host: provide the server name/IP without `https://`, a port, slash or whitespace. ADT/BW URLs belong in the profile.
- Changing an external file: edit it in SAP Logon and refresh. Choose application-managed mode if you want local CRUD instead.
- Access denied/invalid JSON: correct folder permissions or the catalog, then refresh/reopen. Do not remove the last valid configuration to bypass an error.

See [Troubleshooting](TROUBLESHOOTING.md) and the [reporting guide](ISSUES.md). Do not attach real catalogs, mappings, SDKs, secret files or unreviewed logs to public issues. Example formats in `examples/` use fictional systems. Clean Windows and live SAP acceptance remains pending unless recorded in the release validation report.

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

Managed mode supports password-free SAProuter strings. Password segments `/W/` and legacy `/P/` are rejected rather than stored in JSON/XML; secret-aware router support requires a future MCP change. See [SAP route-string syntax](https://help.sap.com/saphelp_em92/helpdata/en/48/6c62b71d5a350ce10000000a42189d/content.htm).

## RFC SDK / ICU startup and EmptyStdout

Preview.3 fixes RFC worker startup on machines where the SDK is not already in Windows PATH. In **SAP RFC SDK**, select the SDK root containing **lib**, then **Verify and save SDK**. The lib folder must contain matching x64 sapnwrfc.dll, libsapucum.dll, icuuc57.dll, icudt57.dll and icuin57.dll. Install the managed Python/PyRFC and Visual C++ prerequisites first. Do not copy SDK DLLs into Windows or edit the global PATH.

The manager and Host build the same child-only SAPNWRFC_HOME and PATH (SDK lib first). Node passes these to Python; RFC-dependent ADT subprocesses use the same Host. The persisted sdkDirectory remains the source of truth. Existing profiles, DPAPI files and client registrations do not need recreation. SDK files remain user-supplied and are not included in either EXE.

**Ready** now requires x64 DLL checks, ICU loading/initialization and a separate Python/PyRFC SDK initialization probe. The probe uses no SAP destination, account or password; it accepts only the expected local missing-address RFC_INVALID_PARAMETER result. An import or version query alone is insufficient. Missing DLLs are caught before native SDK initialization. Errors show safe code, stage, class and available decimal/hex exit code; raw stderr, PATH and credentials are not shown. Windows has no POSIX signal value. Host/Node exit metadata is not a Python worker exit code.

After saving or upgrading, fully restart the RFC/ADT MCP servers in Codex/Claude (restart the client if needed). Then run the SDK check and the connection test again. On the affected environment, retry TED/TEX RFC_PING with an authorized account and record only a sanitized result in [issue #3](https://github.com/tamerinal/sap_mcp_windows/issues/3). The release does not claim this live acceptance has passed.

If EmptyStdout persists, record the application version and safe diagnostic identifiers. Worker exitCode/signal improvements require a new RFC package; they remain an [upstream development request](MCP-RFC-WORKER-DIAGNOSTICS.md). Do not upload raw native traces, PATH, passwords, DPAPI files or actual SAP mappings. Check network/account permissions separately once SDK initialization passes.

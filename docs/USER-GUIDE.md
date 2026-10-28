# User guide — v0.1.0-preview.6

[Türkçe](USER-GUIDE.tr.md) · [Home](../README.md)


New users start in **Basic**: [Basic guide](BASIC-SETUP.md). The detailed page instructions below describe **Advanced**; the full Basic guide is also included at the end of this help.

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


# ADT endpoints, TLS and diagnosis

[Home](../README.md) · [User guide](USER-GUIDE.md)

## Find and save the ADT URL

Ask your SAP Basis team for the workstation-accessible ADT web address. A working Eclipse ADT project can identify the intended system, but its RFC system number does not determine the HTTP port. An authorized administrator can read active HTTP/HTTPS services in SMICM → Goto → Services. The external Web Dispatcher/load-balancer address may differ from that internal address. Prefer the DNS name present in the certificate SAN. Enter a base URL such as https://sap.example.invalid:8443 (fictional); the application appends /sap/bc/adt/discovery itself. Automatic discovery proposes candidates without passwords and does not prove availability. If you cannot access SAP GUI or Eclipse, use the Basis request below. See SAP Help: https://help.sap.com/docs/SUPPORT_CONTENT/abapconn/3354079914.html

### Request to send to Basis

Please provide the ADT HTTP/HTTPS base URL reachable from my workstation, the web port, any reverse-proxy path prefix, VPN/proxy requirements and the verified root/intermediate CA chain. Please confirm that ADT discovery and read-only compatibility services are available to my SAP user/client. Do not send passwords.

The base URL is independent of the RFC system number and SAP client. Keep client/language/account in their own fields. Explicit adtBaseUrl wins. A trailing /sap/bc/adt is removed once; discovery is appended by the client. Queries, fragments, embedded credentials and deeper service URLs are rejected. Reverse-proxy prefixes, IPv4/IPv6 and custom ports are supported. HTTP sends credentials without TLS protection and is always marked with a warning.

Old profiles and DPAPI files are preserved. A legacy blank URL no longer triggers credential-bearing port guesses: select and save a confirmed URL first. Imported legacy URL lists remain in profile data; the first configured address is used only if the explicit field is blank. Address suggestions are opt-in, target-host-only and unauthenticated. Candidate responses including 401/404 do not prove ADT availability. Selecting a suggestion fills the form; save it before testing.

## Profile certificate trust

Save the profile first, then open **Profile certificates**. Obtain root/intermediate CA certificates or the verified self-signed public certificate from Basis over a trusted channel. Import PEM/DER (.pem/.cer/.crt), inspect Subject, Issuer, UTC validity, SAN and SHA-256, confirm the fingerprint with your administrator, then save the trust references. Public certificate files are copied to the application's profile-specific data folder. PFX/private keys are rejected. Removing a reference removes trust from future calls; orphan public files may remain on disk and are not automatically trusted.

Trust combines Node defaults, existing startup extra CAs, Windows trust and only this profile's files. Adding a CA here does not add it to Windows or another profile. Intermediate certificates complete a chain but do not become standalone roots. Hostname/IP SAN and validity still must match. A trusted self-signed certificate for another hostname still fails. Missing files, changed certificate fingerprints and malformed files block the connection.

**Inspect server certificate (no login)** returns unverified metadata without sending account credentials or granting trust. It intentionally permits an untrusted handshake only for inspection. Use a verified certificate file to add trust; do not trust a certificate merely because the network supplied it. An inspection failure may leave no certificate metadata if the network/TLS protocol itself is unavailable.

The advanced TLS-bypass checkbox is per-profile, off for new profiles and visibly warned. It is not a verified secure connection. There is no global TLS-disable operation. Use a correctly trusted certificate rather than leaving bypass enabled.

## Test the selected saved profile

**Test ADT connection** uses the saved selected profile through SapMcpHost and the actual MCP transport, including a non-active profile. A temporary private mapping contains only secret references; the shared active account for Codex/Claude does not change. Unsaved fields must be saved first. Old ADT packages are blocked with an install/repair message until their Preview.4 connection capability is present.

Stages: configuration → DNS/TCP → TLS for HTTPS → authentication → ADT discovery → read-only compatibility graph. Valid XML namespaces/structures are required. An open port, 401, arbitrary XML/HTML or root-path 404 is insufficient. No root-path 404 alone is interpreted as ADT absence. Anonymous resources do not prove the supplied account; the result remains unverified. First credential failure stops the test and automatic retries; do not repeatedly retry an unchanged wrong password.

Errors distinguish DNS, refused TCP, timeout, name/date/trust/chain TLS issues, 401 authentication, 403 authorization and service/content problems. Missing issuer versus missing trust root cannot always be determined from available evidence: TLS_CHAIN_UNRESOLVED reports that ambiguity. Ask Basis to check the complete chain and routing instead of disabling TLS globally.

Existing HTTP_PROXY/HTTPS_PROXY/ALL_PROXY/NO_PROXY variables are honored; HTTP proxy forwarding and HTTPS CONNECT use the shared transport. Proxy URLs/secrets and OS environment are not rewritten by Manager. With a proxy, network stages refer to the effective route; end-to-end origin TLS is still verified. Interactive proxy authentication, SSO/mTLS and new proxy management UI are not supplied.

Profile and certificate changes are read on the next call; no target-only URL cache can override them. Restart client MCP servers after installing the new ADT package, changing Node/environment or updating Windows trust. Settings, profiles, DPAPI secrets and client registrations are preserved across Manager upgrades.

## Support, tests and limits

Export the masked support report from the diagnosis dialog. It contains only approved stage/status/code metadata and masked configuration; no endpoint, account, certificate identity, token, password, cookie, headers or raw body. Do not attach real mapping, CA files, native traces or PATH to public issues.

Local synthetic HTTP/TLS tests cover profile isolation, chains, self-signed, SAN/date errors, redirects, proxies, authentication and false-positive content. Manager/Host/MCP consistency is checked against a local fixture without live SAP. Native keyboard/accessibility, clean Windows/GUI-free installation and authorized customer/live SAP acceptance remain pending until actually performed. Automatic OCSP/CRL/AIA retrieval, SSO/mTLS and SAP-side service/certificate changes are outside this delivery. Windows/Node trust rules are not identical to browser/Eclipse trust stores; a browser success alone is not proof of MCP trust.


# Basic and Advanced setup

[Home](../README.md) · [Türkçe](BASIC-SETUP.tr.md)

New installations open Basic. Existing configurations open Advanced; your choice is remembered. Use the top mode selector to switch. Existing Advanced pages retain their layout and behavior. Unsaved form values survive mode/language changes; no account is saved just by switching mode. Basic and Advanced share the connection catalog, profiles, DPAPI files and active account per system.

## 1. SDK and automatic preparation

Select the SAP NetWeaver RFC SDK folder, then **Prepare installation**. The application checks prerequisites, installs missing managed runtimes and current MCP packages, validates the SDK and performs local MCP initialization/tools-list checks. There is no second MCP installation button. Existing compatible packages are reused. Windows may request UAC for Visual C++ and a restart. SDK files must be supplied by you; they are never bundled.

If you do not have an SDK, select **Continue with ADT only, without an SDK**. ADT can proceed while RFC is shown as unavailable. Missing RFC dependencies or invalid SDK cannot become RFC success. Node/DPAPI/storage failures still block preparation. Paths, mapping and log settings use application defaults on new installations; existing customized settings remain available in Advanced.

## 2. One SAP connection

Choose an existing account or create one. Enter the application server (DNS/IPv4/IPv6, optional HTTPS port), user, password, client, SAP language and two-digit instance number. A blank password preserves the chosen account's saved secret. A new account requires a password. Basic stores it using the existing CurrentUser DPAPI format, not plaintext. It does not change your SAP password.

After the server or manual address changes, a 500 ms debounce starts an unauthenticated HTTPS search. Priority: explicit port, then 443/44300/44301/44302, then remaining 44300–44399 ports. Defaults are centralized: at most 16 concurrent probes, 1.5 seconds per probe and 15 seconds total, including DNS/TLS/HTTP. Slow or uncommon services can fall outside this bounded search; failure does not prove ADT is absent. No HTTP fallback, authorization headers, SAP cookies or redirect following is used. Changing server cancels prior work and invalidates old results.

Only a 200 response with the expected ADT discovery structure is a confirmed discovery. 401 is an authentication-required candidate, not success. HTML, arbitrary XML, 403 and redirects are separate outcomes. A confirmed discovery cancels remaining probes. Enter a manual HTTPS URL or port under **Enter an ADT address / port** when needed; that address takes precedence and is checked alone. Reverse proxy prefixes are supported; do not enter the deeper discovery service URL.

**How do I find the ADT port or address?** Ask Basis for the workstation-accessible HTTPS base URL/port, VPN/proxy requirements and verified public CA chain. An authorized administrator can inspect **SMICM → Goto → Services** in SAP GUI. The Web Dispatcher address can differ from the application server; explicitly enter that external URL if used. RFC instance number alone does not determine the web address. The field help includes a copyable Basis request. See the full [ADT/TLS guide](ADT-CONNECTIONS.md).

TLS chain, dates and DNS/IP name must validate. On a certificate problem, **Profile certificates** lets you inspect a public PEM/DER file and explicitly confirm its identity/fingerprint for this draft/profile. It never trusts a certificate just because it was received over the network; trust is not global and does not fix wrong names or dates. Basic does not allow TLS bypass. Existing Advanced certificate tools remain available.

Press **Save and verify**. Secure candidates are authenticated sequentially, never in parallel. The first explicit rejected login stops remaining candidate and RFC login attempts; correct credentials before retrying. Manager tests the real ADT MCP transport including discovery and a read-only compatibility request; anonymous access does not prove the supplied account. RFC is separately tested with the existing RFC_PING tool. If ADT fails for service/network/TLS reasons, RFC can still be tested. Ping success is not authorization for every RFC function.

An entered instance is used as-is. A same-server 443NN port can supply a tentative instance only; RFC ping must confirm connectivity. If no unique candidate exists, enter the two-digit instance; Basic never silently assumes 00. A Web Dispatcher port does not infer the application-server instance.

- Both verified: profile saved and full connection success shown.
- One verified: profile saved with separate results and a partial-completion warning; only the working MCP is offered to clients.
- Neither verified: stay on the connection step; previous profile/active mapping is preserved. Fix the settings and retry. Draft test files/secrets are cleaned up.

Tests use isolated temporary mappings and do not switch other clients' accounts. Successful save updates the shared active profile for that target. Other accounts/systems remain. Existing SAP GUI sources require an explicit, previewed conversion to a managed catalog before Basic editing; supported entries are preserved, source XML stays unchanged, unsupported/include/SNC/message-server entries stop conversion. Choose Advanced if conversion is unsuitable.

## 3. Codex / Claude Desktop

Review and apply the existing client integration. Only the independently verified components are included; unrelated registrations and backups/conflict handling are preserved. Existing unselected registrations are not automatically deleted. Connection changes invalidate the old verification. Restart the clients' MCP processes after installation/package changes. Clients start the existing Host on demand; no persistent Windows service is added and Manager need not stay open.

This preview is unsigned. Clean Windows and live SAP acceptance remain pending; local synthetic tests do not establish that every SAP installation is compatible. See release validation for actual outcomes and remaining limits.

## Preview.5 host-only discovery error (fixed in Preview.6)

Preview.5 could stop before any network request when the server was entered without a port. Manager sent an optional port as JSON `null`; the ADT discovery process incorrectly rejected it as `DISCOVERY_PORT_INVALID`. A generic message made this look like a network/certificate problem. RFC uses a separate path and could still pass. This failure did not establish whether ADT was reachable.

Upgrade Manager to Preview.6, then use **Prepare installation** in Basic or install/repair ADT in Advanced to install the updated ADT package. Restart client MCP processes. Existing profiles, DPAPI passwords and RFC packages are preserved. Manager now omits absent optional fields; the updated ADT package also accepts legacy null fields. Input/process errors show a specific safe code instead of claiming a remote network failure.

Retry the saved connection. For a known endpoint, **Enter an ADT address / port** takes priority over scanning. Ask Basis for the actual workstation-accessible HTTPS base URL if needed; the RFC instance number does not establish a web port. An IP address may not match the certificate's DNS name: use the approved matching DNS address and profile CA trust when required. Do not disable TLS validation. This fix does not prove a live system is reachable; VPN, ports, TLS and permissions still require validation. See [issue #6](https://github.com/tamerinal/sap_mcp_windows/issues/6).

# Troubleshooting

[Türkçe](TROUBLESHOOTING.tr.md) · [Guide](USER-GUIDE.md)

| Symptom | Action |
|---|---|
| Windows warns about the publisher | Preview EXEs are unsigned. Verify the SHA-256 and your organization's policy; do not disable security software. |
| Missing prerequisites / download fails | Check again; use Install on the failed row. Check HTTPS/proxy access or use Offline. Hash mismatch: delete the failed cached download and retry from the official release. |
| RFC waiting for SDK | Supply your own Windows x64 SDK, install Python/PyRFC and Visual C++ prerequisites, then Verify and save SDK. |
| SDK wrong architecture / DLL missing | Choose the parent nwrfcsdk folder; check both required DLLs and matching dependencies. x86/ARM SDK is incompatible. |
| SDK load fails despite DLLs | Check Python 3.11 x64, PyRFC cp311 win_amd64, VC++ 2013 and 2015+ runtimes, directory access and SDK version consistency. |
| ADT operation reports RFC_UNAVAILABLE | That operation requires verified RFC. Configure RFC/SDK first. The host blocks it before forwarding a SAP mutation. Basic ADT is still available. |
| Landscape empty / unsupported group | Use direct SAPGUI host:port entries. Message-server groups, SNC/SSO and include files are not expanded. |
| Password cannot be decrypted | Use the original Windows account/computer or enter the current SAP password again in the profile. |
| SAP connection test fails | Check VPN/DNS/firewall, host/sysnr, client/user/password, ADT URL/TLS and SAP authorizations. Avoid repeated failed logins. |
| Client config conflicts / changed since preview | Preview again, review names and select replacement only if intended. Timestamped backups are beside the client config. |
| Paths/log settings appear unchanged | Restart MCP servers in the client. Active profile changes affect subsequent calls. |
| Permission or invalid config error | Choose writable absolute paths and correct the file. Launching with `--config <absolute path>` supports an isolated configuration; it does not repair an invalid file automatically. |

Report version, Windows build, component, diagnostic ID and reproducible steps. **Do not upload passwords, DPAPI files, real landscape/mapping/client settings, access tokens or raw SAP payloads.** Describe errors using redacted text. See [Security](../SECURITY.md) for vulnerabilities.

Connection-source errors: choose SAP GUI read-only XML or the managed catalog explicitly. Missing/broken source files do not block other manager pages. Check the selected mode before editing; local CRUD is disabled in GUI mode. Follow [connection management](CONNECTIONS.md) for target migration, deletion and rollback recovery.

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

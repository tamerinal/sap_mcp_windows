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

# Troubleshooting

## 0.1.0-preview.11 — Fresh RFC readiness and ADT 0.4.3

Basic Verify recalculates RFC readiness from current settings after connection consent. Fixing the SDK or prerequisites in Advanced no longer requires restarting Manager. Python, PyRFC, Visual C++ runtimes, SDK, native import and RFC package blockers are reported separately. Explicit ADT-only mode remains `RFC_DISABLED_BY_MODE`.

A PyRFC file on disk does not prove native loading. A separate Python process imports `pyrfc.Connection` without preloading ICU or connecting to SAP. Native failure and timeout remain separate from missing dependencies and SAP ping outcomes. SDK settings affect child processes only, never persistent environment variables. Client registration requires both successful RFC verification and current readiness.

The package includes ADT 0.4.3: TLS warnings do not invalidate an otherwise successful connection; ping and repository tools share acceptance criteria. Origin-scoped certificate fingerprint pins are supported by the ADT package; no trust is granted automatically from the network. Reverify existing profiles. Online/Offline installers and SHA256 files are provided in the preview.11 prerelease.

## 0.1.0-preview.10 — 2026-11-02

Basic separates Save from Verify and never discovers while editing. SID is required. With explicit acceptance, HTTP/HTTPS works without uploaded certificates. RFC ping, server-list permission, RFC-assisted candidates, consented smart/full TCP scanning and final ADT authentication are separate stages. Denied RFC discovery preserves a successful ping. Failed RFC candidates offer a consented scan instead of claiming the service is inactive. Candidates continue after login rejection and stop at the first complete success.

Full TCP scanning targets 30 seconds and stops with partial results at 60 seconds; HTTP probing has a separate budget. ADT 0.4.2 discovery protocol 2 is required. Advanced, Host and shared production policy behavior are preserved. Online/Offline EXEs and SHA256 files are published in the GitHub prerelease. See the [Basic guide](BASIC-SETUP.md).

Older release notes below are historical; current Basic behavior is defined here and in the Basic guide.

## 0.1.0-preview.7 — 2026-10-28 (local release)

Basic discovers HTTP and HTTPS without sending credentials. HTTPS 443/44300–44399 and HTTP 80/80NN/8080/44380 are covered; trusted HTTPS wins. HTTP is visibly unencrypted. Certificate-only HTTPS failures require Advanced; verification is never bypassed. One selected endpoint receives account, ADT discovery and read-only compatibility verification.

ADT 0.4.0 normalizes BOM/line endings in encrypted DPAPI files without trimming the real password. Discovery and account results are separate; edits invalidate prior results. Discovery IDs, timestamps and safe diagnostics persist. RFC/runtime assets are retained. See the delivered verification report for actual local/live results; clean Windows and other-machine acceptance remain separate. Not published to GitHub.


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

After saving or upgrading, fully restart the RFC/ADT MCP servers in Codex/Claude (restart the client if needed). Then run the SDK check and the connection test again. Verify RFC_PING with an authorized account after upgrading and share only a sanitized result in [issue #3](https://github.com/tamerinal/sap_mcp_windows/issues/3). The release does not claim this live acceptance has passed.

If EmptyStdout persists, record the application version and safe diagnostic identifiers. Worker exitCode/signal improvements require a new RFC package; they remain an [upstream development request](MCP-RFC-WORKER-DIAGNOSTICS.md). Do not upload raw native traces, PATH, passwords, DPAPI files or actual SAP mappings. Check network/account permissions separately once SDK initialization passes.


# ADT endpoints, TLS and diagnosis

[Home](../README.md) · [User guide](USER-GUIDE.md)

## Find and save the ADT URL

### 1. What address is required?

ABAP Development Tools (ADT) accesses the SAP development services through HTTP/HTTPS. Enter the base URL, for example https://sap.example.invalid:8443. This is a fictional documentation example, not a real system address. Include a reverse-proxy path prefix only when supplied by Basis. Manager appends /sap/bc/adt/discovery; do not append discovery or another service resource yourself.

### 2. Use automatic discovery and verification

Enter the system and profile details, then select Automatically discover and verify. Review the network and transport warnings before approving a scan or verification. Discovery identifies candidates without a password; subsequent verification can use your account. Review the separate ADT/RFC results and the URL/TLS fields, then select Save to apply the profile. An open port or a browser login prompt alone does not confirm a usable ADT connection.

### 3. Obtain the authoritative address from SAP Basis

Ask for the externally reachable DNS name, protocol, port and any path prefix. A working Eclipse ADT project can help identify the intended system. The RFC instance number does not determine the HTTP/HTTPS port. If SAP GUI or Eclipse is unavailable, copy the request below and send it to your Basis team through your usual support channel.

### 4. Administrative checks

An authorized administrator can inspect active HTTP/HTTPS services in SMICM → Goto → Services, and check the relevant ADT service under default_host → sap → bc → adt in transaction SICF. These are administrative checks; service activation or configuration changes must follow your organization’s procedures. An external Web Dispatcher or load balancer may expose a different host, port or path from the application server.

### 5. HTTPS, certificates and official documentation

Prefer HTTPS and a DNS name covered by the server certificate. Request the approved public CA chain if certificate trust is incomplete. HTTP does not encrypt credentials, and disabling TLS verification does not establish server identity. Read the displayed warnings before proceeding. Use the SAP documentation links below and choose the guide applicable to your SAP release; the linked back-end configuration PDF describes the 2022 release.

[SAP ADT documentation](https://help.sap.com/docs/ABAP_DEVELOPMENT_TOOLS_FOR_ECLIPSE) · [SAP back-end configuration guide (2022)](https://help.sap.com/doc/2e65ad9a26c84878b1413009f8ac07c3/202210.000/en-US/config_guide_system_backend_abap_development_tools.pdf)

### Request to send to Basis

Request to SAP Basis

To configure SAP MCP Manager, please provide the workstation-accessible ADT base URL for the intended SAP system, including protocol, DNS name, port and any reverse-proxy path prefix. Please specify VPN/proxy and firewall requirements, and provide the approved public root/intermediate CA certificates when needed. Please confirm that the required ADT ICF services are active and that my user/client has permission to access discovery and the intended read-only ADT resources. Please indicate whether the external Web Dispatcher address differs from the application server address. Do not include passwords, private keys or other credentials in your response.

The base URL is independent of the RFC system number and SAP client. Keep client/language/account in their own fields. Explicit adtBaseUrl wins. A trailing /sap/bc/adt is removed once; discovery is appended by the client. Queries, fragments, embedded credentials and deeper service URLs are rejected. Reverse-proxy prefixes, IPv4/IPv6 and custom ports are supported. HTTP sends credentials without TLS protection and is always marked with a warning.

Old profiles and DPAPI files are preserved. Advanced automatic discovery and verification evaluates the unsaved form; only Save commits the profile. The former address-suggestion controls and timeout field are removed from the UI. Existing timeout/discovery values remain compatible and are preserved when editing profiles.

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

Local synthetic HTTP/TLS tests cover profile isolation, chains, self-signed, SAN/date errors, redirects, proxies, authentication and false-positive content. Manager/Host/MCP consistency is checked against a local fixture without live SAP. The user confirmed Basic connections and Advanced multiple-profile connections on two machines. Native keyboard/accessibility and clean Windows/GUI-free VM acceptance remain separate, unperformed checks. Automatic OCSP/CRL/AIA retrieval, SSO/mTLS and SAP-side service/certificate changes are outside this delivery. Windows/Node trust rules are not identical to browser/Eclipse trust stores; a browser success alone is not proof of MCP trust.


# Basic setup — 0.1.0-preview.10

Basic configures SAP RFC and ADT independently. Existing package preparation, maintenance, profiles, DPAPI secrets and client previews are preserved. Advanced retains its certificate tools and connection policy.

## Prepare and save

Prepare the runtime/MCP packages first. Choose an RFC SDK directory, or continue with ADT only. Updating Manager also requires updating its ADT package to 0.4.2 (Basic discovery protocol 2). An older helper is rejected with `BASIC_DISCOVERY_UPGRADE_REQUIRED` before it is started.

Enter host, two-digit instance, three-character alphanumeric SID, client, user, password and SAP language. SID is required before verification; a detected SID mismatch prevents committing. An existing profile's blank password preserves its DPAPI secret. Saving does not change the password in SAP.

**Save** only stores local configuration and its protected password reference. It never starts network tests or discovery. The saved profile requires verification before MCP entries can be installed. Opening the app, selecting a profile and editing fields do not start network scans.

## Verify with consent

**Verify** starts the cancelable workflow. Basic does not require certificate files. HTTP is unencrypted; HTTPS encrypts traffic but Basic does not verify the server certificate. A permanent warning and explicit acceptance precede network operations. Acceptance lasts for the current profile/host/instance in this wizard session. Use Advanced for certificate trust configuration.

RFC ping runs first when its prerequisites are available. A successful ping is retained even if `TH_SERVER_LIST` is denied. SAP authorization errors and a local function allowlist restriction are reported separately. If server information is available, a dialog offers RFC-assisted discovery across every returned application host/instance. RFC supplies candidate hosts/instances, not the actual ICM service ports.

If RFC discovery is unavailable, declined or finds no usable candidate, the application requests separate automatic-scan consent. RFC candidate failure does not prove the ADT service is inactive.

The smart scan checks instance-derived ports, 80/443, 8080/8443, 8000–8099, 44300–44399 and Java ports. If no ADT candidate is found, another dialog offers ports 1–65535 on the entered host only. Raw TCP connects use 300 ms and adaptive concurrency 512–1024, reduced under resource pressure. The full TCP scan targets 30 seconds and stops at 60 seconds. Slow networks can leave ports untested; incomplete results are explicitly reported.

Open ports are probed using both HTTP and HTTPS. Each HTTP phase has a separate 30-second budget, up to 32 requests in flight and a 1.5-second request timeout. TCP and HTTP progress are separate; the entire verification is not guaranteed to finish in 30 seconds.

## Candidate authentication and results

Only discovery responses 200/401 can lead to ADT candidate validation. An open port, 404 or 503 is not ADT evidence. HTML or invalid XML never establishes successful ADT access. Redirects are not followed automatically.

TCP and HTTP discovery carry no account, password or cookies. RFC uses credentials at its initial connection step. ADT credentials are sent only after anonymous discovery finishes. Candidates are tried sequentially, including after login rejection, and stop at the first complete success. Valid discovery content ranks before 401 candidates; HTTPS and discovery priority break ties.

Authentication, discovery and read-only compatibility must all pass. Basic accepts a usable connection with a visible unverified-certificate warning without changing the shared ADT policy's secure-verification result. Anonymous access alone does not prove the supplied credentials.

The found address is shown in an editable field. If verification fails, enter an HTTP/HTTPS address manually, including a Web Dispatcher/proxy path if required. A trailing `/sap/bc/adt` is removed once. Manual verification probes the exact normalized address without port scanning.

ADT and RFC results are independent. Only verified components are offered to Codex, Claude Desktop and Claude Code. RFC-only and ADT-only continuation are supported. If neither works, save configuration only or go back. Existing usable records are preserved on failed validation. Previous validation becomes ineligible when connection details or required packages change; older validation-policy records require a new explicit verification.

During verification, Save, Next, navigation and client installation are disabled. Cancel stops subprocesses and discards late results. Temporary credentials and mappings are removed. No password enters discovery input, progress events or logs.

## Release checks

Local fixtures cover HTTP, self-signed HTTPS, authorization branches, consent rejection, cancellation, all-port scanning and client eligibility. Live SAP acceptance is separately reported; a fixture pass does not establish live connectivity. Online/Offline EXEs and SHA256 files are published in the GitHub prerelease.

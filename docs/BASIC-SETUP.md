# Basic setup — 0.1.0-preview.11

## 0.1.0-preview.11 — Fresh RFC readiness and ADT 0.4.3

Basic Verify recalculates RFC readiness from current settings after connection consent. Fixing the SDK or prerequisites in Advanced no longer requires restarting Manager. Python, PyRFC, Visual C++ runtimes, SDK, native import and RFC package blockers are reported separately. Explicit ADT-only mode remains `RFC_DISABLED_BY_MODE`.

A PyRFC file on disk does not prove native loading. A separate Python process imports `pyrfc.Connection` without preloading ICU or connecting to SAP. Native failure and timeout remain separate from missing dependencies and SAP ping outcomes. SDK settings affect child processes only, never persistent environment variables. Client registration requires both successful RFC verification and current readiness.

The package includes ADT 0.4.3: TLS warnings do not invalidate an otherwise successful connection; ping and repository tools share acceptance criteria. Origin-scoped certificate fingerprint pins are supported by the ADT package; no trust is granted automatically from the network. Reverify existing profiles. Online/Offline installers and SHA256 files are provided in the preview.11 prerelease.

Basic configures SAP RFC and ADT independently. Existing package preparation, maintenance, profiles, DPAPI secrets and client previews are preserved. Advanced retains its certificate tools and connection policy.

## Prepare and save

Prepare the runtime/MCP packages first. Choose an RFC SDK directory, or continue with ADT only. Updating Manager also requires updating its ADT package to 0.4.3 (Basic discovery protocol 2). An older helper is rejected with `BASIC_DISCOVERY_UPGRADE_REQUIRED` before it is started.

Enter host, two-digit instance, three-character alphanumeric SID, client, user, password and SAP language. SID is required before verification; a detected SID mismatch prevents committing. An existing profile's blank password preserves its DPAPI secret. Saving does not change the password in SAP.

**Save** only stores local configuration and its protected password reference. It never starts network tests or discovery. The saved profile requires verification before MCP entries can be installed. Opening the app, selecting a profile and editing fields do not start network scans.

## Verify with consent

**Verify** starts the cancelable workflow. Basic does not require certificate files. HTTP is unencrypted; HTTPS encrypts traffic but Basic does not verify the server certificate. A permanent warning and explicit acceptance precede network operations. Acceptance lasts for the current profile/host/instance in this wizard session. Use Advanced for certificate trust configuration.

RFC ping runs first when its prerequisites are available. A successful ping is retained even if `TH_SERVER_LIST` is denied. SAP authorization errors and a local function allowlist restriction are reported separately. If server information is available, a dialog offers RFC-assisted discovery across every returned application host/instance. RFC supplies candidate hosts/instances, not the actual ICM service ports.

If RFC discovery is unavailable, declined or finds no usable candidate, the application requests separate automatic-scan consent. RFC candidate failure does not prove the ADT service is inactive.

The smart scan checks instance-derived ports, 80/443, 8080/8443, 8000–8099, 44300–44399 and Java ports. If no ADT candidate is found, another dialog offers ports 1–65535 on the entered host only. Raw TCP connects use 250 ms and adaptive concurrency 512–1024, reduced under resource pressure. The full TCP scan targets 30 seconds and stops at 60 seconds. Slow networks can leave ports untested; incomplete results are explicitly reported.

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

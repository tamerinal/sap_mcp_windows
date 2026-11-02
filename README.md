# SAP MCP Manager for Windows

## 0.1.0-preview.10 — 2026-11-02

Basic separates Save from Verify and never discovers while editing. SID is required. With explicit acceptance, HTTP/HTTPS works without uploaded certificates. RFC ping, server-list permission, RFC-assisted candidates, consented smart/full TCP scanning and final ADT authentication are separate stages. Denied RFC discovery preserves a successful ping. Failed RFC candidates offer a consented scan instead of claiming the service is inactive. Candidates continue after login rejection and stop at the first complete success.

Full TCP scanning targets 30 seconds and stops with partial results at 60 seconds; HTTP probing has a separate budget. ADT 0.4.2 discovery protocol 2 is required. Advanced, Host and shared production policy behavior are preserved. Online/Offline EXEs and SHA256 files are published in the GitHub prerelease. See the [Basic guide](docs/BASIC-SETUP.md).

[Türkçe](README.tr.md) · [User guide](docs/USER-GUIDE.md) · [Troubleshooting](docs/TROUBLESHOOTING.md)

Install SAP RFC and SAP ADT MCP servers, manage SAP connection profiles and Windows-protected passwords, and connect Codex or Claude Desktop from a desktop application. No source build, GitHub account, Git or compiler is required to use the installers.

![SAP MCP Manager overview](docs/images/overview-en.png)

**Last published preview: v0.1.0-preview.6.** Windows 10 22H2 and Windows 11 **x64**. English/Turkish and light/dark themes. macOS is planned. Installers are unsigned; clean Windows 10/11 and live SAP acceptance tests remain pending. Read the [release notes](docs/RELEASE-NOTES.md).

## Download

| Installer | Choose this when |
|---|---|
| [Online EXE](https://github.com/tamerinal/sap_mcp_windows/releases/download/v0.1.0-preview.10/SAP-MCP-Manager-0.1.0-preview.10-Windows-x64-Online.exe) | Internet is available to download missing runtimes. MCP packages and the self-contained manager are included. |
| [Offline EXE](https://github.com/tamerinal/sap_mcp_windows/releases/download/v0.1.0-preview.10/SAP-MCP-Manager-0.1.0-preview.10-Windows-x64-Offline.exe) | The target cannot download runtimes. Includes the same MCP packages plus runtime installers. |

[Preview assets and checksums](https://github.com/tamerinal/sap_mcp_windows/releases/tag/v0.1.0-preview.10). Compare `Get-FileHash -Algorithm SHA256` with `SHA256SUMS.txt`. SAP access still requires your corporate network. **SAP NetWeaver RFC SDK is not included in either EXE.**

## First setup

1. Install the Online/Offline EXE under your normal Windows account and open Manager. New installations start in **Basic**, existing configurations in **Advanced**; switch from the header.
2. In Basic select the SDK folder or **Continue with ADT only, without an SDK**. **Prepare installation** automatically checks/installs prerequisites and prepares MCP packages.
3. Enter host, instance, SID, client, user and password. **Save** stores local configuration only; **Verify** starts RFC and anonymous ADT discovery after warning/consent.
4. One working connection permits a warning and continuation with only its MCP; neither working preserves the previous profile. Preview/apply Codex/Claude configuration and restart client MCP processes.

The [Basic guide](docs/BASIC-SETUP.md) explains the flow. **Advanced** retains component selection, individual prerequisites, SDK, multiple connections/profiles, clients and logs; see the [full guide](docs/USER-GUIDE.md). Manager need not stay open. Switching language/mode preserves forms and does not change the SAP profile language.

## What you provide

- SAP account, client and authorizations for the intended operations.
- Reachable SAP servers / ADT endpoints, VPN, DNS, firewall access and appropriate TLS certificates.
- Server host/system-number details, or an existing SAP GUI landscape with direct SAPGUI host/port connections. Message-server groups, SNC/SSO and include expansion are outside this preview.
- Your own licensed RFC SDK for RFC operations. The manager does not grant SAP access or change SAP-side passwords.

See [Connection sources: with or without SAP GUI](docs/CONNECTIONS.md) for add/edit/delete, source selection and file formats.

## Bugs and feature requests

Use the [bug report](https://github.com/tamerinal/sap_mcp_windows/issues/new?template=bug_report.yml) or [feature request](https://github.com/tamerinal/sap_mcp_windows/issues/new?template=feature_request.yml) form. The [reporting and tracking guide](docs/ISSUES.md) explains required information, statuses, priorities and filtered queues. Follow progress and release links in the issue, and Subscribe for GitHub notifications. Do not post credentials or real SAP files. Reports require a GitHub account; the app does not send reports automatically.

## Documentation and licensing

- [User guide](docs/USER-GUIDE.md): SDK later, profiles, password updates, clients, logs, repair and uninstall.
- [Troubleshooting](docs/TROUBLESHOOTING.md), [validation](docs/VALIDATION.md), [developer/build/release guide](docs/DEVELOPMENT.md).
- [Changelog](CHANGELOG.md), [contributing](CONTRIBUTING.md), [security](SECURITY.md).
- [MIT](LICENSE) covers author-owned published code. [Third-party notices](THIRD-PARTY-NOTICES.md) retain separate licenses.

RFC/ADT source repositories remain private. **Compiled MCP JavaScript and dependencies can be extracted from the public EXEs**; installers do not provide source confidentiality. Distribution-only path adaptations are recorded in each package's `build-info.json`. SDK files, credentials, real landscapes and logs are excluded from this repository and installers.

Preview.3 fixes SDK/ICU startup when SDK lib is absent from PATH. After upgrading, verify the saved SDK folder and restart client MCP servers. [SDK startup troubleshooting](docs/TROUBLESHOOTING.md#rfc-sdk--icu-startup-and-emptystdout) · [Issue #3](https://github.com/tamerinal/sap_mcp_windows/issues/3). Live TED/TEX acceptance is pending.

Preview.4 adds explicit ADT endpoints, profile certificate trust, staged diagnosis and address help. **Upgrade the Manager, install/repair the ADT component, then restart client MCP servers.** Legacy blank URLs need a confirmed address before authentication. [ADT/TLS guide](docs/ADT-CONNECTIONS.md) · [Issue #4](https://github.com/tamerinal/sap_mcp_windows/issues/4).


[Basic setup and automatic ADT discovery](docs/BASIC-SETUP.md).

## Earlier local release notes

## 0.1.0-preview.9 — SID and MCP output fixes

RFC 0.1.1 and ADT 0.4.1 return complete JSON in text as well as structured content. Legacy ADT list/ping responses follow their schemas. Syntax diagnostics, DDIC details, class/interface components, search descriptions and BW endpoint failures are visible to text-only clients. Both tool name sets remain available.

SID is required when saving connections/profiles. Basic can obtain it from a successful RFC connection, or the user can enter it. A backend SID mismatch prevents saving. Existing blank records remain readable. HTTP profiles retain the plaintext warning; TLS inspection explains that certificates do not apply to HTTP. Configured custom URLs are included first in endpoint discovery.

Native MCP operation/session logs are separate from Host process logs, honor level/rotation/retention, exclude SAP payloads, and expose operation IDs. Backend release information remains unavailable when SAP does not explicitly expose it. UTF-8 and declared Turkish response encodings are decoded at the HTTP boundary.

Local Online/Offline installers only; no GitHub publication. Live writes and activations are excluded from acceptance.

New local release: **0.1.0-preview.9**. Basic status summary, reusable setup screens, and separate Codex / Claude Desktop / Claude Code integrations. [Release notes](docs/RELEASE-NOTES.md). Not published to GitHub.

New local release: **0.1.0-preview.7** (ADT 0.4.0). Local Online/Offline installers are in `artifacts/installers`; GitHub links below refer to the previously published preview.6 release.

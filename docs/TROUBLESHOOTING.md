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

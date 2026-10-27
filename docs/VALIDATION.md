# Validation and acceptance matrix

[Türkçe](VALIDATION.tr.md)

Machine-readable local results: `artifacts/verification/report.json`; test TRX: `artifacts/test-results`; installer smoke results: `artifacts/installer-smoke-results.json`. These are local artifacts, not customer-data uploads. A sanitized release summary reports actual outcomes. Automated tests never log into a real SAP system.

## Public automated tests

- Atomic settings writes, invalid schema/value preservation, backward-compatible missing uiLanguage.
- Landscape parsing, aliases/router/target keys and DTD/XXE rejection.
- DPAPI ↔ Windows PowerShell compatibility, Turkish characters and spaces; actionable corrupt-secret errors.
- Multiple profiles/shared active profile, mapping import precedence and original-file preservation.
- Codex TOML/Claude JSON merging, backups, conflicts, concurrent edits, unrelated servers, multiline TOML and quoted dotted names.
- Archive traversal/link rejection, SHA-256 mismatch preserving existing installation.
- Log levels, redaction, rolling/retention and Off behavior; RFC-required ADT guards.
- Six UI pages × two languages × two themes × two widths; switching language preserves live unsaved name/password controls and SAP language.

## Local release checks

Verify-Release.ps1 uses isolated data under artifacts/verification and locally hash-verified catalog assets. Runtime URLs are blanked to exercise offline extraction. Checks cover ADT-only, RFC-only and both, SDK absent/waiting diagnosis, provided SDK actual load, real host initialization/tools/list, RFC guards before mutation and Off logging. SDK is read for loading only, never copied to release. Test-Installers.ps1 covers Online ADT-only, Offline both, Offline RFC-only and empty selection rejection; self-contained files, persisted selection, asset hashes and uninstall are checked. These are development-machine smoke tests, not clean VM acceptance.

## Pending clean-machine/manual acceptance

| Environment/scenario | Required result |
|---|---|
| Clean Windows 10 22H2 / 11 x64, standard user | UI opens without developer tools or preinstalled runtimes; only VC++ may need UAC. |
| Online with empty cache | Pinned HTTPS download, network failure, cancellation and retry work. |
| Offline with network disabled | All runtimes install; SDK remains user-supplied. |
| Both / RFC-only / ADT-only / empty | Only selected servers install; empty is blocked. |
| SDK absent, wrong architecture, missing DLL | RFC blocked, basic ADT usable; later SDK activation works. |
| SDK moved, invalid folder permissions, locked files | Clear errors; previous configuration/payload preserved. |
| Actual SAP authorized user, explicit ping | RFC/ADT connect; no automatic repeated failed login. |
| Two clients, profile activation | Shared account applies to subsequent calls. |
| Native desktop keyboard/accessibility, widths/themes/languages | Clear labels, usable navigation, no clipping; headless rendering is supplementary. |
| Upgrade/repair/uninstall | Profiles/secrets preserved; optional explicit default-data removal works. |
| Signing and enterprise policy | Appropriate certificate and approved installer policy for production. |

Clean Windows VM and live SAP rows remain **pending** until evidence is collected. macOS, SNC/SSO, message-server groups, real SAP password changes and automatic updates are outside v1 preview.

## Connection-source acceptance

Managed JSON empty/create/edit/delete/reopen, XML projection consumed by both existing MCPs, target/profile migration, retained DPAPI files, duplicate/conflicting targets, read-only GUI source, folder candidates, broken source without manager failure, rollback and cancelled source-change dialogs are covered. Headless UI coverage includes both source modes, bilingual managed screens/editor and existing locale/theme/width coverage. Clean GUI-free Windows and live SAP remain pending.

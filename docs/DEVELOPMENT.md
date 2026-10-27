# Developer guide

[Türkçe](DEVELOPMENT.tr.md) · [Home](../README.md)

## Architecture

`SapMcp.Desktop` is an Avalonia/.NET 10 per-user manager. `SapMcp.Core` owns schema-1 settings, atomic saves, DPAPI, landscape/profile mapping, package verification, prerequisites, client merges and redacted rolling logs. `SapMcp.Host` reads current settings independently of the UI, constructs explicit process environment and proxies MCP stdio. RFC readiness is checked at each launch. ADT operations requiring RFC are conservatively blocked before forwarding when RFC is unavailable. Existing MCP tool names and parameters remain unchanged.

`Resources/Strings.json` in Core is the shared English/Turkish catalog; stable resource keys are independent of MCP diagnostic IDs. Optional `uiLanguage` preserves schema-1 compatibility. UI redisplay updates labels without rebuilding open forms or writing their values. Never send user-entered TextBox content through localization. Platform adapters for macOS secret storage/install are future work.

Connection sources implement a common reader interface: external SAP GUI XML is read-only; managed JSON supports validated CRUD and generates owned XML for unchanged MCP packages. Source mode and paths are optional schema-1 settings. A connection save stages validated content, serializes changes through a gate and rolls back the catalog, mapping, projection and settings on write failure; cancellation also rolls back. This is exception recovery, not a crash-proof multi-file database transaction. Password files are outside the catalog. Detection is advisory, saved mode wins, and legacy landscape settings remain external. See [connection sources](CONNECTIONS.md).

## Public source build and tests

Requirements: Windows x64, .NET SDK 10 (see global.json). No private repository or SAP account is needed:

```powershell
dotnet restore SapMcpManager.slnx --locked-mode
dotnet build SapMcpManager.slnx -c Release --no-restore
dotnet test SapMcpManager.slnx -c Release --no-build --logger trx
dotnet run --project src/SapMcp.Desktop
```

The source-built UI opens without payload; installation checks report the absent catalog. Unit tests cover DPAPI/PowerShell compatibility, atomic saves, import/profile precedence, client preservation, archives, hashes, logs and protocol guards. Headless Avalonia tests render six pages across two languages/themes/widths and preserve unsaved fields/passwords when switching language. Set `SAP_MCP_UI_SNAPSHOTS` to an output directory for PNGs. GitHub Actions performs this public build/test on Windows; it does not access private sources, SDK or SAP. Integration is a separate executable, not part of public test discovery.

## Authorized payload preparation

Only release maintainers with access to the private RFC/ADT sources and an approved cp311 win_amd64 PyRFC build can prepare installers. End users never compile code. Pass explicit paths; no maintainer machine paths are defaults:

```powershell
./scripts/Prepare-Payload.ps1 -RfcSource $rfcSource -AdtSource $adtSource -PyRfcSitePackages $approvedSitePackages
./scripts/Build.ps1 -SkipPayload -InnoCompiler $iscc
./scripts/Verify-Release.ps1 -SdkDirectory $userSuppliedSdk
./scripts/Test-Installers.ps1
./scripts/Test-Publication.ps1
```

`Build.ps1` can prepare payload too when all three source parameters are supplied. Node/Python/VC++ versions and hashes are frozen in `payload/sources.lock.json`; `-RefreshRuntimePins` intentionally updates them and requires review. Downloaded assets are hash-checked; Microsoft runtime signatures are checked when refreshing pins. MCPs build in isolated staging copies using npm ci/build/prune. Distribution adaptations replace machine-specific config defaults; originals stay untouched. `build-info.json` records commit, dirty snapshot status and adaptations. PyRFC metadata records approved upstream commit and original/distributed native hashes; PDB files are omitted and the CodeView PDB pathname is neutralized without changing executable code. An SDK load test must pass after this adaptation.

Packages contain compiled code, production dependencies, lockfiles and provenance, never SAP SDK/customer files. The prepared PyRFC ZIP is a release input; a reproducible upstream wheel pipeline is still requested. Both EXEs include prepared MCPs; Offline adds pinned runtime cache. Online downloads missing runtimes, not private source code. Inno Setup 6.7.3 compiler is a build tool, never distributed. This builder reports non-commercial use: obtain an appropriate Inno license for commercial build/distribution. MIT for our code does not replace Inno or runtime terms.

## Release process

1. Keep `YAPAY_ZEKA` history, including the initial commit. Commit code, tests, docs, scripts, lockfiles, examples and notices; exclude local tools/output/customer data via .gitignore.
2. Align Directory.Build.props, installer/versioned asset names, host diagnostics, payload catalog and release notes. Test compatibility with settings lacking uiLanguage.
3. Prepare, build, run core/UI/integration/installer checks, then Test-Publication. The publication gate scans source and distribution content for machine paths, secrets and SDK. Review screenshots and reports for personal information too.
4. Copy a sanitized validation summary and English/Turkish release notes into assets. Generate SHA256SUMS.txt for the two EXEs and documentation archive. Review licenses and provenance. Do not label clean VM/live SAP tests complete unless performed.
5. Push code and wait for Public Windows CI. Create tag `v0.1.0-preview.3` at the tested commit, publish a **prerelease**, upload both EXEs, guide archive, SHA files, notes and validation summary. README links must target the tag, not latest (prereleases are not stable latest).
6. Download assets over unauthenticated HTTPS and compare hashes. Check README/guide links while signed out. A failure in any gate blocks publication until corrected.

No automatic update server or signing certificate is supplied in this preview. Installer smoke tests refuse to run if an existing user installation is registered. They install/uninstall in workspace artifacts and preserve user data; use a clean VM for production acceptance.

## Upstream requests

The [RFC request](MCP-RFC-DEVELOPMENT-REQUEST.md) and [ADT request](MCP-ADT-DEVELOPMENT-REQUEST.md) specify portable releases, diagnose --json, configurable paths, actual log filtering/retention, reproducible PyRFC and standalone ADT/RFC guards. These are documents for separate projects; no automatic messages or source changes are made there.

## SDK runtime / Preview.3

Shared SDK environment is constructed in Core and reused by launch/validation. SDK probes run in an empty owned scratch directory with a 20-second limit and closed stdin, produce schema-1 progress and a final success record, and never receive a target/profile. Only whitelisted metadata crosses the error boundary. Requirement gains an optional diagnostic object (code/stage/errorClass/exitCode/exitCodeHex/source/signal); settings schema and MCP wire tools stay unchanged.

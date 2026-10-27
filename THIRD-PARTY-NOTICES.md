# Third-party notices

[Türkçe](THIRD-PARTY-NOTICES.tr.md)

The root [MIT license](LICENSE) applies to author-owned published code, not to SAP or other vendors' components. SAP, ABAP, SAP GUI and NetWeaver are SAP SE trademarks; this is an independent project. **SAP NetWeaver RFC SDK is not distributed** and must be obtained/licensed by the user through SAP.

| Component | Terms / retained notices |
|---|---|
| .NET runtime / Microsoft libraries | MIT and runtime third-party notices in the installed runtime files. |
| Avalonia 12.1.4 | MIT. Inter font: SIL Open Font License 1.1. |
| Tomlyn | BSD-2-Clause; package metadata/license retained in NuGet. |
| Node.js | MIT and third-party licenses in the runtime archive's LICENSE. |
| CPython / python-build-standalone | PSF and bundled third-party terms in the runtime archive. |
| PyRFC 3.4 | Apache-2.0 and retained dist-info license/metadata. Approved source commit and native adaptation hashes are recorded in build-info.json. |
| Microsoft Visual C++ Redistributables | Microsoft's accompanying terms, not MIT. The original signed installers are included/downloaded. |
| MCP npm dependencies | Their individual package licenses, retained in node_modules and package-lock.json. |
| Inno Setup 6.7.3 | Build tool only; compiler not redistributed. This build environment reports non-commercial use. Commercial build/distribution requires an appropriate Inno license. |

RFC/ADT owner-written compiled code is distributed with the owner's MIT license; dependency notices remain separate. Source repositories remain private but EXE contents are extractable. The generated dependency inventory `payload/dependency-notices.json` in the installation records package versions, declared licenses and notice-file locations for both MCPs. Missing or nonstandard license declarations require maintainer review before a later release. See [Inno licensing](https://jrsoftware.org/isinfo.php) and [SAP SDK](https://support.sap.com/en/product/connectors/nwrfcsdk.html).

# Üçüncü taraf bildirimleri

[English](THIRD-PARTY-NOTICES.md)

Kökteki [MIT lisansı](LICENSE) yazara ait yayımlanan koda uygulanır; SAP veya diğer sağlayıcıların bileşenlerine uygulanmaz. SAP, ABAP, SAP GUI ve NetWeaver SAP SE markalarıdır; bu bağımsız bir projedir. **SAP NetWeaver RFC SDK dağıtılmaz**; kullanıcı SAP’den edinip lisanslamalıdır.

| Bileşen | Koşullar / korunan bildirimler |
|---|---|
| .NET çalışma zamanı / Microsoft kitaplıkları | MIT ve kurulan çalışma zamanı dosyalarındaki üçüncü taraf bildirimleri. |
| Avalonia 12.1.4 | MIT. Inter yazı tipi: SIL Open Font License 1.1. |
| Tomlyn | BSD-2-Clause; NuGet paket metadata/lisansı korunur. |
| Node.js | MIT ve runtime arşivinin LICENSE içindeki diğer lisanslar. |
| CPython / python-build-standalone | PSF ve runtime arşivindeki diğer üçüncü taraf koşulları. |
| PyRFC 3.4 | Apache-2.0, korunan dist-info lisansı/metadata. Onaylı kaynak commit ve native uyarlama hashleri build-info.json içindedir. |
| Microsoft Visual C++ Redistributable | Microsoft’un eşlik eden koşulları; MIT değildir. Özgün imzalı kurulumlar eklenir/indirilir. |
| MCP npm bağımlılıkları | node_modules ve package-lock.json içindeki ayrı paket lisansları. |
| Inno Setup 6.7.3 | Yalnız derleme aracı; derleyici dağıtılmaz. Bu ortam non-commercial use bildirir. Ticari derleme/dağıtım uygun Inno lisansı gerektirir. |

RFC/ADT’nin yazara ait derlenmiş kodu sahibinin MIT lisansıyla dağıtılır; bağımlılık bildirimleri ayrıdır. Kaynak depolar özel kalır ancak EXE içeriği çıkarılabilir. Kurulumdaki `payload/dependency-notices.json` iki MCP için paket sürümü, beyan edilen lisans ve bildirim konumlarını kaydeder. Eksik/standart dışı lisans beyanları sonraki yayın öncesi sorumlu incelemesi gerektirir. [Inno lisansı](https://jrsoftware.org/isinfo.php), [SAP SDK](https://support.sap.com/en/product/connectors/nwrfcsdk.html).

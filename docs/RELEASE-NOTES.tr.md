# v0.1.0-preview.5 — Basic / Advanced kurulum

Yeni kurulumlar Basic, mevcut kullanıcılar Advanced ile açılır. Advanced ekranları korunur. Basic: SDK ve tek tuşla otomatik hazırlık → tek SAP bağlantısı → mevcut Codex/Claude entegrasyonu. SDK’sız yalnız ADT kullanılabilir.

HTTPS keşfi yalnız belirtilen sunucuda şifresiz, 500 ms debounce, en fazla 16 paralel kontrol, 1,5 saniye aday/15 saniye toplam bütçe ile çalışır. HTTP fallback/redirect yoktur. 401, HTML veya rastgele XML başarı değildir. Sertifika güveni yalnız profile aittir; Basic TLS doğrulamasını atlamaz. Manuel URL/port ve SMICM/Basis yardımı bulunur.

Kaydet gerçek ADT MCP ve ayrı RFC_PING kullanır. Yanlış şifrede otomatik denemeler durur. Bir MCP doğrulanırsa uyarılı kayıt ve yalnız çalışan MCP ile devam; ikisi başarısızsa önceki profil korunur. Profiller/DPAPI/XML/diğer istemci kayıtları korunur. RFC kaynak ve paket hashleri değişmedi.

Yükseltme: yeni Manager’ı kurun, Basic’te **Kurulumu hazırla** veya Advanced’te ADT kur/onar işlemini yapın; istemci MCP süreçlerini yeniden başlatın. Önceki yayınlar korunur. [Basic kılavuzu](https://github.com/tamerinal/sap_mcp_windows/blob/v0.1.0-preview.5/docs/BASIC-SETUP.tr.md) ve [#5](https://github.com/tamerinal/sap_mcp_windows/issues/5) ayrıntıları içerir. Gerçek test sonuçları VALIDATION-SUMMARY dosyalarındadır.

İmzasız önizleme. Temiz Windows, GUI’siz/ağ kapalı kurulum, native erişilebilirlik ve canlı SAP kabulü bekler. Bu ortamda SAP GUI kuruludur; testler GUI çağırmaz. SDK kuruluma dahil değildir. Inno Setup 6.7.3 non-commercial use only bildirir; ticari dağıtım uygun lisans gerektirir. MIT üçüncü taraf koşullarını değiştirmez. Özel ADT kaynağı private kalır; derlenmiş JS/bağımlılıklar EXE’den çıkarılabilir. Her SAP ortamında koşulsuz çalışma iddiası yoktur.

# v0.1.0-preview.3 — RFC SDK başlangıç düzeltmesi

[English](RELEASE-NOTES.md)

SDK lib klasörü başlangıç PATH’inde olmayan Windows makinelerindeki RFC EmptyStdout sorununu düzeltir. Kayıtlı sdkDirectory, Host → Node → Python ve RFC gerektiren ADT başlangıçlarında yalnız alt sürece SAPNWRFC_HOME ve PATH’in başında SDK lib olarak aktarılır. Makine/kullanıcı PATH’i değişmez. Manager bağlantı testi aynı Host’u kullanır.

SDK Ready artık beş x64 SAP/ICU DLL’i, gerçek ICU başlatması ve SAP adresi/hesap/şifre kullanmayan ayrı PyRFC/SDK başlatma kontrolünü gerektirir. Eksik dosya, çökme, zaman aşımı ve bozuk çıktı engellenir. Güvenli tanılama; kod, aşama, sınıf ve varsa ondalık/hex çıkış kodunu içerir. Host/Node bilgisi Python worker çıkışından ayrı tutulur.

**Güncelleme:** Online veya Offline kurulumunu mevcut Manager üzerine uygulayın; kullanıcı verilerini koruyun. SAP RFC SDK ekranında lib içeren SDK kökünü seçip doğrulayarak kaydedin. Codex/Claude içindeki RFC/ADT MCP süreçlerini, gerekirse istemcileri yeniden başlatın. Mevcut profiller, DPAPI şifreleri ve istemci kayıtları geçerlidir. SDK kullanıcı tarafından sağlanır; pakete eklenmez. İki kurulum kendi .NET çalışma zamanını taşır; Offline ayrıca runtime paketlerini içerir. SAP GUI gerekmez. Temel ADT, RFC SDK’sından bağımsızdır.

[#3 kaydı](https://github.com/tamerinal/sap_mcp_windows/issues/3), düzeltmeyi ve sorunlu makinedeki kabulü izler. Ayrı [RFC worker tanılama talebi](https://github.com/tamerinal/sap_mcp_windows/blob/v0.1.0-preview.3/docs/MCP-RFC-WORKER-DIAGNOSTICS.tr.md) beklemektedir: mevcut MCP paketiyle Python exitCode/signal Host tarafından alınamaz. Özel RFC/ADT depoları ve onaylı runtime/MCP arşiv hashleri değişmedi. MCP araçları, target_key, credential biçimi ve ayar şeması değişmedi. Ham stderr/trace, PATH veya müşteri dosyası yüklemeyin.

Gerçek otomatik ve yerel sonuçlar sürümün VALIDATION-SUMMARY.tr.md dosyasındadır. SDK testleri Türkçe/boşluklu yolda özel yalıtılmış kopya kullanır; bu kopya kaynak ve yayın dışında tutulur. Düzeltme öncesi hata yerelde tekrarlandı; alt süreç PATH’ine SDK lib eklendiğinde beklenen yerel parametre hatasına ulaşıldı. Canlı TED/TEX RFC_PING sorunlu ortamda MCP yeniden başlatıldıktan sonra denenmelidir; tamamlandığı iddia edilmez.

**İmzasız önizleme.** SAP GUI’siz/ağ kapalı kurulum dahil temiz Windows 10 22H2/11, native erişilebilirlik/klavye ve canlı SAP kabulü bekler. Geliştirici makinesinde SAP GUI kuruludur; otomatik testler GUI çağırmaz. Inno Setup 6.7.3 non-commercial use only bildirir; ticari derleme/dağıtım uygun lisans gerektirir. MIT yazara ait kodu kapsar, üçüncü taraf koşullarını değiştirmez. Derlenmiş MCP JS çıkarılabilir. PyRFC kaynak kaydı ve native log sınırlamaları belgelerde korunur.

Preview.1 ve preview.2 etiket/dosyaları korunur. Yönetilen bağlantılar, salt okunur SAP GUI XML’i, profil taşıma ve DPAPI koruması sürer. Message-server grupları, SNC/SSO, landscape include, şifre içeren SAProuter, SAP’de şifre değiştirme, macOS ve otomatik güncelleme kapsam dışıdır.

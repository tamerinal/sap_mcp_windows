# Doğrulama ve kabul matrisi

[English](VALIDATION.md)

Test sonuçlarının makinece okunur çıktısı `artifacts/verification/report.json`; xUnit sonuçları `artifacts/test-results/` altına üretilebilir. Gerçek SAP sistemi testlerden otomatik çağrılmaz.

## Otomatik çekirdek testleri

- Atomik ayar yazma ve geçersiz sürüm/değerlerin mevcut dosyayı koruması.
- Landscape XML ayrıştırma, router/alias/target anahtarları ve DTD/XXE reddi.
- DPAPI ↔ Windows PowerShell karşılıklı uyumluluğu; Türkçe ve boşluklu şifre/yollar.
- Bozuk DPAPI dosyasında yeniden giriş yönlendirmesi.
- Çoklu profil, ortak aktif profil, mapping önceliği ve kaynak dosyanın korunması.
- Codex TOML ve Claude JSON birleştirme, yedekleme, çakışma, eşzamanlı düzenleme, başka MCP kayıtlarını koruma.
- Çok satırlı TOML metninin yanlışlıkla tablo sanılmaması; tırnak içinde noktalı sunucu adlarının korunması.
- Arşiv yol taşması/bağlantı reddi, SHA-256 uyuşmazlığında eski kurulumun korunması.
- Log seviyesi, maskeleme, döndürme, Off davranışı ve RFC gerektiren ADT araçlarının ön kontrolü.
- uiLanguage içermeyen eski ayar uyumu; altı ekran × iki dil × iki tema × iki genişlik, dil değişiminde açık alan/şifre ve SAP dilinin korunması, iki dilde aynı yardım kılavuzu.

## Yerel release doğrulaması

`scripts/Verify-Release.ps1` yalnız `artifacts/verification` altında izole kullanıcı verisi oluşturur. Her runtime/paket yerel kataloğa göre doğrulanır. İnternet adresleri boşaltılmış katalogla çevrimdışı çıkarma denenir. ADT-only ve iki bileşenli başlatma/araç listesi, SDK eksikliği, host protokolü ve çağrı engelleme doğrulanır. SDK yükleme testi mevcut SDK’yi yalnız okur; dağıtıma kopyalamaz. SAP kullanıcı/şifreleri kullanılmaz.

## Yayın öncesi temiz VM kontrolleri

Bu matrisin tamamlanması mevcut geliştirici bilgisayardaki testin yerine geçmez:

| Ortam / senaryo | Kabul |
|---|---|
| Windows 10 22H2 x64, standart kullanıcı | Arayüz açılır; çalışma zamanları önceden kurulu olmasa da kurulum ekranı çalışır |
| Windows 11 x64, standart kullanıcı | Aynı davranış; yalnız Microsoft VC++ kurulumu gerektiğinde UAC |
| Online, boş önbellek | Hash sabitli indirme; bağlantı kesilince anlaşılır hata ve tekrar deneme |
| Offline, ağ bağlantısı yok | Gerekli runtime paketleri eksiksiz; SDK ayrıca kullanıcı tarafından verilir |
| RFC+ADT / RFC-only / ADT-only / boş seçim | Yalnız seçilen MCP’ler kurulur; boş seçim engellenir |
| SDK yok / yanlış mimari / eksik DLL | RFC kapalı; ADT temel araçları kullanılabilir |
| SDK sonradan verilir / taşınır | RFC doğrulama sonrası açılır; yol bozulursa yeniden engellenir |
| Standart olmayan klasör izinleri | Eksik izin gösterilir; önceki ayarlar bozulmaz |
| İptal / kilitli paket dosyası | Önceki paket korunur; tekrar kurulum yapılabilir |
| Gerçek SAP, kullanıcı tarafından test | RFC ping ve ADT ping; yanlış şifreye otomatik tekrar döngüsü yok |
| İki istemci / profil değişimi | Ortak aktif hesap sonraki çağrıda kullanılır |
| Güncelleme / onarım | Profiller ve şifreler korunur |
| Kaldırma | Sahip olunan MCP kayıtları kaldırılır; veri koruma varsayılandır |
| İmza / kurumsal güvenlik politikası | Kurum sertifikasıyla imzalı EXE ve installer kabul edilir |

Mac, SNC/SSO, message-server grupları, gerçek SAP parola değişikliği ve otomatik güncelleme sunucusu bu sürümün kabul kapsamına dahil değildir.

## Bağlantı kaynağı kabulü

Boş JSON/oluşturma/düzenleme/silme/yeniden açma, iki mevcut MCP’nin üretilen XML’i okuması, hedef/profil taşıma, DPAPI dosyası koruma, mükerrer/çakışan hedefler, salt okunur GUI kaynağı, klasör adayları, bozuk kaynağın yöneticiyi engellememesi, geri alma ve kaynak değişimini iptal etme kapsanır. Headless arayüz iki kaynak modunu, iki dilde yerel ekran/düzenleyiciyi ve mevcut dil/tema/genişlik matrisini kapsar. Temiz GUI’siz Windows ve canlı SAP bekler.

## Preview.3 SDK regression

SDK içermeyen başlangıç PATH’i, mükerrer/büyük harf/tırnaklı SDK girdisi, Türkçe/boşluklu yollar, beş DLL mimarisi, bozuk kontrol çıktısı, native çıkış kodunun hex gösterimi ve iptal/zaman aşımı sınanır. Yerel yayın testleri yalnız Git dışında kalan artifacts altında özel SDK kopyası kullanır; yayınlara SDK alınmaz. Host → Node → Python ortam aktarımı, değişmeyen worker’ın yapılandırılmış yerel hatası ve Manager eksik-ICU tanılaması doğrulanır. TED/TEX canlı RFC_PING sorunlu makinede beklemektedir.

## Preview.4 ADT/TLS

Yerelde üretilen kök/ara/leaf/self-signed/süresi dolmuş/adı farklı sertifikalar; HTTP, HTTPS, özel port, IPv6, proxy, güven yalıtımı, redirect, kimlik deneme sayısı, içerik ve maskelenmiş raporu sınar. Entegrasyon yerel sunucuyla seçili-profil Manager snapshot’ını aktif-profil MCP araçlarıyla karşılaştırır; özgün mapping’i korur ve Türkçe/boşluklu DPAPI şifresini kontrol eder. Canlı SAP, müşteri sertifikası ve OS deposu değişikliği ön koşul değildir.

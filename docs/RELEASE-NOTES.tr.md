# v0.1.0-preview.2 — Windows x64 önizleme

[English](RELEASE-NOTES.md)

Bu önizleme SAP GUI’den bağımsız bağlantı yönetimi ekler. Uygulama modunda özel JSON kataloğuna sistem ekleyin/düzenleyin/silin; mevcut RFC/ADT MCP’leri için uyumlu XML otomatik üretilir. Alternatif olarak SAP GUI klasörü/XML’ini salt okunur seçip sistemleri SAP Logon’dan yönetin. Algılama öneridir; saklanan açık seçim önceliklidir. Profiller ve DPAPI hedef bazında ortak kalır.

- Onaylanan sunucu/sistem numarası değişimi bağlı profilleri ve aktif mapping’i taşır; çakışma engellenir.
- Onaylı silme yerel sistemi, profilleri ve mapping’i kaldırır; DPAPI dosyaları korunur. SAP GUI kaynak XML’i değiştirilmez.
- Eski şema-1 ayarları mevcut landscape ile sürer. Kaynak hatası diğer sayfaları engellemez. Başarısız kayıtta geri alma denenir; kaydedilmemiş profil değişikliklerinde kaynak değişimi öncesi onay istenir.
- İngilizce/Türkçe arayüz/yardım, bağlantı örnekleri, GitHub bildirim formları ve durum/alan/öncelik takibi dahildir. Özellik kaydı: [#2](https://github.com/tamerinal/sap_mcp_windows/issues/2).

Çalışma zamanı indirmek için Online, dahil çalışma zamanları için Offline seçin. SAP GUI gerekmez; RFC için kullanıcı SDK’sı hâlâ gerekir. Temel ADT Python/SDK gerektirmez; RFC’ye bağımlı ADT işlemleri korunur. MCP araç adları, target_key veya şifre biçimleri değişmedi. Özel MCP depolarına dokunulmadı; önceki doğrulanmış sabit MCP/runtime paketleri yeniden kullanıldı.

Gerçek commit, CI ve test sonuçları VALIDATION-SUMMARY.tr.md içindedir. Otomatik kontroller iki kaynak modunu, iki MCP’de üretilen XML’i, profilleri, hata/geri almayı, iki dilde arayüzü ve dört kurulum senaryosunu kapsar.

**İmzasız önizlemedir. Temiz Windows 10/11 (GUI’siz makine dahil), yerel erişilebilirlik ve canlı SAP kabulü bekler.** Mevcut geliştirici Windows bilgisayarında SAP GUI kuruludur; uygulama modu testleri onu çağırmaz ancak temiz makine testi değildir. Bu ortamda Inno Setup 6.7.3 non-commercial use only bildirir; ticari derleme/dağıtım uygun lisans gerektirir. Yazara ait kodun MIT lisansı diğer koşulları değiştirmez. SDK dosyaları ve müşteri şifreleri dışarıda tutulur. Derlenmiş MCP JS kurulumdan çıkarılabilir. Onaylı PyRFC kaynak kaydı ve yerel log sınırlamaları belgelerdeki gibidir.

Message-server grupları, SNC/SSO, include genişletmesi, SAP’de parola değiştirme, macOS ve otomatik güncelleme kapsam dışıdır. Önceki preview.1 dosyaları/etiketi korunur. Gerçek hesap hazırlamadan kurulan kullanım/bağlantı kılavuzlarını okuyun.

Uygulama modu şifresiz SAProuter dizelerini destekler. `/W/` ve eski `/P/` şifre bölümleri JSON/XML içine yazılmadan reddedilir; güvenli router şifresi desteği sonraki MCP geliştirmesini gerektirir. [SAP rota sözdizimine](https://help.sap.com/saphelp_em92/helpdata/en/48/6c62b71d5a350ce10000000a42189d/content.htm) bakın.

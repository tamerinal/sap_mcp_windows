# Basic kurulum — 0.1.0-preview.12

## 0.1.0-preview.12 — Özel MCP entegrasyon yardımı

Basic ve Advanced istemci entegrasyonu ekranları ile son Basic özetinde diğer agent ve uygulamalara yerel stdio MCP ekleme yardımı bulunur. Gerçek kurulum yollarıyla tam JSON, mevcut `mcpServers` nesnesine eklenecek sunucu girdileri, TOML tabloları veya ayrı komut/argüman alanları kopyalanabilir. Basic örnekleri güncel doğrulama ve RFC hazırlığını izler; uygun bileşen yoksa çalıştırılabilir kod üretilmez. Yardımı açmak ve kopyalamak ağ kontrolü başlatmaz veya istemci yapılandırmasını değiştirmez. ADT/RFC paketleri değişmemiştir. [Özel entegrasyon yardımına](CUSTOM-INTEGRATION.tr.md) bakın.

## 0.1.0-preview.11 — RFC hazırlık durumu ve ADT 0.4.3

Basic Doğrula, uyarı kabul edildikten sonra RFC hazırlığını güncel ayarlardan yeniden hesaplar. SDK veya bağımlılıklar Advanced'te düzeltildiğinde uygulamayı yeniden başlatmak gerekmez. Python, PyRFC, Visual C++ runtime'ları, SDK, native import ve RFC paket dosyalarının engel durumları ayrı gösterilir. Açık yalnız-ADT tercihi `RFC_DISABLED_BY_MODE` olarak korunur.

PyRFC dosyasının diskte bulunması native yükleme başarısı sayılmaz. Ayrı Python süreci, ICU'yu önceden yüklemeden ve SAP bağlantısı yapmadan `pyrfc.Connection` import eder. Native hata ve timeout, eksik bağımlılıktan ve SAP ping sonucundan ayrılır. SDK yolu yalnız alt süreçlere aktarılır; kalıcı ortam değişkenleri değiştirilmez. İstemci kaydı başarılı RFC doğrulaması ve güncel hazırlık sonucunu birlikte gerektirir.

Paket ADT 0.4.3 içerir: TLS uyarıları başarılı bağlantıyı bozmaz; ping ve repository araçları aynı kabul kuralını kullanır. Origin'e bağlı sertifika parmak izi desteği ADT paketindedir; sertifikaya ağdan otomatik güven verilmez. Eski doğrulamaları yeniden çalıştırın. Online/Offline kurulumlar ve SHA256 dosyaları preview.11 ön sürümünde sunulur.

Basic, SAP RFC ve ADT bağlantılarını bağımsız yapılandırır. Mevcut paket hazırlığı, bakım, profiller, DPAPI şifreleri ve istemci önizlemesi korunur. Advanced sertifika araçları ve bağlantı policy'si değişmez.

## Hazırla ve kaydet

Önce çalışma zamanlarını ve MCP paketlerini hazırlayın. RFC SDK klasörü seçin veya yalnız ADT ile devam edin. Manager güncellemesiyle birlikte ADT paketi 0.4.3'e (Basic keşif protokolü 2) güncellenmelidir. Eski yardımcı süreç başlatılmadan `BASIC_DISCOVERY_UPGRADE_REQUIRED` ile durdurulur.

Host, iki haneli instance, üç karakterli alfanümerik SID, client, kullanıcı, şifre ve SAP dilini girin. SID doğrulamadan önce zorunludur; bulunan SID uyuşmazlığı kaydı engeller. Mevcut profilde boş şifre kayıtlı DPAPI referansını korur. Kaydetmek SAP sistemindeki parolayı değiştirmez.

**Kaydet** yalnız yerel konfigürasyonu ve korumalı şifre referansını saklar. Ağ testi veya keşif başlatmaz. MCP kayıtlarını kurmadan önce profil doğrulanmalıdır. Uygulamayı açmak, profil seçmek veya alan düzenlemek ağ taraması başlatmaz.

## Onay vererek doğrula

**Doğrula** iptal edilebilir akışı başlatır. Basic sertifika dosyası istemez. HTTP şifresizdir; HTTPS trafiği şifreler ancak Basic sunucu sertifikasını doğrulamaz. Ağ işlemlerinden önce kalıcı uyarı ve açık kabul sunulur. Kabul, sihirbaz oturumundaki aynı profil/host/instance için geçerlidir. Sertifika güvenini Advanced üzerinden ayarlayın.

Bağımlılıkları uygunsa önce RFC ping çalışır. `TH_SERVER_LIST` yetkisi reddedilse de başarılı ping sonucu korunur. SAP yetki hatası ile yerel fonksiyon izin listesi engeli ayrı gösterilir. Sunucu bilgileri okunabiliyorsa, dönen tüm uygulama host/instance'larında RFC destekli keşif sorulur. RFC aday host/instance sağlar; gerçek ICM servis portlarını sağlamaz.

RFC keşfi kullanılamazsa, reddedilirse veya kullanılabilir aday bulamazsa otomatik tarama için ayrı onay istenir. RFC adaylarının başarısız olması ADT servisinin kapalı olduğunu kanıtlamaz.

Akıllı tarama instance portlarını, 80/443, 8080/8443, 8000–8099, 44300–44399 ve Java portlarını kontrol eder. ADT adayı bulunamazsa yalnız girilen host üzerindeki 1–65535 portlarını taramak için ayrıca onay istenir. Ham TCP bağlantıları 300 ms ve 512–1024 uyarlanan eşzamanlılık kullanır; kaynak yetersizliğinde eşzamanlılık azaltılır. Taramadan önce keşif, hedef host'a tarama süreci içinde tek seferlik hazırlık bağlantısı yapar ve kısa bir bekleme uygular; soğuk ilk paket gecikmesi açık portları kapalı saymaz. Bu, ilk sonuçlara kadar birkaç saniye ekleyebilir. Tam TCP taramasının hedefi 30, üst sınırı 60 saniyedir. Yavaş ağlarda taranmamış portlar kalabilir; kısmi sonuç açıkça bildirilir.

Açık portlarda HTTP ve HTTPS denenir. Her HTTP aşamasının ayrı 30 saniye bütçesi, en fazla 32 paralel isteği ve istek başına 1,5 saniye timeout'u vardır. TCP ve HTTP ilerlemesi ayrıdır; tüm doğrulamanın 30 saniyede tamamlanması garanti edilmez.

## Aday doğrulaması ve sonuç

Yalnız discovery 200/401 yanıtları ADT aday doğrulamasına girebilir. Açık port, 404 veya 503 ADT kanıtı değildir. HTML veya geçersiz XML başarılı ADT erişimi sayılmaz. Yönlendirmeler otomatik izlenmez.

TCP ve HTTP keşfine hesap, şifre veya çerez gönderilmez. RFC ilk bağlantı adımında kimlik bilgilerini kullanır. ADT şifresi anonim keşif bittikten sonra gönderilir. Giriş reddi dahil başarısızlıklarda sonraki aday denenir; ilk tam başarıda durulur. Geçerli discovery içeriği 401 adaylarından önce gelir; eşitlikte HTTPS ve keşif önceliği kullanılır.

Kimlik doğrulama, discovery ve salt okunur compatibility aşamalarının tümü geçmelidir. Basic, sertifika kontrolü kapalı bağlantıyı görünür uyarıyla kullanılabilir kabul eder; ortak ADT policy'sinin güvenli doğrulama sonucu değiştirilmez. Anonim erişim, girilen hesabı doğrulamaz.

Bulunan adres düzenlenebilir alanda gösterilir. Doğrulanamazsa gerekirse Web Dispatcher/proxy yolunu da içeren HTTP/HTTPS adresini elle girin. Sondaki `/sap/bc/adt` bir kez kırpılır. Manuel doğrulama port taramadan yalnız normalize edilen adresi kontrol eder.

ADT ve RFC ayrı raporlanır. Codex, Claude Desktop ve Claude Code'a yalnız doğrulanan bileşenler sunulur. Yalnız RFC veya yalnız ADT ile ilerlenebilir. İkisi de çalışmazsa yalnız konfigürasyonu kaydedin veya geri dönün. Başarısız doğrulama mevcut kullanılabilir kaydı silmez. Bağlantı ayrıntıları veya gerekli paketler değişince doğrulama geçersizleşir; eski doğrulama-policy kayıtları yeniden açık doğrulama ister.

Doğrulamada Kaydet, İleri, gezinme ve istemci kurulumu pasiftir. İptal alt süreçleri durdurur ve gecikmiş sonuçları eler. Geçici şifre ve mapping dosyaları temizlenir. Şifre keşif girdisine, ilerlemeye veya loglara girmez.

## Sürüm kontrolleri

Yerel fixture'lar HTTP, self-signed HTTPS, yetki dalları, onay reddi, iptal, tüm portların taranması ve istemci uygunluğunu kapsar. Canlı SAP kabulü ayrıca raporlanır; fixture başarısı canlı bağlantı kanıtı değildir. Online/Offline EXE ve SHA256 dosyaları GitHub ön sürümünde yayımlanır.

[OpenCode ve Advanced otomatik doğrulama](OPENCODE-ADVANCED.tr.md).

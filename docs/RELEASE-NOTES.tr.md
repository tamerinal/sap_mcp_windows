# Sürüm notları

## 0.1.0-preview.13 — Keşif başlangıcı ve public dokümanlar

Issue #7 için Basic keşif başlangıç sarmalayıcısını ve birleştirilmiş public içerik gizlilik temizliğini içerir. Sarmalayıcı, mevcut keşif girişini yüklemeden önce aynı Node sürecinde ön bağlantıları ve beklemeyi çalıştırır. ADT/RFC paket arşivleri ve sabit runtime sürümleri korunur. Belgelerde kurgusal örnekler kullanılır; ortama özel test notları yayımlanmaz. Keşif düzeltmesinin üretim ortamı teyidi, temiz Windows kabulü ve canlı SAP testleri bu sürüm için henüz yapılmadı.

## 0.1.0-preview.12 — Özel MCP entegrasyon yardımı

Basic ve Advanced istemci entegrasyonu ekranları ile son Basic özetinde diğer agent ve uygulamalara yerel stdio MCP ekleme yardımı bulunur. Gerçek kurulum yollarıyla tam JSON, mevcut `mcpServers` nesnesine eklenecek sunucu girdileri, TOML tabloları veya ayrı komut/argüman alanları kopyalanabilir. Basic örnekleri güncel doğrulama ve RFC hazırlığını izler; uygun bileşen yoksa çalıştırılabilir kod üretilmez. Yardımı açmak ve kopyalamak ağ kontrolü başlatmaz veya istemci yapılandırmasını değiştirmez. ADT/RFC paketleri değişmemiştir. [Özel entegrasyon yardımına](CUSTOM-INTEGRATION.tr.md) bakın.

## 0.1.0-preview.11 — RFC hazırlık durumu ve ADT 0.4.3

Basic Doğrula, uyarı kabul edildikten sonra RFC hazırlığını güncel ayarlardan yeniden hesaplar. SDK veya bağımlılıklar Advanced'te düzeltildiğinde uygulamayı yeniden başlatmak gerekmez. Python, PyRFC, Visual C++ runtime'ları, SDK, native import ve RFC paket dosyalarının engel durumları ayrı gösterilir. Açık yalnız-ADT tercihi `RFC_DISABLED_BY_MODE` olarak korunur.

PyRFC dosyasının diskte bulunması native yükleme başarısı sayılmaz. Ayrı Python süreci, ICU'yu önceden yüklemeden ve SAP bağlantısı yapmadan `pyrfc.Connection` import eder. Native hata ve timeout, eksik bağımlılıktan ve SAP ping sonucundan ayrılır. SDK yolu yalnız alt süreçlere aktarılır; kalıcı ortam değişkenleri değiştirilmez. İstemci kaydı başarılı RFC doğrulaması ve güncel hazırlık sonucunu birlikte gerektirir.

Paket ADT 0.4.3 içerir: TLS uyarıları başarılı bağlantıyı bozmaz; ping ve repository araçları aynı kabul kuralını kullanır. Origin'e bağlı sertifika parmak izi desteği ADT paketindedir; sertifikaya ağdan otomatik güven verilmez. Eski doğrulamaları yeniden çalıştırın. Online/Offline kurulumlar ve SHA256 dosyaları preview.11 ön sürümünde sunulur.

## 0.1.0-preview.10 — 2026-11-02

Basic artık Kaydet ve Doğrula işlemlerini ayırır; alan düzenlenirken keşif çalışmaz. SID zorunludur. Açık kabul sonrasında HTTP/HTTPS sertifika yüklemeden kullanılabilir. RFC ping ve sunucu listesi kontrolü, RFC destekli adaylar, onaylı akıllı/tam TCP taraması ve son ADT kimlik doğrulaması ayrı aşamalardır. Yetkisiz RFC keşfi başarılı ping sonucunu bozmaz. RFC adayları sonuç vermezse onayla taramaya geçilir; servis kapalı varsayılmaz. Her aday giriş reddinden sonra da denenir; ilk tam başarıda durulur.

Tam TCP taraması 30 saniyeyi hedefler, 60 saniyede kısmi sonuçla durur; HTTP prob süresi ayrıdır. ADT 0.4.2 keşif protokolü 2 gereklidir. Advanced, Host ve ortak production policy davranışı korunur. Online/Offline EXE ve SHA256 dosyaları GitHub ön sürümünde yayımlanır. Ayrıntılar [Basic kılavuzunda](BASIC-SETUP.tr.md).


## 0.1.0-preview.9 — SID ve MCP çıktı düzeltmeleri

RFC 0.1.1 ve ADT 0.4.1 tam JSON sonucunu yapılandırılmış çıktı yanında metin olarak da döndürür. Eski ADT liste/ping yanıtları şemalarına uyar. Syntax mesajları, DDIC ayrıntıları, sınıf/arayüz bileşenleri, arama açıklamaları ve BW endpoint hataları metin kullanan istemcilerde görünür. Her iki araç adı seti korunur.

Bağlantı/profil kaydında SID zorunludur. Basic başarılı RFC bağlantısından otomatik alabilir veya kullanıcı girebilir. Backend SID uyuşmazlığı kaydı durdurur; eski boş kayıtlar okunabilir. HTTP uyarısı korunur; sertifika incelemesi HTTP için TLS uygulanmadığını açıklar. Kayıtlı özel adres keşifte önce denenir.

MCP işlem/oturum logları Host süreç loglarından ayrıdır; seviye, rotasyon ve saklama politikasına uyar, SAP verilerini içermez ve operation ID sunar. SAP sürümü backend tarafından açıkça sunulmuyorsa nedeni belirtilir. UTF-8 ve bildirilen Türkçe kodlamalar HTTP sınırında çözülür.

Online/Offline EXE yerel teslim edilir; GitHub yayını yapılmaz. Canlı yazma ve aktivasyon kabul kapsamı dışındadır.

## 0.1.0-preview.8 — 2026-10-28 (yerel sürüm)

- Basic, mevcut kurulumda durum özetiyle açılır. Kurulum/paketler, SAP bağlantısı ve uygulama entegrasyonları doğrudan açılabilir; kayıtlı bağlantı geri yüklenir, parola alanı boş tutulur. Açılışta kurulum veya SAP testi yapılmaz.
- İlk hazırlık yalnız eksik önkoşulları kurar. Sonraki Basic kullanımları önkoşulları kontrol eder; eksikler ve aynı sürüm paket onarımları Advanced üzerinden giderilir. RFC/ADT MCP paketleri yalnız kullanıcı isteğiyle eksik veya eskiyse güncellenir. ADT-only tercihi korunur.
- Codex, Claude Desktop ve Claude Code ayrı hedeflerdir. Claude Code kullanıcı kapsamındaki `.claude.json` kaydını kullanır; `CLAUDE_CONFIG_DIR` desteklenir. Proje ve diğer uygulama ayarları korunur.
- Önizleme yalnız SAP kayıtlarını Yeni / Güncellenecek / Zaten güncel olarak gösterir. Seçim değişince yenilenir; aynı kayıt yeniden yazılmaz. Değiştirme izni, yedek yolu ve uygulama bazında başarı/hata sonuçları görünürdür.
- Önceki Basic etiketleri düzeltildi. Mevcut bağlantılar, DPAPI şifreleri, RFC/ADT payload sürümleri ve Advanced onarım seçenekleri korunur.

Yerel Online/Offline EXE ve SHA256 dosyaları hazırlanır. GitHub yayını yapılmaz. Test sonuçları ve temiz Windows/canlı SAP kabul sınırları teslim raporunda belirtilir.


## 0.1.0-preview.7 — 2026-10-28 (yerel sürüm)

Basic artık kimlik bilgisi göndermeden HTTP ve HTTPS adayları arar. HTTPS 443/44300–44399 yanında HTTP 80/80NN/8080/44380 desteklenir; güvenilir HTTPS önceliklidir. HTTP iletişiminin şifrelenmediği görünürdür. Yalnız sertifika sorunu olan HTTPS bulunursa Advanced gerekir; doğrulama atlanmaz. Seçilen tek adreste hesap, ADT discovery ve salt okunur compatibility doğrulanır.

ADT 0.4.0, BOM/satır sonlu DPAPI şifre dosyalarını okur; gerçek parolanın boşlukları korunur. Keşif ve doğrulama durumları ayrılır, form değişince önceki sonuç güncelliğini kaybeder. Kimlikli, zaman damgalı güvenli keşif kayıtları kalıcıdır. RFC ve çalışma zamanı paketleri korunur. Canlı kabul ve yerel doğrulama sonuçları teslim raporundadır; temiz Windows/diğer makine kabulü ayrıca gerekir. GitHub yayını yapılmadı.


[English](RELEASE-NOTES.md)

Preview.5 hatası düzeltildi: sunucu portsuz girildiğinde `explicitPort: null` gönderilebiliyor, ADT süreci ağa çıkmadan bunu reddediyordu. Genel uyarı bu girdi hatasını gizliyordu; RFC bağımsız olarak başarılı olabiliyordu. Manager artık boş isteğe bağlı alanları göndermez; güncel ADT 0.3.1 eski null alanlarını da kabul eder. Güvenli girdi/süreç kodları, yerel keşif hatalarını ağ/TLS sonuçlarından ayırır.

Manager'ı yükseltin; Basic'te **Kurulumu hazırla** veya Advanced'te ADT kur/onar ile güncel ADT paketini kurun ve istemcilerde MCP süreçlerini yeniden başlatın. Hem Manager hem ADT paketi güncellenmelidir. Profiller, şifreler, ayarlar ve RFC arşivleri korunur. Bilinen HTTPS adresi elle girilebilir. Instance numarası HTTPS portunu kesinleştirmez; gerçek servisin DNS adı ve CA güveni ayrıca doğru olmalıdır.

Regresyonlar gerçek Manager portsuz JSON isteğini, eski null isteğinin gerçek Node CLI'da çalışmasını ve Manager–Node yerel HTTPS keşfini kapsar. Gerçek test sayıları ve sınırlar doğrulama dosyalarındadır. [Basic yardımı](https://github.com/tamerinal/sap_mcp_windows/blob/v0.1.0-preview.6/docs/BASIC-SETUP.tr.md) · [#6 takip kaydı](https://github.com/tamerinal/sap_mcp_windows/issues/6).

İmzasız önizleme. Temiz Windows, canlı SAP ve sorunlu makinede kabul bekliyor. SDK dağıtılmaz. Inno Setup 6.7.3 non-commercial use only bildirir; ticari dağıtım uygun lisans ister. MIT üçüncü taraf koşullarını değiştirmez. Özel MCP'nin derlenmiş JavaScript ve bağımlılıkları EXE'den çıkarılabilir. Önceki yayınlar korunur. Her ortamda koşulsuz uyumluluk iddiası yoktur.

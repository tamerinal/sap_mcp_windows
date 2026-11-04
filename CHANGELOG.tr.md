# Sürüm geçmişi

## 0.1.0-preview.14 — OpenCode entegrasyonu ve Advanced doğrulama

Basic ve Advanced, global/proje dosyası seçimi, önizleme, yedekleme ve JSONC yorum/talimat korumasıyla OpenCode V1 veya V2’ye SAP MCP kaydı ekleyebilir. Advanced Bağlantılar ekranındaki **Otomatik keşfet ve doğrula**, Basic’in keşif/onay akışını ve ayrı ADT/RFC sonuçlarını kullanır. Sonuç forma gelir; URL/TLS alanlarını inceleyip Kaydet’e basana kadar kayıt yapılmaz. Basic davranışı ve MCP/runtime arşivleri korunur.

## 0.1.0-preview.13 — Keşif başlangıcı ve public dokümanlar

Issue #7 için Basic keşif başlangıç sarmalayıcısını ve birleştirilmiş public içerik gizlilik temizliğini içerir. Sarmalayıcı, mevcut keşif işlevini çağırmadan önce aynı Node sürecinde ön bağlantıları ve beklemeyi çalıştırır. ADT/RFC paket arşivleri ve sabit runtime sürümleri korunur. Belgelerde kurgusal örnekler kullanılır; ortama özel test notları yayımlanmaz. Keşif düzeltmesinin üretim ortamı teyidi, temiz Windows kabulü ve canlı SAP testleri bu sürüm için henüz yapılmadı.

## 0.1.0-preview.11 — RFC hazırlık durumu ve ADT 0.4.3

Basic Doğrula, uyarı kabul edildikten sonra RFC hazırlığını güncel ayarlardan yeniden hesaplar. SDK veya bağımlılıklar Advanced'te düzeltildiğinde uygulamayı yeniden başlatmak gerekmez. Python, PyRFC, Visual C++ runtime'ları, SDK, native import ve RFC paket dosyalarının engel durumları ayrı gösterilir. Açık yalnız-ADT tercihi `RFC_DISABLED_BY_MODE` olarak korunur.

PyRFC dosyasının diskte bulunması native yükleme başarısı sayılmaz. Ayrı Python süreci, ICU'yu önceden yüklemeden ve SAP bağlantısı yapmadan `pyrfc.Connection` import eder. Native hata ve timeout, eksik bağımlılıktan ve SAP ping sonucundan ayrılır. SDK yolu yalnız alt süreçlere aktarılır; kalıcı ortam değişkenleri değiştirilmez. İstemci kaydı başarılı RFC doğrulaması ve güncel hazırlık sonucunu birlikte gerektirir.

Paket ADT 0.4.3 içerir: TLS uyarıları başarılı bağlantıyı bozmaz; ping ve repository araçları aynı kabul kuralını kullanır. Origin'e bağlı sertifika parmak izi desteği ADT paketindedir; sertifikaya ağdan otomatik güven verilmez. Eski doğrulamaları yeniden çalıştırın. Online/Offline kurulumlar ve SHA256 dosyaları preview.11 ön sürümünde sunulur.

## 0.1.0-preview.10 — 2026-11-02

Basic artık Kaydet ve Doğrula işlemlerini ayırır; alan düzenlenirken keşif çalışmaz. SID zorunludur. Açık kabul sonrasında HTTP/HTTPS sertifika yüklemeden kullanılabilir. RFC ping ve sunucu listesi kontrolü, RFC destekli adaylar, onaylı akıllı/tam TCP taraması ve son ADT kimlik doğrulaması ayrı aşamalardır. Yetkisiz RFC keşfi başarılı ping sonucunu bozmaz. RFC adayları sonuç vermezse onayla taramaya geçilir; servis kapalı varsayılmaz. Her aday giriş reddinden sonra da denenir; ilk tam başarıda durulur.

Tam TCP taraması 30 saniyeyi hedefler, 60 saniyede kısmi sonuçla durur; HTTP prob süresi ayrıdır. ADT 0.4.2 keşif protokolü 2 gereklidir. Advanced, Host ve ortak production policy davranışı korunur. Online/Offline EXE ve SHA256 dosyaları GitHub ön sürümünde yayımlanır. Ayrıntılar [Basic kılavuzunda](docs/BASIC-SETUP.tr.md).


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


## 0.1.0-preview.6 — 2026-10-28

Basic portsuz ADT keşfi düzeltildi: boş isteğe bağlı port alanları gönderilmez, yeni ADT paketi eski null alanlarını kabul eder. Güvenli girdi/süreç hata kodları gösterilir. Manager–Node ve yerel HTTPS regresyonları, iki dilde yardım eklendi. RFC paketi değişmedi. Temiz Windows ve sorunlu makinede canlı kabul bekliyor.

## 0.1.0-preview.5 — 2026-10-28

Basic/Advanced modları; SDK’dan başlayan otomatik hazırlık; şifresiz sınırlı HTTPS keşfi; bağımsız ADT/RFC doğrulama ve kısmi başarı; ortak kayıtların korunması; EN/TR yardım ve regresyon testleri. RFC paketi değişmedi.

[English](CHANGELOG.md)

## v0.1.0-preview.4 — 2026-10-28

- Açık ADT adresi, adres bulma yardımı, isteğe bağlı kimlik bilgisiz öneri ve şifreyle tahmin yapmayan geçiş.
- Profile özel PEM/DER güveni, sertifika inceleme, ortak Node katmanı, aşamalı tanılama ve maskelenmiş destek raporu.
- Seçili profil Manager testi ortak aktif hesabı değiştirmez; eski ADT paketi onarım ister. ADT güncellendi; RFC/PyRFC/runtime hashleri korundu.
- Yerel HTTP/TLS regresyonları ve iki dilde dokümantasyon. Temiz Windows/canlı SAP bekler.

## 0.1.0-preview.3 — 2026-10-27

- Kayıtlı SDK lib yolunu Host, Node ve Python alt süreçlerinin PATH’ine aktar; işletim sistemi PATH’i ve mevcut ayarlar korunur.
- Ready öncesinde x64 SAP/ICU DLL’leri ve yalıtılmış ICU/PyRFC/SDK başlatması; Manager/Host için güvenli aşama/ondalık/hex çıkış tanılaması.
- SDK regresyon testleri, iki dilde yardım ve ayrı bekleyen RFC worker exitCode/signal talebi. Kayıt #3. Özel MCP paketleri değişmez.
- Yeni Online/Offline önizleme kurulumları; temiz Windows ve canlı SAP kabulü bekler.

## v0.1.0-preview.2 — 2026-10-27

- Salt okunur SAP GUI ve düzenlenebilir JSON bağlantı kaynakları, otomatik MCP XML üretimi, onaylı hedef/profil taşıma ve DPAPI dosyalarını koruyan silme eklendi.
- Geri alınabilir katalog/mapping/XML/ayar kaydı, kaynak hatası düzeltme, GUI algılama önerisi ve iki dilde bağlantı yardımı/örnekleri eklendi.


- İngilizce/Türkçe hata ve geliştirme kabulü, inceleme, sorumluluk, doğrulama ve sürüm takibi belgelendi.
- Formlar varsayılan tür/inceleme etiketleriyle genişletildi; takip etiketleri ve tekrar çalıştırılabilir kurulum betiği eklendi.
- README/katkı belgelerine bildirim bağlantıları ve GitHub’a sabit takip rehberi eklendi.

## v0.1.0-preview.1 — 2026-10-27

- İlk Windows x64 Avalonia/.NET 10 yönetici, kullanıcı başına Online/Offline Inno kurulumları ve bağımsız stdio host.
- Seçimli RFC/ADT, gereksinim denetimi/tekli/toplu kurulum, SDK doğrulama ve bağımsız ADT.
- SAP GUI bulma, çoklu kullanıcı/client profili, ortak aktif profil ve DPAPI şifre yönetimi.
- Koruyucu Codex/Claude birleştirme, genel dışa aktarım ve yalnız metadata içeren maskeli dönen loglar.
- Anlık İngilizce/Türkçe arayüz, saklanan dil, yerelleştirilmiş yardım ve korunan açık formlar.
- Public belgeler, MIT/üçüncü taraf bildirimleri, hata formları, Windows CI ve yayın taraması.
- Kaynak kayıtlı izole dağıtım yol uyarlamaları ve PyRFC debug yolu temizliği.
- İmzasız önizleme; temiz Windows 10/11 ve canlı SAP kabulü bekliyor. Sürüm notlarına bakın.

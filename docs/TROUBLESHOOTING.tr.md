# Sorun giderme

## 0.1.0-preview.11 — RFC hazırlık durumu ve ADT 0.4.3

Basic Doğrula, uyarı kabul edildikten sonra RFC hazırlığını güncel ayarlardan yeniden hesaplar. SDK veya bağımlılıklar Advanced'te düzeltildiğinde uygulamayı yeniden başlatmak gerekmez. Python, PyRFC, Visual C++ runtime'ları, SDK, native import ve RFC paket dosyalarının engel durumları ayrı gösterilir. Açık yalnız-ADT tercihi `RFC_DISABLED_BY_MODE` olarak korunur.

PyRFC dosyasının diskte bulunması native yükleme başarısı sayılmaz. Ayrı Python süreci, ICU'yu önceden yüklemeden ve SAP bağlantısı yapmadan `pyrfc.Connection` import eder. Native hata ve timeout, eksik bağımlılıktan ve SAP ping sonucundan ayrılır. SDK yolu yalnız alt süreçlere aktarılır; kalıcı ortam değişkenleri değiştirilmez. İstemci kaydı başarılı RFC doğrulaması ve güncel hazırlık sonucunu birlikte gerektirir.

Paket ADT 0.4.3 içerir: TLS uyarıları başarılı bağlantıyı bozmaz; ping ve repository araçları aynı kabul kuralını kullanır. Origin'e bağlı sertifika parmak izi desteği ADT paketindedir; sertifikaya ağdan otomatik güven verilmez. Eski doğrulamaları yeniden çalıştırın. Online/Offline kurulumlar ve SHA256 dosyaları preview.11 ön sürümünde sunulur.

## 0.1.0-preview.10 — 2026-11-02

Basic artık Kaydet ve Doğrula işlemlerini ayırır; alan düzenlenirken keşif çalışmaz. SID zorunludur. Açık kabul sonrasında HTTP/HTTPS sertifika yüklemeden kullanılabilir. RFC ping ve sunucu listesi kontrolü, RFC destekli adaylar, onaylı akıllı/tam TCP taraması ve son ADT kimlik doğrulaması ayrı aşamalardır. Yetkisiz RFC keşfi başarılı ping sonucunu bozmaz. RFC adayları sonuç vermezse onayla taramaya geçilir; servis kapalı varsayılmaz. Her aday giriş reddinden sonra da denenir; ilk tam başarıda durulur.

Tam TCP taraması 30 saniyeyi hedefler, 60 saniyede kısmi sonuçla durur; HTTP prob süresi ayrıdır. ADT 0.4.2 keşif protokolü 2 gereklidir. Advanced, Host ve ortak production policy davranışı korunur. Online/Offline EXE ve SHA256 dosyaları GitHub ön sürümünde yayımlanır. Ayrıntılar [Basic kılavuzunda](BASIC-SETUP.tr.md).

Aşağıdaki eski sürüm notları tarihsel bilgidir; güncel Basic davranışı bu bölümde ve Basic kılavuzunda açıklanır.

## 0.1.0-preview.7 — 2026-10-28 (yerel sürüm)

Basic artık kimlik bilgisi göndermeden HTTP ve HTTPS adayları arar. HTTPS 443/44300–44399 yanında HTTP 80/80NN/8080/44380 desteklenir; güvenilir HTTPS önceliklidir. HTTP iletişiminin şifrelenmediği görünürdür. Yalnız sertifika sorunu olan HTTPS bulunursa Advanced gerekir; doğrulama atlanmaz. Seçilen tek adreste hesap, ADT discovery ve salt okunur compatibility doğrulanır.

ADT 0.4.0, BOM/satır sonlu DPAPI şifre dosyalarını okur; gerçek parolanın boşlukları korunur. Keşif ve doğrulama durumları ayrılır, form değişince önceki sonuç güncelliğini kaybeder. Kimlikli, zaman damgalı güvenli keşif kayıtları kalıcıdır. RFC ve çalışma zamanı paketleri korunur. Canlı kabul ve yerel doğrulama sonuçları teslim raporundadır; temiz Windows/diğer makine kabulü ayrıca gerekir. GitHub yayını yapılmadı.


[English](TROUBLESHOOTING.md) · [Kılavuz](USER-GUIDE.tr.md)

| Belirti | İşlem |
|---|---|
| Windows yayıncı uyarısı | Önizleme EXE’leri imzasızdır. SHA-256 ve kurum politikanızı kontrol edin; güvenlik yazılımını kapatmayın. |
| Gereksinim eksik / indirme hatası | Tekrar denetle ve hatalı satırda Kur kullanın. HTTPS/proxy erişimini kontrol edin veya Offline seçin. Hash uyuşmazlığında hatalı önbelleği silip resmî yayından tekrar deneyin. |
| RFC SDK bekliyor | Kendi Windows x64 SDK’nızı sağlayın; Python/PyRFC ve Visual C++ kurup SDK’yı doğrula ve kaydet seçin. |
| SDK mimarisi yanlış / DLL eksik | Üstteki nwrfcsdk klasörünü seçin; iki DLL ve uyumlu bağımlılıkları denetleyin. x86/ARM SDK uyumsuzdur. |
| DLL’ler var ama yükleme başarısız | Python 3.11 x64, PyRFC cp311 win_amd64, VC++ 2013 ve 2015+ çalışma zamanları, klasör erişimi ve SDK sürüm uyumunu denetleyin. |
| ADT işlemi RFC_UNAVAILABLE veriyor | İşlem doğrulanmış RFC gerektirir. Önce RFC/SDK hazırlayın. Host SAP değişikliğini iletmeden engeller. Temel ADT kullanılabilir. |
| Landscape boş / grup desteklenmiyor | Doğrudan SAPGUI host:port kullanın. Message-server grupları, SNC/SSO ve include dosyaları genişletilmez. |
| Şifre çözülemiyor | Özgün Windows hesabı/bilgisayarını kullanın veya profilde geçerli SAP şifresini yeniden girin. |
| SAP bağlantı testi başarısız | VPN/DNS/güvenlik duvarı, host/sysnr, client/kullanıcı/şifre, ADT URL/TLS ve SAP yetkilerini kontrol edin. Hatalı girişleri tekrarlamayın. |
| İstemci çakışması / dosya önizleme sonrası değişti | Yeniden önizleyin, adları inceleyip yalnız istiyorsanız değiştirin. Zaman damgalı yedek istemci ayarının yanındadır. |
| Yol/log ayarı uygulanmamış görünüyor | İstemci MCP’lerini yeniden başlatın. Aktif profil değişikliği sonraki çağrılara uygulanır. |
| İzin veya geçersiz ayar hatası | Yazılabilir mutlak yollar seçip dosyayı düzeltin. `--config <mutlak yol>` izole ayar açar; bozuk dosyayı otomatik onarmaz. |

Sürüm, Windows derlemesi, bileşen, teşhis kimliği ve tekrar adımlarını bildirin. **Şifre, DPAPI dosyası, gerçek landscape/mapping/istemci ayarı, erişim anahtarı veya ham SAP verisi yüklemeyin.** Hatayı maskelenmiş metinle açıklayın. Güvenlik açıkları için [Güvenlik](../SECURITY.tr.md) belgesine bakın.

Bağlantı kaynağı hatasında SAP GUI salt okunur XML veya uygulama kataloğunu açıkça seçin. Eksik/bozuk kaynak diğer yönetim sayfalarını engellemez. Düzenlemeden önce modu kontrol edin; GUI modunda yerel CRUD kapalıdır. Hedef taşıma, silme ve geri alma için [bağlantı yönetimine](CONNECTIONS.tr.md) bakın.

## RFC SDK / ICU başlatması ve EmptyStdout

Preview.3, SDK klasörü Windows PATH içinde olmayan makinelerde RFC worker başlangıcını düzeltir. **SAP RFC SDK** ekranında **lib** içeren SDK kök klasörünü seçip **SDK’yı doğrula ve kaydet** kullanın. lib içinde birbiriyle uyumlu x64 sapnwrfc.dll, libsapucum.dll, icuuc57.dll, icudt57.dll ve icuin57.dll bulunmalıdır. Önce yönetilen Python/PyRFC ve Visual C++ ön koşullarını kurun. SDK DLL’lerini Windows klasörüne kopyalamayın; genel PATH’i değiştirmeniz gerekmez.

Manager ve Host aynı alt süreç ortamını oluşturur: SAPNWRFC_HOME kayıtlı sdkDirectory, PATH’in ilk girdisi SDK lib olur. Node bu ortamı Python’a aktarır; RFC gerektiren ADT alt süreçleri de aynı Host’u kullanır. Mevcut profilleri, DPAPI dosyalarını ve istemci kayıtlarını yeniden oluşturmayın. SDK kullanıcı tarafından sağlanır; iki EXE’ye de eklenmez.

**Ready**, artık x64 DLL kontrolü, ICU yükleme/başlatması ve ayrı Python/PyRFC SDK başlatma kontrolü gerektirir. Kontrol SAP adresi, hesap veya şifre kullanmaz; yalnız yerel eksik adres nedeniyle beklenen RFC_INVALID_PARAMETER sonucunu kabul eder. Import veya sürüm sorgusu tek başına yeterli değildir. Eksik DLL’ler native SDK başlatılmadan yakalanır. Hatalarda güvenli kod, aşama, sınıf ve varsa ondalık/hex çıkış kodu gösterilir; ham stderr, PATH ve hesap bilgileri gösterilmez. Windows’ta POSIX signal değeri bulunmaz. Host/Node çıkış bilgisi Python worker’ın çıkış kodu değildir.

Kaydetme veya güncelleme sonrasında Codex/Claude içindeki RFC/ADT MCP süreçlerini tamamen yeniden başlatın; gerekirse istemciyi kapatıp açın. SDK kontrolünü ve bağlantı testini tekrarlayın. Güncelleme sonrasında yetkili hesapla RFC_PING doğrulanmalı ve yalnız hassas bilgilerden arındırılmış sonuç [#3 kaydına](https://github.com/tamerinal/sap_mcp_windows/issues/3) eklenmelidir. Bu canlı kabulün geçtiği yayımlanan sürümde iddia edilmez.

EmptyStdout sürerse uygulama sürümünü ve güvenli tanılama kimliklerini kaydedin. Worker exitCode/signal geliştirmesi yeni RFC paketi gerektirir; [ayrı geliştirme talebi](MCP-RFC-WORKER-DIAGNOSTICS.tr.md) olarak beklemektedir. Ham native trace, PATH, şifre, DPAPI dosyası veya gerçek SAP mapping dosyası yüklemeyin. SDK başlatması geçtikten sonra ağ/hesap yetkilerini ayrıca kontrol edin.


# ADT adresleri, TLS ve tanılama

[Ana sayfa](../README.tr.md) · [Kullanım kılavuzu](USER-GUIDE.tr.md)

## ADT adresini bulup kaydetme

### 1. Hangi adres gereklidir?

ABAP Development Tools (ADT), SAP geliştirme servislerine HTTP/HTTPS üzerinden erişir. Kök URL’yi, örneğin https://sap.example.invalid:8443 biçiminde giriniz. Bu adres gerçek bir sistemi temsil etmeyen kurgusal dokümantasyon örneğidir. Ters proxy yol önekini yalnız Basis ekibiniz bildirmişse ekleyiniz. Manager /sap/bc/adt/discovery yolunu kendisi ekler; discovery veya başka bir servis kaynağını adresin sonuna yazmayınız.

### 2. Otomatik keşif ve doğrulamayı kullanınız

Sistem ve profil bilgilerini girdikten sonra Otomatik keşfet ve doğrula düğmesine basınız. Tarama veya doğrulamaya izin vermeden önce ağ ve iletişim uyarılarını inceleyiniz. Keşif aşaması parola kullanmadan aday adresleri belirler; sonraki doğrulama hesabınızı kullanabilir. Ayrı ADT/RFC sonuçlarını ve URL/TLS alanlarını inceleyip profili uygulamak için Kaydet’e basınız. Açık bir port veya tarayıcıdaki giriş istemi tek başına kullanılabilir ADT bağlantısını doğrulamaz.

### 3. Yetkili adres bilgisini SAP Basis ekibinden alınız

Bilgisayarınızdan erişilebilen DNS adı, protokol, port ve varsa yol önekini isteyiniz. Çalışan bir Eclipse ADT projesi hedef sistemin belirlenmesine yardımcı olabilir. RFC sistem numarası HTTP/HTTPS portunu belirlemez. SAP GUI veya Eclipse erişiminiz yoksa aşağıdaki talebi kopyalayıp olağan destek kanalınız üzerinden Basis ekibinize iletiniz.

### 4. Yönetici kontrolleri

Yetkili yönetici SMICM → Goto → Services menüsünden etkin HTTP/HTTPS servislerini inceleyebilir; SICF işleminde default_host → sap → bc → adt altındaki ilgili ADT servisini kontrol edebilir. Bu işlemler yönetim yetkisi gerektirir; servis etkinleştirme veya yapılandırma değişiklikleri kurum prosedürlerine göre yapılmalıdır. Dış Web Dispatcher ya da yük dengeleyici; uygulama sunucusundan farklı bir sunucu adı, port veya yol yayımlayabilir.

### 5. HTTPS, sertifikalar ve resmî dokümantasyon

HTTPS’i ve sunucu sertifikasının kapsadığı DNS adını tercih ediniz. Sertifika güven zinciri eksikse onaylı, herkese açık CA zincirini isteyiniz. HTTP kimlik bilgilerini şifrelemez; TLS doğrulamasını kapatmak sunucu kimliğini doğrulamaz. Devam etmeden önce gösterilen uyarıları okuyunuz. Aşağıdaki SAP dokümantasyon bağlantılarından sistem sürümünüze uygun kılavuzu seçiniz; doğrudan bağlantısı verilen arka uç yapılandırma PDF’i 2022 sürümünü açıklar.

[SAP ADT dokümantasyonu](https://help.sap.com/docs/ABAP_DEVELOPMENT_TOOLS_FOR_ECLIPSE) · [SAP arka uç yapılandırma kılavuzu (2022)](https://help.sap.com/doc/2e65ad9a26c84878b1413009f8ac07c3/202210.000/en-US/config_guide_system_backend_abap_development_tools.pdf)

### Basis’e gönderilecek talep

SAP Basis ekibine erişim bilgisi talebi

SAP MCP Manager bağlantısını yapılandırabilmem için hedef SAP sisteminin bilgisayarımdan erişilebilen ADT kök adresini; protokol, DNS adı, port ve varsa ters proxy yol önekiyle birlikte paylaşmanızı rica ederim. VPN/proxy ve güvenlik duvarı gereksinimlerini belirtmenizi; gerekliyse onaylı, herkese açık kök/ara CA sertifikalarını iletmenizi rica ederim. Gerekli ADT ICF servislerinin etkin olduğunu ve kullanıcı/client hesabımın discovery ile kullanılacak salt okunur ADT kaynaklarına erişim yetkisini doğrulayınız. Dış Web Dispatcher adresi uygulama sunucusundan farklıysa lütfen belirtiniz. Yanıtınıza parola, özel anahtar veya başka kimlik bilgisi eklemeyiniz.

Kök URL, RFC sistem numarası ve SAP client’tan bağımsızdır. Client/dil/hesabı kendi alanlarında tutun. Açık adtBaseUrl önceliklidir. Sondaki /sap/bc/adt bir kez kaldırılır; discovery yolunu istemci ekler. Query, fragment, gömülü kimlik bilgisi ve alt servis adresleri reddedilir. Ters proxy yol öneki, IPv4/IPv6 ve özel port desteklenir. HTTP’de kimlik bilgileri TLS ile korunmaz; sürekli uyarı gösterilir.

Eski profiller ve DPAPI dosyaları korunur. Advanced otomatik keşif ve doğrulaması kaydedilmemiş formu değerlendirir; profili yalnız Kaydet uygular. Eski adres önerisi kontrolleri ve zaman aşımı alanı arayüzden kaldırılmıştır. Mevcut zaman aşımı/keşif değerleri uyumlu kalır ve profil düzenlenirken korunur.

## Profile sertifika güveni ekleme

Önce profili kaydedip **Profil sertifikaları** ekranını açın. Kök/ara CA veya doğrulanmış self-signed açık sertifikayı Basis’ten güvenilir kanalla alın. PEM/DER (.pem/.cer/.crt) eklerken sahip, düzenleyen, UTC tarihleri, SAN ve SHA-256 değerini inceleyin; parmak izini yöneticinizle doğrulayıp güven referanslarını kaydedin. Açık sertifikalar uygulamanın profile özel veri klasörüne kopyalanır. PFX/özel anahtar kabul edilmez. Referansı kaldırmak sonraki çağrılardaki güveni kaldırır; kullanılmayan açık dosyalar diskte kalabilir, kendiliğinden güvenilmez.

Güven; Node varsayılanları, mevcut başlangıç ek CA’ları, Windows güveni ve yalnız bu profilin dosyalarından oluşur. Burada CA eklemek Windows’a veya başka profile ekleme yapmaz. Ara sertifikalar zinciri tamamlar, tek başına kök olmaz. DNS/IP SAN ve tarihler yine eşleşmelidir. Başka sunucu adına düzenlenmiş güvenilir self-signed sertifika da reddedilir. Eksik dosya, değişmiş parmak izi ve bozuk dosya bağlantıyı engeller.

**Sunucu sertifikasını incele (giriş yapmadan)**, hesap bilgisi göndermeden veya güven vermeden doğrulanmamış metadata döndürür. Yalnız bu inceleme için güvenilmeyen TLS el sıkışmasına izin verilir. Güven eklemek için doğrulanmış dosya kullanın; sertifikaya ağdan geldiği için güvenmeyin. Ağ/TLS protokolü çalışmıyorsa incelemede metadata alınamayabilir.

Gelişmiş TLS atlama seçeneği yalnız bu profile aittir, yeni profillerde kapalıdır ve sürekli uyarılır. Bu, doğrulanmış güvenli bağlantı değildir. Genel TLS kapatma işlemi yoktur. Kalıcı çözüm doğru sertifika güvenidir.

## Kaydedilmiş seçili profili test etme

**ADT bağlantısını test et**, aktif olmayan profil dahil kaydedilmiş seçili profili SapMcpHost ve gerçek MCP aktarımıyla sınar. Geçici özel mapping yalnız şifre referansları taşır; Codex/Claude ortak aktif hesabı değişmez. Kaydedilmemiş alanları önce kaydedin. Preview.4 bağlantı yeteneği olmayan eski ADT paketleri kur/onar mesajıyla engellenir.

Aşamalar: yapılandırma → DNS/TCP → HTTPS için TLS → kimlik doğrulama → ADT discovery → salt okunur compatibility graph. Beklenen XML namespace/yapıları gerekir. Açık port, 401, herhangi bir XML/HTML veya kök 404 yeterli değildir. Kök 404 tek başına ADT yok demek değildir. Anonim kaynaklar girilen hesabı kanıtlamaz; sonuç doğrulanmamış kalır. İlk hatalı kimlik denemesi testi ve otomatik tekrarları durdurur; değişmemiş yanlış şifreyle tekrar tekrar denemeyin.

DNS, kapalı TCP, timeout, TLS ad/tarih/güven/zincir, 401 kimlik doğrulama, 403 yetki ve servis/içerik hataları ayrılır. Eksik düzenleyen ile eksik güven kökü her zaman kesin ayrılamaz: TLS_CHAIN_UNRESOLVED bunu belirtir. Genel TLS’yi kapatmak yerine Basis’ten tam zincir ve yönlendirmeyi kontrol etmesini isteyin.

Mevcut HTTP_PROXY/HTTPS_PROXY/ALL_PROXY/NO_PROXY değişkenleri kullanılır; HTTP proxy aktarımı ve HTTPS CONNECT ortak katmandan geçer. Manager proxy URL/şifresini veya işletim sistemi ortamını yeniden yazmaz. Proxy varsa ağ aşamaları etkin yola aittir; uç sunucunun TLS’si yine doğrulanır. Etkileşimli proxy kimlik doğrulaması, SSO/mTLS ve yeni proxy yönetim ekranı sağlanmaz.

Profil/sertifika değişikliği sonraki çağrıda okunur; yalnız hedefe göre URL önbelleği ayarı ezemez. Yeni ADT paketi kurunca, Node/ortam veya Windows güvenini değiştirince istemcide MCP’leri yeniden başlatın. Güncellemede ayarlar, profiller, DPAPI şifreleri ve istemci kayıtları korunur.

## Destek, test ve sınırlar

Tanılama ekranından maskelenmiş destek raporu çıkarın. Yalnız izinli aşama/durum/kod ve maskelenmiş yapılandırma bulunur; adres, hesap, sertifika kimliği, token, şifre, cookie, header veya ham gövde yoktur. Gerçek mapping, CA dosyaları, native trace veya PATH’i public kayıtlara eklemeyin.

Yerel kurgusal HTTP/TLS testleri profil yalıtımı, zincir, self-signed, SAN/tarih, redirect, proxy, kimlik ve yanıltıcı içerik senaryolarını kapsar. Manager/Host/MCP tutarlılığı canlı SAP olmadan yerel sunucuda sınanır. Kullanıcı iki makinede Basic bağlantısını ve Advanced çoklu profil bağlantısını doğruladı. Native klavye/erişilebilirlik ve temiz Windows/GUI’siz VM kabulü ayrı, henüz yapılmamış kontrollerdir. Otomatik OCSP/CRL/AIA, SSO/mTLS ve SAP tarafında servis/sertifika değiştirme bu teslimde yoktur. Windows/Node güven kuralları tarayıcı/Eclipse ile aynı değildir; yalnız tarayıcıda açılması MCP güvenini kanıtlamaz.


# Basic kurulum — 0.1.0-preview.10

Basic, SAP RFC ve ADT bağlantılarını bağımsız yapılandırır. Mevcut paket hazırlığı, bakım, profiller, DPAPI şifreleri ve istemci önizlemesi korunur. Advanced sertifika araçları ve bağlantı policy'si değişmez.

## Hazırla ve kaydet

Önce çalışma zamanlarını ve MCP paketlerini hazırlayın. RFC SDK klasörü seçin veya yalnız ADT ile devam edin. Manager güncellemesiyle birlikte ADT paketi 0.4.2'ye (Basic keşif protokolü 2) güncellenmelidir. Eski yardımcı süreç başlatılmadan `BASIC_DISCOVERY_UPGRADE_REQUIRED` ile durdurulur.

Host, iki haneli instance, üç karakterli alfanümerik SID, client, kullanıcı, şifre ve SAP dilini girin. SID doğrulamadan önce zorunludur; bulunan SID uyuşmazlığı kaydı engeller. Mevcut profilde boş şifre kayıtlı DPAPI referansını korur. Kaydetmek SAP sistemindeki parolayı değiştirmez.

**Kaydet** yalnız yerel konfigürasyonu ve korumalı şifre referansını saklar. Ağ testi veya keşif başlatmaz. MCP kayıtlarını kurmadan önce profil doğrulanmalıdır. Uygulamayı açmak, profil seçmek veya alan düzenlemek ağ taraması başlatmaz.

## Onay vererek doğrula

**Doğrula** iptal edilebilir akışı başlatır. Basic sertifika dosyası istemez. HTTP şifresizdir; HTTPS trafiği şifreler ancak Basic sunucu sertifikasını doğrulamaz. Ağ işlemlerinden önce kalıcı uyarı ve açık kabul sunulur. Kabul, sihirbaz oturumundaki aynı profil/host/instance için geçerlidir. Sertifika güvenini Advanced üzerinden ayarlayın.

Bağımlılıkları uygunsa önce RFC ping çalışır. `TH_SERVER_LIST` yetkisi reddedilse de başarılı ping sonucu korunur. SAP yetki hatası ile yerel fonksiyon izin listesi engeli ayrı gösterilir. Sunucu bilgileri okunabiliyorsa, dönen tüm uygulama host/instance'larında RFC destekli keşif sorulur. RFC aday host/instance sağlar; gerçek ICM servis portlarını sağlamaz.

RFC keşfi kullanılamazsa, reddedilirse veya kullanılabilir aday bulamazsa otomatik tarama için ayrı onay istenir. RFC adaylarının başarısız olması ADT servisinin kapalı olduğunu kanıtlamaz.

Akıllı tarama instance portlarını, 80/443, 8080/8443, 8000–8099, 44300–44399 ve Java portlarını kontrol eder. ADT adayı bulunamazsa yalnız girilen host üzerindeki 1–65535 portlarını taramak için ayrıca onay istenir. Ham TCP bağlantıları 300 ms ve 512–1024 uyarlanan eşzamanlılık kullanır; kaynak yetersizliğinde eşzamanlılık azaltılır. Tam TCP taramasının hedefi 30, üst sınırı 60 saniyedir. Yavaş ağlarda taranmamış portlar kalabilir; kısmi sonuç açıkça bildirilir.

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

# Kullanım kılavuzu — v0.1.0-preview.4

[English](USER-GUIDE.md) · [Ana sayfa](../README.tr.md)

## Kurulum ve ön koşullar

Çalışma zamanı indirilebiliyorsa Online, aksi halde Offline indirin. EXE’yi MCP istemcilerini kullanacak Windows hesabıyla çalıştırın. Yönetici .NET içerir ve `%LOCALAPPDATA%\Programs\SapMcpManager` altına kurulur; kullanıcı verisi `%LOCALAPPDATA%\SapMcpManager` altındadır.

İki bileşen başlangıçta seçilidir. Birini veya ikisini bırakın; boş seçim engellenir. EXE hazırlanmış arşivleri diske koyar. Yönetici içinde **Ön koşullar → Tekrar denetle → Gerekli Uygulamaları Kur** eksik kurulabilir gereksinimleri varsayılan işaretli, sürüm/durum/açıklamayla gösterir. Satırdaki Kur veya Seçilenleri Kur kullanın. Microsoft Visual C++ UAC isteyebilir. İptal tamamlanan paketleri korur; hatayı düzelttikten sonra tekrar deneyin. Kurulum sonunda kontroller yinelenir. Ardından **Seçili MCP bileşenlerini kur / onar** ile yalnız seçilen sunucuları kurun.

Ortak kontroller Windows 10 22H2/11 x64, en az 1 GB boş alan, yazılabilir klasörler, PowerShell DPAPI komutları, Node.js ve katalog bütünlüğünü kapsar. RFC için yönetilen Python 3.11/PyRFC, Visual C++ ve SDK da gerekir. Temel ADT Python/SDK gerektirmez. RFC paketi SDK beklerken ADT kullanılabilir. Genel durum ekranından bileşen ekleyip kaldırabilirsiniz; kaldırma profilleri/şifreleri korur.

## SDK’yı şimdi veya sonradan bağlama

Yetkili SAP hesabınızla SAP’nin resmî bağlayıcı sayfasından Windows x64 NetWeaver RFC SDK edinin. SDK hiçbir zaman dahil edilmez. Arşivi açıp uyumlu bağımlılıklarıyla `lib\sapnwrfc.dll` ve `lib\libsapucum.dll` içeren **üstteki `nwrfcsdk` klasörünü** seçin. SDK’yı doğrula ve kaydet kullanın. DLL mimarisi kontrol edilir ve SAP’ye giriş yapmadan Python/PyRFC ile gerçek yükleme denenir. Bu test için önce Python/PyRFC kurulmalıdır. Farklı SDK sürümlerinin DLL’lerini karıştırmayın.

Geçerli SDK yoksa RFC engelli kalır, temel ADT kullanılabilir. SDK’yı aynı sayfadan sonradan bağlayın. SDK’yı taşıma/silme RFC başlangıcını yeniden engeller; yeni yolu seçip doğrulayın.

Bu MCP sunucularını çalıştırmak için SAP GUI gerekmez. RFC, PyRFC ve kullanıcının sağladığı NetWeaver RFC SDK’yi; temel ADT HTTP/HTTPS kullanır. Ağ/VPN erişimi, geçerli SAP hesabı ve yetkiler yine gerekir. Bazı ADT işlemleri hazır RFC gerektirir. Uygulama SAP’deki gerçek parolayı değiştirmez.

## Kaynağı seçme

**SAP bağlantıları** ekranını açın. Yönetici bilinen Windows SAP GUI kayıt defteri/kurulum konumlarını kontrol edip mod önerir. Algılama öneridir: iki moddan birini seçip **Seçilen kaynağı kullan** deyin. Tercih saklanır; SAP GUI kurulunca/kaldırılınca kendiliğinden değişmez. İki sistem listesi ayrıdır, birleştirilmez. Mod değiştirmek kayıtlı hesapları ve korunan şifreleri silmez. Aynı `host:sysnr` için profiller iki istemci ve iki kaynak modu arasında ortaktır.

Açık hesap formunda kaydedilmemiş değişiklik varsa kaynak değişimi, yenileme veya sistem düzenleme öncesinde onay istenir. Vazgeç mevcut formu korur. English/Türkçe değişimi alanları korur. Kaynak/yol değişince yeni XML yolunu kullanmaları için Codex/Claude içindeki MCP’leri yeniden başlatın.

## SAP GUI olmadan: uygulamanın bağlantıları

1. **Uygulamanın bağlantıları → Seçilen kaynağı kullan** seçin. Gerekiyorsa boş yerel katalog ve MCP uyumlu XML otomatik oluşturulur.
2. **Yeni bağlantı** seçin. Açıklayıcı ad, uygulama sunucusu adı/IP ve iki rakamlı sistem numarası (00–99) girin. Sunucu alanına URL veya port yazmayın. SID (en fazla üç harf/rakam) ve SAProuter isteğe bağlıdır.
3. **Bağlantıyı kaydet** seçin. Sonra sistemi seçip alttaki hesap formunda profil adı, kullanıcı, client, SAP dili, şifre ve isteğe bağlı ADT/BW adreslerini girin. Profili kaydedip ortak aktif hesabı seçin.
4. RFC için çalışma zamanlarını ve SDK’nızı kurup doğrulayın. MCP istemcilerini ayarlayın. Yerel MCP doğrulaması başlangıç/araç listesini kontrol eder; açıkça başlattığınız bağlantı testi SAP’ye gider.

**Bağlantıyı düzenle** ile ad, sunucu, sistem numarası, SID veya router değişir. Kimlik sabit kalır. Sunucu/sistem numarası değişikliği etkilenen profilleri gösterir; iki istemcinin profillerini ve aktif mapping’ini yeni hedefe taşımadan önce onay ister. Mevcut başka bağlantı veya profil hedefinin üzerine sessizce yazılmaz.

**Bağlantıyı sil** sistem ve bağlı profilleri gösterir. Onay yerel sistemi, hesap profillerini ve aktif eşleştirmeleri kaldırır. Profil silmede olduğu gibi DPAPI şifre dosyaları korunur; SAP GUI kaydı veya SAP hesabı silinmez. Vazgeç hiçbir şeyi değiştirmez. Ortak bir hedefin profillerini kaldırmak o hedef diğer kaynak modundan kullanıldığında da etkili olur.

## SAP GUI ile: salt okunur sistem listesi

1. Sistem ekleme/değiştirme/silmeyi SAP Logon içinden yapın.
2. Yöneticide **SAP GUI bağlantıları (salt okunur)** seçin. SAP GUI klasörünü veya doğrudan `SAPUILandscape.xml` / `SAPUILandscapeGlobal.xml` dosyasını gösterin. İkisi de varsa kullanılacak dosyayı seçin.
3. **Seçilen kaynağı kullan** deyin. Yönetici doğrudan SAPGUI host:port kayıtlarını okur. Daha sonra SAP Logon değişikliklerinden sonra **Sistemleri yenile** kullanın.
4. Yüklenen sistemi seçip kullanıcı/client profilleri ve DPAPI şifrelerini yöneticiden düzenleyin. Bu modda sistem Yeni/Düzenle/Sil düğmeleri kapalıdır; servis katmanı da bu işlemleri reddeder.

Kaynak XML hiçbir zaman yazma hedefi değildir. SAP GUI kurulu olmasa da mevcut XML seçilebilir. Eksik/bozuk XML bağlantı kaynağı hatası gösterir; diğer yönetim sayfaları açılır. Geçerli dosyayı seçip uygulayın/yenileyin. Başka kaynağa sessiz geçiş yapılmaz. Message-server grupları, SNC/SSO ve landscape include genişletmesi bu önizlemede desteklenmez.

## Hangi dosya ne yapar?

| Dosya | Amaç ve sahiplik |
|---|---|
| Dış `SAPUILandscape.xml` / `SAPUILandscapeGlobal.xml` | SAP GUI sistem kataloğu; yöneticide salt okunur. JSON değil XML’dir. |
| `config/connections.json` | Şema sürümü, sabit kimlik, ad, sunucu, sistem numarası, SID ve router içeren uygulama kataloğu. Şifre içermez. |
| `config/SAPMcpLandscape.xml` | JSON kataloğundan üretilen, mevcut RFC/ADT MCP’lerinin okuduğu XML. Elle düzenlemeyin. |
| `config/sap_credentials.json` | Aktif profillerden üretilen overridesByTarget/secretRef hesap eşleştirmeleri. Sistem kataloğu değildir. |
| `secrets/*.password.dpapi` | DPAPI CurrentUser ile korunan şifreler. Başka kullanıcı/bilgisayarda yeniden giriş gerekebilir. |

Yollar varsayılan olarak `%LOCALAPPDATA%\SapMcpManager` altındadır; özel yapılandırmalar kendi veri klasörünü kullanır. Arayüz yönetilen JSON yolunu gösterir. Mevcut mapping JSON’u hâlâ **Mapping içe aktar** üzerinden alınabilir; özgün kaynak korunur. Mapping almak sunucuları keşfetmez veya oluşturmaz.

Ayar şeması isteğe bağlı `connectionSourceMode`, `externalLandscapeFile`, `managedConnectionsFile` alanlarıyla sürüm 1 olarak kalır. Eski ayarlar siz açıkça mod değiştirmeden mevcut landscape’i dış salt okunur kaynak olarak kullanır. Profiller, DPAPI dosyaları ve istemci kayıtları geçerlidir. Yönetici kaydedilmiş uygulama modunu tekrar açarken kendi üretilen XML’ini onarır/yeniden üretir; SAP GUI XML’ine yazarak onarım yapmaz.

Kayıttan önce aday veriler doğrulanır. Dosya grubu yazımı başarısızsa önceki katalog, mapping, XML ve ayarlar geri alınmaya çalışılır; hata gösterilir. Geri alma da başarısızsa MCP istemcilerini kapatıp devam etmeden belirtilen ayarları onarın. Uygulama çıktılarının yollarını dış landscape’ten ve birbirinden ayrı tutun. Devam eden SAP çağrısı hesabını önceden çözmüş olabilir; liste değişiklikleri sonraki çağrılara uygulanır.

## Sorun giderme ve destek

- Boş liste: önce kaynağı uygulayın, sonra yerel sistem oluşturun veya desteklenen dış XML’i seçin.
- Klasörde aday yok: XML’i doğrudan seçin; seçilen klasörde yalnız iki standart dosya adı aranır.
- Mükerrer/hedef çakışması: başka adres kullanın veya mevcut sistemi yönetin; otomatik profil birleştirme yapılmaz.
- Geçersiz sunucu: `https://`, port, eğik çizgi veya boşluk olmadan ad/IP girin. ADT/BW URL’leri profile yazılır.
- Dış dosya değişikliği: SAP Logon’da düzenleyip yenileyin. Yerel ekleme/düzenleme/silme için uygulama modunu seçin.
- Erişim yok/geçersiz JSON: izinleri veya kataloğu düzeltip yenileyin/yeniden açın. Hatayı atlamak için son geçerli ayarı silmeyin.

[Sorun giderme](TROUBLESHOOTING.tr.md) ve [bildirim kılavuzuna](ISSUES.tr.md) bakın. Gerçek katalog, mapping, SDK, şifre dosyası veya incelenmemiş logları public issue’ya eklemeyin. `examples/` biçimleri kurgusal sistemler kullanır. Sürüm doğrulamasında aksi kaydedilmedikçe temiz Windows ve canlı SAP kabulü bekler.

Sistem ve Yeni profil seçin. Ad, SAP kullanıcısı, üç rakamlı client, SAP dili, mevcut şifre ve isteğe bağlı ADT/BW Modeling adreslerini girin. Bilinen bir geliştirme sisteminin riskini bilerek kabul etmediğiniz sürece TLS sertifika doğrulamasını açık tutun. Profili kaydedin. Bir sistem için birden fazla kullanıcı/client profili oluşturabilirsiniz. Sistem başına Codex ve Claude için **tek ortak aktif profil** vardır; Aktif yap sonraki çağrıları değiştirir. Devam eden çağrı başlangıçtaki hesabını korur. MCP’de sistem seçimi `target_key` (`host:sysnr`) ile yapılır.

Mapping içe aktar mevcut `sap_credentials.json` dosyasını kabul eder ve özgün dosyayı korur. default → SID → ad → target önceliği çözülür, gelişmiş seçenekler korunur, çözülebilen şifreler yeniden şifrelenir. Aktif profiller `overridesByTarget` ve `secretRef` olarak üretilir; araç parametreleri değiştirilmez.

## Şifre güncelleme

Maskeli şifre alanına mevcut SAP şifrenizi girin. Kayıt, PowerShell ConvertFrom-SecureString uyumlu hex biçiminde DPAPI CurrentUser kullanır. JSON yalnız şifre dosyası referansı taşır. Güncellemek için kayıtlı profili seçip yeni geçerli şifreyi girin ve kaydedin. Alanı boş bırakırsanız mevcut şifre korunur. Şifrenin başındaki/sonundaki boşluklar korunur.

Bu işlem yerel korumalı kopyayı günceller; **SAP’deki gerçek parolayı değiştirmez**. SAP’de parola değiştiğinde burayı güncelleyin. Başka Windows hesabı veya bilgisayardan kopyalanan şifre çözülemeyebilir; yeniden girin. Şifre dosyalarını, gerçek landscape/mapping dosyalarını, istemci ayarlarını veya incelenmemiş logları public hata bildirimine eklemeyin.

## İstemciler ve doğrulama

Codex ve/veya Claude Desktop seçin. Codex varsayılanı `CODEX_HOME/config.toml` veya `~/.codex/config.toml`; Claude `%APPDATA%\Claude\claude_desktop_config.json` kullanır. Yolları kontrol edip Değişiklikleri önizle seçin. Yalnız üretilecek SAP kayıtları gösterilir; diğer şifreler gösterilmez. Çakışan `sap_rfc_call` / `sap_adt_mcp` kayıtları için değiştirme seçeneği açıkça işaretlenmelidir. Uygula zaman damgalı yedek oluşturur, diğer sunucuları korur. Dosya önizlemeden sonra değişirse tekrar önizleyin.

İstemciler `SapMcpHost.exe --component rfc|adt --config <mutlak ayar yolu>` çalıştırır. SDK bekleyen RFC kaydı eklenmez. Uyguladıktan sonra istemci MCP’lerini yeniden başlatın. Yönetici açık kalmak zorunda değildir. Genel MCP JSON dışa aktarımı diğer istemcileri destekler; şifre içermez.

Yerel MCP doğrulaması SAP’ye bağlanmadan başlangıç ve tools/list kontrolü yapar. RFC/ADT bağlantısını test et seçilen sisteme açıkça salt okunur ping gönderir. Önce VPN/ağ ve SAP yetkilerini sağlayın. Üst MCP teknik hataları özgün dilinde kalabilir; yönetici seçilen dilde hata özeti sunar.

## Dil, ayarlar ve loglar

Üstteki English / Türkçe hemen uygulanır, tercihi kaydeder, şifre dahil açık form alanlarını korur. İlk açılışta Türkçe sistem arayüzünde Türkçe, diğerlerinde İngilizce kullanılır. Arayüz dili SAP profil dilini, aktif hesapları veya istemci kayıtlarını değiştirmez. Sistem/açık/koyu temaları vardır.

Ayarlar mapping/şifre/çalışma zamanı yollarını, ayrı RFC/ADT log klasörlerini ve düzeylerini yönetir: Off, Error, Warn, Info (varsayılan), Debug, Trace. Varsayılanlar 30 gün, dosya başına 10 MB, 10 döndürülmüş dosyadır. Yol/log değişince istemci MCP’sini yeniden başlatın. Profil etkinleştirme yeniden başlatmadan sonraki çağrılara uygulanır.

Yönetici logları kurulum, landscape okuma, profil ve açık bağlantı testlerini kapsar. Host logları oturum/araç metadata, süre ve hata durumunu tutar. Ham iş verisi ve üst MCP stderr çıktısı kaydedilmez; şifre/yetkilendirme başlıkları her düzeyde maskelenir. Mevcut üst paketlerde tam yerel seviye/döndürme desteği yoktur: logları host yönetir, yerel payload tracing kapatılır. Trace iş verisi yerine metadata kaydeder. Günlükleri yenile veya Log klasörünü aç kullanın.

## Onarım, güncelleme ve kaldırma

Yönetici dosyaları için EXE’yi yeniden çalıştırın; paketler için ön koşulları denetleyip Seçili MCP bileşenlerini kur / onar kullanın. Ayarlar/profiller/şifreler uygulama dışında tutulur ve onarım/güncellemede korunur. Başka Windows kullanıcısının şifreli dosyalarını kopyalayarak taşıma yapmayın.

Windows Yüklü uygulamalar’dan kaldırın. Yalnız bu host’un SAP istemci kayıtları yedeklenerek kaldırılır. Kullanıcı verisi varsayılan olarak ve sessiz kaldırmada korunur. Etkileşimli kaldırmada varsayılan sahip olunan veri klasörü için ayrı açık kalıcı silme seçeneği vardır; özel dış yollar silinmez. Gerekirse ayarları/şifreleri güvenli yedekleyin; DPAPI hesap kısıtları yedeklere de uygulanır.

[Sorun giderme](TROUBLESHOOTING.tr.md) ve [Sürüm notları](RELEASE-NOTES.tr.md). Aynı kılavuz uygulamadaki Yardım düğmesinden seçilen dilde açılır.

Uygulama modu şifresiz SAProuter dizelerini destekler. `/W/` ve eski `/P/` şifre bölümleri JSON/XML içine yazılmadan reddedilir; güvenli router şifresi desteği sonraki MCP geliştirmesini gerektirir. [SAP rota sözdizimine](https://help.sap.com/saphelp_em92/helpdata/en/48/6c62b71d5a350ce10000000a42189d/content.htm) bakın.

## RFC SDK / ICU başlatması ve EmptyStdout

Preview.3, SDK klasörü Windows PATH içinde olmayan makinelerde RFC worker başlangıcını düzeltir. **SAP RFC SDK** ekranında **lib** içeren SDK kök klasörünü seçip **SDK’yı doğrula ve kaydet** kullanın. lib içinde birbiriyle uyumlu x64 sapnwrfc.dll, libsapucum.dll, icuuc57.dll, icudt57.dll ve icuin57.dll bulunmalıdır. Önce yönetilen Python/PyRFC ve Visual C++ ön koşullarını kurun. SDK DLL’lerini Windows klasörüne kopyalamayın; genel PATH’i değiştirmeniz gerekmez.

Manager ve Host aynı alt süreç ortamını oluşturur: SAPNWRFC_HOME kayıtlı sdkDirectory, PATH’in ilk girdisi SDK lib olur. Node bu ortamı Python’a aktarır; RFC gerektiren ADT alt süreçleri de aynı Host’u kullanır. Mevcut profilleri, DPAPI dosyalarını ve istemci kayıtlarını yeniden oluşturmayın. SDK kullanıcı tarafından sağlanır; iki EXE’ye de eklenmez.

**Ready**, artık x64 DLL kontrolü, ICU yükleme/başlatması ve ayrı Python/PyRFC SDK başlatma kontrolü gerektirir. Kontrol SAP adresi, hesap veya şifre kullanmaz; yalnız yerel eksik adres nedeniyle beklenen RFC_INVALID_PARAMETER sonucunu kabul eder. Import veya sürüm sorgusu tek başına yeterli değildir. Eksik DLL’ler native SDK başlatılmadan yakalanır. Hatalarda güvenli kod, aşama, sınıf ve varsa ondalık/hex çıkış kodu gösterilir; ham stderr, PATH ve hesap bilgileri gösterilmez. Windows’ta POSIX signal değeri bulunmaz. Host/Node çıkış bilgisi Python worker’ın çıkış kodu değildir.

Kaydetme veya güncelleme sonrasında Codex/Claude içindeki RFC/ADT MCP süreçlerini tamamen yeniden başlatın; gerekirse istemciyi kapatıp açın. SDK kontrolünü ve bağlantı testini tekrarlayın. Sorunlu ortamda yetkili hesapla TED/TEX RFC_PING tekrar denenmeli ve yalnız hassas bilgilerden arındırılmış sonuç [#3 kaydına](https://github.com/tamerinal/sap_mcp_windows/issues/3) eklenmelidir. Bu canlı kabulün geçtiği yayımlanan sürümde iddia edilmez.

EmptyStdout sürerse uygulama sürümünü ve güvenli tanılama kimliklerini kaydedin. Worker exitCode/signal geliştirmesi yeni RFC paketi gerektirir; [ayrı geliştirme talebi](MCP-RFC-WORKER-DIAGNOSTICS.tr.md) olarak beklemektedir. Ham native trace, PATH, şifre, DPAPI dosyası veya gerçek SAP mapping dosyası yüklemeyin. SDK başlatması geçtikten sonra ağ/hesap yetkilerini ayrıca kontrol edin.


# ADT adresleri, TLS ve tanılama

[Ana sayfa](../README.tr.md) · [Kullanım kılavuzu](USER-GUIDE.tr.md)

## ADT adresini bulup kaydetme

Bilgisayarınızdan erişilebilen ADT web adresini SAP Basis ekibinden isteyin. Çalışan Eclipse ADT projesini sistem referansı olarak gösterebilirsiniz; RFC sistem numarası HTTP portunu belirlemez. Yetkili yönetici SMICM → Goto → Services üzerinden etkin HTTP/HTTPS servislerini okuyabilir. Dış Web Dispatcher/yük dengeleyici adresi bu iç adresten farklı olabilir. Sertifikanın SAN alanındaki DNS adını tercih edin. https://sap.example.invalid:8443 gibi kök URL girin (kurgusal); uygulama /sap/bc/adt/discovery yolunu ekler. Otomatik keşif şifresiz aday önerir, erişimi kanıtlamaz. SAP GUI/Eclipse erişiminiz yoksa aşağıdaki Basis talebini kullanın. SAP Yardım: https://help.sap.com/docs/SUPPORT_CONTENT/abapconn/3354079914.html

### Basis’e gönderilecek talep

Lütfen bilgisayarımdan erişilebilen ADT HTTP/HTTPS kök adresini, web portunu, varsa ters proxy yol önekini, VPN/proxy gereksinimlerini ve doğrulanmış kök/ara CA zincirini paylaşın. SAP kullanıcı/client hesabımın ADT discovery ve salt okunur compatibility servislerine erişebildiğini doğrulayın. Şifre göndermeyin.

Kök URL, RFC sistem numarası ve SAP client’tan bağımsızdır. Client/dil/hesabı kendi alanlarında tutun. Açık adtBaseUrl önceliklidir. Sondaki /sap/bc/adt bir kez kaldırılır; discovery yolunu istemci ekler. Query, fragment, gömülü kimlik bilgisi ve alt servis adresleri reddedilir. Ters proxy yol öneki, IPv4/IPv6 ve özel port desteklenir. HTTP’de kimlik bilgileri TLS ile korunmaz; sürekli uyarı gösterilir.

Eski profiller ve DPAPI dosyaları korunur. Eski boş URL artık şifreyle port tahmini başlatmaz: önce doğrulanmış URL seçip kaydedin. İçe aktarılmış eski adres listeleri profil verisinde kalır; açık alan boşsa ilk tanımlı adres kullanılır. Adres önerisi isteğe bağlı, yalnız hedef sunucuya yönelik ve kimlik bilgisizdir. 401/404 dahil aday yanıtları ADT varlığını kanıtlamaz. Seçim URL’yi forma alır; testten önce kaydedin.

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

Yerel kurgusal HTTP/TLS testleri profil yalıtımı, zincir, self-signed, SAN/tarih, redirect, proxy, kimlik ve yanıltıcı içerik senaryolarını kapsar. Manager/Host/MCP tutarlılığı canlı SAP olmadan yerel sunucuda sınanır. Native klavye/erişilebilirlik, temiz Windows/GUI’siz kurulum ve yetkili müşteri/canlı SAP kabulü yapılana kadar bekler. Otomatik OCSP/CRL/AIA, SSO/mTLS ve SAP tarafında servis/sertifika değiştirme bu teslimde yoktur. Windows/Node güven kuralları tarayıcı/Eclipse ile aynı değildir; yalnız tarayıcıda açılması MCP güvenini kanıtlamaz.

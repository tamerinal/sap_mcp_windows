# Kullanım kılavuzu — v0.1.0-preview.1

[English](USER-GUIDE.md) · [Ana sayfa](../README.tr.md)

## Kurulum ve ön koşullar

Çalışma zamanı indirilebiliyorsa Online, aksi halde Offline indirin. EXE’yi MCP istemcilerini kullanacak Windows hesabıyla çalıştırın. Yönetici .NET içerir ve `%LOCALAPPDATA%\Programs\SapMcpManager` altına kurulur; kullanıcı verisi `%LOCALAPPDATA%\SapMcpManager` altındadır.

İki bileşen başlangıçta seçilidir. Birini veya ikisini bırakın; boş seçim engellenir. EXE hazırlanmış arşivleri diske koyar. Yönetici içinde **Ön koşullar → Tekrar denetle → Gerekli Uygulamaları Kur** eksik kurulabilir gereksinimleri varsayılan işaretli, sürüm/durum/açıklamayla gösterir. Satırdaki Kur veya Seçilenleri Kur kullanın. Microsoft Visual C++ UAC isteyebilir. İptal tamamlanan paketleri korur; hatayı düzelttikten sonra tekrar deneyin. Kurulum sonunda kontroller yinelenir. Ardından **Seçili MCP bileşenlerini kur / onar** ile yalnız seçilen sunucuları kurun.

Ortak kontroller Windows 10 22H2/11 x64, en az 1 GB boş alan, yazılabilir klasörler, PowerShell DPAPI komutları, Node.js ve katalog bütünlüğünü kapsar. RFC için yönetilen Python 3.11/PyRFC, Visual C++ ve SDK da gerekir. Temel ADT Python/SDK gerektirmez. RFC paketi SDK beklerken ADT kullanılabilir. Genel durum ekranından bileşen ekleyip kaldırabilirsiniz; kaldırma profilleri/şifreleri korur.

## SDK’yı şimdi veya sonradan bağlama

Yetkili SAP hesabınızla SAP’nin resmî bağlayıcı sayfasından Windows x64 NetWeaver RFC SDK edinin. SDK hiçbir zaman dahil edilmez. Arşivi açıp uyumlu bağımlılıklarıyla `lib\sapnwrfc.dll` ve `lib\libsapucum.dll` içeren **üstteki `nwrfcsdk` klasörünü** seçin. SDK’yı doğrula ve kaydet kullanın. DLL mimarisi kontrol edilir ve SAP’ye giriş yapmadan Python/PyRFC ile gerçek yükleme denenir. Bu test için önce Python/PyRFC kurulmalıdır. Farklı SDK sürümlerinin DLL’lerini karıştırmayın.

Geçerli SDK yoksa RFC engelli kalır, temel ADT kullanılabilir. SDK’yı aynı sayfadan sonradan bağlayın. SDK’yı taşıma/silme RFC başlangıcını yeniden engeller; yeni yolu seçip doğrulayın.

## Sistemler ve profiller

SAP GUI landscape yaygın konumlarda aranır; gerekirse XML’i kendiniz seçip Sistemleri yükle kullanın. Kaynak XML değiştirilmez. Doğrudan SAPGUI host:port, alias ve router dizeleri desteklenir. Message-server grupları, SNC/SSO ve include genişletmesi bu önizleme dışındadır.

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

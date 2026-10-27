# Sorun giderme

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

Kaydetme veya güncelleme sonrasında Codex/Claude içindeki RFC/ADT MCP süreçlerini tamamen yeniden başlatın; gerekirse istemciyi kapatıp açın. SDK kontrolünü ve bağlantı testini tekrarlayın. Sorunlu ortamda yetkili hesapla TED/TEX RFC_PING tekrar denenmeli ve yalnız hassas bilgilerden arındırılmış sonuç [#3 kaydına](https://github.com/tamerinal/sap_mcp_windows/issues/3) eklenmelidir. Bu canlı kabulün geçtiği yayımlanan sürümde iddia edilmez.

EmptyStdout sürerse uygulama sürümünü ve güvenli tanılama kimliklerini kaydedin. Worker exitCode/signal geliştirmesi yeni RFC paketi gerektirir; [ayrı geliştirme talebi](MCP-RFC-WORKER-DIAGNOSTICS.tr.md) olarak beklemektedir. Ham native trace, PATH, şifre, DPAPI dosyası veya gerçek SAP mapping dosyası yüklemeyin. SDK başlatması geçtikten sonra ağ/hesap yetkilerini ayrıca kontrol edin.

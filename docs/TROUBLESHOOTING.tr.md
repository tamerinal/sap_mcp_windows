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

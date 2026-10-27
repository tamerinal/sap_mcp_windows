# SAP ADT MCP — taşınabilir kurulum ve RFC bağımsızlığı

Hedef depo: `tamerinal/sap_adt_mcp`. Bu dosya o projeye aktarılacak geliştirme talebidir; kurulum projesi kaynak depoyu değiştirmemiştir.

## Gerekli teslim

1. Sürümü etiketli `dist/`, üretim bağımlılıkları, npm kilidi ve commit/sürüm bilgisi içeren release ZIP. Kişisel makine yolu, SDK, gerçek şifre/mapping ve log bulunmamalı.
2. Node.js 24 üzerinde temel ADT işlevleri Python, PyRFC ve RFC SDK olmadan çalışmalı.
3. Landscape, mapping, PowerShell, log ve RFC başlatıcısının tamamı konfigüre edilebilir olmalı. RFC başlatıcısı kurulum tarafından `SapMcpHost.exe --component rfc --config <yol>` olarak verilecek; JSON args dizisini bozmadan kullanın.

## RFC özelliği

- `SAP_ADT_RFC_ENABLED=false` temel ADT kurulumunda geçerli bir durum olmalı. Bu durumda RFC süreçlerini arka planda başlatmayın.
- RFC gerektiren fonksiyon oluşturma/güncelleme/aktivasyon işlemlerinde, **ilk SAP değişikliğinden önce** RFC’nin hazır olduğunu doğrulayın. Sadece dosyanın bulunması yeterli değildir; başlatma/teşhis başarısını kontrol edin.
- Hazır değilse `RFC_UNAVAILABLE` ve yapılacak işlem açıklaması döndürün. Doğrulama yapılamadığı halde işlem başarılı gösterilmemeli; SAP nesnesi oluşturulduktan sonra eksik gereksinim keşfedilmemeli.
- Kullanılabilir özellikleri `--diagnose --json` sonucunda `capabilities.rfcVerification` gibi makinece okunur bir değerle bildirin. Bu teşhis SAP’ye yazmamalı.

Şu an kurulum başlatıcısı güvenlik için `remote_enabled=true` oluşturmayı, fonksiyon kaynağı güncellemesini ve fonksiyon aktivasyonunu RFC yokken engeller. MCP kendi ön kontrolünü sağladığında gereksiz geniş engelleme, sürüm yeteneği manifesti üzerinden daraltılabilir.

## Ortak çalışma sözleşmesi

- `SAP_ADT_LOG_LEVEL=Off|Error|Warn|Info|Debug|Trace`; varsayılan Info.
- `SAP_ADT_LOG_MAX_FILE_BYTES=10485760`, `SAP_ADT_LOG_MAX_FILES=10`, `SAP_ADT_LOG_RETENTION_DAYS=30`.
- Döndürme/saklama bütün istemci süreçlerinde güvenli çalışmalı. Hassas alanlar, kaynak kodu ve ham iş verileri loglara otomatik dökülmemeli.
- `--diagnose --json`: şema sürümü, build sürümü, durumlar, açıklayıcı hata kodları, RFC yeteneği; başarı 0/eksik 2. Başlangıç ve teşhis gerçek SAP bağlantısını zorunlu kılmamalı.
- Windows PowerShell için uyumlu modül yolu ve gizli süreçler kullanılmalı. DPAPI çözümünde baş/son boşlukları silen `trim()` kaldırılmalı; BSTR temizlenmeli.
- `target_key`, mevcut mapping öncelikleri ve `secretRef` uyumluluğu korunmalı. Çoklu profilleri yönetici uygulaması mevcut şemaya indirgeyecek.

## Kabul testleri

Yalnız ADT kurulumu; RFC seçilmemiş/SDK bekleniyor/başlatılamıyor durumları; bloklanan işlemlerde sıfır SAP değişikliği; mevcut kaynağı güncellemeden önce ön kontrol; özel ADT/BW URL’leri; Türkçe/boşluklu dosya yolları ve şifreler; log düzeyleri; gerçek SAP gerektirmeyen MCP handshake/tools-list testi.

Mac için secret provider ve platformdan bağımsız yollar ikinci fazdır. Kurulum projesi bu fazda Mac paketi yayınlamaz.

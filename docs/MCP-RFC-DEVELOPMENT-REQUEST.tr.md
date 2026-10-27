# SAP RFC MCP — taşınabilir kurulum sözleşmesi

Hedef depo: `tamerinal/SAP_RFC_MCP`. Bu dosya o projeye aktarılacak geliştirme talebidir; kurulum projesi kaynak depoyu değiştirmemiştir.

## Gerekli teslim

1. Etiketli sürüm için `dist/`, `python/worker.py`, üretim `node_modules`, `package.json`, bağımlılık kilidi ve `build-info.json` içeren release ZIP üretin. SDK DLL’leri, gerçek landscape, credential dosyaları, loglar ve kişisel yollar pakete girmemeli.
2. Windows x64 uyumluluk manifesti yayınlayın: Node 24; CPython 3.11; PyRFC 3.4 cp311-win_amd64. İlk snapshot’ta kullanılan PyRFC kaynak commit’i `5d4a20a5aee37accf32fc44ed8d668bd69332169`. SDK’yi derleme ortamında kullanıcı/kurum sağlamalı; çıkan PyRFC wheel SDK’yi içermemeli. Kaynak lisansları ve SHA-256 dahil olsun. Arşivlenen upstream’e karşı kurum tarafından bakımı yapılabilir, tekrar üretilebilir wheel akışı gereklidir.
3. `SAPNWRFC_HOME`, `SAP_RFC_PYTHON_EXECUTABLE`, `SAP_RFC_PYTHON_WORKER_FILE`, landscape/mapping/PowerShell/log ortam değişkenleriyle kişisel dizinlere ihtiyaç olmadan çalışın. Sabit makine yolu fallback’lerini kaldırın veya açıklayıcı eksik ayar hatasına dönüştürün.

## Teşhis arayüzü

`--diagnose --json` SAP’ye bağlanmadan aşağıdakileri kontrol etsin: yapılandırma, Python sürümü/mimarisi, PyRFC import, SDK DLL yükleme/sürümü, DPAPI sağlayıcısı ve klasör izinleri. Çıktı şema sürümü, bileşen sürümü, kontrol kimliği/durumu ve düzeltme mesajı içersin. Başarı 0; eksik/engelli koşul 2. Parola çözümü gerekiyorsa sonucu veya parola içeren hata çıktısını döndürmeyin.

Önerilen hata kodları: `SDK_NOT_CONFIGURED`, `SDK_ARCH_MISMATCH`, `SDK_LOAD_FAILED`, `PYTHON_UNAVAILABLE`, `PYRFC_UNAVAILABLE`, `SECRET_UNREADABLE`, `CONFIG_INVALID`.

## Log ve güvenli süreç davranışı

- `SAP_RFC_LOG_LEVEL=Off|Error|Warn|Info|Debug|Trace`; varsayılan Info. `Off` operasyon ve trace dosyası oluşturmamalı. Seviyeler yalnız görüntü filtresi değil yazma filtresi olmalı.
- `SAP_RFC_LOG_MAX_FILE_BYTES`, `SAP_RFC_LOG_MAX_FILES`, `SAP_RFC_LOG_RETENTION_DAYS`; varsayılan 10485760/10/30. Aynı log klasörüne birden fazla istemci yazabilir; döndürme süreçler arasında güvenli olmalı.
- Operasyon/oturum/Python trace olaylarında ortak `operation_id`, süre ve hata kodu olsun. Ham bağlantı sözlüğü, şifre, authorization/cookie/token ve keyfi BAPI iş verileri varsayılan olarak kaydedilmesin.
- PowerShell/Python süreçleri görünür konsol açmamalı. `powershell.exe` için PowerShell 7’den devralınmış uyumsuz `PSModulePath` temizlenmeli. stdout yalnız MCP protokolü için ayrılmalı.
- DPAPI çözümlemesindeki `stdout.trim()` şifrenin baş/son boşluklarını değiştirir. Base64 veya uzunluk belirten taşıma kullanın; parola karakterlerini aynen koruyun. BSTR temizliğini `finally` içinde yapın.

## Kabul testleri

SDK yok/yanlış mimari/bozuk DLL; CPython 3.11’in uyumlu güncel yamaları; SDK yolu boşluk/Türkçe karakter; başka kullanıcının DPAPI dosyası; şifrede Türkçe karakter ve baş/son boşluk; log seviyeleri/döndürme; temiz Windows 10/11 x64.

Mevcut çoklu profil çözümü MCP şema değişikliği gerektirmez: yönetici aktif profili `overridesByTarget[target_key]` ve `secretRef` ile yayınlar. Mac için Windows DPAPI çözümünü platforma ait secret provider arayüzüne ayırmak ikinci faz talebidir.

[Preview.3 worker diagnostics / worker tanılama talebi](MCP-RFC-WORKER-DIAGNOSTICS.tr.md) — pending / bekliyor.

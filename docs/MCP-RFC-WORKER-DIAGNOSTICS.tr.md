# RFC worker tanılaması — ayrı projeye geliştirme talebi

Hedef: tamerinal/SAP_RFC_MCP. Durum: **bekliyor; Manager deposunda uygulanmadı**. Özel depo ve mevcut MCP arşivi preview.3 içinde değişmedi. Teslimi [#3](https://github.com/tamerinal/sap_mcp_windows/issues/3) üzerinden izleyin.

src/sap/python-worker.ts içindeki child close işleyicisini close(code, signal) olarak değiştirin. PythonWorkerError ve güvenli tanılama ayrıntılarına isteğe bağlı exitCode, exitCodeHex (Windows unsigned gösterimi), signal ve terminationReason ekleyin. MCP araç/parametreleri, mevcut hata kind/code alanları ve normal yanıt uyumluluğu korunsun. Bilinmeyen çıkış veya signal null kalsın; Node/Host çıkışından Python çıkışı üretilmesin.

Boş çıktı/sıfır olmayan çıkışı worker süreç sonlanması, boş çıktı/sıfır çıkışı boş yanıt, bozuk stdout’u geçersiz JSON olarak sınıflandırın. Spawn hatası, timeout ve kullanıcı iptali ayrı olsun. Promise yalnız bir kez sonuçlansın; tüm bitiş yollarında timer temizlensin ve close sınıflandırması akışlar tüketildikten sonra yapılsın. Sonradan gelen close, timeout/iptali ezmesin. Yanıltıcı başarı JSON’u olsa bile sıfır olmayan worker çıkışı hata sayılsın.

Yalnız sabit kimlikler ve sayısal çıkış bilgileri döndürün. Ham stderr, tam ortam/PATH, stdin/bağlantı sözlükleri, şifre veya keyfi trace yüklerini istemci/loglara taşımayın. Worker, Host’un SAPNWRFC_HOME ve SDK lib içeren PATH ortamını miras alsın; kişisel varsayılan yola sessiz dönüş yapmasın. Windows yardımcı süreçleri gizli olsun.

Boş stdout/stderr ile hatalı çıkış, başarılı/boş sıfır çıkış, bozuk JSON, geçerli hata zarfı, Windows unsigned native çıkış, desteklenen yerde signal, spawn hatası, timeout/close yarışı, iptal ve stderr içindeki sırlar test edilsin. Manager’ın sonraki sürümde alabilmesi için manifest/SHA-256 içeren yeni sabitlenmiş taşınabilir release paketi üretin. Bu talep özel projeye otomatik mesaj veya değişiklik yapmaz.

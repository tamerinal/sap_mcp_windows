# SAP MCP — ortak agent başlangıcı

Bu depo `tamerinal/sap_mcp_windows` (SAP MCP Manager) geliştirmesi içindir.
Özel `SAP_RFC_MCP` ve `sap_adt_mcp` kaynakları ayrı depolardır; buranın kapsamı yalnız bu depodur.

Her görevde, göreve dönüşte ve bağlam yenilendiğinde, ortak klasördeki şu iki dosyanın
güncel içeriğini okuma aracınla aç; geliştirmeyi bu ortak kurallara ve mimariye göre yürüt:

1. `AGENT-RULES.md` — ortak agent çalışma kuralları
2. `ARCHITECTURE.tr.md` — mimari ve ortak geliştirme düzeni

Ortak klasör, depo dışında yaşayan tek kural kaynağıdır; etkin ağ konumu
`\\192.168.68.7\Bilgisayarlar\Sap_mcp_kurulum`'dur. Her bilgisayar bu paylaşıma kendi
erişim yoluyla ulaşır; somut yerel yol bu depoya işlenmez, o bilgisayarın
`opencode.json` `instructions` listesinde tanımlıdır ve OpenCode tarafından otomatik
yüklenir. Bu dosyalar araçla okunamıyorsa değişiklik yapmadan dur ve erişim sorununu
bildir. Eski yerel kopyayla sessizce devam etme; kuralları burada çoğaltma.

Kaynak kodda çalışılacak yer bu bilgisayarın kendi Git klonudur.
Her görev başında dalı, çalışma ağacını ve son commit'i kontrol et.
Her yeni geliştirme ayrı dalda başlar; aynı işe devam ediliyorsa mevcut iş dalını kullan.
Ortak kurallardaki dal kontrol betiğini değişiklikten ve commit/push/PR'dan önce çalıştır:
`./scripts/Check-WorkBranch.ps1 -ExpectedBranch '<bu-ise-ait-dal>'`.
İlk ilerleme mesajında dalı belirt. Aynı issue'da başka agent'ın iş sahipliğini kontrol et.
Issue'lar `https://github.com/tamerinal/sap_mcp_windows/issues` üzerinden alınır;
PR hedefi `YAPAY_ZEKA` dalıdır. Ana dala doğrudan yazma.

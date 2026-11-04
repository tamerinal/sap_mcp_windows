# SAP MCP — ortak agent başlangıcı

Bu depo `tamerinal/sap_mcp_windows` (SAP MCP Manager) geliştirmesi içindir.
Özel `SAP_RFC_MCP` ve `sap_adt_mcp` kaynakları ayrı depolardır; buranın kapsamı yalnız bu depodur.

Her görevde, göreve dönüşte ve bağlam yenilendiğinde şu iki dosyanın güncel içeriğini
okuma aracınla aç; geliştirmeyi bu ortak kurallara ve mimariye göre yürüt:

1. `D:\OneDrive\Bilgisayarlar\Sap_mcp_kurulum\AGENT-RULES.md`
2. `D:\OneDrive\Bilgisayarlar\Sap_mcp_kurulum\ARCHITECTURE.tr.md`

Ortak klasörün etkin ağ konumu `\\192.168.68.7\Bilgisayarlar\Sap_mcp_kurulum`'dur;
bu bilgisayarda aynı paylaşım OneDrive üzerinden yukarıdaki yoldan erişilir.
Dosyalar okunamıyorsa değişiklik yapmadan dur ve erişim sorununu bildir.
Eski yerel kopyayla sessizce devam etme; kuralları bu dosyada çoğaltma.

Kaynak kodda çalışılacak yer bu bilgisayarın kendi Git klonudur.
Her görev başında dalı, çalışma ağacını ve son commit'i kontrol et.
Her yeni geliştirme ayrı dalda başlar; aynı işe devam ediliyorsa mevcut iş dalını kullan.
Ortak kurallardaki dal kontrol betiğini değişiklikten ve commit/push/PR'dan önce çalıştır:
`./scripts/Check-WorkBranch.ps1 -ExpectedBranch '<bu-ise-ait-dal>'`.
İlk ilerleme mesajında dalı belirt. Aynı issue'da başka agent'ın iş sahipliğini kontrol et.
Issue'lar `https://github.com/tamerinal/sap_mcp_windows/issues` üzerinden alınır;
PR hedefi `YAPAY_ZEKA` dalıdır. Ana dala doğrudan yazma.

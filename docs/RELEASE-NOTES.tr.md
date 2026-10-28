# v0.1.0-preview.6 — Basic ADT keşif düzeltmesi

[English](RELEASE-NOTES.md)

Preview.5 hatası düzeltildi: sunucu portsuz girildiğinde `explicitPort: null` gönderilebiliyor, ADT süreci ağa çıkmadan bunu reddediyordu. Genel uyarı bu girdi hatasını gizliyordu; RFC bağımsız olarak başarılı olabiliyordu. Manager artık boş isteğe bağlı alanları göndermez; güncel ADT 0.3.1 eski null alanlarını da kabul eder. Güvenli girdi/süreç kodları, yerel keşif hatalarını ağ/TLS sonuçlarından ayırır.

Manager'ı yükseltin; Basic'te **Kurulumu hazırla** veya Advanced'te ADT kur/onar ile güncel ADT paketini kurun ve istemcilerde MCP süreçlerini yeniden başlatın. Hem Manager hem ADT paketi güncellenmelidir. Profiller, şifreler, ayarlar ve RFC arşivleri korunur. Bilinen HTTPS adresi elle girilebilir. Instance numarası HTTPS portunu kesinleştirmez; gerçek servisin DNS adı ve CA güveni ayrıca doğru olmalıdır.

Regresyonlar gerçek Manager portsuz JSON isteğini, eski null isteğinin gerçek Node CLI'da çalışmasını ve Manager–Node yerel HTTPS keşfini kapsar. Gerçek test sayıları ve sınırlar doğrulama dosyalarındadır. [Basic yardımı](https://github.com/tamerinal/sap_mcp_windows/blob/v0.1.0-preview.6/docs/BASIC-SETUP.tr.md) · [#6 takip kaydı](https://github.com/tamerinal/sap_mcp_windows/issues/6).

İmzasız önizleme. Temiz Windows, canlı SAP ve sorunlu makinede kabul bekliyor. SDK dağıtılmaz. Inno Setup 6.7.3 non-commercial use only bildirir; ticari dağıtım uygun lisans ister. MIT üçüncü taraf koşullarını değiştirmez. Özel MCP'nin derlenmiş JavaScript ve bağımlılıkları EXE'den çıkarılabilir. Önceki yayınlar korunur. Her ortamda koşulsuz uyumluluk iddiası yoktur.

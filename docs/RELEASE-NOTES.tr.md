# v0.1.0-preview.4 — ADT adresi ve TLS yönetimi

[English](RELEASE-NOTES.md)

Manager ve güncellenen özel ADT MCP’ye profil bazında bağlantı/sertifika yönetimi ekler. Açık HTTP/HTTPS URL, özel port ve proxy yol önekleri eski adres listelerine göre önceliklidir. RFC sistem numarası web portunu belirlemez. Eski boş URL’ler kimlik bilgisi gönderilmeden doğrulanıp kaydedilmelidir; isteğe bağlı adres önerisi kimlik bilgisizdir ve ADT varlığını kanıtlamaz.

ADT alanında “Bu adresi nasıl bulurum?” yardımı; kopyalanabilir Basis talebi, SMICM servis bilgileri ve iç ICM/dış Web Dispatcher ayrımı bulunur. EN/TR yardım uygulamada ve GitHub’dadır. [ADT/TLS kılavuzu](https://github.com/tamerinal/sap_mcp_windows/blob/v0.1.0-preview.4/docs/ADT-CONNECTIONS.tr.md).

Profil sertifikaları doğrulanmış açık PEM/DER dosyalarını alır; kimlik/tarih/SAN/SHA-256 gösterir. Profil güveni Node varsayılanları ve Windows güveniyle birleşir; OS deposu ve genel TLS değiştirilmez. Ara sertifika tek başına kök yapılmaz; ad ve tarihler denetlenir. Kimlik bilgisiz sunucu incelemesi güven vermez. TLS atlama yalnız profil için açık tercihtir, yeni profilde kapalıdır ve uyarılır. HTTP’de şifresiz taşıma uyarısı süreklidir.

Manager, seçili kaydedilmiş profili geçici özel mapping ile test eder; ortak aktif hesap değişmez. Gerçek MCP ile Manager aynı Node katmanını kullanır. Mevcut araçlara diagnose_connection, inspect_tls_certificate ve discover_adt_endpoints eklenir; ping_system tam doğrulama ister. Aşamalar yapılandırma, etkin DNS/TCP yolu, TLS, kimlik, gerçek ADT discovery ve salt okunur graph içerir. Kimlik reddinde otomatik tekrar durur; redirect/farklı origin’e kimlik aktarımı engellenir. Proxy ortamı/NO_PROXY korunur; HTTPS CONNECT uç sunucu TLS’sini doğrular. Maskelenmiş destek raporu yalnız izinli metadata içerir.

**Güncelleme:** Yeni Manager’ı kurun; Seçili MCP bileşenlerini kur/onar ile ADT’yi güncelleyin, sonra Codex/Claude içindeki RFC/ADT MCP süreçlerini tamamen yeniden başlatın. connectionPolicyVersion=1 olmayan eski ADT paketi bilinçli olarak engellenir. Mevcut ayar şeması, profil, şifre referansı ve istemci kayıtları korunur. CA referanslarını Manager’da açıkça doğrulayın; başka kullanıcının DPAPI dosyasını kopyalamayın. RFC/PyRFC ve runtime paket hashleri değişmez; RFC deposuna dokunulmadı. SDK kullanıcı tarafından sağlanır, iki kurulumda da yoktur.

[#4 kaydı](https://github.com/tamerinal/sap_mcp_windows/issues/4), teslimi ve bekleyen kabulü izler. Gerçek kaynak commit/CI/test sonuçları VALIDATION-SUMMARY.tr.md dosyasındadır. Testler canlı SAP yerine yerel kurgusal HTTP/TLS sunucuları ve üretilen sertifikaları kullanır. Özel ADT kaynağı özel kalır; derlenmiş JS ve bağımlılıklar public kurulumdan çıkarılabilir.

**İmzasız önizleme.** GUI’siz/ağ kapalı dahil temiz Windows 10 22H2/11, native erişilebilirlik/klavye ve canlı müşteri SAP kabulü bekler. Bu geliştirici makinesinde SAP GUI vardır; testler çağırmaz. Inno Setup 6.7.3 non-commercial use only bildirir; ticari derleme/dağıtım uygun lisans gerektirir. MIT üçüncü taraf koşullarını değiştirmez. Önceki önizlemeler korunur.

Sınırlar: Basic/DPAPI hesap doğrulaması; yeni SSO/mTLS, etkileşimli proxy doğrulaması, OS güven editörü, otomatik OCSP/CRL/AIA veya SAP’de sertifika/servis değiştirme yoktur. Kesin ayrılamayan zincir/güven koşulları belirsiz olarak raporlanır. Anonim endpoint girilen hesabı kanıtlayamaz. Tarayıcı/Eclipse ve Node güven depoları farklıdır. RFC SDK, message-server/SNC/include, şifreli SAProuter ve macOS sınırlamaları sürer. Her SAP ortamında koşulsuz çalışma iddiası yoktur.

# v0.1.0-preview.1 — Windows x64 önizleme

[English](RELEASE-NOTES.md)

SAP MCP Yöneticisi’nin ilk public önizlemesi: seçmeli RFC/ADT, yönetilen ön koşullar, kullanıcı SDK’sı, DPAPI profilleri, Codex/Claude entegrasyonu, İngilizce/Türkçe arayüz ve yardım. Çalışma zamanı indirmek için Online, dahil paketler için Offline seçin. İkisinde de SAP SDK yoktur. Kurulum için GitHub hesabı veya kaynak derleme gerekmez.

## Doğrulama ve sınırlamalar

Çekirdek/arayüz testleri, yerel MCP entegrasyonları ve dört kurulum senaryosu yetkili geliştirici Windows ortamında çalıştırılır. Yayımlanan VALIDATION-SUMMARY.md gerçek sonuçları açıklar. Headless arayüz kapsamı altı ekran × iki dil × iki tema × iki genişlik ve açık alan/şifre korumasıdır. Bu, yerel masaüstü erişilebilirlik/manuel inceleme veya temiz makine kabulünün **yerine geçmez**.

- Uygulama ve EXE’ler **imzasızdır**; Windows/kurum politikası uyarabilir veya engelleyebilir. Yayımlanan SHA-256 değerlerini doğrulayın.
- **Temiz Windows 10 22H2 ve Windows 11 VM testleri tamamlanmamıştır. Canlı SAP testleri tamamlanmamıştır.** Üretim onaylı sürüm olarak değerlendirmeyin.
- Bu ortamdaki Inno Setup 6.7.3 derleyicisi **non-commercial use only** bildirir. Ticari derleme/dağıtım uygun Inno lisansı gerektirir. Derleyici pakette yoktur; kodun MIT lisansı diğer koşulların yerine geçmez.
- RFC SDK kullanıcıdan gelir. PyRFC arşivlenmiş upstream’den onaylı yerel 3.4 derlemesidir; tam tekrarlanabilir upstream wheel teslimi ayrı taleptir. SDK dosyaları dışarıda tutulur.
- Üst MCP depoları özel kalır; EXE içindeki derlenmiş JS/bağımlılıklar çıkarılabilir. Değiştirilmiş kaynak snapshot’ları paket kaydında belirtilir; dağıtım uyarlamaları kişisel varsayılan ve debug yollarını nötrleştirir.
- Üst MCP yerel logları tam politika desteklemez; host maskeli oturum/araç metadata kaydeder ve ham payload tracing kapatır. RFC hazır değilse ADT fonksiyon güncelleme/etkinleştirme ihtiyatlı engellenir.
- Bu sürümde macOS, SNC/SSO, message-server grubu, SAP’de parola değiştirme veya otomatik güncelleme yoktur.

Ayarlar/şifreler onarım/güncellemede korunur. Kaldırma veriyi varsayılan olarak saklar; yalnız etkileşimli açık onayla varsayılan sahip olunan veri silinir. Gerçek hesap hazırlamadan kılavuzu okuyun.

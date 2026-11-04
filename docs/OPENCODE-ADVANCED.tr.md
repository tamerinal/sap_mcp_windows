# OpenCode ve Advanced otomatik doğrulama

[English](OPENCODE-ADVANCED.md) · [Kullanım kılavuzu](USER-GUIDE.tr.md)

## OpenCode

Basic veya Advanced istemci entegrasyonunda OpenCode’u seçin. Global (tüm projeler) veya Proje kapsamını, tam yapılandırma dosyasını ve V1/V2 biçimini seçin. Mevcut MCP içeriği başlangıç sürümünü belirler. Karışık veya uyumsuz dosya otomatik dönüştürülmez; işlem engellenir. Yeni dosyada varsayılan V2’dir.

Global dosya normalde `~/.config/opencode/opencode.json(c)` konumundadır. Proje dosyası proje kökünde veya `.opencode` altında olabilir. Birden fazla aday varsa birini seçin; Manager tüm adaylara yazmaz. Proje veya yönetilen ayarlar global ayarları geçersiz kılabilir. Kullandığınız OpenCode’un gerçekten yüklediği dosyayı seçin.

V1, sunucuları `enabled` alanıyla doğrudan `mcp` altında; V2, `disabled` alanıyla `mcp.servers` altında tutar. İkisi de SapMcpHost’u yerel stdio komut dizisiyle çalıştırır. Resmî [V1 MCP kılavuzu](https://opencode.ai/docs/mcp-servers/) ve [V2 MCP kılavuzu](https://opencode.ai/v2/docs/mcp-servers/) bağlantılarına bakın.

Önizleme Yeni, Güncellenecek veya Zaten güncel kayıtları gösterir. Çakışmaları inceleyip değiştirmeye açıkça izin verdikten sonra uygulayın. Mevcut yorumlar, ortak agent `instructions` listesi, modeller ve ilgisiz sunucular korunur. Değişiklikten önce yedek alınır; önizlemeden sonra değişmiş dosya yeniden önizlenmelidir. Aynı kayıt yeniden yazılmaz. Aynı Host/bileşen/ayar dosyası için mevcut farklı ad yeniden kullanılır; birden fazla eşleşme varsa yazma engellenir.

Yalnız geçerli modun uygun bileşenleri sunulur. RFC kaydı SDK hazırlık kontrolünü atlatmaz. Registration içine `OpenCode`, seçilen dosya ve gerçek sunucu adları kaydedilir. Kaldırma yalnız bu Manager’a ait kalmış kayıtları siler; komutu değiştirilmiş veya ilgisiz girdilere dokunmaz.

Uyguladıktan sonra OpenCode’u yeniden başlatıp ilgili projede `opencode mcp list` çalıştırın. Kopyalanabilir yardım iki OpenCode biçimini içerir. Kopyalanan örneği mevcut dosyanızın tamamının yerine koymayın.

## Advanced: Otomatik keşfet ve doğrula

Bağlantılar ekranında sistem ve mevcut/yeni profil seçin. Kullanıcı/client/dil bilgilerini ve değiştirilecekse parolayı girin. **Otomatik keşfet ve doğrula** düğmesine basın. Sayfanın açılması veya alan düzenleme tarama başlatmaz.

İşlem Basic’in hazırlık, RFC destekli keşif, onaylı akıllı/tam tarama ve ayrı ADT/RFC doğrulamasını kullanır. Aynı açık iletişim uyarısı geçerlidir: HTTP şifresizdir; değerlendirme sırasında HTTPS sunucu sertifikasını doğrulamaz. Mevcut sertifikalar korunur. İptal etmek veya onayı reddetmek formu kaydetmez.

Başarılı ADT sonucu bulunan URL’yi doldurur ve değerlendirilen bağlantının gerektirdiği TLS kutusunu gösterir. Ayrı sonuçları ve bu alanları inceleyin. **Kaydet’e basılana kadar hiçbir profil kaydı uygulanmaz.** Mevcut etkinleştirme kutusu profil aktivasyonunu belirler. Alan/profil değişikliği değerlendirmeyi geçersiz kılar; yeniden doğrulayın. RFC hazırlığı eksikken yalnız ADT başarılı olabilir.

Harici SAP GUI XML salt okunur kalır; değerlendirme yönetilen kaynağa dönüşüm yapmaz. Başarısızlık mevcut profil, sırlar ve aktif mapping’i korur. Basic akışı ile mevcut Advanced tanılama/sertifika araçları kullanılmaya devam eder.

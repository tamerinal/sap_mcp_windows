# Diğer agent ve uygulamalara MCP ekleme

Manager'ın **İstemci entegrasyonu** ekranının sonundaki **Diğer agent ve uygulamalara ekle** bölümünü açın. Basic'in son özet ekranında da aynı yardım bulunur. JSON, TOML veya **Alanları elle doldur** görünümünü seçin. Uygulama içindeki kodlar gerçek Host ve ayar dosyası yollarıyla üretilir; aşağıdaki yollar yalnız örnektir.

## Hangi uygulamalar kullanabilir?

Hedef uygulama aynı bilgisayarda, Manager ile aynı Windows kullanıcısı altında yerel **stdio MCP** süreci çalıştırabilmelidir. HTTP/SSE URL isteyen bulut bağlantılarına bu komut doğrudan eklenemez. Manager'ın açık kalması gerekmez. DPAPI ile korunan şifreler aynı Windows hesabına bağlıdır.

Tüm istemciler Manager'daki ortak aktif SAP profillerini kullanır. Aktif profil değiştirildiğinde istemcinin MCP süreçlerini yeniden başlatın. Şifreyi, token'ı, SDK PATH ayarını veya SAP Landscape XML içeriğini istemci koduna eklemeyin; Host yerel ayarları ve alt süreç ortamını yönetir.

Basic yalnız güncel doğrulaması uygun bileşenleri gösterir. RFC ayrıca güncel hazırlık sonucu gerektirir. Yalnız ADT başarılıysa RFC girdisi üretilmez. Alan veya bağımlılık değişiklikleri örnekleri geçersizleştirebilir; yeniden doğrulayın. Advanced seçili ve kurulu bileşenleri gösterir; bağlantı testlerini ayrıca tamamlayın. Yardımı açmak/kopyalamak SAP bağlantısı başlatmaz ve hiçbir istemci dosyasını değiştirmez.

## JSON ile ekleme

1. Hedef uygulamanın MCP yapılandırmasını açın ve yedeğini alın.
2. Yeni dosyada aşağıdaki tam örneği kullanın. Mevcut `mcpServers` varsa yardımın **sunucu girdileri** örneğindeki adlandırılmış girdileri bu nesneye birleştirin. Dış nesne parantezlerini olduğu gibi araya yapıştırmayın; virgülleri mevcut JSON'a göre düzenleyin.
3. İkinci bir `mcpServers` anahtarı veya aynı adlı ikinci sunucu eklemeyin; diğer uygulama ayarlarını koruyun. JSON yorum kabul etmez.
4. Gerçek dosya yollarını kullanın. Basic'te gösterilmeyen bileşeni elle ekleyerek doğrulamayı atlamayın.

```json
{
  "mcpServers": {
    "sap_adt_mcp": {
      "command": "C:\\SAP MCP Manager\\SapMcpHost.exe",
      "args": [
        "--component",
        "adt",
        "--config",
        "C:\\SAP MCP Data\\config\\settings.json"
      ]
    },
    "sap_rfc_call": {
      "command": "C:\\SAP MCP Manager\\SapMcpHost.exe",
      "args": [
        "--component",
        "rfc",
        "--config",
        "C:\\SAP MCP Data\\config\\settings.json"
      ]
    }
  }
}
```

Bu örnek `mcpServers` şeması içindir. Başka bir nesne adı veya `type: "stdio"` gibi ek alan isteyen uygulamalarda, o uygulamanın şemasını izleyin. Host komutu ve argümanları aynı kalır. Her MCP için ayrı süreç kullanılır.

## TOML ile ekleme

`mcp_servers` tablolarını destekleyen istemcilerde aşağıdaki bölümleri birleştirin. Var olan SAP tablolarını güncelleyin; aynı tabloyu ikinci kez eklemeyin. Timeout alanları mevcut Codex yapılandırmasıyla aynıdır; başka istemcide yalnız desteklenen alanları kullanın.

```toml
[mcp_servers.sap_adt_mcp]
command = "C:\\SAP MCP Manager\\SapMcpHost.exe"
args = ["--component", "adt", "--config", "C:\\SAP MCP Data\\config\\settings.json"]
startup_timeout_sec = 60
tool_timeout_sec = 180
enabled = true

[mcp_servers.sap_rfc_call]
command = "C:\\SAP MCP Manager\\SapMcpHost.exe"
args = ["--component", "rfc", "--config", "C:\\SAP MCP Data\\config\\settings.json"]
startup_timeout_sec = 60
tool_timeout_sec = 180
enabled = true
```

## Alanları elle doldurma

Custom / Local MCP ekranında her bileşen için ayrı sunucu ekleyin:

| Alan | ADT | RFC |
|---|---|---|
| Ad | `sap_adt_mcp` | `sap_rfc_call` |
| Bağlantı | `stdio` | `stdio` |
| Komut | Host'un tam dosya yolu | Aynı Host yolu |
| Argüman 1 | `--component` | `--component` |
| Argüman 2 | `adt` | `rfc` |
| Argüman 3 | `--config` | `--config` |
| Argüman 4 | Ayar dosyasının tam yolu | Aynı ayar dosyası yolu |

Komut alanına yalnız dosya yolunu girin. Argümanları dört ayrı öğe olarak ekleyin; form alanlarına JSON kaçışlarını veya çevreleyen tırnakları taşımayın. URL ve ek ortam değişkeni gerekmez.

## Son kontrol ve sorun giderme

Kaydedin ve hedef uygulamanın MCP süreçlerini yeniden başlatın. Sunucunun bağlı göründüğünü ve SAP araçlarının listelendiğini kontrol edin. Araçların listelenmesi SAP oturum açma başarısını kanıtlamaz; Manager'da kullanıcı tarafından başlatılan bağlantı doğrulamasını tamamlayın.

Araçlar görünmezse Host/ayar dosyalarının bulunduğunu, istemcinin aynı Windows hesabında çalıştığını, yerel stdio desteğini ve yapılandırma şemasını kontrol edin. Manager'daki hazırlık ve bağlantı sonuçlarına, ardından Host günlüklerine bakın. RFC native yükleme hatasını SAP logon hatasıyla karıştırmayın. Şifre veya tam ortam dökümünü destek kayıtlarına eklemeyin.

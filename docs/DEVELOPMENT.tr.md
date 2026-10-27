# Geliştirici kılavuzu

[English](DEVELOPMENT.md) · [Ana sayfa](../README.tr.md)

## Mimari

`SapMcp.Desktop` Avalonia/.NET 10 tabanlı kullanıcı başına yöneticidir. `SapMcp.Core` şema-1 ayarlarını, atomik kaydı, DPAPI, landscape/profil mapping, paket doğrulama, ön koşul, istemci birleştirme ve maskeli dönen logları yönetir. `SapMcp.Host` arayüzden bağımsız güncel ayarları okuyup açık ortam değişkenleri üretir ve MCP stdio aktarır. Her başlangıçta RFC durumu doğrulanır. RFC yoksa ona bağımlı ADT işlemleri iletilmeden ihtiyatlı biçimde engellenir. MCP araç adları ve parametreleri korunur.

Core içindeki `Resources/Strings.json` ortak İngilizce/Türkçe kaynaktır; sabit kaynak anahtarları MCP teşhis kimliklerinden ayrıdır. İsteğe bağlı `uiLanguage` şema-1 uyumunu korur. Arayüz yeniden gösterimi açık formu yeniden oluşturmadan ve alan değerlerini yazmadan etiketleri günceller. Kullanıcının TextBox girdisini yerelleştirmeden geçirmeyin. Mac şifre/kurulum adaptörleri sonraki aşamadır.

Bağlantı kaynakları ortak okuyucu arayüzünü uygular: dış SAP GUI XML’i salt okunur, yönetilen JSON doğrulanan CRUD destekler ve değişmeyen MCP paketleri için uygulamaya ait XML üretir. Kaynak modu/yolları isteğe bağlı şema-1 alanlarıdır. Kayıt doğrulanan içerikleri hazırlar, değişiklikleri kilitle sıraya alır, yazma/iptal hatasında katalog, mapping, XML ve ayarları geri alır. Bu hata geri almadır; çökme dayanıklı çok dosyalı veritabanı transaction değildir. Şifre dosyaları katalog dışındadır. Algılama öneridir, kayıtlı mod önceliklidir, eski landscape ayarları dış kaynak kalır. [Bağlantı kaynaklarına](CONNECTIONS.tr.md) bakın.

## Public kaynak derleme ve test

Gerekenler Windows x64 ve .NET SDK 10’dur (global.json). Özel depo veya SAP hesabı gerekmez:

```powershell
dotnet restore SapMcpManager.slnx --locked-mode
dotnet build SapMcpManager.slnx -c Release --no-restore
dotnet test SapMcpManager.slnx -c Release --no-build --logger trx
dotnet run --project src/SapMcp.Desktop
```

Kaynaktan derlenen arayüz payload olmadan açılır; kontroller eksik katalog bildirir. Birim testleri DPAPI/PowerShell uyumu, atomik kayıt, içe aktarım/profil önceliği, istemci koruma, arşiv, hash, log ve protokol engellerini kapsar. Headless Avalonia testi altı sayfayı iki dil/tema/genişlikte açar ve dil değişiminde kaydedilmemiş alan/şifreyi korur. PNG için `SAP_MCP_UI_SNAPSHOTS` çıktı klasörü belirtin. GitHub Actions Windows üzerinde bu public derleme/testi yapar; özel kaynak, SDK veya SAP’ye erişmez. Entegrasyon ayrı executable’dır; public test keşfine girmez.

## Yetkili paket hazırlama

Yalnız özel RFC/ADT kaynaklarına ve onaylı cp311 win_amd64 PyRFC derlemesine erişen yayın sorumlusu kurulum hazırlar. Son kullanıcı kod derlemez. Açık yollar verin; geliştirici bilgisayarına ait varsayılan yol yoktur:

```powershell
./scripts/Prepare-Payload.ps1 -RfcSource $rfcSource -AdtSource $adtSource -PyRfcSitePackages $approvedSitePackages
./scripts/Build.ps1 -SkipPayload -InnoCompiler $iscc
./scripts/Verify-Release.ps1 -SdkDirectory $userSuppliedSdk
./scripts/Test-Installers.ps1
./scripts/Test-Publication.ps1
```

Üç kaynak parametresi verilirse Build.ps1 paketleri de hazırlar. Node/Python/VC++ sürüm ve hashleri `payload/sources.lock.json` içinde sabittir; `-RefreshRuntimePins` bunları bilerek günceller ve inceleme gerektirir. İndirmeler hash ile, pin yenilemede Microsoft paketleri imza ile kontrol edilir. MCP’ler izole kopyada npm ci/build/prune ile hazırlanır. Dağıtım uyarlamaları kişisel config varsayılanlarını değiştirir; özgün depolar korunur. `build-info.json` commit, değiştirilmiş snapshot ve uyarlamaları kaydeder. PyRFC metadata onaylı upstream commit ile özgün/dağıtılan native hashlerini içerir; PDB çıkarılır, CodeView PDB yolu çalıştırılabilir kodu değiştirmeden nötrleştirilir. Uyarlama sonrası SDK yükleme testi geçmelidir.

Paketlerde derlenmiş kod, üretim bağımlılıkları, kilitler ve kaynak bilgisi vardır; SDK/müşteri verisi yoktur. Hazır PyRFC ZIP bir yayın girdisidir; tekrarlanabilir upstream wheel akışı hâlâ talep edilmektedir. İki EXE hazır MCP taşır; Offline sabit runtime önbelleğini ekler. Online özel kaynak yerine eksik çalışma zamanını indirir. Inno Setup 6.7.3 yalnız derleme aracıdır, dağıtılmaz. Bu derleyici non-commercial use bildirir: ticari derleme/dağıtım için uygun Inno lisansı edinin. Kodumuzun MIT lisansı Inno veya çalışma zamanı koşullarının yerine geçmez.

## Yayın süreci

1. Başlangıç commit’i dahil `YAPAY_ZEKA` geçmişini koruyun. Kod, test, belge, betik, kilit, örnek ve bildirimleri kaydedin; yerel araç/çıktı/müşteri verilerini .gitignore ile dışarıda tutun.
2. Directory.Build.props, kurulum/dosya adları, host teşhisi, katalog ve sürüm notlarını eşitleyin. uiLanguage içermeyen eski ayarları doğrulayın.
3. Paket hazırlama, derleme, çekirdek/arayüz/entegrasyon/kurulum testleri ve Test-Publication çalıştırın. Yayın kontrolü kaynak ve dağıtımı kişisel yol, şifre ve SDK için tarar. Görselleri ve raporları da inceleyin.
4. Maskelenmiş doğrulama özeti ve İngilizce/Türkçe sürüm notlarını yayın dosyalarına ekleyin. İki EXE ve belge arşivi için SHA256SUMS.txt üretin. Lisans ve kaynak kayıtlarını inceleyin. Temiz VM/canlı SAP yapılmadıysa tamamlandı yazmayın.
5. Kaynağı gönderip Public Windows CI sonucunu bekleyin. Test edilen commit’te `v0.1.0-preview.2` etiketi oluşturup **prerelease** yayınlayın. İki EXE, kılavuz arşivi, SHA dosyaları, notlar ve doğrulama özeti ekleyin. README etiket bağlantısı kullanmalı; önizlemeler kararlı latest değildir.
6. Dosyaları oturumsuz HTTPS üzerinden indirip hash karşılaştırın. README/kılavuz bağlantılarını oturum kapalı kontrol edin. Başarısız kontrol düzeltilmeden yayınlamayın.

Bu önizlemede otomatik güncelleme sunucusu veya imza sertifikası yoktur. Mevcut kullanıcı kurulumu kayıtlıysa smoke test çalışmaz. Workspace artifacts içine kurup kaldırır ve kullanıcı verisini korur; üretim kabulü için temiz VM kullanın.

## Diğer projelere talepler

[RFC talebi](MCP-RFC-DEVELOPMENT-REQUEST.md) ve [ADT talebi](MCP-ADT-DEVELOPMENT-REQUEST.md); taşınabilir yayın, diagnose --json, parametrik yollar, gerçek log filtreleme/saklama, tekrarlanabilir PyRFC ve bağımsız ADT/RFC engellerini tanımlar. Bunlar ayrı projelere iletilecek belgelerdir; otomatik mesaj veya kaynak değişikliği yapılmaz.

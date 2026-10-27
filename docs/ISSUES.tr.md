# Hata bildirimi, geliştirme isteği ve iş takibi

[English](ISSUES.md) · [Ana sayfa](../README.tr.md) · [Bildirim aç](https://github.com/tamerinal/sap_mcp_windows/issues/new/choose) · [Tüm kayıtlar](https://github.com/tamerinal/sap_mcp_windows/issues)

Uygulamanın hata ve geliştirme talepleri GitHub Issues üzerinden ortak, herkese açık kayıtlarda tutulur. Her bildirimin bir issue numarası, konuşması ve geçmişi olur. Türkçe ve İngilizce bildirim kabul edilir. Okumak herkese açıktır; kayıt açmak veya abone olmak için GitHub hesabı gerekir. Uygulama şu anda otomatik bildirim veya log yüklemesi yapmaz.

## Doğru formu seçme

| Form | Ne için kullanılır? | Girilecek bilgiler |
|---|---|---|
| [Hata bildirimi](https://github.com/tamerinal/sap_mcp_windows/issues/new?template=bug_report.yml) | Mevcut davranış çalışmıyor veya belgelenen davranıştan farklı. | Sürüm ve Online/Offline kurulum, Windows derlemesi/arayüz dili/tema, etkilenen alan, adımlar, beklenen/gerçekleşen davranış ve isteğe bağlı maskeli teşhis kimliği/metni. |
| [Geliştirme isteği](https://github.com/tamerinal/sap_mcp_windows/issues/new?template=feature_request.yml) | Yeni davranış veya mevcut akışta iyileştirme. | Kullanıcı ihtiyacı, öneri, ilgili alan, gözlenebilir kabul örneği ve isteğe bağlı alternatifler/mevcut sürüm. |

Önce [Sorun giderme](TROUBLESHOOTING.tr.md) belgesini okuyup açık ve kapalı kayıtları arayın. Aynı sorun varsa yeni kayıt yerine mevcut kayda tekrar bilgilerinizi ekleyin veya 👍 tepkisi verin. Farklı her sorun/istek için ayrı kayıt açın. Kurgusal sistem/kullanıcı kullanın. Başlık tetikleyici ve sonucu anlatsın: “SDK klasörü taşındıktan sonra doğrulama başarısız” veya “Yardım ekranında bildirim bağlantısı gösterilsin” gibi.

## Gizli kalacak bilgiler

Kayıtlar ve ekler herkese açıktır. Şifre, DPAPI dosyası, gerçek SAP landscape/mapping dosyası, istemci ayarı, token, yetkilendirme başlığı, SDK arşivi veya ham SAP iş verisi/kaynak kodu yüklemeyin. Kısa, kendiniz inceleyip maskelediğiniz hata kesiti ve sabit teşhis kimliği yeterlidir. Ekran görüntüsü isteğe bağlıdır; paylaşmadan önce hesap/sistem adlarını, yolları ve hassas bilgileri kendiniz kaldırın. Tam log paketi gerekmez. Güvenlik açıklarında şifre veya saldırı ayrıntısı içeren public issue yerine [Güvenlik bildirimi](../SECURITY.tr.md) yolunu izleyin.

## İlerlemeyi izleme

Kaydın **Subscribe** düğmesiyle GitHub bildirim ayarlarınıza göre güncellemeleri alın. Tüm depo kayıtları için GitHub’da **Watch → Custom → Issues** seçebilirsiniz. Bunlar kişisel GitHub tercihleridir; talep açmak ayrı bir e-posta servisi veya arka planda Codex takibi başlatmaz.

| Görünüm | Gösterdiği |
|---|---|
| [Açık hatalar](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aopen%20label%3Abug) | Mevcut kusurlar |
| [Açık geliştirme istekleri](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aopen%20label%3Aenhancement) | Talep edilen iyileştirmeler |
| [İlk inceleme bekleyenler](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aopen%20label%3Astatus%3Atriage) | Yeni kayıtlar |
| [Bilgi bekleyenler](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aopen%20label%3Astatus%3Aneeds-info) | Bildirenden açıklama gerekiyor |
| [Kabul edilen işler](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aopen%20label%3Astatus%3Aaccepted) | Henüz başlanmamış kabul edilmiş işler |
| [Geliştirilenler](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aopen%20label%3Astatus%3Ain-progress) | Üzerinde çalışılan işler |
| [Engelli işler](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aopen%20label%3Astatus%3Ablocked) | Dış bağımlılık veya çözülmemiş ön koşul |
| [Doğrulama/sürüm bekleyenler](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aopen%20label%3Astatus%3Averification) | Uygulanmış, doğrulama veya teslim bekleyen işler |
| [Yüksek öncelik](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aopen%20label%3Apriority%3Ahigh) | Sorumlunun acil olarak değerlendirdiği işler |
| [Kapalı kayıtlar](https://github.com/tamerinal/sap_mcp_windows/issues?q=is%3Aissue%20is%3Aclosed) | Tamamlanan, mükerrer veya planlanmayan kayıtlar; kapanış açıklamasını okuyun |

Boş görünüm henüz eşleşen kayıt olmadığını gösterir. Kabul veya öncelik etiketi teslim tarihi taahhüdü değildir. Milestone yalnız hedef sürüm gerçekten planlandığında atanır. Kesin teslim, kayıttaki sürüm bağlantısı ve sürüm geçmişiyle belirtilir.

## Sorumlu kişinin iş akışı

1. **İnceleme:** Formlar `bug` veya `enhancement` ile `status:triage` etiketini otomatik uygular. API/CLI ile açılanlar formları atlayabilir; bunları elle etiketleyin. Mükerrerlik, tekrar edilebilirlik, kapsam ve hassas içeriği kontrol edin. Alan etiketi atayıp önceliği değerlendirin.
2. **Bilgi veya kabul:** Eksik bilgide somut sorularla `status:needs-info` kullanın. Bilgi yeterli olunca `status:accepted` ile değiştirin. Kapsam ve kabul ölçütlerini yazın. Durumları elle güncelleyin; yeni yorum kaydı otomatik taşımaz.
3. **Başlama:** İş başladığında sorumluyu atayıp `status:in-progress` seçin. Dal/commit/PR’ı issue numarasıyla bağlayın. Bağımlılık varsa `status:blocked` kullanıp gerekeni açıklayın; uygunsa ilgili public kaydı bağlayın.
4. **Doğrulama:** `status:verification` kullanın; değişikliği ve gerçek test kanıtını yazın. Yapılmayan temiz Windows/canlı SAP testlerini belirtin. Taahhüt edilen kurulum veya gerekli doğrulama bekliyorsa kaydı açık tutun.
5. **Teslim ve kapanış:** Yayımlanan sürüm ve doğrulama sonucunu bağlayın; kabul ölçütleri karşılandığında durum etiketini kaldırıp **Completed** olarak kapatın. Mükerrerde asıl kaydı bağlayıp `duplicate`, reddedilende gerekçeyle `wontfix`/Not planned kullanın. Reddedilen işi sessizce tamamlandı olarak kapatmayın. Sorun sürerse aynı kayıtta yeni sürüm/tekrar bilgisiyle yeniden açılmasını görüşün.

Açık, iş gerektiren her kayıtta tek `status:*`, en fazla tek `priority:*` ve ilgili bir veya birkaç `area:*` etiketi kullanın. Sabitlenen bildirim rehberi `tracking`/`documentation` kaydıdır; geliştirme kuyruğuna girmez ve durum etiketi taşımaz.

| Etiket grubu | Değerler |
|---|---|
| Tür | `bug`, `enhancement`; gerektiğinde mevcut `documentation`, `duplicate`, `wontfix` |
| Öncelik | `priority:high` (engelleyici/ciddi etki), `priority:normal`, `priority:low` |
| Alan | `area:installer`, `area:rfc`, `area:adt`, `area:connections`, `area:clients`, `area:ui`, `area:docs` |

Özel RFC/ADT depolarındaki işler burada maskeli public özet ve bağımlılık/sonuçla takip edilebilir. Özel kaynak, gizli bağlantı veya müşteri verisi public kayda kopyalanmaz. İlgili projeye elle aktarma sorumlunun işlemidir.

Varsayılan `YAPAY_ZEKA` dalını hedefleyen PR’da doğrulama/sürüm beklerken `Refs #123` kullanın. `Fixes #123` / `Closes #123` varsayılan dala birleştirmede kaydı otomatik kapatır; bu anahtarları yalnız birleştirme anında kabul koşulları gerçekten sağlanıyorsa kullanın. Aksi halde teslimden sonra elle kapatın. İngilizce/Türkçe belgeleri ve sürüm geçmişini birlikte güncelleyin.

## Altyapının bakımı

Formlar `.github/ISSUE_TEMPLATE/`, yönetilen etiketler `.github/labels.json` içindedir. Yetkili sorumlu, GitHub CLI oturumu açıkken `./scripts/Sync-IssueLabels.ps1` ile etiketleri oluşturup güncelleyebilir. Listede olmayan etiketler ve mevcut kayıtlar korunur. Betik gelecekteki bildirimleri otomatik işlemez. Sabit rehber ve README bağlantılarını güncel tutun; sorumlular talepleri ele alırken kuyrukları inceler. Yanıt süresi taahhüdü, zamanlanmış takip veya otomatik geliştirme yapılandırılmamıştır.

GitHub kaynakları: [form sözdizimi](https://docs.github.com/en/communities/using-templates-to-encourage-useful-issues-and-pull-requests/syntax-for-issue-forms), [PR ve issue bağlama](https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/linking-a-pull-request-to-an-issue), [bildirim abonelikleri](https://docs.github.com/en/account-and-profile/managing-subscriptions-and-notifications-on-github).

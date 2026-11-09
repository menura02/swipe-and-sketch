# Doğrulama

## Otomatik kontroller

`dotnet run --project tests/TouchpadDraw.Tests/TouchpadDraw.Tests.csproj -c Release`

Çekirdek testleri iki mod, tuş tekrarları, değiştirici tuşlar, eksiksiz Space/Esc dizileri, odak değişimi, yeniden başlatma, alan dışına çıkış, fiziksel tuş çakışması, basma/bırakma hataları ve yinelenen temizlemeyi kapsar. Kurtarma testleri son sahiplik durumunu, bağlantı kopmasını ve ana süreç çıkmadan bırakma yapılmamasını kapsar. Testler gerçek fare olayları üretmez.

## Manuel kabul listesi

Paint ve Photoshop için aşağıdaki maddeleri ayrı ayrı gerçek touchpad ile doğrulayın. Kaydedilmiş kullanıcı belgesi yerine boş bir test tuvali kullanın.

- [ ] Hedef ve alan seçimi; seçim tıklamaları tuvale ulaşmıyor.
- [ ] Bas-aç modunda ilk Space çiziyor, bırakma çizimi sürdürür, ikinci Space bitirir.
- [ ] Basılı tutma modunda Space basılıyken çiziyor, bırakınca bitirir.
- [ ] Tuş tekrarı çizimi açıp kapatmıyor; parmağı kaldırıp koymak devam ettiriyor.
- [ ] Etkin oturumda Space boşluk veya tuval kaydırma üretmiyor.
- [ ] Ctrl+Alt+D sonrasında Space metin alanında normal çalışıyor.
- [ ] Esc duraklatıyor; başka uygulamada Space normal çalışıyor.
- [ ] Space basılıyken odak değiştirmek ve geri dönmek çizimi yeniden başlatmıyor.
- [ ] Alt+Tab, küçültme, hedefi kapatma, ekran kilidi ve uyku çizimi bitiriyor.
- [ ] Alan dışına çıkış ve üstüne başka pencere gelmesi çizimi bitiriyor.
- [ ] Alan içine dönüş için yeni Space basışı gerekiyor.
- [ ] Pencere taşıma alanı koruyor; boyut/DPI değişimi alanı geçersiz kılıyor.
- [ ] Fiziksel sol tuş basılıyken çizim başlamıyor; çizim sırasındaki fiziksel tıklama duraklatıyor.
- [ ] Mod değiştirme ve normal çıkış basılı tuş bırakmıyor.
- [ ] Ana süreç zorla kapatıldığında kurtarma süreci tuşu bırakıyor (boş test tuvalinde).
- [ ] Yüksek yetkili hedef anlaşılır hatayla engelleniyor.
- [ ] Basılı tutma sırasında touchpad sürücüsü hareketi baskılamıyor.
- [ ] İkinci örnek yeni hook kurmadan mevcut ayarları açıyor.
- [ ] Duraklatma kısayolu çakışınca hata gösteriliyor; başka harf seçilebiliyor.

## Bu ortamda doğrulama durumu

- 1.1.0 için 51 otomatik testin tamamı geçti: seçilebilir tuş, tuş değişirken basılı tuşun bırakılması, eski tuşun tekrar ve bırakma olaylarının yutulması, değiştirici kombinasyonları ve hızlı tekrar yolu regresyonları dahil.
- Gerçek WinForms kontrolleri odak almayan, saydam, ekran dışı bir pencerede PNG’ye çizildi; yeni tasarım görsel olarak incelendi. Eski JSON ayarları, özel tuşun JSON kaydı ve gömülü tepsi simgelerinin yüklenmesi bu önizleme yolunda doğrulandı.
- Yayınlanan bağımsız 1.1.0 EXE dosyasının önizleme yolu da çalıştırıldı; ayar ve simge kontrolleri paket üzerinden geçti. Yayın uyarısız tamamlandı.
- Logo 16, 24, 32, 48, 64 ve 96 pikselde görsel olarak incelendi; ICO dosyaları ayrıca 20, 40, 128 ve 256 piksel katmanlar içerir.
- 1.0.1 sürümünde 33 otomatik senaryonun tamamı geçti. Basılı tutma gecikmesi için Space tekrarlarında pencere/modifier kontrollerini atlayan yol eklendi; 10.000 tekrar olayı, güncel hedef doğrulaması ve gecikmeden bırakma için üç regresyon testi bulunur. Bu kontroller gerçek cihazdaki takılmanın giderildiğini doğrulamaz.
- Uygulama derlendi ve Windows’ta açıldı; ilk pencerenin erişilebilirlik ağacı ve görüntüsü incelendi.
- Ekran üzerinden inceleme fiziksel Esc ile kullanıcı tarafından durduruldu; sonraki UI işlemleri yapılmadı.
- Paint ve Photoshop’ta çizim, gerçek parmak hareketi, farklı DPI, ekran kilidi ve zorla kapatma senaryoları **doğrulanmadı**. Yukarıdaki kutular bu nedenle işaretlenmedi.
- Canlı uygulamada seçilen tuşun Paint/Photoshop üzerinde davranışı ve fiziksel touchpad denemeleri bu sürümde yapılmadı; çekirdek davranışları otomatik testlerle kontrol edildi.

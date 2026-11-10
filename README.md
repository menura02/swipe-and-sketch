# Swipe & Sketch

Paint, Photoshop ve fareyle çizimi destekleyen diğer Windows programlarında, touchpad’e basılı tutmadan çizim yapmayı sağlar. Arka planda sol fare düğmesini yönetir; kendi tuvali yoktur.

## Çalıştırma

Windows 11 x64 için taşınabilir paketi oluşturmak için aşağıdaki yayın komutunu çalıştırın. Oluşturulan paket `artifacts/portable-win-x64-1.1.1` klasöründedir. Önce eski uygulamada sistem tepsisi menüsünden **Çıkış** seçin; ardından yeni klasördeki `TouchpadDrawHelper.exe` dosyasını açın. Klasörü bütün olarak taşıyın. .NET kurulumu gerekmez. Uygulama normal kullanıcı yetkisiyle çalışır.

1. Paint veya Photoshop’u açın ve fırça aracını seçin.
2. Yardımcıda **Pencere seç** ile hedef pencereyi seçin.
3. **Çizim alanını seç** ile yalnızca tuval üzerinde bir dikdörtgen çizin. Esc seçimi iptal eder. Menüleri ve araç çubuklarını alanın dışında tutun.
4. **Kontroller → Çizim tuşu** bölümünden Space, F6 veya başka bir tuş seçin. **Aç / kapat** ya da **Basılı tut** modunu seçip **Oturumu etkinleştir** düğmesine basın.
5. İmleci seçilen alana getirin ve seçtiğiniz tuşla çizin.

Çizim tuşu varsayılan olarak Space’tir. Harfler, rakamlar, yön tuşları, F1–F11, F13–F24 ve listedeki diğer normal tuşlar seçilebilir. Esc durdurma için ayrılmıştır; değiştirici ve kilit tuşları çizim tuşu olarak sunulmaz. Tuş veya mod değiştirmek oturumu duraklatır. Tuş tercihi otomatik kaydedilir; eski ayarlar Space varsayılanıyla açılır.

| Mod / kontrol | Davranış |
| --- | --- |
| Aç / kapat | Seçilen tuşa bir kez basınca çizim başlar, tekrar basınca biter. |
| Basılı tut | Seçilen tuş basılıyken çizer, bırakınca bitirir. |
| Ctrl+Alt+D | Oturumu duraklatır / yeniden etkinleştirir. Harf ayarlardan değiştirilebilir. |
| Esc | Etkin oturumu duraklatır ve çizimi bitirir. |

Parmağı kaldırıp touchpad’in ortasına yeniden koymak çizgiyi bitirmez. Ayrı bir çizgi için çizimi bitirin, imleci taşıyın ve yeniden başlayın. Etkin hedef pencerede seçilen çizim tuşu normal programa iletilmez; o tuşla yazı yazmak veya uygulamanın kendi kısayolunu kullanmak için oturumu duraklatın. Ctrl/Alt/Shift/Windows ile birlikte basılan tuş normal programa iletilir. Örneğin F6 seçerseniz Space, Photoshop’ta normal tuval kaydırma görevini korur.

Pencere değişince veya imleç alan dışına çıkınca çizim biter. Geri dönmek çizimi otomatik başlatmaz; seçtiğiniz tuşu bırakıp yeniden basın. Pencere taşınabilir; boyut veya ekran ölçeği değişirse alanı yeniden seçin. Alan sınırı kırpma yapmaz; sınırdan çıkarken küçük bir taşma olabilir.

Pencereyi kapatmak uygulamayı sistem tepsisine gizler. Tepsi simgesine çift tıklayarak ayarları açın; sağ tıklayarak mod değiştirin veya **Çıkış** seçin. İkinci kez çalıştırmak mevcut ayarları açar. Her açılış duraklatılmıştır; hedef ve alan yeniden seçilir.

## Touchpad ve uyumluluk

- Basılı tutma modunda imleç takılıyor, bas-aç modunda akıcı çalışıyorsa Windows’un klavye sonrası touchpad hareketini baskılaması olasıdır. **Ayarlar → Bluetooth ve cihazlar → Dokunmatik yüzey → Dokunmalar → Dokunmatik yüzey hassasiyeti → En yüksek hassasiyet** seçeneğini deneyin (üreticiye göre ad ve konum değişebilir). [Microsoft açıklaması](https://learn.microsoft.com/en-us/windows-hardware/design/component-guidelines/touchpad-tuning-guidelines). Uygulama Windows ayarlarınızı değiştirmez. Bu ayar etkisizse bas-aç modunu kullanın ve touchpad üreticisinin klavye sırasında dokunmatik yüzey engelleme ayarını kontrol edin.
- Hedef program normal kullanıcı yetkisiyle çalışmalıdır. Daha yüksek yetkili hedefte etkinleştirme engellenir.
- Fırça kalınlığı, renk, silgi, geri alma ve kayıt hedef uygulamanın kontrolleridir. Kalem basıncı üretilmez.
- Fiziksel sol düğme zaten basılıyken sentetik çizim başlamaz. Çizim sırasında gerçek sol tıklama oturumu duraklatır.
- Bağımsız kurtarma süreci beklenmedik kapanışta yardımcıya ait basılı fare tuşunu bırakmayı dener. Ana uygulama ve kurtarma süreci birlikte zorla kapatılırsa bu garanti edilemez; fiziksel sol tuşa basıp bırakın.
- Ayarlar `%LocalAppData%\TouchpadDrawHelper\settings.json` içinde saklanır. Tuş içerikleri ve çizimler kaydedilmez. Otomatik başlangıç, ağ bağlantısı veya sürücü kurulumu yoktur.

## Geliştirme ve test

Windows ve .NET 10 SDK gerekir. Harici çalışma zamanı veya test kütüphanesi bağımlılığı yoktur.

```powershell
dotnet build src/TouchpadDraw.App/TouchpadDraw.App.csproj -c Release
dotnet run --project tests/TouchpadDraw.Tests/TouchpadDraw.Tests.csproj -c Release
dotnet run --project src/TouchpadDraw.App/TouchpadDraw.App.csproj
dotnet publish src/TouchpadDraw.App/TouchpadDraw.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/portable-win-x64-1.1.1
```

Çekirdek (`TouchpadDraw.Core`) durum makinesini ve kurtarma protokolünü platformdan bağımsız tutar. WinForms uygulaması Windows hook’larını, `SendInput`, pencere/DPI takibini, seçim katmanını ve tepsiyi yönetir. Test programı gerçek klavye veya fare girdisi üretmeden çalışır ve başarısızlıkta sıfırdan farklı çıkış kodu döndürür.

Manuel doğrulama listesi ve gerçekleştirilmiş testlerin sınırları için [TESTING.md](TESTING.md) dosyasına bakın.

## Tasarım ve logo

1.1.0 sürümü açık renkli kartlar, petrol yeşili başlıklar, iki seçimli mod kontrolü ve görünür oturum durumuyla yenilendi. Özgün touchpad/kalem logosu pencere, çalıştırılabilir dosya ve sistem tepsisinde kullanılır. Tepsi simgesindeki nokta duraklatıldığında gri, hazırken limon yeşili, çizerken turkuaz, hatada mercan rengidir.

Logo kaynağı `src/TouchpadDraw.App/Assets/logo.svg` dosyasındadır. ICO dosyaları 16–256 piksel boyutları içerir ve çalıştırılabilir dosyaya gömülüdür. Simgeyi yeniden üretmek için:

```powershell
dotnet run --project tools/AssetBuilder/AssetBuilder.csproj -c Release -- src/TouchpadDraw.App/Assets
```

Gerçek WinForms kontrollerini masaüstü odağı almadan, hook veya watchdog başlatmadan önizlemek için:

```powershell
dotnet src/TouchpadDraw.App/bin/Release/net10.0-windows/TouchpadDrawHelper.dll --preview artifacts/design-preview.png
```

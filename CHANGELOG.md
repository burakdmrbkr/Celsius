# Changelog

Bu projedeki önemli değişiklikler sürüm numarası mantığında tutulur. Sürümler, `main`'e her
push'ta çalışan otomatik workflow tarafından üretilir ve GitHub Releases ile Inno Setup kurulum
paketleri birlikte yayınlanır.

## [Unreleased] — sıradaki beta

### Eklendi
- **İşlemci veritabanı genişletildi:** Intel **2. nesil (Sandy Bridge)** ile **14. nesil
  (Raptor Lake)** arasındaki tüm ana masaüstü işlemciler + AMD **AM4 soketinin tüm Ryzen**
  (1.–5. nesil, Zen/Zen+/Zen 2/Zen 3 ve tüm APU'lar) modelleri eklendi. Her model için TjMax,
  önerilen sürekli sıcaklık (SustainedMaxTemp) ve modele özgü **risk/bakım notu** tanımlandı.
- **Risk ve bakım önerileri:** Modelin risk notu, panelde işlemci adının yanında gösteriliyor;
  AI danışmanı duruma göre önleyici bakım önerileri üretiyor (termal macun yenileme, kasa
  tozu/hava akışı, fan profili, soğutucu yeterliliği, güncel BIOS/microcode).
- **Eşleştirme sağlamlaştırıldı:** Eşit skorda daha spesifik (daha çok kelime eşleşen) model
  tercih ediliyor; genişleyen veri tabanı sayesinde eski nesil ve AM4 işlemciler artık doğru
  tanınıyor.

## [v1.9-beta] - 2026-08-09

### Değişti
- Logo yenilendi ve ikon altyapısı güçlendirildi: kare (1:1) tasarım, şeffaf kenarların otomatik
  kırpılması, tüm boyutlar (16–256) için gerçek DIB/BMP girişleri + 256 px PNG, 1024×1024 kaynak
  kullanımı → görev çubuğu ve başlık çubuğu ikonu keskin görünüyor.
- Sol üstte marka alanında kar tanesi yerine uygulama logosu görüntülüyor.

## [v1.6-beta] - 2026-08-09

### Eklendi
- **Per-CCD sıcaklık göstergesi:** Per-core sıcaklık sensörü olmayan CPU'larda (ör. AMD Ryzen
  5 5600'ün LHM/SMU okuması yalnızca `Core (Tctl/Tdie)` ve `CCD1 (Tdie)` veriyor) "çekirdek"
  bölümü artık boş kalmıyor; "CCD SICAKLIKLARI" olarak her CCD'nin sıcaklığı (°C) kart biçiminde
  gösteriliyor. Per-core sıcaklık sensörü olan sistemlerde davranış değişmedi.

## [v1.5-beta] - 2026-08-09

### Değişti
- Uygulama ikonu güncelendi (en son indirilen Gemini logosu ile yeniden üretildi).

## [v1.4-beta] - 2026-08-09

### Eklendi
- Uygulama ikonu: Gemini logosundan türetilen `Resources/app.ico` (16/32/48/256). Görev
  çubuğu, pencere başlık çubuğu ve **kurulum (Inno Setup) ikonu** olarak kullanılıyor
  (`ApplicationIcon`, `Window.Icon`, `SetupIconFile`).

## [v1.3-beta] - 2026-08-09

### Değişti
- Sol kenar çubuğu yerine **üst yatay gezinme çubuğu**: markalar solda, sekmeler (Genel Bakış /
  Disk Sağlığı / Stress Test) soldan sağa diziliyor; alt bilgi notu pencere tabanına taşındı.

### Eklendi
- `CpuDatabaseService` seed verisine **"Ryzen 5 5600"** kaydı (TjMax 90, SustainedMaxTemp 80).

## [v1.2-beta] - 2026-08-09

### Düzeltmeler
- Otomatik sürüm release workflow'u sağlamlaştırıldı; csproj sürüm alanları onarıldı.

## [v1.1-beta] - 2026-08-09

### Eklendi
- **PawnIO çekirdek sürücüsü otomatik kurulumu**: uygulama açılışında sürücü yoksa
  `PawnIO_setup.exe -install` ile bir kez kurulur; kurulum da sürücüyü kullanıcı onayıyla bir
  kez kurar.
- Inno Setup dağıtımı + GitHub Actions otomatik sürüm (minor+1) akışı.

## [v1.0.0-beta] - ilk yayın

### Değişti
- HWiNFO arka ucu kaldırıldı; sensör okumaları doğrudan **LibreHardwareMonitor** üzerinden
  yapılıyor.
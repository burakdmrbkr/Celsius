# Changelog

Bu projedeki önemli değişiklikler sürüm numarası mantığında tutulur. Sürümler, `main`'e her
push'ta çalışan otomatik workflow tarafından üretilir ve GitHub Releases ile Inno Setup kurulum
paketleri birlikte yayınlanır.

## [Unreleased] — v1.6-beta (yolda)

### Eklendi
- **Per-CCD sıcaklık göstergesi:** Per-core sıcaklık sensörü olmayan CPU'larda (ör. AMD Ryzen
  5 5600'ün LHM/SMU okuması yalnızca `Core (Tctl/Tdie)` ve `CCD1 (Tdie)` veriyor) "çekirdek"
  bölümü artık boş kalmıyor; bölüm başlığı otomatik olarak "CCD SICAKLIKLARI" oluyor ve her
  CCD'nin sıcaklığı (°C) kart biçiminde gösteriliyor. Per-core sıcaklık sensörü olan
  sistemlerde hiçbir şey değişmez.
- Sol üst köşede kar tanesi (❄) yerine **uygulama logosu** görseli gösteriliyor
  (`Resources/app_logo.png`, 64×64).

### Değişti
- Uygulama ikonu yeni Gemini logosu ile yeniden üretildi ve tüm boyutlar için **DIB/BMP**
  kayıtları (16…128 px) + **PNG** (256 px) içerecek şekilde düzeltildi → Windows/WPF artık
  ikonu 256 px'ten küçültmek yerine doğru boyuttaki girişi kullanıyor; görev çubuğu ve başlık
  çubuğu ikonu keskin görünüyor.

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
- Sol kenar çubuğu yerine **üst yatay gezinme çubuğu**: marka solda, sekmeler (Genel Bakış /
  Disk Sağlığı / Stress Test) soldan sağa diziliyor; alt bilgi notu pencere tabanına taşındı.

### Eklendi
- `CpuDatabaseService` seed verisine **"Ryzen 5 5600"** kaydı (TjMax 90, SustainedMaxTemp 80) —
  yerel referans veritabanında bu model için doğru eşleşme ve termal eşikler.

## [v1.2-beta] - 2026-08-09

### Düzeltmeler
- Otomatik sürüm release workflow'u sağlamlaştırıldı; csproj sürüm alanları onarıldı.

## [v1.1-beta] - 2026-08-09

### Eklendi
- **PawnIO çekirdek sürücüsü otomatik kurulumu**: uygulama açılışında sürücü yoksa
  `PawnIO_setup.exe -install` ile bir kez kurulur (requireAdministrator olduğu için ek UAC
  çıkmaz); kurulum da sürücüyü kullanıcı onayıyla bir kez kurar.
- Inno Setup dağıtımı + GitHub Actions otomatik sürüm (minor+1) akışı.

## [v1.0.0-beta] - ilk yayın

### Değişti
- HWiNFO arka ucu kaldırıldı; sensör okumaları doğrudan **LibreHardwareMonitor** üzerinden
  yapılıyor.
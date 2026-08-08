# ❄ Celsius — Akıllı Sıcaklık İzleyici

**AI destekli** bir Windows donanım izleme uygulaması. İşlemcinin kritik sıcaklığını
**SQLite veritabanındaki model eşlemesinden** bilir; anlık sensör verisini okur ve bakım gerekiyorsa
uyarır. Disk sağlığını (SMART) ve stres testi sırasındaki sıcaklık davranışını da izler.

## Özellikler

- 🔥 **Anlık sensör okuma** — uygulama açıldığında CPU paket/çekirdek sıcaklığı, yük, fan, voltaj, GPU
- 🧠 **AI danışman** — işlemci modeline özel **TjMax / kritik eşik** veritabanı eşlemesi; durum sınıflandırması
  (Sağlıklı → Normal → Dikkat → Bakım Gerekli → Kritik) ve oturum içi trend analizi (termal macun, fan temizliği önerisi)
- 💾 **Disk sağlığı (SMART)** — CrystalDiskInfo tarzı özet: sağlık yüzdesi, sıcaklık, çalışma saati, yeniden ayrılan sektör
- ⚡ **Stress test** — tüm çekirdeklerde yoğun hesaplama; yük altında sıcaklık izleme devam eder
- 🗄️ **SQLite referans veritabanı** — popüler Intel/AMD işlemcilerin termal eşikleri ilk çalıştırmada doldurulur
  (yerel, `%LocalAppData%\Celsius\celsius.db`)

> **Geçmiş kaydedilmez.** Yalnızca anlık veri gösterilir; trend analizi oturum boyunca bellekte tutulur.

## Teknoloji

| Katman | Araç |
|---|---|
| UI | WPF (.NET 8, MVVM — CommunityToolkit.Mvvm) |
| Sensör + SMART | [LibreHardwareMonitorLib](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) |
| Veritabanı | SQLite (Microsoft.Data.Sqlite) |

## Çalıştırma

En iyi sonuç (tüm sensörler + SMART) için **Yönetici olarak çalıştırın** (uygulama otomatik olarak
Yönetici istemi gösterir — WinRing0 sürücüsü nedeniyle).

```bash
dotnet build Celsius/Celsius.sln
dotnet run --project Celsius/Celsius
```

## Bilinen sınırlamalar

- **AMD SMU sıcaklığı bazı anakartlarda okunamıyor:** LibreHardwareMonitor, belirli anakart + Ryzen
  kombinasyonlarında (ör. MSI B550) işlemci paket sıcaklığını 0 olarak okuyabilir. Bu ölü okumalar
  filtrelenir ve "Sensör Yok" olarak gösterilir; yük, GPU ve disk verileri güncel kalır.
  Karşılaştırma için HWiNFO64 farklı bir okuma yöntemi kullandığından değer gösterebilir.
- **GPU sıcaklığı:** Hot Spot / Memory Junction yerine **GPU Core** değeri gösterilir.

## Yapı

```
Celsius/
├── Models/          # CpuModelInfo, SensorSnapshot, DiskHealthInfo, ...
├── Services/
│   ├── HardwareMonitorService.cs   # LibreHardwareMonitor sarmalayıcı
│   ├── CpuDatabaseService.cs       # SQLite referans verisi + eşleştirme
│   ├── DiskSmartService.cs         # SMART özeti
│   ├── StressTestService.cs        # CPU yük üretimi
│   └── AiAdvisorService.cs         # eşik + trend analizi
├── ViewModels/      # MainViewModel, DashboardViewModel, DiskViewModel, StressViewModel
└── Views/           # DashboardView, DiskView, StressView
```

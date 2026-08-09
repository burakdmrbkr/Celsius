using System.IO;
using Celsius.Models;
using Microsoft.Data.Sqlite;

namespace Celsius.Services;

/// <summary>
/// SQLite referans veritabanı. Tek görevi işlemci modellerini ve termal eşiklerini tutmak.
/// Geçmiş ölçüm verisi saklanmaz — uygulama anlık okur.
/// </summary>
public class CpuDatabaseService
{
    private readonly string _dbPath;

    public CpuDatabaseService(string? dbPath = null)
    {
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _dbPath = dbPath ?? Path.Combine(dir, "Celsius", "celsius.db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        Initialize();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }

    private void Initialize()
    {
        using var conn = Open();
        using var create = conn.CreateCommand();
        create.CommandText =
            """
            CREATE TABLE IF NOT EXISTS CpuModels (
                Id                INTEGER PRIMARY KEY AUTOINCREMENT,
                Manufacturer      TEXT NOT NULL,
                ModelName         TEXT NOT NULL,
                TjMax             REAL NOT NULL,
                SustainedMaxTemp  REAL NOT NULL,
                Note              TEXT
            );
            """;
        create.ExecuteNonQuery();
        SeedIfEmpty(conn);
    }

    /// <summary>Veritabanı boşsa işlemci modeli setini doldurur (idempotent).</summary>
    private static void SeedIfEmpty(SqliteConnection conn)
    {
        using var count = conn.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM CpuModels;";
        var existing = Convert.ToInt64(count.ExecuteScalar());
        if (existing > 0) return;

        var seed = new List<(string mfr, string model, double tj, double sust, string? note)>
        {
            // ============ INTEL — Sandy Bridge (2. nesil, LGA1155) ============
            ("Intel", "Core i7-2700K", 98, 90, "Eski sistem — termal macun ve soğutucu bakımı önemli; aşırı ısınma ihtimaline karşı kasa hava akışını kontrol edin."),
            ("Intel", "Core i7-2600K", 98, 90, null),
            ("Intel", "Core i7-2600", 98, 90, null),
            ("Intel", "Core i5-2500K", 98, 90, null),
            ("Intel", "Core i5-2500", 98, 90, null),
            ("Intel", "Core i5-2400", 98, 90, null),
            ("Intel", "Core i5-2300", 98, 90, null),
            ("Intel", "Core i3-2120", 98, 90, null),
            ("Intel", "Core i3-2100", 98, 90, null),

            // ============ Intel — Ivy Bridge (3. nesil, LGA1155) ============
            ("Intel", "Core i7-3770K", 105, 90, null),
            ("Intel", "Core i7-3770", 105, 90, null),
            ("Intel", "Core i5-3570K", 105, 90, null),
            ("Intel", "Core i5-3570", 105, 90, null),
            ("Intel", "Core i5-3470", 105, 90, null),
            ("Intel", "Core i5-3450", 105, 90, null),
            ("Intel", "Core i3-3240", 105, 90, null),
            ("Intel", "Core i3-3220", 105, 90, null),

            // ============ Intel — Haswell (4. nesil, LGA1150) ============
            ("Intel", "Core i7-4790K", 100, 90, null),
            ("Intel", "Core i7-4790", 100, 90, null),
            ("Intel", "Core i7-4770K", 100, 90, null),
            ("Intel", "Core i7-4770", 100, 90, null),
            ("Intel", "Core i5-4670K", 100, 90, null),
            ("Intel", "Core i5-4670", 100, 90, null),
            ("Intel", "Core i5-4590", 100, 90, null),
            ("Intel", "Core i5-4460", 100, 90, null),
            ("Intel", "Core i5-4430", 100, 90, null),
            ("Intel", "Core i3-4150", 100, 90, null),
            ("Intel", "Core i3-4130", 100, 90, null),
            // Haswell-E (LGA2011-3)
            ("Intel", "Core i7-5960X", 90, 85, "X99 platformu — güçlü soğutucu şart; termal macun düzenli yenilenmeli."),
            ("Intel", "Core i7-5930K", 90, 85, null),
            ("Intel", "Core i7-5820K", 90, 85, null),

            // ============ Intel — Broadwell (5. nesil) ============
            ("Intel", "Core i7-5775C", 100, 90, null),
            ("Intel", "Core i5-5675C", 100, 90, null),
            // Broadwell-E
            ("Intel", "Core i7-6950X", 90, 85, null),
            ("Intel", "Core i7-6900K", 90, 85, null),
            ("Intel", "Core i7-6850K", 90, 85, null),
            ("Intel", "Core i7-6800K", 90, 85, null),

            // ============ Intel — Skylake (6. nesil, LGA1151) ============
            ("Intel", "Core i7-6700K", 100, 90, null),
            ("Intel", "Core i7-6700", 100, 90, null),
            ("Intel", "Core i5-6600K", 100, 90, null),
            ("Intel", "Core i5-6600", 100, 90, null),
            ("Intel", "Core i5-6500", 100, 90, null),
            ("Intel", "Core i5-6400", 100, 90, null),
            ("Intel", "Core i3-6300", 100, 90, null),
            ("Intel", "Core i3-6100", 100, 90, null),

            // ============ Intel — Kaby Lake (7. nesil) ============
            ("Intel", "Core i7-7700K", 100, 90, null),
            ("Intel", "Core i7-7700", 100, 90, null),
            ("Intel", "Core i5-7600K", 100, 90, null),
            ("Intel", "Core i5-7600", 100, 90, null),
            ("Intel", "Core i5-7500", 100, 90, null),
            ("Intel", "Core i5-7400", 100, 90, null),
            ("Intel", "Core i3-7300", 100, 90, null),
            ("Intel", "Core i3-7100", 100, 90, null),

            // ============ Intel — Coffee Lake (8. nesil) ============
            ("Intel", "Core i7-8700K", 100, 90, null),
            ("Intel", "Core i7-8700", 100, 90, null),
            ("Intel", "Core i5-8600K", 100, 90, null),
            ("Intel", "Core i5-8600", 100, 90, null),
            ("Intel", "Core i5-8400", 100, 90, null),
            ("Intel", "Core i3-8300", 100, 90, null),
            ("Intel", "Core i3-8100", 100, 90, null),

            // ============ Intel — Coffee Lake Refresh (9. nesil) ============
            ("Intel", "Core i9-9900KS", 100, 90, null),
            ("Intel", "Core i9-9900K", 100, 90, null),
            ("Intel", "Core i9-9900KF", 100, 90, null),
            ("Intel", "Core i9-9900", 100, 90, null),
            ("Intel", "Core i7-9700K", 100, 90, null),
            ("Intel", "Core i7-9700KF", 100, 90, null),
            ("Intel", "Core i7-9700", 100, 90, null),
            ("Intel", "Core i5-9600K", 100, 90, null),
            ("Intel", "Core i5-9600KF", 100, 90, null),
            ("Intel", "Core i5-9500", 100, 90, null),
            ("Intel", "Core i5-9400F", 100, 90, null),
            ("Intel", "Core i5-9400", 100, 90, null),
            ("Intel", "Core i3-9100F", 100, 90, null),
            ("Intel", "Core i3-9100", 100, 90, null),

            // ============ Intel — Comet Lake (10. nesil) ============
            ("Intel", "Core i9-10900K", 100, 90, null),
            ("Intel", "Core i9-10900KF", 100, 90, null),
            ("Intel", "Core i9-10850K", 100, 90, null),
            ("Intel", "Core i7-10700K", 100, 90, null),
            ("Intel", "Core i7-10700KF", 100, 90, null),
            ("Intel", "Core i7-10700", 100, 90, null),
            ("Intel", "Core i5-10600K", 100, 90, null),
            ("Intel", "Core i5-10600KF", 100, 90, null),
            ("Intel", "Core i5-10500", 100, 90, null),
            ("Intel", "Core i5-10400", 100, 90, null),
            ("Intel", "Core i5-10400F", 100, 90, null),
            ("Intel", "Core i3-10300", 100, 90, null),
            ("Intel", "Core i3-10100", 100, 90, null),
            ("Intel", "Core i3-10100F", 100, 90, null),

            // ============ Intel — Rocket Lake (11. nesil) ============
            ("Intel", "Core i9-11900K", 100, 90, null),
            ("Intel", "Core i9-11900KF", 100, 90, null),
            ("Intel", "Core i7-11700K", 100, 90, null),
            ("Intel", "Core i7-11700KF", 100, 90, null),
            ("Intel", "Core i7-11700", 100, 90, null),
            ("Intel", "Core i5-11600K", 100, 90, null),
            ("Intel", "Core i5-11600KF", 100, 90, null),
            ("Intel", "Core i5-11500", 100, 90, null),
            ("Intel", "Core i5-11400", 100, 90, null),
            ("Intel", "Core i5-11400F", 100, 90, null),

            // ============ Intel — Alder Lake (12. nesil) ============
            ("Intel", "Core i9-12900KS", 100, 90, null),
            ("Intel", "Core i9-12900K", 100, 90, null),
            ("Intel", "Core i9-12900KF", 100, 90, null),
            ("Intel", "Core i9-12900", 100, 90, null),
            ("Intel", "Core i7-12700K", 100, 90, null),
            ("Intel", "Core i7-12700KF", 100, 90, null),
            ("Intel", "Core i7-12700", 100, 90, null),
            ("Intel", "Core i5-12600K", 100, 90, null),
            ("Intel", "Core i5-12600KF", 100, 90, null),
            ("Intel", "Core i5-12600", 100, 90, null),
            ("Intel", "Core i5-12500", 100, 90, null),
            ("Intel", "Core i5-12400", 100, 90, null),
            ("Intel", "Core i5-12400F", 100, 90, null),
            ("Intel", "Core i3-12100", 100, 90, null),
            ("Intel", "Core i3-12100F", 100, 90, null),

            // ============ Intel — Raptor Lake (13. nesil) ============
            ("Intel", "Core i9-13900KS", 100, 90, "Raptor Lake yüksek güç çekiyor — güçlü soğutucu ve güncel BIOS (microcode güncellemesi) önerilir."),
            ("Intel", "Core i9-13900K", 100, 90, "Raptor Lake yüksek güç çekiyor — güçlü soğutucu ve güncel BIOS (microcode güncellemesi) önerilir."),
            ("Intel", "Core i9-13900KF", 100, 90, null),
            ("Intel", "Core i9-13900", 100, 90, null),
            ("Intel", "Core i7-13700K", 100, 90, "Raptor Lake yüksek güç çekiyor — güçlü soğutucu ve güncel BIOS (microcode güncellemesi) önerilir."),
            ("Intel", "Core i7-13700KF", 100, 90, null),
            ("Intel", "Core i7-13700", 100, 90, null),
            ("Intel", "Core i5-13600K", 100, 90, null),
            ("Intel", "Core i5-13600KF", 100, 90, null),
            ("Intel", "Core i5-13500", 100, 90, null),
            ("Intel", "Core i5-13400", 100, 90, null),
            ("Intel", "Core i5-13400F", 100, 90, null),
            ("Intel", "Core i3-13100", 100, 90, null),
            ("Intel", "Core i3-13100F", 100, 90, null),

            // ============ Intel — Raptor Lake Refresh (14. nesil) ============
            ("Intel", "Core i9-14900KS", 100, 90, "Raptor Lake yüksek güç çekiyor — güçlü soğutucu ve güncel BIOS (microcode güncellemesi) önerilir."),
            ("Intel", "Core i9-14900K", 100, 90, "Raptor Lake yüksek güç çekiyor — güçlü soğutucu ve güncel BIOS (microcode güncellemesi) önerilir."),
            ("Intel", "Core i9-14900KF", 100, 90, null),
            ("Intel", "Core i9-14900", 100, 90, null),
            ("Intel", "Core i7-14700K", 100, 90, "Raptor Lake yüksek güç çekiyor — güçlü soğutucu ve güncel BIOS (microcode güncellemesi) önerilir."),
            ("Intel", "Core i7-14700KF", 100, 90, null),
            ("Intel", "Core i7-14700", 100, 90, null),
            ("Intel", "Core i5-14600K", 100, 90, null),
            ("Intel", "Core i5-14600KF", 100, 90, null),
            ("Intel", "Core i5-14500", 100, 90, null),
            ("Intel", "Core i5-14400", 100, 90, null),
            ("Intel", "Core i5-14400F", 100, 90, null),
            ("Intel", "Core i3-14100", 100, 90, null),
            ("Intel", "Core i3-14100F", 100, 90, null),

            // ============ AMD — AM4: Summit Ridge (Ryzen 1. nesil, Zen) ============
            ("AMD", "Ryzen 7 1800X", 95, 80, "Birinci nesil Ryzen boşta dalgalı sıcaklık verebilir; güncel BIOS ve güç planı önerilir."),
            ("AMD", "Ryzen 7 1700X", 95, 80, null),
            ("AMD", "Ryzen 7 1700", 95, 80, null),
            ("AMD", "Ryzen 5 1600X", 95, 80, null),
            ("AMD", "Ryzen 5 1600", 95, 80, null),
            ("AMD", "Ryzen 5 1500X", 95, 80, null),
            ("AMD", "Ryzen 5 1400", 95, 80, null),
            ("AMD", "Ryzen 3 1300X", 95, 80, null),
            ("AMD", "Ryzen 3 1200", 95, 80, null),

            // ============ AMD — AM4: Raven Ridge APU (Zen, 1. nesil) ============
            ("AMD", "Ryzen 5 2400G", 95, 80, "APU — Vega grafik birimi paylaşımı ısıyı artırır; kasa içi akış yeterli olmalı."),
            ("AMD", "Ryzen 3 2200G", 95, 80, null),

            // ============ AMD — AM4: Pinnacle Ridge (Ryzen 2. nesil, Zen+) ============
            ("AMD", "Ryzen 7 2700X", 95, 80, null),
            ("AMD", "Ryzen 7 2700", 95, 80, null),
            ("AMD", "Ryzen 5 2600X", 95, 80, null),
            ("AMD", "Ryzen 5 2600", 95, 80, null),
            ("AMD", "Ryzen 5 2500X", 95, 80, null),
            ("AMD", "Ryzen 3 2300X", 95, 80, null),

            // ============ AMD — AM4: Picasso APU (Zen+) ============
            ("AMD", "Ryzen 5 3400G", 95, 80, "APU — dahili GPU yük altında ek ısı üretir; fan profili ve macun yenileme önemli."),
            ("AMD", "Ryzen 3 3200G", 95, 80, null),

            // ============ AMD — AM4: Matisse (Ryzen 3. nesil, Zen 2) ============
            ("AMD", "Ryzen 9 3950X", 95, 85, null),
            ("AMD", "Ryzen 9 3900X", 95, 85, null),
            ("AMD", "Ryzen 9 3900XT", 95, 85, null),
            ("AMD", "Ryzen 7 3800X", 95, 85, null),
            ("AMD", "Ryzen 7 3800XT", 95, 85, null),
            ("AMD", "Ryzen 7 3700X", 95, 85, null),
            ("AMD", "Ryzen 5 3600XT", 95, 85, null),
            ("AMD", "Ryzen 5 3600X", 95, 85, null),
            ("AMD", "Ryzen 5 3600", 95, 85, null),
            ("AMD", "Ryzen 5 3500X", 95, 85, null),
            ("AMD", "Ryzen 5 3500", 95, 85, null),
            ("AMD", "Ryzen 3 3300X", 95, 85, null),
            ("AMD", "Ryzen 3 3100", 95, 85, null),

            // ============ AMD — AM4: Renoir APU (Zen 2) + 4000 serisi ============
            ("AMD", "Ryzen 7 4700G", 95, 85, "APU — güçlü dahili GPU; yeterli soğutma şart."),
            ("AMD", "Ryzen 5 4600G", 95, 85, null),
            ("AMD", "Ryzen 5 4500G", 95, 85, null),
            ("AMD", "Ryzen 3 4300G", 95, 85, null),
            ("AMD", "Ryzen 5 4500", 95, 85, null),
            ("AMD", "Ryzen 3 4100", 95, 85, null),

            // ============ AMD — AM4: Vermeer (Ryzen 5. nesil, Zen 3) ============
            ("AMD", "Ryzen 9 5950X", 90, 80, null),
            ("AMD", "Ryzen 9 5900X", 90, 80, null),
            ("AMD", "Ryzen 9 5900XT", 90, 80, null),
            ("AMD", "Ryzen 7 5800X", 90, 80, "X yongaları çekirdek yoğun — kaliteli soğutucu ve termal macun önerilir."),
            ("AMD", "Ryzen 7 5800X3D", 90, 80, "3D V-Cache yüzünden ısı üretimi yoğun — yük altında 80°C üzeri normaldir; iyi soğutucu şart."),
            ("AMD", "Ryzen 7 5700X", 90, 80, null),
            ("AMD", "Ryzen 7 5700X3D", 90, 80, null),
            ("AMD", "Ryzen 5 5600X", 90, 80, null),
            ("AMD", "Ryzen 5 5600", 90, 80, null),
            ("AMD", "Ryzen 5 5600X3D", 90, 80, "3D V-Cache — yük altında daha sıcak çalışır; soğutma buna göre ayarlanmalı."),
            ("AMD", "Ryzen 5 5500", 90, 80, null),

            // ============ AMD — AM4: Cezanne APU (Zen 3) ============
            ("AMD", "Ryzen 7 5700G", 95, 85, "APU — dahili GPU paylaşımı ısıyı artırır; iyi soğutucu ve hava akışı önerilir."),
            ("AMD", "Ryzen 5 5600G", 95, 85, null),
            ("AMD", "Ryzen 5 5500G", 95, 85, null),
            ("AMD", "Ryzen 3 5300G", 95, 85, null),

            // ============ AMD — AM5 (Zen 4, 7000 serisi; X3D TjMax 89) ============
            ("AMD", "Ryzen 9 7950X", 95, 85, null),
            ("AMD", "Ryzen 9 7950X3D", 89, 80, "3D V-Cache — 89°C TjMax; uzun süre 80°C üzeri önerilmez."),
            ("AMD", "Ryzen 9 7900X", 95, 85, null),
            ("AMD", "Ryzen 9 7900X3D", 89, 80, null),
            ("AMD", "Ryzen 7 7800X3D", 89, 80, "3D V-Cache — 89°C TjMax; uzun süre 80°C üzeri önerilmez."),
            ("AMD", "Ryzen 7 7700X", 95, 85, null),
            ("AMD", "Ryzen 5 7600X", 95, 85, null),
            ("AMD", "Ryzen 5 7600", 95, 85, null),

            // ============ INTEL MOBİL — Sandy Bridge (2. nesil) ============
            ("Intel", "Core i7-2920XM", 100, 90, "Mobil — laptop soğutma sınırlıdır; termal macun ve fan temizliği önemli."),
            ("Intel", "Core i7-2820QM", 100, 90, null),
            ("Intel", "Core i7-2760QM", 100, 90, null),
            ("Intel", "Core i7-2720QM", 100, 90, null),
            ("Intel", "Core i7-2630QM", 100, 90, null),
            ("Intel", "Core i5-2540M", 100, 90, null),
            ("Intel", "Core i5-2520M", 100, 90, null),
            ("Intel", "Core i5-2410M", 100, 90, null),
            ("Intel", "Core i3-2330M", 100, 90, null),
            ("Intel", "Core i3-2310M", 100, 90, null),

            // ============ Intel Mobil — Ivy Bridge (3. nesil; TjMax 105) ============
            ("Intel", "Core i7-3920XM", 105, 90, null),
            ("Intel", "Core i7-3820QM", 105, 90, null),
            ("Intel", "Core i7-3740QM", 105, 90, null),
            ("Intel", "Core i7-3720QM", 105, 90, null),
            ("Intel", "Core i7-3630QM", 105, 90, null),
            ("Intel", "Core i7-3610QM", 105, 90, null),
            ("Intel", "Core i5-3380M", 105, 90, null),
            ("Intel", "Core i5-3320M", 105, 90, null),
            ("Intel", "Core i5-3210M", 105, 90, null),
            ("Intel", "Core i3-3120M", 105, 90, null),
            ("Intel", "Core i3-3110M", 105, 90, null),

            // ============ Intel Mobil — Haswell (4. nesil) ============
            ("Intel", "Core i7-4930MX", 100, 90, null),
            ("Intel", "Core i7-4910MQ", 100, 90, null),
            ("Intel", "Core i7-4810MQ", 100, 90, null),
            ("Intel", "Core i7-4710MQ", 100, 90, null),
            ("Intel", "Core i7-4700MQ", 100, 90, null),
            ("Intel", "Core i7-4720HQ", 100, 90, null),
            ("Intel", "Core i7-4710HQ", 100, 90, null),
            ("Intel", "Core i5-4340M", 100, 90, null),
            ("Intel", "Core i5-4210M", 100, 90, null),
            ("Intel", "Core i5-4200M", 100, 90, null),
            ("Intel", "Core i3-4100M", 100, 90, null),
            ("Intel", "Core i3-4000M", 100, 90, null),

            // ============ Intel Mobil — Broadwell (5. nesil) ============
            ("Intel", "Core i7-5850HQ", 100, 90, null),
            ("Intel", "Core i7-5700HQ", 100, 90, null),
            ("Intel", "Core i7-5600U", 105, 90, null),
            ("Intel", "Core i7-5500U", 105, 90, null),
            ("Intel", "Core i5-5300U", 105, 90, null),
            ("Intel", "Core i5-5200U", 105, 90, null),
            ("Intel", "Core i3-5005U", 105, 90, null),

            // ============ Intel Mobil — Skylake (6. nesil) ============
            ("Intel", "Core i7-6820HK", 100, 90, null),
            ("Intel", "Core i7-6700HQ", 100, 90, null),
            ("Intel", "Core i5-6300HQ", 100, 90, null),
            ("Intel", "Core i7-6560U", 100, 90, null),
            ("Intel", "Core i7-6500U", 100, 90, null),
            ("Intel", "Core i5-6200U", 100, 90, null),
            ("Intel", "Core i3-6100U", 100, 90, null),

            // ============ Intel Mobil — Kaby Lake (7. nesil) ============
            ("Intel", "Core i7-7700HQ", 100, 90, null),
            ("Intel", "Core i5-7300HQ", 100, 90, null),
            ("Intel", "Core i7-7500U", 100, 90, null),
            ("Intel", "Core i5-7200U", 100, 90, null),
            ("Intel", "Core i3-7100U", 100, 90, null),

            // ============ Intel Mobil — Coffee Lake (8./9. nesil) ============
            ("Intel", "Core i7-8750H", 100, 90, null),
            ("Intel", "Core i5-8300H", 100, 90, null),
            ("Intel", "Core i7-8550U", 100, 90, null),
            ("Intel", "Core i5-8250U", 100, 90, null),
            ("Intel", "Core i3-8130U", 100, 90, null),
            ("Intel", "Core i9-9980HK", 100, 90, null),
            ("Intel", "Core i9-9880H", 100, 90, null),
            ("Intel", "Core i7-9880H", 100, 90, null),
            ("Intel", "Core i7-9750H", 100, 90, null),
            ("Intel", "Core i5-9300H", 100, 90, null),

            // ============ Intel Mobil — Comet Lake (10. nesil) ============
            ("Intel", "Core i9-10980HK", 100, 90, null),
            ("Intel", "Core i9-10880H", 100, 90, null),
            ("Intel", "Core i7-10875H", 100, 90, null),
            ("Intel", "Core i7-10750H", 100, 90, null),
            ("Intel", "Core i5-10500H", 100, 90, null),
            ("Intel", "Core i5-10300H", 100, 90, null),
            ("Intel", "Core i7-10710U", 100, 90, null),
            ("Intel", "Core i7-10510U", 100, 90, null),
            ("Intel", "Core i5-10210U", 100, 90, null),

            // ============ Intel Mobil — Tiger Lake (11. nesil) ============
            ("Intel", "Core i7-11800H", 100, 90, null),
            ("Intel", "Core i5-11400H", 100, 90, null),
            ("Intel", "Core i7-11370H", 100, 90, null),
            ("Intel", "Core i7-1185G7", 100, 90, null),
            ("Intel", "Core i7-1165G7", 100, 90, null),
            ("Intel", "Core i5-1135G7", 100, 90, null),
            ("Intel", "Core i3-1115G4", 100, 90, null),

            // ============ Intel Mobil — Alder Lake (12. nesil) ============
            ("Intel", "Core i9-12900HK", 100, 90, null),
            ("Intel", "Core i9-12900H", 100, 90, null),
            ("Intel", "Core i7-12800H", 100, 90, null),
            ("Intel", "Core i7-12700H", 100, 90, null),
            ("Intel", "Core i5-12600H", 100, 90, null),
            ("Intel", "Core i5-12500H", 100, 90, null),
            ("Intel", "Core i7-1260P", 100, 90, null),
            ("Intel", "Core i5-1240P", 100, 90, null),
            ("Intel", "Core i7-1255U", 100, 90, null),
            ("Intel", "Core i5-1235U", 100, 90, null),

            // ============ Intel Mobil — Raptor Lake (13. nesil) ============
            ("Intel", "Core i9-13980HX", 100, 90, null),
            ("Intel", "Core i9-13950HX", 100, 90, null),
            ("Intel", "Core i9-13900HX", 100, 90, null),
            ("Intel", "Core i7-13800HX", 100, 90, null),
            ("Intel", "Core i7-13700HX", 100, 90, null),
            ("Intel", "Core i7-13700H", 100, 90, null),
            ("Intel", "Core i5-13600HX", 100, 90, null),
            ("Intel", "Core i5-13600H", 100, 90, null),
            ("Intel", "Core i5-13500H", 100, 90, null),
            ("Intel", "Core i7-1370P", 100, 90, null),
            ("Intel", "Core i5-1340P", 100, 90, null),
            ("Intel", "Core i7-1365U", 100, 90, null),
            ("Intel", "Core i5-1335U", 100, 90, null),
            ("Intel", "Core i3-1315U", 100, 90, null),

            // ============ Intel Mobil — Raptor Lake Refresh (14. nesil) ============
            ("Intel", "Core i9-14900HX", 100, 90, null),
            ("Intel", "Core i7-14700HX", 100, 90, null),
            ("Intel", "Core i5-14600HX", 100, 90, null),
            ("Intel", "Core i5-14500HX", 100, 90, null),

            // ============ AMD Mobil — Raven Ridge (2000 serisi, Zen) ============
            ("AMD", "Ryzen 7 2700U", 95, 80, "Mobil — laptop soğutma sınırlı; fan/macun bakımı önemli."),
            ("AMD", "Ryzen 5 2500U", 95, 80, null),
            ("AMD", "Ryzen 3 2300U", 95, 80, null),

            // ============ AMD Mobil — Picasso (3000 serisi; TjMax 105) ============
            ("AMD", "Ryzen 7 3780U", 105, 90, null),
            ("AMD", "Ryzen 7 3700U", 105, 90, null),
            ("AMD", "Ryzen 5 3550H", 105, 90, null),
            ("AMD", "Ryzen 5 3500U", 105, 90, null),
            ("AMD", "Ryzen 3 3300U", 105, 90, null),
            ("AMD", "Ryzen 3 3200U", 105, 90, null),

            // ============ AMD Mobil — Renoir (4000 serisi; TjMax 105) ============
            ("AMD", "Ryzen 9 4900H", 105, 90, null),
            ("AMD", "Ryzen 9 4900HS", 105, 90, null),
            ("AMD", "Ryzen 7 4800H", 105, 90, null),
            ("AMD", "Ryzen 7 4800HS", 105, 90, null),
            ("AMD", "Ryzen 7 4700U", 105, 90, null),
            ("AMD", "Ryzen 5 4600H", 105, 90, null),
            ("AMD", "Ryzen 5 4600U", 105, 90, null),
            ("AMD", "Ryzen 5 4500U", 105, 90, null),
            ("AMD", "Ryzen 3 4300U", 105, 90, null),

            // ============ AMD Mobil — Cezanne (5000 serisi; TjMax 105) ============
            ("AMD", "Ryzen 9 5980HX", 105, 90, null),
            ("AMD", "Ryzen 9 5900HX", 105, 90, null),
            ("AMD", "Ryzen 7 5800H", 105, 90, null),
            ("AMD", "Ryzen 7 5800HS", 105, 90, null),
            ("AMD", "Ryzen 7 5700U", 105, 90, null),
            ("AMD", "Ryzen 5 5600H", 105, 90, null),
            ("AMD", "Ryzen 5 5600U", 105, 90, null),
            ("AMD", "Ryzen 5 5500U", 105, 90, null),
            ("AMD", "Ryzen 3 5400U", 105, 90, null),

            // ============ AMD Mobil — Rembrandt (6000 serisi; TjMax 95) ============
            ("AMD", "Ryzen 9 6980HX", 95, 80, "Rembrandt TjMax 95°C — yük altında 85°C üzeri uzun süre tutulmamalı."),
            ("AMD", "Ryzen 9 6900HX", 95, 80, null),
            ("AMD", "Ryzen 7 6800H", 95, 80, null),
            ("AMD", "Ryzen 7 6800U", 95, 80, null),
            ("AMD", "Ryzen 5 6600H", 95, 80, null),
            ("AMD", "Ryzen 5 6600U", 95, 80, null),

            // ============ AMD Mobil — Rembrandt-R (7000; TjMax 95) ============
            ("AMD", "Ryzen 7 7735H", 95, 80, null),
            ("AMD", "Ryzen 7 7735U", 95, 80, null),
            ("AMD", "Ryzen 5 7535H", 95, 80, null),
            ("AMD", "Ryzen 5 7535U", 95, 80, null),

            // ============ AMD Mobil — Phoenix (7040; TjMax 100) ============
            ("AMD", "Ryzen 9 7940HS", 100, 85, null),
            ("AMD", "Ryzen 9 7940H", 100, 85, null),
            ("AMD", "Ryzen 7 7840HS", 100, 85, null),
            ("AMD", "Ryzen 7 7840U", 100, 85, null),
            ("AMD", "Ryzen 5 7640HS", 100, 85, null),
            ("AMD", "Ryzen 5 7640U", 100, 85, null),

            // ============ AMD Mobil — Dragon Range (7045 HX; TjMax 100) ============
            ("AMD", "Ryzen 9 7945HX3D", 100, 85, "3D V-Cache — mobil HX ısı yoğun; güçlü soğutma şart."),
            ("AMD", "Ryzen 9 7945HX", 100, 85, null),
            ("AMD", "Ryzen 7 7745HX", 100, 85, null),
            ("AMD", "Ryzen 5 7645HX", 100, 85, null),

            // ============ AMD Mobil — Hawk Point (8000; TjMax 100) ============
            ("AMD", "Ryzen 9 8945HS", 100, 85, null),
            ("AMD", "Ryzen 7 8845HS", 100, 85, null),
            ("AMD", "Ryzen 7 8845U", 100, 85, null),
            ("AMD", "Ryzen 5 8645HS", 100, 85, null),
            ("AMD", "Ryzen 5 8640U", 100, 85, null),
            ("AMD", "Ryzen 5 8540U", 100, 85, null),
        };

        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            "INSERT INTO CpuModels (Manufacturer, ModelName, TjMax, SustainedMaxTemp, Note) VALUES ($m, $n, $t, $s, $note);";
        var pm = cmd.Parameters.Add("$m", SqliteType.Text);
        var pn = cmd.Parameters.Add("$n", SqliteType.Text);
        var pt = cmd.Parameters.Add("$t", SqliteType.Real);
        var ps = cmd.Parameters.Add("$s", SqliteType.Real);
        var pnote = cmd.Parameters.Add("$note", SqliteType.Text);
        foreach (var (mfr, model, tj, sust, note) in seed)
        {
            pm.Value = mfr; pn.Value = model; pt.Value = tj; ps.Value = sust;
            pnote.Value = note is null ? DBNull.Value : note;
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public IReadOnlyList<CpuModelInfo> GetAll()
    {
        var list = new List<CpuModelInfo>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Manufacturer, ModelName, TjMax, SustainedMaxTemp, Note FROM CpuModels ORDER BY Manufacturer, ModelName;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new CpuModelInfo
            {
                Id = reader.GetInt32(0),
                Manufacturer = reader.GetString(1),
                ModelName = reader.GetString(2),
                TjMax = reader.GetDouble(3),
                SustainedMaxTemp = reader.GetDouble(4),
                Note = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }
        return list;
    }

    public int Count()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM CpuModels;";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>
    /// Tespit edilen işlemci adını veritabanındaki modele en iyi eşleştirir.
    /// Ör. "Intel(R) Core(TM) i7-12700K CPU @ 3.60GHz" → i7-12700K.
    /// Kelime puanı + üretici bonusu; eşitlikte daha spesifik (çok kelime eşleşen) model kazanır.
    /// </summary>
    public CpuModelInfo? FindBestMatch(string? detectedName)
    {
        if (string.IsNullOrWhiteSpace(detectedName)) return null;

        var normalized = Normalize(detectedName);
        var isAmd = normalized.Contains("ryzen", StringComparison.OrdinalIgnoreCase)
                    || normalized.Contains("amd", StringComparison.OrdinalIgnoreCase);

        CpuModelInfo? best = null;
        var bestScore = 0;
        var bestWords = 0;

        foreach (var model in GetAll())
        {
            var modelNorm = Normalize(model.ModelName);
            var score = 0;
            var words = 0;
            foreach (var word in modelNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (word.Length < 2) continue;
                if (normalized.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    score += word.Length;
                    words++;
                }
            }
            // Üretici eşleşmesi bonusu
            if (score > 0 && isAmd == (model.Manufacturer == "AMD"))
                score += 10;

            // Daha yüksek skor kazanır; eşitlikte daha çok kelime eşleşeni tercih et
            if (score > bestScore || (score == bestScore && words > bestWords))
            {
                bestScore = score;
                bestWords = words;
                best = model;
            }
        }

        return bestScore >= 4 ? best : null;
    }

    /// <summary>Alfanümerik olmayan karakterleri boşluğa çevirip sıkıştırır.</summary>
    private static string Normalize(string s)
    {
        var cleaned = new string(s.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ').ToArray());
        return string.Join(" ", cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}

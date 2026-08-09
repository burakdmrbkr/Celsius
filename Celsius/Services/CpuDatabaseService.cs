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

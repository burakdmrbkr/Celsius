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

    /// <summary>Veritabanı boşsa popüler CPU modeli setini doldurur (idempotent).</summary>
    private static void SeedIfEmpty(SqliteConnection conn)
    {
        using var count = conn.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM CpuModels;";
        var existing = Convert.ToInt64(count.ExecuteScalar());
        if (existing > 0) return;

        var seed = new List<(string mfr, string model, double tj, double sust)>
        {
            // Intel — Raptor Lake / Alder Lake (14., 13., 12. nesil)
            ("Intel", "Core i9-14900K",  100, 90), ("Intel", "Core i9-14900KS", 100, 90),
            ("Intel", "Core i7-14700K",  100, 90), ("Intel", "Core i5-14600K",  100, 90),
            ("Intel", "Core i5-14400",   100, 90), ("Intel", "Core i3-14100",   100, 90),
            ("Intel", "Core i9-13900K",  100, 90), ("Intel", "Core i9-13900KS", 100, 90),
            ("Intel", "Core i7-13700K",  100, 90), ("Intel", "Core i5-13600K",  100, 90),
            ("Intel", "Core i5-13400",   100, 90), ("Intel", "Core i3-13100",   100, 90),
            ("Intel", "Core i9-12900K",  100, 90), ("Intel", "Core i7-12700K",  100, 90),
            ("Intel", "Core i5-12600K",  100, 90), ("Intel", "Core i5-12400",   100, 90),
            // Intel — Rocket Lake / Comet Lake
            ("Intel", "Core i9-11900K",  100, 90), ("Intel", "Core i7-11700K",  100, 90),
            ("Intel", "Core i5-11600K",  100, 90), ("Intel", "Core i5-11400",   100, 90),
            ("Intel", "Core i9-10900K",  100, 90), ("Intel", "Core i7-10700K",  100, 90),
            ("Intel", "Core i5-10600K",  100, 90), ("Intel", "Core i5-10400",   100, 90),
            // Intel — Coffe Lake / Sky Lake
            ("Intel", "Core i9-9900K",   100, 90), ("Intel", "Core i7-9700K",   100, 90),
            ("Intel", "Core i5-9600K",   100, 90), ("Intel", "Core i7-8700K",   100, 90),
            ("Intel", "Core i5-8600K",   100, 90), ("Intel", "Core i7-7700K",   100, 90),
            // AMD — Zen 4 (7000 serisi, TjMax 95)
            ("AMD",   "Ryzen 9 7950X",   95, 85), ("AMD",   "Ryzen 9 7950X3D", 95, 85),
            ("AMD",   "Ryzen 9 7900X",   95, 85), ("AMD",   "Ryzen 9 7900X3D", 95, 85),
            ("AMD",   "Ryzen 7 7800X3D", 95, 85), ("AMD",   "Ryzen 7 7700X",   95, 85),
            ("AMD",   "Ryzen 5 7600X",   95, 85), ("AMD",   "Ryzen 5 7600",    95, 85),
            // AMD — Zen 3 (TjMax 90)
            ("AMD",   "Ryzen 9 5950X",   90, 80), ("AMD",   "Ryzen 9 5900X",   90, 80),
            ("AMD",   "Ryzen 7 5800X",   90, 80), ("AMD",   "Ryzen 7 5800X3D", 90, 80),
            ("AMD",   "Ryzen 7 5700X",   90, 80), ("AMD",   "Ryzen 5 5600X",   90, 80),
            ("AMD",   "Ryzen 5 5600G",   90, 80), ("AMD",   "Ryzen 5 5600",    90, 80),
            ("AMD",   "Ryzen 5 5500",    90, 80),
            // AMD — Zen2
            ("AMD",   "Ryzen 9 3950X",   95, 85), ("AMD",   "Ryzen 9 3900X",   95, 85),
            ("AMD",   "Ryzen 7 3800X",   95, 85), ("AMD",   "Ryzen 7 3700X",   95, 85),
            ("AMD",   "Ryzen 5 3600X",   95, 85), ("AMD",   "Ryzen 5 3600",    95, 85)
        };

        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            "INSERT INTO CpuModels (Manufacturer, ModelName, TjMax, SustainedMaxTemp) VALUES ($m, $n, $t, $s);";
        var pm = cmd.Parameters.Add("$m", SqliteType.Text);
        var pn = cmd.Parameters.Add("$n", SqliteType.Text);
        var pt = cmd.Parameters.Add("$t", SqliteType.Real);
        var ps = cmd.Parameters.Add("$s", SqliteType.Real);
        foreach (var (mfr, model, tj, sust) in seed)
        {
            pm.Value = mfr; pn.Value = model; pt.Value = tj; ps.Value = sust;
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
    /// </summary>
    public CpuModelInfo? FindBestMatch(string? detectedName)
    {
        if (string.IsNullOrWhiteSpace(detectedName)) return null;

        var normalized = Normalize(detectedName);
        var isAmd = normalized.Contains("ryzen", StringComparison.OrdinalIgnoreCase)
                    || normalized.Contains("amd", StringComparison.OrdinalIgnoreCase);

        CpuModelInfo? best = null;
        var bestScore = 0;

        foreach (var model in GetAll())
        {
            var modelNorm = Normalize(model.ModelName);
            var score = 0;
            // Model adının her kelimesinin tespit edilen ad içinde geçip geçmediğine bak
            foreach (var word in modelNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (word.Length < 2) continue;
                if (normalized.Contains(word, StringComparison.OrdinalIgnoreCase))
                    score += word.Length;
            }
            // Üretici eşleşmesi bonusu
            if (score > 0 && isAmd == (model.Manufacturer == "AMD"))
                score += 10;

            if (score > bestScore)
            {
                bestScore = score;
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
namespace Celsius.Services;

/// <summary>
/// CPU'yu tüm çekirdeklerde yoğun hesaplamayla yüke sokar.
/// Yük çalışırken sensör servisi ölçüme devam eder; böylece yük altındaki sıcaklıklar izlenebilir.
/// </summary>
public sealed class StressTestService : IDisposable
{
    private CancellationTokenSource? _cts;
    private List<Task>? _workers;

    public bool IsRunning { get; private set; }

    public void Start()
    {
        if (IsRunning) return;
        _cts = new CancellationTokenSource();
        IsRunning = true;
        var token = _cts.Token;
        var threadCount = Math.Max(1, Environment.ProcessorCount);
        _workers = Enumerable.Range(0, threadCount)
            .Select(_ => Task.Run(() => Run(token)))
            .ToList();
    }

    public void Stop()
    {
        if (!IsRunning) return;
        IsRunning = false;
        _cts?.Cancel();
        _workers = null;
    }

    private static void Run(CancellationToken token)
    {
        // Deterministik yük üreten döngü: karekök + FMA benzeri işlemlerle tüm çekirdekleri doldurur.
        var buffer = new double[8192];
        var random = new Random(42);
        for (var i = 0; i < buffer.Length; i++) buffer[i] = random.NextDouble();

        while (!token.IsCancellationRequested)
        {
            for (var i = 0; i < buffer.Length - 1; i++)
            {
                buffer[i] = Math.Sqrt(buffer[i] * buffer[i] + 1.0) * 0.9999999 + buffer[i + 1] * 1e-12;
            }
            if (token.IsCancellationRequested) break;
        }
    }

    public void Dispose() => Stop();
}

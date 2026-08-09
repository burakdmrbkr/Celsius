using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Celsius.Services;

/// <summary>
/// LibreHardwareMonitor 0.9.6+ ring0 sürücüsünün (PawnIO) otomatik kurulumu.
/// LHM kütüphanesi sürücüyü kendiliğinden kurmaz; kurulum istemi yalnızca LHM'nin
/// GUI uygulamasında yaşar. Celsius kütüphaneyi doğrudan kullandığı için, açılışta
/// sürücü kurulu değilse gömülü PawnIO_setup.exe'yi çıkarıp "-install" ile çalıştırır.
/// Uygulama requireAdministrator ile zaten yönetici olduğundan ekstra UAC gerekmez.
/// </summary>
public static class PawnIoDriverInstaller
{
    private const string EmbeddedResource = "Celsius.Resources.PawnIO_setup.exe";

    public const string DriverDisplayName = "PawnIO";
    public const string UninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO";

    public static bool IsDriverInstalled
    {
        get
        {
            try { return LibreHardwareMonitor.PawnIo.PawnIo.IsInstalled; }
            catch { return false; }
        }
    }

    /// <summary>Gerekirse PawnIO sürücüsünü kurar; döner değer sürücünün nihai durumudur.</summary>
    public static bool EnsureInstalled()
    {
        try
        {
            if (IsDriverInstalled)
                return true;

            var setupPath = ExtractSetup();
            if (setupPath is null)
                return false;

            try
            {
                // ShellExecute (UseShellExecute=true) imzalı setup'un requireAdministrator
                // manifest'ini onurlandırır; üst süreç zaten yönetici olduğundan UAC çıkmaz.
                var psi = new ProcessStartInfo(setupPath, "-install") { UseShellExecute = true };
                using var p = Process.Start(psi);
                p?.WaitForExit(60_000);
            }
            finally
            {
                try { File.Delete(setupPath); } catch { /* temp, önemsiz */ }
            }

            return IsDriverInstalled;
        }
        catch
        {
            return false;
        }
    }

    private static string? ExtractSetup()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(EmbeddedResource);
        if (stream is null) return null;

        var dest = Path.Combine(Path.GetTempPath(), "Celsius_PawnIO_setup.exe");
        using (var fs = File.Create(dest))
            stream.CopyTo(fs);
        return dest;
    }
}
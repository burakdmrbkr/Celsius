using System.Windows;
using Celsius.Services;
using Celsius.ViewModels;

namespace Celsius;

/// <summary>
/// Uygulama giriş noktası: servisleri kurar ve ana pencereyi açar.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Beklenmeyen istisnaları yakala, çökme yerine mesaj göster
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                "Beklenmeyen bir hata oluştu:\n" + args.Exception.Message,
                "Celsius", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        // PawnIO ring0 sürücüsünü gerekirse otomatik kur — LHM sensörlerinden önce olmalı.
        // (Kütüphane sürücüyü kendisi kurmaz; CPU sıcaklığı için zorunludur.)
        if (!PawnIoDriverInstaller.EnsureInstalled())
        {
            MessageBox.Show(
                "CPU sıcaklığı için gereken PawnIO sürücüsü kurulamadı. " +
                "Sensörler sınırlı olabilir; uygulamayı 'Yönetici olarak çalıştır' ile tekrar açmayı deneyin.",
                "Celsius", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // Bağımlılıkları kur
        var cpuDb = new CpuDatabaseService();
        var hw = new HardwareMonitorService();
        var stress = new StressTestService();
        var advisor = new AiAdvisorService(cpuDb);

        var mainVm = new MainViewModel(hw, stress, advisor);
        var window = new MainWindow(mainVm);
        MainWindow = window;
        window.Show();
    }
}

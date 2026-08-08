using System.Windows;
using System.Windows.Threading;
using Celsius.Services;
using Celsius.ViewModels;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace Celsius;

/// <summary>
/// Uygulama giriş noktası: servisleri kurar, LiveCharts'ı yapılandırır ve ana pencereyi açar.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // LiveCharts2: SkiaSharp çizim motorunu etkinleştir
        LiveChartsCore.LiveCharts.Configure(config => config.AddSkiaSharp().AddDefaultMappers());

        // Beklenmeyen istisnaları yakala, çökme yerine mesaj göster
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                "Beklenmeyen bir hata oluştu:\n" + args.Exception.Message,
                "Celsius", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

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

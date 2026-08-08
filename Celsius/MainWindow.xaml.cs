using System.ComponentModel;
using System.Windows;
using Celsius.ViewModels;

namespace Celsius;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    private void Window_Closing(object? sender, CancelEventArgs e) => _vm.Shutdown();
}
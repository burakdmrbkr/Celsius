using System.Collections.ObjectModel;
using Celsius.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Celsius.ViewModels;

/// <summary>"Disk Sağlığı" sekmesi: SMART verisiyle disk kartları.</summary>
public partial class DiskViewModel : ObservableObject
{
    public ObservableCollection<DiskHealthInfo> Disks { get; } = new();

    [ObservableProperty] private bool _hasDisks;

    /// <summary>SMART özetini UI listesine yansıtır.</summary>
    public void Refresh(IReadOnlyList<DiskHealthInfo> disks)
    {
        Disks.Clear();
        foreach (var d in disks) Disks.Add(d);
        HasDisks = disks.Count > 0;
    }
}